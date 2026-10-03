using Photon;
using UnityEngine;

public class BombMode : PunBehaviour
{
	public static BombMode instance;

	private void Awake()
	{
		if (PhotonNetwork.offlineMode)
		{
			Object.Destroy(this);
			return;
		}
		if (PhotonNetwork.room.GetGameMode() != GameMode.Bomb)
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
		GameManager.OnSelectTeam(Team.Red);
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
		if (!PhotonNetwork.isMasterClient)
		{
			return;
		}
		GameManager.UpdateScore(playerConnect);
		if (GameManager.GetRoundState() == RoundState.WaitPlayer)
		{
			return;
		}
		CheckPlayers();
		if (UIGameManager.instance.isScoreTimer && !BombManager.BombPlaced)
		{
			TimerManager.In(0.5f, () =>
			{
				base.photonView.RPC("UpdateTimer", playerConnect, UIGameManager.instance.ScoreTimer - Time.time);
			});
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
			base.photonView.RPC("PhotonStartRound", PhotonTargets.All);
		}
	}

	[PunRPC]
	private void PhotonStartRound(PhotonMessageInfo info)
	{
		EventManager.Dispatch("StartRound");
		float num = 120f;
		num -= (float)(PhotonNetwork.time - info.timestamp);
		UIGameManager.StartScoreTimer(num, StopTimer);
		OnCreatePlayer();
	}

	private void StopTimer()
	{
		if (PhotonNetwork.isMasterClient)
		{
			++GameManager.BlueScore;
			GameManager.UpdateScore();
			UIMainStatus.Add("@", false, 5f, "Blue Win");
			base.photonView.RPC("OnFinishRound", PhotonTargets.All);
		}
	}

	public void Boom()
	{
		if (PhotonNetwork.isMasterClient && GameManager.GetRoundState() == RoundState.PlayRound)
		{
			++GameManager.RedScore;
			GameManager.UpdateScore();
			UIMainStatus.Add("@", false, 5f, "Red Win");
			base.photonView.RPC("OnFinishRound", PhotonTargets.All);
		}
	}

	public void DeactiveBoom()
	{
		if (PhotonNetwork.isMasterClient && GameManager.GetRoundState() == RoundState.PlayRound)
		{
			++GameManager.BlueScore;
			GameManager.UpdateScore();
			UIMainStatus.Add("@", false, 5f, "Blue Win");
			base.photonView.RPC("OnFinishRound", PhotonTargets.All);
		}
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
		if (damageInfo.PlayerID != -1)
		{
			UIStatus.Add(Utils.KillerStatus(damageInfo));
			UIDeathScreen.Show(damageInfo);
		}
		else
		{
			string text = Utils.GetTeamHexColor(PhotonNetwork.player) + " @";
			UIStatus.Add(text, false, "died");
		}
		PlayerInput playerInput = GameManager.GetController().PlayerInput;
		Vector3 ragdollForce = Utils.GetRagdollForce(playerInput.PlayerTransform.position, damageInfo.AttackPosition);
		CameraManager.ActiveDeadCamera(GameManager.GetController().PlayerInput.FPCamera.Transform.position, playerInput.FPCamera.Transform.eulerAngles, ragdollForce * 100f);
		GameManager.GetController().DeactivePlayer(ragdollForce, damageInfo.HeadShot);
		if (damageInfo.PlayerID != -1)
		{
			base.photonView.RPC("OnKilledPlayer", PhotonPlayer.Find(damageInfo.PlayerID), damageInfo);
		}
		base.photonView.RPC("CheckPlayers", PhotonTargets.MasterClient);
		GameManager.BalanceTeam(true);
		TimerManager.In(3f, () =>
		{
			if ((bool)GameManager.GetController().PlayerInput.Dead)
			{
				CameraManager.ActiveSpectateCamera();
			}
		});
		BombManager.DeadPlayer();
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
		UIGameManager.instance.isScoreTimer = false;
		GameManager.UpdateRoundState(RoundState.EndRound);
		GameManager.BalanceTeam(true);
		if ((int)GameManager.BlueScore + (int)GameManager.RedScore == (int)GameManager.MaxScore / 2)
		{
			if (PhotonNetwork.isMasterClient)
			{
				int num = GameManager.BlueScore;
				GameManager.BlueScore = GameManager.RedScore;
				GameManager.RedScore = num;
				GameManager.UpdateScore();
			}
			if (GameManager.GetPlayerTeam() == Team.Blue)
			{
				GameManager.UpdatePlayerTeam(Team.Red);
			}
			else if (GameManager.GetPlayerTeam() == Team.Red)
			{
				GameManager.UpdatePlayerTeam(Team.Blue);
			}
		}
		else
		{
			GameManager.BalanceTeam(true);
		}
		if (GameManager.CheckScore())
		{
			GameManager.LoadNextLevel(GameMode.Bomb);
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
		if (!BombManager.BombPlaced)
		{
			for (int j = 0; j < playerList.Length; j++)
			{
				if (playerList[j].GetTeam() == Team.Red && !playerList[j].GetDead())
				{
					flag2 = true;
					break;
				}
			}
		}
		else
		{
			flag2 = true;
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
