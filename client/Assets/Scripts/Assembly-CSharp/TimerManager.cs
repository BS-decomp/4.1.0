using System.Collections.Generic;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
	private class Event
	{
		public int ID;

		public Callback Function;

		public int Iterations = 1;

		public float Interval = -1f;

		public float StartTime;

		public float DueTime;

		public bool CancelOnLoad = true;

		public void Invoke()
		{
			if (ID == 0 || DueTime == 0f)
			{
				Recycle();
				return;
			}
			try
			{
				if (Function != null)
				{
					Function();
				}
			}
			catch
			{
				Recycle();
				return;
			}
			if (Iterations > 0)
			{
				Iterations--;
				if (Iterations < 1)
				{
					Recycle();
					return;
				}
			}
			DueTime = Time.time + Interval;
		}

		public void Recycle()
		{
			ID = 0;
			Function = null;
			DueTime = 0f;
			CancelOnLoad = true;
			if (instance.Active.Remove(this))
			{
				instance.Pool.Add(this);
			}
		}
	}

	public delegate void Callback();

	private List<Event> Active = new List<Event>();

	private List<Event> Pool = new List<Event>();

	private int EventCount;

	private int EventIterator;

	private int EventBatch;

	private int MaxEventsPerFrame = 10;

	private static TimerManager instance;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
			Object.DontDestroyOnLoad(base.gameObject);
		}
	}

	private static void Init()
	{
		if (instance == null)
		{
			GameObject gameObject = new GameObject("TimerManager");
			gameObject.AddComponent<TimerManager>();
		}
	}

	private void Update()
	{
		EventBatch = 0;
		while (Active.Count > 0 && EventBatch < MaxEventsPerFrame)
		{
			if (EventIterator < 0)
			{
				EventIterator = Active.Count - 1;
				break;
			}
			if (EventIterator > Active.Count - 1)
			{
				EventIterator = Active.Count - 1;
			}
			if (Time.time >= Active[EventIterator].DueTime || Active[EventIterator].ID == 0)
			{
				Active[EventIterator].Invoke();
			}
			EventIterator--;
			EventBatch++;
		}
	}

	public static int Start()
	{
		return Start(true);
	}

	public static int Start(bool cancelOnLoad)
	{
		return In(315360000f, cancelOnLoad, null);
	}

	public static float GetDuration(int id)
	{
		for (int i = 0; i < instance.Active.Count; i++)
		{
			if (instance.Active[i].ID == id)
			{
				return Time.time - instance.Active[i].StartTime;
			}
		}
		return 0f;
	}

	public static int In(float delay, Callback callback)
	{
		return In(delay, true, 1, 0f, callback);
	}

	public static int In(float delay, bool cancelOnLoad, Callback callback)
	{
		return In(delay, cancelOnLoad, 1, 0f, callback);
	}

	public static int In(float delay, int iterations, float interval, Callback callback)
	{
		return In(delay, true, iterations, interval, callback);
	}

	public static int In(float delay, bool cancelOnLoad, int iterations, float interval, Callback callback)
	{
		Init();
		delay = Mathf.Max(0f, delay);
		iterations = Mathf.Max(0, iterations);
		interval = Mathf.Max(0f, interval);
		Event obj = null;
		if (instance.Pool.Count > 0)
		{
			obj = instance.Pool[0];
			instance.Pool.Remove(obj);
		}
		else
		{
			obj = new Event();
		}
		instance.EventCount++;
		obj.ID = instance.EventCount;
		obj.Function = callback;
		obj.Iterations = iterations;
		obj.Interval = interval;
		obj.StartTime = Time.time;
		obj.DueTime = Time.time + delay;
		obj.CancelOnLoad = cancelOnLoad;
		instance.Active.Add(obj);
		return obj.ID;
	}

	public static void Cancel(int id)
	{
		if (0 >= id)
		{
			return;
		}
		for (int num = instance.Active.Count - 1; num > -1; num--)
		{
			if (instance.Active[num].ID == id)
			{
				instance.Active[num].ID = 0;
				break;
			}
		}
	}

	public static void Cancel(int[] ids)
	{
		if (ids == null || ids.Length <= 0)
		{
			return;
		}
		for (int num = instance.Active.Count - 1; num > -1; num--)
		{
			if (ids.Contains(instance.Active[num].ID))
			{
				instance.Active[num].ID = 0;
			}
		}
	}

	public static void CancelAll()
	{
		for (int num = instance.Active.Count - 1; num > -1; num--)
		{
			instance.Active[num].ID = 0;
		}
	}

	private void OnLevelWasLoaded(int level)
	{
		for (int num = Active.Count - 1; num > -1; num--)
		{
			if (Active[num].CancelOnLoad)
			{
				Active[num].ID = 0;
			}
		}
	}

	public static bool IsActive(int id)
	{
		for (int num = instance.Active.Count - 1; num > -1; num--)
		{
			if (instance.Active[num].ID == id)
			{
				return true;
			}
		}
		return false;
	}
}
