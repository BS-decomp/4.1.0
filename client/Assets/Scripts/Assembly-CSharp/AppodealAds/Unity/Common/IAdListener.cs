namespace AppodealAds.Unity.Common
{
	internal interface IAdListener
	{
		void FireRewardUser(int amount);

		void FireAdLoaded();

		void FireAdFailedToLoad(string message);

		void FireAdOpened();

		void FireAdClosing();

		void FireAdClosed();

		void FireAdLeftApplication();
	}
}
