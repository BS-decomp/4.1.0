using System;
using System.Collections.Generic;
using System.Text;
using CodeStage.AntiCheat.ObscuredTypes;
using Photon;
using UnityEngine;

public class mStoreWeapons : PunBehaviour
{
	public enum SelectPanel
	{
		Weapon = 0,
		Skins = 1,
		Stickers = 2
	}

	[Header("Weapon Data")]
	public GameObject WeaponDataPanel;

	public UILabel WeaponNameLabel;

	public UILabel DamageLabel;

	public UIProgressBar DamageProgressBar;

	public UILabel FireRateLabel;

	public UIProgressBar FireRateProgressBar;

	public UILabel AccuracyLabel;

	public UIProgressBar AccuracyProgressBar;

	public UILabel AmmoLabel;

	public UIProgressBar AmmoProgressBar;

	public UILabel MaxAmmoLabel;

	public UIProgressBar MaxAmmoProgressBar;

	public UILabel MobilityLabel;

	public UIProgressBar MobilityProgressBar;

	public UILabel RangeLabel;

	public UIProgressBar RangeProgressBar;

	[Space(10f)]
	public ObscuredFloat MaxDamage;

	public ObscuredFloat MaxFireRate;

	public ObscuredFloat MaxAccuracy;

	public ObscuredFloat MaxAmmo;

	public ObscuredFloat MaxMaxAmmo;

	public ObscuredFloat MaxMobility;

	public ObscuredFloat MaxRange;

	[Header("Weapon Panel")]
	public UISprite SelectWeaponButton;

	public UILabel SelectWeaponButtonLabel;

	public GameObject WeaponBuyButton;

	public UILabel WeaponBuyButtonLabel;

	public UITexture WeaponBuyButtonTexture;

	public GameObject WeaponUpgradeButton;

	public UILabel WeaponUpgradeButtonLabel;

	public UIGrid WeaponButtonsGrid;

	public UILabel FireStatLabel;

	private List<string> WeaponsList = new List<string>();

	private int Weapon;

	private WeaponType WeaponType;

	private WeaponData WeaponData;

	private WeaponStoreData WeaponStoreData;

	private int WeaponUpgrade;

	private int WeaponSkin;

	[Header("Weapon Skin")]
	public Color NormalColor = Color.gray;

	public Color BaseColor = Color.cyan;

	public Color ProfessionalColor = Color.red;

	public Color LegendaryColor = Color.magenta;

	public UILabel SkinNameLabel;

	public UILabel SkinRarityLabel;

	public GameObject SkinBackground;

	public UISprite SelectSkinButton;

	public UILabel SelectSkinButtonLabel;

	public GameObject SkinBuyButton;

	public UILabel SkinBuyButtonLabel;

	public GameObject SkinDropInCase;

	public UIGrid SkinButtonsGrid;

	[Header("Weapon Stickers")]
	public GameObject StickersButton;

	public UIPopupList StickersPopupList;

	public UILabel StickerName;

	public UISprite SelectStickerSprite;

	public UILabel SelectStickerCountLabel;

	public UISprite DeleteStickerSprite;

	public UILabel StickersListLabel;

	private int WeaponStickersIndexCount;

	private int[] StickersIds;

	private int SelectedSticker;

	[Header("Others")]
	public GameObject WeaponPanel;

	public GameObject SkinPanel;

	public GameObject StickersPanel;

	public GameObject InAppPanel;

	public Texture2D MoneyTexture;

	public Texture2D GoldTexture;

	public GameObject ShareButton;

	private SelectPanel SelectedPanel;

	public float LimitSkinBackgroundColor = 100f;

	private bool Active;

	private void Start()
	{
		PhotonClassesManager.Add(this);
		UIEventListener uIEventListener = UIEventListener.Get(SkinBackground);
		uIEventListener.onDrag = RotateWeapon;
	}

	public override void OnDisconnectedFromPhoton()
	{
		Close();
	}

	private void RotateWeapon(GameObject go, Vector2 drag)
	{
		mWeaponCamera.Rotate(drag, SelectedPanel == SelectPanel.Weapon);
	}

	public void Show(int weaponType)
	{
		Active = true;
		SkinBackground.SetActive(true);
		Weapon = 0;
		WeaponType = (WeaponType)weaponType;
		GetWeaponList();
		UpdateWeapon();
	}

