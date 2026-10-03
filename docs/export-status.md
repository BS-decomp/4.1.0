# Export status — Block Strike 4.1.0

## Current state

| Area | Status | Details |
| --- | --- | --- |
| Repository scaffold | ✅ Done | Layout follows the BS-decomp convention: `client/`, `original/apk/`, `tools/`, `docs/` (see 3.7.0 / 6.5.1). |
| Ground-truth APK | 🕓 Pending | `original/apk/com.rexetstudio.blockstrike-780.apk` is being delivered; not yet analyzed. |
| APK fingerprint | ⏳ Not started | SHA-256 + content inventory to be recorded on arrival. |
| Unity project export | ⏳ Not started | `client/` is empty. |

Nothing about the original build is asserted until it is read from the APK (see `AGENTS.md`, Zero Hallucination Policy).

## First-verification checklist (run when the APK arrives)

1. Record the APK **SHA-256** and file size.
2. Parse `AndroidManifest.xml` (AXML): confirm package name, `versionName` (expected `4.1.0`), `versionCode` (expected `780`), min/target SDK.
3. Read the Unity version string from `assets/bin/Data/globalgamemanagers` — record the exact engine version.
4. Inspect `lib/<abi>/`: `libunity.so` version, presence of `libil2cpp.so` (IL2CPP backend) vs `libmono.so` + `assets/bin/Data/Managed/*.dll` (Mono backend).
5. If Mono: check whether `Assembly-CSharp.dll` is TEA-encrypted (as in 3.7.0 build 608) — inspect the DLL header/entropy and any TEA helper in native libs.
6. If IL2CPP: inventory `global-metadata.dat` and note the metadata version.
7. Enumerate `assets/bin/Data/`: `level0`–`levelN`, `sharedassets*`, `resources.assets`, streaming assets.
8. Write all results back into this file and update the fact table in `README.md`.

## Notes

- This version sits technologically between 3.7.0 (Unity 4.7.2f1, Mono, TEA) and 6.5.1 (Unity 2019.2.3f1, IL2CPP). Neither toolchain may be assumed; both recovery paths (AssetRipper-style Mono export, Cpp2IL/IL2CPP) are candidate approaches until the APK says otherwise.
