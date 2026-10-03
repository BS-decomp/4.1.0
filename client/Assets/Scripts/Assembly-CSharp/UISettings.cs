using System;
using UnityEngine;

public class UISettings : MonoBehaviour
{
	[Header("General")]
	private int SelectLanguage;

	public UILabel RegionLabel;

	private CloudRegionCode RegionCode;

	public UIToggle FPSMeter;

	public UIToggle Chat;

	public UIToggle Console;

	public UIToggle ShowDamage;

	public UIToggle BulletHole;

	public UIToggle Blood;

	public UIToggle HitMaker;

	public UIToggle HUD;

	public UIToggle ShowWeapon;

	public UISprite СolorСrosshair;

	public UIToggle Shell;

	public UIToggle ProjectileEffect;

	public UIToggle SmokePlume;

	public UIToggle SyncHealth;

	private int SelectСolorСrosshair;

	[Header("Control")]
	public UISlider Sensitivity;

	public UILabel SensitivityLabel;

	public UISlider ButtonAlpha;

	public UILabel ButtonAlphaLabel;

	public UIToggle Lefty;

	[Header("Audio")]
	public UISlider Volume;

	public UILabel VolumeLabel;

	public UIToggle Audio;

	public UIToggle AmbientAudio;

	private void Start()
	{
		Load();
	}

	private void Load()
	{
		Settings.Load();
		UpdateLanguage();
		FPSMeter.value = Settings.FPSMeter;
		Chat.value = Settings.Chat;
		Console.value = Settings.Console;
		UpdateConsole();
		ShowDamage.value = Settings.ShowDamage;
		BulletHole.value = Settings.BulletHole;
		Blood.value = Settings.Blood;
		HitMaker.value = Settings.HitMaker;
		HUD.value = Settings.HUD;
		ShowWeapon.value = Settings.ShowWeapon;
		Shell.value = Settings.Shell;
		ProjectileEffect.value = Settings.ProjectileEffect;
		SmokePlume.value = Settings.SmokePlume;
		SyncHealth.value = Settings.SyncHealth;
		UpdateColorCrosshair();
		UpdateSensitivity();
		UpdateButtonAlpha();
		Lefty.value = Settings.Lefty;
		UpdateVolume();
		Audio.value = Settings.Audio;
		AmbientAudio.value = Settings.AmbientAudio;
	}

	public void Save()
	{
		Settings.FPSMeter = FPSMeter.value;
		Settings.Chat = Chat.value;
		Settings.Console = Console.value;
		UpdateConsole();
		Settings.ShowDamage = ShowDamage.value;
		Settings.BulletHole = BulletHole.value;
		Settings.Blood = Blood.value;
		Settings.HitMaker = HitMaker.value;
		Settings.HUD = HUD.value;
		Settings.ShowWeapon = ShowWeapon.value;
		Settings.Shell = Shell.value;
		Settings.ProjectileEffect = ProjectileEffect.value;
		Settings.SmokePlume = SmokePlume.value;
		Settings.SyncHealth = SyncHealth.value;
		Settings.Lefty = Lefty.value;
		Settings.Audio = Audio.value;
		Settings.AmbientAudio = AmbientAudio.value;
		Settings.Save();
		EventManager.Dispatch("UpdateSettings");
	}

	public void Default()
	{
		Settings.FPSMeter = false;
		Settings.Chat = true;
		Settings.Console = false;
		UpdateConsole();
		Settings.ShowDamage = false;
		Settings.BulletHole = true;
		Settings.Blood = true;
		Settings.HitMaker = true;
		Settings.HUD = true;
		Settings.ShowWeapon = true;
		Settings.SmokePlume = true;
		Settings.Shell = true;
		Settings.ProjectileEffect = true;
		Settings.SyncHealth = false;
		Settings.Sensitivity = 0.2f;
		Settings.ButtonAlpha = 1f;
		Settings.Lefty = false;
		Settings.Volume = 0.8f;
		Settings.Audio = true;
		Settings.AmbientAudio = true;
		Settings.Save();
		Load();
		EventManager.Dispatch("UpdateSettings");
	}

	private void UpdateLanguage()
	{
		if (PlayerPrefs.HasKey("Language"))
		{
			SetLanguage(PlayerPrefs.GetString("Language"));
		}
		else if (Application.systemLanguage == SystemLanguage.Russian || Application.systemLanguage == SystemLanguage.Ukrainian || Application.systemLanguage == SystemLanguage.Belarusian)
		{
			SetLanguage("Russia");
		}
		else if (Application.systemLanguage == SystemLanguage.English)
		{
			SetLanguage("English");
		}
		else if (Application.systemLanguage == SystemLanguage.Korean)
		{
			SetLanguage("Korean");
		}
		else if (Application.systemLanguage == SystemLanguage.Spanish)
		{
			SetLanguage("Spanish");
		}
		else if (Application.systemLanguage == SystemLanguage.Portuguese)
		{
			SetLanguage("Portuguese");
		}
		else if (Application.systemLanguage == SystemLanguage.French)
		{
			SetLanguage("French");
		}
		else if (Application.systemLanguage == SystemLanguage.Japanese)
		{
			SetLanguage("Japan");
		}
		else if (Application.systemLanguage == SystemLanguage.Polish)
		{
			SetLanguage("Polish");
		}
	}