	public void Close()
	{
		if (Active)
		{
			SkinBackground.SetActive(false);
			mPanelManager.SetActivePlayerData(true);
			TweenPosition component = SkinNameLabel.GetComponent<TweenPosition>();
			if (component != null)
			{
				component.enabled = false;
			}
			Vector3 localPosition = SkinNameLabel.cachedTransform.localPosition;
			localPosition.y = 150f;
			SkinNameLabel.cachedTransform.localPosition = localPosition;
			mWeaponCamera.ResetRotateX(false);
			mWeaponCamera.SetViewportRect(new Rect(0.35f, 0.08f, 1f, 0.728f), 0f);
			mWeaponCamera.Close();
			SelectedPanel = SelectPanel.Weapon;
			TweenAlpha component2 = SkinPanel.GetComponent<TweenAlpha>();
			if (component2 != null)
			{
				component2.enabled = false;
			}
			component2 = WeaponPanel.GetComponent<TweenAlpha>();
			if (component2 != null)
			{
				component2.enabled = false;
			}
			SkinPanel.GetComponent<UIPanel>().alpha = 0f;
			WeaponPanel.GetComponent<UIPanel>().alpha = 1f;
			Active = false;
		}
	}

	public void UpdateAccount()
	{
		AccountManager.UpdateDefaultAndWeaponsData();
	}

	public void NextWeapon()
	{
		if (SelectedPanel == SelectPanel.Weapon)
		{
			Weapon++;
			if (Weapon >= WeaponsList.Count)
			{
				Weapon = 0;
			}
			UpdateWeapon();
		}
	}

	public void LastWeapon()
	{
		if (SelectedPanel == SelectPanel.Weapon)
		{
			Weapon--;
			if (Weapon <= -1)
			{
				Weapon = WeaponsList.Count - 1;
			}
			UpdateWeapon();
		}
	}

	private void UpdateWeapon()
	{
		GetWeaponData();
		GetWeaponStoreData();
		WeaponUpgrade = AccountManager.GetWeaponUpgrade(WeaponData.ID);
		WeaponSkin = AccountManager.GetWeaponSkinSelected(WeaponData.ID);
		mWeaponCamera.SetViewportRect(new Rect(0.35f, 0.08f, 1f, 0.728f), 0f);
		mWeaponCamera.Show(WeaponsList[Weapon]);
		bool flag = AccountManager.GetWeapon(WeaponData.ID);
		if ((int)WeaponStoreData.Price == 0)
		{
			flag = true;
		}
		if (flag)
		{
			SelectedWeaponPanel();
		}
		else
		{
			BuyWeaponPanel();
		}
		StickersButton.SetActive(mWeaponCamera.HasStickers());
		UpdateWeaponData();
		UpdateWeaponSkin();
	}

	private void SelectedWeaponPanel()
	{
		SelectWeaponButton.cachedGameObject.SetActive(true);
		if (AccountManager.GetWeaponSelected(WeaponType) == (int)WeaponData.ID)
		{
			SelectWeaponButtonLabel.text = Localization.Get("Selected");
			SelectWeaponButton.alpha = 0.5f;
		}
		else
		{
			SelectWeaponButtonLabel.text = Localization.Get("Select");
			SelectWeaponButton.alpha = 1f;
		}
		WeaponBuyButton.SetActive(false);
		if (WeaponUpgrade == 3 || WeaponStoreData.Upgrades.Count == 0)
		{
			WeaponUpgradeButton.SetActive(false);
		}
		else
		{
			WeaponUpgradeButton.SetActive(true);
			WeaponUpgradeButtonLabel.text = WeaponStoreData.Upgrades[WeaponUpgrade].Price.ToString();
		}
		WeaponButtonsGrid.repositionNow = true;
	}

	public void SelectWeapon()
	{
		if (AccountManager.GetWeaponSelected(WeaponType) != (int)WeaponData.ID)
		{
			AccountManager.SetWeaponSelected(WeaponType, WeaponData.ID);
			SelectedWeaponPanel();
			WeaponManager.UpdateData();
			UpdateWeaponSkin();
		}
	}

	public void UpgradeWeapon()
	{
		if ((int)WeaponStoreData.Upgrades[WeaponUpgrade].Price > AccountManager.GetGold())
		{
			InAppPanel.SetActive(true);
			UIToast.Show(Localization.Get("Not enough money"));
			return;
		}
		AccountManager.SetGold(AccountManager.GetGold() - (int)WeaponStoreData.Upgrades[WeaponUpgrade].Price);
		AccountManager.SetWeaponUpgrade(WeaponData.ID);
		WeaponUpgrade = AccountManager.GetWeaponUpgrade(WeaponData.ID);
		SelectedWeaponPanel();
		UpdateWeaponData();
		EventManager.Dispatch("AccountUpdate");
	}

