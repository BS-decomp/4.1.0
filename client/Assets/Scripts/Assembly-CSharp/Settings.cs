using UnityEngine;

public class Settings
{
	public static bool FPSMeter;

	public static bool Console;

	public static bool Chat = true;

	public static bool ShowDamage;

	public static bool BulletHole = true;

	public static bool Blood = true;

	public static bool HitMaker = true;

	public static int ColorCrosshair;

	public static float Sensitivity = 0.2f;

	public static float Volume = 0.8f;

	public static bool Audio = true;

	public static bool AmbientAudio = true;

	public static bool Lefty;

	public static float ButtonAlpha = 1f;

	public static bool HUD = true;

	public static bool ShowWeapon = true;

	public static bool Shell = true;

	public static bool ProjectileEffect = true;

	public static bool SmokePlume = true;

	public static bool SyncHealth;

	public static void Load()
	{
		FPSMeter = GetBool("FPSMeter", false);
		Console = GetBool("Console", false);
		Chat = GetBool("Chat", true);
		ShowDamage = GetBool("ShowDamage", false);
		BulletHole = GetBool("BulletHole", true);
		Blood = GetBool("Blood", true);
		HitMaker = GetBool("HitMaker", true);
		ColorCrosshair = GetInt("ColorCrosshair", 0);
		Sensitivity = GetFloat("Sensitivity", 0.2f);
		Volume = GetFloat("Volume", 0.8f);
		Audio = GetBool("Audio", true);
		AmbientAudio = GetBool("AmbientAudio", true);
		Lefty = GetBool("Lefty", false);
		ButtonAlpha = Mathf.Clamp(GetFloat("ButtonAlpha", 1f), 0.01f, 1f);
		HUD = GetBool("HUD", true);
		ShowWeapon = GetBool("ShowWeapon", true);
		Shell = GetBool("Shell", true);
		ProjectileEffect = GetBool("ProjectileEffect", true);
		SmokePlume = GetBool("SmokePlume", true);
		SyncHealth = GetBool("SyncHealth", false);
		AudioListener.volume = Volume;
	}

	public static void Save()
	{
		SetBool("FPSMeter", FPSMeter);
		SetBool("Console", Console);
		SetBool("Chat", Chat);
		SetBool("ShowDamage", ShowDamage);
		SetBool("BulletHole", BulletHole);
		SetBool("Blood", Blood);
		SetBool("HitMaker", HitMaker);
		SetInt("ColorCrosshair", ColorCrosshair);
		SetFloat("Sensitivity", Sensitivity);
		SetFloat("Volume", Volume);
		SetBool("Audio", Audio);
		SetBool("AmbientAudio", AmbientAudio);
		SetBool("Lefty", Lefty);
		SetFloat("ButtonAlpha", Mathf.Clamp(ButtonAlpha, 0.01f, 1f));
		SetBool("HUD", HUD);
		SetBool("ShowWeapon", ShowWeapon);
		SetBool("Shell", Shell);
		SetBool("ProjectileEffect", ProjectileEffect);
		SetBool("SmokePlume", SmokePlume);
		SetBool("SyncHealth", SyncHealth);
		AudioListener.volume = Volume;
	}

	private static bool GetBool(string key, bool defaultValue)
	{
		if (PlayerPrefs.HasKey(key))
		{
			return PlayerPrefs.GetInt(key) == 1;
		}
		return defaultValue;
	}

	private static int GetInt(string key, int defaultValue)
	{
		return PlayerPrefs.GetInt(key, defaultValue);
	}

	private static float GetFloat(string key, float defaultValue)
	{
		return PlayerPrefs.GetFloat(key, defaultValue);
	}

	private static void SetBool(string key, bool value)
	{
		PlayerPrefs.SetInt(key, value ? 1 : 0);
	}

	private static void SetInt(string key, int value)
	{
		PlayerPrefs.SetInt(key, value);
	}

	private static void SetFloat(string key, float value)
	{
		PlayerPrefs.SetFloat(key, value);
	}
}
