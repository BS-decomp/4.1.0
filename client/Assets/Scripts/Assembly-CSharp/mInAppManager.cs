using UnityEngine;

public class mInAppManager : MonoBehaviour
{
	private CryptoInt RewardedVideoMoney = 50;

	private CryptoInt RewardedVideoGold = 1;

	private GameCurrency Currency;

	private bool isRewardedVideo;

	public void OnRewardedVideo(int currency)
	{
		if (!isRewardedVideo)
		{
			Currency = (GameCurrency)currency;
			UIToast.Show(Localization.Get("Please wait") + "...");
			isRewardedVideo = true;
			TimerManager.In(0.5f, () =>
			{
				AdsManager.ShowRewardedVideo(RewardedVideoComplete, RewardedVideoFailed, RewardedVideoAborted, (Currency != GameCurrency.Gold) ? "RewardedMoney" : "RewardedGold");
			});
		}
	}

	private void RewardedVideoComplete()
	{
		TimerManager.In(0.3f, () =>
		{
			if (Currency == GameCurrency.Money)
			{
				AccountManager.SetMoney1(RewardedVideoMoney);
				UIToast.Show("+50 " + Localization.Get("Money"));
			}
			else
			{
				AccountManager.SetGold1(RewardedVideoGold);
				UIToast.Show("+1 " + Localization.Get("Gold"));
			}
			EventManager.Dispatch("AccountUpdate");
			isRewardedVideo = false;
		});
	}

	private void RewardedVideoAborted()
	{
		UIToast.Show(Localization.Get("Cancel"));
		isRewardedVideo = false;
	}

	private void RewardedVideoFailed()
	{
		isRewardedVideo = false;
		UIToast.Show(Localization.Get("Video not available"), 3f);
	}

	public void Close()
	{
		AccountManager.UpdateDefaultData(null, null);
	}
}
