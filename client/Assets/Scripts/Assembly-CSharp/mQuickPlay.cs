using System;
using System.Collections.Generic;
using UnityEngine;

public class mQuickPlay : MonoBehaviour
{
	public UIPopupList SelectModePopupList;

	public UIPopupList SelectMapPopupList;

	public GameObject SelectedMaxPlayers;

	public UIGrid Grid;

	private int MaxPlayers;

	private RoomInfo SelectRoom;

	public void Open()
	{
		SelectModePopupList.Clear();
		SelectModePopupList.AddItem("Any");
		GameMode[] gameModeList = GameModeManager.GetGameModeList();
		for (int i = 0; i < gameModeList.Length; i++)
		{
			SelectModePopupList.AddItem(gameModeList[i].ToString());
		}
		SelectModePopupList.value = SelectModePopupList.items[0];
		UpdateMaps();
	}

	private void UpdateMaps()
	{
		if (SelectModePopupList.value == "Any")
		{
			SelectMapPopupList.transform.parent.gameObject.SetActive(false);
		}
		else
		{
			SelectMapPopupList.transform.parent.gameObject.SetActive(true);
			List<string> gameModeScenes = LevelManager.GetGameModeScenes((GameMode)(int)Enum.Parse(typeof(GameMode), SelectModePopupList.value));
			SelectMapPopupList.Clear();
			SelectMapPopupList.AddItem(Localization.Get("Any"));
			for (int i = 0; i < gameModeScenes.Count; i++)
			{
				SelectMapPopupList.AddItem(gameModeScenes[i]);
			}
			SelectMapPopupList.value = SelectMapPopupList.items[0];
		}
		Grid.repositionNow = true;
	}

	public void OnSelectGameMode()
	{
		UpdateMaps();
	}

	public void SetMaxPlayer(GameObject go)
	{
		if (go.name == "-")
		{
			MaxPlayers = 0;
		}
		else
		{
			MaxPlayers = int.Parse(go.name);
		}
		TweenPosition.Begin(SelectedMaxPlayers, 0.2f, go.transform.localPosition);
	}

	public void QuickPlay()
	{
		mPopUp.ShowText(Localization.Get("Search Server") + "...");
		TimerManager.In(0.2f + UnityEngine.Random.value, () =>
		{
			SelectServer(SelectModePopupList.value, SelectMapPopupList.value, MaxPlayers);
		});
	}

	private void SelectServer(string mode, string map, int maxPlayers)
	{
		RoomInfo[] roomList = PhotonNetwork.GetRoomList();
		List<RoomInfo> list = new List<RoomInfo>();
		for (int i = 0; i < roomList.Length; i++)
		{
			if (string.IsNullOrEmpty(roomList[i].GetPassword()) && roomList[i].PlayerCount != roomList[i].MaxPlayers && (mode == "Any" || roomList[i].GetGameMode().ToString() == mode) && (map == Localization.Get("Any") || roomList[i].GetSceneName() == map || mode == "Any") && (maxPlayers == 0 || roomList[i].MaxPlayers == maxPlayers))
			{
				list.Add(roomList[i]);
			}
		}
		if (list.Count == 0)
		{
			mPopUp.HideAll("Server");
			mPopUp.ShowPopup(Localization.Get("The server with the selected data was not found. You want to create your own server?"), Localization.Get("Search Server"), Localization.Get("No"), () =>
			{
				mPopUp.HideAll("Server");
			}, Localization.Get("Yes"), () =>
			{
				mCreateServer.OpenPanel();
				mPopUp.HideAll("CreateServer");
			});
			return;
		}
		if (maxPlayers == 0)
		{
			SelectRoom = list[UnityEngine.Random.Range(0, list.Count)];
		}
		else
		{
			list.Sort(SortByPlayerCount);
			int playerCount = list[0].PlayerCount;
			for (int num = 0; num < list.Count; num++)
			{
				if (list[num].PlayerCount != playerCount)
				{
					list.RemoveAt(num);
					num = 0;
				}
			}
			SelectRoom = list[UnityEngine.Random.Range(0, list.Count)];
		}
		mPhotonSettings.OnJoinServer(SelectRoom);
	}

	public static int SortByPlayerCount(RoomInfo a, RoomInfo b)
	{
		return b.PlayerCount.CompareTo(a.PlayerCount);
	}
}
