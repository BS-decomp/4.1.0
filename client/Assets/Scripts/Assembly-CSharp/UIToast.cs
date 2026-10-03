using System.Collections.Generic;
using UnityEngine;

public class UIToast : MonoBehaviour
{
	public UILabel label;

	public UISprite background;

	private int Timer;

	private List<string> QueueText;

	private static UIToast instance;

	private void Awake()
	{
		instance = this;
	}

	public static void Show(string text)
	{
		Show(text, 2f);
	}

	public static void Show(string text, float duration)
	{
		TimerManager.Cancel(instance.Timer);
		instance.label.alpha = 0f;
		TweenAlpha.Begin(instance.label.cachedGameObject, 0.2f, 1f);
		instance.label.text = text;
		instance.background.UpdateAnchors();
		instance.Timer = TimerManager.In(duration, () =>
		{
			if (instance.QueueText != null && instance.QueueText.Count != 0)
			{
				Show(instance.QueueText[0]);
				instance.QueueText.RemoveAt(0);
			}
			else
			{
				TweenAlpha.Begin(instance.label.cachedGameObject, 0.2f, 0f);
			}
		});
	}

	public static void AddQueue(string text)
	{
		if (instance.QueueText == null)
		{
			instance.QueueText = new List<string>();
		}
		instance.QueueText.Add(text);
	}
}
