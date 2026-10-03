using System;
using System.Collections;
using System.Collections.Generic;
using AppodealAds.Unity.Api;
using AppodealAds.Unity.Common;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

public class AdsManager : MonoBehaviour, IInterstitialAdListener, IRewardedVideoAdListener, ISkippableVideoAdListener
{
	public static bool isBlockAds;

	private static Action RewardedVideoComplete;

	private static Action RewardedVideoFailed;

	private static Action RewardedVideoAborted;

	private static int TimerID;

	private static int FinishRewardedVideo = -1;

	private void Start()
	{
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		Init();
	}

	public void Init()
	{
		StartCoroutine(CheckHostsFile());
		Appodeal.disableNetwork("avocarrot");
		Appodeal.disableNetwork("facebook");
		Appodeal.disableNetwork("flurry");
		Appodeal.disableNetwork("pubnative");
		Appodeal.disableNetwork("yandex");
		UserSettings userSettings = new UserSettings();
		if (!ObscuredPrefs.HasKey("Age"))
		{
			ObscuredPrefs.SetInt("Age", UnityEngine.Random.Range(12, 18));
		}
		userSettings.setAge(ObscuredPrefs.GetInt("Age"));
		if (!ObscuredPrefs.HasKey("Interests"))
		{
			List<string> list = new List<string>();
			list.Add("video games");
			list.Add("sport");
			list.Add("food");
			list.Add("business");
			list.Add("travel");
			list.Add("shopping");
			list.Add("science");
			list.Add("news");
			list.Add("entertainment");
			list.Add("action");
			list.Add("adventure");
			list.Add("arcade");
			list.Add("board");
			list.Add("card");
			list.Add("casino");
			list.Add("casual");
			list.Add("educational");
			list.Add("music");
			list.Add("puzzle");
			list.Add("racing");
			list.Add("role playing");
			list.Add("simulation");
			list.Add("sports");
			list.Add("strategy");
			list.Add("trivia");
			list.Add("word");
			List<string> list2 = list;
			int num = UnityEngine.Random.Range(5, 12);
			string text = string.Empty;
			for (int i = 0; i < num; i++)
			{
				int index = UnityEngine.Random.Range(0, list2.Count);
				text += list2[index];
				list2.RemoveAt(index);
				if (i < num - 1)
				{
					text += ", ";
				}
			}
			ObscuredPrefs.SetString("Interests", text);
		}
		userSettings.setInterests(ObscuredPrefs.GetString("Interests"));
		Appodeal.confirm(2);
		Appodeal.setRewardedVideoCallbacks(this);
		Appodeal.setSkippableVideoCallbacks(this);
		Appodeal.setInterstitialCallbacks(this);
		Appodeal.initialize("fd12215587d2499bf80048b3ef940e61a2f341760ce6d1b4", 131);
	}

	private IEnumerator CheckHostsFile()
	{
		WWW www = new WWW("file:///etc/hosts");
		yield return www;
		if (www.error == string.Empty && www.size > 100)
		{
			isBlockAds = true;
		}
	}

	public static void ShowAny()
	{
		ShowAny("default");
	}

	public static void ShowAny(string placement)
	{
		if (Appodeal.isLoaded(128))
		{
			Appodeal.show(128, placement);
		}
		else
		{
			Appodeal.show(3, placement);
		}
	}

	public static void ShowInterstitial()
	{
		ShowInterstitial("default");
	}

	public static void ShowInterstitial(string placement)
	{
		if (Appodeal.isLoaded(1))
		{
			Appodeal.show(1, placement);
		}
	}

	public static void ShowRewardedVideo(Action complete, Action failed, Action aborted)
	{
		ShowRewardedVideo(complete, failed, aborted, "default");
	}

	public static void ShowRewardedVideo(Action complete, Action failed, Action aborted, string placement)
	{
		RewardedVideoComplete = complete;
		RewardedVideoFailed = failed;
		RewardedVideoAborted = aborted;
		FinishRewardedVideo = -1;
		if (Appodeal.isLoaded(128))
		{
			Appodeal.show(128, placement);
		}
		else if (Appodeal.isLoaded(2))
		{
			Appodeal.show(2, placement);
		}
		else
		{
			RewardedVideoFinished(2);
		}
	}

