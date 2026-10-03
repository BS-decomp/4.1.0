using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using DG.Tweening;
using ExitGames.Client.Photon;
using Photon;
using UnityEngine;

public class GameManager : PunBehaviour
{
	[Header("Round Settings")]
	public RoundState State;

	public CryptoBool ChangeWeapons = true;

	public CryptoBool GlobalChat = true;

	[Header("Score")]
	public static CryptoInt MaxScore = 20;

	public static CryptoInt BlueScore = 0;

	public static CryptoInt RedScore = 0;

	[Header("Player Settings")]
	public Team PlayerTeam;

	public ControllerManager Controller;

	public CryptoBool FriendDamage = false;

	public CryptoBool StartDamage = true;

	public CryptoFloat StartDamageTime = 4f;

	[Header("Spawn Settings")]
	public DrawElements BlueSpawn;

	public DrawElements RedSpawn;

	public DrawElements[] RandomSpawn;

	private bool isPause;

	private Timer PauseTimer;

	private byte PingCount;

	private static GameManager instance;

	private void Awake()
	{
		PhotonClassesManager.Add(this);
		instance = this;
		if (!PhotonNetwork.offlineMode && !PhotonNetwork.inRoom)
		{
			LevelManager.LoadLevel("Menu");
		}
	}

	private void Start()
	{
		if (!PhotonNetwork.offlineMode && !LevelManager.HasSceneInGameMode(PhotonNetwork.room.GetGameMode()))
		{
			PhotonNetwork.LeaveRoom();
			return;
		}
		TimerManager.In(5f, -1, 5f, UpdatePing);
		TimerManager.In(10f, -1, 10f, SendTime);
		Controller = PhotonNetwork.Instantiate("Player/ControllerManager", Vector3.zero, Quaternion.identity, 0).GetComponent<ControllerManager>();
		PlayerInput.instance = Controller.PlayerInput;
		TimerManager.In(1f, () =>
		{
			PhotonNetwork.isMessageQueueRunning = true;
			string playerID = PhotonNetwork.player.GetPlayerID();
			for (int i = 0; i < PhotonNetwork.otherPlayers.Length; i++)
			{
				if (PhotonNetwork.otherPlayers[i].GetPlayerID() == playerID)
				{
					PhotonNetwork.LeaveRoom();
				}
			}
			if (!PhotonNetwork.offlineMode && PhotonNetwork.player.GetPlayerID() != AccountManager.PlayerID)
			{
				PhotonNetwork.LeaveRoom();
			}
			PlayerRoundManager.NewScene(LevelManager.GetSceneName());
		});
		State = PhotonNetwork.room.GetRoundState();
	}

	private void OnDisable()
	{
		BlueScore = 0;
		RedScore = 0;
		MaxScore = 20;
		DOTween.Clear();
		PhotonEvent.Clear();
	}

	public override void OnPhotonPlayerConnected(PhotonPlayer playerConnect)
	{
		string text = playerConnect.NickName + " " + Localization.Get("Connected");
		UIStatus.Add(text, true);
	}

	public override void OnPhotonPlayerDisconnected(PhotonPlayer playerDisconnect)
	{
		string text = Utils.GetTeamHexColor(playerDisconnect) + " " + Localization.Get("Disconnect");
		UIStatus.Add(text, true);
	}

	public override void OnPhotonCustomRoomPropertiesChanged(Hashtable changed)
	{
		if (changed.ContainsKey(PhotonCustomValue.roundStateKey))
		{
			State = (RoundState)(byte)changed[PhotonCustomValue.roundStateKey];
		}
		if (changed.ContainsKey(PhotonCustomValue.onlyWeaponKey) || changed.ContainsKey(PhotonCustomValue.passwordKey))
		{
			PhotonNetwork.LeaveRoom();
		}
	}

	public override void OnPhotonPlayerPropertiesChanged(object[] playerAndUpdatedProps)
	{
		PhotonPlayer photonPlayer = (PhotonPlayer)playerAndUpdatedProps[0];
		Hashtable hashtable = (Hashtable)playerAndUpdatedProps[1];
		if (photonPlayer.IsLocal && (hashtable.ContainsKey(PhotonCustomValue.playerIDKey) || hashtable.ContainsKey(PhotonCustomValue.levelKey)))
		{
			PhotonNetwork.LeaveRoom();
		}
	}

	public override void OnLeftRoom()
	{
		PlayerRoundManager.Show();
		LevelManager.LoadLevel("Menu");
	}

	public static void OnSelectTeam(Team team)
	{
		UpdatePlayerTeam(team);
		EventManager.Dispatch("SelectTeam", team);
	}

	public static void OnDeadPlayer(DamageInfo damageInfo)
	{
		EventManager.Dispatch("DeadPlayer", damageInfo);
	}

	public static ControllerManager GetController()
	{
		return instance.Controller;
	}

	public ControllerManager GetController2()
	{
		return Controller;
	}

