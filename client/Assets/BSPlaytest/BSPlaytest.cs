// BS-decomp / Block Strike 4.1.0 — playtest bootstrap (editor-only).
//
// Goal: open ANY recovered map, press Play, and be a FULL player — the same
// account defaults, weapons, skin, HUD, joystick and physics the real game
// gives you — without a Photon server or the account backend.
//
// Why v1 produced a crippled player
// ---------------------------------
// v1 faked a minimal account inside the map scene. But a map alone initialises
// almost nothing: the real game boots `AwakeScene -> Logo -> Menu`, and it is
// `Logo`/`Menu` that call `Settings.Load()`, create the `AccountManager`
// (DontDestroyOnLoad, with its serialized Data/DefaultData), warm up
// Localization (otherwise NGUI labels stay "new label"), the weapon/skin store
// managers and the Photon plumbing (`mPhotonSettings`). Dropping straight into
// a map skips all of that, so you get no joystick, no weapon and no mode.
//
// What v2 does instead
// --------------------
// It reproduces the path a player takes: boot the `Menu` scene, stub the
// account that would normally come from the backend, and then call the game's
// OWN offline-server entry point — `mPhotonSettings.OnCreateServerOffline(map)`
// — which creates the offline room and loads the map through `LevelManager`,
// exactly like the in-game training/offline flow.
//
// Known limitation (by the game's design, not ours): every game-mode component
// destroys itself in `Awake()` when `PhotonNetwork.offlineMode` is true, so
// scoring/round logic is not driven. If the map leaves the player unspawned,
// the runner performs the mode's own local spawn sequence as a fallback.

#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[Serializable]
public class BSPlaytestSession
{
    public bool armed;
    public string ticket = string.Empty;
    public string nick = "byvlal";
    public string scene = string.Empty;
    public int gameMode = -1;           // -1 = resolve from Resources/others/SceneManager.json
    public bool spawnPlayer = true;
    public bool bootThroughMenu = true; // false = old behaviour (straight into the map)
    public bool editorInput = true;     // keyboard + mouse instead of the touch-only controls
    public bool sandbox = true;         // stay in the Menu and let the player create servers
    public bool autoStartMap;           // sandbox + jump straight into `scene`
    public int gold = 9999999;
    public int money = 9999999;
    public string bootScene = "Menu";
    public string createdUtc = string.Empty;
    public int maxAgeMinutes = 180;

    public static string FilePath
    {
        get { return Path.Combine(Path.Combine(Application.dataPath, ".."), "Temp/BSPlaytestSession.json"); }
    }
}

public static class BSPlaytest
{
    public const string Version = "2.0";
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

        // One arming = one play mode, even if everything below throws.
        try { File.Delete(path); } catch { }

        BSPlaytestSession session;
        try { session = JsonUtility.FromJson<BSPlaytestSession>(json); }
        catch (Exception e)
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

        // Things Logo.Start() does and every scene relies on.
        try { Settings.Load(); } catch (Exception e) { Debug.LogWarning("[BS Playtest] Settings.Load failed: " + e.Message); }

        GameObject host = new GameObject("[BS Playtest]");
        UnityEngine.Object.DontDestroyOnLoad(host);
        host.hideFlags = HideFlags.DontSave;
        if (session.sandbox)
        {
            BSPlaytestSandbox sandbox = host.AddComponent<BSPlaytestSandbox>();
            sandbox.nick = session.nick;
            sandbox.gold = session.gold;
            sandbox.money = session.money;
        }
        host.AddComponent<BSPlaytestRunner>().session = session;
        if (session.editorInput)
        {
            host.AddComponent<BSPlaytestEditorInput>();
        }

