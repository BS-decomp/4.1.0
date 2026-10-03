using Lean;
using UnityEngine;

public class SimpleMove : MonoBehaviour
{
	private void LateUpdate()
	{
		LeanTouch.MoveObject(base.transform, LeanTouch.DragDelta);
	}
}