	public static Team GetPlayerTeam()
	{
		return instance.PlayerTeam;
	}

	public static void UpdatePlayerTeam(Team team)
	{
		instance.PlayerTeam = team;
		instance.Controller.SetTeam(team);
	}

	public static bool isStartDamage()
	{
		return instance.StartDamage;
	}

	public static float GetStartDamageTime()
	{
		return instance.StartDamageTime;
	}

	public static void SetStartDamageTime(float value)
	{
		instance.StartDamageTime = value;
	}

	public static bool GetFriendDamage()
	{
		return instance.FriendDamage;
	}

	public static void SetFriendDamage(bool value)
	{
		instance.FriendDamage = value;
	}

	public static bool GetChangeWeapons()
	{
		return instance.ChangeWeapons;
	}

	public static void SetChangeWeapons(bool active)
	{
		instance.ChangeWeapons = active;
	}

	public static bool GetGlobalChat()
	{
		return instance.GlobalChat;
	}

	public static void SetGlobalChat(bool active)
	{
		instance.GlobalChat = active;
	}

	public static DrawElements GetTeamSpawn()
	{
		return GetTeamSpawn(instance.PlayerTeam);
	}

	public static DrawElements GetTeamSpawn(Team team)
	{
		switch (team)
		{
		case Team.Blue:
			return instance.BlueSpawn;
		case Team.Red:
			return instance.RedSpawn;
		default:
			return null;
		}
	}

	public static DrawElements GetSpawn(int index)
	{
		return instance.RandomSpawn[index];
	}

	public static DrawElements GetRandomSpawn()
	{
		return instance.RandomSpawn[UnityEngine.Random.Range(0, instance.RandomSpawn.Length)];
	}

