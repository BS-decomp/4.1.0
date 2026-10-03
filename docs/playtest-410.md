# Playtest tool — Block Strike 4.1.0

`Tools > Block Strike > Playtest` (Ctrl/Cmd+Shift+P) arms **one** play-mode run:
open any recovered map, press Play, and you are standing in it as `byvlal`,
without a Photon server, an account server or the Menu scene.

Files:

| File | Role |
| --- | --- |
| `client/Assets/BSPlaytest/BSPlaytest.cs` | runtime bootstrap + mode driver (wrapped in `#if UNITY_EDITOR`, never ships in a build) |
| `client/Assets/Editor/BlockStrikeRecovery/BSPlaytestWindow.cs` | the window, the preflight and the session guards |
| `client/Assets/Editor/BlockStrikeRecoveryMenu.cs` | `Tools > Block Strike > Audit all maps` — the same checks over all 59 scenes |

## What it actually does at Play

`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` runs **before** the map's
`GameManager.Awake()` (which would otherwise bounce you to `Menu`) and:

1. creates a stub account — `AccountManager.Init()`, `isConnect = true`,
   `AccountName = "byvlal"`;
2. `PhotonNetwork.offlineMode = true`, `playerName = "byvlal"`,
   `CreateRoom("Playtest")`, `automaticallySyncScene = false`;
3. writes the standard room properties with the game's own keys —
   `s` scene name, `p` password (empty), `g` game mode, `r` round state
   (`PlayRound`);
4. clears/sets the local player properties (`ClearProperties`, player id, level);
5. after the map has loaded, reproduces the **local-player part** of the mode's
   `Start()`: round state, spawn point (`GetTeamSpawn` / `GetRandomSpawn` /
   `GetPlayerIDSpawn`, exactly as the matching mode script picks it), weapon
   (Rifle / Knife / Pistol per mode), `UIPanelManager.ShowPanel("Display")`,
   plus `SurfEnabled` for Surf and auto-jump for BunnyHop.

The game mode comes from the game's own `Resources/others/SceneManager.json`
(scene → mode); the window lets you override it.

### Why the mode scripts themselves do not run

Every mode component in 4.1.0 starts with

```csharp
private void Awake()
{
    if (PhotonNetwork.offlineMode) { Object.Destroy(this); }
    else if (PhotonNetwork.room.GetGameMode() != GameMode.X) { Object.Destroy(this); }
    ...
}
```

so offline rooms deliberately have no mode logic — that is how the original
tutorial/offline flow works. The playtest therefore emulates the local-player
setup only. **Not emulated:** scoring, rounds, bots, team balance, bomb/zombie
logic, networked events. You get a working map, a working player, weapons,
physics, triggers and colliders — enough to walk the level and test geometry,
spawns, lightmaps and shaders.

## Preflight

The window refuses to arm while any of these fail (full detail goes to the
console, a summary into a dialog):

* scripts compile (`EditorUtility.scriptCompilationFailed`);
* the scene file exists;
* `Resources/player/ControllerManager` and `Resources/PhotonServerSettings` load;
* the scene contains a `GameManager`;
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
