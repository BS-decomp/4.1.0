using System.Collections.Generic;
using Photon;
using UnityEngine;

public class UIPlayerStatistics : Photon.MonoBehaviour
{
	public class PlayerInfoClass
	{
		public int xp;

		public int deaths;

		public int kills;

		public int headshot;
	}

	[Header("Parents")]
	public Transform BlueTeamParent;

	public Transform RedTeamParent;

	[Header("Panel Labels")]
	public UILabel BlueLabel;

	public UILabel RedLabel;

	public UILabel SpectatorsLabel;

	[Header("Server")]
	public UILabel ServerNameLabel;

	public UILabel ModeLabel;

	public UILabel MapLabel;

	public UILabel PlayersLabel;

	[Header("Player Info")]
	public GameObject PlayerInfo;

	public UILabel PlayerInfoName;

	public UILabel PlayerInfoID;

	public UILabel PlayerInfoLevel;

	public UILabel PlayerInfoXP;

	public UILabel PlayerInfoDeaths;

	public UILabel PlayerInfoKills;

	public UILabel PlayerInfoHeadshot;

	private Dictionary<string, PlayerInfoClass> PlayerInfoList = new Dictionary<string, PlayerInfoClass>();

	[Header("Others")]
	public GameObject Container;

	public GameObject Root;

	public static PhotonPlayer SelectPlayer;

	private bool isShow;

	private List<UIPlayerStatisticsElement> PlayerList = new List<UIPlayerStatisticsElement>();

	private List<UIPlayerStatisticsElement> PlayerListPool = new List<UIPlayerStatisticsElement>();

	private void OnEnable()
	{
		InputManager.GetButtonDownEvent += GetButtonDown;
		PhotonEvent.AddListener(PhotonEventTag.GetPlayerInfo, PhotonGetSelectPlayerInfo);
		PhotonEvent.AddListener(PhotonEventTag.SetPlayerInfo, PhotonSetSelectPlayerInfo);
	}

	private void OnDisable()
	{
		InputManager.GetButtonDownEvent -= GetButtonDown;
	}

	private void GetButtonDown(string name)
	{
		if (name == "Statistics")
		{
			Show();
		}
	}

	private void Show()
	{
		if (PhotonNetwork.offlineMode)
		{
			return;
		}
		UIPanelManager.ShowPanel("Statistics");
		ServerNameLabel.text = PhotonNetwork.room.Name;
		PlayersLabel.text = Localization.Get("Players") + ": " + PhotonNetwork.room.PlayerCount + "/" + PhotonNetwork.room.MaxPlayers;
		MapLabel.text = Localization.Get("Map") + ": " + PhotonNetwork.room.GetSceneName();
		if (PhotonNetwork.room.GetGameMode() == GameMode.Only)
		{
			ModeLabel.text = Localization.Get(PhotonNetwork.room.GetGameMode().ToString()) + " (" + WeaponManager.GetWeaponName(PhotonNetwork.room.GetOnlyWeapon()) + ")";
		}
		else
		{
			ModeLabel.text = Localization.Get("Mode") + ": " + Localization.Get(PhotonNetwork.room.GetGameMode().ToString());
		}
		PhotonPlayer[] playerList = PhotonNetwork.playerList;
		List<PhotonPlayer> list = new List<PhotonPlayer>();
		List<PhotonPlayer> list2 = new List<PhotonPlayer>();
		List<string> list3 = new List<string>();
		for (int i = 0; i < playerList.Length; i++)
		{
			if (playerList[i].GetTeam() == Team.Blue)
			{
				list.Add(playerList[i]);
			}
			else if (playerList[i].GetTeam() == Team.Red)
			{
				list2.Add(playerList[i]);
			}
			else if (playerList[i].GetTeam() == Team.None)
			{
				list3.Add(playerList[i].NickName);
			}
		}
		list.Sort(SortByKills);
		list2.Sort(SortByKills);
		if (PhotonNetwork.room.GetGameMode() == GameMode.ZombieSurvival)
		{
			BlueLabel.text = Localization.Get("Survivors");
		}
		else
		{
			BlueLabel.text = Localization.Get("Blue Team");
		}
		if (PhotonNetwork.room.GetGameMode() == GameMode.ZombieSurvival)
		{
			RedLabel.text = Localization.Get("Zombie");
		}
		else
		{
			RedLabel.text = Localization.Get("Red Team");
		}
		SpectatorsLabel.text = Localization.Get("Spectators") + ": " + string.Join(",", list3.ToArray());
		for (int j = 0; j < list.Count; j++)
		{
			UIPlayerStatisticsElement playerContainer = GetPlayerContainer(list[j].NickName);
			playerContainer.SetData(list[j]);
			playerContainer.Transform.SetParent(BlueTeamParent);
			if (j == 0)
			{
				playerContainer.Transform.localPosition = Vector3.zero;
			}
			else
			{
				playerContainer.Transform.localPosition = Vector3.down * 25f * j;
			}
			PlayerList.Add(playerContainer);
		}
		for (int k = 0; k < list2.Count; k++)
		{
			UIPlayerStatisticsElement playerContainer2 = GetPlayerContainer(list2[k].NickName);
			playerContainer2.SetData(list2[k]);
			playerContainer2.Transform.SetParent(RedTeamParent);
			if (k == 0)
			{
				playerContainer2.Transform.localPosition = Vector3.zero;
			}
			else
			{
				playerContainer2.Transform.localPosition = Vector3.down * 25f * k;
			}
			PlayerList.Add(playerContainer2);
		}
	}

