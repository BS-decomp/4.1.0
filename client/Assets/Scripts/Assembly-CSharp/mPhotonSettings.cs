using ExitGames.Client.Photon;
using Photon;
using UnityEngine;

public class mPhotonSettings : PunBehaviour
{
	private string SelectMap;

	private static mPhotonSettings instance;

	private bool newRegion;

	private void Start()
	{
		instance = this;
		PhotonClassesManager.Add(this);
		PhotonNetwork.automaticallySyncScene = true;
		PhotonNetwork.offlineMode = false;
	}

	public void OnConnectToPhoton()
	{
		if (!AccountManager.isConnect)
		{
			UIToast.Show(Localization.Get("Connection account"));
			return;
		}
		if (PhotonNetwork.connected && !newRegion)
		{
			mPanelManager.ShowPanel("Server", true);
			return;
		}
		if (PhotonNetwork.connected)
		{
			PhotonNetwork.Disconnect();
		}
		mPopUp.ShowText(Localization.Get("Connecting") + "...");
		string bundleVersion = VersionManager.bundleVersion;
		string appID = AesEncryptor.DecryptString(GameSettings.instance.PhotonID);
		if (PlayerPrefs.GetString("SelectRegion", "null") == "null")
		{
			PhotonNetwork.ConnectToBestCloudServer(bundleVersion, appID);
		}
		else
		{
			CloudRegionCode region = Region.Parse(PlayerPrefs.GetString("SelectRegion"));
			PhotonNetwork.ConnectToRegion(region, bundleVersion, appID);
		}
		newRegion = false;
	}

	public void OnSelectBestRegion(string region)
	{
		if (!(PlayerPrefs.GetString("SelectRegion") == region))
		{
			PlayerPrefs.SetString("SelectRegion", region);
			newRegion = true;
			mVersionManager.UpdateRegion();
			if (PhotonNetwork.connected)
			{
				PhotonNetwork.Disconnect();
			}
		}
	}

	public override void OnConnectedToPhoton()
	{
		mVersionManager.UpdateRegion();
		PhotonNetwork.playerName = AccountManager.AccountName;
		PhotonNetwork.player.SetLevel(AccountManager.GetLevel());
		if (ClansManager.HasClan)
		{
			PhotonNetwork.player.SetClan(ClansManager.Clan.Tag);
		}
		mPopUp.HideAll("Server");
	}

	public override void OnDisconnectedFromPhoton()
	{
		mPopUp.HideAll("Menu");
	}

	public override void OnFailedToConnectToPhoton(DisconnectCause cause)
	{
		UIToast.Show("Failed: " + cause);
	}

	public override void OnConnectionFail(DisconnectCause cause)
	{
		UIToast.Show("Fail: " + cause);
	}

	public static void OnCreateServer()
	{
		if (!AccountManager.isConnect)
		{
			UIToast.Show(Localization.Get("Connection account"));
			return;
		}
		mPopUp.ShowText(Localization.Get("Creating Server") + "...");
		PhotonNetwork.playerName = AccountManager.AccountName;
		PhotonNetwork.player.SetPlayerID(AccountManager.PlayerID);
		PhotonNetwork.player.ClearProperties();
		GameMode gameMode = mCreateServer.GetGameMode();
		string serverName = mCreateServer.GetServerName();
		int maxPlayers = mCreateServer.GetMaxPlayers();
		maxPlayers = Mathf.Clamp(maxPlayers, 4, 12);
		string password = mCreateServer.GetPassword();
		instance.SelectMap = mCreateServer.GetMap();
		Hashtable hashtable = PhotonNetwork.room.CreateRoomHashtable(password, gameMode);
		if (gameMode == GameMode.Only)
		{
			hashtable[PhotonCustomValue.onlyWeaponKey] = (byte)mCreateServer.GetWeapon();
		}
		RoomOptions roomOptions = new RoomOptions();
		roomOptions.MaxPlayers = (byte)maxPlayers;
		roomOptions.IsOpen = true;
		roomOptions.IsVisible = true;
		roomOptions.CustomRoomProperties = hashtable;
		if (gameMode == GameMode.Only)
		{
			roomOptions.CustomRoomPropertiesForLobby = new string[4]
			{
				PhotonCustomValue.sceneNameKey,
				PhotonCustomValue.passwordKey,
				PhotonCustomValue.gameModeKey,
				PhotonCustomValue.onlyWeaponKey
			};
		}
		else
		{
			roomOptions.CustomRoomPropertiesForLobby = new string[3]
			{
				PhotonCustomValue.sceneNameKey,
				PhotonCustomValue.passwordKey,
				PhotonCustomValue.gameModeKey
			};
		}
		PhotonNetwork.CreateRoom(serverName, roomOptions, null);
	}

	public static void OnJoinChat(string chatName)
	{
		mPopUp.ShowText(Localization.Get("Please wait") + "...");
		RoomOptions roomOptions = new RoomOptions();
		roomOptions.MaxPlayers = 0;
		roomOptions.IsOpen = true;
		roomOptions.IsVisible = false;
		PhotonNetwork.JoinOrCreateRoom(chatName, roomOptions, null);
	}

	public void OnCreateServerOffline(string scene)
	{
		if (!AccountManager.isConnect)
		{
			UIToast.Show(Localization.Get("Connection account"));
			return;
		}
		if (PhotonNetwork.connected)
		{
			PhotonNetwork.Disconnect();
		}
		SelectMap = scene;
		PhotonNetwork.offlineMode = true;
		PhotonNetwork.CreateRoom(scene);
	}

	public static void OnJoinServer(RoomInfo room)
	{
		if (!AccountManager.isConnect)
		{
			UIToast.Show(Localization.Get("Connection account"));
			return;
		}
		mPopUp.ShowText(Localization.Get("Connecting") + "...");
		PhotonNetwork.playerName = AccountManager.AccountName;
		PhotonNetwork.player.SetPlayerID(AccountManager.PlayerID);
		PhotonNetwork.player.ClearProperties();
		instance.SelectMap = room.GetSceneName();
		PhotonNetwork.JoinRoom(room.Name);
	}

	public override void OnJoinedRoom()
	{
		PlayerRoundManager.Clear();
		if (PhotonNetwork.offlineMode)
		{
			mPopUp.ShowText(Localization.Get("Loading") + "...");
			LevelManager.LoadLevel(SelectMap);
		}
		else
		{
			mPopUp.ShowText(Localization.Get("Loading") + "...");
			PhotonNetwork.isMessageQueueRunning = false;
			PhotonNetwork.LoadLevel(SelectMap);
		}
	}

	public override void OnPhotonJoinRoomFailed(object[] codeAndMsg)
	{
		short num = (short)codeAndMsg[0];
		string text = (string)codeAndMsg[1];
		mPopUp.HideAll("ServerList", false);
		if (text == "Game full")
		{
			UIToast.Show(Localization.Get("The server is full"));
			return;
		}
		UIToast.Show("Error Code: " + num + "Message: " + text);
	}
}
