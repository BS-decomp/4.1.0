using System;
using System.Runtime.InteropServices;
using Photon;
using UnityEngine;

public class ControllerManager : PunBehaviour, IPunObservable
{
	private Transform PlayerTransform;

	[NonSerialized]
	public PlayerInput PlayerInput;

	[NonSerialized]
	public PlayerSkin PlayerSkin;

	private Vector3 PlayerPositionWidth = new Vector3(0f, -0.08f, 0f);

	private int falsePositives;

	private void Awake()
	{
		PhotonClassesManager.Add(this);
		base.name = base.photonView.owner.NickName;
		if (base.photonView.isMine)
		{
			PlayerTransform = Utils.AddChild(GameSettings.instance.PlayerController, base.transform).transform;
			PlayerInput = PlayerTransform.GetComponent<PlayerInput>();
		}
		else
		{
			PlayerTransform = Utils.AddChild(GameSettings.instance.PlayerSkin, base.transform).transform;
			PlayerSkin = PlayerTransform.GetComponent<PlayerSkin>();
		}
	}

	public override void OnPhotonPlayerConnected(PhotonPlayer playerConnect)
	{
		if (base.photonView.isMine)
		{
			bool activeSelf = PlayerInput.gameObject.activeSelf;
			Team playerTeam = PlayerInput.PlayerTeam;
			int num = AccountManager.GetPlayerSkinSelected();
			if (playerTeam == Team.Red && (bool)PlayerInput.Zombie)
			{
				num = 99;
			}
			int num2 = PlayerInput.PlayerWeapon.GetSelectedWeaponData().ID;
			int weaponSkinSelected = AccountManager.GetWeaponSkinSelected(num2);
			int num3 = -1;
			if (AccountManager.GetFireStat(num2, weaponSkinSelected))
			{
				num3 = AccountManager.GetFireStatCounter(num2, weaponSkinSelected);
			}
			byte[] array = Utils.SerializeWeaponStickers(num2, weaponSkinSelected);
			base.photonView.RPC("PhotonConnected", playerConnect, activeSelf, (byte)num, (byte)playerTeam, (byte)num2, (byte)weaponSkinSelected, num3, array);
		}
	}

