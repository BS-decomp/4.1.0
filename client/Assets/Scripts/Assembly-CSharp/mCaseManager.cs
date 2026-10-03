using System;
using System.Collections.Generic;
using System.Text;
using Photon;
using UnityEngine;

public class mCaseManager : PunBehaviour
{
	[Serializable]
	public class Case
	{
		public string Name;

		public CryptoBool PriceGold;

		public CryptoInt Price;

		public UILabel NameLabel;

		public UILabel InfoLabel;

		public CryptoBool Money;

		public CryptoInt Normal;

		public CryptoInt Base;

		public CryptoInt Professional;

		public CryptoInt Legendary;

		public CryptoInt FireStat;

		public CryptoInt SecretWeapon;
	}

	[Header("Cases")]
	public Case[] SkinCases;

	public Case[] StickerCases;

	private bool isSkinCases = true;

	[Header("Case Wheel")]
	public bool CaseRotate;

	private string SelectCaseName;

	private float StartRotate;

	public AnimationCurve Curve;

	public float Duration = 8f;

	public float Lerp = 1f;

	public Vector2 FinishInterval;

	private float FinishPosition;

	public AudioSource SoundSource;

	public float SoundInterval;

	private float LastSoundInterval;

	public mCaseItem[] CaseItems;

	public mCaseItem FinishItem;

	public Transform CaseItemsRoot;

	[Header("Finish Panel")]
	public UIPanel FinishPanel;

	public UISprite FinishBackground;

	public UITexture FinishEffect1;

	public UITexture FinishEffect2;

	public UILabel FinishLabel;

	public UISprite FinishWeaponTexture;

	public UITexture FinishMoneyGoldTexture;

	public GameObject FinishAlreadyAvailable;

	public UITexture FinishAlreadyAvailableTexture;

	public UILabel FinishAlreadyAvailableLabel;

	public UITexture FinishFireStatEffect;

	public UITexture FinishSecretWeaponEffect;

	private bool isFinishWeapon;

	private bool isFinishGold;

	private WeaponData FinishWeapon;

	private WeaponSkinData FinishWeaponSkin;

	private StickerData FinishSticker;

	private bool FinishWeaponFireStat;

	private bool FinishWeaponAlready;

	private List<KeyValuePair<int, int>> NormalSkins = new List<KeyValuePair<int, int>>();

	private List<KeyValuePair<int, int>> BaseSkins = new List<KeyValuePair<int, int>>();

	private List<KeyValuePair<int, int>> ProfessionalSkins = new List<KeyValuePair<int, int>>();

	private List<KeyValuePair<int, int>> LegendarySkins = new List<KeyValuePair<int, int>>();

	private List<KeyValuePair<int, int>> SecretWeaponSkins = new List<KeyValuePair<int, int>>();

	private List<int> BaseStickers = new List<int>();

	private List<int> ProfessionalStickers = new List<int>();

	private List<int> LegendaryStickers = new List<int>();

	[Header("Money & Gold Data")]
	public Texture2D MoneyTexture;

	public Texture2D GoldTexture;

	[Header("Others")]
	public GameObject InAppPanel;

	public GameObject ShareButton;

	public GameObject BackButton;

	private void Awake()
	{
		PhotonClassesManager.Add(this);
	}

	public override void OnDisconnectedFromPhoton()
	{
		Close();
	}

	private void Update()
	{
		UpdateCaseWheel();
	}

	public void Show(bool skin)
	{
		isSkinCases = skin;
		if (isSkinCases)
		{
			for (int i = 0; i < SkinCases.Length; i++)
			{
				SkinCases[i].NameLabel.text = Localization.Get(SkinCases[i].Name);
				SkinCases[i].InfoLabel.text = GetCaseInfo(SkinCases[i]);
			}
			UpdateSkinsList();
		}
		else
		{
			for (int j = 0; j < StickerCases.Length; j++)
			{
				StickerCases[j].NameLabel.text = Localization.Get(StickerCases[j].Name);
				StickerCases[j].InfoLabel.text = GetCaseInfo(StickerCases[j]);
			}
			UpdateStickerList();
		}
	}

	public void Close()
	{
		CaseRotate = false;
		if (isSkinCases)
		{
			mPanelManager.ShowPanel("SkinCases", true);
		}
		else
		{
			mPanelManager.ShowPanel("StickerCases", true);
		}
	}

