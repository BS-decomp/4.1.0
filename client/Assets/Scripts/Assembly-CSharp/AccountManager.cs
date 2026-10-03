using System;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using FreeJSON;
using UnityEngine;

public class AccountManager : MonoBehaviour
{
	public AccountData Data = new AccountData();

	public AccountData DefaultData = new AccountData();

	public static CryptoBool isConnect = false;

	public static CryptoString AccountID;

	public static CryptoString AccountToken;

	private static AccountManager instance;

	public static string PlayerID
	{
		get
		{
			return AccountConvert.ConvertAccountID(AccountID, true);
		}
	}

	public static string AccountName
	{
		get
		{
			return instance.Data.AccountName;
		}
		set
		{
			instance.Data.AccountName = value;
		}
	}

	private static string AccountParent
	{
		get
		{
			return ((string)AccountID).ToUpper()[0].ToString();
		}
	}

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public static void Init()
	{
		GameObject gameObject = new GameObject("AccountManager");
		gameObject.AddComponent<AccountManager>();
	}

	public static void Login(Action<bool> complete, Action<string> failed)
	{
		Login(AccountID, complete, failed);
	}

	public static void Login(string id, Action<bool> complete, Action<string> failed)
	{
		AccountID = id;
		if (string.IsNullOrEmpty(AccountToken))
		{
			Loom.RunAsync(() =>
			{
				AccountToken = new FirebaseToken(AesEncryptor.DecryptString("+mlNKsj7XuPKZwdMClu578HutQtE0ILq4zKYIQCKCl0DbUObj8EMYCPVkvEXnAcejgGyxYwderbeUEIBGzSiGQ==")).CreateToken(AccountID);
				Loom.QueueOnMainThread(() =>
				{
					LoginNext(complete, failed);
				});
			});
		}
		else
		{
			LoginNext(complete, failed);
		}
	}

	private static void LoginNext(Action<bool> complete, Action<string> failed)
	{
		Firebase firebase = new Firebase();
		firebase.Auth = AccountToken;
		firebase.Child("Players").Child("Accounts").Child(AccountParent)
			.Child(AccountID)
			.GetValue((string result) =>
			{
				if (result == "null")
				{
					complete(false);
					isConnect = false;
				}
				else
				{
					instance.DefaultData = AccountConvert.Deserialize(result);
					instance.Data = AccountConvert.Deserialize(result);
					if (CheckVersion())
					{
						complete(true);
						isConnect = true;
						UpdateLastLogin();
						UpdateSession();
					}
					else
					{
						failed("Old Version");
						isConnect = false;
					}
				}
			}, (string error) =>
			{
				firebase = new Firebase();
				firebase.Child("Players").Child("AccountsBans").GetValue(FirebaseParam.Default.OrderByKey().EqualTo(AccountID), (string result) =>
				{
					JsonObject jsonObject = JsonObject.Parse(result);
					if (jsonObject.Length != 0)
					{
						failed(jsonObject.Get<string>(AccountID));
					}
					else
					{
						failed(error);
					}
				}, (string text) =>
				{
					failed(error);
				});
				isConnect = false;
			});
	}

	public static bool CheckVersion()
	{
		if (AccountConvert.ToInt(VersionManager.bundleVersion) >= (int)instance.Data.GameVersion)
		{
			return true;
		}
		return false;
	}

	public static void Register(Action complete, Action<string> failed)
	{
		AccountData accountData = new AccountData();
		accountData.Weapons.Add(new AccountWeapon
		{
			ID = 12,
			Buy = true
		});
		accountData.Weapons.Add(new AccountWeapon
		{
			ID = 4,
			Buy = true
		});
		accountData.Weapons.Add(new AccountWeapon
		{
			ID = 3,
			Buy = true
		});
		accountData.GameVersion = AccountConvert.ToInt(VersionManager.bundleVersion);
		accountData.AndroidID = AndroidNativeFunctions.GetAndroidID();
		Register(accountData, complete, failed);
	}

	public static void Register(AccountData data, Action complete, Action<string> failed)
	{
		string json = AccountConvert.Serialize(data, true);
		Firebase firebase = new Firebase();
		firebase.Auth = AccountToken;
		firebase.Child("Players").Child("Accounts").Child(AccountParent)
			.Child(AccountID)
			.UpdateValue(json, (string result) =>
			{
				instance.DefaultData = AccountConvert.Deserialize(result);
				instance.Data = AccountConvert.Deserialize(result);
				complete();
				isConnect = true;
				UpdateLastLogin();
				UpdateSession();
			}, (string error) =>
			{
				failed(error);
				isConnect = false;
			});
	}

