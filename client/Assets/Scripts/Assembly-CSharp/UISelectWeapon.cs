using System.Collections.Generic;
using UnityEngine;

public class UISelectWeapon : MonoBehaviour
{
	public WeaponType Weapon;

	public UILabel WeaponNameLabel;

	public UISprite WeaponSprite;

	public float Size = 1f;

	private List<string> WeaponList = new List<string>();

	private int SelectWeapon;

	private static bool isChange = false;

	public static CryptoBool AllWeapons = false;

	public static CryptoBool SelectedUpdateWeaponManager = false;

	private void Start()
	{
		UpdateWeaponsName();
		GetSelectWeapon();
		UpdateSelectedWeapon();
	}

	public void Close()
	{
		if ((bool)SelectedUpdateWeaponManager && isChange)
		{
			PlayerInput.instance.PlayerWeapon.UpdateWeaponAll();
			isChange = false;
		}
	}

	public void Left()
	{
		SelectWeapon--;
		if (SelectWeapon < 0)
		{
			SelectWeapon = WeaponList.Count - 1;
		}
		if (!AllWeapons)
		{
			AccountManager.SetWeaponSelected(Weapon, WeaponManager.GetWeaponID(WeaponList[SelectWeapon]));
		}
		UpdateSelectedWeapon();
	}

	public void Right()
	{
		SelectWeapon++;
		if (SelectWeapon > WeaponList.Count - 1)
		{
			SelectWeapon = 0;
		}
		if (!AllWeapons)
		{
			AccountManager.SetWeaponSelected(Weapon, WeaponManager.GetWeaponID(WeaponList[SelectWeapon]));
		}
		UpdateSelectedWeapon();
	}

	private void GetSelectWeapon()
	{
		int weaponSelected = AccountManager.GetWeaponSelected(Weapon);
		string weaponName = WeaponManager.GetWeaponName(weaponSelected);
		for (int i = 0; i < WeaponList.Count; i++)
		{
			if (WeaponList[i] == weaponName)
			{
				SelectWeapon = i;
				break;
			}
		}
	}

	private void UpdateWeaponsName()
	{
		for (int i = 0; i < GameSettings.instance.Weapons.Count; i++)
		{
			if (GameSettings.instance.Weapons[i].Type != Weapon)
			{
				continue;
			}
			int num = GameSettings.instance.Weapons[i].ID;
			string item = GameSettings.instance.Weapons[i].Name;
			if (!AccountManager.GetWeapon(num) && !AllWeapons)
			{
				continue;
			}
			if (num == 4 || num == 3 || num == 12)
			{
				WeaponList.Insert(0, item);
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
						WeaponList.Add(item);
					}
				}
				else
				{
					WeaponList.Add(item);
				}
			}
		}
	}

	private void UpdateSelectedWeapon()
	{
		string text = WeaponList[SelectWeapon];
		for (int i = 0; i < GameSettings.instance.Weapons.Count; i++)
		{
			if (GameSettings.instance.Weapons[i].Type != Weapon || !(GameSettings.instance.Weapons[i].Name == text))
			{
				continue;
			}
			WeaponManager.SetSelectWeapon(Weapon, GameSettings.instance.Weapons[i].ID);
			int weaponSkinSelected = AccountManager.GetWeaponSkinSelected(GameSettings.instance.Weapons[i].ID);
			for (int j = 0; j < GameSettings.instance.WeaponsStore[i].Skins.Count; j++)
			{
				if ((int)GameSettings.instance.WeaponsStore[i].Skins[j].ID == weaponSkinSelected)
				{
					WeaponNameLabel.text = text + "  |  " + GetWeaponSkinRarityColor(GameSettings.instance.WeaponsStore[i].Skins[j]);
					WeaponSprite.spriteName = string.Concat(GameSettings.instance.Weapons[i].ID, "-", GameSettings.instance.WeaponsStore[i].Skins[j].ID);
					WeaponSprite.width = (int)(GameSettings.instance.WeaponsCaseSize[i].x * Size);
					WeaponSprite.height = (int)(GameSettings.instance.WeaponsCaseSize[i].y * Size);
					if ((bool)SelectedUpdateWeaponManager)
					{
						isChange = true;
					}
					break;
				}
			}
			break;
		}
	}

	private string GetWeaponSkinRarityColor(WeaponSkinData skin)
	{
		switch (skin.Quality)
		{
		case WeaponSkinQuality.Default:
		case WeaponSkinQuality.Normal:
			return Utils.ColorToHex(new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue), skin.Name);
		case WeaponSkinQuality.Basic:
			return Utils.ColorToHex(new Color32(54, 189, byte.MaxValue, byte.MaxValue), skin.Name);
		case WeaponSkinQuality.Professional:
			return Utils.ColorToHex(new Color32(byte.MaxValue, 0, 0, byte.MaxValue), skin.Name);
		case WeaponSkinQuality.Legendary:
			return Utils.ColorToHex(new Color32(byte.MaxValue, 0, byte.MaxValue, byte.MaxValue), skin.Name);
		default:
			return skin.Name;
		}
	}
}
