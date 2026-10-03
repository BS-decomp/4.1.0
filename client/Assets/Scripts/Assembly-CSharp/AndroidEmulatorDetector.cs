using System.IO;
using UnityEngine;

public class AndroidEmulatorDetector
{
	public static bool isEmulator()
	{
		if (Check01())
		{
			return true;
		}
		return false;
	}

	private static bool Check01()
	{
		string[] array = new string[17]
		{
			"/dev/socket/genyd", "/dev/socket/baseband_genyd", "fstab.andy", "ueventd.andy.rc", "fstab.nox", "init.nox.rc", "ueventd.nox.rc", "/dev/socket/qemud", "/dev/qemu_pipe", "ueventd.android_x86.rc",
			"x86.prop", "ueventd.ttVM_x86.rc", "init.ttVM_x86.rc", "fstab.ttVM_x86", "fstab.vbox86", "init.vbox86.rc", "ueventd.vbox86.rc"
		};
		string[] array2 = array;
		foreach (string path in array2)
		{
			if (File.Exists(path))
			{
				return true;
			}
		}
		return false;
	}

	private static bool Check02()
	{
		string[] array = new string[3] { "com.google.android.launcher.layouts.genymotion", "com.bluestacks", "com.bignox.app" };
		for (int i = 0; i < array.Length; i++)
		{
			AndroidJavaObject androidJavaObject = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity").Call<AndroidJavaObject>("getPackageManager", new object[0]);
			try
			{
				AndroidJavaObject androidJavaObject2 = androidJavaObject.Call<AndroidJavaObject>("getLaunchIntentForPackage", new object[1] { array[i] });
				if (androidJavaObject.Call<AndroidJavaObject>("queryIntentActivities", new object[2] { androidJavaObject2, 65536 }) != null)
				{
					return true;
				}
			}
			catch
			{
			}
		}
		return false;
	}

	private static bool Check03()
	{
		string[] array = new string[2] { "/proc/tty/drivers", "/proc/cpuinfo" };
		string[] array2 = array;
		foreach (string path in array2)
		{
			if (File.Exists(path))
			{
				string text = File.ReadAllText(path);
				if (text.Contains("goldfish"))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool Check04()
	{
		AndroidJavaClass androidJavaClass = new AndroidJavaClass("android.opengl.GLES20");
		string text = androidJavaClass.CallStatic<string>("glGetString", new object[1] { 7937 });
		if (!string.IsNullOrEmpty(text) && (text.Contains("Bluestacks") || text.Contains("Translator")))
		{
			return true;
		}
		return false;
	}

	private static bool Check05()
	{
		string path = new AndroidJavaClass("android.os.Environment").CallStatic<AndroidJavaObject>("getExternalStorageDirectory", new object[0]).Call<string>("toString", new object[0]) + "/windows/BstSharedFolder";
		return Directory.Exists(path);
	}

	private static bool Check06()
	{
		AndroidJavaClass androidJavaClass = new AndroidJavaClass("android.os.Build");
		string text = androidJavaClass.GetStatic<string>("PRODUCT");
		if (text.Contains("sdk"))
		{
			return true;
		}
		if (text.Contains("Andy"))
		{
			return true;
		}
		if (text.Contains("ttVM_Hdragon"))
		{
			return true;
		}
		if (text.Contains("google_sdk"))
		{
			return true;
		}
		if (text.Contains("Droid4X"))
		{
			return true;
		}
		if (text.Contains("nox"))
		{
			return true;
		}
		if (text.Contains("sdk_x86"))
		{
			return true;
		}
		if (text.Contains("sdk_google"))
		{
			return true;
		}
		if (text.Contains("vbox86p"))
		{
			return true;
		}
		string text2 = androidJavaClass.GetStatic<string>("MANUFACTURER");
		if (text2.Equals("unknown"))
		{
			return true;
		}
		if (text2.Equals("Genymotion"))
		{
			return true;
		}
		if (text2.Contains("Andy"))
		{
			return true;
		}
		if (text2.Contains("MIT"))
		{
			return true;
		}
		if (text2.Contains("nox"))
		{
			return true;
		}
		if (text2.Contains("TiantianVM"))
		{
			return true;
		}
		string text3 = androidJavaClass.GetStatic<string>("BRAND");
		if (text3.Equals("generic"))
		{
			return true;
		}
		if (text3.Equals("generic_x86"))
		{
			return true;
		}
		if (text3.Equals("TTVM"))
		{
			return true;
		}
		if (text3.Contains("Andy"))
		{
			return true;
		}
		string text4 = androidJavaClass.GetStatic<string>("DEVICE");
		if (text4.Contains("generic"))
		{
			return true;
		}
		if (text4.Contains("generic_x86"))
		{
			return true;
		}
		if (text4.Contains("Andy"))
		{
			return true;
		}
		if (text4.Contains("ttVM_Hdragon"))
		{
			return true;
		}
		if (text4.Contains("Droid4X"))
		{
			return true;
		}
		if (text4.Contains("nox"))
		{
			return true;
		}
		if (text4.Contains("generic_x86_64"))
		{
			return true;
		}
		if (text4.Contains("vbox86p"))
		{
			return true;
		}
		string text5 = androidJavaClass.GetStatic<string>("MODEL");
		if (text5.Equals("sdk"))
		{
			return true;
		}
		if (text5.Equals("google_sdk"))
		{
			return true;
		}
		if (text5.Contains("Droid4X"))
		{
			return true;
		}
		if (text5.Contains("TiantianVM"))
		{
			return true;
		}
		if (text5.Contains("Andy"))
		{
			return true;
		}
		if (text5.Contains("Android SDK built for x86_64"))
		{
			return true;
		}
		if (text5.Contains("Android SDK built for x86"))
		{
			return true;
		}
		string text6 = androidJavaClass.GetStatic<string>("HARDWARE");
		if (text6.Equals("goldfish"))
		{
			return true;
		}
		if (text6.Equals("vbox86"))
		{
			return true;
		}
		if (text6.Contains("nox"))
		{
			return true;
		}
		if (text6.Contains("ttVM_x86"))
		{
			return true;
		}
		string text7 = androidJavaClass.GetStatic<string>("FINGERPRINT");
		if (text7.Contains("generic/sdk/generic"))
		{
			return true;
		}
		if (text7.Contains("generic_x86/sdk_x86/generic_x86"))
		{
			return true;
		}
		if (text7.Contains("Andy"))
		{
			return true;
		}
		if (text7.Contains("ttVM_Hdragon"))
		{
			return true;
		}
		if (text7.Contains("generic_x86_64"))
		{
			return true;
		}
		if (text7.Contains("generic/google_sdk/generic"))
		{
			return true;
		}
		if (text7.Contains("vbox86p"))
		{
			return true;
		}
		if (text7.Contains("generic/vbox86p/vbox86p"))
		{
			return true;
		}
		return false;
	}
}
