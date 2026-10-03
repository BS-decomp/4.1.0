# Lightmaps — Block Strike 4.1.0

Status: **re-bound in all 57 lightmapped scenes**, verified offline.

```
$ python3 tools/verify_lightmaps.py
scenes 59 | with lightmaps 57 | correctly bound 57 | textures resolved 57
OK
```

## Why the maps looked flat

Nothing was lost — the bake was there the whole time. Three separate things
stopped it from reaching the screen:

**1. Unity 2021 ignores a Unity 4 `LightmapSettings`.** The scenes still carry

```yaml
m_Lightmaps:
- m_Lightmap: {fileID: 2800000, guid: 0830a6c4…, type: 3}
  m_IndirectLightmap: {fileID: 0}
m_LightmapsMode: 0
m_BakedColorSpace: 0
m_UseDualLightmapsInForward: 0
```

Unity 5 moved the baked array out of the scene into the LightingData asset and
renamed the rest, so a modern editor reads none of it:
`LightmapSettings.lightmaps` stays empty and the `LIGHTMAP_ON` keyword is never
enabled — while every renderer still has a perfectly good `m_LightmapIndex` and
`m_LightmapTilingOffset`.

**2. The index sentinels changed.** Unity 4 used `255` for "no lightmap" and
`254` for "lightmapped, not assigned"; Unity 5 moved them to `65535`/`65534`.
The export contained `255` on 5 063 renderers and `254` on 480, i.e. thousands
of requests for lightmap number 255 in scenes that have exactly one.

**3. The lightmap shaders could not sample a lightmap any more.**
`Mobile/Unlit (Supports Lightmap)` — the main map shader, 561 material
references — and `Mobile/VertexLit` do their lightmapping in fixed-function
`VertexLM` / `VertexLMRGBM` passes via `SetTexture [unity_Lightmap] { Matrix
[unity_LightmapMatrix] … }`. `unity_LightmapMatrix` and those combiners are
gone, so even a bound lightmap would not have been read: the geometry rendered
through the plain unlit `Vertex` pass. That is literally "flat".

## The fix

| Step | Tool | Result |
| --- | --- | --- |
| Re-bind the baked textures **and the per-renderer data** at load time | `tools/install_lightmap_binder.py --apply` + `client/Assets/Scripts/Recovery/BSLegacyLightmaps.cs` | 57 scenes got one small `BS Legacy Lightmaps` object. It assigns `LightmapSettings.lightmaps` (NonDirectional) from the original texture list **and restores `lightmapIndex` + `lightmapScaleOffset` on all 3 810 baked renderers** — Unity treats a scene without a LightingData asset as "never baked" and drops those per-renderer values on import, which is why binding the textures alone changed nothing. `[ExecuteAlways]`, so the Scene view matches the game and builds keep working. |
| Port the sentinels | `tools/fix_lightmap_indices.py --apply` | `255 → 65535` (5 063), `254 → 65534` (480); real indices (`0`, 3 810 renderers) untouched. |
| Make the shaders sample lightmaps again | `tools/rebuild_shaders.py` | `Mobile/Unlit (Supports Lightmap)` and `Mobile/VertexLit` rewritten as a single **`LightMode = ForwardBase`** pass with `#pragma multi_compile _ LIGHTMAP_ON` and `unity_LightmapST`, decoding the dLDR map with the `×2` of the original `VertexLM` pass. A CG pass tagged `"Vertex"` (the literal APK tag) never receives the `LIGHTMAP_ON` keyword in modern Unity — that is why the first attempt still rendered unlit. |

The original Unity 4 `m_Lightmaps` block is **left in the scenes** as ground
truth; the binder is additive and reversible.

## Verification

`tools/verify_lightmaps.py` checks, per scene:

1. the original lightmap list still exists and every texture resolves;
2. the binder references **exactly** that list, in the same order (renderers
   address lightmaps by index, so order is not cosmetic);
3. no renderer asks for an index outside the baked set (sentinels excluded);
4. the shader behind lightmapped materials can actually sample a lightmap.

## Why the official binding alone was not enough

`Tools > Block Strike > Diagnose lighting` reported exactly this on Villa:

```
lightmaps bound: 1
renderers with lightmap: 0
binder: есть
```

The array binds fine, but **Unity overwrites `Renderer.lightmapIndex` outside
play mode** whenever it rebuilds lighting for a scene it considers "not baked"
— and without a LightingData asset every scene of this export is exactly that.
So the renderers keep saying "I have no lightmap" no matter what the binder
does, and the shader renders unlit (or, before the guard was added, black).

