using System.Collections;
using UnityEngine;

public class mCheckUpdateGame : MonoBehaviour
{
	private void Start()
	{
		if (!AccountManager.isConnect)
		{
			StartCoroutine(CheckGame());
		}
	}

	private void Show()
	{
		AndroidNativeFunctions.ShowAlert(Localization.Get("Available new version of the game"), Localization.Get("New Version"), Localization.Get("Download"), string.Empty, string.Empty, Download);
		GameSettings.instance.PhotonID = string.Empty;
		TimerManager.In(0.1f, -1, 0.1f, () =>
		{
			AccountManager.isConnect = false;
		});
		TimerManager.In(20f, () =>
		{
			Application.Quit();
		});
	}

	private IEnumerator CheckGame()
	{
		string url = "https://play.google.com/store/apps/details?id=com.rexetstudio.blockstrike&hl=en";
		WWW www = new WWW(url);
		yield return www;
		if (string.IsNullOrEmpty(www.error))
		{
			if (StringToInt(VersionManager.bundleVersion) < StringToInt(GetVersion(www.text)))
			{
				Show();
			}
		}
		else
		{
			StartCoroutine(CheckGame());
		}
	}

	private void Download(DialogInterface dialog)
	{
		AndroidNativeFunctions.OpenGooglePlay("com.rexetstudio.blockstrike");
		AndroidNativeFunctions.ShowAlert(Localization.Get("Available new version of the game"), Localization.Get("New Version"), Localization.Get("Download"), string.Empty, string.Empty, Download);
	}

	private string GetVersion(string data)
	{
		string text = data;
		int count = text.LastIndexOf("softwareVersion") + 18;
		text = text.Remove(0, count);
		count = text.IndexOf("</div>") - 2;
		return text.Remove(count);
	}

	private int StringToInt(string text)
	{
		string text2 = string.Empty;
		for (int i = 0; i < text.Length; i++)
		{
			if (char.IsDigit(text[i]))
			{
				text2 += text[i];
			}
		}
		return int.Parse(text2);
	}
}
