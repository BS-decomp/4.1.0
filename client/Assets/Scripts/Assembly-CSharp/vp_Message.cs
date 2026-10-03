using System;
using System.Collections.Generic;
using System.Reflection;

public class vp_Message : vp_Event
{
	public delegate void Sender();

	public Sender Send;

	public vp_Message(string name)
		: base(name)
	{
		InitFields();
	}

	protected static void Empty()
	{
	}

	protected override void InitFields()
	{
		m_Fields = new FieldInfo[1] { GetType().GetField("Send") };
		StoreInvokerFieldNames();
		m_DefaultMethods = new MethodInfo[1] { GetType().GetMethod("Empty") };
		m_DelegateTypes = new Type[1] { typeof(Sender) };
		Prefixes = new Dictionary<string, int> { { "OnMessage_", 0 } };
		Send = Empty;
	}

	public override void Register(object t, string m, int v)
	{
		Send = (Sender)Delegate.Combine(Send, (Sender)Delegate.CreateDelegate(m_DelegateTypes[v], t, m));
		Refresh();
	}

	public override void Unregister(object t)
	{
		RemoveExternalMethodFromField(t, m_Fields[0]);
		Refresh();
	}
}
public class vp_Message<V> : vp_Message
{
	public new delegate void Sender<T>(T value);

	public new Sender<V> Send;

	public vp_Message(string name)
		: base(name)
	{
	}

	protected static void Empty<T>(T value)
	{
	}

	protected override void InitFields()
	{
		m_Fields = new FieldInfo[1] { GetType().GetField("Send") };
		StoreInvokerFieldNames();
		m_DefaultMethods = new MethodInfo[1] { GetStaticGenericMethod(GetType(), "Empty", m_ArgumentType, typeof(void)) };
		m_DelegateTypes = new Type[1] { typeof(Sender<>) };
		Prefixes = new Dictionary<string, int> { { "OnMessage_", 0 } };
		Send = Empty;
		if (m_DefaultMethods[0] != null)
		{
			SetFieldToLocalMethod(m_Fields[0], m_DefaultMethods[0], MakeGenericType(m_DelegateTypes[0]));
		}
	}

	public override void Register(object t, string m, int v)
	{
		if (m != null)
		{
			AddExternalMethodToField(t, m_Fields[v], m, MakeGenericType(m_DelegateTypes[v]));
			Refresh();
		}
	}

	public override void Unregister(object t)
	{
		RemoveExternalMethodFromField(t, m_Fields[0]);
		Refresh();
	}
}
public class vp_Message<V, VResult> : vp_Message
{
	public new delegate TResult Sender<T, TResult>(T value);

	public new Sender<V, VResult> Send;

	public vp_Message(string name)
		: base(name)
	{
	}

	protected static TResult Empty<T, TResult>(T value)
	{
		return default(TResult);
	}

	protected override void InitFields()
	{
		m_Fields = new FieldInfo[1] { GetType().GetField("Send") };
		StoreInvokerFieldNames();
		m_DefaultMethods = new MethodInfo[1] { GetStaticGenericMethod(GetType(), "Empty", m_ArgumentType, m_ReturnType) };
		m_DelegateTypes = new Type[1] { typeof(Sender<, >) };
		Prefixes = new Dictionary<string, int> { { "OnMessage_", 0 } };
		if (m_DefaultMethods[0] != null)
		{
			SetFieldToLocalMethod(m_Fields[0], m_DefaultMethods[0], MakeGenericType(m_DelegateTypes[0]));
		}
	}

	public override void Register(object t, string m, int v)
	{
		if (m != null)
		{
			AddExternalMethodToField(t, m_Fields[0], m, MakeGenericType(m_DelegateTypes[0]));
			Refresh();
		}
	}

	public override void Unregister(object t)
	{
		RemoveExternalMethodFromField(t, m_Fields[0]);
		Refresh();
	}
}
