using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class vp_StateEventHandler : vp_EventHandler
{
	private List<vp_Component> m_StateTargets = new List<vp_Component>();

	protected override void Awake()
	{
		base.Awake();
		vp_Component[] components = base.transform.root.GetComponents<vp_Component>();
		foreach (vp_Component item in components)
		{
			m_StateTargets.Add(item);
		}
	}

	protected void BindStateToActivity(vp_Activity a)
	{
		BindStateToActivityOnStart(a);
		BindStateToActivityOnStop(a);
	}

	protected void BindStateToActivityOnStart(vp_Activity a)
	{
		if (!ActivityInitialized(a))
		{
			return;
		}
		string s = a.EventName;
		a.StartCallbacks = (vp_Activity.Callback)Delegate.Combine(a.StartCallbacks, (vp_Activity.Callback)(() =>
		{
			foreach (vp_Component stateTarget in m_StateTargets)
			{
				stateTarget.SetState(s, true, true);
			}
		}));
	}

	protected void BindStateToActivityOnStop(vp_Activity a)
	{
		if (!ActivityInitialized(a))
		{
			return;
		}
		string s = a.EventName;
		a.StopCallbacks = (vp_Activity.Callback)Delegate.Combine(a.StopCallbacks, (vp_Activity.Callback)(() =>
		{
			foreach (vp_Component stateTarget in m_StateTargets)
			{
				stateTarget.SetState(s, false, true);
			}
		}));
	}

	public void RefreshActivityStates()
	{
		foreach (vp_Event value in m_HandlerEvents.Values)
		{
			if (!(value is vp_Activity) && value.GetType().BaseType != typeof(vp_Activity))
			{
				continue;
			}
			foreach (vp_Component stateTarget in m_StateTargets)
			{
				stateTarget.SetState(value.EventName, ((vp_Activity)value).Active, true);
			}
		}
	}

	public void ResetActivityStates()
	{
		foreach (vp_Component stateTarget in m_StateTargets)
		{
			stateTarget.ResetState();
		}
	}

	public void SetState(string state, bool setActive = true, bool recursive = true, bool includeDisabled = false)
	{
		foreach (vp_Component stateTarget in m_StateTargets)
		{
			stateTarget.SetState(state, setActive, recursive, includeDisabled);
		}
	}

	private bool ActivityInitialized(vp_Activity a)
	{
		if (a == null)
		{
			Debug.LogError(string.Concat("Error: (", this, ") Activity is null."));
			return false;
		}
		if (string.IsNullOrEmpty(a.EventName))
		{
			Debug.LogError(string.Concat("Error: (", this, ") Activity not initialized. Make sure the event handler has run its Awake call before binding layers."));
			return false;
		}
		return true;
	}
}