	private void BuyWeaponPanel()
	{
		SelectWeaponButton.cachedGameObject.SetActive(false);
		SelectWeaponButton.alpha = 1f;
		WeaponUpgradeButton.SetActive(false);
		WeaponBuyButton.SetActive(true);
		WeaponBuyButtonLabel.text = WeaponStoreData.Price.ToString("n0");
		WeaponBuyButtonTexture.mainTexture = ((WeaponStoreData.Currency != GameCurrency.Gold) ? MoneyTexture : GoldTexture);
		WeaponButtonsGrid.repositionNow = true;
	}

	public void BuyWeapon()
	{
		if (!AccountManager.isConnect)
		{
			UIToast.Show(Localization.Get("Connection account"));
			return;
		}
		int num = ((WeaponStoreData.Currency != GameCurrency.Gold) ? AccountManager.GetMoney() : AccountManager.GetGold());
		if ((int)WeaponStoreData.Price > num)
		{
			InAppPanel.SetActive(true);
			UIToast.Show(Localization.Get("Not enough money"));
			return;
		}
		if (WeaponStoreData.Currency == GameCurrency.Gold)
		{
			AccountManager.SetGold(num - (int)WeaponStoreData.Price);
		}
		else
		{
			AccountManager.SetMoney(num - (int)WeaponStoreData.Price);
		}
		AccountManager.SetWeapon(WeaponData.ID);
		AccountManager.SetWeaponSelected(WeaponType, WeaponData.ID);
		SelectedWeaponPanel();
		EventManager.Dispatch("AccountUpdate");
		WeaponManager.UpdateData();
	}

	private void UpdateWeaponData()
	{
		WeaponDataPanel.SetActive(true);
		WeaponNameLabel.text = WeaponData.Name;
		SetDamage();
		SetFireRate();
		SetAccuracy();
		SetAmmo();
		SetMaxAmmo();
		SetMobility();
	}

	private void UpdateWeaponSkin()
	{
		SkinNameLabel.text = WeaponStoreData.Skins[WeaponSkin].Name;
		FireStatLabel.cachedGameObject.SetActive(AccountManager.GetFireStat(WeaponData.ID, WeaponStoreData.Skins[WeaponSkin].ID));
		if (FireStatLabel.cachedGameObject.activeSelf)
		{
			FireStatLabel.text = "FireStat: " + AccountManager.GetFireStatCounter(WeaponData.ID, WeaponStoreData.Skins[WeaponSkin].ID).ToString("D6");
		}
		mWeaponCamera.SetSkin(WeaponData.ID, WeaponStoreData.Skins[WeaponSkin].ID);
		if (AccountManager.GetWeaponSkin(WeaponData.ID, WeaponStoreData.Skins[WeaponSkin].ID))
		{
			SelectSkinButton.cachedGameObject.SetActive(true);
			if (AccountManager.GetWeaponSkinSelected(WeaponData.ID) == (int)WeaponStoreData.Skins[WeaponSkin].ID)
			{
				SelectSkinButtonLabel.text = Localization.Get("Selected");
				SelectSkinButton.alpha = 0.5f;
			}
			else
			{
				SelectSkinButtonLabel.text = Localization.Get("Select");
				SelectSkinButton.alpha = 1f;
			}
			SkinDropInCase.SetActive(false);
			SkinBuyButton.SetActive(false);
			SkinButtonsGrid.repositionNow = true;
		}
		else
		{
			if ((int)WeaponStoreData.Skins[WeaponSkin].Price == 0)
			{
				SkinDropInCase.SetActive(true);
				SkinBuyButton.SetActive(false);
			}
			else
			{
				SkinDropInCase.SetActive(false);
				SkinBuyButton.SetActive(true);
				SkinBuyButtonLabel.text = WeaponStoreData.Skins[WeaponSkin].Price.ToString();
			}
			SelectSkinButton.cachedGameObject.SetActive(false);
			SkinButtonsGrid.repositionNow = true;
		}
		switch (WeaponStoreData.Skins[WeaponSkin].Quality)
		{
		case WeaponSkinQuality.Default:
			SkinRarityLabel.text = Localization.Get("Normal quality");
			TweenColor.Begin(SkinBackground, 0.5f, NormalColor);
			break;
		case WeaponSkinQuality.Normal:
			SkinRarityLabel.text = Localization.Get("Normal quality");
			TweenColor.Begin(SkinBackground, 0.5f, NormalColor);
			break;
		case WeaponSkinQuality.Basic:
			SkinRarityLabel.text = Localization.Get("Basic quality");
			TweenColor.Begin(SkinBackground, 0.5f, BaseColor);
			break;
		case WeaponSkinQuality.Professional:
			SkinRarityLabel.text = Localization.Get("Professional quality");
			TweenColor.Begin(SkinBackground, 0.5f, ProfessionalColor);
			break;
		case WeaponSkinQuality.Legendary:
			SkinRarityLabel.text = Localization.Get("Legendary quality");
			TweenColor.Begin(SkinBackground, 0.5f, LegendaryColor);
			break;
		}
		UpdateWeaponStickers();
	}

