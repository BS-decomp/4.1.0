using Lean;
using UnityEngine;

public class SimpleOrthographicZoom : MonoBehaviour
{
	public float MinSize = 10f;

	public float MaxSize = 60f;

	protected virtual void LateUpdate()
	{
		if (Camera.main != null && LeanTouch.PinchScale > 0f)
		{
			Camera.main.orthographicSize /= LeanTouch.PinchScale;
			Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, MinSize, MaxSize);
		}
	}
}
