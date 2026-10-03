# Unity 2021 port plan — Block Strike 4.1.0

## Baseline facts

The APK is Unity 4.7.2f1, Mono, with a TEA-wrapped `Assembly-CSharp.dll`. AssetRipper 2.0.0 exported 3,321 objects and 59 scenes. The export is an evidence baseline, not a release-ready legacy project.

## Target

`Unity 2021.3.45f2` is the target editor. Unity 4.7.2f1 is not required for the port. Unity 5.6.7f1 remains an emergency conversion fallback only.

## Order of work

1. Run `tools/verify_export_410.py client` and preserve its report.
2. Resolve the 41 `DummyShaderTextExporter` shader placeholders from 4.1.0 APK evidence; do not copy 3.7.0 shader replacements without matching properties and GLES programs.
3. Audit all 59 scenes for static batching, lightmap indices, atlas textures, colliders, and GUID/fileID references. Geometry recovery tools from 3.7.0 are references only; manifests must be generated from 4.1.0.
   - **Static batching: done.** `tools/debatch_static_meshes.py` de-batched 4305 renderers in all 59 scenes and `tools/verify_static_batching.py` checks them against the original combined meshes; manifests live in `tools/static-batch-manifests/`. Details: [`static-batching-410.md`](static-batching-410.md).
   - Still open in this step: lightmap index/offset audit, atlas textures, occlusion culling rebake.
4. Make a working copy for Unity 2021 migration. Do not rewrite the baseline in place before a rollback copy exists.
5. Migrate ProjectSettings, packages, scripts, plugins, shaders, and legacy ProBuilder in separate, reviewable steps.
6. Open/import in Unity 2021 and record every compiler/importer error. Never delete components merely to silence errors.
7. Validate representative maps and then all scenes before Android packaging.

## Acceptance gates

- zero unresolved user GUIDs;
- zero Missing MonoBehaviour components;
- zero dummy shaders;
- all build scenes present;
- representative map geometry and colliders preserved (static batching resolved: 0 renderers left on a combined mesh);
- lightmap textures bind to the original scene indices/offsets;
- scripts compile without deleting recovered behavior;
- Android build tested on a current supported ABI.

The baseline `client/` must remain recoverable until all gates pass.
