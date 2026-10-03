using System.Collections.Generic;
using System.Linq;
using Photon;
using UnityEngine;

public class ZombieMode : PunBehaviour
{
	public ZombieBlock[] Blocks;

	private int StartZombieTimerID;

	private static ZombieMode instance;

	private void Awake()
	{
		if (PhotonNetwork.offlineMode)
		{
			Object.Destroy(this);
			return;
		}
		if (PhotonNetwork.room.GetGameMode() != GameMode.ZombieSurvival)
		{
			Object.Destroy(this);
			return;
		}
		PhotonClassesManager.Add(this);
		instance = this;
	}

	private void Start()
	{
		UIGameManager.SetActiveScore(true, 20);
		GameManager.SetStartDamageTime(1f);
		UIPanelManager.ShowPanel("Display");
		GameManager.MaxScore = 20;
		CameraManager.ActiveStaticCamera();
		TimerManager.In(1f, () =>
		{
			if (PhotonNetwork.isMasterClient)
			{
				ActivationWaitPlayer();
			}
			else if (GameManager.GetRoundState() == RoundState.WaitPlayer || GameManager.GetRoundState() == RoundState.StartRound)
			{
				OnCreatePlayer();
			}
			else
			{
				OnCreateZombie();
			}
		});
		EventManager.AddListener<DamageInfo>("DeadPlayer", OnDeadPlayer);
		EventManager.AddListener<string>("ServerCommand", CheckPlayersCommand);
	}

	private void ActivationWaitPlayer()
	{
		EventManager.Dispatch("WaitPlayer");
		GameManager.UpdateRoundState(RoundState.WaitPlayer);
		OnWaitPlayer();
		OnCreatePlayer();
	}

	private void OnWaitPlayer()
	{
		UIStatus.Add(Localization.Get("Waiting for other players"), true);
		TimerManager.In(4f, () =>
		{
			if (GameManager.GetRoundState() == RoundState.WaitPlayer)
			{
				if (PhotonNetwork.playerList.Length <= 1)
				{
					OnWaitPlayer();
				}
				else
				{
					GameManager.UpdateRoundState(RoundState.StartRound);
					TimerManager.In(4f, () =>
					{
						OnStartRound();
					});
				}
			}
		});
	}

	public override void OnPhotonPlayerConnected(PhotonPlayer playerConnect)
	{
		if (!PhotonNetwork.isMasterClient)
		{
			return;
		}
		TimerManager.In(0.5f, () =>
		{
			GameManager.UpdateScore(playerConnect);
		});
		if (GameManager.GetRoundState() != RoundState.WaitPlayer)
		{
			CheckPlayers();
			if (UIGameManager.instance.isScoreTimer)
			{
				base.photonView.RPC("UpdateTimer", playerConnect, UIGameManager.instance.ScoreTimer - Time.time);
			}
		}
		if (Blocks == null || Blocks.Length <= 0)
		{
			return;
		}
		List<byte> list = new List<byte>();
		for (int num = 0; num < Blocks.Length; num++)
		{
			if (!Blocks[num].GetActive())
			{
				list.Add((byte)Blocks[num].ID);
			}
		}
		base.photonView.RPC("DeactiveBlocks", playerConnect, list.ToArray());
	}

	public override void OnPhotonPlayerDisconnected(PhotonPlayer playerDisconnect)
	{
		if (PhotonNetwork.isMasterClient)
		{
			CheckPlayers();
		}
	}

	private void OnStartRound()
	{
		DecalsManager.ClearBulletHoles();
		if (PhotonNetwork.playerList.Length <= 1)
		{
			ActivationWaitPlayer();
		}
		else if (PhotonNetwork.isMasterClient)
		{
			GameManager.UpdateRoundState(RoundState.StartRound);
			base.photonView.RPC("StartTimer", PhotonTargets.All);
		}
	}

	[PunRPC]
	private void StartTimer(PhotonMessageInfo info)
	{
		EventManager.Dispatch("StartRound");
		OnCreatePlayer();
		UIToast.Show(Localization.Get("Infestation will start in 20 seconds"));
		float num = 20f;
		num -= (float)(PhotonNetwork.time - info.timestamp);
		StartZombieTimerID = TimerManager.In(num, () =>
		{
			if (PhotonNetwork.isMasterClient)
			{
				List<PhotonPlayer> list = PhotonNetwork.playerList.ToList();
				int num2 = OnSelectMaxDeaths(list.Count);
				string text = string.Empty;
				for (int i = 0; i < num2; i++)
				{
					int index = Random.Range(0, list.Count);
					text = text + list[index].ID + "#";
					list.RemoveAt(index);
				}
				base.photonView.RPC("OnSendKillerInfo", PhotonTargets.All, text);
			}
			GameManager.UpdateRoundState(RoundState.PlayRound);
		});
	}