	public void Close()
	{
		UIPanelManager.ShowPanel("Display");
		ClearList();
	}

	private UIPlayerStatisticsElement GetPlayerContainer(string name)
	{
		if (PlayerListPool.Count != 0)
		{
			UIPlayerStatisticsElement uIPlayerStatisticsElement = null;
			for (int i = 0; i < PlayerListPool.Count; i++)
			{
				if (PlayerListPool[i].PlayerNameLabel.text == name)
				{
					uIPlayerStatisticsElement = PlayerListPool[i];
					PlayerListPool.RemoveAt(i);
					return uIPlayerStatisticsElement;
				}
			}
			uIPlayerStatisticsElement = PlayerListPool[0];
			PlayerListPool.RemoveAt(0);
			return uIPlayerStatisticsElement;
		}
		GameObject gameObject = NGUITools.AddChild(Root, Container);
		return gameObject.GetComponent<UIPlayerStatisticsElement>();
	}

	private void ClearList()
	{
		if (PlayerList.Count != 0)
		{
			for (int i = 0; i < PlayerList.Count; i++)
			{
				PlayerListPool.Add(PlayerList[i]);
			}
			PlayerList.Clear();
			for (int j = 0; j < PlayerListPool.Count; j++)
			{
				PlayerListPool[j].Widget.alpha = 0f;
			}
		}
	}

	public static int SortByKills(PhotonPlayer a, PhotonPlayer b)
	{
		if (a.GetKills() == b.GetKills())
		{
			if (a.GetDeaths() == b.GetDeaths())
			{
				if (a.GetLevel() == b.GetLevel())
				{
					return b.NickName.CompareTo(a.NickName);
				}
				return b.GetLevel().CompareTo(a.GetLevel());
			}
			return a.GetDeaths().CompareTo(b.GetDeaths());
		}
		return b.GetKills().CompareTo(a.GetKills());
	}

	public static int GetPlayerStatsPosition(PhotonPlayer player)
	{
		List<PhotonPlayer> list = new List<PhotonPlayer>();
		for (int i = 0; i < PhotonNetwork.playerList.Length; i++)
		{
			if (PhotonNetwork.playerList[i].GetTeam() == player.GetTeam())
			{
				list.Add(PhotonNetwork.playerList[i]);
			}
		}
		list.Sort(SortByKills);
		for (int j = 0; j < list.Count; j++)
		{
			if (list[j] == player)
			{
				return j + 1;
			}
		}
		return 1;
	}

	public void OnSelectPlayer(UIPlayerStatisticsElement player)
	{
		if (player.PlayerInfo != null && !player.PlayerInfo.IsLocal)
		{
			SelectPlayer = player.PlayerInfo;
			PlayerInfo.SetActive(true);
			PlayerInfoName.text = player.PlayerInfo.NickName;
			PlayerInfoID.text = player.PlayerInfo.GetPlayerID();
			PlayerInfoLevel.text = player.PlayerInfo.GetLevel().ToString();
			if (PlayerInfoList.ContainsKey(player.PlayerInfo.NickName))
			{
				PlayerInfoClass playerInfoClass = PlayerInfoList[player.PlayerInfo.NickName];
				PhotonSetSelectPlayerInfo(playerInfoClass.xp, playerInfoClass.deaths, playerInfoClass.kills, playerInfoClass.headshot);
				return;
			}
			PlayerInfoXP.text = "-";
			PlayerInfoDeaths.text = "-";
			PlayerInfoKills.text = "-";
			PlayerInfoHeadshot.text = "-";
			PhotonEvent.RPC(PhotonEventTag.GetPlayerInfo, player.PlayerInfo);
		}
	}

	private void PhotonGetSelectPlayerInfo(PhotonEventData data)
	{
		int xP = AccountManager.GetXP();
		int deaths = AccountManager.GetDeaths();
		int kills = AccountManager.GetKills();
		int headshot = AccountManager.GetHeadshot();
		PhotonEvent.RPC(PhotonEventTag.SetPlayerInfo, PhotonPlayer.Find(data.senderID), xP, deaths, kills, headshot);
	}

	private void PhotonSetSelectPlayerInfo(PhotonEventData data)
	{
		int xp = (int)data.parameters[0];
		int deaths = (int)data.parameters[1];
		int kills = (int)data.parameters[2];
		int headshot = (int)data.parameters[3];
		if (!PlayerInfoList.ContainsKey(PhotonPlayer.Find(data.senderID).NickName))
		{
			PlayerInfoClass playerInfoClass = new PlayerInfoClass();
			playerInfoClass.xp = xp;
			playerInfoClass.deaths = deaths;
			playerInfoClass.kills = kills;
			playerInfoClass.headshot = headshot;
			PlayerInfoList.Add(PhotonPlayer.Find(data.senderID).NickName, playerInfoClass);
		}
		PhotonSetSelectPlayerInfo(xp, deaths, kills, headshot);
	}

	private void PhotonSetSelectPlayerInfo(int xp, int deaths, int kills, int headshot)
	{
		PlayerInfoXP.text = xp.ToString();
		PlayerInfoDeaths.text = deaths.ToString();
		PlayerInfoKills.text = kills.ToString();
		PlayerInfoHeadshot.text = headshot.ToString();
	}
}
