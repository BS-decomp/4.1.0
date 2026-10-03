using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

internal static class vp_TargetEventHandler
{
	private static List<Dictionary<object, Dictionary<string, Delegate>>> m_TargetDict;

	private static List<Dictionary<object, Dictionary<string, Delegate>>> TargetDict
	{
		get
		{
			if (m_TargetDict == null)
			{
				m_TargetDict = new List<Dictionary<object, Dictionary<string, Delegate>>>(100);
				for (int i = 0; i < 8; i++)
				{
					m_TargetDict.Add(new Dictionary<object, Dictionary<string, Delegate>>(100));
				}
			}
			return m_TargetDict;
		}
	}

	public static void Register(object target, string eventName, Delegate callback, int dictionary)
	{
		if (target == null)
		{
			Debug.LogWarning("Warning: (" + vp_Utility.GetErrorLocation(2) + " -> vp_TargetEvent.Register) Target object is null.");
			return;
		}
		if (string.IsNullOrEmpty(eventName))
		{
			Debug.LogWarning("Warning: (" + vp_Utility.GetErrorLocation(2) + " -> vp_TargetEvent.Register) Event name is null or empty.");
			return;
		}
		if ((object)callback == null)
		{
			Debug.LogWarning("Warning: (" + vp_Utility.GetErrorLocation(2) + " -> vp_TargetEvent.Register) Callback is null.");
			return;
		}
		if (callback.Method.Name.StartsWith("<"))
		{
			Debug.LogWarning("Warning: (" + vp_Utility.GetErrorLocation(2) + " -> vp_TargetEvent.Register) Target events can only be registered to declared methods.");
			return;
		}
		if (!TargetDict[dictionary].ContainsKey(target))
		{
			TargetDict[dictionary].Add(target, new Dictionary<string, Delegate>(100));
		}
		Dictionary<string, Delegate> value;
		TargetDict[dictionary].TryGetValue(target, out value);
		Delegate value2;
		while (true)
		{
			value.TryGetValue(eventName, out value2);
			if ((object)value2 == null)
			{
				value.Add(eventName, callback);
				return;
			}
			if (value2.GetType() == callback.GetType())
			{
				break;
			}
			eventName += "_";
		}
		callback = Delegate.Combine(value2, callback);
		if ((object)callback != null)
		{
			value.Remove(eventName);
			value.Add(eventName, callback);
		}
	}

	public static void Unregister(object target, string eventName = null, Delegate callback = null)
	{
		if ((eventName != null || (object)callback == null) && ((object)callback != null || eventName == null))
		{
			for (int i = 0; i < 8; i++)
			{
				Unregister(target, i, eventName, callback);
			}
		}
	}

	public static void Unregister(Component component)
	{
		if (!(component == null))
		{
			for (int i = 0; i < 8; i++)
			{
				Unregister(i, component);
			}
		}
	}

	private static void Unregister(int dictionary, Component component)
	{
		if (component == null)
		{
			return;
		}
		Dictionary<string, Delegate> value;
		if (TargetDict[dictionary].TryGetValue(component, out value))
		{
			TargetDict[dictionary].Remove(component);
		}
		object transform = component.transform;
		if (transform == null || !TargetDict[dictionary].TryGetValue(transform, out value))
		{
			return;
		}
		List<string> list = new List<string>(value.Keys);
		foreach (string item in list)
		{
			Delegate value2;
			if (item == null || !value.TryGetValue(item, out value2) || (object)value2 == null)
			{
				continue;
			}
			Delegate[] invocationList = value2.GetInvocationList();
			if (invocationList == null || invocationList.Length < 1)
			{
				continue;
			}
			for (int num = invocationList.Length - 1; num > -1; num--)
			{
				if (invocationList[num].Target == component)
				{
					value.Remove(item);
					Delegate obj = Delegate.Remove(value2, invocationList[num]);
					if (obj.GetInvocationList().Length > 0)
					{
						value.Add(item, obj);
					}
				}
			}
		}
	}

	private static void Unregister(object target, int dictionary, string eventName, Delegate callback)
	{
		Dictionary<string, Delegate> value;
		if (target == null || !TargetDict[dictionary].TryGetValue(target, out value) || value == null || value.Count == 0)
		{
			return;
		}
		if (eventName == null && (object)callback == null)
		{
			TargetDict[dictionary].Remove(value);
		}
		else
		{
			Delegate value2;
			if (!value.TryGetValue(eventName, out value2))
			{
				return;
			}
			if ((object)value2 != null)
			{
				value.Remove(eventName);
				value2 = Delegate.Remove(value2, callback);
				if ((object)value2 != null && value2.GetInvocationList() != null)
				{
					value.Add(eventName, value2);
				}
			}
			else
			{
				value.Remove(eventName);
			}
			if (value.Count <= 0)
			{
				TargetDict[dictionary].Remove(target);
			}
		}
	}