	public void NextSkin()
	{
		if (SelectedPanel != SelectPanel.Skins)
		{
			return;
		}
		if ((bool)WeaponData.Secret)
		{
			List<int> list = new List<int>();
			for (int i = 0; i < WeaponStoreData.Skins.Count; i++)
			{
				if (AccountManager.GetWeaponSkin(WeaponData.ID, WeaponStoreData.Skins[i].ID))
				{
					list.Add(i);
				}
			}
			for (int j = 0; j < list.Count; j++)
			{
				if (WeaponSkin == list[j])
				{
					if (WeaponSkin == list[list.Count - 1])
					{
						WeaponSkin = list[0];
					}
					else
					{
						WeaponSkin = list[j + 1];
					}
					break;
				}
			}
		}
		else
		{
			WeaponSkin++;
			if (WeaponSkin >= WeaponStoreData.Skins.Count)
			{
				WeaponSkin = 0;
			}
		}
		UpdateWeaponSkin();
	}

	public void LastSkin()
	{
		if (SelectedPanel != SelectPanel.Skins)
		{
			return;
		}
		if ((bool)WeaponData.Secret)
		{
			List<int> list = new List<int>();
			for (int i = 0; i < WeaponStoreData.Skins.Count; i++)
			{
				if (AccountManager.GetWeaponSkin(WeaponData.ID, WeaponStoreData.Skins[i].ID))
				{
					list.Add(i);
				}
			}
			for (int j = 0; j < list.Count; j++)
			{
				if (WeaponSkin == list[j])
				{
					if (WeaponSkin == list[0])
					{
						WeaponSkin = list[list.Count - 1];
					}
					else
					{
						WeaponSkin = list[j - 1];
					}
					break;
				}
			}
		}
		else
		{
			WeaponSkin--;
			if (WeaponSkin < 0)
			{
				WeaponSkin = WeaponStoreData.Skins.Count - 1;
			}
		}
		UpdateWeaponSkin();
	}

	public void SelectSkin()
	{
		if (SelectedPanel == SelectPanel.Skins)
		{
			AccountManager.SetWeaponSkinSelected(WeaponData.ID, WeaponStoreData.Skins[WeaponSkin].ID);
			UpdateWeaponSkin();
		}
	}

	public void BuySkin()
	{
		if (SelectedPanel == SelectPanel.Skins)
		{
			if (!AccountManager.isConnect)
			{
				UIToast.Show(Localization.Get("Connection account"));
				return;
			}
			if ((int)WeaponStoreData.Skins[WeaponSkin].Price > AccountManager.GetGold())
			{
				InAppPanel.SetActive(true);
				UIToast.Show(Localization.Get("Not enough money"));
				return;
			}
			AccountManager.SetGold(AccountManager.GetGold() - (int)WeaponStoreData.Skins[WeaponSkin].Price);
			AccountManager.SetWeaponSkin(WeaponData.ID, WeaponStoreData.Skins[WeaponSkin].ID);
			AccountManager.SetWeaponSkinSelected(WeaponData.ID, WeaponStoreData.Skins[WeaponSkin].ID);
			UpdateWeaponSkin();
			EventManager.Dispatch("AccountUpdate");
		}
	}

	private void UpdateWeaponStickers()
	{
		AccountWeaponStickers weaponStickers = AccountManager.GetWeaponStickers(WeaponData.ID, WeaponStoreData.Skins[WeaponSkin].ID);
		mWeaponCamera.SetStickers(weaponStickers);
	}