        Debug.Log(string.Format("[BS Playtest {0}] armed for \"{1}\" as \"{2}\" ({3}).",
            Version, session.scene, session.nick,
            session.bootThroughMenu ? "booting through " + session.bootScene : "direct map boot"));
    }

    /// <summary>Everything the account backend would normally provide.</summary>
    public static void StubAccount(string nick)
    {
        if (UnityEngine.Object.FindObjectOfType<AccountManager>() == null)
        {
            AccountManager.Init();
        }
        AccountManager.AccountID = "PLAYTEST";
        AccountManager.AccountToken = "PLAYTEST";
        AccountManager.isConnect = true;

        AccountManager manager = UnityEngine.Object.FindObjectOfType<AccountManager>();
        if (manager == null)
        {
            Debug.LogError("[BS Playtest] AccountManager could not be created.");
            return;
        }
        if (manager.Data == null)
        {
            manager.Data = new AccountData();
        }
        if (manager.DefaultData == null)
        {
            manager.DefaultData = new AccountData();
        }

        // AccountData's own defaults are the game's defaults: Money 100, Gold 10,
        // Level 1, SelectedRifle 12, SelectedPistol 3, SelectedKnife 4,
        // SelectedPlayerSkin 0. Only the name has to be filled in.
        manager.Data.AccountName = nick;
        manager.DefaultData.AccountName = nick;
        if ((int)manager.Data.Level < 1) { manager.Data.Level = 1; }
        if ((int)manager.Data.Money < 0) { manager.Data.Money = 100; }

        Debug.Log(string.Format(
            "[BS Playtest] account stub: \"{0}\", level {1}, rifle {2}, pistol {3}, knife {4}, skin {5}.",
            (string)manager.Data.AccountName, (int)manager.Data.Level,
            (int)manager.Data.SelectedRifle, (int)manager.Data.SelectedPistol,
            (int)manager.Data.SelectedKnife, (int)manager.Data.SelectedPlayerSkin));
    }
}

/// <summary>Room/player property keys, mirrored from the game's internal PhotonCustomValue.</summary>
internal static class PhotonCustomValueAccess
{
    public const string SceneNameKey = "s";
    public const string PasswordKey = "p";
    public const string GameModeKey = "g";
    public const string RoundStateKey = "r";
}

public class BSPlaytestRunner : MonoBehaviour
{
    public BSPlaytestSession session;

    private void Start()
    {
        if (session == null)
        {
            return;
        }
        if (session.sandbox)
        {
            StartCoroutine(BootSandbox());
            StartCoroutine(WatchMaps());
            return;
        }
        StartCoroutine(session.bootThroughMenu ? BootThroughMenu() : BootDirect());
    }

    // ------------------------------------------------------------------ //
    // sandbox: land in the Menu and let the player drive the real UI
    // ------------------------------------------------------------------ //

