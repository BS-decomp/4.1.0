using AppodealAds.Unity.Common;
using UnityEngine;

namespace AppodealAds.Unity.Android
{
	public class AppodealBannerCallbacks : AndroidJavaProxy
	{
		private IBannerAdListener listener;

		internal AppodealBannerCallbacks(IBannerAdListener listener)
			: base("com.appodeal.ads.BannerCallbacks")
		{
			this.listener = listener;
		}

		private void onBannerLoaded(int height, bool isPrecache)
		{
			listener.onBannerLoaded();
		}

		private void onBannerFailedToLoad()
		{
			listener.onBannerFailedToLoad();
		}

		private void onBannerShown()
		{
			listener.onBannerShown();
		}

		private void onBannerClicked()
		{
			listener.onBannerClicked();
		}
	}
}
