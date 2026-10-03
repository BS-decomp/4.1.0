# Playtest tool — Block Strike 4.1.0

`Tools > Block Strike > Playtest` (Ctrl/Cmd+Shift+P) arms **one** play-mode run:
open any recovered map, press Play, and you are standing in it as `byvlal`,
without a Photon server, an account server or the Menu scene.

Files:

| File | Role |
| --- | --- |
| `client/Assets/BSPlaytest/BSPlaytest.cs` | runtime bootstrap, account stub and spawn fallback (wrapped in `#if UNITY_EDITOR`, never ships in a build) |
| `client/Assets/BSPlaytest/BSPlaytestEditorInput.cs` | keyboard + mouse for the editor, because the game is touch-only |
| `client/Assets/Editor/BlockStrikeRecovery/BSPlaytestWindow.cs` | the window, the preflight and the session guards |
| `client/Assets/Editor/BlockStrikeRecoveryMenu.cs` | `Tools > Block Strike > Audit all maps` — the same checks over all 59 scenes |

## v3 — a sandbox, not a scripted launch

The tool is an **emulator of everything the game expects from the outside
world**, so you can play the real build offline. It is a tool, not a patch: it
lives entirely in `client/Assets/BSPlaytest` + the editor window, it never
modifies game scripts, scenes or prefabs, and everything it fakes exists only
in memory for the duration of play mode.

Press Play (from any scene) and you land in the real **Menu** with:

| Emulated | How |
| --- | --- |
| account | `AccountManager` created/stubbed, `isConnect = true`, your nick |
| region | `SelectRegion` pref set, so the Menu behaves as "connected" |
| offline Photon | `PhotonNetwork.offlineMode` is held ON outside matches, so the Menu's own **Create server** builds a local room with any map and any mode, as many times as you want |
| wallet | gold and silver topped back up to 9 999 999 (configurable), so shop purchases go through and apply for the session |
| player | every time a map loads, the runner makes sure a live player exists (mode scripts kill themselves in offline mode) |

Nothing is written to disk: the account lives in memory, and the `PlayerPrefs`
keys the sandbox touches (`SelectRegion`, `Tutorial`) are snapshotted and
restored when play mode ends, so purchases and balances disappear with it.

Optional: tick "Сразу запустить карту" to skip the menu clicking and drop
straight into one map through the same offline path.

## PC controls

The game is touch-only: `InputJoystick` and `InputTouchLook` read nothing but
`Input.GetTouch`, and NGUI's `UICamera` only processes touches with the Android
settings baked into the scenes. `BSPlaytestEditorInput` feeds the *same* bus the
on-screen controls feed (`InputManager.SetAxis` / `SetButtonDown/Up`) and flips
`UICamera` to the mouse while play mode runs.

| Key | Action | Key | Action |
| --- | --- | --- | --- |
| **Left Alt** | capture / release the mouse | Space | Jump |
| WASD / arrows | move | LMB / RMB | Fire / Aim |
| mouse | look | R / E / Q | Reload / Use / next weapon |
| **1 / 2 / 3** | rifle / pistol / knife | **5** | bomb (4.1.0 plants it with "Use") |
| Tab / T / P / V | stats / chat / pause / mic | C / Shift | crouch / run |

The badge in the bottom-left corner (above the health label) shows a cursor
icon and `Left ALT`: **solid white = captured**, dimmed = released.

Input is forwarded **only** while the mouse is captured. It is released
automatically — and nothing at all reaches the game — while:

* you are typing in a chat field (`UIInput.selection != null`),
* the game is paused (`Time.timeScale == 0`),
* the Game view is not focused.

Capture returns by itself afterwards, and it is re-applied after every scene
load (Unity drops the cursor lock there, which is why the mouse used to die
when the level changed).

### Why mode logic still does not run

Every mode component (TDMMode, Deathmatch, ZombieMode, …) starts with
`if (PhotonNetwork.offlineMode) { Destroy(this); }`. Offline rooms deliberately
have no mode logic — that is the game's design, not a porting gap. Scoring,
rounds, bots and networked events are therefore not emulated; everything that
belongs to the local player is.

## Preflight

The window refuses to arm while any of these fail (full detail goes to the
console, a summary into a dialog):

* scripts compile (`EditorUtility.scriptCompilationFailed`);
* the scene file exists;
* `Resources/player/ControllerManager` and `Resources/PhotonServerSettings` load;
* the scene contains a `GameManager`;
* `Menu` exists and is enabled in Build Settings (when booting through it);
* no renderer in the scene still points at a `Combined Mesh (root: scene)`
  (regression guard for the static-batch repair).

Warnings (do not block): scene missing from Build Settings, mode not found in
`SceneManager.json`, missing MonoBehaviour scripts, materials using placeholder
shaders, unresolved lightmap textures.

## One session, one Play — and the crash guards

* Arming writes `Temp/BSPlaytestSession.json` (never inside `Assets/`, so no
  reimport, and Unity wipes `Temp/` itself).
* The bootstrap **deletes the file before doing anything else**: one arming can
  only ever affect one play mode, even if the setup throws.
* Leaving play mode clears the session and the `EditorPrefs` ticket.
* `[InitializeOnLoad] BSPlaytestStaleSessionGuard` runs on every editor start:
  if a session file is still there and we are *not* entering play mode (Unity
  was closed, crashed, or Windows shut down while armed), it is deleted and a
  warning is logged.
* A session older than 180 minutes is ignored even if the file survives.
* `Tools > Block Strike > Clear playtest state` wipes everything by hand.

Nothing inside `Assets/` is modified by arming or by running: no injected
GameObjects are saved, no scene is touched, no PlayerPrefs are written by the
tool.

## Status

`TODO: unverified` — the C# has not been compiled or run here (no Unity in this
environment). It is written against the signatures in the recovered
`Assembly-CSharp` (`GameManager.GetController/GetTeamSpawn/GetRandomSpawn/
GetPlayerIDSpawn/UpdateRoundState`, `ControllerManager.ActivePlayer`,
`PlayerInput.SetHealth/SetBunnyHopAutoJump/SurfEnabled`,
`PlayerWeapons.UpdateWeaponAll`, `UIPanelManager.ShowPanel`,
`CameraManager.DeactiveAll`, `PhotonCustomValue` keys). Every runtime step is
wrapped in try/catch and logs what failed, so a wrong assumption produces a
readable console message instead of a broken scene.
