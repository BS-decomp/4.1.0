# Export status — Block Strike 4.1.0

## Current state

| Area | Status | Details |
| --- | --- | --- |
| Repository scaffold | ✅ Done | Layout follows the BS-decomp convention. |
| Ground-truth APK | ✅ Received | `original/apk/com.rexetstudio.blockstrike-780.apk`; SHA-256 `4275b1ab06565bf16b8d049b2ad91d2a16c6637a0f41bedacca308a93a608c18`; 56,650,916 bytes. |
| APK fingerprint | ✅ Done | Mono Android build; armeabi-v7a and x86 libraries; 58 scenes (`level0`–`level57`) and 60 `sharedassets` files observed. |
| Scene names | ✅ Decrypted | DES/PBKDF2 key recomputed from the APK; all 59 scenes renamed to their real names. See [`scene-names-410.md`](scene-names-410.md). |
| Playtest tooling | ✅ v2.1 — boots through `Menu`, keyboard/mouse input | `Tools > Block Strike > Playtest` arms a one-shot offline run as `byvlal`, using the game's own `OnCreateServerOffline` path so the account, weapons, skin and HUD are the real ones. See [`playtest-410.md`](playtest-410.md). |
| Script artifacts | ✅ 35 fixed | `RaycastHit.collider.GetComponent<Collider>()` → `.collider`: restored player physics, hit detection and NGUI input. See [`script-artifacts-410.md`](script-artifacts-410.md). |
| Shaders | ✅ Rebuilt from the APK | All 17 placeholders replaced with shaders transcribed from the compiled ShaderLab in the APK; `tools/verify_shaders.py` checks names, properties and render state. See [`shaders-410.md`](shaders-410.md). |
| Lightmaps | ✅ Re-bound | 57 scenes: textures re-bound at load (`BSLegacyLightmaps`), Unity 4 index sentinels 255/254 ported to 65535/65534, lightmap shaders rewritten for `LIGHTMAP_ON`. See [`lightmaps-410.md`](lightmaps-410.md). |
| Static batching repair | ✅ Done | All 59 scenes de-batched from the Unity 4 `Combined Mesh (root: scene)` layout; 4305 renderers repaired and verified against the original combined meshes. See [`static-batching-410.md`](static-batching-410.md). |
| Unity project export | ✅ Initial export complete | AssetRipper 2.0.0 exported 3,321 assets to `client/` using Unity 4.7.2f1; export is present locally and requires validation before committing/migration. |

## Verified APK facts

- Unity serialized scene/data headers contain the exact editor version string **`4.7.2f1`**.
- Backend is **Mono**: `libmono.so` is present for `armeabi-v7a` and `x86`; `Assembly-CSharp.dll` and other Managed DLLs are present.
- No `libil2cpp.so` or `global-metadata.dat` was found.
- Managed payload includes `ProBuilderCore-Unity4.dll` and `ProBuilderMeshOps-Unity4.dll`.
- `Assembly-CSharp.dll` is TEA-wrapped with the same `<J3Tech>` wrapper pattern used by 3.7.0: AssetRipper rejected the unwrapped APK payload as an invalid PE, while `tools/prepare_export.py --decrypt-tea` produced a valid `MZ` PE and allowed export.
- APK contents use split files such as `level1.split0`/`level1.split1`; `tools/prepare_export.py` reassembles them.

## Target editor decision

The original engine is confirmed as **Unity 4.7.2f1**. Unity 2021.3.45f2 is therefore a possible *later migration target*, not the initial export target. The first AssetRipper export must be checked in a compatible legacy workflow before migration to a modern editor.

## Next steps

1. Run AssetRipper against the prepared `/tmp/bs41-export` input or import the APK directly if supported.
2. Capture the export log and recommended Unity version.
3. Validate scenes, scripts, GUID/fileID links, materials, and legacy ProBuilder data.
4. Only then choose the migration editor and pin `client/ProjectSettings/ProjectVersion.txt`.

## Geometry

- Built-in Unity 4 static batching was the cause of the "flying barrels" on `Bust`:
  modern editors ignore `m_SubsetIndices`, so every batched renderer drew submesh 0
  of the shared combined mesh through its own transform.
- Repaired with `tools/debatch_static_meshes.py`, validated with
  `tools/verify_static_batching.py` (vertex-by-vertex, max world drift 1.5e-5).
- Occlusion culling data in the scenes is stale after this change and still needs
  a rebake. `TODO: unverified` — per-scene visual check in Unity 2021.

## Current audit (tools/audit_project_410.py)

```
scenes                       : 59
renderers on combined meshes : 0
missing script GUIDs         : 0
placeholder shader assets    : 0 of 49 shaders
materials on placeholders    : 0 (scene references)
missing lightmap textures    : 0
scenes not in Build Settings : 0
```

Full data: [`audit-410.json`](audit-410.json). Shader ground truth extracted
from the APK lives in `tools/shader-extract/` (71 shaders).

## Notes

All conclusions above are based on the 4.1.0 APK, not on neighboring versions. The 3.7.0 tools are used only where adapted and explicitly marked.