	public static void UpdateName(string newName, Action<string> complete, Action<string> failed)
	{
		Firebase firebase = new Firebase();
		firebase.Auth = AccountToken;
		string oldName = instance.Data.AccountName;
		string parentNew = newName.ToUpper()[0].ToString();
		string parentOld = string.Empty;
		if (!string.IsNullOrEmpty(oldName))
		{
			parentOld = oldName.ToUpper()[0].ToString();
		}
		firebase.Child("Players").Child("NickNames").Child(parentNew)
			.Child(newName)
			.GetValue((string result) =>
			{
				bool flag = false;
				if (result != "null" && result != "{}")
				{
					flag = true;
				}
				if (flag)
				{
					failed("Name already taken");
				}
				else
				{
					JsonObject json = new JsonObject();
					json.Add("AccountName", newName);
					firebase = new Firebase();
					firebase.Auth = AccountToken;
					firebase.Child("Players").Child("Accounts").Child(AccountParent)
						.Child(AccountID)
						.UpdateValue(json.ToString(), (string text) =>
						{
							instance.Data.AccountName = newName;
							complete(newName);
							firebase = new Firebase();
							firebase.Auth = AccountToken;
							json = new JsonObject();
							json.Add(newName, (string)AccountID);
							firebase.Child("Players").Child("NickNames").Child(parentNew)
								.UpdateValue(json.ToString(), (string text2) =>
								{
									if (ObscuredPrefs.GetBool("CheckNickName") && !string.IsNullOrEmpty(parentOld))
									{
										firebase = new Firebase();
										firebase.Auth = AccountToken;
										firebase.Child("Players").Child("NickNames").Child(parentOld)
											.Child(oldName)
											.Delete();
									}
									ObscuredPrefs.SetBool("CheckNickName", true);
								}, (string error) =>
								{
									if (ObscuredPrefs.GetBool("CheckNickName") && !string.IsNullOrEmpty(parentOld))
									{
										firebase = new Firebase();
										firebase.Auth = AccountToken;
										firebase.Child("Players").Child("NickNames").Child(parentOld)
											.Child(oldName)
											.Delete();
									}
									ObscuredPrefs.SetBool("CheckNickName", true);
								});
						}, (string error) =>
						{
							failed(error);
						});
				}
			}, (string error) =>
			{
				failed(error);
			});
	}

	public static void UpdateLastLogin()
	{
		if ((bool)isConnect)
		{
			Firebase firebase = new Firebase();
			firebase.Auth = AccountToken;
			JsonObject jsonObject = new JsonObject();
			jsonObject.Add(GetLevel().ToString(), JsonObject.Parse(Firebase.GetTimeStamp()));
			firebase.Child("Players").Child("LastLogin").Child(AccountParent)
				.Child(AccountID)
				.SetValue(jsonObject.ToString(), (string result) =>
				{
					long result2 = 0L;
					long.TryParse(JsonObject.Parse(result).Get<string>(GetLevel().ToString()), out result2);
					ObscuredPrefs.SetLong("LastLogin", result2);
				}, null);
		}
	}

	public static void UpdateSession()
	{
		Firebase firebase = new Firebase();
		firebase.Auth = AccountToken;
		JsonObject jsonObject = new JsonObject();
		jsonObject.Add("Session", (int)instance.Data.Session + 1);
		firebase.Child("Players").Child("Accounts").Child(AccountParent)
			.Child(AccountID)
			.UpdateValue(jsonObject.ToString(), (string result) =>
			{
				AccountData data = instance.Data;
				data.Session = (int)data.Session + 1;
			}, null);
	}

	public static void CheckSession(Action<bool> action)
	{
		Firebase firebase = new Firebase();
		firebase.Auth = AccountToken;
		firebase.Child("Players").Child("Accounts").Child(AccountParent)
			.Child(AccountID)
			.Child("Session")
			.GetValue((string result) =>
			{
				if (result == instance.Data.Session.ToString())
				{
					action(true);
				}
				else
				{
					action(false);
					if ((bool)isConnect)
					{
						UIToast.Show(Localization.Get("Session is already outdated"));
						UIToast.AddQueue(Localization.Get("Restart the game"));
						isConnect = false;
						PhotonNetwork.Disconnect();
					}
				}
			}, (string error) =>
			{
				action(false);
				if ((bool)isConnect)
				{
					UIToast.Show(Localization.Get("Session is already outdated"));
					UIToast.AddQueue(Localization.Get("Restart the game"));
					isConnect = false;
					PhotonNetwork.Disconnect();
				}
			});
	}