	[PunRPC]
	private void UpdateTimer(float time, PhotonMessageInfo info)
	{
		TimerManager.In(1.5f, () =>
		{
			time -= (float)(PhotonNetwork.time - info.timestamp);
			UIGameManager.StartScoreTimer(time, StopTimer);
		});
	}

	private void StopTimer()
	{
		if (PhotonNetwork.isMasterClient)
		{
			++GameManager.BlueScore;
			GameManager.UpdateScore();
			UIMainStatus.Add("@", false, 5f, "Survivors Win");
			base.photonView.RPC("OnFinishRound", PhotonTargets.All);
		}
	}

	[PunRPC]
	private void OnSendKillerInfo(string text, PhotonMessageInfo info)
	{
		string[] array = text.Split("#"[0]);
		bool flag = false;
		for (int i = 0; i < array.Length - 1; i++)
		{
			if (PhotonNetwork.player.ID == int.Parse(array[i]))
			{
				flag = true;
				break;
			}
		}
		UIToast.Show(Localization.Get("Infestation started"));
		float num = 300f;
		num -= (float)(PhotonNetwork.time - info.timestamp);
		UIGameManager.StartScoreTimer(num, StopTimer);
		if (flag)
		{
			OnCreateZombie();
		}
		TimerManager.In(3f, () =>
		{
			if (PhotonNetwork.isMasterClient)
			{
				CheckPlayers();
			}
		});
	}

	private void OnCreatePlayer()
	{
		PlayerInput playerInput = GameManager.GetController().PlayerInput;
		playerInput.Zombie = false;
		playerInput.DamageSpeed = false;
		GameManager.OnSelectTeam(Team.Blue);
		playerInput.MaxHealth = 100;
		playerInput.SetHealth(100);
		CameraManager.DeactiveAll();
		GameManager.GetController().ActivePlayer(GameManager.GetTeamSpawn().GetSpawnPosition(), GameManager.GetTeamSpawn().GetSpawnRotation());
		playerInput.UpdatePlayerSpeed(0.18f);
		playerInput.FPCamera.GetComponent<Camera>().fieldOfView = 60f;
		WeaponManager.SetSelectWeapon(WeaponType.Knife, AccountManager.GetWeaponSelected(WeaponType.Knife));
		WeaponManager.SetSelectWeapon(WeaponType.Pistol, AccountManager.GetWeaponSelected(WeaponType.Pistol));
		WeaponManager.SetSelectWeapon(WeaponType.Rifle, AccountManager.GetWeaponSelected(WeaponType.Rifle));
		playerInput.PlayerWeapon.UpdateWeaponAll(WeaponType.Rifle);
		TimerManager.In(0.5f, () =>
		{
			PlayerWeapons.PlayerWeaponData weaponData = playerInput.PlayerWeapon.GetWeaponData(WeaponType.Pistol);
			weaponData.AmmoMax = (int)weaponData.AmmoMax * 3;
			PlayerWeapons.PlayerWeaponData weaponData2 = playerInput.PlayerWeapon.GetWeaponData(WeaponType.Rifle);
			weaponData2.AmmoMax = (int)weaponData2.AmmoMax * 3;
			UIGameManager.SetAmmoLabel(playerInput.PlayerWeapon.GetSelectedWeaponData().Ammo, playerInput.PlayerWeapon.GetSelectedWeaponData().AmmoMax);
		});
	}

