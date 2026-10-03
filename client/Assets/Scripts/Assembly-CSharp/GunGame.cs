using Photon;
using UnityEngine;

public class GunGame : PunBehaviour
{
	public CryptoInt MaxScore = 100;

	private int PlayerKills;

	private int[] Weapons = new int[31]
	{
		3, 27, 13, 36, 6, 2, 21, 37, 9, 25,
		26, 14, 24, 12, 7, 18, 29, 19, 28, 1,
		5, 15, 8, 30, 23, 38, 11, 10, 16, 4,
		22
	};

	private int SelectWeaponIndex;

	private WeaponData SelectWeapon;

	private void Awake()
	{
		if (PhotonNetwork.offlineMode)
		{
			Object.Destroy(this);
		}
		else if (PhotonNetwork.room.GetGameMode() != GameMode.GunGame)
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
		GameManager.UpdateRoundState(RoundState.PlayRound);
		CameraManager.ActiveStaticCamera();
		UIGameManager.SetActiveScore(true, MaxScore);
		WeaponManager.SetSelectWeapon(WeaponType.Knife, 0);
		WeaponManager.SetSelectWeapon(WeaponType.Pistol, 3);
		WeaponManager.SetSelectWeapon(WeaponType.Rifle, 0);
		GameManager.MaxScore = MaxScore;
		GameManager.SetChangeWeapons(false);
		GameManager.StartAutoBalance();
		SelectWeapon = WeaponManager.GetWeaponData(3);
		UISelectTeam.OnStart();
		EventManager.AddListener<Team>("SelectTeam", OnSelectTeam);
		EventManager.AddListener<DamageInfo>("DeadPlayer", OnDeadPlayer);
		EventManager.AddListener<Team>("AutoBalance", OnAutoBalance);
	}

	private void OnSelectTeam(Team team)
	{
		UIPanelManager.ShowPanel("Display");
		OnRevivalPlayer();
	}

	private void OnAutoBalance(Team team)
	{
		GameManager.UpdatePlayerTeam(team);
		OnRevivalPlayer();
	}

	private void OnRevivalPlayer()
	{
		PlayerInput playerInput = GameManager.GetController().PlayerInput;
		playerInput.SetHealth(100);
		CameraManager.DeactiveAll();
		GameManager.GetController().ActivePlayer(GameManager.GetTeamSpawn().GetSpawnPosition(), GameManager.GetTeamSpawn().GetSpawnRotation());
		playerInput.PlayerWeapon.UpdateWeaponAll(SelectWeapon.Type);
		if (SelectWeapon.Type != WeaponType.Knife)
		{
			TimerManager.In(0.1f, () =>
			{
				PlayerWeapons.PlayerWeaponData weaponData = playerInput.PlayerWeapon.GetWeaponData(SelectWeapon.Type);
				weaponData.AmmoMax = (int)weaponData.AmmoMax * 2;
				UIGameManager.SetAmmoLabel(playerInput.PlayerWeapon.GetSelectedWeaponData().Ammo, playerInput.PlayerWeapon.GetSelectedWeaponData().AmmoMax);
			});
		}
	}