	public static void UpdateDefaultData(Action<bool> complete, Action<string> failed)
	{
		JsonObject json = AccountConvert.CompareDefaultValue(instance.DefaultData, instance.Data);
		if (json.Length == 0)
		{
			return;
		}
		if (complete != null)
		{
			complete(false);
		}
		CheckSession((bool session) =>
		{
			if (session && (bool)isConnect)
			{
				AccountData data = AccountConvert.Copy(instance.Data);
				Firebase firebase = new Firebase();
				firebase.Auth = AccountToken;
				firebase.Child("Players").Child("Accounts").Child(AccountParent)
					.Child(AccountID)
					.UpdateValue(json.ToString(), (string result) =>
					{
						AccountConvert.CopyDefaultValue(data, instance.DefaultData);
						if (complete != null)
						{
							complete(true);
						}
					}, (string error) =>
					{
						if (failed != null)
						{
							failed(error);
						}
					});
			}
		});
	}

	public static void UpdateWeaponsData(Action<bool> complete, Action<string> failed)
	{
		JsonObject json = AccountConvert.CompareWeaponValue(instance.DefaultData, instance.Data);
		if (json.Length == 0)
		{
			return;
		}
		if (complete != null)
		{
			complete(false);
		}
		CheckSession((bool session) =>
		{
			if (session && (bool)isConnect)
			{
				AccountData data = AccountConvert.Copy(instance.Data);
				Firebase firebase = new Firebase();
				firebase.Auth = AccountToken;
				firebase.Child("Players").Child("Accounts").Child(AccountParent)
					.Child(AccountID)
					.Child("Weapons")
					.UpdateValue(json.Get<string>("Weapons"), (string result) =>
					{
						AccountConvert.CopyWeaponsValue(data, instance.DefaultData);
						if (complete != null)
						{
							complete(true);
						}
					}, (string error) =>
					{
						if (failed != null)
						{
							failed(error);
						}
					});
			}
		});
	}

	public static void UpdateDefaultAndWeaponsData()
	{
		JsonObject json = AccountConvert.CompareDefaultValue(instance.DefaultData, instance.Data);
		JsonObject json2 = AccountConvert.CompareWeaponValue(instance.DefaultData, instance.Data);
		if (json.Length == 0 && json2.Length == 0)
		{
			return;
		}
		CheckSession((bool session) =>
		{
			if (session && (bool)isConnect)
			{
				if (json.Length != 0)
				{
					AccountData data = AccountConvert.Copy(instance.Data);
					Firebase firebase = new Firebase();
					firebase.Auth = AccountToken;
					firebase.Child("Players").Child("Accounts").Child(AccountParent)
						.Child(AccountID)
						.UpdateValue(json.ToString(), (string result) =>
						{
							AccountConvert.CopyDefaultValue(data, instance.DefaultData);
						}, null);
				}
				if (json2.Length != 0)
				{
					TimerManager.In(0.2f, () =>
					{
						AccountData data2 = AccountConvert.Copy(instance.Data);
						Firebase firebase2 = new Firebase();
						firebase2.Auth = AccountToken;
						firebase2.Child("Players").Child("Accounts").Child(AccountParent)
							.Child(AccountID)
							.Child("Weapons")
							.UpdateValue(json2.Get<string>("Weapons"), (string result) =>
							{
								AccountConvert.CopyWeaponsValue(data2, instance.DefaultData);
							}, null);
					});
				}
			}
		});
	}

	public static void SetMoney(int money)
	{
		SetMoney(money, false, null, null);
	}

	public static void SetMoney(int money, bool update)
	{
		SetMoney(money, update, null, null);
	}

	public static void SetMoney(int money, bool update, Action<bool> complete, Action<string> failed)
	{
		instance.Data.Money = money;
		if (update)
		{
			UpdateDefaultData(complete, failed);
		}
	}

	public static void SetMoney1(int money)
	{
		SetMoney1(money, false, null, null);
	}

	public static void SetMoney1(int money, bool update)
	{
		SetMoney1(money, update, null, null);
	}

	public static void SetMoney1(int money, bool update, Action<bool> complete, Action<string> failed)
	{
		SetMoney(GetMoney() + money, update, complete, failed);
	}

	public static int GetMoney()
	{
		return instance.Data.Money;
	}

	public static void SetGold(int gold)
	{
		SetGold(gold, false, null, null);
	}

	public static void SetGold(int gold, bool update)
	{
		SetGold(gold, update, null, null);
	}

	public static void SetGold(int gold, bool update, Action<bool> complete, Action<string> failed)
	{
		instance.Data.Gold = gold;
		if (update)
		{
			UpdateDefaultData(complete, failed);
		}
	}

	public static void SetGold1(int gold)
	{
		SetGold1(gold, false, null, null);
	}

	public static void SetGold1(int gold, bool update)
	{
		SetGold1(gold, update, null, null);
	}

	public static void SetGold1(int gold, bool update, Action<bool> complete, Action<string> failed)
	{
		SetGold(GetGold() + gold, update, complete, failed);
	}

	public static int GetGold()
	{
		return instance.Data.Gold;
	}

	public static void SetXP(int xp)
	{
		SetXP(xp, false, null, null);
	}

