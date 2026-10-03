using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using FreeJSON;
using UnityEngine;

public class AccountConvert
{
	private static List<string> a = new List<string>
	{
		"a", "e", "i", "o", "u", "y", "b", "c", "d", "f",
		"g", "h", "j", "k", "l", "m", "n", "p", "q", "r",
		"s", "t", "v", "w", "x", "z", "0", "1", "2", "3",
		"4", "5", "6", "7", "8", "9", "*"
	};

	private static List<string> b = new List<string>
	{
		"p", "h", "z", "r", "i", "q", "y", "b", "m", "s",
		"5", "6", "w", "k", "u", "9", "g", "j", "o", "d",
		"c", "v", "l", "e", "2", "7", "8", "a", "0", "1",
		"t", "n", "*", "4", "3", "f", "x"
	};

	public static AccountData Copy(AccountData data)
	{
		IFormatter formatter = new BinaryFormatter();
		Stream stream = new MemoryStream();
		using (stream)
		{
			formatter.Serialize(stream, data);
			stream.Seek(0L, SeekOrigin.Begin);
			return (AccountData)formatter.Deserialize(stream);
		}
	}

	public static void CopyDefaultValue(AccountData from, AccountData to)
	{
		to.GameVersion = from.GameVersion;
		to.AccountName = from.AccountName;
		to.Money = from.Money;
		to.Gold = from.Gold;
		to.XP = from.XP;
		to.Level = from.Level;
		to.OpenCase = from.OpenCase;
		to.Deaths = from.Deaths;
		to.Kills = from.Kills;
		to.Headshot = from.Headshot;
		to.SelectedRifle = from.SelectedRifle;
		to.SelectedPistol = from.SelectedPistol;
		to.SelectedKnife = from.SelectedKnife;
		to.SelectedPlayerSkin = from.SelectedPlayerSkin;
		to.PlayerSkins = from.PlayerSkins;
		to.Stickers = from.Stickers;
		to.InAppPurchase = from.InAppPurchase;
		to.AndroidID = from.AndroidID;
	}

	public static void CopyWeaponsValue(AccountData from, AccountData to)
	{
		to.Weapons = from.Weapons;
	}

