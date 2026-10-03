using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using YMMJSONUtils;

public class AnotherSceneManager : MonoBehaviour
{
	private const int DELAY_BACKGROUND_SEC = 120;

	private const string DEFAULT_EVENT = "test event";

	private const string DEFAULT_KEY = "key";

	private const string DEFAULT_VALUE = "value";

	private const string DEFAULT_ENVIRONMENT = "environment";

	private static string eventValue = "test event";

	private static string environmentValue = "environment";

	private Hashtable map = new Hashtable();

	private string key = "key";

	private string value = "value";

	private PopUp popupWindow = new PopUp();

	private void OnGUI()
	{
		popupWindow.onGUI();
		GUI.contentColor = Color.black;
		onCustomEventGUI();
		onParamsGUI();
		onEnvironmentGUI();
		if (Button("Back To Main Scene"))
		{
			SceneManager.LoadScene("MainScene");
		}
		GUILayout.Label(JSONEncoder.Encode(map));
	}

	private bool Button(string title)
	{
		return GUILayout.Button(title, GUILayout.Width(Screen.width), GUILayout.Height(Screen.height / 13));
	}

	private void onCustomEventGUI()
	{
		eventValue = GUILayout.TextField(eventValue);
		if (Button("Report Event"))
		{
			AppMetrica.Instance.ReportEvent(eventValue);
			popupWindow.showPopup("Report: " + eventValue);
			eventValue = "test event";
		}
	}

	private void onParamsGUI()
	{
		key = GUILayout.TextField(key);
		value = GUILayout.TextField(value);
		if (Button("Add param"))
		{
			if (map[key] != null)
			{
				map.Remove(key);
			}
			map.Add(key, value);
			key = "key";
			value = "value";
		}
		if (Button("Clear params"))
		{
			map.Clear();
		}
		if (Button("Report with params"))
		{
			AppMetrica.Instance.ReportEvent(eventValue, map);
			popupWindow.showPopup("Report with params");
			map.Clear();
		}
	}

	private void onEnvironmentGUI()
	{
		environmentValue = GUILayout.TextField(environmentValue);
		if (Button("Set crash environment"))
		{
			AppMetrica.Instance.SetEnvironmentValue("environment", environmentValue);
			environmentValue = "environment";
		}
	}
}