	private void OnUpdateWeapon()
	{
		if (SelectWeaponIndex >= Weapons.Length - 1)
		{
			SelectWeaponIndex = 0;
		}
		else
		{
			SelectWeaponIndex++;
		}
		WeaponManager.SetSelectWeapon(WeaponType.Knife, 0);
		WeaponManager.SetSelectWeapon(WeaponType.Pistol, 0);
		WeaponManager.SetSelectWeapon(WeaponType.Rifle, 0);
		SelectWeapon = WeaponManager.GetWeaponData(Weapons[SelectWeaponIndex]);
		UIToast.Show(SelectWeapon.Name);
		switch (SelectWeapon.Type)
		{
		case WeaponType.Knife:
			WeaponManager.SetSelectWeapon(WeaponType.Knife, SelectWeapon.ID);
			break;
		case WeaponType.Pistol:
			WeaponManager.SetSelectWeapon(WeaponType.Pistol, SelectWeapon.ID);
			break;
		case WeaponType.Rifle:
			WeaponManager.SetSelectWeapon(WeaponType.Rifle, SelectWeapon.ID);
			break;
		}
		PlayerInput playerInput = GameManager.GetController().PlayerInput;
		if ((bool)playerInput.Dead)
		{
			return;
		}
		playerInput.PlayerWeapon.CanFire = false;
		TimerManager.In(0.2f, () =>
		{
			if (!playerInput.Dead)
			{
				playerInput.PlayerWeapon.UpdateWeaponAll(SelectWeapon.Type);
				TimerManager.In(0.1f, () =>
				{
					playerInput.PlayerWeapon.CanFire = true;
					if (SelectWeapon.Type != WeaponType.Knife)
					{
						PlayerWeapons.PlayerWeaponData weaponData = playerInput.PlayerWeapon.GetWeaponData(SelectWeapon.Type);
						weaponData.AmmoMax = (int)weaponData.AmmoMax * 2;
						UIGameManager.SetAmmoLabel(playerInput.PlayerWeapon.GetSelectedWeaponData().Ammo, playerInput.PlayerWeapon.GetSelectedWeaponData().AmmoMax);
					}
				});
			}
			else
			{
				playerInput.PlayerWeapon.CanFire = true;
			}
		});
	}

	private void OnDeadPlayer(DamageInfo damageInfo)
	{
		PhotonNetwork.player.SetDeaths1();
		PlayerRoundManager.SetDeaths1();
		if (damageInfo.PlayerID != -1)
		{
			UIStatus.Add(Utils.KillerStatus(damageInfo));
			OnScore(damageInfo.AttackerTeam);
			UIDeathScreen.Show(damageInfo);
		}
		Vector3 ragdollForce = Utils.GetRagdollForce(GameManager.GetController().PlayerInput.PlayerTransform.position, damageInfo.AttackPosition);
		CameraManager.ActiveDeadCamera(GameManager.GetController().PlayerInput.FPCamera.Transform.position, GameManager.GetController().PlayerInput.FPCamera.Transform.eulerAngles, ragdollForce * 100f);
		GameManager.GetController().DeactivePlayer(ragdollForce, damageInfo.HeadShot);
		base.photonView.RPC("OnKilledPlayer", PhotonPlayer.Find(damageInfo.PlayerID), damageInfo);
		TimerManager.In(3f, () =>
		{
			OnRevivalPlayer();
		});
	}

	public override void OnPhotonPlayerConnected(PhotonPlayer playerConnect)
	{
		if (PhotonNetwork.isMasterClient)
		{
			GameManager.UpdateScore(playerConnect);
		}
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
			PlayerRoundManager.SetXP(10);
			PlayerRoundManager.SetMoney(6);
			PlayerRoundManager.SetHeadshot1();
		}
		else
		{
			PlayerRoundManager.SetXP(5);
			PlayerRoundManager.SetMoney(3);
		}
		if (PlayerKills >= 1 || (PlayerKills >= 0 && SelectWeapon.Type == WeaponType.Knife))
		{
			PlayerKills = 0;
			OnUpdateWeapon();
		}
		else
		{
			PlayerKills++;
		}
	}

	public void OnScore(Team team)
	{
		base.photonView.RPC("PhotonOnScore", PhotonTargets.MasterClient, (int)team);
	}

	[PunRPC]
	private void PhotonOnScore(int intTeam)
	{
		switch ((Team)intTeam)
		{
		case Team.Blue:
			++GameManager.BlueScore;
			break;
		case Team.Red:
			++GameManager.RedScore;
			break;
		}
		GameManager.UpdateScore();
		if (GameManager.CheckScore())
		{
			GameManager.UpdateRoundState(RoundState.EndRound);
			if (GameManager.WinTeam() == Team.Blue)
			{
				UIMainStatus.Add("@", false, 5f, "Blue Win");
			}
			else if (GameManager.WinTeam() == Team.Red)
			{
				UIMainStatus.Add("@", false, 5f, "Red Win");
			}
			base.photonView.RPC("PhotonNextLevel", PhotonTargets.All);
		}
	}

	[PunRPC]
	private void PhotonNextLevel()
	{
		GameManager.LoadNextLevel(GameMode.GunGame);
	}
}