	private string GetCaseInfo(Case selectCase)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(Localization.Get("ChanceOfDrop") + ":");
		stringBuilder.AppendLine(string.Empty);
		if ((bool)selectCase.Money)
		{
			stringBuilder.AppendLine(Localization.Get("Money") + " & " + Localization.Get("Gold") + " -  50%");
		}
		if ((int)selectCase.Normal != 0)
		{
			stringBuilder.AppendLine(string.Concat(Localization.Get("Normal quality"), " - ", selectCase.Normal, "%"));
		}
		if ((int)selectCase.Base != 0)
		{
			stringBuilder.AppendLine(string.Concat("[00aff0]", Localization.Get("Basic quality"), "[-] - ", selectCase.Base, "%"));
		}
		if ((int)selectCase.Professional != 0)
		{
			stringBuilder.AppendLine(string.Concat("[ff0000]", Localization.Get("Professional quality"), "[-] - ", selectCase.Professional, "%"));
		}
		if ((int)selectCase.Legendary != 0)
		{
			stringBuilder.AppendLine(string.Concat("[E00061]", Localization.Get("Legendary quality"), "[-] - ", selectCase.Legendary, "%"));
		}
		if ((int)selectCase.SecretWeapon != 0)
		{
			stringBuilder.AppendLine(string.Concat("[757575]", Localization.Get("Secret Weapon"), "[-] - ", selectCase.SecretWeapon, "%"));
		}
		return stringBuilder.ToString();
	}

	private Case GetCase(string caseName)
	{
		if (isSkinCases)
		{
			for (int i = 0; i < SkinCases.Length; i++)
			{
				if (SkinCases[i].Name == caseName)
				{
					return SkinCases[i];
				}
			}
		}
		else
		{
			for (int j = 0; j < StickerCases.Length; j++)
			{
				if (StickerCases[j].Name == caseName)
				{
					return StickerCases[j];
				}
			}
		}
		return null;
	}

	private void UpdateSkinsList()
	{
		if (NormalSkins.Count != 0)
		{
			return;
		}
		for (int i = 0; i < GameSettings.instance.Weapons.Count; i++)
		{
			if ((bool)GameSettings.instance.Weapons[i].Lock || (bool)GameSettings.instance.Weapons[i].Secret)
			{
				continue;
			}
			for (int j = 0; j < GameSettings.instance.WeaponsStore[i].Skins.Count; j++)
			{
				if ((int)GameSettings.instance.WeaponsStore[i].Skins[j].Price == 0)
				{
					switch (GameSettings.instance.WeaponsStore[i].Skins[j].Quality)
					{
					case WeaponSkinQuality.Normal:
						NormalSkins.Add(new KeyValuePair<int, int>(GameSettings.instance.Weapons[i].ID, GameSettings.instance.WeaponsStore[i].Skins[j].ID));
						break;
					case WeaponSkinQuality.Basic:
						BaseSkins.Add(new KeyValuePair<int, int>(GameSettings.instance.Weapons[i].ID, GameSettings.instance.WeaponsStore[i].Skins[j].ID));
						break;
					case WeaponSkinQuality.Professional:
						ProfessionalSkins.Add(new KeyValuePair<int, int>(GameSettings.instance.Weapons[i].ID, GameSettings.instance.WeaponsStore[i].Skins[j].ID));
						break;
					case WeaponSkinQuality.Legendary:
						LegendarySkins.Add(new KeyValuePair<int, int>(GameSettings.instance.Weapons[i].ID, GameSettings.instance.WeaponsStore[i].Skins[j].ID));
						break;
					}
				}
			}
		}
		for (int k = 0; k < GameSettings.instance.Weapons.Count; k++)
		{
			if ((bool)GameSettings.instance.Weapons[k].Lock || !GameSettings.instance.Weapons[k].Secret)
			{
				continue;
			}
			for (int l = 0; l < GameSettings.instance.WeaponsStore[k].Skins.Count; l++)
			{
				if (GameSettings.instance.WeaponsStore[k].Skins[l].Quality != WeaponSkinQuality.Default && (int)GameSettings.instance.WeaponsStore[k].Skins[l].Price == 0)
				{
					SecretWeaponSkins.Add(new KeyValuePair<int, int>(GameSettings.instance.Weapons[k].ID, GameSettings.instance.WeaponsStore[k].Skins[l].ID));
				}
			}
		}
	}

	private void UpdateStickerList()
	{
		if (BaseStickers.Count != 0)
		{
			return;
		}
		for (int i = 0; i < GameSettings.instance.Stickers.Count; i++)
		{
			switch (GameSettings.instance.Stickers[i].Quality)
			{
			case StickerQuality.Basic:
				BaseStickers.Add(GameSettings.instance.Stickers[i].ID);
				break;
			case StickerQuality.Professional:
				ProfessionalStickers.Add(GameSettings.instance.Stickers[i].ID);
				break;
			case StickerQuality.Legendary:
				LegendaryStickers.Add(GameSettings.instance.Stickers[i].ID);
				break;
			}
		}
	}

	public void StartCaseWheel(string caseName)
	{
		if (!AccountManager.isConnect)
		{
			UIToast.Show(Localization.Get("Connection account"));
			return;
		}
		SelectCaseName = caseName;
		Case obj = GetCase(caseName);
		if ((int)obj.Price != 0)
		{
			if ((bool)obj.PriceGold)
			{
				if ((int)obj.Price > AccountManager.GetGold())
				{
					InAppPanel.SetActive(true);
					UIToast.Show(Localization.Get("Not enough money"));
					return;
				}
				AccountManager.SetGold(AccountManager.GetGold() - (int)obj.Price);
			}
			else
			{
				if ((int)obj.Price > AccountManager.GetMoney())
				{
					InAppPanel.SetActive(true);
					UIToast.Show(Localization.Get("Not enough money"));
					return;
				}
				AccountManager.SetMoney1(-(int)obj.Price);
			}
		}
		TweenAlpha component = FinishPanel.GetComponent<TweenAlpha>();
		if (component != null)
		{
			component.enabled = false;
		}
		FinishPanel.alpha = 0f;
		mPanelManager.ShowPanel("CaseWheel", false);
		CaseItemsRoot.localPosition = new Vector3(0f, CaseItemsRoot.localPosition.y, 0f);
		StartRotate = Time.time;
		CaseRotate = true;
		LastSoundInterval = SoundInterval / 2f;
		FinishPosition = (int)UnityEngine.Random.Range(FinishInterval.x, FinishInterval.y);
		bool flag = obj.Money;
		bool finishMoneyData = false;
		UpdateOthersCaseItems();
		if (flag)
		{
			if (UnityEngine.Random.value > 0.25f)
			{
				if (UnityEngine.Random.value > 0.8f)
				{
					finishMoneyData = true;
				}
			}
			else
			{
				flag = false;
			}
		}
		if (flag)
		{
			SetFinishMoneyData(finishMoneyData);
		}
		else
		{
			SetFinishWeaponData(obj);
		}
	}

	private void UpdateOthersCaseItems()
	{
		Case obj = GetCase(SelectCaseName);
		bool flag = obj.Money;
		bool flag2 = false;
		int max = GameSettings.instance.Stickers.Count - 1;
		for (int i = 0; i < CaseItems.Length; i++)
		{
			flag = obj.Money;
			flag2 = false;
			if (flag)
			{
				if (UnityEngine.Random.value > 0.4f)
				{
					if (UnityEngine.Random.value > 0.6f)
					{
						flag2 = true;
					}
				}
				else
				{
					flag = false;
				}
			}
			if (flag)
			{
				SetCaseMoneyItem(CaseItems[i], flag2);
			}
			else if (isSkinCases)
			{
				int num = UnityEngine.Random.Range(0, 100);
				KeyValuePair<int, int> keyValuePair = default(KeyValuePair<int, int>);
				keyValuePair = ((num < 25) ? (((int)obj.Normal == 0) ? BaseSkins[UnityEngine.Random.Range(0, BaseSkins.Count)] : NormalSkins[UnityEngine.Random.Range(0, NormalSkins.Count)]) : ((num < 50) ? BaseSkins[UnityEngine.Random.Range(0, BaseSkins.Count)] : ((num < 75) ? ProfessionalSkins[UnityEngine.Random.Range(0, ProfessionalSkins.Count)] : ((i <= 10 || (int)obj.SecretWeapon == 0 || num <= 98) ? LegendarySkins[UnityEngine.Random.Range(0, LegendarySkins.Count)] : SecretWeaponSkins[UnityEngine.Random.Range(0, SecretWeaponSkins.Count)]))));
				SetCaseWeaponItem(CaseItems[i], keyValuePair.Key, keyValuePair.Value);
			}
			else
			{
				int index = UnityEngine.Random.Range(0, max);
				SetCaseStickerItem(CaseItems[i], GameSettings.instance.Stickers[index]);
			}
		}
	}

	private void UpdateCaseWheel()
	{
		if (CaseRotate)
		{
			float time = (Time.time - StartRotate) / Duration;
			float num = Curve.Evaluate(time);
			float x = num * FinishPosition;
			float num2 = 1f - num + Lerp;
			CaseItemsRoot.localPosition = Vector3.Lerp(CaseItemsRoot.localPosition, new Vector3(x, CaseItemsRoot.localPosition.y, 0f), Time.deltaTime * num2);
			if (0f - CaseItemsRoot.localPosition.x > LastSoundInterval)
			{
				SoundSource.PlayOneShot(SoundSource.clip);
				LastSoundInterval += SoundInterval;
			}
			if (CaseItemsRoot.localPosition.x <= FinishPosition + 2f)
			{
				CaseRotate = false;
				StartFinishPanel();
			}
		}
	}

	private void SetFinishWeaponData(Case selectCase)
	{
		int randomQualty = GetRandomQualty(selectCase);
		if (isSkinCases)
		{
			isFinishWeapon = true;
			bool flag = (int)selectCase.SecretWeapon >= UnityEngine.Random.Range(1, 100);
			KeyValuePair<int, int> keyValuePair = default(KeyValuePair<int, int>);
			if (flag)
			{
				keyValuePair = SecretWeaponSkins[UnityEngine.Random.Range(0, SecretWeaponSkins.Count)];
			}
			else
			{
				switch (randomQualty)
				{
				case 1:
					keyValuePair = NormalSkins[UnityEngine.Random.Range(0, NormalSkins.Count)];
					break;
				case 2:
					keyValuePair = BaseSkins[UnityEngine.Random.Range(0, BaseSkins.Count)];
					break;
				case 3:
					keyValuePair = ProfessionalSkins[UnityEngine.Random.Range(0, ProfessionalSkins.Count)];
					break;
				case 4:
					keyValuePair = LegendarySkins[UnityEngine.Random.Range(0, LegendarySkins.Count)];
					break;
				}
			}
			FinishWeapon = WeaponManager.GetWeaponData(keyValuePair.Key);
			FinishWeaponSkin = WeaponManager.GetWeaponSkin(keyValuePair.Key, keyValuePair.Value);
			if (FinishWeaponSkin.Quality != WeaponSkinQuality.Default && FinishWeaponSkin.Quality != WeaponSkinQuality.Normal && FinishWeapon.Type != WeaponType.Knife && (int)GetCase(SelectCaseName).FireStat >= UnityEngine.Random.Range(1, 100))
			{
				FinishWeaponFireStat = true;
			}
			else
			{
				FinishWeaponFireStat = false;
			}
			if (FinishWeaponFireStat)
			{
				FinishWeaponAlready = AccountManager.GetFireStat(FinishWeapon.ID, FinishWeaponSkin.ID);
			}
			else
			{
				FinishWeaponAlready = AccountManager.GetWeaponSkin(FinishWeapon.ID, FinishWeaponSkin.ID);
			}
			if (FinishWeaponAlready)
			{
				SetQualityMoney(FinishWeaponSkin.Quality, FinishWeaponFireStat, FinishWeapon.Secret, GetCase(SelectCaseName).Money);
			}
			AccountManager.SetWeaponSkin(FinishWeapon.ID, FinishWeaponSkin.ID);
			SetCaseWeaponItem(FinishItem, keyValuePair.Key, keyValuePair.Value);
			AccountManager.UpdateDefaultAndWeaponsData();
		}
		else
		{
			int index = -1;
			switch (randomQualty)
			{
			case 1:
				index = BaseStickers[UnityEngine.Random.Range(0, BaseStickers.Count)];
				break;
			case 2:
				index = ProfessionalStickers[UnityEngine.Random.Range(0, ProfessionalStickers.Count)];
				break;
			case 3:
				index = LegendaryStickers[UnityEngine.Random.Range(0, LegendaryStickers.Count)];
				break;
			}
			FinishSticker = GameSettings.instance.Stickers[index];
			AccountManager.SetStickers(FinishSticker.ID);
			SetCaseStickerItem(FinishItem, FinishSticker);
			AccountManager.UpdateDefaultAndWeaponsData();
		}
	}

	private int GetRandomQualty(Case selectCase)
	{
		int num = UnityEngine.Random.Range(0, 100);
		if (isSkinCases)
		{
			int num2 = ((!selectCase.Money) ? 1 : 2);
			if (((int)selectCase.Normal + (int)selectCase.Base + (int)selectCase.Professional) * num2 < num)
			{
				return 4;
			}
			if (((int)selectCase.Normal + (int)selectCase.Base) * num2 < num)
			{
				return 3;
			}
			if ((int)selectCase.Normal * num2 < num)
			{
				return 2;
			}
			return 1;
		}
		if ((int)selectCase.Base + (int)selectCase.Professional < num)
		{
			return 3;
		}
		if ((int)selectCase.Base < num)
		{
			return 2;
		}
		return 1;
	}

	private void SetFinishMoneyData(bool isGold)
	{
		isFinishWeapon = false;
		isFinishGold = isGold;
		if (isFinishGold)
		{
			AccountManager.SetGold1(2);
		}
		else
		{
			AccountManager.SetMoney1(55);
		}
		SetCaseMoneyItem(FinishItem, isGold);
		AccountManager.UpdateDefaultData(null, null);
	}

	private void SetCaseWeaponItem(mCaseItem caseItem, int weaponID, int ID)
	{
		WeaponSkinData weaponSkin = WeaponManager.GetWeaponSkin(weaponID, ID);
		caseItem.ItemWeaponTexture.atlas = GameSettings.instance.WeaponIconAtlas;
		caseItem.ItemWeaponTexture.cachedGameObject.SetActive(true);
		caseItem.ItemGoldMoneyTexture.cachedGameObject.SetActive(false);
		caseItem.ItemWeaponTexture.spriteName = weaponID + "-" + ID;
		caseItem.ItemWeaponTexture.width = (int)GameSettings.instance.WeaponsCaseSize[weaponID - 1].x;
		caseItem.ItemWeaponTexture.height = (int)GameSettings.instance.WeaponsCaseSize[weaponID - 1].y;
		caseItem.ItemLabel.text = weaponSkin.Name;
		caseItem.ItemLabelSprite.color = GetSkinQualityColor(weaponSkin.Quality);
	}

	private void SetCaseMoneyItem(mCaseItem caseItem, bool gold)
	{
		caseItem.ItemGoldMoneyTexture.cachedGameObject.SetActive(true);
		caseItem.ItemWeaponTexture.cachedGameObject.SetActive(false);
		caseItem.ItemGoldMoneyTexture.mainTexture = ((!gold) ? MoneyTexture : GoldTexture);
		caseItem.ItemGoldMoneyTexture.width = 80;
		caseItem.ItemGoldMoneyTexture.height = 80;
		caseItem.ItemGoldMoneyTexture.uvRect = new Rect(0f, 0f, 1f, 1f);
		caseItem.ItemLabel.text = Localization.Get((!gold) ? "Money" : "Gold");
		caseItem.ItemLabelSprite.color = GetSkinQualityColor(WeaponSkinQuality.Normal);
	}

	private void SetCaseStickerItem(mCaseItem caseItem, StickerData sticker)
	{
		caseItem.ItemWeaponTexture.atlas = GameSettings.instance.StickersAtlas;
		caseItem.ItemWeaponTexture.cachedGameObject.SetActive(true);
		caseItem.ItemGoldMoneyTexture.cachedGameObject.SetActive(false);
		caseItem.ItemWeaponTexture.spriteName = sticker.ID.ToString();
		caseItem.ItemWeaponTexture.width = 64;
		caseItem.ItemWeaponTexture.height = 64;
		caseItem.ItemLabel.text = sticker.Name;
		caseItem.ItemLabelSprite.color = GetStickerQualityColor(sticker.Quality);
	}

	private Color GetSkinQualityColor(WeaponSkinQuality quality)
	{
		switch (quality)
		{
		case WeaponSkinQuality.Default:
		case WeaponSkinQuality.Normal:
			return new Color(0.63f, 0.63f, 0.63f, 1f);
		case WeaponSkinQuality.Basic:
			return new Color(0.07f, 0.65f, 0.87f, 1f);
		case WeaponSkinQuality.Professional:
			return new Color(0.9f, 0f, 0f, 1f);
		case WeaponSkinQuality.Legendary:
			return new Color(0.87f, 0f, 0.38f, 1f);
		default:
			return new Color(0.63f, 0.63f, 0.63f, 1f);
		}
	}

	private Color GetStickerQualityColor(StickerQuality quality)
	{
		switch (quality)
		{
		case StickerQuality.Basic:
			return new Color(0.07f, 0.65f, 0.87f, 1f);
		case StickerQuality.Professional:
			return new Color(0.9f, 0f, 0f, 1f);
		case StickerQuality.Legendary:
			return new Color(0.87f, 0f, 0.38f, 1f);
		default:
			return new Color(0.63f, 0.63f, 0.63f, 1f);
		}
	}

	public void StartFinishPanel()
	{
		TweenAlpha.Begin(FinishPanel.cachedGameObject, 0.5f, 1f);
		AccountManager.SetOpenCase1();
		if (isSkinCases)
		{
			if (isFinishWeapon)
			{
				Color skinQualityColor = GetSkinQualityColor(FinishWeaponSkin.Quality);
				if (FinishWeaponSkin.Quality != WeaponSkinQuality.Default && FinishWeaponSkin.Quality != WeaponSkinQuality.Normal && FinishWeapon.Type != WeaponType.Knife)
				{
					if (FinishWeaponFireStat)
					{
						AccountManager.SetFireStat(FinishWeapon.ID, FinishWeaponSkin.ID);
						FinishFireStatEffect.cachedTransform.parent.gameObject.SetActive(true);
						FinishFireStatEffect.color = skinQualityColor;
					}
					else
					{
						FinishFireStatEffect.cachedTransform.parent.gameObject.SetActive(false);
					}
				}
				else
				{
					FinishFireStatEffect.cachedTransform.parent.gameObject.SetActive(false);
				}
				if ((bool)FinishWeapon.Secret)
				{
					AccountManager.SetWeapon(FinishWeapon.ID);
					FinishSecretWeaponEffect.cachedTransform.parent.gameObject.SetActive(true);
					FinishSecretWeaponEffect.color = skinQualityColor;
				}
				else
				{
					FinishSecretWeaponEffect.cachedTransform.parent.gameObject.SetActive(false);
				}
				FinishBackground.color = skinQualityColor;
				FinishBackground.alpha = 0.6f;
				FinishEffect1.color = skinQualityColor;
				FinishEffect1.alpha = 0.7f;
				FinishEffect2.color = skinQualityColor;
				FinishLabel.text = string.Concat(FinishWeapon.Name, " | ", FinishWeaponSkin.Name);
				FinishWeaponTexture.atlas = GameSettings.instance.WeaponIconAtlas;
				FinishWeaponTexture.cachedGameObject.SetActive(true);
				FinishMoneyGoldTexture.cachedGameObject.SetActive(false);
				FinishWeaponTexture.spriteName = string.Concat(FinishWeapon.ID, "-", FinishWeaponSkin.ID);
				FinishWeaponTexture.width = (int)GameSettings.instance.WeaponsCaseSize[(int)FinishWeapon.ID - 1].x * 2;
				FinishWeaponTexture.height = (int)GameSettings.instance.WeaponsCaseSize[(int)FinishWeapon.ID - 1].y * 2;
				FinishAlreadyAvailable.SetActive(FinishWeaponAlready);
			}
			else
			{
				Color skinQualityColor2 = GetSkinQualityColor(WeaponSkinQuality.Normal);
				FinishBackground.color = skinQualityColor2;
				FinishBackground.alpha = 0.6f;
				FinishEffect1.color = skinQualityColor2;
				FinishEffect1.alpha = 0.7f;
				FinishEffect2.color = skinQualityColor2;
				FinishLabel.text = Localization.Get((!isFinishGold) ? "Money" : "Gold") + " | " + ((!isFinishGold) ? "+55" : "+2");
				FinishMoneyGoldTexture.cachedGameObject.SetActive(true);
				FinishWeaponTexture.cachedGameObject.SetActive(false);
				FinishMoneyGoldTexture.mainTexture = ((!isFinishGold) ? MoneyTexture : GoldTexture);
				FinishAlreadyAvailable.SetActive(false);
				FinishFireStatEffect.cachedTransform.parent.gameObject.SetActive(false);
				FinishSecretWeaponEffect.cachedTransform.parent.gameObject.SetActive(false);
			}
		}
		else
		{
			Color stickerQualityColor = GetStickerQualityColor(FinishSticker.Quality);
			FinishFireStatEffect.cachedTransform.parent.gameObject.SetActive(false);
			FinishSecretWeaponEffect.cachedTransform.parent.gameObject.SetActive(false);
			FinishBackground.color = stickerQualityColor;
			FinishBackground.alpha = 0.6f;
			FinishEffect1.color = stickerQualityColor;
			FinishEffect1.alpha = 0.7f;
			FinishEffect2.color = stickerQualityColor;
			FinishLabel.text = FinishSticker.Name;
			FinishWeaponTexture.atlas = GameSettings.instance.StickersAtlas;
			FinishWeaponTexture.cachedGameObject.SetActive(true);
			FinishMoneyGoldTexture.cachedGameObject.SetActive(false);
			FinishWeaponTexture.spriteName = FinishSticker.ID.ToString();
			FinishWeaponTexture.width = 128;
			FinishWeaponTexture.height = 128;
			FinishAlreadyAvailable.SetActive(false);
		}
		EventManager.Dispatch("AccountUpdate");
	}

	private void SetQualityMoney(WeaponSkinQuality quality, bool firestat, bool secret, bool moneyCase)
	{
		int num = 0;
		int gold = 0;
		switch (quality)
		{
		case WeaponSkinQuality.Default:
		case WeaponSkinQuality.Normal:
			if (secret)
			{
				gold = 15;
			}
			else
			{
				num = 100;
			}
			break;
		case WeaponSkinQuality.Basic:
			if (secret)
			{
				gold = 20;
			}
			else if (firestat)
			{
				gold = 8;
			}
			else
			{
				num = 300;
			}
			break;
		case WeaponSkinQuality.Professional:
			gold = ((!secret) ? ((!firestat) ? 5 : 12) : 25);
			break;
		case WeaponSkinQuality.Legendary:
			gold = ((!secret) ? ((!firestat) ? 8 : 16) : 30);
			break;
		}
		if (moneyCase)
		{
			FinishAlreadyAvailableTexture.cachedGameObject.SetActive(false);
			return;
		}
		FinishAlreadyAvailableTexture.cachedGameObject.SetActive(true);
		AccountManager.SetMoney1(num);
		AccountManager.SetGold1(gold);
		if (num > 0)
		{
			FinishAlreadyAvailableTexture.mainTexture = MoneyTexture;
			FinishAlreadyAvailableLabel.text = num.ToString();
		}
		else
		{
			FinishAlreadyAvailableTexture.mainTexture = GoldTexture;
			FinishAlreadyAvailableLabel.text = gold.ToString();
		}
	}

	public void StartRewardedCase()
	{
		if (!AccountManager.isConnect)
		{
			UIToast.Show(Localization.Get("Connection account"));
			return;
		}
		mPopUp.ShowText(Localization.Get("Please wait") + "...");
		TimerManager.In(0.5f, () =>
		{
			AdsManager.ShowRewardedVideo(RewardedVideoComplete, RewardedVideoFailed, RewardedVideoAborted, "CaseVideo");
		});
	}

	private void RewardedVideoComplete()
	{
		TimerManager.In(0.3f, () =>
		{
			mPopUp.HideAll("CaseWheel", false);
			StartCaseWheel("Free");
		});
	}

	private void RewardedVideoAborted()
	{
		TimerManager.In(0.3f, () =>
		{
			UIToast.Show(Localization.Get("Cancel"));
			mPopUp.HideAll("Cases", true);
		});
	}

	private void RewardedVideoFailed()
	{
		UIToast.Show(Localization.Get("Video not available"));
		mPopUp.HideAll("Cases", true);
	}
}