	public static string Serialize(AccountData data, bool registerTime)
	{
		JsonObject jsonObject = new JsonObject();
		if ((int)data.GameVersion == 0)
		{
			jsonObject.Add("GameVersion", ToInt(VersionManager.bundleVersion));
		}
		else
		{
			jsonObject.Add("GameVersion", (int)data.GameVersion);
		}
		jsonObject.Add("AccountName", (string)data.AccountName);
		jsonObject.Add("Money", (int)data.Money);
		jsonObject.Add("Gold", (int)data.Gold);
		jsonObject.Add("LevelXP", (int)data.XP);
		jsonObject.Add("PlayerLevel", Mathf.Clamp(data.Level, 1, 250));
		jsonObject.Add("OpenCase", (int)data.OpenCase);
		jsonObject.Add("Deaths", (int)data.Deaths);
		jsonObject.Add("Kills", (int)data.Kills);
		jsonObject.Add("Headshot", (int)data.Headshot);
		jsonObject.Add("SelectedRifle", (int)data.SelectedRifle);
		jsonObject.Add("SelectedPistol", (int)data.SelectedPistol);
		jsonObject.Add("SelectedKnife", (int)data.SelectedKnife);
		jsonObject.Add("SelectedPlayerSkin", (int)data.SelectedPlayerSkin);
		jsonObject.Add("Clan", (string)data.Clan);
		JsonObject jsonObject2 = new JsonObject();
		for (int i = 0; i < data.PlayerSkins.Count; i++)
		{
			jsonObject2.Add(i.ToString(), (int)data.PlayerSkins[i]);
		}
		jsonObject.Add("PlayerSkins", jsonObject2);
		JsonObject jsonObject3 = new JsonObject();
		data.SortStickers();
		for (int j = 0; j < data.Stickers.Count; j++)
		{
			jsonObject3.Add(data.Stickers[j].ID.ToString("D2"), (int)data.Stickers[j].Count);
		}
		jsonObject.Add("Stickers", jsonObject3);
		JsonArray jsonArray = new JsonArray();
		for (int k = 0; k < GameSettings.instance.Weapons.Count; k++)
		{
			AccountWeapon weaponData = GetWeaponData(GameSettings.instance.Weapons[k].ID, data);
			JsonObject jsonObject4 = new JsonObject();
			jsonObject4.Add("ID", (int)weaponData.ID);
			jsonObject4.Add("Buy", (bool)weaponData.Buy);
			jsonObject4.Add("Upgrade", (int)weaponData.Upgrade);
			JsonObject jsonObject5 = new JsonObject();
			for (int l = 0; l < weaponData.Skins.Count; l++)
			{
				if ((int)weaponData.Skins[l] != 0)
				{
					jsonObject5.Add(l.ToString(), (int)weaponData.Skins[l]);
				}
			}
			jsonObject4.Add("Skins", jsonObject5);
			JsonObject jsonObject6 = new JsonObject();
			for (int m = 0; m < GameSettings.instance.WeaponsStore[k].Skins.Count; m++)
			{
				if (weaponData.FireStats.Count > m && (int)weaponData.FireStats[m] != -1)
				{
					jsonObject6.Add(m.ToString("D2"), (int)weaponData.FireStats[m]);
				}
			}
			jsonObject4.Add("FireStats", jsonObject6);
			JsonObject jsonObject7 = new JsonObject();
			weaponData.SortWeaponStickers();
			for (int n = 0; n < weaponData.Stickers.Count; n++)
			{
				JsonObject jsonObject8 = new JsonObject();
				for (int num = 0; num < weaponData.Stickers[n].StickerData.Count; num++)
				{
					jsonObject8.Add(weaponData.Stickers[n].StickerData[num].Index.ToString("D2"), (int)weaponData.Stickers[n].StickerData[num].StickerID);
				}
				jsonObject7.Add(weaponData.Stickers[n].SkinID.ToString("D2"), jsonObject8);
			}
			jsonObject4.Add("Stickers", jsonObject7);
			jsonObject4.Add("SelectedSkin", (int)weaponData.SelectedSkin);
			jsonArray.Add(jsonObject4);
		}
		jsonObject.Add("Weapons", jsonArray);
		JsonObject jsonObject9 = new JsonObject();
		for (int num2 = 0; num2 < data.InAppPurchase.Count; num2++)
		{
			jsonObject9.Add(num2.ToString(), (string)data.InAppPurchase[num2]);
		}
		jsonObject.Add("InAppPurchase", jsonObject9);
		if (data.AndroidID == string.Empty)
		{
			jsonObject.Add("AndroidID", AndroidNativeFunctions.GetAndroidID());
		}
		else
		{
			jsonObject.Add("AndroidID", (string)data.AndroidID);
		}
		if (registerTime)
		{
			jsonObject.Add("RegisterTime", JsonObject.Parse(Firebase.GetTimeStamp()));
		}
		return jsonObject.ToString();
	}

	private static AccountWeapon GetWeaponData(int id, AccountData data)
	{
		for (int i = 0; i < data.Weapons.Count; i++)
		{
			if (id == (int)data.Weapons[i].ID)
			{
				return data.Weapons[i];
			}
		}
		AccountWeapon accountWeapon = new AccountWeapon();
		accountWeapon.ID = id;
		return accountWeapon;
	}

