using System.IO;
using CodeStage.AntiCheat.ObscuredTypes;
using Photon;
using Prime31;
using UnityEngine;

public class Logo : PunBehaviour
{
	public GameObject RexetPanel;

	public GameObject RedictPanel;

	public bool isLoadMenu = true;

	public static bool isLoad = true;

	private void Start()
	{
		if (CheckGame())
		{
			AndroidNativeFunctions.ShowAlert("Files corrupted, please reinstall the game.", "Block Strike", "OK", string.Empty, string.Empty, OnClick);
			return;
		}
		if (AndroidNativeFunctions.GetAppInfo().packageName != VersionManager.bundleIdentifier || AndroidNativeFunctions.GetSignature() != "669FC0E7" || AndroidNativeFunctions.isInstalledApp(AesEncryptor.DecryptString("u27FJJeKE80w5vLMYG8CU1ny+gumK4UejwQ5cluiaKhLYmE7FaTxCklDgeX2p+GH")) || ObscuredPrefs.HasKey("Test"))
		{
			ObscuredPrefs.SetBool("Test", true);
			return;
		}
		PlayGameServices.authenticate();
		AndroidNativeFunctions.ImmersiveMode();
		Settings.Load();
		isLoadMenu = PlayerPrefs.HasKey("Tutorial");
		Screen.sleepTimeout = -1;
		Utils.SetActiveConsole(Settings.Console);
		OnAnimation();
		AccountManager.Init();
	}

	private bool CheckGame()
	{
		if (Directory.Exists(Path.GetDirectoryName(Application.dataPath) + "/arm"))
		{
			return true;
		}
		if (File.Exists(Path.GetDirectoryName(Application.dataPath) + "/" + Path.GetFileNameWithoutExtension(Application.dataPath) + ".odex"))
		{
			return true;
		}
		return false;
	}

	private void OnClick(DialogInterface dialog)
	{
		if (dialog == DialogInterface.Positive)
		{
			Application.Quit();
		}
	}

	private void OnAnimation()
	{
		TimerManager.In(0.5f, () =>
		{
			TweenAlpha.Begin(RexetPanel, 1f, 1f);
			TimerManager.In(1.8f, () =>
			{
				TweenAlpha.Begin(RexetPanel, 1f, 0f);
				TimerManager.In(1.2f, () =>
				{
					if (isLoad)
					{
						if (isLoadMenu)
						{
							LevelManager.LoadLevel("Menu");
						}
						else
						{
							LoadTutorial();
						}
					}
				});
			});
		});
	}

	private void LoadTutorial()
	{
		PhotonClassesManager.Add(this);
		PhotonNetwork.offlineMode = true;
		PhotonNetwork.CreateRoom("tutorial");
	}

	public override void OnJoinedRoom()
	{
		LevelManager.LoadLevel("MainTutorial");
	}
}
