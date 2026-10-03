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

## What it actually does at Play (v2)

v1 faked a minimal account inside the map scene and that produced a crippled
player: no joystick, no weapon, no physics, NGUI labels stuck on "new label".
The reason is that a map scene initialises almost nothing — the real game boots
`AwakeScene -> Logo -> Menu`, and it is `Logo`/`Menu` that call
`Settings.Load()`, create the `AccountManager` (DontDestroyOnLoad, with its
serialized `Data`/`DefaultData`), warm up Localization, the weapon/skin store
managers and `mPhotonSettings`.

v2 therefore reproduces the path a player takes:

1. `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` arms the session and calls
   `Settings.Load()` (what `Logo.Start()` does);
2. it loads **`Menu`** and waits for the scene to come up, so every manager the
   game needs exists and survives via DontDestroyOnLoad;
3. it stubs the account the backend would normally deliver —
   `AccountManager.Init()`, `isConnect = true`, `AccountID/Token`,
   `AccountName = "byvlal"`. The loadout comes from `AccountData`'s own
   defaults, i.e. the game's: Money 100, Gold 10, Level 1, rifle 12, pistol 3,
   knife 4, player skin 0;
4. it calls the game's **own** offline entry point,
   `mPhotonSettings.OnCreateServerOffline(map)` — the same call the in-game
   offline/training flow uses. That sets `offlineMode`, creates the room and
   loads the map through `LevelManager`;
5. it stamps the standard room properties (`s` scene, `p` password, `g` game
   mode, `r` round state) and the local player properties;
6. after the map loads it waits 2 s; if the map spawned the player itself
   (tutorial-style maps do) it stays out of the way, otherwise it runs the
   matching mode's local spawn sequence (spawn point, weapon, HUD panel,
   Surf/BunnyHop flags).

The old behaviour is still available: untick "Поднимать игру через Menu" in the
window. It is only useful for checking geometry — the player will be incomplete,
and the window says so.

## Input: why nothing moved before

The game has no desktop controls at all. `InputJoystick` and `InputTouchLook`
only read `Input.touchCount` / `Input.GetTouch`, and the Windows editor produces
no touches, so movement and camera look were simply never fed. NGUI's
`UICamera` has the same issue: the scenes are authored for Android with
`useTouch = true`, and that branch ignores the mouse, so part of the on-screen
buttons did nothing either.

`BSPlaytestEditorInput` fixes this **without touching game logic**: it pushes
values into the very same bus the on-screen controls use —

```
InputJoystick  -> InputManager.SetAxis("Horizontal" / "Vertical", v)
InputTouchLook -> InputManager.SetAxis("Mouse X" / "Mouse Y", v)
InputButton    -> InputManager.SetButtonDown/Up(name)
```

and flips `UICamera.useTouch/useMouse` at runtime (play mode only, nothing is
saved) so NGUI buttons answer the mouse.

| Key | Action | Key | Action |
| --- | --- | --- | --- |
| WASD / arrows | move | Space | Jump |
| mouse | look | LMB | Fire |
| RMB | Aim | R | Reload |
| E | Use | Q | SelectWeapon |
| Tab | Statistics | T | Chat |
| P | Pause | V | Microphone |
| C | Crouch | Left Shift | Run |
| L | release/lock the cursor (to click the UI) | | |

The axes are only written when the keyboard state changes, so the on-screen
joystick keeps working next to it.

## The "thrown back to the Menu" bug (fixed)

`GameManager` has an anti-cheat: it calls `PhotonNetwork.LeaveRoom()` when the
room's `password`/`onlyWeapon` property changes, or when the **local player's
id or level** changes while a match is running — and `OnLeftRoom()` loads
`Menu`. v2.0 wrote exactly those properties right after the map had loaded, so
the game kicked the playtest out every single time.

Now the player properties (`nick`, id, level) are written **before** the room is
created — the same place `mPhotonSettings.OnCreateServer` writes them — and the
room properties are written while still in the Menu, never after the map loads.
If something else kicks us, the runner now says so explicitly instead of
reporting a missing spawn point.

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