    private IEnumerator BootSandbox()
    {
        string boot = string.IsNullOrEmpty(session.bootScene) ? "Menu" : session.bootScene;
        if (Application.loadedLevelName != boot)
        {
            Debug.Log("[BS Playtest] sandbox: loading \"" + boot + "\".");
            Application.LoadLevel(boot);
            float deadline = Time.realtimeSinceStartup + 30f;
            while (Application.loadedLevelName != boot && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            if (Application.loadedLevelName != boot)
            {
                Debug.LogError("[BS Playtest] \"" + boot + "\" did not load in 30 s. Is it in Build Settings?");
                yield break;
            }
        }

        yield return new WaitForSeconds(0.5f);
        BSPlaytest.StubAccount(session.nick);

        if (session.autoStartMap && !string.IsNullOrEmpty(session.scene))
        {
            yield return StartMapThroughMenu(session.scene);
            yield break;
        }

        Debug.Log("[BS Playtest] sandbox ready. Create a server from the Menu with any map and mode — " +
                  "it will be offline, the wallet is topped up, and the player spawns automatically.");
    }

    private IEnumerator StartMapThroughMenu(string map)
    {
        mPhotonSettings photon = UnityEngine.Object.FindObjectOfType<mPhotonSettings>();
        if (photon == null)
        {
            Debug.LogError("[BS Playtest] no mPhotonSettings in the Menu — cannot auto-start \"" + map + "\".");
            yield break;
        }
        ApplyLocalPlayerProperties();
        try
        {
            photon.OnCreateServerOffline(map);
        }
        catch (Exception e)
        {
            Debug.LogError("[BS Playtest] OnCreateServerOffline threw: " + e);
            yield break;
        }
        ApplyRoomProperties();
    }

    /// <summary>Every time a map comes up — no matter whether the player started
    /// it from the Menu or the tool did — make sure there is a live player.</summary>
    private IEnumerator WatchMaps()
    {
        string previous = Application.loadedLevelName;
        string boot = string.IsNullOrEmpty(session.bootScene) ? "Menu" : session.bootScene;
        while (true)
        {
            yield return null;
            string current = Application.loadedLevelName;
            if (current == previous)
            {
                continue;
            }
            previous = current;
            if (current == boot || current == "Logo" || current == "AwakeScene")
            {
                continue;
            }
            session.scene = current;
            session.gameMode = BSPlaytestRuntimeModes.Resolve(current);
            Debug.Log("[BS Playtest] map \"" + current + "\" loaded (mode " +
                      (session.gameMode >= 0 ? ((GameMode)session.gameMode).ToString() : "unknown") + ").");
            yield return EnsurePlayer();
        }
    }

    // ------------------------------------------------------------------ //
    // the game's own path: Menu -> offline room -> map
    // ------------------------------------------------------------------ //

    private IEnumerator BootThroughMenu()
    {
        string boot = string.IsNullOrEmpty(session.bootScene) ? "Menu" : session.bootScene;

        if (Application.loadedLevelName != boot)
        {
            Debug.Log("[BS Playtest] loading \"" + boot + "\" first so the game initialises itself.");
            Application.LoadLevel(boot);
            float deadline = Time.realtimeSinceStartup + 30f;
            while (Application.loadedLevelName != boot && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            if (Application.loadedLevelName != boot)
            {
                Debug.LogError("[BS Playtest] \"" + boot + "\" did not load in 30 s. Is it in Build Settings?");
                yield break;
            }
            yield return null;
        }

        // Let the Menu scene run its Awake/Start chain.
        yield return new WaitForSeconds(0.5f);

        mPhotonSettings photon = null;
        float wait = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < wait)
        {
            photon = UnityEngine.Object.FindObjectOfType<mPhotonSettings>();
            if (photon != null)
            {
                break;
            }
            yield return null;
        }

        BSPlaytest.StubAccount(session.nick);
        ApplyLocalPlayerProperties();

        if (photon == null)
        {
            Debug.LogError(
                "[BS Playtest] No mPhotonSettings in \"" + boot + "\" — cannot use the game's own offline flow.\n" +
                "  Falling back to loading the map directly; the player may be incomplete.");
            yield return BootDirect();
            yield break;
        }

        Debug.Log("[BS Playtest] creating the offline room through mPhotonSettings.OnCreateServerOffline(\"" +
                  session.scene + "\") — the same call the in-game offline/training flow uses.");
        bool failed = false;
        try
        {
            photon.OnCreateServerOffline(session.scene);
        }
        catch (Exception e)
        {
            failed = true;
            Debug.LogError("[BS Playtest] OnCreateServerOffline threw: " + e);
        }
        if (failed)
        {
            yield return BootDirect();
            yield break;
        }

        // The offline room exists immediately. Stamp the room properties now,
        // while we are still in the Menu: GameManager kicks the player out of
        // the room when the password/weapon room property or the local player's
        // id/level changes while a match is running (its anti-cheat), so this
        // must never happen after the map has loaded.
        ApplyRoomProperties();

        float sceneDeadline = Time.realtimeSinceStartup + 60f;
        while (Application.loadedLevelName != session.scene && Time.realtimeSinceStartup < sceneDeadline)
        {
            yield return null;
        }
        if (Application.loadedLevelName != session.scene)
        {
            Debug.LogError("[BS Playtest] the map \"" + session.scene + "\" never loaded. " +
                           "Check that it is enabled in Build Settings.");
            yield break;
        }
        yield return EnsurePlayer();
    }

    // ------------------------------------------------------------------ //
    // old path, kept for maps that must be tested without the Menu
    // ------------------------------------------------------------------ //

    private IEnumerator BootDirect()
    {
        BSPlaytest.StubAccount(session.nick);
        ApplyLocalPlayerProperties();
        try
        {
            PhotonNetwork.automaticallySyncScene = false;
            PhotonNetwork.isMessageQueueRunning = true;
            PhotonNetwork.offlineMode = true;
            if (!PhotonNetwork.inRoom)
            {
                PhotonNetwork.CreateRoom("Playtest");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[BS Playtest] offline room setup failed: " + e);
            yield break;
        }
        ApplyRoomProperties();
        yield return EnsurePlayer();
    }

    private void ApplyRoomProperties()
    {
        try
        {
            if (PhotonNetwork.room == null)
            {
                return;
            }
            Hashtable props = new Hashtable();
            props[PhotonCustomValueAccess.SceneNameKey] = session.scene;
            props[PhotonCustomValueAccess.PasswordKey] = string.Empty;
            props[PhotonCustomValueAccess.GameModeKey] = (byte)Mathf.Max(0, session.gameMode);
            props[PhotonCustomValueAccess.RoundStateKey] = (byte)RoundState.PlayRound;
            PhotonNetwork.room.SetCustomProperties(props);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BS Playtest] room properties incomplete: " + e.Message);
        }
    }

    /// <summary>Nick, id and level, set BEFORE joining a room — exactly where
    /// mPhotonSettings.OnCreateServer sets them. GameManager force-leaves the
    /// room if the local player's id or level changes during a match.</summary>
    private void ApplyLocalPlayerProperties()
    {
        try
        {
            PhotonNetwork.playerName = session.nick;
            PhotonNetwork.player.ClearProperties();
            PhotonNetwork.player.SetPlayerID("PLAYTEST");
            PhotonNetwork.player.SetLevel(1);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BS Playtest] player properties incomplete: " + e.Message);
        }
    }

