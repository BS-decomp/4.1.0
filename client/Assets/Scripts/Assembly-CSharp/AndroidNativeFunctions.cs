using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FreeJSON;
using UnityEngine;
using UnityEngine.Events;

public class AndroidNativeFunctions : MonoBehaviour
{
	private class ShowAlertListener : AndroidJavaProxy
	{
		private UnityAction<DialogInterface> action;

		public ShowAlertListener(UnityAction<DialogInterface> a)
			: base("android.content.DialogInterface$OnClickListener")
		{
			action = a;
		}

		public void onClick(AndroidJavaObject obj, int which)
		{
			if (action != null)
			{
				action((DialogInterface)which);
			}
		}
	}

	private class ShowAlertInputListener : AndroidJavaProxy
	{
		private UnityAction<DialogInterface, string> action;

		private AndroidJavaObject editText;

		public ShowAlertInputListener(UnityAction<DialogInterface, string> a, AndroidJavaObject et)
			: base("android.content.DialogInterface$OnClickListener")
		{
			action = a;
			editText = et;
		}

		public void onClick(AndroidJavaObject obj, int which)
		{
			if (action != null)
			{
				action((DialogInterface)which, editText.Call<AndroidJavaObject>("getText", new object[0]).Call<string>("toString", new object[0]));
			}
		}
	}

	private class ShowAlertListListener : AndroidJavaProxy
	{
		private string[] list;

		private UnityAction<string> action;

		public ShowAlertListListener(UnityAction<string> w, string[] a)
			: base("android.content.DialogInterface$OnClickListener")
		{
			action = w;
			list = a;
		}

		public void onClick(AndroidJavaObject obj, int which)
		{
			action(list[which]);
		}
	}

	private static bool immersiveMode;

	private static AndroidNativeFunctions instance;

	private static AndroidJavaObject progressDialog;

	private static AndroidJavaObject currentActivity
	{
		get
		{
			return new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
		}
	}

	private static void CreateGO()
	{
		if (!(instance != null))
		{
			GameObject gameObject = new GameObject("AndroidNativeFunctions");
			instance = gameObject.AddComponent<AndroidNativeFunctions>();
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
		}
	}

	private void OnApplicationFocus(bool focusStatus)
	{
		if (immersiveMode && focusStatus)
		{
			ImmersiveMode();
		}
	}

	public static void StartApp(string packageName, bool isExitThisApp)
	{
		if (Application.platform == RuntimePlatform.Android)
		{
			AndroidJavaObject androidJavaObject = currentActivity.Call<AndroidJavaObject>("getPackageManager", new object[0]).Call<AndroidJavaObject>("getLaunchIntentForPackage", new object[1] { packageName });
			currentActivity.Call("startActivity", androidJavaObject);
			if (isExitThisApp)
			{
				Application.Quit();
			}
		}
	}