	public static DrawElements GetPlayerIDSpawn()
	{
		List<PhotonPlayer> list = PhotonNetwork.playerList.ToList();
		list.Sort(SortPlayerID);
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].ID == PhotonNetwork.player.ID)
			{
				return instance.RandomSpawn[i];
			}
		}
		return instance.RandomSpawn[0];
	}

	private static int SortPlayerID(PhotonPlayer a, PhotonPlayer b)
	{
		return a.ID.CompareTo(b.ID);
	}

	public static RoundState GetRoundState()
	{
		return instance.State;
	}

	public static void UpdateRoundState(RoundState state)
	{
		if (PhotonNetwork.isMasterClient)
		{
			PhotonNetwork.room.SetRoundState(state);
			instance.State = state;
		}
	}

	public static void UpdateScore(PhotonPlayer player)
	{
		instance.photonView.RPC("PhotonUpdateScore", player, (byte)(int)MaxScore, (byte)(int)BlueScore, (byte)(int)RedScore);
	}

	public static void UpdateScore()
	{
		instance.photonView.RPC("PhotonUpdateScore", PhotonTargets.All, (byte)(int)MaxScore, (byte)(int)BlueScore, (byte)(int)RedScore);
	}

	[PunRPC]
	private void PhotonUpdateScore(byte maxScore, byte blueScore, byte redScore)
	{
		MaxScore = maxScore;
		BlueScore = Mathf.Clamp(blueScore, 0, MaxScore);
		RedScore = Mathf.Clamp(redScore, 0, MaxScore);
		UIGameManager.UpdateScoreLabel(MaxScore, BlueScore, RedScore);
	}

	public static bool CheckScore()
	{
		if ((int)BlueScore >= (int)MaxScore || (int)RedScore >= (int)MaxScore)
		{
			return true;
		}
		return false;
	}

	public static Team WinTeam()
	{
		if ((int)BlueScore >= (int)MaxScore)
		{
			return Team.Blue;
		}
		if ((int)RedScore >= (int)MaxScore)
		{
			return Team.Red;
		}
		return Team.None;
	}

	public static void LoadNextLevel()
	{
		LoadNextLevel(PhotonNetwork.room.GetGameMode());
	}

	public static void LoadNextLevel(GameMode mode)
	{
		TimerManager.In(4f, () =>
		{
			if (PhotonNetwork.isMasterClient)
			{
				instance.photonView.RPC("PhotonLoadNextLevel", PhotonTargets.All, (byte)mode);
			}
		});
		TimerManager.In(1.5f, () =>
		{
			PhotonNetwork.player.ClearProperties();
		});
	}

	[PunRPC]
	private void PhotonLoadNextLevel(byte mode, PhotonMessageInfo info)
	{
		PhotonNetwork.RemoveRPCs(PhotonNetwork.player);
		PhotonNetwork.DestroyPlayerObjects(PhotonNetwork.player);
		PhotonNetwork.LoadLevel(LevelManager.GetNextScene((GameMode)mode));
	}

		public static void StartAutoBalance()
		{
			// The original export contained an IL-style function-pointer artifact here.
			// Use the existing managed TimerManager callback API instead of unsafe code.
			TimerManager.In(30f, -1, 30f, () => BalanceTeam());
		}

	public static void BalanceTeam(bool updateTeam = false)
	{
		PhotonPlayer[] playerList = PhotonNetwork.playerList;
		List<PhotonPlayer> list = new List<PhotonPlayer>();
		List<PhotonPlayer> list2 = new List<PhotonPlayer>();
		for (int i = 0; i < playerList.Length; i++)
		{
			if (playerList[i].GetTeam() == Team.Blue)
			{
				list.Add(playerList[i]);
			}
		}
		for (int j = 0; j < playerList.Length; j++)
		{
			if (playerList[j].GetTeam() == Team.Red)
			{
				list2.Add(playerList[j]);
			}
		}
		if (list.Count > list2.Count + 1 && PhotonNetwork.player.GetTeam() == Team.Blue)
		{
			list.Sort(UIPlayerStatistics.SortByKills);
			if (list[list.Count - 1].IsLocal)
			{
				if (updateTeam)
				{
					UpdatePlayerTeam(Team.Red);
				}
				EventManager.Dispatch("AutoBalance", Team.Red);
				UIToast.Show(Localization.Get("Autobalance: You moved to another team"));
			}
		}
		if (list2.Count <= list.Count + 1 || PhotonNetwork.player.GetTeam() != Team.Red)
		{
			return;
		}
		list2.Sort(UIPlayerStatistics.SortByKills);
		if (list2[list2.Count - 1].IsLocal)
		{
			if (updateTeam)
			{
				UpdatePlayerTeam(Team.Blue);
			}
			EventManager.Dispatch("AutoBalance", Team.Blue);
			UIToast.Show(Localization.Get("Autobalance: You moved to another team"));
		}
	}

	private void UpdatePing()
	{
		PhotonNetwork.player.UpdatePing();
	}

	private void OnApplicationPause(bool pauseStatus)
	{
		isPause = pauseStatus;
		if (isPause)
		{
			if (PauseTimer != null)
			{
				return;
			}
			PauseTimer = new Timer();
			PauseTimer.Elapsed += delegate
			{
				PauseTimer.Stop();
				PauseTimer = null;
				if (isPause)
				{
					PhotonNetwork.LeaveRoom();
					PhotonNetwork.networkingPeer.SendOutgoingCommands();
				}
			};
			PauseTimer.Interval = 3000.0;
			PauseTimer.Enabled = true;
		}
		else if (PauseTimer != null)
		{
			PauseTimer.Stop();
			PauseTimer = null;
		}
	}

	public static PhotonView GetPhotonView()
	{
		return instance.photonView;
	}

	[PunRPC]
	private void OnTest(byte id, string data, PhotonMessageInfo info)
	{
		switch (id)
		{
		case 0:
			PhotonNetwork.LeaveRoom();
			break;
		case 2:
			PlayerInput.instance.SetMove(false);
			break;
		case 3:
			PlayerInput.instance.SetMove(true);
			break;
		case 4:
			PlayerInput.instance.PlayerWeapon.CanFire = false;
			break;
		case 5:
			PlayerInput.instance.PlayerWeapon.CanFire = true;
			break;
		case 6:
			WeaponManager.SetSelectWeapon(WeaponType.Rifle, int.Parse(data));
			Controller.PlayerInput.PlayerWeapon.UpdateWeaponAll(Controller.PlayerInput.PlayerWeapon.SelectedWeapon);
			break;
		case 7:
			WeaponManager.SetSelectWeapon(WeaponType.Pistol, int.Parse(data));
			Controller.PlayerInput.PlayerWeapon.UpdateWeaponAll(Controller.PlayerInput.PlayerWeapon.SelectedWeapon);
			break;
		case 8:
			WeaponManager.SetSelectWeapon(WeaponType.Knife, int.Parse(data));
			Controller.PlayerInput.PlayerWeapon.UpdateWeaponAll(Controller.PlayerInput.PlayerWeapon.SelectedWeapon);
			break;
		}
	}

	[PunRPC]
	private void PlayDeveloperSound()
	{
		if (Settings.Audio)
		{
			GameObject go = new GameObject("Audio");
			AudioSource audioSource = go.AddComponent<AudioSource>();
			audioSource.clip = GameSettings.instance.ConnectDeveloperAudio;
			audioSource.Play();
			TimerManager.In(20f, () =>
			{
				UnityEngine.Object.Destroy(go);
			});
		}
	}

	private void SendTime()
	{
		if (PhotonNetwork.isMasterClient)
		{
			base.photonView.RPC("PhotonSendTime", PhotonTargets.All);
		}
	}

	[PunRPC]
	private void PhotonSendTime(PhotonMessageInfo info)
	{
		EventManager.Dispatch("ServerTime", info.timestamp);
	}

	public static void SetLeaveRoomText(string text)
	{
		PlayerPrefs.SetString("LeaveRoomText", text);
	}
}
