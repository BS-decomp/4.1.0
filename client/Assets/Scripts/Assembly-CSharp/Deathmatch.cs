using Photon;
using UnityEngine;

public class Deathmatch : Photon.MonoBehaviour
{
	public CryptoInt MaxScore = 50;

	private CryptoInt BlueScore = 0;

	private CryptoInt RedScore = 0;

	private void Awake()
	{
		if (PhotonNetwork.offlineMode)
		{
			Object.Destroy(this);
		}
		else if (PhotonNetwork.room.GetGameMode() != GameMode.Deathmatch)
		{
			Object.Destroy(this);
		}
	}

	private void Start()
	{
		GameManager.UpdateRoundState(RoundState.PlayRound);
		GameManager.SetStartDamageTime(2f);
		GameManager.SetFriendDamage(true);
		UIGameManager.SetActiveScore(true, MaxScore);
		GameManager.MaxScore = MaxScore;
		UIPanelManager.ShowPanel("Display");
		CameraManager.ActiveStaticCamera();
		TimerManager.In(0.5f, () =>
		{
			GameManager.OnSelectTeam(Team.Blue);
			OnRevivalPlayer();
		});
		EventManager.AddListener<DamageInfo>("DeadPlayer", OnDeadPlayer);
	}

	private void OnRevivalPlayer()
	{
		PlayerInput playerInput = GameManager.GetController().PlayerInput;
		playerInput.SetHealth(100);
		CameraManager.DeactiveAll();
		DrawElements randomSpawn = GameManager.GetRandomSpawn();
		GameManager.GetController().ActivePlayer(randomSpawn.GetSpawnPosition(), randomSpawn.GetSpawnRotation());
		playerInput.PlayerWeapon.UpdateWeaponAll(WeaponType.Rifle);
	}

	private void OnDeadPlayer(DamageInfo damageInfo)
	{
		PhotonNetwork.player.SetDeaths1();
		PlayerRoundManager.SetDeaths1();
		++RedScore;
		UIGameManager.UpdateScoreLabel(MaxScore, BlueScore, RedScore);
		if (damageInfo.PlayerID != -1)
		{
			UIStatus.Add(Utils.KillerStatus(damageInfo));
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

	[PunRPC]
	private void OnKilledPlayer(DamageInfo damageInfo)
	{
		EventManager.Dispatch("KillPlayer", damageInfo);
		PhotonNetwork.player.SetKills1();
		PlayerRoundManager.SetKills1();
		++BlueScore;
		UIGameManager.UpdateScoreLabel(MaxScore, BlueScore, RedScore);
		if ((int)BlueScore >= (int)MaxScore)
		{
			OnScore(PhotonNetwork.player);
		}
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
	}

	public void OnScore(PhotonPlayer player)
	{
		base.photonView.RPC("PhotonOnScore", PhotonTargets.MasterClient, player.ID);
	}

	[PunRPC]
	private void PhotonOnScore(int playerID)
	{
		if (GameManager.GetRoundState() == RoundState.PlayRound)
		{
			PhotonPlayer photonPlayer = PhotonPlayer.Find(playerID);
			GameManager.UpdateRoundState(RoundState.EndRound);
			UIMainStatus.Add(photonPlayer.NickName + " @", false, 5f, "Win");
			base.photonView.RPC("PhotonNextLevel", PhotonTargets.All);
		}
	}

	[PunRPC]
	private void PhotonNextLevel()
	{
		GameManager.LoadNextLevel(GameMode.Deathmatch);
	}
}
