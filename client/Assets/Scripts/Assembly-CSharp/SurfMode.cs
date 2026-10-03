using Photon;
using UnityEngine;

public class SurfMode : PunBehaviour
{
	private Vector3 StartSpawnPosition;

	private Quaternion StartSpawnRotation;

	private static SurfMode instance;

	private void Awake()
	{
		if (PhotonNetwork.offlineMode)
		{
			Object.Destroy(this);
		}
		else if (PhotonNetwork.room.GetGameMode() != GameMode.Surf)
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
		GameManager.SetStartDamageTime(1f);
		UIPanelManager.ShowPanel("Display");
		GameManager.SetChangeWeapons(false);
		CameraManager.ActiveStaticCamera();
		TimerManager.In(0.5f, () =>
		{
			GameManager.OnSelectTeam(Team.Blue);
			StartSpawnPosition = GameManager.GetTeamSpawn(Team.Blue).GetTransform().position;
			StartSpawnRotation = GameManager.GetTeamSpawn(Team.Blue).GetTransform().rotation;
			PlayerInput playerInput = GameManager.GetController().PlayerInput;
			playerInput.SurfEnabled = true;
			playerInput.FPCamera.camera.farClipPlane = 300f;
			playerInput.FPController.MotorAirSpeed = 0.13f;
			playerInput.FPController.PhysicsGravityModifier = 0.15f;
			playerInput.FPController.PhysicsForceDamping = 0.045f;
			playerInput.FPController.PhysicsSlopeSlideLimit = 90f;
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
		GameManager.GetController().PlayerInput.StopSurf();
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
		playerInput.StopSurf();
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
		float num = 3600f;
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
		GameManager.LoadNextLevel(GameMode.Surf);
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
		GameManager.GetController().PlayerInput.StopSurf();
		PhotonNetwork.player.SetKills1();
		++GameManager.BlueScore;
		UIGameManager.UpdateScoreLabel(0, GameManager.BlueScore, GameManager.RedScore);
	}

	public static void SpawnDead()
	{
		instance.OnSpawnPlayer();
	}
}
