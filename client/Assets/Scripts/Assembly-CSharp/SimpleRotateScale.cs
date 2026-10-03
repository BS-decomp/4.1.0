using Lean;
using UnityEngine;

public class SimpleRotateScale : MonoBehaviour
{
	protected virtual void LateUpdate()
	{
		LeanTouch.RotateObject(base.transform, LeanTouch.TwistDegrees);
		LeanTouch.ScaleObject(base.transform, LeanTouch.PinchScale);
	}
}
