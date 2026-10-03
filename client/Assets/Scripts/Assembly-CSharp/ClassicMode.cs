using Photon;
using UnityEngine;

public class ClassicMode : PunBehaviour
{
	private void Awake()
	{
		if (PhotonNetwork.offlineMode)
		{
			Object.Destroy(this);
		}
		else if (PhotonNetwork.room.GetGameMode() != GameMode.Classic)
		{
			Object.Destroy(this);
		}
		else
		{
			PhotonClassesManager.Add(this);
		}
	}

	private void Start()
	{
		UIGameManager.SetActiveScore(true, 20);
		GameManager.SetStartDamageTime(1f);
		GameManager.SetGlobalChat(false);
		UIPanelManager.ShowPanel("Display");
		CameraManager.ActiveStaticCamera();
		CameraManager.SpectatePlayerTeam = true;
		if (PhotonNetwork.isMasterClient)
		{
			TimerManager.In(0.5f, () =>
			{
				ActivationWaitPlayer();
			});
		}
		else
		{
			UISelectTeam.OnStart();
		}
		EventManager.AddListener<Team>("SelectTeam", OnSelectTeam);
		EventManager.AddListener<DamageInfo>("DeadPlayer", OnDeadPlayer);
	}

	private void OnSelectTeam(Team team)
	{
		UIPanelManager.ShowPanel("Display");
		if ((bool)GameManager.GetController().PlayerInput.Dead)
		{
			CameraManager.ActiveSpectateCamera();
		}
	}

	private void ActivationWaitPlayer()
	{
		EventManager.Dispatch("WaitPlayer");
		GameManager.UpdateRoundState(RoundState.WaitPlayer);
		GameManager.OnSelectTeam(Team.Blue);
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
		if (PhotonNetwork.isMasterClient)
		{
			GameManager.UpdateScore(playerConnect);
			if (GameManager.GetRoundState() != RoundState.WaitPlayer)
			{
				CheckPlayers();
			}
		}
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
			GameManager.UpdateRoundState(RoundState.PlayRound);
			base.photonView.RPC("OnCreatePlayer", PhotonTargets.All);
		}
	}

	[PunRPC]
	private void OnCreatePlayer()
	{
		if (PhotonNetwork.player.GetTeam() != Team.None)
		{
			PlayerInput playerInput = GameManager.GetController().PlayerInput;
			playerInput.SetHealth(100);
			CameraManager.DeactiveAll();
			GameManager.GetController().ActivePlayer(GameManager.GetTeamSpawn().GetSpawnPosition(), GameManager.GetTeamSpawn().GetSpawnRotation());
			playerInput.PlayerWeapon.UpdateWeaponAll(WeaponType.Rifle);
		}
	}

	private void OnDeadPlayer(DamageInfo damageInfo)
	{
		PhotonNetwork.player.SetDeaths1();
		PlayerRoundManager.SetDeaths1();
		UIStatus.Add(Utils.KillerStatus(damageInfo));
		UIDeathScreen.Show(damageInfo);
		Vector3 ragdollForce = Utils.GetRagdollForce(GameManager.GetController().PlayerInput.PlayerTransform.position, damageInfo.AttackPosition);
		CameraManager.ActiveDeadCamera(GameManager.GetController().PlayerInput.FPCamera.Transform.position, GameManager.GetController().PlayerInput.FPCamera.Transform.eulerAngles, ragdollForce * 100f);
		GameManager.GetController().DeactivePlayer(ragdollForce, damageInfo.HeadShot);
		base.photonView.RPC("OnKilledPlayer", PhotonPlayer.Find(damageInfo.PlayerID), damageInfo);
		base.photonView.RPC("CheckPlayers", PhotonTargets.MasterClient);
		GameManager.BalanceTeam(true);
		TimerManager.In(3f, () =>
		{
			if ((bool)GameManager.GetController().PlayerInput.Dead)
			{
				CameraManager.ActiveSpectateCamera();
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
			PlayerRoundManager.SetXP(12);
			PlayerRoundManager.SetMoney(10);
			PlayerRoundManager.SetHeadshot1();
		}
		else
		{
			PlayerRoundManager.SetXP(6);
			PlayerRoundManager.SetMoney(5);
		}
	}

	[PunRPC]
	private void OnFinishRound(PhotonMessageInfo info)
	{
		GameManager.UpdateRoundState(RoundState.EndRound);
		GameManager.BalanceTeam(true);
		if (GameManager.CheckScore())
		{
			GameManager.LoadNextLevel(GameMode.Classic);
			return;
		}
		float delay = 8f - (float)(PhotonNetwork.time - info.timestamp);
		TimerManager.In(delay, () =>
		{
			OnStartRound();
		});
	}

	[PunRPC]
	private void CheckPlayers()
	{
		if (!PhotonNetwork.isMasterClient || GameManager.GetRoundState() == RoundState.EndRound)
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
			if (playerList[j].GetTeam() == Team.Red && !playerList[j].GetDead())
			{
				flag2 = true;
				break;
			}
		}
		if (!flag)
		{
			++GameManager.RedScore;
			GameManager.UpdateScore();
			UIMainStatus.Add("@", false, 5f, "Red Win");
			base.photonView.RPC("OnFinishRound", PhotonTargets.All);
		}
		else if (!flag2)
		{
			++GameManager.BlueScore;
			GameManager.UpdateScore();
			UIMainStatus.Add("@", false, 5f, "Blue Win");
			base.photonView.RPC("OnFinishRound", PhotonTargets.All);
		}
	}
}
