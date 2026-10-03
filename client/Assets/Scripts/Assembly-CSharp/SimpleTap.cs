using System;
using Lean;
using UnityEngine;

public class SimpleTap : MonoBehaviour
{
	public GameObject Prefab;

	protected virtual void OnEnable()
	{
		LeanTouch.OnFingerTap = (Action<LeanFinger>)Delegate.Combine(LeanTouch.OnFingerTap, new Action<LeanFinger>(OnFingerTap));
	}

	protected virtual void OnDisable()
	{
		LeanTouch.OnFingerTap = (Action<LeanFinger>)Delegate.Remove(LeanTouch.OnFingerTap, new Action<LeanFinger>(OnFingerTap));
	}

	public void OnFingerTap(LeanFinger finger)
	{
		if (Prefab != null && !finger.IsOverGui)
		{
			Vector3 worldPosition = finger.GetWorldPosition(50f);
			Quaternion identity = Quaternion.identity;
			GameObject obj = (GameObject)UnityEngine.Object.Instantiate(Prefab, worldPosition, identity);
			UnityEngine.Object.Destroy(obj, 2f);
		}
	}
}
