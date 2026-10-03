using UnityEngine;

public class mVersionManager : MonoBehaviour
{
	public UILabel VersionLabel;

	private static mVersionManager instance;

	private void Start()
	{
		instance = this;
		VersionLabel.text = VersionManager.bundleVersion;
		UpdateRegion();
	}

	private void OnLocalize()
	{
		UpdateRegion();
	}

	public static void UpdateRegion()
	{
		string text = PlayerPrefs.GetString("SelectRegion", "Best");
		string bundleVersion = VersionManager.bundleVersion;
		switch (text)
		{
		case "eu":
			bundleVersion = bundleVersion + "\n" + Localization.Get("Europe");
			break;
		case "us":
			bundleVersion = bundleVersion + "\n" + Localization.Get("USA");
			break;
		case "kr":
			bundleVersion = bundleVersion + "\n" + Localization.Get("South Korea");
			break;
		case "sa":
			bundleVersion = bundleVersion + "\n" + Localization.Get("Brazil");
			break;
		case "jp":
			bundleVersion = bundleVersion + "\n" + Localization.Get("Japan");
			break;
		case "in":
			bundleVersion = bundleVersion + "\n" + Localization.Get("India");
			break;
		case "au":
			bundleVersion = bundleVersion + "\n" + Localization.Get("Australia");
			break;
		default:
			bundleVersion += "\nBest";
			break;
		}
		instance.VersionLabel.text = bundleVersion;
	}
}