	private void UpdateSelectedSticker()
	{
		int pos = int.Parse(StickersPopupList.value);
		int weaponSticker = AccountManager.GetWeaponSticker(WeaponData.ID, WeaponStoreData.Skins[WeaponSkin].ID, pos);
		if (weaponSticker != -1)
		{
			StickerData stickerData = WeaponManager.GetStickerData(weaponSticker);
			mWeaponCamera.ActivePrevSticker(pos, stickerData.ID);
			SelectStickerSprite.cachedGameObject.SetActive(false);
			DeleteStickerSprite.cachedGameObject.SetActive(true);
			DeleteStickerSprite.spriteName = stickerData.ID.ToString();
			StickerName.text = stickerData.Name;
		}
		else if (StickersIds.Length > 0)
		{
			StickerData stickerData2 = WeaponManager.GetStickerData(StickersIds[SelectedSticker]);
			mWeaponCamera.ActivePrevSticker(pos, stickerData2.ID);
			SelectStickerSprite.cachedGameObject.SetActive(true);
			DeleteStickerSprite.cachedGameObject.SetActive(false);
			SelectStickerSprite.spriteName = stickerData2.ID.ToString();
			SelectStickerCountLabel.text = AccountManager.GetStickerCount(stickerData2.ID).ToString();
			StickerName.text = stickerData2.Name;
		}
		else
		{
			mWeaponCamera.DeactivePrevSticker();
			SelectStickerSprite.cachedGameObject.SetActive(false);
			DeleteStickerSprite.cachedGameObject.SetActive(false);
			StickerName.text = string.Empty;
		}
	}

	public void SetSticker()
	{
		if (SelectedPanel == SelectPanel.Stickers)
		{
			mPopUp.ShowPopup(Localization.Get("Do you really want to stick a sticker?"), Localization.Get("Stickers"), Localization.Get("No"), () =>
			{
				mPopUp.HideAll();
			}, Localization.Get("Yes"), () =>
			{
				mPopUp.HideAll();
				int pos = int.Parse(StickersPopupList.value);
				AccountManager.DeleteSticker(StickersIds[SelectedSticker]);
				AccountManager.SetWeaponSticker(WeaponData.ID, WeaponStoreData.Skins[WeaponSkin].ID, pos, StickersIds[SelectedSticker]);
				StickersIds = AccountManager.GetStickers();
				SelectedSticker = 0;
				UpdateSelectedSticker();
			});
		}
	}

	public void DeleteSticker()
	{
		mPopUp.ShowPopup(Localization.Get("Do you really want to remove the sticker? The sticker will be destroyed!"), Localization.Get("Delete"), Localization.Get("Yes"), () =>
		{
			mPopUp.HideAll();
			int pos = int.Parse(StickersPopupList.value);
			AccountManager.DeleteWeaponSticker(WeaponData.ID, WeaponStoreData.Skins[WeaponSkin].ID, pos);
			UpdateSelectedSticker();
		}, Localization.Get("No"), () =>
		{
			mPopUp.HideAll();
		});
	}

	public void NextSticker()
	{
		if (SelectedPanel == SelectPanel.Stickers)
		{
			SelectedSticker++;
			if (SelectedSticker >= StickersIds.Length)
			{
				SelectedSticker = 0;
			}
			UpdateSelectedSticker();
		}
	}

	public void LastSticker()
	{
		if (SelectedPanel == SelectPanel.Stickers)
		{
			SelectedSticker--;
			if (SelectedSticker <= -1)
			{
				SelectedSticker = StickersIds.Length - 1;
			}
			UpdateSelectedSticker();
		}
	}

	public void SelectStickerPosition()
	{
		if (SelectedPanel == SelectPanel.Stickers)
		{
			UpdateSelectedSticker();
		}
	}

