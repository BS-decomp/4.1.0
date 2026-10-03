using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PlayerWeapons : MonoBehaviour
{
	[Serializable]
	public class PlayerWeaponData
	{
		public bool Enabled;

		public CryptoInt ID;

		public CryptoBool CanFire;

		public CryptoString Name;

		public CryptoInt Damage;

		public CryptoFloat FireRate;

		public CryptoFloat Accuracy;

		public CryptoFloat FireAccuracy;

		public CryptoInt FireBullets;

		public CryptoFloat ReloadTime;

		public CryptoFloat Distance;

		public CryptoFloat Mass;

		public CryptoBool Scope;

		public CryptoInt ScopeSize;

		public CryptoFloat ScopeSensitivity;

		public CryptoFloat ScopeRecoil;

		public CryptoFloat ScopeAccuracy;

		public WeaponSound FireSound;

		public WeaponSound ReloadSound;

		public CryptoInt Ammo;

		public CryptoInt AmmoTotal;

		public CryptoInt AmmoMax;

		public float LastFire;

		public FPWeaponShooter Script;
	}

	public WeaponType SelectedWeapon;

	public bool CanFire = true;

	public bool isDebug;

	[Disabled]
	public bool isFire;

	[Disabled]
	public bool isScope;

	[Disabled]
	public bool isReload;

	private int ReloadTimeID;

	[Disabled]
	public bool Wielded;

	private int WieldedTimeID;

	[Disabled]
	public bool InfiniteAmmo;

	private bool isDryFire;

	private Vector2 FireAccuracy;

	private Ray FireRay;

	private RaycastHit FireRaycastHit;

	[Header("Weapons Data")]
	public PlayerWeaponData KnifeData = new PlayerWeaponData();

	public PlayerWeaponData PistolData = new PlayerWeaponData();

	public PlayerWeaponData RifleData = new PlayerWeaponData();

	private bool isUpdateWeaponData;

	[Header("RigidBody")]
	public bool PushRigidbody;

	public float PushRigidbodyForce = 1000f;

	[Header("Sounds")]
	public PlayerSounds Sounds;

	[Header("Others")]
	public LayerMask FireLayers;

	public PlayerInput m_PlayerInput;

	public Camera PlayerCamera;

	public Camera WeaponCamera;

	private Dictionary<string, GameObject> WeaponObjects = new Dictionary<string, GameObject>();

	private void Start()
	{
		EventManager.AddListener<DamageInfo>("KillPlayer", KillPlayer);
	}

	private void OnEnable()
	{
		UICrosshair.SetActiveCrosshair(true);
		InputManager.GetButtonDownEvent += GetButtonDown;
		InputManager.GetButtonUpEvent += GetButtonUp;
	}

	private void OnDisable()
	{
		UICrosshair.SetActiveCrosshair(false);
		InputManager.GetButtonDownEvent -= GetButtonDown;
		InputManager.GetButtonUpEvent -= GetButtonUp;
		isReload = false;
		isFire = false;
		TimerManager.Cancel(ReloadTimeID);
		TimerManager.Cancel(WieldedTimeID);
	}

	private void GetButtonDown(string name)
	{
		switch (name)
		{
		case "Fire":
			isFire = true;
			break;
		case "Aim":
			ScopeWeapon(true);
			break;
		case "Reload":
			ReloadWeapon();
			break;
		case "Pause":
			DeactiveScope();
			break;
		case "SelectWeapon":
			UpdateSelectWeapon();
			break;
		}
	}

	private void GetButtonUp(string name)
	{
		if (name == "Fire")
		{
			isFire = false;
			isDryFire = false;
		}
	}

	private void Update()
	{
		if (isFire)
		{
			FireWeapon();
		}
	}

	private void UpdateSelectWeapon()
	{
		if (SelectedWeapon == WeaponType.Knife)
		{
			if (RifleData.Enabled)
			{
				SetWeapon(WeaponType.Rifle);
			}
			else if (PistolData.Enabled)
			{
				SetWeapon(WeaponType.Pistol);
			}
		}
		else if (SelectedWeapon == WeaponType.Pistol)
		{
			if (KnifeData.Enabled)
			{
				SetWeapon(WeaponType.Knife);
			}
			else if (RifleData.Enabled)
			{
				SetWeapon(WeaponType.Rifle);
			}
		}
		else if (SelectedWeapon == WeaponType.Rifle)
		{
			if (PistolData.Enabled)
			{
				SetWeapon(WeaponType.Pistol);
			}
			else if (KnifeData.Enabled)
			{
				SetWeapon(WeaponType.Knife);
			}
		}
	}

	public PlayerWeaponData GetSelectedWeaponData()
	{
		return GetWeaponData(SelectedWeapon);
	}

	public PlayerWeaponData GetWeaponData(WeaponType weapon)
	{
		switch (weapon)
		{
		case WeaponType.Knife:
			return KnifeData;
		case WeaponType.Pistol:
			return PistolData;
		case WeaponType.Rifle:
			return RifleData;
		default:
			return null;
		}
	}

	public void UpdateWeaponAll()
	{
		UpdateWeaponAll(WeaponManager.DefaultWeaponType);
	}

	public void UpdateWeaponAll(WeaponType defaultWeapon)
	{
		int knifeID = WeaponManager.GetSelectWeapon(WeaponType.Knife);
		int pistolID = WeaponManager.GetSelectWeapon(WeaponType.Pistol);
		int rifleID = WeaponManager.GetSelectWeapon(WeaponType.Rifle);
		TimerManager.In(0.03f, () =>
		{
			isUpdateWeaponData = true;
			DeactiveAll();
			WeaponManager.SetSelectWeapon(WeaponType.Knife, knifeID);
			WeaponManager.SetSelectWeapon(WeaponType.Pistol, pistolID);
			WeaponManager.SetSelectWeapon(WeaponType.Rifle, rifleID);
			UpdateWeaponData(WeaponType.Knife);
			UpdateWeaponData(WeaponType.Pistol);
			UpdateWeaponData(WeaponType.Rifle);
			TimerManager.In(0.05f, () =>
			{
				SetWeapon(defaultWeapon, false);
				isUpdateWeaponData = false;
			});
			byte b = 0;
			if (KnifeData.Enabled)
			{
				b++;
			}
			if (PistolData.Enabled)
			{
				b++;
			}
			if (RifleData.Enabled)
			{
				b++;
			}
			if (b >= 2)
			{
				UIControllerList.SelectWeapon.cachedGameObject.SetActive(true);
			}
			else
			{
				UIControllerList.SelectWeapon.cachedGameObject.SetActive(false);
			}
		});
	}

	public void UpdateWeaponData(WeaponType weaponType)
	{
		WeaponData weaponData = null;
		PlayerWeaponData playerWeaponData = new PlayerWeaponData();
		switch (weaponType)
		{
		case WeaponType.Knife:
			if (WeaponManager.HasSelectWeapon(WeaponType.Knife))
			{
				weaponData = WeaponManager.GetSelectWeaponData(WeaponType.Knife);
			}
			break;
		case WeaponType.Pistol:
			if (WeaponManager.HasSelectWeapon(WeaponType.Pistol))
			{
				weaponData = WeaponManager.GetSelectWeaponData(WeaponType.Pistol);
			}
			break;
		case WeaponType.Rifle:
			if (WeaponManager.HasSelectWeapon(WeaponType.Rifle))
			{
				weaponData = WeaponManager.GetSelectWeaponData(WeaponType.Rifle);
			}
			break;
		}
		if (weaponData != null && !WeaponObjects.ContainsKey(weaponData.Name) && weaponData.FpsPrefab != null)
		{
			GameObject fpsPrefab = weaponData.FpsPrefab;
			fpsPrefab = Utils.AddChild(fpsPrefab, m_PlayerInput.FPCamera.transform);
			WeaponObjects.Add(weaponData.Name, fpsPrefab);
			fpsPrefab.SetActive(true);
		}
		if (weaponData != null)
		{
			playerWeaponData.Enabled = true;
			playerWeaponData.ID = (int)weaponData.ID;
			playerWeaponData.CanFire = (bool)weaponData.CanFire;
			playerWeaponData.Name = (string)weaponData.Name;
			playerWeaponData.Damage = (int)weaponData.BodyDamage;
			int weaponUpgrade = AccountManager.GetWeaponUpgrade(playerWeaponData.ID);
			if (weaponUpgrade != 0)
			{
				WeaponUpgradeData weaponUpgradeData = GameSettings.instance.WeaponsStore[(int)playerWeaponData.ID - 1].Upgrades[weaponUpgrade - 1];
				playerWeaponData.FireRate = (float)weaponUpgradeData.FireRate;
				playerWeaponData.Accuracy = (float)weaponUpgradeData.Accuracy;
				playerWeaponData.FireAccuracy = (float)weaponUpgradeData.FireAccuracy;
				playerWeaponData.Ammo = (int)weaponUpgradeData.Ammo;
				playerWeaponData.AmmoTotal = (int)weaponUpgradeData.Ammo;
				playerWeaponData.AmmoMax = (int)weaponUpgradeData.MaxAmmo;
				playerWeaponData.Mass = (float)weaponUpgradeData.Mass;
			}
			else
			{
				playerWeaponData.FireRate = (float)weaponData.FireRate;
				playerWeaponData.Accuracy = (float)weaponData.Accuracy;
				playerWeaponData.FireAccuracy = (float)weaponData.FireAccuracy;
				playerWeaponData.Ammo = (int)weaponData.Ammo;
				playerWeaponData.AmmoTotal = (int)weaponData.Ammo;
				playerWeaponData.AmmoMax = (int)weaponData.MaxAmmo;
				playerWeaponData.Mass = (float)weaponData.Mass;
			}
			playerWeaponData.FireBullets = (int)weaponData.FireBullets;
			playerWeaponData.ReloadTime = (float)weaponData.ReloadTime;
			playerWeaponData.Distance = (float)weaponData.Distance;
			playerWeaponData.Scope = (bool)weaponData.Scope;
			playerWeaponData.ScopeSize = (int)weaponData.ScopeSize;
			playerWeaponData.ScopeSensitivity = (float)weaponData.ScopeSensitivity;
			playerWeaponData.ScopeRecoil = (float)weaponData.ScopeRecoil;
			playerWeaponData.ScopeAccuracy = (float)weaponData.ScopeAccuracy;
			playerWeaponData.FireSound = weaponData.FireSound;
			playerWeaponData.ReloadSound = weaponData.ReloadSound;
			playerWeaponData.LastFire = 0f;
			playerWeaponData.Script = WeaponObjects[weaponData.Name].GetComponent<FPWeaponShooter>();
			playerWeaponData.Script.UpdateHandAtlas();
			playerWeaponData.Script.UpdateWeaponAtlas(playerWeaponData.ID);
		}
		else
		{
			playerWeaponData.Enabled = false;
		}
		switch (weaponType)
		{
		case WeaponType.Knife:
			KnifeData = playerWeaponData;
			break;
		case WeaponType.Pistol:
			PistolData = playerWeaponData;
			break;
		case WeaponType.Rifle:
			RifleData = playerWeaponData;
			break;
		}
	}

	public void SetWeapon(WeaponType weapon, bool checkSelectedWeapon = true)
	{
		if ((!checkSelectedWeapon || SelectedWeapon != weapon) && GetWeaponData(weapon).Enabled)
		{
			SelectedWeapon = weapon;
			DeactiveScope();
			DeactiveAll();
			PlayerWeaponData selectedWeaponData = GetSelectedWeaponData();
			UICrosshair.SetAccuracy(selectedWeaponData.Accuracy);
			UIGameManager.SetAmmoLabel(selectedWeaponData.Ammo, selectedWeaponData.AmmoMax, InfiniteAmmo);
			UIControllerList.Aim.cachedGameObject.SetActive(selectedWeaponData.Scope);
			m_PlayerInput.SetPlayerSpeed(selectedWeaponData.Mass);
			TimerManager.Cancel(ReloadTimeID);
			isReload = false;
			Sounds.Stop();
			TimerManager.Cancel(WieldedTimeID);
			Wielded = true;
			WieldedTimeID = TimerManager.In(0.5f, () =>
			{
				Wielded = false;
			});
			switch (weapon)
			{
			case WeaponType.Knife:
				KnifeData.Script.Active();
				break;
			case WeaponType.Pistol:
				PistolData.Script.Active();
				break;
			case WeaponType.Rifle:
				RifleData.Script.Active();
				break;
			}
			m_PlayerInput.Controller.SetWeapon(selectedWeaponData.ID);
		}
	}

	private void DeactiveAll()
	{
		if (KnifeData.Script != null)
		{
			KnifeData.Script.Deactive();
		}
		if (PistolData.Script != null)
		{
			PistolData.Script.Deactive();
		}
		if (RifleData.Script != null)
		{
			RifleData.Script.Deactive();
		}
	}

	public void FireWeapon()
	{
		if (CanFire && !isReload && GameManager.GetRoundState() != RoundState.EndRound)
		{
			switch (SelectedWeapon)
			{
			case WeaponType.Knife:
				Fire(KnifeData);
				break;
			case WeaponType.Pistol:
				Fire(PistolData);
				break;
			case WeaponType.Rifle:
				Fire(RifleData);
				break;
			}
		}
	}

	private void Fire(PlayerWeaponData weapon)
	{
		if (Wielded || weapon.LastFire > Time.time || isUpdateWeaponData || !weapon.CanFire)
		{
			return;
		}
		if (weapon.Script.KnifeWeapon)
		{
			weapon.LastFire = Time.time + (float)weapon.FireRate;
			Sounds.Play(weapon.FireSound);
			weapon.Script.Fire();
			TimerManager.In(weapon.Script.KnifeDelay, () =>
			{
				FireData(weapon);
			});
		}
		else if ((int)weapon.Ammo > 0)
		{
			--weapon.Ammo;
			weapon.LastFire = Time.time + (float)weapon.FireRate;
			Sounds.Play(weapon.FireSound);
			weapon.Script.Fire();
			FireData(weapon);
			if (isScope && (float)weapon.ScopeRecoil != 0f)
			{
				m_PlayerInput.FPCamera.Pitch -= weapon.ScopeRecoil;
				float num = (float)weapon.ScopeRecoil / 2f;
				m_PlayerInput.FPCamera.Yaw += UnityEngine.Random.Range(0f - num, num);
			}
		}
		else if ((int)weapon.AmmoMax == 0)
		{
			DryFire(weapon);
		}
		else
		{
			Reload(weapon);
		}
	}

	private void FireData(PlayerWeaponData weapon)
	{
		UIGameManager.SetAmmoLabel(weapon.Ammo, weapon.AmmoMax, InfiniteAmmo);
		DecalInfo decalInfo = DecalInfo.Get();
		if (SelectedWeapon == WeaponType.Knife)
		{
			decalInfo.isKnife = true;
		}
		for (int i = 0; i < (int)weapon.FireBullets; i++)
		{
			if (isScope)
			{
				FireAccuracy = UICrosshair.Fire(weapon.ScopeAccuracy);
			}
			else
			{
				FireAccuracy = UICrosshair.Fire(weapon.FireAccuracy);
			}
			FireAccuracy.x = UnityEngine.Random.Range(0f - FireAccuracy.x, FireAccuracy.x);
			FireAccuracy.y = UnityEngine.Random.Range(0f - FireAccuracy.y, FireAccuracy.y);
			FireRay = PlayerCamera.ViewportPointToRay(new Vector3(0.5f + FireAccuracy.x, 0.5f + FireAccuracy.y, 0f));
			if (!Physics.Raycast(FireRay, out FireRaycastHit, weapon.Distance, FireLayers))
			{
				continue;
			}
			if (FireRaycastHit.GetComponent<Collider>().CompareTag("PlayerSkin"))
			{
				if (decalInfo.BloodDecal == 200)
				{
					decalInfo.BloodDecal = (byte)decalInfo.Points.Count;
					decalInfo.Points.Add(FireRaycastHit.point);
					decalInfo.Normals.Add(FireRaycastHit.normal);
				}
				DamageInfo value = DamageInfo.Get(weapon.Damage, m_PlayerInput.PlayerTransform.position, m_PlayerInput.PlayerTeam, weapon.ID, PhotonNetwork.player.ID, false);
				FireRaycastHit.transform.SendMessage("Damage", value, SendMessageOptions.DontRequireReceiver);
				if (!isScope)
				{
					SparkEffectManager.Fire(FireRaycastHit);
				}
			}
			else
			{
				if (FireRaycastHit.GetComponent<Collider>().CompareTag("IgnoreDecal"))
				{
					continue;
				}
				if (FireRaycastHit.GetComponent<Collider>().CompareTag("RigidbodyObject"))
				{
					if (PushRigidbody)
					{
						FireRaycastHit.transform.GetComponent<RigidbodyObject>().Force(PlayerCamera.transform.forward * PushRigidbodyForce);
					}
					continue;
				}
				if (FireRaycastHit.GetComponent<Collider>().CompareTag("DamageObject"))
				{
					DamageInfo value2 = DamageInfo.Get(weapon.Damage, m_PlayerInput.PlayerTransform.position, m_PlayerInput.PlayerTeam, weapon.ID, PhotonNetwork.player.ID, false);
					FireRaycastHit.transform.SendMessage("Damage", value2, SendMessageOptions.DontRequireReceiver);
					continue;
				}
				if (FireRaycastHit.GetComponent<Collider>().CompareTag("PaintObject"))
				{
					EventManager.Dispatch("Paint", FireRaycastHit);
					FireRaycastHit.transform.SendMessage("OnPaint", FireRaycastHit, SendMessageOptions.DontRequireReceiver);
					continue;
				}
				decalInfo.Points.Add(FireRaycastHit.point);
				decalInfo.Normals.Add(FireRaycastHit.normal);
				if (!isScope)
				{
					SparkEffectManager.Fire(FireRaycastHit);
				}
			}
		}
		m_PlayerInput.Controller.FireWeapon(decalInfo);
	}

	private void DryFire(PlayerWeaponData weapon)
	{
		if (!isDryFire)
		{
			isDryFire = true;
			weapon.Script.DryFire();
			Sounds.Play(WeaponSound.AmmoEmpty);
		}
	}

	private void ReloadWeapon()
	{
		if (isReload)
		{
			return;
		}
		switch (SelectedWeapon)
		{
		case WeaponType.Knife:
			if (KnifeData.Script != null)
			{
				KnifeData.Script.ShowWeapon();
			}
			break;
		case WeaponType.Pistol:
			Reload(PistolData);
			break;
		case WeaponType.Rifle:
			Reload(RifleData);
			break;
		}
	}

	private void Reload(PlayerWeaponData weapon)
	{
		if (isScope)
		{
			DeactiveScope();
		}
		if ((int)weapon.Ammo == (int)weapon.AmmoTotal || (int)weapon.AmmoMax == 0)
		{
			weapon.Script.ShowWeapon();
			return;
		}
		isReload = true;
		Sounds.Play(weapon.ReloadSound);
		weapon.Script.Reload(weapon.ReloadTime);
		m_PlayerInput.Controller.ReloadWeapon();
		ReloadTimeID = TimerManager.In((float)weapon.ReloadTime + 0.5f, () =>
		{
			isReload = false;
			if (InfiniteAmmo)
			{
				weapon.Ammo = weapon.AmmoTotal;
			}
			else if ((int)weapon.AmmoMax > (int)weapon.AmmoTotal)
			{
				PlayerWeaponData playerWeaponData = weapon;
				playerWeaponData.AmmoMax = (int)playerWeaponData.AmmoMax - ((int)weapon.AmmoTotal - (int)weapon.Ammo);
				weapon.Ammo = weapon.AmmoTotal;
			}
			else
			{
				int num = weapon.Ammo;
				PlayerWeaponData playerWeaponData2 = weapon;
				playerWeaponData2.Ammo = (int)playerWeaponData2.Ammo + (int)weapon.AmmoMax;
				weapon.Ammo = Mathf.Min(weapon.AmmoTotal, weapon.Ammo);
				PlayerWeaponData playerWeaponData3 = weapon;
				playerWeaponData3.AmmoMax = (int)playerWeaponData3.AmmoMax - ((int)weapon.Ammo - num);
				weapon.AmmoMax = Mathf.Max(0, weapon.AmmoMax);
			}
			UIGameManager.SetAmmoLabel(weapon.Ammo, weapon.AmmoMax, InfiniteAmmo);
		});
	}

	private void ScopeWeapon(bool check)
	{
		if ((!check || !isReload) && ((bool)GetSelectedWeaponData().Scope || !check))
		{
			isScope = !isScope;
			DOTween.Kill("Scope");
			if (isScope)
			{
				PlayerCamera.DOFieldOfView((int)GetSelectedWeaponData().ScopeSize, 0.2f).id = "Scope";
				WeaponCamera.fieldOfView = 1f;
			}
			else
			{
				DOTween.Kill("Scope");
				PlayerCamera.DOFieldOfView(60f, 0.2f).id = "Scope";
				WeaponCamera.fieldOfView = 60f;
			}
			UICrosshair.SetActiveScope(isScope);
			GetSelectedWeaponData().Script.ScopeRifle();
		}
	}

	public void DeactiveScope()
	{
		if (isScope)
		{
			ScopeWeapon(false);
		}
		if ((bool)m_PlayerInput.Dead)
		{
			UICrosshair.SetActiveCrosshair(false);
		}
	}

	private void KillPlayer(DamageInfo damageInfo)
	{
		PlayerWeaponData playerWeaponData = null;
		if (PistolData.Enabled && (int)PistolData.ID == damageInfo.WeaponID && PistolData.Script.FireStat)
		{
			playerWeaponData = PistolData;
		}
		else if (RifleData.Enabled && (int)RifleData.ID == damageInfo.WeaponID && RifleData.Script.FireStat)
		{
			playerWeaponData = RifleData;
		}
		if (playerWeaponData != null)
		{
			int num = AccountManager.GetFireStatCounter(damageInfo.WeaponID, damageInfo.WeaponSkinID) + 1;
			playerWeaponData.Script.UpdateFireStat(num);
			AccountManager.SetFireStatCounter(damageInfo.WeaponID, damageInfo.WeaponSkinID, num);
			m_PlayerInput.Controller.UpdateFireStatValue(playerWeaponData.ID);
		}
	}
}