	public static AccountData Deserialize(string text)
	{
		JsonObject jsonObject = JsonObject.Parse(text);
		AccountData accountData = new AccountData();
		accountData.GameVersion = jsonObject.Get<int>("GameVersion");
		accountData.AccountName = jsonObject.Get<string>("AccountName");
		accountData.Money = jsonObject.Get<int>("Money");
		accountData.Gold = jsonObject.Get<int>("Gold");
		if (jsonObject.ContainsKey("LevelXP"))
		{
			accountData.XP = jsonObject.Get<int>("LevelXP");
		}
		else
		{
			accountData.XP = jsonObject.Get<int>("XP");
		}
		if (jsonObject.ContainsKey("PlayerLevel"))
		{
			accountData.Level = Mathf.Clamp(jsonObject.Get<int>("PlayerLevel"), 1, 250);
		}
		else
		{
			accountData.Level = Mathf.Clamp(jsonObject.Get<int>("Level"), 1, 250);
		}
		accountData.OpenCase = jsonObject.Get<int>("OpenCase");
		accountData.Deaths = jsonObject.Get<int>("Deaths");
		accountData.Kills = jsonObject.Get<int>("Kills");
		accountData.Headshot = jsonObject.Get<int>("Headshot");
		accountData.SelectedRifle = jsonObject.Get<int>("SelectedRifle");
		accountData.SelectedPistol = jsonObject.Get<int>("SelectedPistol");
		accountData.SelectedKnife = jsonObject.Get<int>("SelectedKnife");
		accountData.SelectedPlayerSkin = jsonObject.Get<int>("SelectedPlayerSkin");
		accountData.Clan = jsonObject.Get<string>("Clan");
		List<int> list = jsonObject.Get<List<int>>("PlayerSkins");
		for (int i = 0; i < list.Count; i++)
		{
			accountData.PlayerSkins.Add(list[i]);
		}
		JsonObject jsonObject2 = jsonObject.Get<JsonObject>("Stickers");
		List<AccountSticker> list2 = new List<AccountSticker>();
		for (int j = 0; j < jsonObject2.Length; j++)
		{
			int result = -1;
			int num = jsonObject2.Get<int>(jsonObject2.GetKey(j));
			int.TryParse(jsonObject2.GetKey(j), out result);
			if (result != -1 && num > 0)
			{
				AccountSticker accountSticker = new AccountSticker();
				accountSticker.ID = result;
				accountSticker.Count = num;
				list2.Add(accountSticker);
			}
		}
		accountData.Stickers = list2;
		accountData.SortStickers();
		JsonArray jsonArray = jsonObject.Get<JsonArray>("Weapons");
		for (int k = 0; k < GameSettings.instance.Weapons.Count; k++)
		{
			AccountWeapon accountWeapon = new AccountWeapon();
			if (jsonArray.Length > k)
			{
				JsonObject jsonObject3 = jsonArray.Get<JsonObject>(k);
				accountWeapon.ID = jsonObject3.Get<int>("ID");
				accountWeapon.Buy = jsonObject3.Get<bool>("Buy");
				accountWeapon.Upgrade = jsonObject3.Get<int>("Upgrade");
				List<int> list3 = jsonObject3.Get<List<int>>("Skins");
				for (int l = 0; l < list3.Count; l++)
				{
					accountWeapon.Skins.Add(list3[l]);
				}
				JsonObject jsonObject4 = jsonObject3.Get<JsonObject>("Stickers");
				List<AccountWeaponStickers> list4 = new List<AccountWeaponStickers>();
				for (int m = 0; m < jsonObject4.Length; m++)
				{
					int result2 = -1;
					if (!int.TryParse(jsonObject4.GetKey(m), out result2))
					{
						continue;
					}
					JsonObject jsonObject5 = jsonObject4.Get<JsonObject>(jsonObject4.GetKey(m));
					AccountWeaponStickers accountWeaponStickers = new AccountWeaponStickers();
					accountWeaponStickers.SkinID = result2;
					for (int n = 0; n < jsonObject5.Length; n++)
					{
						int result3 = -1;
						if (int.TryParse(jsonObject5.GetKey(n), out result3))
						{
							AccountWeaponStickerData accountWeaponStickerData = new AccountWeaponStickerData();
							accountWeaponStickerData.Index = result3;
							accountWeaponStickerData.StickerID = jsonObject5.Get<int>(jsonObject5.GetKey(n));
							accountWeaponStickers.StickerData.Add(accountWeaponStickerData);
						}
					}
					if (accountWeaponStickers.StickerData.Count != 0)
					{
						list4.Add(accountWeaponStickers);
					}
				}
				accountWeapon.Stickers = list4;
				accountWeapon.SortWeaponStickers();
				for (int num2 = 0; num2 < GameSettings.instance.WeaponsStore[(int)accountWeapon.ID - 1].Skins.Count; num2++)
				{
					accountWeapon.FireStats.Add(-1);
				}
				JsonObject jsonObject6 = jsonObject3.Get<JsonObject>("FireStats");
				for (int num3 = 0; num3 < jsonObject6.Length; num3++)
				{
					int result4 = 0;
					int num4 = jsonObject6.Get<int>(jsonObject6.GetKey(num3));
					int.TryParse(jsonObject6.GetKey(num3), out result4);
					if (result4 == 0)
					{
						continue;
					}
					if (accountWeapon.FireStats.Count > result4)
					{
						accountWeapon.FireStats[result4] = num4;
						continue;
					}
					for (int num5 = accountWeapon.FireStats.Count - 1; num5 < result4; num5++)
					{
						accountWeapon.FireStats.Add(-1);
					}
					accountWeapon.FireStats[accountWeapon.FireStats.Count - 1] = num4;
				}
				accountWeapon.SelectedSkin = jsonObject3.Get<int>("SelectedSkin");
			}
			else
			{
				AccountWeapon accountWeapon2 = new AccountWeapon();
				accountWeapon2.ID = k + 1;
				accountWeapon = accountWeapon2;
			}
			accountData.Weapons.Add(accountWeapon);
		}
		List<string> list5 = jsonObject.Get<List<string>>("InAppPurchase");
		for (int num6 = 0; num6 < list5.Count; num6++)
		{
			accountData.InAppPurchase.Add(list5[num6]);
		}
		accountData.AndroidID = jsonObject.Get<string>("AndroidID");
		accountData.Session = jsonObject.Get<int>("Session");
		return accountData;
	}

