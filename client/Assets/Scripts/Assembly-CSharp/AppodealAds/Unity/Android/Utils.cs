namespace AppodealAds.Unity.Android
{
	internal class Utils
	{
		public const string AdListenerClassName = "com.appodeal.ads.Appodeal.AdListener";

		public const string AdRequestClassName = "com.appodeal.ads.Appodeal.AODAdRequest";

		public const string AppodealClassName = "com.appodeal.ads.Appodeal";

		public const string BannerViewClassName = "com.appodeal.ads.Appodeal";

		public const string InterstitialClassName = "com.appodeal.ads.Appodeal";

		public const string VideoClassName = "com.appodeal.ads.Appodeal";

		public const string UnityAdBannerListenerClassName = "com.appodeal.ads.BannerCallbacks";

		public const string UnityVideoAdListenerClassName = "com.appodeal.ads.VideoCallbacks";

		public const string UnityInterstitialAdListenerClassName = "com.appodeal.ads.InterstitialCallbacks";

		public const string UnityActivityClassName = "com.unity3d.player.UnityPlayer";

		public const string BundleClassName = "android.os.Bundle";

		public const string DateClassName = "java.util.Date";

		public static string GetListenerFromType(int type)
		{
			switch (type)
			{
			case 4:
				return "com.appodeal.ads.BannerCallbacks";
			case 1:
				return "com.appodeal.ads.InterstitialCallbacks";
			case 2:
				return "com.appodeal.ads.VideoCallbacks";
			default:
				return "com.appodeal.ads.BannerCallbacks";
			}
		}
	}
}