	private void OnCreateZombie()
	{
		PlayerInput playerInput = GameManager.GetController().PlayerInput;
		playerInput.Zombie = true;
		playerInput.DamageSpeed = true;
		GameManager.OnSelectTeam(Team.Red);
		playerInput.MaxHealth = 1000;
		playerInput.SetHealth(1000);
		CameraManager.DeactiveAll();
		GameManager.GetController().ActivePlayer(GameManager.GetTeamSpawn().GetSpawnPosition(), GameManager.GetTeamSpawn().GetSpawnRotation());
		playerInput.UpdatePlayerSpeed(0.19f);
		playerInput.FPCamera.GetComponent<Camera>().fieldOfView = 100f;
		WeaponManager.SetSelectWeapon(WeaponType.Knife, 17);
		WeaponManager.SetSelectWeapon(WeaponType.Pistol, 0);
		WeaponManager.SetSelectWeapon(WeaponType.Rifle, 0);
		playerInput.PlayerWeapon.UpdateWeaponAll(WeaponType.Knife);
	}

	private void OnDeadPlayer(DamageInfo damageInfo)
	{
		if (GameManager.GetRoundState() == RoundState.PlayRound || GameManager.GetRoundState() == RoundState.StartRound)
		{
			PhotonNetwork.player.SetDeaths1();
			PlayerRoundManager.SetDeaths1();
		}
		if (damageInfo.PlayerID != -1)
		{
			UIStatus.Add(Utils.KillerStatus(damageInfo));
			if (damageInfo.AttackerTeam == Team.Blue)
			{
				UIDeathScreen.Show(damageInfo);
			}
		}
		else if (GameManager.GetRoundState() == RoundState.PlayRound || GameManager.GetRoundState() == RoundState.StartRound)
		{
			string text = Utils.GetTeamHexColor(PhotonNetwork.player) + " @";
			UIStatus.Add(text, false, "died");
		}
		if (GameManager.GetRoundState() == RoundState.PlayRound || GameManager.GetRoundState() == RoundState.StartRound)
		{
			Vector3 ragdollForce = Utils.GetRagdollForce(GameManager.GetController().PlayerInput.PlayerTransform.position, damageInfo.AttackPosition);
			CameraManager.ActiveDeadCamera(GameManager.GetController().PlayerInput.FPCamera.Transform.position, GameManager.GetController().PlayerInput.FPCamera.Transform.eulerAngles, ragdollForce * 100f);
			GameManager.GetController().DeactivePlayer(ragdollForce, damageInfo.HeadShot);
		}
		else
		{
			OnCreatePlayer();
		}
		if (damageInfo.PlayerID != -1)
		{
			base.photonView.RPC("OnKilledPlayer", PhotonPlayer.Find(damageInfo.PlayerID), damageInfo);
		}
		if (GameManager.GetRoundState() != RoundState.PlayRound && GameManager.GetRoundState() != RoundState.StartRound)
		{
			return;
		}
		int num = ((damageInfo.PlayerID == -1) ? 1 : 3);
		base.photonView.RPC("CheckPlayers", PhotonTargets.MasterClient);
		TimerManager.In(num, () =>
		{
			if ((bool)GameManager.GetController().PlayerInput.Dead)
			{
				OnCreateZombie();
			}
		});
	}

	[PunRPC]
	private void OnKilledPlayer(DamageInfo damageInfo)
	{
		EventManager.Dispatch("KillPlayer", damageInfo);
		PhotonNetwork.player.SetKills1();
		PlayerRoundManager.SetKills1();
		AchievementsManager.UpdateKills(damageInfo);
		if (damageInfo.HeadShot)
		{
			if (damageInfo.AttackerTeam == Team.Red)
			{
				PlayerRoundManager.SetXP(10);
				PlayerRoundManager.SetMoney(5);
			}
			else
			{
				PlayerRoundManager.SetXP(15);
				PlayerRoundManager.SetMoney(10);
			}
			PlayerRoundManager.SetHeadshot1();
		}
		else if (damageInfo.AttackerTeam == Team.Red)
		{
			PlayerRoundManager.SetXP(5);
			PlayerRoundManager.SetMoney(4);
		}
		else
		{
			PlayerRoundManager.SetXP(10);
			PlayerRoundManager.SetMoney(8);
		}
	}

	[PunRPC]
	private void OnFinishRound(PhotonMessageInfo info)
	{
		TimerManager.Cancel(StartZombieTimerID);
		UIGameManager.instance.isScoreTimer = false;
		GameManager.UpdateRoundState(RoundState.EndRound);
		if (GameManager.CheckScore())
		{
			GameManager.LoadNextLevel(GameMode.ZombieSurvival);
			return;
		}
		float delay = 6f - (float)(PhotonNetwork.time - info.timestamp);
		TimerManager.In(delay, () =>
		{
			OnStartRound();
		});
	}

