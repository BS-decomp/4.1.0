# Export status — Block Strike 4.1.0

## Current state

| Area | Status | Details |
| --- | --- | --- |
| Repository scaffold | ✅ Done | Layout follows the BS-decomp convention. |
| Ground-truth APK | ✅ Received | `original/apk/com.rexetstudio.blockstrike-780.apk`; SHA-256 `4275b1ab06565bf16b8d049b2ad91d2a16c6637a0f41bedacca308a93a608c18`; 56,650,916 bytes. |
| APK fingerprint | ✅ Done | Mono Android build; armeabi-v7a and x86 libraries; 58 scenes (`level0`–`level57`) and 60 `sharedassets` files observed. |
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

## Notes

All conclusions above are based on the 4.1.0 APK, not on neighboring versions. The 3.7.0 tools are used only where adapted and explicitly marked.
