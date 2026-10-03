using UnityEngine;

public class InputTouchLook : MonoBehaviour
{
	private Rect touchZone = new Rect(50f, 0f, 50f, 100f);

	private bool move;

	private int id = -1;

	private float dpi;

	private Vector2 value;

	private bool Lefty;

	private void Start()
	{
		dpi = Screen.dpi / 100f;
		if (dpi == 0f)
		{
			dpi = 1.6f;
		}
		EventManager.AddListener("UpdateSettings", UpdateSettings);
		UpdateSettings();
	}

	private void OnDisable()
	{
		UpdateValue(Vector2.zero);
		id = -1;
		move = false;
	}

	private void Update()
	{
		for (int i = 0; i < Input.touchCount; i++)
		{
			Touch touch = Input.GetTouch(i);
			if (touch.phase == TouchPhase.Began && touchZone.Contains(new Vector3(touch.position.x, (float)Screen.height - touch.position.y, 0f)))
			{
				move = true;
				id = touch.fingerId;
			}
			if ((touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) && id == touch.fingerId && move)
			{
				Vector2 zero = Vector2.zero;
				zero = touch.deltaPosition / dpi;
				UpdateValue(zero);
			}
			if ((touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended) && id == touch.fingerId && move)
			{
				id = -1;
				move = false;
				UpdateValue(Vector2.zero);
			}
		}
	}

	private void UpdateValue(Vector2 v)
	{
		value = v;
		InputManager.SetAxis("Mouse X", value.x);
		InputManager.SetAxis("Mouse Y", value.y);
	}

	private void UpdateSettings()
	{
		Lefty = Settings.Lefty;
		if (Lefty)
		{
			touchZone = NewRect(new Rect(0f, 0f, 50f, 100f));
		}
		else
		{
			touchZone = NewRect(new Rect(50f, 0f, 50f, 100f));
		}
	}

	private Rect NewRect(Rect rect)
	{
		float left = (float)Screen.width * rect.x / 100f;
		float top = (float)Screen.height * rect.y / 100f;
		float width = (float)Screen.width * rect.width / 100f;
		float height = (float)Screen.height * rect.height / 100f;
		return new Rect(left, top, width, height);
	}
}
