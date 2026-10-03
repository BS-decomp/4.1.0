using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AntiSpeedHack4Android;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

public class CheckManager : MonoBehaviour
{
	[Serializable]
	public class AppData
	{
		public string packageName;

		public string text;
	}

	public AppData[] CryptoData;

	public ObscuredString[] Permissions;

	private List<string> ggPackageName;

	private int serverFalsePositives;

	private int falsePositives;

	public static bool checkData = true;

	private void Start()
	{
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		AntiSpeedHack.detectListener = (OnDetectListener)Delegate.Combine(AntiSpeedHack.detectListener, new OnDetectListener(Quit));
		CheckPermission();
		OnApplicationFocus(true);
		CheckDevicesBans();
		if (AndroidEmulatorDetector.isEmulator())
		{
			AndroidNativeFunctions.ShowToast("Android Emulator Detected");
			TimerManager.In(1f, () =>
			{
				Application.Quit();
			});
		}
	}

	public void Quit()
	{
		if (PhotonNetwork.inRoom)
		{
			PhotonNetwork.LeaveRoom();
		}
		Application.Quit();
	}

	private void UpdateTime(double serverTime)
	{
		double time = PhotonNetwork.time;
		if (serverTime + 1.0 > time)
		{
			falsePositives = 0;
			if (serverTime - time >= 1.0)
			{
				serverFalsePositives++;
				if (serverFalsePositives >= 3)
				{
					GameManager.SetLeaveRoomText(Localization.Get("ServerAdminSpeedHack"));
					Quit();
				}
			}
		}
		else
		{
			falsePositives++;
			if (falsePositives >= 3)
			{
				Quit();
			}
		}
	}

	private void OnLevelWasLoaded(int level)
	{
		if (!PhotonNetwork.offlineMode && (bool)AccountManager.isConnect)
		{
			TimerManager.In(0.5f, () =>
			{
				EventManager.AddListener<double>("ServerTime", UpdateTime);
			});
		}
	}

	private void CheckDevicesBans()
	{
		if (Application.platform == RuntimePlatform.Android)
		{
			Firebase firebase = new Firebase();
			firebase.Child("Players").Child("DevicesBans").GetValue(FirebaseParam.Default.OrderByValue().EqualTo(AndroidNativeFunctions.GetAndroidID()), CheckDevicesBansSuccess, CheckDevicesBansFailed);
		}
	}

	private void CheckDevicesBansSuccess(string json)
	{
		if (json.Contains(AndroidNativeFunctions.GetAndroidID()))
		{
			AndroidNativeFunctions.ShowAlert("Your device is banned in the game.", "Block Strike", "OK", string.Empty, string.Empty, CheckDevicesBansOnClick);
			GameSettings.instance.PhotonID = string.Empty;
			TimerManager.In(0.1f, -1, 0.1f, () =>
			{
				AccountManager.isConnect = false;
			});
			TimerManager.In(20f, () =>
			{
				Application.Quit();
			});
		}
	}

	private void CheckDevicesBansFailed(string json)
	{
		CheckDevicesBans();
	}

	private void CheckDevicesBansOnClick(DialogInterface dialog)
	{
		if (dialog == DialogInterface.Positive)
		{
			Application.Quit();
		}
	}

	private void CheckPermission()
	{
		for (int i = 0; i < Permissions.Length; i++)
		{
			if (!AndroidNativeFunctions.CheckPermission("android.permission." + Permissions[i]))
			{
				AndroidNativeFunctions.ShowAlert("For the game to work need permission: " + Permissions[i], "Block Strike", "OK", string.Empty, string.Empty, CheckDevicesBansOnClick);
				Utils.SetActiveConsole(false);
				Logo.isLoad = false;
				GameSettings.instance.PhotonID = string.Empty;
				TimerManager.In(0.1f, -1, 0.1f, () =>
				{
					AccountManager.isConnect = false;
				});
				TimerManager.In(20f, () =>
				{
					Application.Quit();
				});
				break;
			}
		}
	}

	private void OnApplicationFocus(bool pause)
	{
		if (!pause)
		{
			return;
		}
		if (!checkData)
		{
			checkData = true;
			return;
		}
		TimerManager.In(0.2f, () =>
		{
			StopAllCoroutines();
			StartCoroutine(Test());
		});
	}

