using System;
using System.Collections.Generic;
using UnityEngine;

public class mCreateServer : MonoBehaviour
{
	public GameMode SelectMode;

	public UIPopupList SelectModePopupList;

	public UIPopupList SelectWeaponPopupList;

	public UIPopupList SelectMapPopupList;

	public UIInput ServerName;

	public GameObject SelectedMaxPlayers;

	public UIGrid Grid;

	public int MaxPlayers = 4;

	public UIInput Password;

	private static mCreateServer instance;

	private void Start()
	{
		instance = this;
	}

	public void Open()
	{
		ServerName.value = "Room " + UnityEngine.Random.Range(0, 9999);
		SelectModePopupList.Clear();
		GameMode[] gameModeList = GameModeManager.GetGameModeList();
		for (int i = 0; i < gameModeList.Length; i++)
		{
			SelectModePopupList.AddItem(gameModeList[i].ToString());
		}
		SelectWeaponPopupList.Clear();
		for (int j = 0; j < GameSettings.instance.Weapons.Count; j++)
		{
			if (!GameSettings.instance.Weapons[j].Lock && !GameSettings.instance.Weapons[j].Secret)
			{
				SelectWeaponPopupList.AddItem(GameSettings.instance.Weapons[j].Name);
			}
		}
		SelectModePopupList.value = SelectModePopupList.items[0];
		UpdateMaps();
	}

	public static void OpenPanel()
	{
		instance.Open();
	}

	private void UpdateMaps()
	{
		List<string> gameModeScenes = LevelManager.GetGameModeScenes(SelectMode);
		SelectMapPopupList.Clear();
		for (int i = 0; i < gameModeScenes.Count; i++)
		{
			SelectMapPopupList.AddItem(gameModeScenes[i]);
		}
		SelectMapPopupList.value = SelectMapPopupList.items[0];
	}

	public void OnSelectGameMode()
	{
		SelectMode = (GameMode)(int)Enum.Parse(typeof(GameMode), SelectModePopupList.value);
		if (SelectMode == GameMode.Only)
		{
			SelectWeaponPopupList.transform.parent.gameObject.SetActive(true);
		}
		else
		{
			SelectWeaponPopupList.transform.parent.gameObject.SetActive(false);
		}
		Grid.repositionNow = true;
		Grid.transform.parent.localPosition = Vector3.zero;
		UpdateMaps();
	}

	public void OnCheckServerName()
	{
		if (ServerName.value.Length < 4 || Utils.IsNullOrWhiteSpace(ServerName.value))
		{
			ServerName.value = "Room " + UnityEngine.Random.Range(0, 9999);
		}
		ServerName.value = NGUIText.StripSymbols(ServerName.value);
		RoomInfo[] roomList = PhotonNetwork.GetRoomList();
		for (int i = 0; i < roomList.Length; i++)
		{
			if (roomList[i].Name == ServerName.value)
			{
				ServerName.value = "Room " + UnityEngine.Random.Range(0, 9999);
				UIToast.Show(Localization.Get("Name already taken"));
				break;
			}
		}
	}

	public void SetMaxPlayer(GameObject go)
	{
		MaxPlayers = int.Parse(go.name);
		TweenPosition.Begin(SelectedMaxPlayers, 0.2f, go.transform.localPosition);
	}

	public static GameMode GetGameMode()
	{
		return instance.SelectMode;
	}

	public static string GetMap()
	{
		return instance.SelectMapPopupList.value;
	}

	public static string GetServerName()
	{
		return instance.ServerName.value;
	}

	public static int GetMaxPlayers()
	{
		return instance.MaxPlayers;
	}

	public static string GetPassword()
	{
		return instance.Password.value;
	}

	public static int GetWeapon()
	{
		return WeaponManager.GetWeaponID(instance.SelectWeaponPopupList.value);
	}

	public void CreateServer()
	{
		mPhotonSettings.OnCreateServer();
	}
}