	public static void SetXP(int xp, bool update)
	{
		SetXP(xp, update, null, null);
	}

	public static void SetXP(int xp, bool update, Action<bool> complete, Action<string> failed)
	{
		instance.Data.XP = xp;
		if (update)
		{
			UpdateDefaultData(complete, failed);
		}
	}

	public static void SetXP1(int xp)
	{
		int level = GetLevel();
		int num = GetXP() + xp;
		int maxXP = GetMaxXP();
		if (num >= maxXP)
		{
			if (level == 250)
			{
				num = maxXP;
				maxXP = 0;
			}
			else
			{
				level++;
				num -= maxXP;
				num = Mathf.Max(num, 0);
				maxXP = 150 + 150 * level;
				SetLevel(level);
				if (LevelManager.GetSceneName() == "Menu")
				{
					UIToast.Show(Localization.Get("New Level") + " " + level);
					SetMoney1(500);
					SetGold1(10);
					AchievementsManager.UpdateLevel();
				}
			}
		}
		SetXP(num);
	}

	public static int GetXP()
	{
		if (GetLevel() == 250)
		{
			return GetMaxXP();
		}
		return instance.Data.XP;
	}

	public static int GetMaxXP()
	{
		return 150 + 150 * GetLevel();
	}

	public static void SetLevel(int level)
	{
		SetLevel(level, false, null, null);
	}

	public static void SetLevel(int level, bool update)
	{
		SetLevel(level, update, null, null);
	}

	public static void SetLevel(int level, bool update, Action<bool> complete, Action<string> failed)
	{
		instance.Data.Level = level;
		if (update)
		{
			UpdateDefaultData(complete, failed);
		}
	}

	public static int GetLevel()
	{
		return instance.Data.Level;
	}

	public static void SetOpenCase(int count)
	{
		SetOpenCase(count, false, null, null);
	}

	public static void SetOpenCase(int count, bool update)
	{
		SetOpenCase(count, update, null, null);
	}

	public static void SetOpenCase(int count, bool update, Action<bool> complete, Action<string> failed)
	{
		instance.Data.OpenCase = count;
		if (update)
		{
			UpdateDefaultData(complete, failed);
		}
	}

	public static void SetOpenCase1()
	{
		SetOpenCase1(false, null, null);
	}

	public static void SetOpenCase1(bool update)
	{
		SetOpenCase1(update, null, null);
	}

	public static void SetOpenCase1(bool update, Action<bool> complete, Action<string> failed)
	{
		SetOpenCase(GetOpenCase() + 1, update, complete, failed);
	}

	public static int GetOpenCase()
	{
		return instance.Data.OpenCase;
	}

	public static void SetInAppPurchase(string purchase)
	{
		SetInAppPurchase(purchase, false, null, null);
	}

	public static void SetInAppPurchase(string purchase, bool update)
	{
		SetInAppPurchase(purchase, update, null, null);
	}

	public static void SetInAppPurchase(string purchase, bool update, Action<bool> complete, Action<string> failed)
	{
		instance.Data.InAppPurchase.Add(purchase);
		if (update)
		{
			UpdateDefaultData(complete, failed);
		}
	}

	public static List<string> GetInAppPurchase()
	{
		List<string> list = new List<string>();
		for (int i = 0; i < instance.Data.InAppPurchase.Count; i++)
		{
			list.Add(instance.Data.InAppPurchase[i]);
		}
		return list;
	}

	public static void SetDeaths(int deaths)
	{
		SetDeaths(deaths, false, null, null);
	}

	public static void SetDeaths(int deaths, bool update)
	{
		SetDeaths(deaths, update, null, null);
	}

	public static void SetDeaths(int deaths, bool update, Action<bool> complete, Action<string> failed)
	{
		instance.Data.Deaths = deaths;
		if (update)
		{
			UpdateDefaultData(complete, failed);
		}
	}

	public static void SetDeaths1(int deaths)
	{
		SetDeaths1(deaths, false, null, null);
	}

	public static void SetDeaths1(int deaths, bool update)
	{
		SetDeaths1(deaths, update, null, null);
	}

	public static void SetDeaths1(int deaths, bool update, Action<bool> complete, Action<string> failed)
	{
		SetDeaths(GetDeaths() + deaths, update, complete, failed);
	}

	public static int GetDeaths()
	{
		return instance.Data.Deaths;
	}

	public static void SetKills(int kills)
	{
		SetKills(kills, false, null, null);
	}

	public static void SetKills(int kills, bool update)
	{
		SetKills(kills, update, null, null);
	}

	public static void SetKills(int kills, bool update, Action<bool> complete, Action<string> failed)
	{
		instance.Data.Kills = kills;
		if (update)
		{
			UpdateDefaultData(complete, failed);
		}
	}

