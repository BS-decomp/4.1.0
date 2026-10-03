using System;
using System.Collections;

internal static class vp_GlobalEventInternal
{
	public class UnregisterException : Exception
	{
		public UnregisterException(string msg)
			: base(msg)
		{
		}
	}

	public class SendException : Exception
	{
		public SendException(string msg)
			: base(msg)
		{
		}
	}

	public static Hashtable Callbacks = new Hashtable();

	public static UnregisterException ShowUnregisterException(string name)
	{
		return new UnregisterException(string.Format("Attempting to Unregister the event {0} but vp_GlobalEvent has not registered this event.", name));
	}

	public static SendException ShowSendException(string name)
	{
		return new SendException(string.Format("Attempting to Send the event {0} but vp_GlobalEvent has not registered this event.", name));
	}
}
