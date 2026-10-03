using System;
using Lean;
using UnityEngine;

public class SimpleInfo : MonoBehaviour
{
	protected virtual void OnEnable()
	{
		LeanTouch.OnFingerDown = (Action<LeanFinger>)Delegate.Combine(LeanTouch.OnFingerDown, new Action<LeanFinger>(OnFingerDown));
		LeanTouch.OnFingerSet = (Action<LeanFinger>)Delegate.Combine(LeanTouch.OnFingerSet, new Action<LeanFinger>(OnFingerSet));
		LeanTouch.OnFingerUp = (Action<LeanFinger>)Delegate.Combine(LeanTouch.OnFingerUp, new Action<LeanFinger>(OnFingerUp));
		LeanTouch.OnFingerDrag = (Action<LeanFinger>)Delegate.Combine(LeanTouch.OnFingerDrag, new Action<LeanFinger>(OnFingerDrag));
		LeanTouch.OnFingerTap = (Action<LeanFinger>)Delegate.Combine(LeanTouch.OnFingerTap, new Action<LeanFinger>(OnFingerTap));
		LeanTouch.OnFingerSwipe = (Action<LeanFinger>)Delegate.Combine(LeanTouch.OnFingerSwipe, new Action<LeanFinger>(OnFingerSwipe));
		LeanTouch.OnFingerHeldDown = (Action<LeanFinger>)Delegate.Combine(LeanTouch.OnFingerHeldDown, new Action<LeanFinger>(OnFingerHeldDown));
		LeanTouch.OnFingerHeldSet = (Action<LeanFinger>)Delegate.Combine(LeanTouch.OnFingerHeldSet, new Action<LeanFinger>(OnFingerHeld));
		LeanTouch.OnFingerHeldUp = (Action<LeanFinger>)Delegate.Combine(LeanTouch.OnFingerHeldUp, new Action<LeanFinger>(OnFingerHeldUp));
		LeanTouch.OnMultiTap = (Action<int>)Delegate.Combine(LeanTouch.OnMultiTap, new Action<int>(OnMultiTap));
		LeanTouch.OnDrag = (Action<Vector2>)Delegate.Combine(LeanTouch.OnDrag, new Action<Vector2>(OnDrag));
		LeanTouch.OnSoloDrag = (Action<Vector2>)Delegate.Combine(LeanTouch.OnSoloDrag, new Action<Vector2>(OnSoloDrag));
		LeanTouch.OnMultiDrag = (Action<Vector2>)Delegate.Combine(LeanTouch.OnMultiDrag, new Action<Vector2>(OnMultiDrag));
		LeanTouch.OnPinch = (Action<float>)Delegate.Combine(LeanTouch.OnPinch, new Action<float>(OnPinch));
		LeanTouch.OnTwistDegrees = (Action<float>)Delegate.Combine(LeanTouch.OnTwistDegrees, new Action<float>(OnTwistDegrees));
		LeanTouch.OnTwistRadians = (Action<float>)Delegate.Combine(LeanTouch.OnTwistRadians, new Action<float>(OnTwistRadians));
	}