	[PunRPC]
	private void OnLoadNextMap(PhotonMessageInfo info)
	{
		float num = 5f - (float)(PhotonNetwork.time - info.timestamp);
		if (num < 0f)
		{
			num = 0.1f;
		}
		TimerManager.In(1f, () =>
		{
			PhotonNetwork.player.ClearProperties();
		});
		TimerManager.In(num, () =>
		{
			PhotonNetwork.RemoveRPCs(PhotonNetwork.player);
			PhotonNetwork.DestroyPlayerObjects(PhotonNetwork.player);
			PhotonNetwork.LoadLevel(LevelManager.GetNextScene(GameMode.ZombieSurvival));
		});
	}

	[PunRPC]
	private void CheckPlayers()
	{
		if (!PhotonNetwork.isMasterClient || GameManager.GetRoundState() != RoundState.PlayRound)
		{
			return;
		}
		PhotonPlayer[] playerList = PhotonNetwork.playerList;
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < playerList.Length; i++)
		{
			if (playerList[i].GetTeam() == Team.Blue && !playerList[i].GetDead())
			{
				flag = true;
				break;
			}
		}
		for (int j = 0; j < playerList.Length; j++)
		{
			if (playerList[j].GetTeam() == Team.Red)
			{
				flag2 = true;
				break;
			}
		}
		if (!flag)
		{
			++GameManager.RedScore;
			GameManager.UpdateScore();
			UIMainStatus.Add("@", false, 5f, "Zombie Win");
			base.photonView.RPC("OnFinishRound", PhotonTargets.All);
		}
		else if (!flag2)
		{
			++GameManager.BlueScore;
			GameManager.UpdateScore();
			UIMainStatus.Add("@", false, 5f, "Survivors Win");
			base.photonView.RPC("OnFinishRound", PhotonTargets.All);
		}
	}

	private void CheckPlayersCommand(string text)
	{
		if (text != "!checkserver" || !PhotonNetwork.isMasterClient || GameManager.GetRoundState() != RoundState.StartRound)
		{
			return;
		}
		PhotonPlayer[] playerList = PhotonNetwork.playerList;
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < playerList.Length; i++)
		{
			if (playerList[i].GetTeam() == Team.Blue && !playerList[i].GetDead())
			{
				flag = true;
				break;
			}
		}
		for (int j = 0; j < playerList.Length; j++)
		{
			if (playerList[j].GetTeam() == Team.Red)
			{
				flag2 = true;
				break;
			}
		}
		if (!flag)
		{
			++GameManager.RedScore;
			GameManager.UpdateScore();
			UIMainStatus.Add("@", false, 5f, "Zombie Win");
			base.photonView.RPC("OnFinishRound", PhotonTargets.All);
		}
		else if (!flag2)
		{
			++GameManager.BlueScore;
			GameManager.UpdateScore();
			UIMainStatus.Add("@", false, 5f, "Survivors Win");
			base.photonView.RPC("OnFinishRound", PhotonTargets.All);
		}
	}

	private int OnSelectMaxDeaths(int maxPlayers)
	{
		if (maxPlayers >= 8)
		{
			return 2;
		}
		return 1;
	}

	public static void AddDamage(byte id)
	{
		instance.photonView.RPC("PhotonAddDamage", PhotonTargets.All, id);
	}

	[PunRPC]
	private void PhotonAddDamage(byte id)
	{
		for (int i = 0; i < Blocks.Length; i++)
		{
			if (Blocks[i].ID == id)
			{
				Blocks[i].Attack();
				if (PhotonNetwork.isMasterClient && Blocks[i].CountAttack == 0)
				{
					base.photonView.RPC("DeactiveBlock", PhotonTargets.All, id);
				}
			}
		}
	}

	[PunRPC]
	private void DeactiveBlock(byte id)
	{
		for (int i = 0; i < Blocks.Length; i++)
		{
			if (Blocks[i].ID == id)
			{
				Blocks[i].SetActive(false);
			}
		}
	}

	[PunRPC]
	private void DeactiveBlocks(byte[] ids)
	{
		for (int i = 0; i < Blocks.Length; i++)
		{
			for (int j = 0; j < ids.Length; j++)
			{
				if (Blocks[i].ID == ids[j])
				{
					Blocks[i].SetActive(false);
				}
			}
		}
	}
}
