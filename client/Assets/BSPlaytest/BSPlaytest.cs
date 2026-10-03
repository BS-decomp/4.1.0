// BS-decomp / Block Strike 4.1.0 — playtest bootstrap (editor-only).
//
// Purpose: let a developer open ANY recovered map, press Play, and actually run
// around in it, without a Photon server, an account server or the Menu scene.
//
// How the original game behaves (from the decompiled 4.1.0 scripts, not guessed):
//   * GameManager.Awake() sends you back to "Menu" unless PhotonNetwork is in a
//     room or in offline mode;
//   * GameManager.Start() spawns "player/ControllerManager" through
//     PhotonNetwork.Instantiate and reads the round state from the room;
//   * every game-mode component (TDMMode, Deathmatch, ZombieMode, ...) calls
//     Destroy(this) in Awake() when PhotonNetwork.offlineMode is true — offline
//     rooms are what the tutorial uses, mode logic is network-only by design.
//
// Therefore the playtest runs in offline mode and this file reproduces the
// *local-player* part of the matching mode's Start(): round state, spawn point,
// weapon, HUD panel and mode flags. Scoring, bots and networked round logic are
// NOT emulated — see docs/playtest-410.md.
//
// The session is armed by Tools > Block Strike > Playtest, is consumed by the
// first play mode that starts, and is deleted again when play mode ends. If
// Unity dies before or during play mode, the stale session file is ignored
// (ticket + timestamp) and cleaned up on the next editor start.

#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[Serializable]
public class BSPlaytestSession
{
    public bool armed;
    public string ticket = string.Empty;
    public string nick = "byvlal";
    public string scene = string.Empty;
    public int gameMode = -1;          // -1 = resolve from Resources/others/SceneManager.json
    public bool spawnPlayer = true;
    public string createdUtc = string.Empty;
    public int maxAgeMinutes = 180;

    public static string FilePath
    {
        get { return Path.Combine(Path.Combine(Application.dataPath, ".."), "Temp/BSPlaytestSession.json"); }
    }
}

public static class BSPlaytest
{
    public const string Version = "1.0";
    private static BSPlaytestSession active;

    public static BSPlaytestSession Active { get { return active; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        string path = BSPlaytestSession.FilePath;
        if (!File.Exists(path))
        {
            return;
        }

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception e)
        {
            Debug.LogError("[BS Playtest] Cannot read the armed session: " + e);
            return;
        }

        // Consume the session immediately: one arming = one play mode, even if
        // the setup below throws or the editor is killed mid-session.
        try { File.Delete(path); } catch { }

        BSPlaytestSession session = null;
        try { session = JsonUtility.FromJson<BSPlaytestSession>(json); } catch (Exception e)
        {
            Debug.LogError("[BS Playtest] Broken session file: " + e);
            return;
        }
        if (session == null || !session.armed)
        {
            return;
        }

        DateTime created;
        if (!DateTime.TryParse(session.createdUtc, null,
                System.Globalization.DateTimeStyles.AdjustToUniversal, out created))
        {
            Debug.LogError("[BS Playtest] Session has no valid timestamp — ignored.");
            return;
        }
        double age = (DateTime.UtcNow - created).TotalMinutes;
        if (age > session.maxAgeMinutes)
        {
            Debug.LogWarning(string.Format(
                "[BS Playtest] Session is {0:0} minutes old (limit {1}) — ignored. Arm it again.",
                age, session.maxAgeMinutes));
            return;
        }

        active = session;
        try
        {
            Prepare(session);
        }
        catch (Exception e)
        {
            Debug.LogError("[BS Playtest] Setup failed, the map will behave like a normal Play: " + e);
            return;
        }

        GameObject host = new GameObject("[BS Playtest]");
        UnityEngine.Object.DontDestroyOnLoad(host);
        host.hideFlags = HideFlags.DontSave;
        host.AddComponent<BSPlaytestRunner>().session = session;
    }

    private static void Prepare(BSPlaytestSession session)
    {
        // --- account (the game refuses to do anything without one) ----------
        try
        {
            AccountManager.Init();
            AccountManager.AccountID = "PLAYTEST";
            AccountManager.AccountToken = "PLAYTEST";
            AccountManager.isConnect = true;
            AccountManager.AccountName = session.nick;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BS Playtest] Account stub incomplete: " + e.Message);
        }

        // --- Photon: offline room, standard parameters ----------------------
        PhotonNetwork.automaticallySyncScene = false;
        PhotonNetwork.isMessageQueueRunning = true;
        PhotonNetwork.offlineMode = true;
        PhotonNetwork.playerName = session.nick;
        PhotonNetwork.CreateRoom("Playtest");

        if (PhotonNetwork.room == null)
        {
            throw new Exception("offline room was not created by PUN");
        }

        Hashtable props = new Hashtable();
        props[PhotonCustomValueAccess.SceneNameKey] = session.scene;
        props[PhotonCustomValueAccess.PasswordKey] = string.Empty;
        props[PhotonCustomValueAccess.GameModeKey] = (byte)Mathf.Max(0, session.gameMode);
        props[PhotonCustomValueAccess.RoundStateKey] = (byte)RoundState.PlayRound;
        PhotonNetwork.room.SetCustomProperties(props);

