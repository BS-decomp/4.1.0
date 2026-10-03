using UnityEngine;
using UnityEngine.Events;

public class UIEventClick : MonoBehaviour
{
	public UnityEvent onClick;

	private void OnClick()
	{
		if (onClick != null)
		{
			onClick.Invoke();
		}
	}
}