	private void SetLanguage(string language)
	{
		for (int i = 0; i < Localization.knownLanguages.Length; i++)
		{
			if (Localization.knownLanguages[i] == language)
			{
				Localization.language = Localization.knownLanguages[i];
				SelectLanguage = i;
				break;
			}
		}
	}

	public void NextLanguage()
	{
		SelectLanguage++;
		if (SelectLanguage > Localization.knownLanguages.Length - 1)
		{
			SelectLanguage = 0;
		}
		SetLanguage(Localization.knownLanguages[SelectLanguage]);
	}

	public void LastLanguage()
	{
		SelectLanguage--;
		if (SelectLanguage < 0)
		{
			SelectLanguage = Localization.knownLanguages.Length - 1;
		}
		SetLanguage(Localization.knownLanguages[SelectLanguage]);
	}

	private void UpdateRegion()
	{
		if (PlayerPrefs.HasKey("SelectRegion"))
		{
			string text = PlayerPrefs.GetString("SelectRegion");
			RegionCode = (CloudRegionCode)(int)Enum.Parse(typeof(CloudRegionCode), text);
			switch (RegionCode)
			{
			case CloudRegionCode.eu:
				text = Localization.Get("Europe");
				break;
			case CloudRegionCode.us:
				text = Localization.Get("USA");
				break;
			case CloudRegionCode.asia:
				text = Localization.Get("Asia");
				break;
			case CloudRegionCode.jp:
				text = Localization.Get("Japan");
				break;
			case CloudRegionCode.au:
				text = Localization.Get("Australia");
				break;
			case CloudRegionCode.sa:
				text = Localization.Get("Brazil");
				break;
			}
			RegionLabel.text = Localization.Get("Region") + ": " + text;
		}
		else
		{
			RegionLabel.text = Localization.Get("Region") + ": Auto";
		}
	}

	public void NextRegion()
	{
		int regionCode = (int)RegionCode;
		regionCode++;
		if (regionCode == 4)
		{
			regionCode++;
		}
		if (regionCode > Enum.GetValues(typeof(CloudRegionCode)).Length - 1)
		{
			regionCode = 0;
		}
		PlayerPrefs.SetString("SelectRegion", ((CloudRegionCode)regionCode).ToString());
		UpdateRegion();
	}

	public void LastRegion()
	{
		int regionCode = (int)RegionCode;
		regionCode--;
		if (regionCode == 4)
		{
			regionCode--;
		}
		if (regionCode < 0)
		{
			regionCode = Enum.GetValues(typeof(CloudRegionCode)).Length - 1;
		}
		PlayerPrefs.SetString("SelectRegion", ((CloudRegionCode)regionCode).ToString());
		UpdateRegion();
	}

	private void UpdateConsole()
	{
		Utils.SetActiveConsole(Settings.Console);
	}

	private void UpdateColorCrosshair()
	{
		SelectСolorСrosshair = Settings.ColorCrosshair;
		Color color = Utils.GetColor(SelectСolorСrosshair);
		if (color == Color.clear)
		{
			color = new Color(1f, 1f, 1f, 0.5f);
		}
		СolorСrosshair.color = color;
	}

	public void SetСolorСrosshair()
	{
		SelectСolorСrosshair++;
		if (9 < SelectСolorСrosshair)
		{
			SelectСolorСrosshair = 0;
		}
		Color color = Utils.GetColor(SelectСolorСrosshair);
		if (color.a == 0f)
		{
			color = new Color(1f, 1f, 1f, 0.05f);
		}
		СolorСrosshair.color = color;
		Settings.ColorCrosshair = SelectСolorСrosshair;
	}

	private void UpdateSensitivity()
	{
		Sensitivity.value = Settings.Sensitivity;
		SensitivityLabel.text = Localization.Get("Sensitivity") + ": " + Mathf.RoundToInt(Sensitivity.value * 100f) + "%";
	}

	public void SetSensitivity()
	{
		Settings.Sensitivity = Sensitivity.value;
		SensitivityLabel.text = Localization.Get("Sensitivity") + ": " + Mathf.RoundToInt(Sensitivity.value * 100f) + "%";
	}

	private void UpdateButtonAlpha()
	{
		ButtonAlpha.value = Settings.ButtonAlpha;
		ButtonAlphaLabel.text = Localization.Get("Button Alpha") + ": " + Mathf.RoundToInt(ButtonAlpha.value * 100f) + "%";
	}

	public void SetButtonAlpha()
	{
		Settings.ButtonAlpha = Mathf.Clamp(ButtonAlpha.value, 0.01f, 1f);
		ButtonAlphaLabel.text = Localization.Get("Button Alpha") + ": " + Mathf.RoundToInt(ButtonAlpha.value * 100f) + "%";
	}

	private void UpdateVolume()
	{
		Volume.value = Settings.Volume;
		VolumeLabel.text = Localization.Get("Volume") + ": " + Mathf.RoundToInt(Volume.value * 100f) + "%";
	}

	public void SetVolume()
	{
		Settings.Volume = Volume.value;
		VolumeLabel.text = Localization.Get("Volume") + ": " + Mathf.RoundToInt(Volume.value * 100f) + "%";
	}

	public void DefaultButtons()
	{
		EventManager.Dispatch("DefaultButton");
	}

	public void SaveButtons()
	{
		EventManager.Dispatch("SaveButton");
	}
}