	private IEnumerator Test()
	{
		yield return new WaitForSeconds(0.1f);
		for (int i = 0; i < CryptoData.Length; i++)
		{
			yield return new WaitForSeconds(0.03f);
			string a = AesEncryptor.DecryptString(CryptoData[i].packageName);
			if (AndroidNativeFunctions.isInstalledApp(a))
			{
				string b = AesEncryptor.DecryptString(CryptoData[i].text);
				AndroidNativeFunctions.ShowToast(b);
				TimerManager.In(1f, () =>
				{
					Application.Quit();
				});
				yield return new WaitForSeconds(2f);
			}
			else
			{
				yield return new WaitForSeconds(0.2f);
			}
		}
		string a2 = AesEncryptor.DecryptString("4GX6r3wIP8Z/OH2FBfnm694X4sRJFA5BseJ+y3MasvsgiLnuBdjDrgA8L1GOiBZi");
		string a3 = AesEncryptor.DecryptString("bkRLBNB/++jAUd9bG2uAu0Cn0Ur7d2xcqK8h19RWPGjT2uL2/sqoaxRlIJ4kWXr1");
		string a4 = AesEncryptor.DecryptString("m9PpsQYLpnCHcUzm/u2NvaySqPmDSjb6G5frBYZ3QIU=");
		yield return new WaitForSeconds(0.02f);
		string b2 = AesEncryptor.DecryptString("u62v4Skbxm8EimNSBstNMxN0ymaaQF5gF2HZk6prfwU=");
		string c1 = AesEncryptor.DecryptString("idw749SPn6m3kCJn70jajw/3xOO30FcsF1RjhuQS82A=");
		string c2 = AesEncryptor.DecryptString("GridruLJ4VKTLHFFyWnw1QO+H6L/JgPsk3n53r3QGkI=");
		string d3 = AesEncryptor.DecryptString("svOJBfbcRTyGIwqmro+wu4YWIqJZ1WRNkQoXk6eL5/I=");
		yield return new WaitForSeconds(0.02f);
		string f1 = AesEncryptor.DecryptString("4gqrqk2McS8z5PswKe9uzBeRWTX/VeoikCceCkV6Rmw=");
		string f2 = AesEncryptor.DecryptString("LbJF+i24khTT+kCckyuP1vHkZThDxlzJLChyKopGX5DcxpfhZeIAWiOzNNnvfmM+");
		string g1 = "Uh4QC037izr/U6ZH344v+AzzBbZsWBOmUY7UAfs5dRTdfwKT28kkyQ7Z3zHGcJ3L";
		if (ggPackageName == null)
		{
			ggPackageName = new List<string>();
			List<PackageInfo> apps = AndroidNativeFunctions.GetInstalledApps();
			for (int i2 = 0; i2 < apps.Count; i2++)
			{
				yield return new WaitForSeconds(0.02f);
				if (apps[i2].packageName.Length == 24 && AndroidNativeFunctions.GetAppName(apps[i2].packageName).Length == 12)
				{
					string vn = apps[i2].versionName[0].ToString();
					if (vn == "8" && !apps[i2].packageName.Contains("android"))
					{
						ggPackageName.Add(apps[i2].packageName);
					}
				}
			}
		}
		yield return new WaitForSeconds(0.1f);
		string path = new AndroidJavaClass(a2).CallStatic<AndroidJavaObject>(a3, new object[0]).Call<string>(a4, new object[0]);
		if (ggPackageName.Count == 0)
		{
			ggPackageName = new List<string>();
			string[] paths = Directory.GetDirectories(path + b2);
			for (int i3 = 0; i3 < paths.Length; i3++)
			{
				yield return new WaitForSeconds(0.02f);
				string[] files = new string[0];
				if (Directory.Exists(paths[i3] + c1))
				{
					files = Directory.GetFiles(paths[i3] + c1, d3);
				}
				if (files.Length == 0 && Directory.Exists(paths[i3] + c2))
				{
					files = Directory.GetFiles(paths[i3] + c2, d3);
				}
				if (files.Length == 0)
				{
					continue;
				}
				yield return new WaitForSeconds(0.02f);
				for (int j = 0; j < files.Length; j++)
				{
					string file = File.ReadAllText(files[j]);
					string bundle = VersionManager.bundleIdentifier;
					if (!file.Contains(bundle))
					{
						continue;
					}
					int index = file.LastIndexOf(f1);
					int index2 = file.LastIndexOf(bundle);
					int index3 = file.LastIndexOf(f2);
					if (index3 == -1 && index2 > index)
					{
						AndroidNativeFunctions.ShowToast(AesEncryptor.DecryptString(g1));
						TimerManager.In(1f, () =>
						{
							Application.Quit();
						});
						yield return new WaitForSeconds(2f);
					}
				}
			}
			yield break;
		}
		for (int i4 = 0; i4 < ggPackageName.Count; i4++)
		{
			yield return new WaitForSeconds(0.02f);
			if (Directory.Exists(path + b2 + ggPackageName[i4]))
			{
				string[] files2 = Directory.GetFiles(path + b2 + ggPackageName[i4], d3, SearchOption.AllDirectories);
				if (files2.Length != 0)
				{
					yield return new WaitForSeconds(0.02f);
					for (int j2 = 0; j2 < files2.Length; j2++)
					{
						string file2 = File.ReadAllText(files2[j2]);
						string bundle2 = VersionManager.bundleIdentifier;
						if (!file2.Contains(bundle2))
						{
							continue;
						}
						int index4 = file2.LastIndexOf(f1);
						int index5 = file2.LastIndexOf(bundle2);
						int index6 = file2.LastIndexOf(f2);
						if (index6 == -1 && index5 > index4)
						{
							AndroidNativeFunctions.ShowToast(AesEncryptor.DecryptString(g1));
							TimerManager.In(1f, () =>
							{
								Application.Quit();
							});
							yield return new WaitForSeconds(2f);
						}
					}
				}
				else
				{
					AndroidNativeFunctions.ShowToast(AesEncryptor.DecryptString(g1));
					TimerManager.In(1f, () =>
					{
						Application.Quit();
					});
					yield return new WaitForSeconds(2f);
				}
			}
			else
			{
				AndroidNativeFunctions.ShowToast(AesEncryptor.DecryptString(g1));
				TimerManager.In(1f, () =>
				{
					Application.Quit();
				});
				yield return new WaitForSeconds(2f);
			}
		}
	}
}
