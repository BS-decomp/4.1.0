using AppodealAds.Unity.Common;

namespace AppodealAds.Unity.Api
{
	public class Appodeal
	{
		public const int NONE = 0;

		public const int INTERSTITIAL = 1;

		public const int SKIPPABLE_VIDEO = 2;

		public const int BANNER = 4;

		public const int BANNER_BOTTOM = 8;

		public const int BANNER_TOP = 16;

		public const int REWARDED_VIDEO = 128;

		public const int NON_SKIPPABLE_VIDEO = 128;

		private static IAppodealAdsClient client;

		private static IAppodealAdsClient getInstance()
		{
			if (client == null)
			{
				client = AppodealAdsClientFactory.GetAppodealAdsClient();
			}
			return client;
		}

		public static void initialize(string appKey, int adTypes)
		{
			getInstance().initialize(appKey, adTypes);
		}

		public static void setInterstitialCallbacks(IInterstitialAdListener listener)
		{
			getInstance().setInterstitialCallbacks(listener);
		}

		public static void setSkippableVideoCallbacks(ISkippableVideoAdListener listener)
		{
			getInstance().setSkippableVideoCallbacks(listener);
		}

		public static void setNonSkippableVideoCallbacks(INonSkippableVideoAdListener listener)
		{
			getInstance().setNonSkippableVideoCallbacks(listener);
		}

		public static void setRewardedVideoCallbacks(IRewardedVideoAdListener listener)
		{
			getInstance().setRewardedVideoCallbacks(listener);
		}

		public static void setBannerCallbacks(IBannerAdListener listener)
		{
			getInstance().setBannerCallbacks(listener);
		}

		public static void cache(int adTypes)
		{
			getInstance().cache(adTypes);
		}

		public static void confirm(int adTypes)
		{
			getInstance().confirm(adTypes);
		}

		public static bool isLoaded(int adTypes)
		{
			bool flag = false;
			return getInstance().isLoaded(adTypes);
		}

		public static bool isPrecache(int adTypes)
		{
			bool flag = false;
			return getInstance().isPrecache(adTypes);
		}

		public static bool show(int adTypes)
		{
			bool flag = false;
			return getInstance().show(adTypes);
		}

		public static bool show(int adTypes, string placement)
		{
			bool flag = false;
			return getInstance().show(adTypes, placement);
		}

		public static void hide(int adTypes)
		{
			getInstance().hide(adTypes);
		}

		public static void orientationChange()
		{
			getInstance().orientationChange();
		}

		public static void setAutoCache(int adTypes, bool autoCache)
		{
			getInstance().setAutoCache(adTypes, autoCache);
		}

		public static void setOnLoadedTriggerBoth(int adTypes, bool onLoadedTriggerBoth)
		{
			getInstance().setOnLoadedTriggerBoth(adTypes, onLoadedTriggerBoth);
		}

		public static void disableNetwork(string network)
		{
			getInstance().disableNetwork(network);
		}

		public static void disableNetwork(string network, int adType)
		{
			getInstance().disableNetwork(network, adType);
		}

		public static void disableLocationPermissionCheck()
		{
			getInstance().disableLocationPermissionCheck();
		}

		public static void disableWriteExternalStoragePermissionCheck()
		{
			getInstance().disableWriteExternalStoragePermissionCheck();
		}

		public static void requestAndroidMPermissions(IPermissionGrantedListener listener)
		{
			getInstance().requestAndroidMPermissions(listener);
		}

		public static void setTesting(bool test)
		{
			getInstance().setTesting(test);
		}

		public static void setLogging(bool logging)
		{
			getInstance().setLogging(logging);
		}

		public static string getVersion()
		{
			string text = null;
			return getInstance().getVersion();
		}

		public static void trackInAppPurchase(double amount, string currency)
		{
			getInstance().trackInAppPurchase(amount, currency);
		}

		public static void setCustomRule(string name, bool value)
		{
			getInstance().setCustomRule(name, value);
		}

		public static void setCustomRule(string name, int value)
		{
			getInstance().setCustomRule(name, value);
		}

		public static void setCustomRule(string name, double value)
		{
			getInstance().setCustomRule(name, value);
		}

		public static void setCustomRule(string name, string value)
		{
			getInstance().setCustomRule(name, value);
		}

		public static void setSmartBanners(bool value)
		{
			getInstance().setSmartBanners(value);
		}

		public static void setBannerBackground(bool value)
		{
			getInstance().setBannerBackground(value);
		}

		public static void setBannerAnimation(bool value)
		{
			getInstance().setBannerAnimation(value);
		}
	}
}