        try
        {
            PhotonNetwork.player.ClearProperties();
            PhotonNetwork.player.SetPlayerID("PLAYTEST");
            PhotonNetwork.player.SetLevel(1);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BS Playtest] Player properties incomplete: " + e.Message);
        }

        Debug.Log(string.Format(
            "[BS Playtest {0}] offline room ready — nick \"{1}\", scene \"{2}\", mode {3}.",
            Version, session.nick, session.scene,
            session.gameMode >= 0 ? ((GameMode)session.gameMode).ToString() : "auto"));
    }
}

/// <summary>Key names of the room/player properties, mirrored from the game's
/// internal PhotonCustomValue so the playtest writes exactly the same keys.</summary>
internal static class PhotonCustomValueAccess
{
    public const string SceneNameKey = "s";
    public const string PasswordKey = "p";
    public const string GameModeKey = "g";
    public const string RoundStateKey = "r";
}

/// <summary>Runs after the map has loaded and reproduces the local-player part
/// of the mode that the map belongs to.</summary>
public class BSPlaytestRunner : MonoBehaviour
{
    public BSPlaytestSession session;

    private void Start()
    {
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        if (session == null || !session.spawnPlayer)
        {
            yield break;
        }

        float deadline = Time.realtimeSinceStartup + 15f;
        ControllerManager controller = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            try { controller = GameManager.GetController(); } catch { controller = null; }
            if (controller != null && controller.PlayerInput != null)
            {
                break;
            }
            yield return null;
        }

        if (controller == null || controller.PlayerInput == null)
        {
            Debug.LogError(
                "[BS Playtest] No ControllerManager appeared in 15 s.\n" +
                "  * is there a GameManager in this scene?\n" +
                "  * does Resources/player/ControllerManager.prefab still exist?\n" +
                "  * check the console above for the first exception thrown by the map.");
            yield break;
        }

        // Let the scene's own Start() methods finish first.
        yield return new WaitForSeconds(0.5f);

        GameMode mode = session.gameMode >= 0 ? (GameMode)session.gameMode : GameMode.TeamDeathmatch;
        try
        {
            GameManager.UpdateRoundState(RoundState.PlayRound);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BS Playtest] UpdateRoundState failed: " + e.Message);
        }

        DrawElements spawn = ResolveSpawn(mode);
        if (spawn == null)
        {
            Debug.LogError(
                "[BS Playtest] This map has no usable spawn point (GameManager.BlueSpawn / RedSpawn / RandomSpawn are empty).\n" +
                "  The player cannot be placed; walk the scene with the editor camera instead.");
            yield break;
        }

        try
        {
            CameraManager.DeactiveAll();
        }
        catch { }

        try
        {
            if (IsTeamMode(mode))
            {
                GameManager.OnSelectTeam(Team.Blue);
            }
            controller.ActivePlayer(spawn.GetSpawnPosition(), spawn.GetSpawnRotation());
            controller.PlayerInput.SetHealth(100);
            ApplyModeFlags(controller, mode);
            UIPanelManager.ShowPanel("Display");
        }
        catch (Exception e)
        {
            Debug.LogError("[BS Playtest] Could not activate the player: " + e);
            yield break;
        }

        Debug.Log(string.Format(
            "[BS Playtest] \"{0}\" is running as {1}. Mode scripts are disabled by the game itself in " +
            "offline mode, so scoring/bots/round logic are not emulated.",
            session.scene, mode));
    }

    private static bool IsTeamMode(GameMode mode)
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

    private static DrawElements ResolveSpawn(GameMode mode)
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
        if (spawn == null)
        {
            try { spawn = GameManager.GetRandomSpawn(); } catch { }
        }
        if (spawn == null)
        {
            try { spawn = GameManager.GetTeamSpawn(); } catch { }
        }
        return spawn;
    }

    /// <summary>Weapon and movement flags copied from each mode's own Start().</summary>
    private static void ApplyModeFlags(ControllerManager controller, GameMode mode)
    {
        PlayerInput input = controller.PlayerInput;
        WeaponType weapon = WeaponType.Rifle;
        switch (mode)
        {
            case GameMode.KnifeMode:
            case GameMode.DeathRun:
            case GameMode.BunnyHop:
            case GameMode.HungerGames:
            case GameMode.Surf:
            case GameMode.Football:
                weapon = WeaponType.Knife;
                break;
            case GameMode.BuildBattle:
                weapon = WeaponType.Pistol;
                break;
        }

        try
        {
            if (input.PlayerWeapon != null)
            {
                input.PlayerWeapon.UpdateWeaponAll(weapon);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BS Playtest] Weapon setup failed (" + weapon + "): " + e.Message);
        }

        try
        {
            if (mode == GameMode.Surf)
            {
                input.SurfEnabled = true;
            }
            if (mode == GameMode.BunnyHop)
            {
                input.SetBunnyHopAutoJump(true);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BS Playtest] Mode flags failed: " + e.Message);
        }
    }
}
#endif