	public static string GetAppName(string packageName)
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return string.Empty;
		}
		AndroidJavaObject androidJavaObject = currentActivity.Call<AndroidJavaObject>("getPackageManager", new object[0]);
		AndroidJavaObject androidJavaObject2 = androidJavaObject.Call<AndroidJavaObject>("getApplicationInfo", new object[2] { packageName, 0 });
		return androidJavaObject.Call<string>("getApplicationLabel", new object[1] { androidJavaObject2 });
	}

	public static List<PackageInfo> GetInstalledApps()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return new List<PackageInfo>();
		}
		AndroidJavaObject androidJavaObject = currentActivity.Call<AndroidJavaObject>("getPackageManager", new object[0]).Call<AndroidJavaObject>("getInstalledPackages", new object[1] { 0 });
		int num = androidJavaObject.Call<int>("size", new object[0]);
		List<PackageInfo> list = new List<PackageInfo>();
		for (int i = 0; i < num; i++)
		{
			AndroidJavaObject androidJavaObject2 = androidJavaObject.Call<AndroidJavaObject>("get", new object[1] { i });
			PackageInfo packageInfo = new PackageInfo();
			packageInfo.firstInstallTime = androidJavaObject2.Get<long>("firstInstallTime");
			packageInfo.packageName = androidJavaObject2.Get<string>("packageName");
			packageInfo.lastUpdateTime = androidJavaObject2.Get<long>("lastUpdateTime");
			packageInfo.versionCode = androidJavaObject2.Get<int>("versionCode");
			packageInfo.versionName = androidJavaObject2.Get<string>("versionName");
			list.Add(packageInfo);
		}
		return list;
	}

	public static PackageInfo GetAppInfo()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return new PackageInfo();
		}
		return GetAppInfo(currentActivity.Call<string>("getPackageName", new object[0]));
	}

	public static PackageInfo GetAppInfo(string packageName)
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return null;
		}
		AndroidJavaObject androidJavaObject = currentActivity.Call<AndroidJavaObject>("getPackageManager", new object[0]).Call<AndroidJavaObject>("getPackageInfo", new object[2] { packageName, 0 });
		PackageInfo packageInfo = new PackageInfo();
		packageInfo.firstInstallTime = androidJavaObject.Get<long>("firstInstallTime");
		packageInfo.packageName = androidJavaObject.Get<string>("packageName");
		packageInfo.lastUpdateTime = androidJavaObject.Get<long>("lastUpdateTime");
		packageInfo.versionCode = androidJavaObject.Get<int>("versionCode");
		packageInfo.versionName = androidJavaObject.Get<string>("versionName");
		return packageInfo;
	}

	public static DeviceInfo GetDeviceInfo()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return null;
		}
		AndroidJavaClass androidJavaClass = new AndroidJavaClass("android.os.Build$VERSION");
		DeviceInfo deviceInfo = new DeviceInfo();
		deviceInfo.CODENAME = androidJavaClass.GetStatic<string>("CODENAME");
		deviceInfo.INCREMENTAL = androidJavaClass.GetStatic<string>("INCREMENTAL");
		deviceInfo.RELEASE = androidJavaClass.GetStatic<string>("RELEASE");
		deviceInfo.SDK = androidJavaClass.GetStatic<int>("SDK_INT");
		return deviceInfo;
	}

	public static string GetAndroidID()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return SystemInfo.deviceUniqueIdentifier;
		}
		AndroidJavaObject androidJavaObject = currentActivity.Call<AndroidJavaObject>("getContentResolver", new object[0]);
		return new AndroidJavaClass("android.provider.Settings$Secure").CallStatic<string>("getString", new object[2] { androidJavaObject, "android_id" });
	}

	public static void ShareText(string text, string subject, string chooser)
	{
		if (Application.platform == RuntimePlatform.Android)
		{
			AndroidJavaObject androidJavaObject = new AndroidJavaObject("android.content.Intent");
			androidJavaObject.Call<AndroidJavaObject>("setAction", new object[1] { "android.intent.action.SEND" });
			androidJavaObject.Call<AndroidJavaObject>("setType", new object[1] { "text/plain" });
			androidJavaObject.Call<AndroidJavaObject>("putExtra", new object[2] { "android.intent.extra.TEXT", text });
			androidJavaObject.Call<AndroidJavaObject>("putExtra", new object[2] { "android.intent.extra.SUBJECT", subject });
			AndroidJavaObject androidJavaObject2 = androidJavaObject.CallStatic<AndroidJavaObject>("createChooser", new object[2] { androidJavaObject, chooser });
			currentActivity.Call("startActivity", androidJavaObject2);
		}
	}

	public static void ShareImage(string text, string subject, string chooser, Texture2D picture)
	{
		if (Application.platform == RuntimePlatform.Android)
		{
			byte[] array = new AndroidJavaObject("android.util.Base64").CallStatic<byte[]>("decode", new object[2]
			{
				Convert.ToBase64String(picture.EncodeToPNG()),
				0
			});
			AndroidJavaObject androidJavaObject = new AndroidJavaObject("android.graphics.BitmapFactory").CallStatic<AndroidJavaObject>("decodeByteArray", new object[3] { array, 0, array.Length });
			AndroidJavaObject androidJavaObject2 = new AndroidJavaClass("android.graphics.Bitmap$CompressFormat").GetStatic<AndroidJavaObject>("JPEG");
			androidJavaObject.Call<bool>("compress", new object[3]
			{
				androidJavaObject2,
				100,
				new AndroidJavaObject("java.io.ByteArrayOutputStream")
			});
			string text2 = new AndroidJavaClass("android.provider.MediaStore$Images$Media").CallStatic<string>("insertImage", new object[4]
			{
				currentActivity.Call<AndroidJavaObject>("getContentResolver", new object[0]),
				androidJavaObject,
				picture.name,
				string.Empty
			});
			AndroidJavaObject androidJavaObject3 = new AndroidJavaClass("android.net.Uri").CallStatic<AndroidJavaObject>("parse", new object[1] { text2 });
			AndroidJavaObject androidJavaObject4 = new AndroidJavaObject("android.content.Intent");
			androidJavaObject4.Call<AndroidJavaObject>("setAction", new object[1] { "android.intent.action.SEND" });
			androidJavaObject4.Call<AndroidJavaObject>("setType", new object[1] { "image/*" });
			androidJavaObject4.Call<AndroidJavaObject>("putExtra", new object[2] { "android.intent.extra.STREAM", androidJavaObject3 });
			androidJavaObject4.Call<AndroidJavaObject>("putExtra", new object[2] { "android.intent.extra.TEXT", text });
			androidJavaObject4.Call<AndroidJavaObject>("putExtra", new object[2] { "android.intent.extra.SUBJECT", subject });
			AndroidJavaObject androidJavaObject5 = androidJavaObject4.CallStatic<AndroidJavaObject>("createChooser", new object[2] { androidJavaObject4, chooser });
			currentActivity.Call("startActivity", androidJavaObject5);
		}
	}

	public static void ShareScreenshot(string text, string subject, string chooser, string screenshotPath)
	{
		if (Application.platform == RuntimePlatform.Android)
		{
			AndroidJavaObject androidJavaObject = new AndroidJavaObject("android.content.Intent");
			AndroidJavaObject androidJavaObject2 = new AndroidJavaClass("android.net.Uri").CallStatic<AndroidJavaObject>("parse", new object[1] { "file://" + screenshotPath });
			androidJavaObject.Call<AndroidJavaObject>("setAction", new object[1] { "android.intent.action.SEND" });
			androidJavaObject.Call<AndroidJavaObject>("setType", new object[1] { "image/png" });
			androidJavaObject.Call<AndroidJavaObject>("putExtra", new object[2] { "android.intent.extra.STREAM", androidJavaObject2 });
			androidJavaObject.Call<AndroidJavaObject>("putExtra", new object[2] { "android.intent.extra.TEXT", text });
			androidJavaObject.Call<AndroidJavaObject>("putExtra", new object[2] { "android.intent.extra.SUBJECT", subject });
			AndroidJavaObject androidJavaObject3 = androidJavaObject.CallStatic<AndroidJavaObject>("createChooser", new object[2] { androidJavaObject, chooser });
			currentActivity.Call("startActivity", androidJavaObject3);
		}
	}

	public static void ShowAlert(string message, string title, string positiveButton, string negativeButton, string neutralButton, UnityAction<DialogInterface> action)
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return;
		}
		currentActivity.Call("runOnUiThread", (AndroidJavaRunnable)(() =>
		{
			AndroidJavaObject androidJavaObject = new AndroidJavaObject("android/app/AlertDialog$Builder", currentActivity);
			androidJavaObject.Call<AndroidJavaObject>("setMessage", new object[1] { message });
			androidJavaObject.Call<AndroidJavaObject>("setCancelable", new object[1] { false });
			if (!string.IsNullOrEmpty(title))
			{
				androidJavaObject.Call<AndroidJavaObject>("setTitle", new object[1] { title });
			}
			androidJavaObject.Call<AndroidJavaObject>("setPositiveButton", new object[2]
			{
				positiveButton,
				new ShowAlertListener(action)
			});
			if (!string.IsNullOrEmpty(negativeButton))
			{
				androidJavaObject.Call<AndroidJavaObject>("setNegativeButton", new object[2]
				{
					negativeButton,
					new ShowAlertListener(action)
				});
			}
			if (!string.IsNullOrEmpty(neutralButton))
			{
				androidJavaObject.Call<AndroidJavaObject>("setNeutralButton", new object[2]
				{
					neutralButton,
					new ShowAlertListener(action)
				});
			}
			AndroidJavaObject androidJavaObject2 = androidJavaObject.Call<AndroidJavaObject>("create", new object[0]);
			androidJavaObject2.Call("show");
		}));
	}

	public static void ShowAlertInput(string text, string message, string title, string positiveButton, string negativeButton, UnityAction<DialogInterface, string> action)
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return;
		}
		currentActivity.Call("runOnUiThread", (AndroidJavaRunnable)(() =>
		{
			AndroidJavaObject androidJavaObject = new AndroidJavaObject("android/app/AlertDialog$Builder", currentActivity);
			AndroidJavaObject androidJavaObject2 = new AndroidJavaObject("android.widget.EditText", currentActivity);
			if (!string.IsNullOrEmpty(text))
			{
				androidJavaObject2.Call("setText", text);
			}
			androidJavaObject.Call<AndroidJavaObject>("setView", new object[1] { androidJavaObject2 });
			if (!string.IsNullOrEmpty(message))
			{
				androidJavaObject.Call<AndroidJavaObject>("setMessage", new object[1] { message });
			}
			androidJavaObject.Call<AndroidJavaObject>("setCancelable", new object[1] { false });
			if (!string.IsNullOrEmpty(title))
			{
				androidJavaObject.Call<AndroidJavaObject>("setTitle", new object[1] { title });
			}
			androidJavaObject.Call<AndroidJavaObject>("setPositiveButton", new object[2]
			{
				positiveButton,
				new ShowAlertInputListener(action, androidJavaObject2)
			});
			if (!string.IsNullOrEmpty(negativeButton))
			{
				androidJavaObject.Call<AndroidJavaObject>("setNegativeButton", new object[2]
				{
					negativeButton,
					new ShowAlertInputListener(action, androidJavaObject2)
				});
			}
			AndroidJavaObject androidJavaObject3 = androidJavaObject.Call<AndroidJavaObject>("create", new object[0]);
			androidJavaObject3.Call("show");
		}));
	}

	public static void ShowAlertList(string title, string[] list, UnityAction<string> action)
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return;
		}
		currentActivity.Call("runOnUiThread", (AndroidJavaRunnable)(() =>
		{
			AndroidJavaObject androidJavaObject = new AndroidJavaObject("android/app/AlertDialog$Builder", currentActivity);
			androidJavaObject.Call<AndroidJavaObject>("setCancelable", new object[1] { false });
			if (!string.IsNullOrEmpty(title))
			{
				androidJavaObject.Call<AndroidJavaObject>("setTitle", new object[1] { title });
			}
			androidJavaObject.Call<AndroidJavaObject>("setItems", new object[2]
			{
				list,
				new ShowAlertListListener(action, list)
			});
			AndroidJavaObject androidJavaObject2 = androidJavaObject.Call<AndroidJavaObject>("create", new object[0]);
			androidJavaObject2.Call("show");
		}));
	}

	public static void ShowToast(string message)
	{
		ShowToast(message, true);
	}

	public static void ShowToast(string message, bool shortDuration)
	{
		if (Application.platform == RuntimePlatform.Android)
		{
			currentActivity.Call("runOnUiThread", (AndroidJavaRunnable)(() =>
			{
				AndroidJavaObject androidJavaObject = new AndroidJavaObject("android.widget.Toast", currentActivity);
				androidJavaObject.CallStatic<AndroidJavaObject>("makeText", new object[3]
				{
					currentActivity,
					message,
					(!shortDuration) ? 1 : 0
				}).Call("show");
			}));
		}
	}

	public static void ShowProgressDialog(string message)
	{
		ShowProgressDialog(message, string.Empty);
	}

	public static void ShowProgressDialog(string message, string title)
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return;
		}
		if (progressDialog != null)
		{
			HideProgressDialog();
		}
		currentActivity.Call("runOnUiThread", (AndroidJavaRunnable)(() =>
		{
			progressDialog = new AndroidJavaObject("android.app.ProgressDialog", currentActivity);
			progressDialog.Call("setProgressStyle", 0);
			progressDialog.Call("setIndeterminate", true);
			progressDialog.Call("setCancelable", false);
			progressDialog.Call("setMessage", message);
			if (!string.IsNullOrEmpty(title))
			{
				progressDialog.Call("setTitle", title);
			}
			progressDialog.Call("show");
		}));
	}

	public static void HideProgressDialog()
	{
		if (progressDialog != null)
		{
			currentActivity.Call("runOnUiThread", (AndroidJavaRunnable)(() =>
			{
				progressDialog.Call("hide");
				progressDialog.Call("dismiss");
				progressDialog = null;
			}));
		}
	}

	public static void ImmersiveMode()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return;
		}
		int num = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
		if (num >= 19)
		{
			CreateGO();
			immersiveMode = true;
			currentActivity.Call("runOnUiThread", (AndroidJavaRunnable)(() =>
			{
				AndroidJavaClass androidJavaClass = new AndroidJavaClass("android.view.View");
				AndroidJavaObject androidJavaObject = currentActivity.Call<AndroidJavaObject>("findViewById", new object[1] { new AndroidJavaClass("android.R$id").GetStatic<int>("content") });
				androidJavaObject.Call("setSystemUiVisibility", androidJavaClass.GetStatic<int>("SYSTEM_UI_FLAG_LAYOUT_STABLE") | androidJavaClass.GetStatic<int>("SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION") | androidJavaClass.GetStatic<int>("SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN") | androidJavaClass.GetStatic<int>("SYSTEM_UI_FLAG_HIDE_NAVIGATION") | androidJavaClass.GetStatic<int>("SYSTEM_UI_FLAG_FULLSCREEN") | androidJavaClass.GetStatic<int>("SYSTEM_UI_FLAG_IMMERSIVE_STICKY"));
			}));
		}
	}

	public static void OpenGooglePlay(string packageName)
	{
		if (Application.platform == RuntimePlatform.Android)
		{
			Application.OpenURL("market://details?id=" + packageName);
		}
		else
		{
			Application.OpenURL("https://play.google.com/store/apps/details?id=" + packageName);
		}
	}

	public static bool isDeviceRooted()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return false;
		}
		string[] array = new string[9] { "/system/app/Superuser.apk", "/sbin/su", "/system/bin/su", "/system/xbin/su", "/data/local/xbin/su", "/data/local/bin/su", "/system/sd/xbin/su", "/system/bin/failsafe/su", "/data/local/su" };
		string[] array2 = array;
		foreach (string path in array2)
		{
			if (File.Exists(path))
			{
				return true;
			}
		}
		string text = new AndroidJavaClass("android.os.Build").GetStatic<string>("TAGS");
		if (text != null && text.Contains("test-keys"))
		{
			return true;
		}
		return false;
	}

	public static bool isInstalledApp(string packageName)
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return false;
		}
		try
		{
			currentActivity.Call<AndroidJavaObject>("getPackageManager", new object[0]).Call<AndroidJavaObject>("getPackageInfo", new object[2] { packageName, 0 });
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool isTVDevice()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return false;
		}
		int num = currentActivity.Call<AndroidJavaObject>("getSystemService", new object[1] { "uimode" }).Call<int>("getCurrentModeType", new object[0]);
		if (num == 4)
		{
			return true;
		}
		return false;
	}

	public static bool isWiredHeadset()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return false;
		}
		return currentActivity.Call<AndroidJavaObject>("getSystemService", new object[1] { "audio" }).Call<bool>("isWiredHeadsetOn", new object[0]);
	}

	public static void SetTotalVolume(int volumeLevel)
	{
		if (Application.platform == RuntimePlatform.Android)
		{
			volumeLevel = Mathf.Clamp(volumeLevel, 0, 15);
			currentActivity.Call<AndroidJavaObject>("getSystemService", new object[1] { "audio" }).Call("setStreamVolume", 3, volumeLevel, 0);
		}
	}

	public static int GetTotalVolume()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return 0;
		}
		return currentActivity.Call<AndroidJavaObject>("getSystemService", new object[1] { "audio" }).Call<int>("getStreamVolume", new object[1] { 3 });
	}

	public static bool isConnectInternet()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return false;
		}
		try
		{
			AndroidJavaObject androidJavaObject = currentActivity.Call<AndroidJavaObject>("getSystemService", new object[1] { "connectivity" }).Call<AndroidJavaObject>("getActiveNetworkInfo", new object[0]);
			if (androidJavaObject == null)
			{
				return false;
			}
			return androidJavaObject.Call<bool>("isConnectedOrConnecting", new object[0]);
		}
		catch
		{
			return false;
		}
	}

	public static bool isConnectWifi()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return false;
		}
		try
		{
			AndroidJavaObject androidJavaObject = currentActivity.Call<AndroidJavaObject>("getSystemService", new object[1] { "connectivity" }).Call<AndroidJavaObject>("getNetworkInfo", new object[1] { 1 });
			if (androidJavaObject == null)
			{
				return false;
			}
			return androidJavaObject.Call<bool>("isConnectedOrConnecting", new object[0]);
		}
		catch
		{
			return false;
		}
	}

	public static int GetBatteryLevel()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return 0;
		}
		AndroidJavaObject androidJavaObject = currentActivity.Call<AndroidJavaObject>("getApplicationContext", new object[0]).Call<AndroidJavaObject>("registerReceiver", new object[2]
		{
			null,
			new AndroidJavaObject("android.content.IntentFilter", "android.intent.action.BATTERY_CHANGED")
		});
		int num = androidJavaObject.Call<int>("getIntExtra", new object[2] { "level", -1 });
		int num2 = androidJavaObject.Call<int>("getIntExtra", new object[2] { "scale", -1 });
		if (num == -1 || num2 == -1)
		{
			return 0;
		}
		return (int)((float)num / (float)num2 * 100f);
	}

	public static void SendEmail(string text, string subject, string email)
	{
		if (Application.platform == RuntimePlatform.Android)
		{
			AndroidJavaObject androidJavaObject = new AndroidJavaObject("android.content.Intent");
			androidJavaObject.Call<AndroidJavaObject>("setAction", new object[1] { "android.intent.action.SENDTO" });
			androidJavaObject.Call<AndroidJavaObject>("setType", new object[1] { "text/plain" });
			androidJavaObject.Call<AndroidJavaObject>("putExtra", new object[2] { "android.intent.extra.TEXT", text });
			androidJavaObject.Call<AndroidJavaObject>("putExtra", new object[2] { "android.intent.extra.SUBJECT", subject });
			androidJavaObject.Call<AndroidJavaObject>("setData", new object[1] { new AndroidJavaClass("android.net.Uri").CallStatic<AndroidJavaObject>("parse", new object[1] { "mailto:" + email }) });
			currentActivity.Call("startActivity", androidJavaObject);
		}
	}

	public static bool VerifyGooglePlayPurchase(string purchaseJson, string base64Signature, string publicKey)
	{
		bool result = false;
		try
		{
			RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
			rSACryptoServiceProvider.FromXmlString(publicKey);
			byte[] signature = Convert.FromBase64String(base64Signature);
			SHA1Managed halg = new SHA1Managed();
			byte[] bytes = Encoding.UTF8.GetBytes(purchaseJson);
			result = rSACryptoServiceProvider.VerifyData(bytes, halg, signature);
		}
		catch (Exception ex)
		{
			JsonObject jsonObject = new JsonObject();
			jsonObject.Add("message", ex.Message);
			jsonObject.Add("starkTrace", ex.StackTrace);
			Firebase firebase = new Firebase();
			firebase.Child("Others").Child("InAppError").Child(AccountManager.AccountID)
				.SetValue(jsonObject.ToString());
			MonoBehaviour.print(ex.Message);
		}
		return result;
	}

	public static string GetSignature()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return string.Empty;
		}
		AndroidJavaObject androidJavaObject = currentActivity.Call<AndroidJavaObject>("getPackageManager", new object[0]).Call<AndroidJavaObject>("getPackageInfo", new object[2]
		{
			GetAppInfo().packageName,
			64
		});
		AndroidJavaObject[] array = androidJavaObject.Get<AndroidJavaObject[]>("signatures");
		return array[0].Call<int>("hashCode", new object[0]).ToString("X");
	}

	public static string[] GetEmails()
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return new string[0];
		}
		AndroidJavaObject androidJavaObject = currentActivity.Call<AndroidJavaObject>("getApplication", new object[0]);
		AndroidJavaObject androidJavaObject2 = new AndroidJavaClass("android.accounts.AccountManager").CallStatic<AndroidJavaObject>("get", new object[1] { androidJavaObject }).Call<AndroidJavaObject>("getAccountsByType", new object[1] { "com.google" });
		AndroidJavaObject[] array = AndroidJNIHelper.ConvertFromJNIArray<AndroidJavaObject[]>(androidJavaObject2.GetRawObject());
		string[] array2 = new string[array.Length];
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i] = array[i].Get<string>("name");
		}
		return array2;
	}

	public static void UpdateGallery(string path)
	{
		if (Application.platform == RuntimePlatform.Android)
		{
			AndroidJavaObject androidJavaObject = new AndroidJavaObject("android.content.ContentValues");
			AndroidJavaClass androidJavaClass = new AndroidJavaClass("android.provider.MediaStore$MediaColumns");
			androidJavaObject.Call("put", androidJavaClass.GetStatic<string>("TITLE"), Path.GetFileNameWithoutExtension(path));
			string text = Convert.ToInt64((DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds).ToString();
			androidJavaObject.Call("put", androidJavaClass.GetStatic<string>("DATE_ADDED"), text);
			androidJavaObject.Call("put", androidJavaClass.GetStatic<string>("DATE_MODIFIED"), text);
			androidJavaObject.Call("put", androidJavaClass.GetStatic<string>("MIME_TYPE"), "image/png");
			androidJavaObject.Call("put", androidJavaClass.GetStatic<string>("DATA"), path);
			AndroidJavaObject androidJavaObject2 = new AndroidJavaClass("android.provider.MediaStore$Images$Media").GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI");
			currentActivity.Call<AndroidJavaObject>("getContentResolver", new object[0]).Call<AndroidJavaObject>("insert", new object[2] { androidJavaObject2, androidJavaObject });
		}
	}

	public static bool CheckPermission(string permission)
	{
		if (Application.platform != RuntimePlatform.Android)
		{
			return true;
		}
		if (currentActivity.Call<int>("checkCallingOrSelfPermission", new object[1] { permission }) == 0)
		{
			return true;
		}
		return false;
	}

	public static void TakeScreenshot(string name, string directory)
	{
		TakeScreenshot(name, directory, 0, null);
	}

	public static void TakeScreenshot(string name, string directory, int superSize)
	{
		TakeScreenshot(name, directory, superSize, null);
	}

	public static void TakeScreenshot(string name, string directory, Action<string> finishAction)
	{
		TakeScreenshot(name, directory, 0, finishAction);
	}

	public static void TakeScreenshot(string name, string directory, int superSize, Action<string> finishAction)
	{
		if (Application.platform == RuntimePlatform.Android)
		{
			CreateGO();
			instance.StartCoroutine(instance.CreateScreenshot(name, directory, superSize, finishAction));
		}
	}

	private IEnumerator CreateScreenshot(string name, string directory, int superSize, Action<string> finishAction)
	{
		name += ".png";
		Application.CaptureScreenshot(name, superSize);
		yield return new WaitForSeconds(0.5f);
		while (!File.Exists(Application.persistentDataPath + "/" + name))
		{
			yield return new WaitForSeconds(0.1f);
		}
		string originalPath = Application.persistentDataPath + "/" + name;
		string picturesDirectory = new AndroidJavaClass("android.os.Environment").CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory", new object[1] { new AndroidJavaClass("android.os.Environment").GetStatic<string>("DIRECTORY_PICTURES") }).Call<string>("getAbsolutePath", new object[0]);
		string newPath = picturesDirectory + "/" + directory + "/" + name;
		if (!Directory.Exists(picturesDirectory + "/" + directory))
		{
			Directory.CreateDirectory(picturesDirectory + "/" + directory);
		}
		File.Move(originalPath, newPath);
		UpdateGallery(newPath);
		if (finishAction != null)
		{
			finishAction(newPath);
		}
	}
}