	public static JsonObject CompareDefaultValue(AccountData defaultData, AccountData data)
	{
		JsonObject jsonObject = new JsonObject();
		if ((int)defaultData.GameVersion != ToInt(VersionManager.bundleVersion))
		{
			data.GameVersion = ToInt(VersionManager.bundleVersion);
			jsonObject.Add("GameVersion", (int)data.GameVersion);
		}
		if ((int)defaultData.Money != (int)data.Money)
		{
			jsonObject.Add("Money", (int)data.Money);
		}
		if ((int)defaultData.Gold != (int)data.Gold)
		{
			jsonObject.Add("Gold", (int)data.Gold);
		}
		if ((int)defaultData.XP != (int)data.XP)
		{
			jsonObject.Add("LevelXP", (int)data.XP);
		}
		if ((int)defaultData.Level != (int)data.Level)
		{
			jsonObject.Add("PlayerLevel", (int)data.Level);
		}
		if ((int)defaultData.OpenCase != (int)data.OpenCase)
		{
			jsonObject.Add("OpenCase", (int)data.OpenCase);
		}
		if ((int)defaultData.Deaths != (int)data.Deaths)
		{
			jsonObject.Add("Deaths", (int)data.Deaths);
		}
		if ((int)defaultData.Kills != (int)data.Kills)
		{
			jsonObject.Add("Kills", (int)data.Kills);
		}
		if ((int)defaultData.Headshot != (int)data.Headshot)
		{
			jsonObject.Add("Headshot", (int)data.Headshot);
		}
		if ((int)defaultData.SelectedRifle != (int)data.SelectedRifle)
		{
			jsonObject.Add("SelectedRifle", (int)data.SelectedRifle);
		}
		if ((int)defaultData.SelectedPistol != (int)data.SelectedPistol)
		{
			jsonObject.Add("SelectedPistol", (int)data.SelectedPistol);
		}
		if ((int)defaultData.SelectedKnife != (int)data.SelectedKnife)
		{
			jsonObject.Add("SelectedKnife", (int)data.SelectedKnife);
		}
		if ((int)defaultData.SelectedPlayerSkin != (int)data.SelectedPlayerSkin)
		{
			jsonObject.Add("SelectedPlayerSkin", (int)data.SelectedPlayerSkin);
		}
		if ((string)defaultData.Clan != (string)data.Clan)
		{
			jsonObject.Add("Clan", (string)data.Clan);
		}
		if (defaultData.PlayerSkins.Count != data.PlayerSkins.Count)
		{
			List<int> list = new List<int>();
			for (int i = 0; i < data.PlayerSkins.Count; i++)
			{
				list.Add(data.PlayerSkins[i]);
			}
			jsonObject.Add("PlayerSkins", list);
		}
		data.SortStickers();
		defaultData.SortStickers();
		if (CheckStickers(defaultData, data))
		{
			JsonObject jsonObject2 = new JsonObject();
			for (int j = 0; j < data.Stickers.Count; j++)
			{
				jsonObject2.Add(data.Stickers[j].ID.ToString("D2"), (int)data.Stickers[j].Count);
			}
			jsonObject.Add("Stickers", jsonObject2);
		}
		if (defaultData.InAppPurchase.Count != data.InAppPurchase.Count)
		{
			List<string> list2 = new List<string>();
			for (int k = 0; k < data.InAppPurchase.Count; k++)
			{
				list2.Add(data.InAppPurchase[k]);
			}
			jsonObject.Add("InAppPurchase", list2);
		}
		if (defaultData.AndroidID != AndroidNativeFunctions.GetAndroidID())
		{
			data.AndroidID = AndroidNativeFunctions.GetAndroidID();
			jsonObject.Add("AndroidID", (string)data.AndroidID);
		}
		return jsonObject;
	}

