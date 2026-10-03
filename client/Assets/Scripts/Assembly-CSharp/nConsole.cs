using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class nConsole : MonoBehaviour
{
	private KeyCode key = KeyCode.Escape;

	private GUIStyle style = new GUIStyle();

	private GUIStyle styleButton = new GUIStyle();

	private Texture2D background;

	private Vector2 scroll;

	private List<string> list = new List<string>();

	private bool showGUI;

	private float dpi;

	private static nConsole instance;

	private void Awake()
	{
		instance = this;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
	}

	private void Start()
	{
		background = CreateTexture2D(new Color32(0, 0, 0, 230));
		style.normal.textColor = Color.white;
		style.fontSize = (int)(13f * (float)Screen.width / 800f);
		style.padding.left = 4;
		styleButton.normal.textColor = Color.white;
		styleButton.normal.background = background;
		styleButton.fontSize = (int)(13f * (float)Screen.width / 800f);
		styleButton.padding.left = 4;
		styleButton.alignment = TextAnchor.MiddleCenter;
		dpi = Screen.dpi / 100f;
		if (dpi == 0f)
		{
			dpi = 1.6f;
		}
	}

	public static void Init()
	{
		if (!(instance != null))
		{
			GameObject gameObject = new GameObject("nConsole");
			gameObject.AddComponent<nConsole>();
		}
	}

	public static void Destroy()
	{
		if (instance != null)
		{
			UnityEngine.Object.Destroy(instance.gameObject);
		}
	}

	private void OnEnable()
	{
		ConsoleManager.LogCallback = (Action<string, string, LogType>)Delegate.Combine(ConsoleManager.LogCallback, new Action<string, string, LogType>(HandleLog));
	}

	private void OnDisable()
	{
		ConsoleManager.LogCallback = (Action<string, string, LogType>)Delegate.Remove(ConsoleManager.LogCallback, new Action<string, string, LogType>(HandleLog));
	}

	private void Update()
	{
		if (Input.GetKeyDown(key))
		{
			showGUI = !showGUI;
		}
		if (Input.touchCount == 1)
		{
			Touch touch = Input.touches[0];
			if (touch.phase == TouchPhase.Moved)
			{
				scroll += touch.deltaPosition / dpi * 5f;
			}
		}
	}

	private void OnGUI()
	{
		if (showGUI)
		{
			GUI.DrawTextureWithTexCoords(xRect(2f, 2f, 96f, 88f), background, new Rect(0f, 0f, 1f, 1f), true);
			GUILayout.BeginArea(xRect(2f, 2f, 96f, 88f));
			scroll = GUILayout.BeginScrollView(scroll);
			GUILayout.Space(2f);
			for (int i = 0; i < list.Count; i++)
			{
				GUILayout.Label(list[i], style);
			}
			GUILayout.EndScrollView();
			GUILayout.EndArea();
			if (GUI.Button(xRect(2f, 91f, 96f, 8f), "Clear", styleButton))
			{
				list.Clear();
			}
		}
	}

	private void HandleLog(string message, string stackTrace, LogType type)
	{
		StringBuilder stringBuilder = new StringBuilder();
		switch (type)
		{
		case LogType.Assert:
			stringBuilder.AppendLine("<color=red>Assert:</color> " + message);
			break;
		case LogType.Exception:
			stringBuilder.AppendLine("<color=red>Exception:</color> " + message);
			break;
		case LogType.Error:
			stringBuilder.AppendLine("<color=red>Error:</color> " + message);
			break;
		case LogType.Warning:
			stringBuilder.AppendLine("<color=yellow>Warning:</color> " + message);
			break;
		case LogType.Log:
			stringBuilder.AppendLine("<color=grey>Log:</color> " + message);
			break;
		}
		stringBuilder.Append("<color=grey>" + stackTrace + "</color>");
		list.Add(stringBuilder.ToString());
	}

	private Rect xRect(Rect rect)
	{
		return xRect(rect.x, rect.y, rect.width, rect.height);
	}

	private Rect xRect(float x, float y, float width, float height)
	{
		x = (float)Screen.width * x / 100f;
		y = (float)Screen.height * y / 100f;
		width = (float)Screen.width * width / 100f;
		height = (float)Screen.height * height / 100f;
		return new Rect(x, y, width, height);
	}

	private Texture2D CreateTexture2D(Color color)
	{
		Texture2D texture2D = new Texture2D(2, 2);
		for (int i = 0; i < texture2D.width; i++)
		{
			for (int j = 0; j < texture2D.height; j++)
			{
				texture2D.SetPixel(i, j, color);
			}
		}
		texture2D.Apply();
		return texture2D;
	}
}
