using System;
using Lean;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimpleSwipe : MonoBehaviour
{
	public float ForceMultiplier = 1f;

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
		Rigidbody component = GetComponent<Rigidbody>();
		component.AddForce(finger.ScaledSwipeDelta * ForceMultiplier);
	}
}
