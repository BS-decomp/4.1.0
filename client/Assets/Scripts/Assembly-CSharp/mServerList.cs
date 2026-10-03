using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mServerList : MonoBehaviour
{
	private int SelectMode = -1;

	public UILabel ServerInfoLabel;

	public UIPopupList ModePopupList;

	public GameObject ServerListElement;

	public GameObject ServerListSwitch;

	public UILabel ServerListSwitchLabel;

	public GameObject ServerListSwitch2;

	public UILabel ServerListSwitchLabel2;

	public UIInput SearchInput;

	public GameObject ServerListParent;

	private List<GameObject> ServerList = new List<GameObject>();

	private List<GameObject> ServerListPool = new List<GameObject>();

	private bool isCreatingServerList;

	private int MaxPlayers;

	private int MaxServers;

	private RoomInfo[] RoomList;

	private int SelectAllModeList = 1;

	private int MaxAllModeList;

	public void Open()
	{
		ModePopupList.Clear();
		ModePopupList.AddItem("All", -1);
		GameMode[] gameModeList = GameModeManager.GetGameModeList();
		for (int i = 0; i < gameModeList.Length; i++)
		{
			ModePopupList.AddItem(gameModeList[i].ToString(), (int)gameModeList[i]);
		}
		ModePopupList.value = "All";
	}

	public void UpdateServerList()
	{
		MaxAllModeList = 0;
		SelectAllModeList = 1;
		if (!isCreatingServerList)
		{
			ServerListParent.GetComponent<UIScrollView>().ResetPosition();
			StartCoroutine(CreateServerList(true));
		}
	}

	private IEnumerator CreateServerList(bool updateRoomList)
	{
		isCreatingServerList = true;
		yield return new WaitForSeconds(0.01f);
		ClearServerList();
		MaxPlayers = 0;
		MaxServers = 0;
		if (updateRoomList)
		{
			RoomList = PhotonNetwork.GetRoomList();
		}
		RoomInfo[] roomList = GetRooms();
		int count = -1;
		for (int i = 0; i < roomList.Length; i++)
		{
			MaxServers++;
			MaxPlayers += roomList[i].PlayerCount;
		}
		ServerListSwitch2.SetActive(false);
		UpdateServerInfo();
		if (roomList.Length / 30 >= 1)
		{
			if (MaxAllModeList == 0)
			{
				MaxAllModeList = Mathf.CeilToInt((float)roomList.Length / 30f);
			}
			ServerListSwitch.SetActive(true);
			ServerListSwitch.transform.localPosition = Vector3.up * 150f;
			int startIndex = (SelectAllModeList - 1) * 30;
			int maxLength = 30;
			if (SelectAllModeList * 30 > roomList.Length)
			{
				maxLength = roomList.Length - (SelectAllModeList - 1) * 30;
			}
			ServerListSwitchLabel.text = startIndex + 1 + "-" + (startIndex + maxLength);
			ServerListSwitchLabel2.text = startIndex + 1 + "-" + (startIndex + maxLength);
			for (int j = startIndex; j < startIndex + maxLength; j++)
			{
				Transform element = GetElement();
				count++;
				element.GetComponent<mServerInfo>().SetData(roomList[j]);
				element.localPosition = Vector3.up * (102 - 48 * count);
				element.gameObject.SetActive(true);
				ServerList.Add(element.gameObject);
				yield return new WaitForSeconds(0.01f);
			}
			ServerListSwitch2.SetActive(true);
			ServerListSwitch2.transform.localPosition = Vector3.up * (102 - 48 * (count + 1));
		}
		else
		{
			ServerListSwitch.SetActive(false);
			for (int k = 0; k < roomList.Length; k++)
			{
				if (SelectMode == -1 || SelectMode == (int)roomList[k].GetGameMode())
				{
					Transform element2 = GetElement();
					count++;
					element2.GetComponent<mServerInfo>().SetData(roomList[k]);
					element2.localPosition = Vector3.up * (150 - 48 * count);
					element2.gameObject.SetActive(true);
					ServerList.Add(element2.gameObject);
					yield return new WaitForSeconds(0.01f);
				}
			}
		}
		isCreatingServerList = false;
	}

	private RoomInfo[] GetRooms()
	{
		if (string.IsNullOrEmpty(SearchInput.value))
		{
			if (SelectMode == -1)
			{
				return RoomList;
			}
			List<RoomInfo> list = new List<RoomInfo>();
			for (int i = 0; i < RoomList.Length; i++)
			{
				if (SelectMode == (int)RoomList[i].GetGameMode())
				{
					list.Add(RoomList[i]);
				}
			}
			return list.ToArray();
		}
		List<RoomInfo> list2 = new List<RoomInfo>();
		for (int j = 0; j < RoomList.Length; j++)
		{
			if (SelectMode == -1)
			{
				if (RoomList[j].Name.ToLower().Contains(SearchInput.value.ToLower()) || RoomList[j].PlayerCount.ToString() == SearchInput.value || RoomList[j].MaxPlayers.ToString() == SearchInput.value)
				{
					list2.Add(RoomList[j]);
				}
			}
			else if (SelectMode == (int)RoomList[j].GetGameMode() && (RoomList[j].Name.ToLower().Contains(SearchInput.value.ToLower()) || RoomList[j].PlayerCount.ToString() == SearchInput.value || RoomList[j].MaxPlayers.ToString() == SearchInput.value))
			{
				list2.Add(RoomList[j]);
			}
		}
		return list2.ToArray();
	}

	private Transform GetElement()
	{
		GameObject gameObject = null;
		if (ServerListPool.Count != 0)
		{
			gameObject = ServerListPool[0];
			ServerListPool.RemoveAt(0);
		}
		else
		{
			gameObject = NGUITools.AddChild(ServerListParent, ServerListElement);
		}
		return gameObject.transform;
	}

	private void ClearServerList()
	{
		for (int i = 0; i < ServerList.Count; i++)
		{
			ServerList[i].SetActive(false);
			ServerListPool.Add(ServerList[i]);
		}
		ServerList.Clear();
	}

	private void UpdateServerInfo()
	{
		string text = Localization.Get("Players") + ": " + MaxPlayers + "\n" + Localization.Get("Servers") + ": " + MaxServers + "\n" + Localization.Get("Ping") + ": " + PhotonNetwork.GetPing();
		ServerInfoLabel.text = text;
	}

	public void OnSelectMode()
	{
		if (SelectMode != (int)ModePopupList.data)
		{
			SelectMode = (int)ModePopupList.data;
			MaxAllModeList = 0;
			SelectAllModeList = 1;
			if (isCreatingServerList)
			{
				isCreatingServerList = false;
				StopCoroutine(CreateServerList(false));
			}
			ServerListParent.GetComponent<UIScrollView>().ResetPosition();
			StartCoroutine(CreateServerList(true));
		}
	}

	public void RightSwitch()
	{
		SelectAllModeList++;
		if (SelectAllModeList > MaxAllModeList)
		{
			SelectAllModeList = 1;
		}
		if (isCreatingServerList)
		{
			isCreatingServerList = false;
			StopCoroutine(CreateServerList(false));
		}
		ServerListParent.GetComponent<UIScrollView>().ResetPosition();
		StartCoroutine(CreateServerList(false));
	}

	public void LeftSwitch()
	{
		SelectAllModeList--;
		if (SelectAllModeList <= 0)
		{
			SelectAllModeList = MaxAllModeList;
		}
		if (isCreatingServerList)
		{
			isCreatingServerList = false;
			StopCoroutine(CreateServerList(false));
		}
		ServerListParent.GetComponent<UIScrollView>().ResetPosition();
		StartCoroutine(CreateServerList(false));
	}
}
