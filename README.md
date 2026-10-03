# BS-decomp / Block Strike 4.1.0

An unofficial reconstruction of the **Block Strike 4.1.0** Unity project from the original Android release. The goal is to make this version's scenes, scripts, and assets accessible for study, preservation, and further repair — the same approach as the sibling reconstructions in the [BS-decomp](https://github.com/BS-decomp) organization. This is a working Unity project and a set of recovery tools, **not an official game release**.

> **Status: bootstrap.** The repository structure is in place, and the ground-truth APK is being delivered to `original/apk/`. Recovery has not started yet: the exact engine version, scripting backend, and scene list will be established from the APK and recorded in [`docs/export-status.md`](docs/export-status.md).

## Repository layout

| Path | Contents |
| --- | --- |
| `client/` | Recovered Unity project: scenes, scripts, assets, and editor tooling (populated once the export begins). |
| `original/apk/` | Ground-truth Android package (`com.rexetstudio.blockstrike-780.apk`, expected versionName `4.1.0` — to be confirmed from the APK manifest). It is not a build of this project. |
| `tools/` | Scripts and Unity editor tools for reproducing parts of the recovery process. |
| `docs/` | Technical notes: export status, scene names, geometry, shaders, lightmaps, compatibility. |
| `AGENTS.md` | Working rules for agents and contributors (in Russian). |

## Known and unknown facts

Nothing about the original 4.1.0 build is asserted until it is read from the APK. Current state:

| Fact | Value | Status |
| --- | --- | --- |
| Package name | `com.rexetstudio.blockstrike` | Expected — confirm from `AndroidManifest.xml` |
| versionName | `4.1.0` | Expected — confirm from `AndroidManifest.xml` |
| versionCode | `780` (from the APK file name) | Expected — confirm from `AndroidManifest.xml` |
| Unity engine version | `4.7.2f1` | Confirmed from serialized scene/data headers in the APK |
| Scripting backend | Mono | Confirmed by `libmono.so` and Managed DLLs; no IL2CPP payload found |
| Scenes | 58 (`level0`–`level57`) | Confirmed from APK inventory; 60 `sharedassets` files observed |

For context, this version sits between the two already-reconstructed neighbors:

- [BS-decomp/3.7.0](https://github.com/BS-decomp/3.7.0) — original build on Unity `4.7.2f1` (Mono, TEA-encrypted `Assembly-CSharp.dll`), recovered project adapted to Unity `5.6.7f1`, 56 scenes.
- [BS-decomp/6.5.1](https://github.com/BS-decomp/6.5.1) — original build on Unity `2019.2.3f1` (IL2CPP), recovered project adapted to Unity `2021.3.45f2 LTS`, 74 scenes.

## Open the project

`client/` is a placeholder until the export is produced. The target editor version will be announced here and pinned in `client/ProjectSettings/ProjectVersion.txt` once recovery begins.

## Bugs and contributions

This is a reconstruction, so bugs, missing functionality, and differences from the original game are possible. In particular, opening a scene in the editor is not a guarantee that an Android build or online gameplay will work: some platform integrations and services depend on components outside the recovered Unity project.

If you find an issue, please include the scene or feature involved, steps to reproduce, your Unity version, and any relevant logs or screenshots. Contributions and fixes are welcome. The technical background and current limitations are documented in [`docs/`](docs/).

## Rights

Block Strike and its original content belong to their respective rights holders. The presence of a [`LICENSE`](LICENSE) file does not by itself grant permission to redistribute the original game, APK, or third-party assets; please respect their applicable rights when using this repository.