	private void UpdateStickersList()
	{
		AccountWeaponStickers weaponStickers = AccountManager.GetWeaponStickers(WeaponData.ID, WeaponStoreData.Skins[WeaponSkin].ID);
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < WeaponStickersIndexCount; i++)
		{
			string weaponStickerText = GetWeaponStickerText(weaponStickers, i + 1);
			if (WeaponStickersIndexCount - 1 < i)
			{
				stringBuilder.Append(weaponStickerText);
			}
			else
			{
				stringBuilder.AppendLine(weaponStickerText);
			}
		}
		StickersListLabel.text = stringBuilder.ToString();
	}

	private string GetWeaponStickerText(AccountWeaponStickers stickers, int index)
	{
		for (int i = 0; i < stickers.StickerData.Count; i++)
		{
			if ((int)stickers.StickerData[i].Index == index)
			{
				return string.Concat(stickers.StickerData[i].Index, ": ", WeaponManager.GetStickerName(stickers.StickerData[i].StickerID));
			}
		}
		return index + ": -";
	}

	private Color GetStickerQualityColor(StickerQuality quality)
	{
		switch (quality)
		{
		case StickerQuality.Basic:
			return BaseColor;
		case StickerQuality.Professional:
			return ProfessionalColor;
		case StickerQuality.Legendary:
			return LegendaryColor;
		default:
			return Color.white;
		}
	}

	private void GetWeaponList()
	{
		WeaponsList.Clear();
		for (int i = 0; i < GameSettings.instance.Weapons.Count; i++)
		{
			if (GameSettings.instance.Weapons[i].Type != WeaponType)
			{
				continue;
			}
			int num = GameSettings.instance.Weapons[i].ID;
			if (num == 4 || num == 3 || num == 12)
			{
				WeaponsList.Insert(0, GameSettings.instance.Weapons[i].Name);
			}
			else
			{
				if ((bool)GameSettings.instance.Weapons[i].Lock)
				{
					continue;
				}
				if ((bool)GameSettings.instance.Weapons[i].Secret)
				{
					if (AccountManager.GetWeapon(GameSettings.instance.Weapons[i].ID))
					{
						WeaponsList.Add(GameSettings.instance.Weapons[i].Name);
					}
				}
				else
				{
					WeaponsList.Add(GameSettings.instance.Weapons[i].Name);
				}
			}
		}
	}

	private void GetWeaponData()
	{
		for (int i = 0; i < GameSettings.instance.Weapons.Count; i++)
		{
			if (GameSettings.instance.Weapons[i].Name == WeaponsList[Weapon])
			{
				WeaponData = GameSettings.instance.Weapons[i];
				break;
			}
		}
	}

	private void GetWeaponStoreData()
	{
		for (int i = 0; i < GameSettings.instance.Weapons.Count; i++)
		{
			if (GameSettings.instance.Weapons[i].Name == WeaponsList[Weapon])
			{
				WeaponStoreData = GameSettings.instance.WeaponsStore[i];
				break;
			}
		}
	}

	private void SetDamage()
	{
		float num = ((int)WeaponData.FaceDamage * (int)WeaponData.FireBullets + (int)WeaponData.BodyDamage * (int)WeaponData.FireBullets + (int)WeaponData.HandDamage * (int)WeaponData.FireBullets + (int)WeaponData.LegDamage * (int)WeaponData.FireBullets) / 4;
		float num2 = 0f;
		if (WeaponUpgrade != 0)
		{
			WeaponUpgradeData weaponUpgradeData = WeaponStoreData.Upgrades[WeaponUpgrade - 1];
			num2 = ((int)weaponUpgradeData.FaceDamage * (int)WeaponData.FireBullets + (int)weaponUpgradeData.BodyDamage * (int)WeaponData.FireBullets + (int)weaponUpgradeData.HandDamage * (int)WeaponData.FireBullets + (int)weaponUpgradeData.LegDamage * (int)WeaponData.FireBullets) / 4;
			num2 -= num;
			if (num2 != 0f)
			{
				DamageLabel.text = num + "[00Ff22] + " + num2;
				DamageProgressBar.value = (num + num2) / (float)MaxDamage;
			}
			else
			{
				DamageLabel.text = num.ToString();
				DamageProgressBar.value = num / (float)MaxDamage;
			}
		}
		else if ((bool)WeaponData.CanFire)
		{
			DamageLabel.text = num.ToString();
			DamageProgressBar.value = num / (float)MaxDamage;
		}
		else
		{
			DamageLabel.text = "-";
			DamageProgressBar.value = 0f;
		}
	}

	private void SetFireRate()
	{
		if (WeaponData.Type == WeaponType.Knife || !WeaponData.CanFire)
		{
			FireRateLabel.text = "-";
			FireRateProgressBar.value = 0f;
			return;
		}
		int num = Mathf.FloorToInt(100f - (float)WeaponData.FireRate * 100f / 1.5f);
		int num2 = 0;
		if (WeaponUpgrade != 0)
		{
			WeaponUpgradeData weaponUpgradeData = WeaponStoreData.Upgrades[WeaponUpgrade - 1];
			num2 = Mathf.FloorToInt(100f - (float)weaponUpgradeData.FireRate * 100f / 1.5f);
			num2 -= num;
			if (num2 != 0)
			{
				FireRateLabel.text = num + "[00Ff22] + " + num2;
				FireRateProgressBar.value = (float)(num + num2) / (float)MaxFireRate;
			}
			else
			{
				FireRateLabel.text = num.ToString();
				FireRateProgressBar.value = (float)num / (float)MaxFireRate;
			}
		}
		else
		{
			FireRateLabel.text = num.ToString();
			FireRateProgressBar.value = (float)num / (float)MaxFireRate;
		}
	}

	private void SetAccuracy()
	{
		if (WeaponData.Type == WeaponType.Knife || !WeaponData.CanFire)
		{
			AccuracyLabel.text = "-";
			AccuracyProgressBar.value = 0f;
			return;
		}
		int num = Mathf.FloorToInt(100f - (float)WeaponData.FireAccuracy - (float)WeaponData.Accuracy);
		int num2 = 0;
		if (WeaponUpgrade != 0)
		{
			WeaponUpgradeData weaponUpgradeData = WeaponStoreData.Upgrades[WeaponUpgrade - 1];
			num2 = Mathf.FloorToInt(100f - (float)weaponUpgradeData.FireAccuracy - (float)weaponUpgradeData.Accuracy);
			num2 -= num;
			if (num2 != 0)
			{
				AccuracyLabel.text = num + "[00Ff22] + " + num2;
				AccuracyProgressBar.value = (float)(num + num2) / (float)MaxAccuracy;
			}
			else
			{
				AccuracyLabel.text = num.ToString();
				AccuracyProgressBar.value = (float)num / (float)MaxAccuracy;
			}
		}
		else
		{
			AccuracyLabel.text = num.ToString();
			AccuracyProgressBar.value = (float)num / (float)MaxAccuracy;
		}
	}

	private void SetAmmo()
	{
		if (WeaponData.Type == WeaponType.Knife || !WeaponData.CanFire)
		{
			AmmoLabel.text = "-";
			AmmoProgressBar.value = 0f;
			return;
		}
		int num = WeaponData.Ammo;
		int num2 = 0;
		if (WeaponUpgrade != 0)
		{
			WeaponUpgradeData weaponUpgradeData = WeaponStoreData.Upgrades[WeaponUpgrade - 1];
			num2 = weaponUpgradeData.Ammo;
			num2 -= num;
			if (num2 != 0)
			{
				AmmoLabel.text = num + "[00Ff22] + " + num2;
				AmmoProgressBar.value = (float)(num + num2) / (float)MaxAmmo;
			}
			else
			{
				AmmoLabel.text = num.ToString();
				AmmoProgressBar.value = (float)num / (float)MaxAmmo;
			}
		}
		else
		{
			AmmoLabel.text = num.ToString();
			AmmoProgressBar.value = (float)num / (float)MaxAmmo;
		}
	}

	private void SetMaxAmmo()
	{
		if (WeaponData.Type == WeaponType.Knife || !WeaponData.CanFire)
		{
			MaxAmmoLabel.text = "-";
			MaxAmmoProgressBar.value = 0f;
			return;
		}
		int num = WeaponData.MaxAmmo;
		int num2 = 0;
		if (WeaponUpgrade != 0)
		{
			WeaponUpgradeData weaponUpgradeData = WeaponStoreData.Upgrades[WeaponUpgrade - 1];
			num2 = weaponUpgradeData.MaxAmmo;
			num2 -= num;
			if (num2 != 0)
			{
				MaxAmmoLabel.text = num + "[00Ff22] + " + num2;
				MaxAmmoProgressBar.value = (float)(num + num2) / (float)MaxMaxAmmo;
			}
			else
			{
				MaxAmmoLabel.text = num.ToString();
				MaxAmmoProgressBar.value = (float)num / (float)MaxMaxAmmo;
			}
		}
		else
		{
			MaxAmmoLabel.text = num.ToString();
			MaxAmmoProgressBar.value = (float)num / (float)MaxMaxAmmo;
		}
	}

	private void SetMobility()
	{
		int num = Mathf.FloorToInt(100f - (float)WeaponData.Mass * 1000f);
		int num2 = 0;
		if (WeaponUpgrade != 0)
		{
			WeaponUpgradeData weaponUpgradeData = WeaponStoreData.Upgrades[WeaponUpgrade - 1];
			num2 = Mathf.FloorToInt(100f - (float)weaponUpgradeData.Mass * 1000f);
			num2 -= num;
			if (num2 != 0)
			{
				MobilityLabel.text = num + "[00Ff22] + " + num2;
				MobilityProgressBar.value = (float)(num + num2) / (float)MaxMobility;
			}
			else
			{
				MobilityLabel.text = num.ToString();
				MobilityProgressBar.value = (float)num / (float)MaxMobility;
			}
		}
		else if (!WeaponData.CanFire)
		{
			MobilityLabel.text = "-";
		}
		else
		{
			MobilityLabel.text = num.ToString();
			MobilityProgressBar.value = (float)num / (float)MaxMobility;
		}
	}

	public void ShowStickersPanel()
	{
		if (SelectedPanel == SelectPanel.Weapon)
		{
			mPanelManager.SetActivePlayerData(false);
			Vector3 localPosition = SkinNameLabel.cachedTransform.localPosition;
			localPosition.y = 214f;
			TweenPosition.Begin(SkinNameLabel.cachedGameObject, 0.7f, localPosition);
			mWeaponCamera.SetViewportRect(new Rect(0f, 0f, 1f, 1f), 1f);
			SelectedPanel = SelectPanel.Stickers;
			TweenAlpha.Begin(WeaponPanel, 0.5f, 0f);
			TweenAlpha.Begin(StickersPanel, 0.7f, 1f);
			StickersIds = AccountManager.GetStickers();
			SelectedSticker = 0;
			WeaponStickersIndexCount = mWeaponCamera.GetStickersCount();
			StickersPopupList.Clear();
			for (int i = 0; i < WeaponStickersIndexCount; i++)
			{
				StickersPopupList.AddItem((i + 1).ToString());
			}
			StickersPopupList.value = "1";
		}
	}

	public void ShowSkinPanel()
	{
		if (SelectedPanel == SelectPanel.Weapon)
		{
			mPanelManager.SetActivePlayerData(false);
			Vector3 localPosition = SkinNameLabel.cachedTransform.localPosition;
			localPosition.y = 214f;
			TweenPosition.Begin(SkinNameLabel.cachedGameObject, 0.7f, localPosition);
			mWeaponCamera.SetViewportRect(new Rect(0f, 0f, 1f, 1f), 1f);
			SelectedPanel = SelectPanel.Skins;
			TweenAlpha.Begin(WeaponPanel, 0.5f, 0f);
			TweenAlpha.Begin(SkinPanel, 0.7f, 1f);
		}
	}

	public void ShowWeaponPanel()
	{
		mPanelManager.SetActivePlayerData(true);
		Vector3 localPosition = SkinNameLabel.cachedTransform.localPosition;
		localPosition.y = 150f;
		TweenPosition.Begin(SkinNameLabel.cachedGameObject, 0.7f, localPosition);
		mWeaponCamera.ResetRotateX(true);
		mWeaponCamera.SetViewportRect(new Rect(0.35f, 0.08f, 1f, 0.728f), 1f);
		if (SelectedPanel == SelectPanel.Skins)
		{
			TweenAlpha.Begin(SkinPanel, 0.5f, 0f);
		}
		else if (SelectedPanel == SelectPanel.Stickers)
		{
			TweenAlpha.Begin(StickersPanel, 0.5f, 0f);
			mWeaponCamera.DeactivePrevSticker();
		}
		TweenAlpha.Begin(WeaponPanel, 0.7f, 1f);
		SelectedPanel = SelectPanel.Weapon;
		WeaponSkin = AccountManager.GetWeaponSkinSelected(WeaponData.ID);
		UpdateWeaponSkin();
	}

	public void ShareScreenshot()
	{
		if (Application.platform == RuntimePlatform.Android)
		{
			SkinPanel.SetActive(false);
			string text = string.Concat(WeaponData.Name, " | ", WeaponStoreData.Skins[WeaponSkin].Name, "_", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
			AndroidNativeFunctions.TakeScreenshot(text, "Block Strike", FinishScreenshot);
		}
	}

	private void FinishScreenshot(string path)
	{
		SkinPanel.SetActive(true);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("#BlockStrike #BS");
		stringBuilder.AppendLine(string.Concat("My weapon ", WeaponData.Name, " | ", WeaponStoreData.Skins[WeaponSkin].Name, " in game Block Strike"));
		stringBuilder.AppendLine("http://bit.ly/blockstrike");
		AndroidNativeFunctions.ShareScreenshot(stringBuilder.ToString(), "Block Strike", Localization.Get("Share"), path);
	}
}
