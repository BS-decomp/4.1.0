using System;
using Lean;
using UnityEngine;
using UnityEngine.UI;

public class SimpleSwipeDirection : MonoBehaviour
{
	public Text InfoText;

	protected virtual void OnEnable()
	{
		LeanTouch.OnFingerSwipe = (Action<LeanFinger>)Delegate.Combine(LeanTouch.OnFingerSwipe, new Action<LeanFinger>(OnFingerSwipe));
	}

	protected virtual void OnDisable()
	{
		LeanTouch.OnFingerSwipe = (Action<LeanFinger>)Delegate.Remove(LeanTouch.OnFingerSwipe, new Action<LeanFinger>(OnFingerSwipe));
	}

	public void OnFingerSwipe(LeanFinger finger)
	{
		if (InfoText != null)
		{
			Vector2 swipeDelta = finger.SwipeDelta;
			if (swipeDelta.x < 0f - Mathf.Abs(swipeDelta.y))
			{
				InfoText.text = "You swiped left!";
			}
			if (swipeDelta.x > Mathf.Abs(swipeDelta.y))
			{
				InfoText.text = "You swiped right!";
			}
			if (swipeDelta.y < 0f - Mathf.Abs(swipeDelta.x))
			{
				InfoText.text = "You swiped down!";
			}
			if (swipeDelta.y > Mathf.Abs(swipeDelta.x))
			{
				InfoText.text = "You swiped up!";
			}
		}
	}
}
