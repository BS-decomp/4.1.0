using AppodealAds.Unity.Common;
using UnityEngine;

namespace AppodealAds.Unity.Android
{
	public class AppodealRewardedVideoCallbacks : AndroidJavaProxy
	{
		private IRewardedVideoAdListener listener;

		internal AppodealRewardedVideoCallbacks(IRewardedVideoAdListener listener)
			: base("com.appodeal.ads.RewardedVideoCallbacks")
		{
			this.listener = listener;
		}

		private void onRewardedVideoLoaded()
		{
			listener.onRewardedVideoLoaded();
		}

		private void onRewardedVideoFailedToLoad()
		{
			listener.onRewardedVideoFailedToLoad();
		}

		private void onRewardedVideoShown()
		{
			listener.onRewardedVideoShown();
		}

		private void onRewardedVideoFinished(int amount, AndroidJavaObject name)
		{
			listener.onRewardedVideoFinished(amount, null);
		}

		private void onRewardedVideoFinished(int amount, string name)
		{
			listener.onRewardedVideoFinished(amount, name);
		}

		private void onRewardedVideoClosed(bool finished)
		{
			listener.onRewardedVideoClosed();
		}
	}
}