	private static bool CheckStickers(AccountData defaultData, AccountData data)
	{
		if (defaultData.Stickers.Count != data.Stickers.Count)
		{
			return true;
		}
		for (int i = 0; i < data.Stickers.Count; i++)
		{
			if ((int)data.Stickers[i].ID != (int)defaultData.Stickers[i].ID)
			{
				return true;
			}
			if ((int)data.Stickers[i].Count != (int)defaultData.Stickers[i].Count)
			{
				return true;
			}
		}
		return false;
	}

	public static JsonObject CompareWeaponValue(AccountData defaultData, AccountData data)
	{
		JsonObject jsonObject = new JsonObject();
		Dictionary<string, JsonObject> dictionary = new Dictionary<string, JsonObject>();
		for (int i = 0; i < GameSettings.instance.Weapons.Count; i++)
		{
			AccountWeapon weaponData = GetWeaponData(GameSettings.instance.Weapons[i].ID, data);
			if ((bool)defaultData.Weapons[i].Buy != (bool)data.Weapons[i].Buy)
			{
				if (!dictionary.ContainsKey(i.ToString()))
				{
					dictionary.Add(i.ToString(), GetWeaponToJson(weaponData));
				}
			}
			else if ((int)defaultData.Weapons[i].Upgrade != (int)data.Weapons[i].Upgrade)
			{
				if (!dictionary.ContainsKey(i.ToString()))
				{
					dictionary.Add(i.ToString(), GetWeaponToJson(weaponData));
				}
			}
			else if ((int)defaultData.Weapons[i].SelectedSkin != (int)data.Weapons[i].SelectedSkin)
			{
				if (!dictionary.ContainsKey(i.ToString()))
				{
					dictionary.Add(i.ToString(), GetWeaponToJson(weaponData));
				}
			}
			else if (defaultData.Weapons[i].Skins.Count != data.Weapons[i].Skins.Count)
			{
				if (!dictionary.ContainsKey(i.ToString()))
				{
					dictionary.Add(i.ToString(), GetWeaponToJson(weaponData));
				}
			}
			else if (!dictionary.ContainsKey(i.ToString()))
			{
				if (CheckWeaponFireStats(defaultData.Weapons[i].FireStats, data.Weapons[i].FireStats))
				{
					dictionary.Add(i.ToString(), GetWeaponToJson(weaponData));
				}
				defaultData.Weapons[i].SortWeaponStickers();
				data.Weapons[i].SortWeaponStickers();
				if (CheckWeaponStickers(defaultData.Weapons[i].Stickers, data.Weapons[i].Stickers))
				{
					dictionary.Add(i.ToString(), GetWeaponToJson(weaponData));
				}
			}
		}
		if (dictionary.Count != 0)
		{
			jsonObject.Add("Weapons", dictionary);
		}
		return jsonObject;
	}

