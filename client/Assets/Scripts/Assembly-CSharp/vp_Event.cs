using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public abstract class vp_Event
{
	protected string m_Name;

	protected Type m_ArgumentType;

	protected Type m_ReturnType;

	protected FieldInfo[] m_Fields;

	protected Type[] m_DelegateTypes;

	protected MethodInfo[] m_DefaultMethods;

	public string[] InvokerFieldNames;

	public Dictionary<string, int> Prefixes;

	public string EventName
	{
		get
		{
			return m_Name;
		}
	}

	public Type ArgumentType
	{
		get
		{
			return m_ArgumentType;
		}
	}

	public Type ReturnType
	{
		get
		{
			return m_ReturnType;
		}
	}

	private Type GetArgumentType
	{
		get
		{
			if (!GetType().IsGenericType)
			{
				return typeof(void);
			}
			return GetType().GetGenericArguments()[0];
		}
	}

	private Type GetGenericReturnType
	{
		get
		{
			if (!GetType().IsGenericType)
			{
				return typeof(void);
			}
			if (GetType().GetGenericArguments().Length != 2)
			{
				return typeof(void);
			}
			return GetType().GetGenericArguments()[1];
		}
	}

	public vp_Event(string name = "")
	{
		m_ArgumentType = GetArgumentType;
		m_ReturnType = GetGenericReturnType;
		m_Name = name;
	}

	public abstract void Register(object target, string method, int variant);

	public abstract void Unregister(object target);

	protected abstract void InitFields();

	protected void StoreInvokerFieldNames()
	{
		InvokerFieldNames = new string[m_Fields.Length];
		for (int i = 0; i < m_Fields.Length; i++)
		{
			InvokerFieldNames[i] = m_Fields[i].Name;
		}
	}

	protected Type MakeGenericType(Type type)
	{
		if (m_ReturnType == typeof(void))
		{
			return type.MakeGenericType(m_ArgumentType, m_ArgumentType);
		}
		return type.MakeGenericType(m_ArgumentType, m_ReturnType, m_ArgumentType, m_ReturnType);
	}

	protected void SetFieldToExternalMethod(object target, FieldInfo field, string method, Type type)
	{
		Delegate obj = Delegate.CreateDelegate(type, target, method, false, false);
		if ((object)obj == null)
		{
			Debug.LogError(string.Concat("Error (", this, ") Failed to bind: ", target, " -> ", method, "."));
		}
		else
		{
			field.SetValue(this, obj);
		}
	}

	protected void AddExternalMethodToField(object target, FieldInfo field, string method, Type type)
	{
		Delegate obj = Delegate.Combine((Delegate)field.GetValue(this), Delegate.CreateDelegate(type, target, method, false, false));
		if ((object)obj == null)
		{
			Debug.LogError(string.Concat("Error (", this, ") Failed to bind: ", target, " -> ", method, "."));
		}
		else
		{
			field.SetValue(this, obj);
		}
	}

	protected void SetFieldToLocalMethod(FieldInfo field, MethodInfo method, Type type)
	{
		if (method != null)
		{
			Delegate obj = Delegate.CreateDelegate(type, method);
			if ((object)obj == null)
			{
				Debug.LogError(string.Concat("Error (", this, ") Failed to bind: ", method, "."));
			}
			else
			{
				field.SetValue(this, obj);
			}
		}
	}

	protected void RemoveExternalMethodFromField(object target, FieldInfo field)
	{
		List<Delegate> list = new List<Delegate>(((Delegate)field.GetValue(this)).GetInvocationList());
		if (list == null)
		{
			Debug.LogError(string.Concat("Error (", this, ") Failed to remove: ", target, " -> ", field.Name, "."));
			return;
		}
		for (int num = list.Count - 1; num > -1; num--)
		{
			if (list[num].Target == target)
			{
				list.Remove(list[num]);
			}
		}
		if (list != null)
		{
			field.SetValue(this, Delegate.Combine(list.ToArray()));
		}
	}

	protected MethodInfo GetStaticGenericMethod(Type e, string name, Type parameterType, Type returnType)
	{
		MethodInfo[] methods = e.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
		foreach (MethodInfo methodInfo in methods)
		{
			if (methodInfo != null && !(methodInfo.Name != name))
			{
				MethodInfo methodInfo2 = ((GetGenericReturnType != typeof(void)) ? methodInfo.MakeGenericMethod(m_ArgumentType, m_ReturnType) : methodInfo.MakeGenericMethod(m_ArgumentType));
				if (methodInfo2.GetParameters().Length <= 1 && (methodInfo2.GetParameters().Length != 1 || parameterType != typeof(void)) && (methodInfo2.GetParameters().Length != 0 || parameterType == typeof(void)) && (methodInfo2.GetParameters().Length != 1 || methodInfo2.GetParameters()[0].ParameterType == parameterType) && returnType == methodInfo2.ReturnType)
				{
					return methodInfo2;
				}
			}
		}
		return null;
	}

	public Type GetParameterType(int index)
	{
		if (!GetType().IsGenericType)
		{
			return typeof(void);
		}
		if (index > m_Fields.Length - 1)
		{
			Debug.LogError(string.Concat("Error: (", this, ") Event '", EventName, "' only supports ", m_Fields.Length, " indices. 'GetParameterType' referenced index ", index, "."));
		}
		if (m_DelegateTypes[index].GetMethod("Invoke").GetParameters().Length == 0)
		{
			return typeof(void);
		}
		return m_ArgumentType;
	}

	public Type GetReturnType(int index)
	{
		if (index > m_Fields.Length - 1)
		{
			Debug.LogError(string.Concat("Error: (", this, ") Event '", EventName, "' only supports ", m_Fields.Length, " indices. 'GetReturnType' referenced index ", index, "."));
			return null;
		}
		if (GetType().GetGenericArguments().Length > 1)
		{
			return GetGenericReturnType;
		}
		Type returnType = m_DelegateTypes[index].GetMethod("Invoke").ReturnType;
		if (returnType.IsGenericParameter)
		{
			return m_ArgumentType;
		}
		return returnType;
	}

	protected void Refresh()
	{
	}
}
