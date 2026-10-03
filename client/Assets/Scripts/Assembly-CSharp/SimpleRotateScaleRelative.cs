using Lean;
using UnityEngine;

public class SimpleRotateScaleRelative : MonoBehaviour
{
	protected virtual void LateUpdate()
	{
		Vector2 centerOfFingers = LeanTouch.GetCenterOfFingers();
		LeanTouch.RotateObjectRelative(base.transform, LeanTouch.TwistDegrees, centerOfFingers);
		LeanTouch.ScaleObjectRelative(base.transform, LeanTouch.PinchScale, centerOfFingers);
	}
}