	public static void SetKills1(int kills)
	{
		SetKills1(kills, false, null, null);
	}

	public static void SetKills1(int kills, bool update)
	{
		SetKills1(kills, update, null, null);
	}

	public static void SetKills1(int kills, bool update, Action<bool> complete, Action<string> failed)
	{
		SetKills(GetKills() + kills, update, complete, failed);
	}

	public static int GetKills()
	{
		return instance.Data.Kills;
	}

	public static void SetHeadshot(int headshot)
	{
		SetHeadshot(headshot, false, null, null);
	}

	public static void SetHeadshot(int headshot, bool update)
	{
		SetHeadshot(headshot, update, null, null);
	}

	public static void SetHeadshot(int headshot, bool update, Action<bool> complete, Action<string> failed)
	{
		instance.Data.Headshot = headshot;
		if (update)
		{
			UpdateDefaultData(complete, failed);
		}
	}

	public static void SetHeadshot1(int headshot)
	{
		SetHeadshot1(headshot, false, null, null);
	}

	public static void SetHeadshot1(int headshot, bool update)
	{
		SetHeadshot1(headshot, update, null, null);
	}

	public static void SetHeadshot1(int headshot, bool update, Action<bool> complete, Action<string> failed)
	{
		SetHeadshot(GetHeadshot() + headshot, update, complete, failed);
	}

	public static int GetHeadshot()
	{
		return instance.Data.Headshot;
	}

	public static void SetWeaponSelected(WeaponType weaponType, int weaponID)
	{
		SetWeaponSelected(weaponType, weaponID, false, null, null);
	}

	public static void SetWeaponSelected(WeaponType weaponType, int weaponID, bool update)
	{
		SetWeaponSelected(weaponType, weaponID, update, null, null);
	}

	public static void SetWeaponSelected(WeaponType weaponType, int weaponID, bool update, Action<bool> complete, Action<string> failed)
	{
		switch (weaponType)
		{
		case WeaponType.Knife:
			instance.Data.SelectedKnife = weaponID;
			break;
		case WeaponType.Pistol:
			instance.Data.SelectedPistol = weaponID;
			break;
		case WeaponType.Rifle:
			instance.Data.SelectedRifle = weaponID;
			break;
		}
		if (update)
		{
			UpdateDefaultData(complete, failed);
		}
	}

	public static int GetWeaponSelected(WeaponType weaponType)
	{
		switch (weaponType)
		{
		case WeaponType.Knife:
			return instance.Data.SelectedKnife;
		case WeaponType.Pistol:
			return instance.Data.SelectedPistol;
		case WeaponType.Rifle:
			return instance.Data.SelectedRifle;
		default:
			return 0;
		}
	}

	public static void SetPlayerSkinSelected(int id)
	{
		SetPlayerSkinSelected(id, false, null, null);
	}

	public static void SetPlayerSkinSelected(int id, bool update)
	{
		SetPlayerSkinSelected(id, update, null, null);
	}

	public static void SetPlayerSkinSelected(int id, bool update, Action<bool> complete, Action<string> failed)
	{
		instance.Data.SelectedPlayerSkin = id;
		if (update)
		{
			UpdateDefaultData(complete, failed);
		}
	}

	public static int GetPlayerSkinSelected()
	{
		return instance.Data.SelectedPlayerSkin;
	}

	public static void SetPlayerSkin(int id)
	{
		SetPlayerSkin(id, false, null, null);
	}

	public static void SetPlayerSkin(int id, bool update)
	{
		SetPlayerSkin(id, update, null, null);
	}

	public static void SetPlayerSkin(int id, bool update, Action<bool> complete, Action<string> failed)
	{
		if (!GetPlayerSkin(id))
		{
			instance.Data.PlayerSkins.Add(id);
			if (update)
			{
				UpdateDefaultData(complete, failed);
			}
		}
	}

	public static bool GetPlayerSkin(int id)
	{
		if (id == 0)
		{
			return true;
		}
		return instance.Data.PlayerSkins.Contains(id);
	}

	public static void SetWeapon(int id)
	{
		SetWeapon(id, false, null, null);
	}

	public static void SetWeapon(int id, bool update)
	{
		SetWeapon(id, update, null, null);
	}

	public static void SetWeapon(int id, bool update, Action<bool> complete, Action<string> failed)
	{
		for (int i = 0; i < instance.Data.Weapons.Count; i++)
		{
			if ((int)instance.Data.Weapons[i].ID == id)
			{
				instance.Data.Weapons[i].Buy = true;
				if (update)
				{
					UpdateWeaponsData(complete, failed);
				}
				break;
			}
		}
	}

	public static bool GetWeapon(int id)
	{
		for (int i = 0; i < instance.Data.Weapons.Count; i++)
		{
			if ((int)instance.Data.Weapons[i].ID == id)
			{
				return instance.Data.Weapons[i].Buy;
			}
		}
		return false;
	}