	public static void UnregisterAll()
	{
		m_TargetDict = null;
	}

	public static Delegate GetCallback(object target, string eventName, bool upwards, int d, vp_TargetEventOptions options)
	{
		if (target == null)
		{
			return null;
		}
		if (string.IsNullOrEmpty(eventName))
		{
			return null;
		}
		Delegate value;
		do
		{
			value = null;
			if ((options & vp_TargetEventOptions.IncludeInactive) == vp_TargetEventOptions.IncludeInactive)
			{
				goto IL_008c;
			}
			GameObject gameObject = target as GameObject;
			if (gameObject != null)
			{
				if (vp_Utility.IsActive(gameObject))
				{
					goto IL_008c;
				}
				if (!upwards)
				{
					return null;
				}
			}
			else
			{
				Behaviour behaviour = target as Behaviour;
				if (!(behaviour != null) || (behaviour.enabled && vp_Utility.IsActive(behaviour.gameObject)))
				{
					goto IL_008c;
				}
				if (!upwards)
				{
					return null;
				}
			}
			goto IL_00ce;
			IL_00ce:
			if ((object)value != null || !upwards)
			{
				break;
			}
			target = vp_Utility.GetParent(target as Component);
			continue;
			IL_008c:
			Dictionary<string, Delegate> value2 = null;
			if (!TargetDict[d].TryGetValue(target, out value2))
			{
				if (!upwards)
				{
					return null;
				}
			}
			else if (!value2.TryGetValue(eventName, out value) && !upwards)
			{
				return null;
			}
			goto IL_00ce;
		}
		while (target != null);
		return value;
	}

	public static void OnNoReceiver(string eventName, vp_TargetEventOptions options)
	{
		if ((options & vp_TargetEventOptions.RequireReceiver) == vp_TargetEventOptions.RequireReceiver)
		{
			Debug.LogError("Error: (" + vp_Utility.GetErrorLocation(2) + ") vp_TargetEvent '" + eventName + "' has no receiver!");
		}
	}

	public static string Dump()
	{
		Dictionary<object, string> dictionary = new Dictionary<object, string>();
		foreach (Dictionary<object, Dictionary<string, Delegate>> item in TargetDict)
		{
			foreach (object key in item.Keys)
			{
				string text = string.Empty;
				if (key == null)
				{
					continue;
				}
				Dictionary<string, Delegate> value;
				if (item.TryGetValue(key, out value))
				{
					foreach (string key2 in value.Keys)
					{
						text = text + "        \"" + key2 + "\" -> ";
						bool flag = false;
						Delegate value2;
						if (string.IsNullOrEmpty(key2) || !value.TryGetValue(key2, out value2))
						{
							continue;
						}
						if (value2.GetInvocationList().Length > 1)
						{
							flag = true;
							text += "\n";
						}
						Delegate[] invocationList = value2.GetInvocationList();
						foreach (Delegate obj in invocationList)
						{
							string text2 = text;
							text = string.Concat(text2, (!flag) ? string.Empty : "                        ", obj.Method.ReflectedType, ".cs -> ");
							string text3 = string.Empty;
							ParameterInfo[] parameters = obj.Method.GetParameters();
							foreach (ParameterInfo parameterInfo in parameters)
							{
								text2 = text3;
								text3 = text2 + vp_Utility.GetTypeAlias(parameterInfo.ParameterType) + " " + parameterInfo.Name + ", ";
							}
							if (text3.Length > 0)
							{
								text3 = text3.Remove(text3.LastIndexOf(", "));
							}
							text = text + vp_Utility.GetTypeAlias(obj.Method.ReturnType) + " ";
							if (obj.Method.Name.Contains("m_"))
							{
								string text4 = obj.Method.Name.TrimStart('<');
								text4 = text4.Remove(text4.IndexOf('>'));
								text = text + text4 + " -> delegate";
							}
							else
							{
								text += obj.Method.Name;
							}
							text = text + "(" + text3 + ")\n";
						}
					}
				}
				string value3;
				if (!dictionary.TryGetValue(key, out value3))
				{
					dictionary.Add(key, text);
					continue;
				}
				dictionary.Remove(key);
				dictionary.Add(key, value3 + text);
			}
		}
		string text5 = "--- TARGET EVENT DUMP ---\n\n";
		foreach (object key3 in dictionary.Keys)
		{
			if (key3 != null)
			{
				text5 = text5 + key3.ToString() + ":\n";
				string value4;
				if (dictionary.TryGetValue(key3, out value4))
				{
					text5 += value4;
				}
			}
		}
		return text5;
	}
}