	private static void RewardedVideoFinished(int isComplete)
	{
		if (FinishRewardedVideo != 0)
		{
			FinishRewardedVideo = isComplete;
		}
		if (TimerManager.IsActive(TimerID))
		{
			return;
		}
		TimerID = TimerManager.In(0.2f, () =>
		{
			if (isComplete == 0)
			{
				if (RewardedVideoComplete != null)
				{
					RewardedVideoComplete();
				}
			}
			else if (isComplete == 1)
			{
				if (RewardedVideoAborted != null)
				{
					RewardedVideoAborted();
				}
			}
			else if (RewardedVideoFailed != null)
			{
				RewardedVideoFailed();
			}
			RewardedVideoComplete = null;
			RewardedVideoFailed = null;
			RewardedVideoAborted = null;
		});
	}

	public void onRewardedVideoLoaded()
	{
		if (Settings.Console)
		{
			TimerManager.In(0.1f, () =>
			{
				MonoBehaviour.print("Rewarded Video loaded");
			});
		}
	}

	public void onRewardedVideoFailedToLoad()
	{
		RewardedVideoFinished(1);
		if (Settings.Console)
		{
			TimerManager.In(0.1f, () =>
			{
				MonoBehaviour.print("Rewarded Video failed");
			});
		}
	}

	public void onRewardedVideoShown()
	{
		if (Settings.Console)
		{
			TimerManager.In(0.1f, () =>
			{
				MonoBehaviour.print("Rewarded Video shown");
			});
		}
	}

	public void onRewardedVideoClosed()
	{
		if (Settings.Console)
		{
			TimerManager.In(0.1f, () =>
			{
				MonoBehaviour.print("Rewarded Video closed");
			});
		}
	}

	public void onRewardedVideoFinished(int amount, string name)
	{
		RewardedVideoFinished(0);
		if (Settings.Console)
		{
			TimerManager.In(0.1f, () =>
			{
				MonoBehaviour.print("Rewarded Video finished: Reward: " + amount + name);
			});
		}
	}

	public void onSkippableVideoLoaded()
	{
		if (Settings.Console)
		{
			TimerManager.In(0.1f, () =>
			{
				MonoBehaviour.print("Video loaded");
			});
		}
	}

	public void onSkippableVideoFailedToLoad()
	{
		RewardedVideoFinished(2);
		if (Settings.Console)
		{
			TimerManager.In(0.1f, () =>
			{
				MonoBehaviour.print("Video failed");
			});
		}
	}

	public void onSkippableVideoShown()
	{
		if (Settings.Console)
		{
			TimerManager.In(0.1f, () =>
			{
				MonoBehaviour.print("Video shown");
			});
		}
	}

	public void onSkippableVideoFinished()
	{
		RewardedVideoFinished(0);
		if (Settings.Console)
		{
			TimerManager.In(0.1f, () =>
			{
				MonoBehaviour.print("Video finished");
			});
		}
	}

	public void onSkippableVideoClosed()
	{
		RewardedVideoFinished(1);
		if (Settings.Console)
		{
			TimerManager.In(0.1f, () =>
			{
				MonoBehaviour.print("Video closed");
			});
		}
	}

	public void onInterstitialLoaded()
	{
		if (Settings.Console)
		{
			TimerManager.In(0.1f, () =>
			{
				MonoBehaviour.print("Interstitial loaded");
			});
		}
	}

	public void onInterstitialFailedToLoad()
	{
		if (Settings.Console)
		{
			TimerManager.In(0.1f, () =>
			{
				MonoBehaviour.print("Interstitial failed");
			});
		}
	}

	public void onInterstitialShown()
	{
		if (Settings.Console)
		{
			TimerManager.In(0.1f, () =>
			{
				MonoBehaviour.print("Interstitial opened");
			});
		}
	}

	public void onInterstitialClosed()
	{
		if (Settings.Console)
		{
			TimerManager.In(0.1f, () =>
			{
				MonoBehaviour.print("Interstitial closed");
			});
		}
	}

	public void onInterstitialClicked()
	{
		if (Settings.Console)
		{
			TimerManager.In(0.1f, () =>
			{
				MonoBehaviour.print("Interstitial clicked");
			});
		}
	}
}