	private static JsonObject GetWeaponToJson(AccountWeapon weapon)
	{
		JsonObject jsonObject = new JsonObject();
		jsonObject.Add("ID", (int)weapon.ID);
		jsonObject.Add("Buy", (bool)weapon.Buy);
		jsonObject.Add("Upgrade", (int)weapon.Upgrade);
		jsonObject.Add("SelectedSkin", (int)weapon.SelectedSkin);
		List<int> list = new List<int>();
		for (int i = 0; i < weapon.Skins.Count; i++)
		{
			list.Add(weapon.Skins[i]);
		}
		jsonObject.Add("Skins", list);
		JsonObject jsonObject2 = new JsonObject();
		for (int j = 0; j < weapon.Stickers.Count; j++)
		{
			JsonObject jsonObject3 = new JsonObject();
			for (int k = 0; k < weapon.Stickers[j].StickerData.Count; k++)
			{
				jsonObject3.Add(weapon.Stickers[j].StickerData[k].Index.ToString("D2"), (int)weapon.Stickers[j].StickerData[k].StickerID);
			}
			jsonObject2.Add(weapon.Stickers[j].SkinID.ToString("D2"), jsonObject3);
		}
		jsonObject.Add("Stickers", jsonObject2);
		JsonObject jsonObject4 = new JsonObject();
		for (int l = 0; l < weapon.FireStats.Count; l++)
		{
			if ((int)weapon.FireStats[l] != -1)
			{
				jsonObject4.Add(l.ToString("D2"), (int)weapon.FireStats[l]);
			}
		}
		jsonObject.Add("FireStats", jsonObject4);
		return jsonObject;
	}

	private static bool CheckWeaponStickers(List<AccountWeaponStickers> defaultData, List<AccountWeaponStickers> data)
	{
		if (defaultData.Count != data.Count)
		{
			for (int i = 0; i < data.Count; i++)
			{
				if (data[i].StickerData.Count == 0)
				{
					data.RemoveAt(i);
					i = -1;
				}
			}
			if (defaultData.Count != data.Count)
			{
				return true;
			}
		}
		for (int j = 0; j < data.Count; j++)
		{
			if ((int)data[j].SkinID != (int)defaultData[j].SkinID)
			{
				return true;
			}
			if (data[j].StickerData.Count != defaultData[j].StickerData.Count)
			{
				return true;
			}
			for (int k = 0; k < data[j].StickerData.Count; k++)
			{
				if ((int)data[j].StickerData[k].Index != (int)defaultData[j].StickerData[k].Index)
				{
					return true;
				}
				if ((int)data[j].StickerData[k].StickerID != (int)defaultData[j].StickerData[k].StickerID)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool CheckWeaponFireStats(List<CryptoInt> defaultData, List<CryptoInt> data)
	{
		if (defaultData.Count != data.Count)
		{
			return true;
		}
		for (int i = 0; i < data.Count; i++)
		{
			if ((int)data[i] != (int)defaultData[i])
			{
				return true;
			}
		}
		return false;
	}

	public static int ToInt(string value)
	{
		string text = string.Empty;
		for (int i = 0; i < value.Length; i++)
		{
			if (char.IsDigit(value[i]))
			{
				text += value[i];
			}
		}
		return int.Parse(text);
	}

	public static string ConvertAccountID(string id, bool encrypt)
	{
		string text = string.Empty;
		id = id.ToLower();
		bool flag = false;
		if (encrypt)
		{
			for (int i = 0; i < id.Length; i++)
			{
				flag = false;
				for (int j = 0; j < a.Count; j++)
				{
					if (id[i].ToString() == a[j])
					{
						text += b[j];
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					text += id[i];
				}
			}
			return text.ToUpper();
		}
		for (int k = 0; k < id.Length; k++)
		{
			flag = false;
			for (int l = 0; l < b.Count; l++)
			{
				if (id[k].ToString() == b[l])
				{
					text += a[l];
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				text += id[k];
			}
		}
		return text;
	}
}