    // ------------------------------------------------------------------ //
    // make sure we end up as a real, armed, moving player
    // ------------------------------------------------------------------ //

    private IEnumerator EnsurePlayer()
    {
        if (!session.spawnPlayer)
        {
            yield break;
        }

        ControllerManager controller = null;
        float deadline = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < deadline)
        {
            try { controller = GameManager.GetController(); } catch { controller = null; }
            if (controller != null && controller.PlayerInput != null)
            {
                break;
            }
            yield return null;
        }

        if (Application.loadedLevelName != session.scene)
        {
            Debug.LogError(
                "[BS Playtest] the game kicked us back to \"" + Application.loadedLevelName + "\".\n" +
                "  GameManager leaves the room when the room password/weapon property or the local " +
                "player's id/level changes during a match, or when PhotonNetwork is neither in a room " +
                "nor in offline mode. Check the lines above for OnLeftRoom.");
            yield break;
        }

        if (controller == null || controller.PlayerInput == null)
        {
            Debug.LogError(
                "[BS Playtest] No ControllerManager appeared in 20 s.\n" +
                "  * does this scene have a GameManager?\n" +
                "  * does Resources/player/ControllerManager.prefab still exist?\n" +
                "  * look for the first exception thrown by the map above this line.");
            yield break;
        }

        // Give the map's own mode logic a chance first (tutorial maps drive the
        // player themselves even in offline mode).
        yield return new WaitForSeconds(2.0f);
        if (IsPlayerActive(controller))
        {
            Debug.Log("[BS Playtest] the map spawned the player itself — nothing to do.");
            yield break;
        }

        GameMode mode = session.gameMode >= 0 ? (GameMode)session.gameMode : GameMode.TeamDeathmatch;
        Debug.Log("[BS Playtest] the map did not spawn a player (mode scripts self-destruct in offline " +
                  "mode), emulating " + mode + " locally.");
        BSPlaytestModeDriver.Apply(controller, mode);

        yield return new WaitForSeconds(1.0f);
        if (!IsPlayerActive(controller))
        {
            Debug.LogWarning("[BS Playtest] the player object is still inactive — check the console for the " +
                             "first error from the map's own scripts.");
        }
    }

    private static bool IsPlayerActive(ControllerManager controller)
    {
        try
        {
            PlayerInput input = controller.PlayerInput;
            if (input == null)
            {
                return false;
            }
            if (input.FPController != null && input.FPController.gameObject.activeInHierarchy)
            {
                return true;
            }
            return input.gameObject.activeInHierarchy && input.enabled;
        }
        catch
        {
            return false;
        }
    }



    /// <summary>Weapon and movement flags taken from each mode's own Start().</summary>
}

/// <summary>Scene -> game mode at runtime, read from the game's own
/// Resources/others/SceneManager.json (same data the Menu uses).</summary>
public static class BSPlaytestRuntimeModes
{
    private static Dictionary<string, int> map;

    public static int Resolve(string sceneName)
    {
        if (map == null)
        {
            map = new Dictionary<string, int>();
            try
            {
                TextAsset asset = Resources.Load<TextAsset>("others/SceneManager");
                if (asset != null)
                {
                    foreach (Match block in Regex.Matches(asset.text,
                        "\\{\\s*\"GameMode\"\\s*:\\s*\"([A-Za-z]+)\"\\s*,\\s*\"Scenes\"\\s*:\\s*\\[(.*?)\\]\\s*\\}",
                        RegexOptions.Singleline))
                    {
                        int mode;
                        try { mode = (int)(GameMode)Enum.Parse(typeof(GameMode), block.Groups[1].Value); }
                        catch { continue; }
                        foreach (Match scene in Regex.Matches(block.Groups[2].Value, "\"([^\"]+)\""))
                        {
                            string name = scene.Groups[1].Value;
                            if (!map.ContainsKey(name))
                            {
                                map[name] = mode;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BS Playtest] could not read SceneManager.json: " + e.Message);
            }
        }
        int result;
        return map.TryGetValue(sceneName, out result) ? result : -1;
    }
}
#endif
