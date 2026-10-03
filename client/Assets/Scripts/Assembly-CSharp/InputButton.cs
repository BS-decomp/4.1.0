using UnityEngine;

public class InputButton : MonoBehaviour
{
	public string button;

	public float fadeAlpha = 0.5f;

	public float fadeDuration = 0.1f;

	private bool mPressed;

	private GameObject mGameObject;

	private float alphaButton;

	private void Start()
	{
		mGameObject = base.gameObject;
		EventManager.AddListener("SaveButton", OnSetPosition);
		EventManager.AddListener("UpdateSettings", UpdateSettings);
		OnSetPosition();
		UpdateSettings();
	}

	private void OnEnable()
	{
		if (fadeAlpha != 0f && mGameObject != null)
		{
			TweenAlpha.Begin(mGameObject, fadeDuration, alphaButton);
		}
	}

	private void OnDisable()
	{
		if (mPressed)
		{
			InputManager.SetButtonUp(button);
		}
	}

	private void OnPress(bool pressed)
	{
		mPressed = pressed;
		if (pressed)
		{
			if (fadeAlpha != 0f)
			{
				TweenAlpha.Begin(mGameObject, fadeDuration, fadeAlpha * alphaButton);
			}
			InputManager.SetButtonDown(button);
		}
		else
		{
			if (fadeAlpha != 0f)
			{
				TweenAlpha.Begin(mGameObject, fadeDuration, alphaButton);
			}
			InputManager.SetButtonUp(button);
		}
	}

	private void OnSetPosition()
	{
		TimerManager.In(0.1f, () =>
		{
			if (PlayerPrefs.HasKey("ButtonX" + button))
			{
				UISprite component = mGameObject.GetComponent<UISprite>();
				component.cachedTransform.localPosition = Utils.GetVector3(PlayerPrefs.GetString("ButtonX" + button));
				int height = (component.width = PlayerPrefs.GetInt("ButtonSizeX" + button));
				component.height = height;
			}
		});
	}

	private void UpdateSettings()
	{
		alphaButton = Settings.ButtonAlpha;
		if (!Settings.HUD)
		{
			alphaButton = 1f;
		}
		mGameObject.GetComponent<UISprite>().alpha = alphaButton;
	}
}
