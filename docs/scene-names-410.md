# Scene names — Block Strike 4.1.0

All 59 scenes shipped under encrypted names (`SXZtRDEyM0ExMt6xab3TLQVn.unity`).
They are now stored decrypted (`Bust.unity`); the mapping is in
[`scene-names-410.json`](scene-names-410.json).

## How the obfuscation works (read from the game, not guessed)

`LevelManager` loads levels by the *encrypted* name:

```csharp
public static void LoadLevel(string name) => Application.LoadLevel(GetEncryptSceneName(name));
private static string GetEncryptSceneName(string name) => Utils.Encrypt(name).Replace("/", "#");
```

`Utils.Encrypt` is DES-CBC + base64:

| Part | Value |
| --- | --- |
| Salt / IV source | `Utils.GetIV()` = UTF8 `"IvmD123A12"`; the blob starts with those 10 bytes, DES uses the first 8 as IV |
| Key | `Rfc2898DeriveBytes(Utils.test, GetIV(), 555).GetBytes(8)` — PBKDF2-HMAC-SHA1, 555 iterations |
| Password `Utils.test` | built in `UIFontControl.GenerateFont()` from the **byte sizes of two files inside the APK itself** |
| `/` in names | stored as `#` (not a legal path character) |

`UIFontControl` reads those two files through `WWW`, with the URLs hidden by
`AesEncryptor` (AES-128-CBC, key `"defaultKeyString"`, IV = first 16 bytes):

```
"jar:file://" + Application.dataPath + "!/classes.dex"
"jar:file://" + Application.dataPath + "!/assets/bin/Data/Managed/Assembly-CSharp.dll"
```

For `com.rexetstudio.blockstrike-780.apk`:

```
classes.dex              8 101 932 bytes
Assembly-CSharp.dll      1 500 688 bytes
Utils.test             = "81019321500688"
DES key                = 002b472627036f02
```

Check: `SXZtRDEyM0ExMt6xab3TLQVn` → `Bust`, and re-encrypting `Bust` reproduces
the original file name byte for byte. `tools/bs_crypto.py` contains a
self-contained DES (no OpenSSL legacy provider needed) and runs that round-trip
as a self-test on every invocation.

## What changed in the project

* `tools/recover_scene_names.py --apply` renamed the 59 scenes and their `.meta`
  files, the per-scene lightmap folders and the de-batched mesh folders, and
  rewrote `ProjectSettings/EditorBuildSettings.asset`, the static-batch
  manifests and `docs/static-batching-report.json`.
  **GUIDs were not touched**, so every reference in the project still resolves.
* `LevelManager` got one flag:

  ```csharp
  public static bool PlainSceneNames = true;
  ```

  With it, `GetSceneName()` returns `Application.loadedLevelName` directly and
  `GetEncryptSceneName()` returns the plain name. This is required: the password
  above depends on files that only exist inside the APK, so in a Unity project
  `Utils.test` is empty and `Utils.Encrypt` would produce names that do not
  exist. Set the flag to `false` to get the original behaviour back verbatim —
  `Utils.Encrypt`/`Utils.Decrypt` themselves are untouched.
* `Resources/others/SceneManager.json`, which maps game modes to scenes, already
  used plain names (`"Bust"`, `"Military Range"`, …), so it now matches the file
  names and `LevelManager.HasSceneInGameMode()` works in the editor.

## Verification

```
$ python3 tools/recover_scene_names.py --verify
scenes: 59
OK
```

The check re-encrypts every plain name with the key derived from the APK and
compares it to the name recorded in `scene-names-410.json`, confirms every
`EditorBuildSettings` path exists, and confirms the scene count.

## Notes

* Three scenes live outside `Maps/`: `AwakeScene`, `Logo`, `Menu`.
* `Shooting Range` (scene) sits in the folder `ShootingRange`; that is how the
  original export named the folder, only the scene name is authoritative.
* `mWeaponSkinsShow` loads a scene called `WeaponSkinsShow`, which does not
  exist in this build. `TODO: unverified` — that is original game code, not a
  recovery gap, but it has not been traced in the APK yet.
