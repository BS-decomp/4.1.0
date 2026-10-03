using Prime31;
using UnityEngine;

public class mGooglePlay : MonoBehaviour
{
	public UILabel SignInOutLabel;

	private void Start()
	{
		if (PlayGameServices.isSignedIn())
		{
			SignInOutLabel.text = Localization.Get("Sign out");
		}
		else
		{
			SignInOutLabel.text = Localization.Get("Sign in");
		}
	}

	private void OnEnable()
	{
		GPGManager.authenticationSucceededEvent += OnSignIn;
		GPGManager.authenticationFailedEvent += OnSignInError;
	}

	private void OnDisable()
	{
		GPGManager.authenticationSucceededEvent -= OnSignIn;
		GPGManager.authenticationFailedEvent -= OnSignInError;
	}

	private void OnSignIn(string playerID)
	{
		SignInOutLabel.text = Localization.Get("Sign out");
	}

	private void OnSignInError(string error)
	{
		SignInOutLabel.text = Localization.Get("Sign in");
		UIToast.Show(error, 3f);
	}

	private void OnSignOut()
	{
		SignInOutLabel.text = Localization.Get("Sign in");
	}

	public void OnSignInOut()
	{
		if (PlayGameServices.isSignedIn())
		{
			PlayGameServices.signOut();
			SignInOutLabel.text = Localization.Get("Sign in");
		}
		else
		{
			PlayGameServices.authenticate();
			SignInOutLabel.text = Localization.Get("Sign out");
		}
	}

	public void OnAchievement()
	{
		PlayGameServices.showAchievements();
	}
}
