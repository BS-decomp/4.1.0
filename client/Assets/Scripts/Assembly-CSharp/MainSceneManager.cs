using UnityEngine;
using UnityEngine.SceneManagement;

public class MainSceneManager : MonoBehaviour
{
	private GameObject nullGameObject;

	private static bool isTrackLocationEnabled = true;

	private PopUp popupWindow = new PopUp();

	private static int counter = 1;

	private void initGUI()
	{
		GUI.skin.button.fontSize = 40;
		GUI.skin.textField.fontSize = 35;
		GUI.contentColor = Color.white;
		GUI.skin.label.fontSize = 40;
	}

	private void OnGUI()
	{
		initGUI();
		popupWindow.onGUI();
		IYandexAppMetrica instance = AppMetrica.Instance;
		if (Button("Report Test"))
		{
			string text = "Test" + counter++;
			instance.ReportEvent(text);
			popupWindow.showPopup("Report: " + text);
		}
		if (Button("Track Location Enabled: " + isTrackLocationEnabled))
		{
			isTrackLocationEnabled = !isTrackLocationEnabled;
			instance.SetTrackLocationEnabled(isTrackLocationEnabled);
		}
		if (Button("[CRASH] NullReference"))
		{
			nullGameObject.SendMessage(string.Empty);
		}
		if (Button("LOG Library Version"))
		{
			popupWindow.showPopup("Version: " + instance.LibraryVersion);
		}
		if (Button("LOG Library API Level"))
		{
			popupWindow.showPopup("Level: " + instance.LibraryApiLevel);
		}
		if (Button("[SCENE] Load"))
		{
			SceneManager.LoadScene("AnotherScene");
		}
		if (Button("Exit"))
		{
			Application.Quit();
		}
	}

	private bool Button(string title)
	{
		return GUILayout.Button(title, GUILayout.Width(Screen.width), GUILayout.Height(Screen.height / 10));
	}
}
