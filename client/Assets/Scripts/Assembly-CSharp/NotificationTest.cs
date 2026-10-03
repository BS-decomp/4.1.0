using UnityEngine;

public class NotificationTest : MonoBehaviour
{
	private float sleepUntil;

	private void OnGUI()
	{
		GUI.enabled = sleepUntil < Time.time;
		if (GUILayout.Button("5 SECONDS", GUILayout.Height((float)Screen.height * 0.2f)))
		{
			LocalNotification.SendNotification(1, 5L, "Title", "Long message text", new Color32(byte.MaxValue, 68, 68, byte.MaxValue), true, true, true, string.Empty);
			sleepUntil = Time.time + 5f;
		}
		if (GUILayout.Button("5 SECONDS BIG ICON", GUILayout.Height((float)Screen.height * 0.2f)))
		{
			LocalNotification.SendNotification(1, 5L, "Title", "Long message text with big icon", new Color32(byte.MaxValue, 68, 68, byte.MaxValue), true, true, true, "app_icon");
			sleepUntil = Time.time + 5f;
		}
		if (GUILayout.Button("EVERY 5 SECONDS", GUILayout.Height((float)Screen.height * 0.2f)))
		{
			LocalNotification.SendRepeatingNotification(1, 5L, 5L, "Title", "Long message text", new Color32(byte.MaxValue, 68, 68, byte.MaxValue), true, true, true, string.Empty);
			sleepUntil = Time.time + 99999f;
		}
		if (GUILayout.Button("10 SECONDS EXACT", GUILayout.Height((float)Screen.height * 0.2f)))
		{
			LocalNotification.SendNotification(1, 10L, "Title", "Long exact message text", new Color32(byte.MaxValue, 68, 68, byte.MaxValue), true, true, true, string.Empty, LocalNotification.NotificationExecuteMode.ExactAndAllowWhileIdle);
			sleepUntil = Time.time + 10f;
		}
		GUI.enabled = true;
		if (GUILayout.Button("STOP", GUILayout.Height((float)Screen.height * 0.2f)))
		{
			LocalNotification.CancelNotification(1);
			sleepUntil = 0f;
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Application.Quit();
		}
	}
}
