using Photon;
using UnityEngine;

public class BunnyHop : PunBehaviour
{
	private Vector3 StartSpawnPosition;

	private Quaternion StartSpawnRotation;

	private static BunnyHop instance;

	private void Awake()
	{
		if (PhotonNetwork.room.GetGameMode() != GameMode.BunnyHop)
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
		instance = this;
		GameManager.UpdateRoundState(RoundState.PlayRound);
		UIGameManager.SetActiveScore(true, 0);
		GameManager.SetStartDamageTime(0.1f);
		UIPanelManager.ShowPanel("Display");
		GameManager.SetChangeWeapons(false);
		CameraManager.ActiveStaticCamera();
		TimerManager.In(0.5f, () =>
		{
			GameManager.OnSelectTeam(Team.Blue);
			StartSpawnPosition = GameManager.GetTeamSpawn(Team.Blue).GetTransform().position;
			StartSpawnRotation = GameManager.GetTeamSpawn(Team.Blue).GetTransform().rotation;
			PlayerInput playerInput = GameManager.GetController().PlayerInput;
			playerInput.BunnyHopEnabled = true;
			playerInput.FPController.MotorJumpForce = 0.2f;
			playerInput.FPController.MotorAirSpeed = 1f;
			OnRevivalPlayer();
			TimerManager.In(1.5f, () =>
			{
				if (PhotonNetwork.isMasterClient)
				{
					base.photonView.RPC("StartTimer", PhotonTargets.All);
				}
			});
		});
		EventManager.AddListener<DamageInfo>("DeadPlayer", OnDeadPlayer);
	}

	private void OnSpawnPlayer()
	{
		GameManager.GetController().SpawnPlayer(GameManager.GetTeamSpawn().GetSpawnPosition(), GameManager.GetTeamSpawn().GetSpawnRotation());
	}

	private void OnRevivalPlayer()
	{
		WeaponManager.SetSelectWeapon(WeaponType.Pistol, 0);
		WeaponManager.SetSelectWeapon(WeaponType.Rifle, 0);
		PlayerInput playerInput = GameManager.GetController().PlayerInput;
		playerInput.SetHealth(100);
		CameraManager.DeactiveAll();
		GameManager.GetController().ActivePlayer(GameManager.GetTeamSpawn().GetSpawnPosition(), GameManager.GetTeamSpawn().GetSpawnRotation());
		playerInput.PlayerWeapon.UpdateWeaponAll(WeaponType.Knife);
	}

	private void OnDeadPlayer(DamageInfo damageInfo)
	{
		PhotonNetwork.player.SetDeaths1();
		++GameManager.RedScore;
		UIGameManager.UpdateScoreLabel(0, GameManager.BlueScore, GameManager.RedScore);
		OnSpawnPlayer();
	}

	public override void OnPhotonPlayerConnected(PhotonPlayer playerConnect)
	{
		if (PhotonNetwork.isMasterClient && UIGameManager.instance.isScoreTimer)
		{
			base.photonView.RPC("UpdateTimer", playerConnect, UIGameManager.instance.ScoreTimer - Time.time);
		}
	}

	[PunRPC]
	private void StartTimer(PhotonMessageInfo info)
	{
		float num = 600f;
		num -= (float)(PhotonNetwork.time - info.timestamp);
		UIGameManager.StartScoreTimer(num, StopTimer);
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
			GameManager.UpdateRoundState(RoundState.EndRound);
			UIMainStatus.Add("@", false, 5f, "Next Map");
			base.photonView.RPC("OnFinishRound", PhotonTargets.All);
		}
	}

	[PunRPC]
	private void OnFinishRound(PhotonMessageInfo info)
	{
		GameManager.LoadNextLevel(GameMode.BunnyHop);
	}

	public static void FinishMap(int money = 0, int xp = 0)
	{
		Transform transform = GameManager.GetTeamSpawn().GetTransform();
		transform.position = instance.StartSpawnPosition;
		transform.rotation = instance.StartSpawnRotation;
		UIMainStatus.Add(PhotonNetwork.player.NickName + " @", false, 5f, "Finished map");
		if (money != 0)
		{
			PlayerRoundManager.SetMoney(money);
			UIToast.Show("+" + money + " " + Localization.Get("Money"));
		}
		PlayerRoundManager.SetXP(xp);
		GameManager.GetController().SpawnPlayer(GameManager.GetTeamSpawn().GetSpawnPosition(), Vector3.up * Random.Range(0, 360));
		PhotonNetwork.player.SetKills1();
		++GameManager.BlueScore;
		UIGameManager.UpdateScoreLabel(0, GameManager.BlueScore, GameManager.RedScore);
	}

	public static void SpawnDead()
	{
		instance.OnSpawnPlayer();
	}
}
