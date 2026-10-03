using UnityEngine;

public class TPWeaponShooter : TimerBehaviour
{
	[Header("Weapon")]
	public int WeaponID;

	public int WeaponSkin;

	public MeshAtlas[] WeaponAtlas;

	public bool isMuzzle = true;

	public Transform Muzzle;

	[Header("FireStat")]
	public bool FireStat;

	public GameObject FireStatModel;

	public MeshAtlas[] FireStatCounters;

	private int FireStatValue = -1;

	[Header("Stickers")]
	public MeshAtlas[] Stickers;

	[Header("Others")]
	public bool CanSound = true;

	private PlayerSkin PlayerSkin;

	private WeaponSound FireSound;

	private WeaponSound ReloadSound;

	[Header("Two Handed Weapon")]
	public bool TwoHandedWeapon;

	public GameObject TwoWeapon;

	public Transform TwoMuzzle;

	public Vector3 TwoWeaponPosition;

	public Vector3 TwoWeaponRotation;

	private bool isFireTwoWeapon;

	private Transform TwoWeaponRoot;

	private GameObject cachedGameObject;

	public void Init(int weaponID, int weaponSkin, PlayerSkin playerSkin)
	{
		cachedGameObject = base.gameObject;
		PlayerSkin = playerSkin;
		WeaponID = weaponID;
		WeaponSkin = weaponSkin;
		FireSound = WeaponManager.GetWeaponData(weaponID).FireSound;
		ReloadSound = WeaponManager.GetWeaponData(weaponID).ReloadSound;
		UpdateSkin();
		SetTwoWeapon();
	}

	public void Active()
	{
		cachedGameObject.SetActive(true);
		if (TwoHandedWeapon)
		{
			TwoWeapon.SetActive(true);
		}
	}

	public void Deactive()
	{
		cachedGameObject.SetActive(false);
		if (TwoHandedWeapon)
		{
			TwoWeapon.SetActive(false);
		}
	}

	public void Fire(bool isVisible)
	{
		if (isVisible && isMuzzle)
		{
			if (TwoHandedWeapon && isFireTwoWeapon)
			{
				TwoMuzzle.gameObject.SetActive(true);
				base.addTimer = TimerManager.In(0.05f, () =>
				{
					TwoMuzzle.gameObject.SetActive(false);
				});
			}
			else
			{
				Muzzle.gameObject.SetActive(true);
				base.addTimer = TimerManager.In(0.05f, () =>
				{
					Muzzle.gameObject.SetActive(false);
				});
			}
			if (TwoHandedWeapon)
			{
				isFireTwoWeapon = !isFireTwoWeapon;
			}
		}
		if (CanSound)
		{
			PlayerSkin.Sounds.Play(FireSound);
		}
	}

	public void Reload()
	{
		if (CanSound)
		{
			PlayerSkin.Sounds.Play(ReloadSound, 0.2f);
		}
	}

	private void UpdateSkin()
	{
		string text = WeaponID + "-" + WeaponSkin;
		for (int i = 0; i < WeaponAtlas.Length; i++)
		{
			if (WeaponAtlas[i].spriteName != text)
			{
				MeshAtlas meshAtlas = WeaponAtlas[i];
				meshAtlas.spriteName = text;
			}
		}
	}

	public void SetFireStat(int firestat)
	{
		if (firestat >= -1)
		{
			FireStat = true;
			FireStatModel.SetActive(true);
			UpdateFireStat(firestat);
		}
	}

	public void UpdateFireStat1()
	{
		if (FireStat)
		{
			UpdateFireStat(FireStatValue + 1);
		}
	}

	public void UpdateFireStat(int counter)
	{
		if (FireStat && FireStatValue != counter)
		{
			FireStatValue = counter;
			string text = counter.ToString("D6");
			for (int i = 0; i < FireStatCounters.Length; i++)
			{
				FireStatCounters[i].spriteName = "f" + text[i];
			}
		}
	}

	public void SetTwoWeapon()
	{
		if (TwoHandedWeapon)
		{
			TwoWeaponRoot = PlayerSkin.PlayerTwoWeaponRoot;
			TwoWeapon.transform.SetParent(TwoWeaponRoot);
			TwoWeapon.transform.localPosition = TwoWeaponPosition;
			TwoWeapon.transform.localEulerAngles = TwoWeaponRotation;
		}
	}

	public void SetStickers(byte[] stickers)
	{
		int num = 0;
		for (int i = 0; i < stickers.Length / 2; i++)
		{
			Stickers[stickers[num] - 1].cachedGameObject.SetActive(true);
			Stickers[stickers[num] - 1].spriteName = stickers[num + 1].ToString();
			num += 2;
		}
	}
}
