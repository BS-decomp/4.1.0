// BS-decomp / Block Strike 4.1.0 — playtest sandbox (editor-only emulation).
//
// This is a TOOL, not a patch: it emulates the backend the game expects so you
// can play the whole thing offline from the real Menu. No game script is
// modified, nothing is written into Assets, and every change it makes lives in
// memory for the duration of play mode only.
//
// What it emulates
//   * account  — `AccountManager` exists, `isConnect = true`, your nick;
//   * region   — a selected Photon region, so the Menu behaves as "connected";
//   * offline  — `PhotonNetwork.offlineMode` is kept ON outside of matches, so
//                "Create server" from the Menu builds a local room with any map
//                and any mode, as many times as you like;
//   * currency — gold and silver are topped back up to the configured amount,
//                so purchases in the shop go through and stay for the session.
//
// What it deliberately does NOT do
//   * it never writes to the account backend (there is none) — purchases are
//     in-memory, so leaving play mode wipes them, by design;
//   * it never edits scenes, prefabs or game scripts;
//   * PlayerPrefs keys it touches are snapshotted and restored on exit.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

public class BSPlaytestSandbox : MonoBehaviour
{
    public string nick = "byvlal";
    public int gold = 9999999;
    public int money = 9999999;
    public bool enforceOffline = true;
    public string region = "eu";

    private readonly Dictionary<string, string> stringBackup = new Dictionary<string, string>();
    private readonly List<string> createdKeys = new List<string>();
    private float nextTick;
    private string lastScene = string.Empty;

    private void Awake()
    {
        BackupPref("SelectRegion");
        BackupPref("Tutorial");
        if (!string.IsNullOrEmpty(region))
        {
            PlayerPrefs.SetString("SelectRegion", region);
        }
        // Logo sends first-time players into the tutorial; the sandbox wants the Menu.
        PlayerPrefs.SetInt("Tutorial", 1);
        Apply(true);
    }

    private void Update()
    {
        string scene = Application.loadedLevelName;
        bool sceneChanged = scene != lastScene;
        if (sceneChanged)
        {
            lastScene = scene;
        }
        if (Time.unscaledTime < nextTick && !sceneChanged)
        {
            return;
        }
        nextTick = Time.unscaledTime + 0.25f;
        Apply(sceneChanged);
    }

    private void Apply(bool verbose)
    {
        EnsureAccount(verbose);
        if (enforceOffline)
        {
            EnsureOffline(verbose);
        }
    }

    // ------------------------------------------------------------------ //

    private void EnsureAccount(bool verbose)
    {
        try
        {
            AccountManager manager = UnityEngine.Object.FindObjectOfType<AccountManager>();
            if (manager == null)
            {
                AccountManager.Init();
                manager = UnityEngine.Object.FindObjectOfType<AccountManager>();
                if (manager == null)
                {
                    return;
                }
                verbose = true;
            }

            AccountManager.AccountID = "PLAYTEST";
            AccountManager.AccountToken = "PLAYTEST";
            AccountManager.isConnect = true;

            if (manager.Data == null) { manager.Data = new AccountData(); }
            if (manager.DefaultData == null) { manager.DefaultData = new AccountData(); }

            if (string.IsNullOrEmpty((string)manager.Data.AccountName))
            {
                manager.Data.AccountName = nick;
                manager.DefaultData.AccountName = nick;
            }
            // Top the wallet back up instead of setting it once: the shop
            // subtracts from it, and we want every purchase to succeed.
            if ((int)manager.Data.Gold < gold) { manager.Data.Gold = gold; }
            if ((int)manager.Data.Money < money) { manager.Data.Money = money; }
            if ((int)manager.DefaultData.Gold < gold) { manager.DefaultData.Gold = gold; }
            if ((int)manager.DefaultData.Money < money) { manager.DefaultData.Money = money; }

            try { PhotonNetwork.playerName = (string)manager.Data.AccountName; } catch { }

            if (verbose)
            {
                Debug.Log(string.Format("[BS Sandbox] account \"{0}\": gold {1}, silver {2}, level {3}.",
                    (string)manager.Data.AccountName, (int)manager.Data.Gold,
                    (int)manager.Data.Money, (int)manager.Data.Level));
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BS Sandbox] account emulation hiccup: " + e.Message);
        }
    }

    private void EnsureOffline(bool verbose)
    {
        try
        {
            if (PhotonNetwork.offlineMode)
            {
                return;
            }
            if (PhotonNetwork.connected || PhotonNetwork.connecting)
            {
                // A real connection attempt is running (it cannot succeed in the
                // editor: the APK only ships the Android socket plugin).
                PhotonNetwork.Disconnect();
            }
            PhotonNetwork.offlineMode = true;
            if (verbose)
            {
                Debug.Log("[BS Sandbox] PhotonNetwork.offlineMode = true — " +
                          "\"Create server\" from the Menu now builds a local room.");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BS Sandbox] could not force offline mode: " + e.Message);
        }
    }

    // ------------------------------------------------------------------ //

    private void BackupPref(string key)
    {
        if (PlayerPrefs.HasKey(key))
        {
            stringBackup[key] = PlayerPrefs.GetString(key, string.Empty);
        }
        else
        {
            createdKeys.Add(key);
        }
    }

    private void OnDisable()
    {
        try
        {
            foreach (KeyValuePair<string, string> pair in stringBackup)
            {
                PlayerPrefs.SetString(pair.Key, pair.Value);
            }
            foreach (string key in createdKeys)
            {
                PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
            Debug.Log("[BS Sandbox] play mode finished: PlayerPrefs restored, " +
                      "emulated account and purchases discarded.");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BS Sandbox] could not restore PlayerPrefs: " + e.Message);
        }
    }
}
#endif
