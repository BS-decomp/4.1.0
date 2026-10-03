using System;
using System.Collections.Generic;
using Photon;
using UnityEngine;

public class mg_100TrapsManager : PunBehaviour
{
	[Serializable]
	public struct LevelData
	{
		public GameObject Map;

		public Transform Spawn;
	}

	public CryptoInt SelectLevel = 1;

	public List<LevelData> Levels;

	private void Awake()
	{
		PhotonClassesManager.Add(this);
	}

	private void Start()
	{
		GameManager.UpdateRoundState(RoundState.PlayRound);
		UIGameManager.SetActiveScore(true, 0);
		GameManager.SetStartDamageTime(0.1f);
		UIPanelManager.ShowPanel("Display");
		GameManager.SetChangeWeapons(false);
		CameraManager.ActiveStaticCamera();
		TimerManager.In(0.5f, () =>
		{
			GameManager.OnSelectTeam(Team.Blue);
			GameManager.GetController().PlayerInput.FPController.MotorDoubleJump = true;
			OnRevivalPlayer();
			UIGameManager.UpdateScoreLabel(Levels.Count, GameManager.BlueScore, GameManager.RedScore);
		});
		EventManager.AddListener<DamageInfo>("DeadPlayer", OnDeadPlayer);
	}

	private void OnSpawnPlayer()
	{
		GameManager.GetController().SpawnPlayer(Levels[(int)SelectLevel - 1].Spawn.position, Levels[(int)SelectLevel - 1].Spawn.eulerAngles);
	}

	private void OnRevivalPlayer()
	{
		WeaponManager.SetSelectWeapon(WeaponType.Pistol, 0);
		WeaponManager.SetSelectWeapon(WeaponType.Rifle, 0);
		PlayerInput playerInput = GameManager.GetController().PlayerInput;
		playerInput.SetHealth(100);
		CameraManager.DeactiveAll();
		GameManager.GetController().ActivePlayer(Levels[(int)SelectLevel - 1].Spawn.position, Levels[(int)SelectLevel - 1].Spawn.eulerAngles);
		playerInput.PlayerWeapon.UpdateWeaponAll(WeaponType.Knife);
	}

	private void OnDeadPlayer(DamageInfo damageInfo)
	{
		PhotonNetwork.player.SetDeaths1();
		++GameManager.RedScore;
		UIGameManager.UpdateScoreLabel(Levels.Count, GameManager.BlueScore, GameManager.RedScore);
		OnSpawnPlayer();
	}

	public override void OnPhotonPlayerConnected(PhotonPlayer playerConnect)
	{
		if (PhotonNetwork.isMasterClient && UIGameManager.instance.isScoreTimer)
		{
			base.photonView.RPC("UpdateTimer", playerConnect, UIGameManager.instance.ScoreTimer - Time.time);
		}
	}

	public void NextLevel()
	{
		++SelectLevel;
		if (Levels.Count + 1 <= (int)SelectLevel)
		{
			SelectLevel = 1;
			UIMainStatus.Add(PhotonNetwork.player.NickName + " @", false, 5f, "Finished map");
		}
		Levels[(int)SelectLevel - 1].Map.SetActive(true);
		OnSpawnPlayer();
		PhotonNetwork.player.SetKills(SelectLevel);
		GameManager.BlueScore = SelectLevel;
		PlayerRoundManager.SetXP(2);
		PlayerRoundManager.SetMoney(5);
		UIGameManager.UpdateScoreLabel(Levels.Count, GameManager.BlueScore, GameManager.RedScore);
		if ((int)SelectLevel == 1)
		{
			Levels[Levels.Count - 1].Map.SetActive(false);
		}
		else
		{
			Levels[(int)SelectLevel - 2].Map.SetActive(false);
		}
	}
}