	public static void SetWeaponUpgrade(int id)
	{
		SetWeaponUpgrade(id, false, null, null);
	}

	public static void SetWeaponUpgrade(int id, bool update)
	{
		SetWeaponUpgrade(id, update, null, null);
	}

	public static void SetWeaponUpgrade(int id, bool update, Action<bool> complete, Action<string> failed)
	{
		for (int i = 0; i < instance.Data.Weapons.Count; i++)
		{
			if ((int)instance.Data.Weapons[i].ID == id)
			{
				AccountWeapon accountWeapon = instance.Data.Weapons[i];
				accountWeapon.Upgrade = (int)accountWeapon.Upgrade + 1;
				if (update)
				{
					UpdateWeaponsData(complete, failed);
				}
				break;
			}
		}
	}

	public static int GetWeaponUpgrade(int id)
	{
		for (int i = 0; i < instance.Data.Weapons.Count; i++)
		{
			if ((int)instance.Data.Weapons[i].ID == id)
			{
				return instance.Data.Weapons[i].Upgrade;
			}
		}
		return 0;
	}

	public static void SetWeaponSkin(int id, int skin)
	{
		SetWeaponSkin(id, skin, false, null, null);
	}

	public static void SetWeaponSkin(int id, int skin, bool update)
	{
		SetWeaponSkin(id, skin, update, null, null);
	}

	public static void SetWeaponSkin(int id, int skin, bool update, Action<bool> complete, Action<string> failed)
	{
		for (int i = 0; i < instance.Data.Weapons.Count; i++)
		{
			if ((int)instance.Data.Weapons[i].ID != id)
			{
				continue;
			}
			if (!instance.Data.Weapons[i].Skins.Contains(skin))
			{
				instance.Data.Weapons[i].Skins.Add(skin);
				if (update)
				{
					UpdateWeaponsData(complete, failed);
				}
			}
			break;
		}
	}

	public static bool GetWeaponSkin(int id, int skin)
	{
		if (skin == 0)
		{
			return true;
		}
		for (int i = 0; i < instance.Data.Weapons.Count; i++)
		{
			if ((int)instance.Data.Weapons[i].ID == id)
			{
				return instance.Data.Weapons[i].Skins.Contains(skin);
			}
		}
		return false;
	}

	public static void SetWeaponSkinSelected(int id, int skin)
	{
		SetWeaponSkinSelected(id, skin, false, null, null);
	}

	public static void SetWeaponSkinSelected(int id, int skin, bool update)
	{
		SetWeaponSkinSelected(id, skin, update, null, null);
	}

	public static void SetWeaponSkinSelected(int id, int skin, bool update, Action<bool> complete, Action<string> failed)
	{
		for (int i = 0; i < instance.Data.Weapons.Count; i++)
		{
			if ((int)instance.Data.Weapons[i].ID == id)
			{
				instance.Data.Weapons[i].SelectedSkin = skin;
				if (update)
				{
					UpdateWeaponsData(complete, failed);
				}
				break;
			}
		}
	}

	public static int GetWeaponSkinSelected(int id)
	{
		for (int i = 0; i < instance.Data.Weapons.Count; i++)
		{
			if ((int)instance.Data.Weapons[i].ID == id)
			{
				return instance.Data.Weapons[i].SelectedSkin;
			}
		}
		return 0;
	}

	public static void SetFireStat(int id, int skin)
	{
		SetFireStat(id, skin, false, null, null);
	}

	public static void SetFireStat(int id, int skin, bool update)
	{
		SetFireStat(id, skin, update, null, null);
	}

	public static void SetFireStat(int id, int skin, bool update, Action<bool> complete, Action<string> failed)
	{
		for (int i = 0; i < instance.Data.Weapons.Count; i++)
		{
			if ((int)instance.Data.Weapons[i].ID != id)
			{
				continue;
			}
			if (instance.Data.Weapons[i].FireStats.Count > skin)
			{
				if ((int)instance.Data.Weapons[i].FireStats[skin] < 0)
				{
					instance.Data.Weapons[i].FireStats[skin] = 0;
				}
			}
			else
			{
				for (int j = instance.Data.Weapons[i].FireStats.Count - 1; j < skin; j++)
				{
					instance.Data.Weapons[i].FireStats.Add(-1);
				}
				instance.Data.Weapons[i].FireStats[instance.Data.Weapons[i].FireStats.Count - 1] = 0;
			}
			if (update)
			{
				UpdateWeaponsData(complete, failed);
			}
			break;
		}
	}

