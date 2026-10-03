using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
	private static InputManager instance;

	public static event Action<string> GetButtonDownEvent;

	public static event Action<string> GetButtonEvent;

	public static event Action<string> GetButtonUpEvent;

	public static event Action<string, float> GetAxisEvent;

	private void Start()
	{
		instance = this;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
	}

	public static void Init()
	{
		if (instance == null)
		{
			GameObject gameObject = new GameObject("InputManager");
			gameObject.AddComponent<InputManager>();
		}
	}

	public static void SetButtonDown(string name)
	{
		if (GetButtonDownEvent != null)
		{
			GetButtonDownEvent(name);
		}
	}

	public static void SetButtonUp(string name)
	{
		if (GetButtonUpEvent != null)
		{
			GetButtonUpEvent(name);
		}
	}

	public static void SetAxis(string name, float value)
	{
		if (GetAxisEvent != null)
		{
			GetAxisEvent(name, value);
		}
	}
}
