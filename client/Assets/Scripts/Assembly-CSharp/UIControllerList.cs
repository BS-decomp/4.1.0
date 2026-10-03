using UnityEngine;

public class UIControllerList : MonoBehaviour
{
	public UISprite FireSprite;

	public UISprite JumpSprite;

	public UISprite ReloadSprite;

	public UISprite AimSprite;

	public UISprite StatsSprite;

	public UISprite ChatSprite;

	public UISprite SelectWeaponSprite;

	public UISprite UseSprite;

	public UISprite MicrophoneSprite;

	private static UIControllerList instance;

	public static UISprite Fire
	{
		get
		{
			return instance.FireSprite;
		}
	}

	public static UISprite Jump
	{
		get
		{
			return instance.JumpSprite;
		}
	}

	public static UISprite Reload
	{
		get
		{
			return instance.ReloadSprite;
		}
	}

	public static UISprite Aim
	{
		get
		{
			return instance.AimSprite;
		}
	}

	public static UISprite Stats
	{
		get
		{
			return instance.StatsSprite;
		}
	}

	public static UISprite Chat
	{
		get
		{
			return instance.ChatSprite;
		}
	}

	public static UISprite SelectWeapon
	{
		get
		{
			return instance.SelectWeaponSprite;
		}
	}

	public static UISprite Use
	{
		get
		{
			return instance.UseSprite;
		}
	}

	public static UISprite Microphone
	{
		get
		{
			return instance.MicrophoneSprite;
		}
	}

	private void Start()
	{
		instance = this;
	}
}