	public static bool GetFireStat(int id, int skin)
	{
		for (int i = 0; i < instance.Data.Weapons.Count; i++)
		{
			if ((int)instance.Data.Weapons[i].ID == id)
			{
				if (instance.Data.Weapons[i].FireStats.Count > skin && (int)instance.Data.Weapons[i].FireStats[skin] != -1)
				{
					return true;
				}
				return false;
			}
		}
		return false;
	}

	public static void SetFireStatCounter(int id, int skin, int value)
	{
		SetFireStatCounter(id, skin, value, false, null, null);
	}

	public static void SetFireStatCounter(int id, int skin, int value, bool update)
	{
		SetFireStatCounter(id, skin, value, update, null, null);
	}

	public static void SetFireStatCounter(int id, int skin, int value, bool update, Action<bool> complete, Action<string> failed)
	{
		for (int i = 0; i < instance.Data.Weapons.Count; i++)
		{
			if ((int)instance.Data.Weapons[i].ID != id)
			{
				continue;
			}
			if (instance.Data.Weapons[i].FireStats.Count > skin)
			{
				instance.Data.Weapons[i].FireStats[skin] = value;
			}
			else
			{
				for (int j = instance.Data.Weapons[i].FireStats.Count - 1; j < skin; j++)
				{
					instance.Data.Weapons[i].FireStats.Add(-1);
				}
				instance.Data.Weapons[i].FireStats[instance.Data.Weapons[i].FireStats.Count - 1] = value;
			}
			if (update)
			{
				UpdateWeaponsData(complete, failed);
			}
			break;
		}
	}

	public static int GetFireStatCounter(int id, int skin)
	{
		if (skin == 0)
		{
			return -1;
		}
		for (int i = 0; i < instance.Data.Weapons.Count; i++)
		{
			if ((int)instance.Data.Weapons[i].ID == id)
			{
				if (instance.Data.Weapons[i].FireStats.Count > skin)
				{
					return instance.Data.Weapons[i].FireStats[skin];
				}
				return -1;
			}
		}
		return -1;
	}

	public static void SetClan(string clan)
	{
		SetClan(clan, false, null, null);
	}

	public static void SetClan(string clan, bool update)
	{
		SetClan(clan, update, null, null);
	}

	public static void SetClan(string clan, bool update, Action<bool> complete, Action<string> failed)
	{
		instance.Data.Clan = clan;
		if (update)
		{
			UpdateDefaultData(complete, failed);
		}
	}

	public static string GetClan()
	{
		return instance.Data.Clan;
	}

	public static void SetStickers(int id)
	{
		SetStickers(id, false, null, null);
	}

	public static void SetStickers(int id, bool update)
	{
		SetStickers(id, update, null, null);
	}

	public static void SetStickers(int id, bool update, Action<bool> complete, Action<string> failed)
	{
		if (GetStickers(id))
		{
			for (int i = 0; i < instance.Data.Stickers.Count; i++)
			{
				if ((int)instance.Data.Stickers[i].ID == id)
				{
					++instance.Data.Stickers[i].Count;
					break;
				}
			}
		}
		else
		{
			AccountSticker accountSticker = new AccountSticker();
			accountSticker.ID = id;
			accountSticker.Count = 1;
			instance.Data.Stickers.Add(accountSticker);
			instance.Data.SortStickers();
		}
		if (update)
		{
			UpdateDefaultData(complete, failed);
		}
	}

	public static void DeleteSticker(int id)
	{
		DeleteSticker(id, false, null, null);
	}

	public static void DeleteSticker(int id, bool update)
	{
		DeleteSticker(id, update, null, null);
	}

	public static void DeleteSticker(int id, bool update, Action<bool> complete, Action<string> failed)
	{
		if (GetStickers(id))
		{
			for (int i = 0; i < instance.Data.Stickers.Count; i++)
			{
				if ((int)instance.Data.Stickers[i].ID == id)
				{
					--instance.Data.Stickers[i].Count;
					if ((int)instance.Data.Stickers[i].Count == 0)
					{
						instance.Data.Stickers.RemoveAt(i);
						instance.Data.SortStickers();
					}
					break;
				}
			}
		}
		if (update)
		{
			UpdateDefaultData(complete, failed);
		}
	}

	public static bool GetStickers(int id)
	{
		for (int i = 0; i < instance.Data.Stickers.Count; i++)
		{
			if ((int)instance.Data.Stickers[i].ID == id)
			{
				return true;
			}
		}
		return false;
	}

	public static int[] GetStickers()
	{
		int[] array = new int[instance.Data.Stickers.Count];
		for (int i = 0; i < instance.Data.Stickers.Count; i++)
		{
			array[i] = instance.Data.Stickers[i].ID;
		}
		return array;
	}

	public static int GetStickerCount(int id)
	{
		for (int i = 0; i < instance.Data.Stickers.Count; i++)
		{
			if ((int)instance.Data.Stickers[i].ID == id)
			{
				return instance.Data.Stickers[i].Count;
			}
		}
		return 0;
	}

