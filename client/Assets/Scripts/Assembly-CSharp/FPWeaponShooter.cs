using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class FPWeaponShooter : MonoBehaviour
{
	[Serializable]
	public class ShowWeaponSettings
	{
		public float Duration = 0.5f;

		public Vector3 Position;

		public Vector3 Rotation;
	}

	[Header("Motion")]
	public Vector3 MotionPositionRecoil = new Vector3(0f, 0f, -0.035f);

	public Vector3 MotionRotationRecoil = new Vector3(-10f, 0f, 0f);

	[Header("Fire Reload Settings")]
	public bool FireReload;

	public float FireReloadDelay = 0.2f;

	public float FireReloadDuration = 0.5f;

	public int FireReloadForce = 60;

	public Vector3 FireReloadPosition;

	public Vector3 FireReloadRotation;

	[Header("Knife Settings")]
	public bool KnifeWeapon;

	public float KnifeDelay = 0.1f;

	public int KnifeDelayForce = 50;

	public Vector3 KnifeDelayForcePosition;

	public Vector3 KnifeDelayForceRotation;

	public float KnifeAttackTime = 0.2f;

	public int KnifeAttackForce = 50;

	public Vector3 KnifeAttackForcePosition;

	public Vector3 KnifeAttackForceRotation;

	[Header("Two Handed Weapon")]
	public bool TwoHandedWeapon;

	public FPWeaponTwoHanded OneHandWeapon;

	public FPWeaponTwoHanded TwoHandWeapon;

	public Transform TwoMuzzle;

	private bool isTwoHandWeapon;

	[Header("Show Settings")]
	public int ShowForce = 50;

	public Vector3 ShowPosition;

	public Vector3 ShowRotation;

	public float ShowDuration = 0.5f;

	public Vector3 ShowPosition2;

	public Vector3 ShowRotation2;

	public Transform ShowHandLeft;

	private float ShowHadLeftDefault;

	private Tweener ShowHandLeftTweener;

	public ShowWeaponSettings[] ShowWeaponList;

	[Disabled]
	public bool Show;

	[Header("FireStat")]
	public GameObject FireStatModel;

	public MeshAtlas[] FireStatCounters;

	[Disabled]
	public bool FireStat;

	[Header("Stickers")]
	public MeshAtlas[] Stickers;

	[Header("Bullet Effect")]
	public bool isBulletEffect = true;

	public ParticleSystem BulletEffect;

	[Header("Others")]
	public vp_FPWeapon FPWeapon;

	public bool SparkEffect = true;

	public Transform Muzzle;

	public MeshAtlas[] WeaponAtlas;

	public MeshAtlas[] HandsAtlas;

	private void Start()
	{
		UpdateHandAtlas();
	}

	private void OnDisable()
	{
		StopShowWeapon();
	}

	public void Active()
	{
		if (!KnifeWeapon && SparkEffect)
		{
			SmokePlumeManager.SetParent(Muzzle.parent, Muzzle.localPosition);
			SparkEffectManager.SetParent(Muzzle.parent, Muzzle.localPosition);
		}
		FPWeapon.Activate();
		FPWeapon.Wield();
		if (TwoHandedWeapon)
		{
			OneHandWeapon.Wield();
			TwoHandWeapon.Wield();
		}
	}

	public void Deactive()
	{
		if (!KnifeWeapon && SparkEffect)
		{
			SmokePlumeManager.ClearParent();
			SparkEffectManager.ClearParent();
		}
		FPWeapon.Deactivate();
	}

	public void Reload(float duration)
	{
		if (!KnifeWeapon)
		{
			FPWeapon.SetState("Reload");
			TimerManager.In(duration, () =>
			{
				FPWeapon.SetState("Reload", false);
			});
		}
	}

	public void ShowWeapon()
	{
		if (!Show && ShowWeaponList.Length != 0)
		{
			Show = true;
			StartCoroutine(ShowWeaponCoroutine());
		}
	}

	private IEnumerator ShowWeaponCoroutine()
	{
		if (ShowHandLeft != null)
		{
			if (ShowHadLeftDefault == 0f)
			{
				ShowHadLeftDefault = ShowHandLeft.localPosition.y;
			}
			ShowHandLeftTweener = ShowHandLeft.DOLocalMoveY(-1f, 0.5f);
			TimerManager.In(0.5f, () =>
			{
				if (Show)
				{
					ShowHandLeft.gameObject.SetActive(false);
				}
			});
		}
		for (int i = 0; i < ShowWeaponList.Length; i++)
		{
			if (Show)
			{
				FPWeapon.StopSprings();
				if (TwoHandedWeapon)
				{
					OneHandWeapon.StopSprings();
					TwoHandWeapon.StopSprings();
				}
				FPWeapon.AddSoftForce(ShowWeaponList[i].Position, ShowWeaponList[i].Rotation, (int)(ShowWeaponList[i].Duration * 60f));
				yield return new WaitForSeconds(ShowWeaponList[i].Duration);
			}
		}
		if (ShowHandLeft != null)
		{
			yield return new WaitForSeconds(ShowWeaponList[ShowWeaponList.Length - 1].Duration * 0.15f + 0.2f);
			ShowHandLeft.gameObject.SetActive(true);
			ShowHandLeftTweener = ShowHandLeft.DOLocalMoveY(ShowHadLeftDefault, 0.2f);
			yield return new WaitForSeconds(0.2f);
			Show = false;
		}
		else
		{
			Show = false;
		}
	}

	public void StopShowWeapon()
	{
		if (!Show)
		{
			return;
		}
		Show = false;
		if (ShowHandLeft != null)
		{
			ShowHandLeft.gameObject.SetActive(true);
			if (ShowHandLeftTweener != null && ShowHandLeftTweener.IsActive())
			{
				ShowHandLeftTweener.Kill();
			}
			ShowHandLeft.localPosition = new Vector3(ShowHandLeft.localPosition.x, ShowHadLeftDefault, ShowHandLeft.localPosition.z);
		}
	}

	public void Fire()
	{
		if (Show)
		{
			FPWeapon.StopSprings();
			if (TwoHandedWeapon)
			{
				OneHandWeapon.StopSprings();
				TwoHandWeapon.StopSprings();
			}
			Show = false;
			if (ShowHandLeft != null)
			{
				ShowHandLeft.gameObject.SetActive(true);
				if (ShowHandLeftTweener != null && ShowHandLeftTweener.IsActive())
				{
					ShowHandLeftTweener.Kill();
				}
				ShowHandLeft.localPosition = new Vector3(ShowHandLeft.localPosition.x, ShowHadLeftDefault, ShowHandLeft.localPosition.z);
			}
		}
		if (KnifeWeapon)
		{
			FireKnife();
		}
		else
		{
			FireWeapon();
		}
	}

	private void FireWeapon()
	{
		if (!KnifeWeapon)
		{
			if (UnityEngine.Random.value > 0.2f)
			{
				if (TwoHandedWeapon && isTwoHandWeapon)
				{
					TwoMuzzle.localEulerAngles = new Vector3(TwoMuzzle.localEulerAngles.x, TwoMuzzle.localEulerAngles.y, UnityEngine.Random.value * 360f);
					TwoMuzzle.gameObject.SetActive(true);
					TimerManager.In(0.02f, () =>
					{
						TwoMuzzle.gameObject.SetActive(false);
					});
				}
				else
				{
					Muzzle.localEulerAngles = new Vector3(Muzzle.localEulerAngles.x, Muzzle.localEulerAngles.y, UnityEngine.Random.value * 360f);
					Muzzle.gameObject.SetActive(true);
					TimerManager.In(0.02f, () =>
					{
						Muzzle.gameObject.SetActive(false);
					});
				}
			}
			SmokePlumeManager.Fire();
			if (isBulletEffect && Settings.Shell)
			{
				BulletEffect.Emit(1);
			}
		}
		FPWeapon.ResetSprings(0.5f, 0.5f, 1f, 1f);
		if (MotionRotationRecoil.z == 0f)
		{
			if (TwoHandedWeapon)
			{
				if (isTwoHandWeapon)
				{
					TwoHandWeapon.AddForce2(MotionPositionRecoil, MotionRotationRecoil);
				}
				else
				{
					OneHandWeapon.AddForce2(MotionPositionRecoil, MotionRotationRecoil);
				}
				isTwoHandWeapon = !isTwoHandWeapon;
			}
			else
			{
				FPWeapon.AddForce2(MotionPositionRecoil, MotionRotationRecoil);
			}
		}
		else if (TwoHandedWeapon)
		{
			if (isTwoHandWeapon)
			{
				TwoHandWeapon.AddForce2(MotionPositionRecoil, Vector3.Scale(MotionRotationRecoil, Vector3.one + Vector3.back) + ((!(UnityEngine.Random.value < 0.5f)) ? Vector3.back : Vector3.forward) * UnityEngine.Random.Range(MotionRotationRecoil.z * 0.5f, MotionRotationRecoil.z));
			}
			else
			{
				OneHandWeapon.AddForce2(MotionPositionRecoil, Vector3.Scale(MotionRotationRecoil, Vector3.one + Vector3.back) + ((!(UnityEngine.Random.value < 0.5f)) ? Vector3.back : Vector3.forward) * UnityEngine.Random.Range(MotionRotationRecoil.z * 0.5f, MotionRotationRecoil.z));
			}
			isTwoHandWeapon = !isTwoHandWeapon;
		}
		else
		{
			FPWeapon.AddForce2(MotionPositionRecoil, Vector3.Scale(MotionRotationRecoil, Vector3.one + Vector3.back) + ((!(UnityEngine.Random.value < 0.5f)) ? Vector3.back : Vector3.forward) * UnityEngine.Random.Range(MotionRotationRecoil.z * 0.5f, MotionRotationRecoil.z));
		}
		if (!FireReload)
		{
			return;
		}
		TimerManager.In(FireReloadDelay, () =>
		{
			FPWeapon.AddSoftForce(FireReloadPosition, FireReloadRotation, FireReloadForce);
			TimerManager.In(FireReloadDuration, () =>
			{
				FPWeapon.StopSprings();
				if (TwoHandedWeapon)
				{
					OneHandWeapon.StopSprings();
					TwoHandWeapon.StopSprings();
				}
			});
		});
	}

	private void FireKnife()
	{
		if (KnifeDelay != 0f)
		{
			FPWeapon.AddSoftForce(KnifeDelayForcePosition, KnifeDelayForceRotation, KnifeDelayForce);
		}
		TimerManager.In(KnifeDelay, () =>
		{
			FPWeapon.StopSprings();
			FPWeapon.AddSoftForce(KnifeAttackForcePosition, KnifeAttackForceRotation, KnifeAttackForce);
			TimerManager.In(KnifeAttackTime, () =>
			{
				FPWeapon.StopSprings();
			});
		});
	}

	public void DryFire()
	{
		if (KnifeWeapon)
		{
			return;
		}
		if (TwoHandedWeapon)
		{
			if (isTwoHandWeapon)
			{
				TwoHandWeapon.AddForce2(MotionPositionRecoil * -0.1f, MotionRotationRecoil * -0.1f);
			}
			else
			{
				OneHandWeapon.AddForce2(MotionPositionRecoil * -0.1f, MotionRotationRecoil * -0.1f);
			}
			isTwoHandWeapon = !isTwoHandWeapon;
		}
		else
		{
			FPWeapon.AddForce2(MotionPositionRecoil * -0.1f, MotionRotationRecoil * -0.1f);
		}
	}

	public void ScopeRifle()
	{
		if (Show)
		{
			FPWeapon.StopSprings();
			StopShowWeapon();
		}
	}

	public void UpdateHandAtlas()
	{
		string text = (int)PlayerInput.instance.PlayerTeam + "-" + AccountManager.GetPlayerSkinSelected();
		for (int i = 0; i < HandsAtlas.Length; i++)
		{
			if (HandsAtlas[i].spriteName != text)
			{
				HandsAtlas[i].spriteName = text;
			}
		}
	}

	public void UpdateWeaponAtlas(int weaponID)
	{
		int weaponSkinSelected = AccountManager.GetWeaponSkinSelected(weaponID);
		for (int i = 0; i < WeaponAtlas.Length; i++)
		{
			if (WeaponAtlas[i].spriteName != weaponID + "-" + weaponSkinSelected)
			{
				WeaponAtlas[i].spriteName = weaponID + "-" + weaponSkinSelected;
			}
		}
		if (!KnifeWeapon && AccountManager.GetFireStat(weaponID, weaponSkinSelected))
		{
			FireStat = true;
			FireStatModel.SetActive(true);
			UpdateFireStat(AccountManager.GetFireStatCounter(weaponID, weaponSkinSelected));
		}
		UpdateStickers(weaponID, weaponSkinSelected);
	}

	public void UpdateFireStat(int counter)
	{
		if (FireStat)
		{
			string text = counter.ToString("D6");
			for (int i = 0; i < text.Length; i++)
			{
				FireStatCounters[i].spriteName = "f" + text[i];
			}
		}
	}

	private void UpdateStickers(int weaponID, int skin)
	{
		AccountWeaponStickers weaponStickers = AccountManager.GetWeaponStickers(weaponID, skin);
		if (weaponStickers != null)
		{
			for (int i = 0; i < weaponStickers.StickerData.Count; i++)
			{
				Stickers[(int)weaponStickers.StickerData[i].Index - 1].cachedGameObject.SetActive(true);
				Stickers[(int)weaponStickers.StickerData[i].Index - 1].spriteName = weaponStickers.StickerData[i].StickerID.ToString();
			}
		}
	}
}
