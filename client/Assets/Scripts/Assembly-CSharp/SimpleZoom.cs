using Lean;
using UnityEngine;

public class SimpleZoom : MonoBehaviour
{
	public float MinFov = 10f;

	public float MaxFov = 60f;

	protected virtual void LateUpdate()
	{
		if (Camera.main != null && LeanTouch.PinchScale > 0f)
		{
			Camera.main.fieldOfView /= LeanTouch.PinchScale;
			Camera.main.fieldOfView = Mathf.Clamp(Camera.main.fieldOfView, MinFov, MaxFov);
		}
	}
}
