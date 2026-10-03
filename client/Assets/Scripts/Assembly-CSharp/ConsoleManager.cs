using System;
using UnityEngine;

public class ConsoleManager : MonoBehaviour
{
	public static Action<string, string, LogType> LogCallback;

	private void OnEnable()
	{
		Application.RegisterLogCallback(HandleLog);
	}

	private void OnDisable()
	{
		Application.RegisterLogCallback(null);
	}

	private void HandleLog(string message, string stackTrace, LogType type)
	{
		if (LogCallback != null)
		{
			LogCallback(message, stackTrace, type);
		}
	}
}
