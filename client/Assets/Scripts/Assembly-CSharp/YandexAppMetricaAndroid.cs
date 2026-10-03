using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YMMJSONUtils;

public class YandexAppMetricaAndroid : IYandexAppMetrica
{
	private AndroidJavaClass metricaClass;

	public bool CollectInstalledApps
	{
		get
		{
			if (metricaClass != null)
			{
				return metricaClass.CallStatic<bool>("getCollectInstalledApps", new object[0]);
			}
			return false;
		}
		set
		{
			if (metricaClass != null)
			{
				metricaClass.CallStatic("setCollectInstalledApps", value);
			}
		}
	}

	public int LibraryApiLevel
	{
		get
		{
			if (metricaClass != null)
			{
				return metricaClass.CallStatic<int>("getLibraryApiLevel", new object[0]);
			}
			return 0;
		}
	}

	public string LibraryVersion
	{
		get
		{
			if (metricaClass != null)
			{
				return metricaClass.CallStatic<string>("getLibraryVersion", new object[0]);
			}
			return null;
		}
	}

	public void ActivateWithAPIKey(string apiKey)
	{
		metricaClass = new AndroidJavaClass("com.yandex.metrica.YandexMetrica");
		using (AndroidJavaClass androidJavaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
		{
			AndroidJavaObject androidJavaObject = androidJavaClass.GetStatic<AndroidJavaObject>("currentActivity");
			metricaClass.CallStatic("activate", androidJavaObject, apiKey);
		}
	}

	public void ActivateWithConfiguration(YandexAppMetricaConfig config)
	{
		metricaClass = new AndroidJavaClass("com.yandex.metrica.YandexMetrica");
		using (AndroidJavaClass androidJavaClass = new AndroidJavaClass("com.yandex.metrica.YandexMetricaConfig"))
		{
			AndroidJavaObject androidJavaObject = androidJavaClass.CallStatic<AndroidJavaObject>("newConfigBuilder", new object[1] { config.ApiKey });
			if (config.Location != null)
			{
				androidJavaObject.Call<AndroidJavaObject>("setLocation", new object[1] { config.Location.ToLocation() });
			}
			if (config.AppVersion != null)
			{
				androidJavaObject.Call<AndroidJavaObject>("setAppVersion", new object[1] { config.AppVersion });
			}
			if (config.TrackLocationEnabled.HasValue)
			{
				androidJavaObject.Call<AndroidJavaObject>("setTrackLocationEnabled", new object[1] { config.TrackLocationEnabled.Value });
			}
			if (config.SessionTimeout.HasValue)
			{
				androidJavaObject.Call<AndroidJavaObject>("setSessionTimeout", new object[1] { config.SessionTimeout.Value });
			}
			if (config.ReportCrashesEnabled.HasValue)
			{
				androidJavaObject.Call<AndroidJavaObject>("setReportCrashesEnabled", new object[1] { config.ReportCrashesEnabled.Value });
			}
			bool? loggingEnabled = config.LoggingEnabled;
			if (loggingEnabled.HasValue && loggingEnabled.Value)
			{
				androidJavaObject.Call<AndroidJavaObject>("setLogEnabled", new object[0]);
			}
			if (config.CollectInstalledApps.HasValue)
			{
				androidJavaObject.Call<AndroidJavaObject>("setCollectInstalledApps", new object[1] { config.CollectInstalledApps.Value });
			}
			if (config.PreloadInfo != null)
			{
				AndroidJavaClass androidJavaClass2 = new AndroidJavaClass("com.yandex.metrica.PreloadInfo");
				AndroidJavaObject androidJavaObject2 = androidJavaClass2.CallStatic<AndroidJavaObject>("newBuilder", new object[1] { config.PreloadInfo.TrackingId });
				foreach (KeyValuePair<string, string> item in config.PreloadInfo.AdditionalInfo)
				{
					androidJavaObject2.Call<AndroidJavaObject>("setAdditionalParams", new object[2] { item.Key, item.Value });
				}
				androidJavaObject.Call<AndroidJavaObject>("setPreloadInfo", new object[1] { androidJavaObject2.Call<AndroidJavaObject>("build", new object[0]) });
			}
			androidJavaObject.Call<AndroidJavaObject>("setReportNativeCrashesEnabled", new object[1] { false });
			using (AndroidJavaClass androidJavaClass3 = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			{
				AndroidJavaObject androidJavaObject3 = androidJavaClass3.GetStatic<AndroidJavaObject>("currentActivity");
				metricaClass.CallStatic("activate", androidJavaObject3, androidJavaObject.Call<AndroidJavaObject>("build", new object[0]));
			}
		}
	}

	public void OnResumeApplication()
	{
		if (metricaClass != null)
		{
			using (AndroidJavaClass androidJavaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			{
				AndroidJavaObject androidJavaObject = androidJavaClass.GetStatic<AndroidJavaObject>("currentActivity");
				metricaClass.CallStatic("onResumeActivity", androidJavaObject);
			}
		}
	}

	public void OnPauseApplication()
	{
		if (metricaClass != null)
		{
			using (AndroidJavaClass androidJavaClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
			{
				AndroidJavaObject androidJavaObject = androidJavaClass.GetStatic<AndroidJavaObject>("currentActivity");
				metricaClass.CallStatic("onPauseActivity", androidJavaObject);
			}
		}
	}

	public void ReportEvent(string message)
	{
		if (metricaClass != null)
		{
			metricaClass.CallStatic("reportEvent", message);
		}
	}

	public void ReportEvent(string message, Hashtable parameters)
	{
		if (metricaClass != null)
		{
			metricaClass.CallStatic("reportEvent", message, JSONEncoder.Encode(parameters));
		}
	}

	public void ReportEvent(string message, string key, string value)
	{
		if (metricaClass != null)
		{
			Hashtable hashtable = new Hashtable();
			hashtable.Add(key, value);
			metricaClass.CallStatic("reportEvent", message, JSONEncoder.Encode(hashtable));
		}
	}

	public void ReportError(string condition, string stackTrace)
	{
		if (metricaClass != null)
		{
			AndroidJavaObject androidJavaObject = new AndroidJavaObject("java.lang.Throwable", "\n" + stackTrace);
			metricaClass.CallStatic("reportError", condition, androidJavaObject);
		}
	}

	public void SetTrackLocationEnabled(bool enabled)
	{
		if (metricaClass != null)
		{
			metricaClass.CallStatic("setTrackLocationEnabled", enabled);
		}
	}

	public void SetLocation(Coordinates coordinates)
	{
		if (metricaClass != null)
		{
			metricaClass.CallStatic("setLocation", coordinates.ToLocation());
		}
	}

	public void SetSessionTimeout(uint sessionTimeoutSeconds)
	{
		if (metricaClass != null)
		{
			metricaClass.CallStatic("setSessionTimeout", (int)sessionTimeoutSeconds);
		}
	}

	public void SetReportCrashesEnabled(bool enabled)
	{
		if (metricaClass != null)
		{
			metricaClass.CallStatic("setReportCrashesEnabled", enabled);
		}
	}

	public void SetCustomAppVersion(string appVersion)
	{
		if (metricaClass != null)
		{
			metricaClass.CallStatic("setCustomAppVersion", appVersion);
		}
	}

	public void SetLoggingEnabled()
	{
		if (metricaClass != null)
		{
			metricaClass.CallStatic("setLoggingEnabled");
		}
	}

	public void SetEnvironmentValue(string key, string value)
	{
		if (metricaClass != null)
		{
			metricaClass.CallStatic("setEnvironmentValue", key, value);
		}
	}
}
