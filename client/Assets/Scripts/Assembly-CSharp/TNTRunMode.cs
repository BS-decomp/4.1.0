using Photon;
using UnityEngine;

public class TNTRunMode : Photon.MonoBehaviour
{
	private void Awake()
	{
		if (PhotonNetwork.offlineMode)
		{
			Object.Destroy(this);
		}
	}

	private void Start()
	{
		UIGameManager.SetActiveScore(true, 20);
		GameManager.SetStartDamageTime(0.1f);
		UIPanelManager.ShowPanel("Display");
		CameraManager.ActiveStaticCamera();
		WeaponManager.SetSelectWeapon(WeaponType.Pistol, 0);
		WeaponManager.SetSelectWeapon(WeaponType.Rifle, 0);
		GameManager.SetChangeWeapons(false);
		TimerManager.In(0.5f, () =>
		{
			GameManager.OnSelectTeam(Team.Blue);
			if (PhotonNetwork.isMasterClient)
			{
				ActivationWaitPlayer();
			}
			else if ((bool)GameManager.GetController().PlayerInput.Dead)
			{
				CameraManager.ActiveSpectateCamera();
			}
		});
		EventManager.AddListener<DamageInfo>("DeadPlayer", OnDeadPlayer);
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

	private void OnStartRound()
	{
		TNTRunManager.ResetBlocks();
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
		OnCreatePlayer();
		float num = (float)(PhotonNetwork.time - info.timestamp);
		TimerManager.In(3f - num, () =>
		{
			UIMainStatus.Add("3", true);
		});
		TimerManager.In(4f - num, () =>
		{
			UIMainStatus.Add("2", true);
		});
		TimerManager.In(5f - num, () =>
		{
			UIMainStatus.Add("1", true);
		});
		TimerManager.In(6f - num, () =>
		{
			UIMainStatus.Add("Go", true);
			DrawElements playerIDSpawn = GameManager.GetPlayerIDSpawn();
			GameManager.GetController().SetPosition(playerIDSpawn.GetSpawnPosition());
			GameManager.UpdateRoundState(RoundState.PlayRound);
		});
	}

	private void OnCreatePlayer()
	{
		if (PhotonNetwork.player.GetTeam() != Team.None)
		{
			PlayerInput playerInput = GameManager.GetController().PlayerInput;
			playerInput.SetHealth(100);
			CameraManager.DeactiveAll();
			DrawElements playerIDSpawn = GameManager.GetPlayerIDSpawn();
			GameManager.GetController().ActivePlayer(playerIDSpawn.GetSpawnPosition(), playerIDSpawn.GetSpawnRotation());
			playerInput.PlayerWeapon.UpdateWeaponAll(WeaponType.Knife);
		}
	}

	private void OnDeadPlayer(DamageInfo damageInfo)
	{
		if (GameManager.GetRoundState() == RoundState.PlayRound || GameManager.GetRoundState() == RoundState.StartRound)
		{
			PhotonNetwork.player.SetDeaths1();
			PlayerRoundManager.SetDeaths1();
		}
		string text = Utils.GetTeamHexColor(PhotonNetwork.player) + " @";
		UIStatus.Add(text, false, "died");
		if (GameManager.GetRoundState() == RoundState.PlayRound || GameManager.GetRoundState() == RoundState.StartRound)
		{
			Vector3 ragdollForce = Utils.GetRagdollForce(GameManager.GetController().PlayerInput.PlayerTransform.position, damageInfo.AttackPosition);
			CameraManager.ActiveDeadCamera(GameManager.GetController().PlayerInput.FPCamera.Transform.position, GameManager.GetController().PlayerInput.FPCamera.Transform.eulerAngles, ragdollForce * 100f);
			GameManager.GetController().DeactivePlayer(ragdollForce, damageInfo.HeadShot);
			base.photonView.RPC("CheckPlayers", PhotonTargets.MasterClient);
			TimerManager.In(3f, () =>
			{
				if ((bool)GameManager.GetController().PlayerInput.Dead)
				{
					CameraManager.ActiveSpectateCamera();
				}
			});
		}
		else
		{
			OnCreatePlayer();
		}
	}

	[PunRPC]
	private void OnFinishRound(PhotonMessageInfo info)
	{
		GameManager.UpdateRoundState(RoundState.EndRound);
		if (!GameManager.CheckScore())
		{
			float delay = 8f - (float)(PhotonNetwork.time - info.timestamp);
			TimerManager.In(delay, () =>
			{
				OnStartRound();
			});
		}
	}

	[PunRPC]
	private void CheckPlayers()
	{
		if (!PhotonNetwork.isMasterClient || GameManager.GetRoundState() == RoundState.EndRound)
		{
			return;
		}
		PhotonPlayer[] playerList = PhotonNetwork.playerList;
		PhotonPlayer photonPlayer = null;
		for (int i = 0; i < playerList.Length; i++)
		{
			if (!playerList[i].GetDead())
			{
				if (photonPlayer != null)
				{
					return;
				}
				photonPlayer = playerList[i];
			}
		}
		if (photonPlayer != null)
		{
			++GameManager.BlueScore;
			GameManager.UpdateScore();
			UIMainStatus.Add(photonPlayer.NickName + " @", false, 5f, "Win");
			base.photonView.RPC("OnFinishRound", PhotonTargets.All);
		}
	}
}