	[PunRPC]
	private void PhotonConnected(bool activePlayer, byte playerSkin, byte team, byte weaponID, byte weaponSkin, int fireStat, byte[] stickers)
	{
		PlayerSkin.PlayerTeam = (Team)team;
		PlayerSkin.PlayerSkinID = playerSkin;
		if (activePlayer)
		{
			PhotonActivePlayer(Vector3.zero, Vector3.zero);
			PlayerSkin.SetWeapon(WeaponManager.GetWeaponData(weaponID), weaponSkin, fireStat, stickers);
		}
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.isWriting)
		{
			float num = PlayerInput.MoveAxis.magnitude;
			if (PlayerInput.MoveAxis.y < 0f)
			{
				num = 0f - num;
			}
			stream.SendNext(PlayerTransform.position + PlayerPositionWidth);
			stream.SendNext(PlayerTransform.rotation);
			stream.SendNext(num);
			stream.SendNext(PlayerInput.mCharacterController.isGrounded);
			stream.SendNext(PlayerInput.RotateCamera);
			stream.SendNext((short)(int)PlayerInput.Health);
		}
		else
		{
			PlayerSkin.PhotonPosition = (Vector3)stream.ReceiveNext();
			PlayerSkin.PhotonRotation = (Quaternion)stream.ReceiveNext();
			PlayerSkin.SetMove((float)stream.ReceiveNext());
			PlayerSkin.SetGrounded((bool)stream.ReceiveNext());
			PlayerSkin.SetRotate((float)stream.ReceiveNext());
			PlayerSkin.Health = (short)stream.ReceiveNext();
		}
	}

	public void SetWeapon(int weaponID)
	{
		int weaponSkinSelected = AccountManager.GetWeaponSkinSelected(weaponID);
		int num = -1;
		if (AccountManager.GetFireStat(weaponID, weaponSkinSelected))
		{
			num = AccountManager.GetFireStatCounter(weaponID, weaponSkinSelected);
		}
		byte[] array = Utils.SerializeWeaponStickers(weaponID, weaponSkinSelected);
		base.photonView.RPC("PhotonSetWeapon", PhotonTargets.Others, (byte)weaponID, (byte)weaponSkinSelected, num, array);
	}

	[PunRPC]
	private void PhotonSetWeapon(byte weaponID, byte skinID, int firestat, byte[] stickers)
	{
		PlayerSkin.SetWeapon(WeaponManager.GetWeaponData(weaponID), skinID, firestat, stickers);
	}

	public void FireWeapon(DecalInfo decalInfo)
	{
		base.photonView.RPC("PhotonFireWeapon", PhotonTargets.All, decalInfo);
	}

	[PunRPC]
	private void PhotonFireWeapon(DecalInfo decalInfo)
	{
		if (!base.photonView.isMine)
		{
			PlayerSkin.Fire();
		}
		DecalsManager.FireWeapon(decalInfo);
	}

	public void ReloadWeapon()
	{
		base.photonView.RPC("PhotonReloadWeapon", PhotonTargets.Others);
	}

	[PunRPC]
	public void PhotonReloadWeapon()
	{
		PlayerSkin.Reload();
	}

	public void ActivePlayer(Vector3 pos, Vector3 rot)
	{
		if (base.photonView.isMine)
		{
			PlayerInput.FPController.Activate();
			PlayerInput.FPController.Stop();
			PlayerInput.FPController.SetPosition(pos);
			PlayerInput.FPCamera.SetRotation(rot);
			PhotonNetwork.player.SetDead(false);
		}
		base.photonView.RPC("PhotonActivePlayer", PhotonTargets.Others, pos, rot);
	}

	[PunRPC]
	private void PhotonActivePlayer(Vector3 pos, Vector3 rot)
	{
		if (!base.photonView.isMine)
		{
			PlayerTransform.gameObject.SetActive(true);
			PlayerSkin.PlayerRagdoll.Deactive();
			PlayerSkin.SetPosition(pos);
			PlayerSkin.SetRotation(rot);
			PlayerSkin.Dead = false;
			PlayerSkin.StartDamageTime();
		}
	}

	public void DeactivePlayer([Optional] Vector3 force, bool headshot = false)
	{
		if (base.photonView.isMine)
		{
			PlayerInput.FPController.Deactivate();
			PhotonNetwork.player.SetDead(true);
		}
		base.photonView.RPC("PhotonDeactivePlayer", PhotonTargets.Others, force, headshot);
	}

	[PunRPC]
	public void PhotonDeactivePlayer(Vector3 force, bool headshot)
	{
		if (!base.photonView.isMine)
		{
			PlayerSkin.PlayerRagdoll.Active(force, headshot);
			PlayerSkin.Dead = true;
		}
	}

	public void SetPosition(Vector3 position)
	{
		base.photonView.RPC("PhotonSetPosition", PhotonTargets.All, position);
	}

	[PunRPC]
	private void PhotonSetPosition(Vector3 position)
	{
		if (base.photonView.isMine)
		{
			PlayerInput.FPController.Stop();
			PlayerInput.FPController.SetPosition(position);
		}
		else
		{
			PlayerSkin.SetPosition(position);
		}
	}

	public void SetRotation(Vector3 rotation)
	{
		base.photonView.RPC("PhotonSetRotation", PhotonTargets.All, rotation);
	}

	[PunRPC]
	private void PhotonSetRotation(Vector3 rotation)
	{
		if (base.photonView.isMine)
		{
			PlayerInput.FPCamera.SetRotation(rotation);
		}
		else
		{
			PlayerSkin.SetRotation(rotation);
		}
	}

	public void SpawnPlayer(Vector3 position, Vector3 rotation)
	{
		base.photonView.RPC("PhotonSpawnPlayer", PhotonTargets.All, position, rotation);
	}

	[PunRPC]
	private void PhotonSpawnPlayer(Vector3 position, Vector3 rotation)
	{
		if (base.photonView.isMine)
		{
			PlayerInput.FPController.Stop();
			PlayerInput.FPController.SetPosition(position);
			PlayerInput.FPCamera.SetRotation(rotation);
		}
		else
		{
			PlayerSkin.SetPosition(position);
			PlayerSkin.SetRotation(rotation);
		}
	}

	public void Damage(DamageInfo damageInfo)
	{
		base.photonView.RPC("PhotonDamage", base.photonView.owner, damageInfo);
	}

	[PunRPC]
	private void PhotonDamage(DamageInfo damageInfo, PhotonMessageInfo info)
	{
		if (info.timestamp + 0.6000000238418579 > PhotonNetwork.time)
		{
			PlayerInput.Damage(damageInfo);
			if (falsePositives >= 0)
			{
				falsePositives--;
			}
		}
		else
		{
			falsePositives++;
			if (falsePositives >= 10)
			{
				PhotonNetwork.LeaveRoom();
			}
		}
	}

	public void SetTeam(Team team)
	{
		base.photonView.owner.SetTeam(team);
		int num = AccountManager.GetPlayerSkinSelected();
		if (team == Team.Red && (bool)PlayerInput.Zombie)
		{
			num = 99;
		}
		base.photonView.RPC("PhotonSetTeam", PhotonTargets.All, (byte)team, (byte)num);
	}

	[PunRPC]
	private void PhotonSetTeam(byte team, byte skin)
	{
		if (base.photonView.isMine)
		{
			PlayerInput.PlayerTeam = (Team)team;
			return;
		}
		PlayerSkin.PlayerTeam = (Team)team;
		PlayerSkin.PlayerSkinID = skin;
		PlayerSkin.UpdateSkin();
	}

	public void PlayerSize(float size)
	{
		base.photonView.RPC("PhotonPlayerSize", PhotonTargets.All, size);
	}

	[PunRPC]
	private void PhotonPlayerSize(float size)
	{
		if (!base.photonView.isMine)
		{
			base.transform.localScale = Vector3.one * size;
		}
	}

	public void UpdateFireStatValue(int weaponID)
	{
		base.photonView.RPC("PhotonUpdateFireStatValue", PhotonTargets.Others, (byte)weaponID);
	}

	[PunRPC]
	private void PhotonUpdateFireStatValue(byte weaponID)
	{
		if (PlayerSkin.SelectWeapon.WeaponID == weaponID)
		{
			PlayerSkin.SelectWeapon.UpdateFireStat1();
		}
	}
}
