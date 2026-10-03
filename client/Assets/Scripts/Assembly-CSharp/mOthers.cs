using FreeJSON;
using UnityEngine;

public class mOthers : MonoBehaviour
{
	public GameObject InAppPanel;

	private bool isExit;

	private static bool sendFirebase;

	private void Start()
	{
		Application.targetFrameRate = 60;
		InputManager.Init();
		if (PlayerPrefs.HasKey("LeaveRoomText"))
		{
			mPopUp.ShowText(PlayerPrefs.GetString("LeaveRoomText"), 3f, "Menu");
			PlayerPrefs.DeleteKey("LeaveRoomText");
		}
		AchievementsManager.UpdateMoney();
		WeaponManager.Init();
		UISelectWeapon.AllWeapons = false;
		UISelectWeapon.SelectedUpdateWeaponManager = false;
		TimerManager.In(1f, () =>
		{
			isExit = true;
		});
		CheckFireStats();
		CheckData();
		LocalNotification.CancelNotification(1);
		LocalNotification.SendNotification(1, 259200L, "Block Strike", Localization.Get("NotificationText"), Color.red, true, false, false, "app_icon");
	}

	private void CheckFireStats()
	{
		if (!AccountManager.isConnect)
		{
			return;
		}
		for (int i = 0; i < GameSettings.instance.WeaponsStore.Count; i++)
		{
			for (int j = 0; j < GameSettings.instance.WeaponsStore[i].Skins.Count; j++)
			{
				if (AccountManager.GetFireStat(i + 1, GameSettings.instance.WeaponsStore[i].Skins[j].ID))
				{
					WeaponSkinQuality quality = GameSettings.instance.WeaponsStore[i].Skins[j].Quality;
					if (quality == WeaponSkinQuality.Default || quality == WeaponSkinQuality.Normal || (int)GameSettings.instance.WeaponsStore[i].Skins[j].Price != 0)
					{
						AccountManager.SetFireStatCounter(i + 1, GameSettings.instance.WeaponsStore[i].Skins[j].ID, -1);
					}
				}
			}
		}
	}

	private void CheckData()
	{
		if (((bool)AccountManager.isConnect || sendFirebase) && (AccountManager.GetMoney() >= 300000 || AccountManager.GetGold() >= 7000 || AccountManager.GetLevel() >= 200))
		{
			Firebase firebase = new Firebase();
			JsonObject jsonObject = new JsonObject();
			jsonObject.Add("level", AccountManager.GetLevel());
			jsonObject.Add("money", AccountManager.GetMoney());
			jsonObject.Add("gold", AccountManager.GetGold());
			jsonObject.Add("androidID", AndroidNativeFunctions.GetAndroidID());
			firebase.Child("Players").Child("CheckPlayers").Child(AccountManager.AccountID)
				.SetValue(jsonObject.ToString());
			sendFirebase = true;
		}
	}

	private void Update()
	{
		if (!isExit && Input.GetKeyDown(KeyCode.Escape))
		{
			Application.Quit();
		}
	}

	public void ExitGame()
	{
		if (isExit)
		{
			mPopUp.ShowPopup(Localization.Get("ExitText"), Localization.Get("Exit"), Localization.Get("Yes"), () =>
			{
				Application.Quit();
			}, Localization.Get("No"), () =>
			{
				mPopUp.HideAll("Menu");
			});
		}
	}

	public void ShowOthersGames()
	{
		Application.OpenURL("https://play.google.com/store/apps/dev?id=6363329851677974248");
	}

	public void ComingSoon()
	{
		UIToast.Show(Localization.Get("Coming soon"));
	}
}
