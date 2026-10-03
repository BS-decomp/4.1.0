using System.Collections.Generic;
using System.Linq;
using Photon;
using UnityEngine;

public class FootballMode : PunBehaviour
{
	private void Awake()
	{
		if (PhotonNetwork.offlineMode)
		{
			Object.Destroy(this);
		}
		else if (PhotonNetwork.room.GetGameMode() != GameMode.Football)
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
		GameManager.SetStartDamageTime(-1f);
		UIPanelManager.ShowPanel("Display");
		CameraManager.ActiveStaticCamera();
		GameManager.GetController().PlayerInput.PlayerWeapon.PushRigidbody = true;
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
		TimerManager.In(0.5f, () =>
		{
			PlayerInput playerInput = GameManager.GetController().PlayerInput;
			playerInput.BunnyHopEnabled = true;
			playerInput.BunnyHopSpeed = 0.25f;
			playerInput.FPController.MotorJumpForce = 0.2f;
			playerInput.FPController.MotorAirSpeed = 1f;
		});
		EventManager.AddListener<Team>("SelectTeam", OnSelectTeam);
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
		FootballManager.StartRound();
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

	public void OnCreatePlayer()
	{
		OnCreatePlayer(default(PhotonMessageInfo));
	}

	[PunRPC]
	private void OnCreatePlayer(PhotonMessageInfo info)
	{
		if (PhotonNetwork.player.GetTeam() != Team.None)
		{
			WeaponManager.SetSelectWeapon(WeaponType.Pistol, 0);
			WeaponManager.SetSelectWeapon(WeaponType.Rifle, 0);
			PlayerInput playerInput = GameManager.GetController().PlayerInput;
			playerInput.SetHealth(100);
			if (info.timestamp == 0.0)
			{
				float duration = (float)(PhotonNetwork.time - info.timestamp + 3.0);
				playerInput.SetMove(false, duration);
			}
			CameraManager.DeactiveAll();
			int num = UIPlayerStatistics.GetPlayerStatsPosition(PhotonNetwork.player) - 1;
			if (PhotonNetwork.player.GetTeam() == Team.Red)
			{
				num += 6;
			}
			GameManager.GetController().ActivePlayer(GameManager.GetSpawn(num).GetSpawnPosition(), GameManager.GetSpawn(num).GetSpawnRotation());
			playerInput.PlayerWeapon.UpdateWeaponAll(WeaponType.Knife);
		}
	}

	public void Goal(PhotonPlayer player, Team gateTeam)
	{
		bool flag = false;
		UIMainStatus.Add(player.NickName + " @", false, 5f, "scored a goal");
		if (player.GetTeam() == Team.Red)
		{
			if (gateTeam == Team.Blue)
			{
				++GameManager.RedScore;
				GameManager.UpdateScore();
			}
			else
			{
				++GameManager.BlueScore;
				GameManager.UpdateScore();
				flag = true;
			}
		}
		else if (gateTeam == Team.Red)
		{
			++GameManager.BlueScore;
			GameManager.UpdateScore();
		}
		else
		{
			++GameManager.RedScore;
			GameManager.UpdateScore();
			flag = true;
		}
		base.photonView.RPC("OnFinishRound", PhotonTargets.All);
		if (flag)
		{
			TimerManager.In(1.5f, () =>
			{
				UIMainStatus.Add("@", false, 3f, "Autogoal");
			});
			base.photonView.RPC("AutoGoal", player);
		}
		else
		{
			base.photonView.RPC("GoalPlayer", player);
		}
	}

	[PunRPC]
	private void GoalPlayer()
	{
		PhotonNetwork.player.SetKills1();
		PlayerRoundManager.SetXP(10 + 2 * PhotonNetwork.playerList.Length);
		PlayerRoundManager.SetMoney(4 + PhotonNetwork.playerList.Length - 1);
	}

	[PunRPC]
	private void AutoGoal()
	{
		PhotonNetwork.player.SetDeaths1();
	}

	[PunRPC]
	private void OnFinishRound(PhotonMessageInfo info)
	{
		GameManager.UpdateRoundState(RoundState.EndRound);
		GameManager.BalanceTeam(true);
		if (GameManager.CheckScore())
		{
			GameManager.LoadNextLevel(GameMode.Football);
			return;
		}
		float delay = 5f - (float)(PhotonNetwork.time - info.timestamp);
		TimerManager.In(delay, () =>
		{
			OnStartRound();
		});
	}

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
		if (!flag || !flag2)
		{
			base.photonView.RPC("OnFinishRound", PhotonTargets.All);
		}
	}

	private void UpdateMasterServer()
	{
		if (PhotonNetwork.isMasterClient && GameManager.GetRoundState() == RoundState.PlayRound)
		{
			List<PhotonPlayer> list = PhotonNetwork.playerList.ToList();
			list.Sort(SortByPing);
			if (list[0].ID != PhotonNetwork.player.ID)
			{
				PhotonNetwork.SetMasterClient(list[0]);
			}
		}
	}

	public static int SortByPing(PhotonPlayer a, PhotonPlayer b)
	{
		return a.GetPing().CompareTo(b.GetPing());
	}
}