	public static void SetWeaponSticker(int weapon, int skin, int pos, int sticker)
	{
		SetWeaponSticker(weapon, skin, pos, sticker, false, null, null);
	}

	public static void SetWeaponSticker(int weapon, int skin, int pos, int sticker, bool update)
	{
		SetWeaponSticker(weapon, skin, pos, sticker, update, null, null);
	}

	public static void SetWeaponSticker(int weapon, int skin, int pos, int sticker, bool update, Action<bool> complete, Action<string> failed)
	{
		AccountWeaponStickers weaponStickers = GetWeaponStickers(weapon, skin);
		if (HasWeaponSticker(weapon, skin, pos))
		{
			for (int i = 0; i < weaponStickers.StickerData.Count; i++)
			{
				if ((int)weaponStickers.StickerData[i].Index == pos)
				{
					weaponStickers.StickerData[i].StickerID = sticker;
					break;
				}
			}
		}
		else
		{
			AccountWeaponStickerData accountWeaponStickerData = new AccountWeaponStickerData();
			accountWeaponStickerData.Index = pos;
			accountWeaponStickerData.StickerID = sticker;
			weaponStickers.StickerData.Add(accountWeaponStickerData);
			weaponStickers.SortWeaponStickerData();
		}
		if (update)
		{
			UpdateDefaultData(complete, failed);
		}
	}

	public static void DeleteWeaponSticker(int weapon, int skin, int pos)
	{
		DeleteWeaponSticker(weapon, skin, pos, false, null, null);
	}

	public static void DeleteWeaponSticker(int weapon, int skin, int pos, bool update)
	{
		DeleteWeaponSticker(weapon, skin, pos, update, null, null);
	}

	public static void DeleteWeaponSticker(int weapon, int skin, int pos, bool update, Action<bool> complete, Action<string> failed)
	{
		AccountWeaponStickers weaponStickers = GetWeaponStickers(weapon, skin);
		if (weaponStickers == null)
		{
			return;
		}
		for (int i = 0; i < weaponStickers.StickerData.Count; i++)
		{
			if ((int)weaponStickers.StickerData[i].Index == pos)
			{
				weaponStickers.StickerData.RemoveAt(i);
				weaponStickers.SortWeaponStickerData();
				break;
			}
		}
		if (update)
		{
			UpdateDefaultData(complete, failed);
		}
	}

	public static bool HasWeaponSticker(int weapon, int skin, int pos)
	{
		for (int i = 0; i < instance.Data.Weapons.Count; i++)
		{
			if ((int)instance.Data.Weapons[i].ID != weapon)
			{
				continue;
			}
			for (int j = 0; j < instance.Data.Weapons[i].Stickers.Count; j++)
			{
				if ((int)instance.Data.Weapons[i].Stickers[j].SkinID != skin)
				{
					continue;
				}
				for (int k = 0; k < instance.Data.Weapons[i].Stickers[j].StickerData.Count; k++)
				{
					if ((int)instance.Data.Weapons[i].Stickers[j].StickerData[k].Index == pos)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public static int GetWeaponSticker(int weapon, int skin, int pos)
	{
		for (int i = 0; i < instance.Data.Weapons.Count; i++)
		{
			if ((int)instance.Data.Weapons[i].ID != weapon)
			{
				continue;
			}
			for (int j = 0; j < instance.Data.Weapons[i].Stickers.Count; j++)
			{
				if ((int)instance.Data.Weapons[i].Stickers[j].SkinID != skin)
				{
					continue;
				}
				for (int k = 0; k < instance.Data.Weapons[i].Stickers[j].StickerData.Count; k++)
				{
					if ((int)instance.Data.Weapons[i].Stickers[j].StickerData[k].Index == pos)
					{
						return instance.Data.Weapons[i].Stickers[j].StickerData[k].StickerID;
					}
				}
			}
		}
		return -1;
	}

	public static AccountWeaponStickers GetWeaponStickers(int weapon, int skin)
	{
		for (int i = 0; i < instance.Data.Weapons.Count; i++)
		{
			if ((int)instance.Data.Weapons[i].ID != weapon)
			{
				continue;
			}
			for (int j = 0; j < instance.Data.Weapons[i].Stickers.Count; j++)
			{
				if ((int)instance.Data.Weapons[i].Stickers[j].SkinID == skin)
				{
					return instance.Data.Weapons[i].Stickers[j];
				}
			}
			AccountWeaponStickers accountWeaponStickers = new AccountWeaponStickers();
			accountWeaponStickers.SkinID = skin;
			instance.Data.Weapons[i].Stickers.Add(accountWeaponStickers);
			return instance.Data.Weapons[i].Stickers[instance.Data.Weapons[i].Stickers.Count - 1];
		}
		return null;
	}
}