	protected virtual void OnDisable()
	{
		LeanTouch.OnFingerDown = (Action<LeanFinger>)Delegate.Remove(LeanTouch.OnFingerDown, new Action<LeanFinger>(OnFingerDown));
		LeanTouch.OnFingerSet = (Action<LeanFinger>)Delegate.Remove(LeanTouch.OnFingerSet, new Action<LeanFinger>(OnFingerSet));
		LeanTouch.OnFingerUp = (Action<LeanFinger>)Delegate.Remove(LeanTouch.OnFingerUp, new Action<LeanFinger>(OnFingerUp));
		LeanTouch.OnFingerDrag = (Action<LeanFinger>)Delegate.Remove(LeanTouch.OnFingerDrag, new Action<LeanFinger>(OnFingerDrag));
		LeanTouch.OnFingerTap = (Action<LeanFinger>)Delegate.Remove(LeanTouch.OnFingerTap, new Action<LeanFinger>(OnFingerTap));
		LeanTouch.OnFingerSwipe = (Action<LeanFinger>)Delegate.Remove(LeanTouch.OnFingerSwipe, new Action<LeanFinger>(OnFingerSwipe));
		LeanTouch.OnFingerHeldDown = (Action<LeanFinger>)Delegate.Remove(LeanTouch.OnFingerHeldDown, new Action<LeanFinger>(OnFingerHeldDown));
		LeanTouch.OnFingerHeldSet = (Action<LeanFinger>)Delegate.Remove(LeanTouch.OnFingerHeldSet, new Action<LeanFinger>(OnFingerHeld));
		LeanTouch.OnFingerHeldUp = (Action<LeanFinger>)Delegate.Remove(LeanTouch.OnFingerHeldUp, new Action<LeanFinger>(OnFingerHeldUp));
		LeanTouch.OnMultiTap = (Action<int>)Delegate.Remove(LeanTouch.OnMultiTap, new Action<int>(OnMultiTap));
		LeanTouch.OnDrag = (Action<Vector2>)Delegate.Remove(LeanTouch.OnDrag, new Action<Vector2>(OnDrag));
		LeanTouch.OnSoloDrag = (Action<Vector2>)Delegate.Remove(LeanTouch.OnSoloDrag, new Action<Vector2>(OnSoloDrag));
		LeanTouch.OnMultiDrag = (Action<Vector2>)Delegate.Remove(LeanTouch.OnMultiDrag, new Action<Vector2>(OnMultiDrag));
		LeanTouch.OnPinch = (Action<float>)Delegate.Remove(LeanTouch.OnPinch, new Action<float>(OnPinch));
		LeanTouch.OnTwistDegrees = (Action<float>)Delegate.Remove(LeanTouch.OnTwistDegrees, new Action<float>(OnTwistDegrees));
		LeanTouch.OnTwistRadians = (Action<float>)Delegate.Remove(LeanTouch.OnTwistRadians, new Action<float>(OnTwistRadians));
	}

	public void OnFingerDown(LeanFinger finger)
	{
		Debug.Log("Finger " + finger.Index + " began touching the screen");
	}

	public void OnFingerSet(LeanFinger finger)
	{
		Debug.Log("Finger " + finger.Index + " is still touching the screen");
	}

	public void OnFingerUp(LeanFinger finger)
	{
		Debug.Log("Finger " + finger.Index + " finished touching the screen");
	}

	public void OnFingerDrag(LeanFinger finger)
	{
		Debug.Log(string.Concat("Finger ", finger.Index, " moved ", finger.DeltaScreenPosition, " pixels across the screen"));
	}

	public void OnFingerTap(LeanFinger finger)
	{
		Debug.Log("Finger " + finger.Index + " tapped the screen");
	}

	public void OnFingerSwipe(LeanFinger finger)
	{
		Debug.Log("Finger " + finger.Index + " swiped the screen");
	}

	public void OnFingerHeldDown(LeanFinger finger)
	{
		Debug.Log("Finger " + finger.Index + " began touching the screen for a long time");
	}

	public void OnFingerHeld(LeanFinger finger)
	{
		Debug.Log("Finger " + finger.Index + " is still touching the screen for a long time");
	}

	public void OnFingerHeldUp(LeanFinger finger)
	{
		Debug.Log("Finger " + finger.Index + " stopped touching the screen for a long time");
	}

	public void OnMultiTap(int fingerCount)
	{
		Debug.Log("The screen was just tapped by " + fingerCount + " finger(s)");
	}

	public void OnDrag(Vector2 pixels)
	{
		Debug.Log(string.Concat("One or many fingers moved ", pixels, " across the screen"));
	}

	public void OnSoloDrag(Vector2 pixels)
	{
		Debug.Log(string.Concat("One finger moved ", pixels, " across the screen"));
	}

	public void OnMultiDrag(Vector2 pixels)
	{
		Debug.Log(string.Concat("Many fingers moved ", pixels, " across the screen"));
	}

	public void OnPinch(float scale)
	{
		Debug.Log("Many fingers pinched " + scale + " percent");
	}

	public void OnTwistDegrees(float angle)
	{
		Debug.Log("Many fingers twisted " + angle + " degrees");
	}

	public void OnTwistRadians(float angle)
	{
		Debug.Log("Many fingers twisted " + angle + " radians");
	}
}
