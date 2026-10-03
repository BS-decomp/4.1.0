using System;
using Lean;
using UnityEngine;

public class SimpleDrag : MonoBehaviour
{
	public LayerMask LayerMask = -5;

	private LeanFinger draggingFinger;

	protected virtual void OnEnable()
	{
		LeanTouch.OnFingerDown = (Action<LeanFinger>)Delegate.Combine(LeanTouch.OnFingerDown, new Action<LeanFinger>(OnFingerDown));
		LeanTouch.OnFingerUp = (Action<LeanFinger>)Delegate.Combine(LeanTouch.OnFingerUp, new Action<LeanFinger>(OnFingerUp));
	}

	protected virtual void OnDisable()
	{
		LeanTouch.OnFingerDown = (Action<LeanFinger>)Delegate.Remove(LeanTouch.OnFingerDown, new Action<LeanFinger>(OnFingerDown));
		LeanTouch.OnFingerUp = (Action<LeanFinger>)Delegate.Remove(LeanTouch.OnFingerUp, new Action<LeanFinger>(OnFingerUp));
	}

	protected virtual void LateUpdate()
	{
		if (draggingFinger != null)
		{
			LeanTouch.MoveObject(base.transform, draggingFinger.DeltaScreenPosition);
		}
	}

	public void OnFingerDown(LeanFinger finger)
	{
		Ray ray = finger.GetRay();
		RaycastHit hitInfo = default(RaycastHit);
		if (Physics.Raycast(ray, out hitInfo, float.PositiveInfinity, LayerMask) && hitInfo.collider.gameObject == base.gameObject)
		{
			draggingFinger = finger;
		}
	}

	public void OnFingerUp(LeanFinger finger)
	{
		if (finger == draggingFinger)
		{
			draggingFinger = null;
		}
	}
}