A second diagnostic run then showed the other half of the problem:

```
binder: 1 texture(s), 0 renderer(s) recorded
```

Unity read the **asset** references of the binder (the lightmap texture, by
GUID) but dropped every **scene-local component** reference
(`renderers: - {fileID: 1279}`) while importing these Unity 4 scenes. So the
binder had nothing to work with. It now identifies renderers by
**name + world position + mesh name** instead — all plain serialised data, no
file IDs involved. The mesh name is the tie-breaker: two objects can share a
name *and* a position (`BuildingRed-detach` in Villa), but the de-batched
meshes are unique per renderer, so all 3 810 keys across the 57 scenes are
unambiguous (the verifier fails if any pair is not).

The binder therefore delivers the baked map a second way, which Unity's
lighting logic never touches:

* `BSLegacyLightmaps` writes `_BSLightmap` (the texture) and `_BSLightmapST`
  (the renderer's `m_LightmapTilingOffset`) into a **`MaterialPropertyBlock`**
  on each baked renderer;
* `Mobile/Unlit (Supports Lightmap)` and `Mobile/VertexLit` use the standard
  `LIGHTMAP_ON` path when Unity does provide one, and fall back to those two
  uniforms otherwise — same texture, same scale/offset, same `×2` dLDR decode.

Result: identical lighting in the Scene view, in play mode and in a build,
on any renderer and platform, without a LightingData asset.

## If a map still looks flat

`Tools > Block Strike > Diagnose lighting` prints, for the open scene: the
bound lightmap array, the binder contents, how many renderers currently report
a lightmap index, the shaders those renderers use, whether each shader really
has a `LIGHTMAP_ON` variant, the UV1 channel of a sample mesh and the project's
colour space / lightmap encoding. That output pins the failure down in one step.

## 2026-10 fix: the property block never reached the shader

Symptom (reported in the editor and in play mode): every map rendered as raw
unlit albedo — flat, dark, "chocolate" surfaces instead of the baked lighting.
Nothing went magenta and no error was printed, because the black-sample guard
turned "no lightmap delivered" into a white multiplier, i.e. plain unlit.

Cause: `Mobile/Unlit (Supports Lightmap)` and `Mobile/VertexLit` sampled
`_BSLightmap` / `_BSLightmapST` as plain CG uniforms that were **not declared
in the `Properties` block**. A `MaterialPropertyBlock` can only address
properties declared there, so every `SetTexture` / `SetVector` call in
`BSLegacyLightmaps.ApplyRenderers` was a silent no-op: `_BSLightmapST` stayed
`(0,0,0,0)`, the fallback branch never ran, and the `LIGHTMAP_ON` variant —
the only other source of baked light — is never enabled by the editor for a
scene without a LightingData asset. Result: neither lightmap path worked.

Fix (regenerate with `tools/rebuild_shaders.py`, allowed by
`tools/verify_shaders.py` as a documented deviation; everything else still
matches the APK property list):

```
[HideInInspector] _BSLightmap ("BS legacy lightmap (recovery)", 2D) = "black" {}
[HideInInspector] _BSLightmapST ("BS legacy lightmap scale/offset (recovery)", Vector) = (0, 0, 0, 0)
```

`Mobile/VertexLit` also gained an explicit source priority in the fragment
shader — engine lightmap (`LIGHTMAP_ON`, play mode) > property-block lightmap
> `ShadeVertexLights` — matching the original `VertexLM` behaviour where a
baked map replaces lighting entirely.

`_BSDebugMode` deliberately stays a code-only uniform: it is driven through
`Shader.SetGlobalFloat` (`Tools > Block Strike > Lighting: debug view`), and a
global is shadowed by a material property the moment one is declared.

In-editor confirmation steps live in [`editor-check-410.md`](editor-check-410.md).

## Known limits

* `TODO: unverified` — the visual result has not been compared against the APK
  on a device; the binding, indices and shader paths are verified offline.
* Lightmap textures still use the Unity 4 import settings
  (`linearTexture: 0`, i.e. gamma/dLDR). If the project is ever switched to
  linear colour space, they need `sRGB` and the decode re-checked.
* Unity 4 "near" (dual) lightmaps have no modern equivalent; the export only
  ships far maps, so the slot is empty by design.
