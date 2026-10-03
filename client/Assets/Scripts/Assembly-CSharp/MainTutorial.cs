using UnityEngine;

public class MainTutorial : MonoBehaviour
{
	public Transform Spawn;

	[Header("Buttons")]
	public GameObject FireButton;

	public GameObject ReloadButton;

	public GameObject JumpButton;

	public GameObject SelectWeaponButton;

	public GameObject AimButton;

	public GameObject ChatButton;

	public GameObject StatsButton;

	public GameObject PauseButton;

	public GameObject Joystick;

	public GameObject TouchLook;

	[Header("UI")]
	public GameObject CrosshairUI;

	public GameObject JoystickZone;

	public GameObject LookZone;

	[Header("Targets")]
	public TutorialTarget Target1;

	public TutorialTarget Target2;

	public TutorialTarget Target3;

	[Header("Weapons")]
	public GameObject Rifle;

	public GameObject Pistol;

	private void Start()
	{
		TimerManager.In(0.05f, () =>
		{
			UpdateLanguage();
			UIPanelManager.ShowPanel("Display");
			InputManager.Init();
			WeaponManager.Init();
			SetWeapons();
			CreatePlayer();
			UIGameManager.SetActiveScore(false, 0);
			StartButtons();
			StartUI();
			Tutorial01();
		});
	}

	private void CreatePlayer()
	{
		PlayerInput playerInput = GameManager.GetController().PlayerInput;
		playerInput.SetHealth(0);
		CameraManager.DeactiveAll();
		GameManager.GetController().ActivePlayer(Spawn.position, Spawn.eulerAngles);
		playerInput.PlayerWeapon.UpdateWeaponAll(WeaponType.Knife);
		playerInput.PlayerWeapon.InfiniteAmmo = true;
	}

	private void SetWeapons()
	{
		WeaponManager.SetSelectWeapon(WeaponType.Knife, 0);
		WeaponManager.SetSelectWeapon(WeaponType.Pistol, 0);
		WeaponManager.SetSelectWeapon(WeaponType.Rifle, 0);
	}

	private void StartButtons()
	{
		FireButton.SetActive(false);
		ReloadButton.SetActive(false);
		JumpButton.SetActive(false);
		SelectWeaponButton.SetActive(false);
		AimButton.SetActive(false);
		ChatButton.SetActive(false);
		StatsButton.SetActive(false);
		PauseButton.SetActive(false);
		Joystick.SetActive(false);
		TouchLook.SetActive(false);
	}

	private void StartUI()
	{
		CrosshairUI.SetActive(false);
	}

	private void Tutorial01()
	{
		TimerManager.In(1f, () =>
		{
			TweenAlpha.Begin(JoystickZone, 1f, 0.5f);
			Joystick.SetActive(true);
			UIToast.Show(Localization.Get("Use the joystick to move"), 3f);
			SetWeapons();
		});
	}

	public void Tutorial02()
	{
		TweenAlpha.Begin(JoystickZone, 1f, 0f);
		TweenAlpha.Begin(LookZone, 1f, 0.5f);
		TouchLook.SetActive(true);
		UIToast.Show(Localization.Get("Use the right part of the screen to look around"), 3f);
	}

	public void Tutorial03()
	{
		TweenAlpha.Begin(LookZone, 1f, 0f);
		JumpButton.SetActive(true);
		UIToast.Show(Localization.Get("Use the jump to pass the obstacles"), 3f);
	}

	public void Tutorial04()
	{
		UIToast.Show(Localization.Get("Climb the ladder"), 3f);
	}

	public void Tutorial05()
	{
		UIToast.Show(Localization.Get("Go to the weapon to pick up"), 3f);
	}

	public void Tutorial06()
	{
		Rifle.SetActive(false);
		Pistol.SetActive(false);
		WeaponManager.SetSelectWeapon(WeaponType.Rifle, 1);
		PlayerInput.instance.PlayerWeapon.UpdateWeaponAll();
		TimerManager.In(1f, () =>
		{
			InputManager.SetButtonDown("Reload");
			TimerManager.In(2f, () =>
			{
				FireButton.SetActive(true);
				CrosshairUI.SetActive(true);
				UIToast.Show(Localization.Get("Press the button to fire"), 3f);
				InputManager.GetButtonDownEvent += Tutorial06_1;
			});
		});
	}

	private void Tutorial06_1(string button)
	{
		if (!(button != "Fire"))
		{
			InputManager.GetButtonDownEvent -= Tutorial06_1;
			TimerManager.In(0.5f, () =>
			{
				UIToast.Show(Localization.Get("Shoot at targets"), 3f);
				Target1.SetActive(true);
			});
		}
	}

	public void DeactivedTarget(int target)
	{
		switch (target)
		{
		case 1:
			Target1.SetActive(false);
			Target2.SetActive(true);
			break;
		case 2:
			Target2.SetActive(false);
			Target3.SetActive(true);
			break;
		case 3:
			Target3.SetActive(false);
			break;
		}
		if (!Target1.GetActive() && !Target2.GetActive() && !Target3.GetActive())
		{
			Tutorial06_2();
		}
	}

	private void Tutorial06_2()
	{
		ReloadButton.SetActive(true);
		UIToast.Show(Localization.Get("Press the button to reload"), 3f);
		InputManager.GetButtonDownEvent += Tutorial06_3;
	}

	private void Tutorial06_3(string button)
	{
		if (!(button != "Reload"))
		{
			InputManager.GetButtonDownEvent -= Tutorial06_3;
			TimerManager.In(2.5f, () =>
			{
				WeaponManager.SetSelectWeapon(WeaponType.Pistol, 2);
				PlayerInput.instance.PlayerWeapon.UpdateWeaponAll();
				UIToast.Show(Localization.Get("Switch weapons"), 3f);
				InputManager.GetButtonDownEvent += Tutorial06_4;
			});
		}
	}

	private void Tutorial06_4(string button)
	{
		if (button != "SelectWeapon")
		{
			return;
		}
		InputManager.GetButtonDownEvent -= Tutorial06_4;
		TimerManager.In(1f, () =>
		{
			UIToast.Show(Localization.Get("Training completed"), 3f);
			PlayerPrefs.SetInt("Tutorial", 1);
			TimerManager.In(5f, () =>
			{
				PhotonNetwork.LeaveRoom();
			});
		});
	}

	private void UpdateLanguage()
	{
		if (!PlayerPrefs.HasKey("Language"))
		{
			if (Application.systemLanguage == SystemLanguage.Russian || Application.systemLanguage == SystemLanguage.Ukrainian || Application.systemLanguage == SystemLanguage.Belarusian)
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
	}

	private void SetLanguage(string language)
	{
		for (int i = 0; i < Localization.knownLanguages.Length; i++)
		{
			if (Localization.knownLanguages[i] == language)
			{
				Localization.language = Localization.knownLanguages[i];
				break;
			}
		}
	}
}
