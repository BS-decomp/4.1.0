// BS-decomp / Block Strike 4.1.0 — game-mode emulation for the playtest tool.
//
// Every mode component in the game starts with
//     if (PhotonNetwork.offlineMode) { Destroy(this); }
// so an offline room has no mode logic at all. This driver reproduces, from
// outside, the part of each mode's own Start()/spawn sequence that a single
// local player can have: round state, team, spawn point, weapon, HUD, damage
// rules, movement tweaks and the events map objects listen to.
//
// It is emulation, not a patch: no game script is modified, everything is
// called through the game's existing public API, and it only runs while the
// playtest session is active.
//
// Faithfulness notes (read from the mode scripts in Assembly-CSharp):
//   Football   -> knife, PlayerWeapons.PushRigidbody = true, bunny-hop tuning
//                 (BunnyHopSpeed 0.25, MotorJumpForce 0.2, MotorAirSpeed 1),
//                 score 20, StartDamageTime -1
//   Zombie     -> score 20, StartDamageTime 1, dispatches "WaitPlayer" so every
//                 ZombieBlock resets to its round-start state (that is why the
//                 obstacles looked pre-opened without emulation)
//   Deathmatch -> random spawn, rifle, StartDamageTime 2, friendly fire on
//   BunnyHop   -> knife, auto jump
//   Surf       -> knife, SurfEnabled
//   DeathRun / HungerGames / TNTRun -> knife
//   BuildBattle-> pistol
//   others     -> team spawn + rifle, like TDM/Classic/Bomb/Juggernaut

#if UNITY_EDITOR
using System;
using UnityEngine;

public static class BSPlaytestModeDriver
{
    public static void Apply(ControllerManager controller, GameMode mode)
    {
        if (controller == null || controller.PlayerInput == null)
        {
            return;
        }

        Step("round state", () => GameManager.UpdateRoundState(RoundState.PlayRound));
        Step("score board", () => UIGameManager.SetActiveScore(true, MaxScore(mode)));
        Step("max score", () => GameManager.MaxScore = MaxScore(mode));
        Step("damage rules", () =>
        {
            GameManager.SetStartDamageTime(StartDamageTime(mode));
            GameManager.SetFriendDamage(mode == GameMode.Deathmatch);
        });

        if (mode == GameMode.ZombieSurvival)
        {
            // ZombieBlock listens for "WaitPlayer"/"StartRound" and resets the
            // barricades; without the mode script nobody ever fires those.
            Step("zombie blocks", () => EventManager.Dispatch("WaitPlayer"));
        }

        Step("camera", CameraManager.DeactiveAll);

        if (IsTeamMode(mode))
        {
            Step("team", () => GameManager.OnSelectTeam(Team.Blue));
        }

        DrawElements spawn = ResolveSpawn(mode);
        if (spawn == null)
        {
            Debug.LogError("[BS Playtest] no spawn point in this map (GameManager.BlueSpawn / RedSpawn / " +
                           "RandomSpawn are empty and the scene has no DrawElements).");
            return;
        }

        Step("spawn", () => controller.ActivePlayer(spawn.GetSpawnPosition(), spawn.GetSpawnRotation()));
        Step("health", () => controller.PlayerInput.SetHealth(100));
        Step("weapon", () => controller.PlayerInput.PlayerWeapon.UpdateWeaponAll(Weapon(mode)));
        Step("HUD", () => UIPanelManager.ShowPanel("Display"));
        ApplyMovement(controller, mode);

        Debug.Log("[BS Playtest] mode " + mode + " emulated locally (" + Weapon(mode) +
                  ", score " + MaxScore(mode) + "). Scoring and bots are not simulated.");
    }

    // ------------------------------------------------------------------ //

    private static void ApplyMovement(ControllerManager controller, GameMode mode)
    {
        PlayerInput input = controller.PlayerInput;
        switch (mode)
        {
            case GameMode.Football:
                Step("football rules", () =>
                {
                    input.PlayerWeapon.PushRigidbody = true;
                    input.BunnyHopEnabled = true;
                    input.BunnyHopSpeed = 0.25f;
                    input.FPController.MotorJumpForce = 0.2f;
                    input.FPController.MotorAirSpeed = 1f;
                });
                break;
            case GameMode.BunnyHop:
                Step("bunny hop", () => input.SetBunnyHopAutoJump(true));
                break;
            case GameMode.Surf:
                Step("surf", () => input.SurfEnabled = true);
                break;
        }
    }

    private static WeaponType Weapon(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.KnifeMode:
            case GameMode.DeathRun:
            case GameMode.BunnyHop:
            case GameMode.HungerGames:
            case GameMode.Surf:
            case GameMode.Football:
            case GameMode.MiniGames:
                return WeaponType.Knife;
            case GameMode.BuildBattle:
                return WeaponType.Pistol;
            default:
                return WeaponType.Rifle;
        }
    }

    private static int MaxScore(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.TeamDeathmatch:
                return 100;
            case GameMode.Deathmatch:
                return 50;
            case GameMode.Football:
            case GameMode.ZombieSurvival:
                return 20;
            default:
                return 20;
        }
    }

    private static float StartDamageTime(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.Football:
                return -1f;
            case GameMode.ZombieSurvival:
                return 1f;
            case GameMode.Deathmatch:
                return 2f;
            default:
                return 4f;
        }
    }

    public static bool IsTeamMode(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.Deathmatch:
            case GameMode.HungerGames:
            case GameMode.MiniGames:
                return false;
            default:
                return true;
        }
    }

    public static DrawElements ResolveSpawn(GameMode mode)
    {
        DrawElements spawn = null;
        try
        {
            switch (mode)
            {
                case GameMode.Deathmatch:
                    spawn = GameManager.GetRandomSpawn();
                    break;
                case GameMode.HungerGames:
                case GameMode.MiniGames:
                    spawn = GameManager.GetPlayerIDSpawn();
                    break;
                default:
                    spawn = GameManager.GetTeamSpawn(Team.Blue);
                    break;
            }
        }
        catch { }
        if (spawn == null) { try { spawn = GameManager.GetRandomSpawn(); } catch { } }
        if (spawn == null) { try { spawn = GameManager.GetTeamSpawn(Team.Red); } catch { } }
        if (spawn == null)
        {
            DrawElements[] all = UnityEngine.Object.FindObjectsOfType<DrawElements>();
            if (all != null && all.Length > 0)
            {
                spawn = all[0];
                Debug.Log("[BS Playtest] GameManager has no spawn assigned, using the scene marker \"" +
                          all[0].name + "\" (" + all.Length + " found).");
            }
        }
        return spawn;
    }

    private static void Step(string what, Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BS Playtest] mode emulation step \"" + what + "\" failed: " + e.Message);
        }
    }
}
#endif
