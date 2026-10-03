using System;
using UnityEngine;

[Serializable]
public struct CryptoString : IEquatable<CryptoString>
{
	[SerializeField]
	private string cryptoKey;

	[SerializeField]
	private byte[] hiddenValue;

	[SerializeField]
	private string fakeValue;

	[SerializeField]
	private bool inited;

	private CryptoString(string value)
	{
		cryptoKey = CryptoRandom.random.Next(int.MinValue, int.MaxValue).ToString();
		hiddenValue = GetBytes(value);
		for (int i = 0; i < hiddenValue.Length; i++)
		{
			hiddenValue[i] = (byte)(hiddenValue[i] ^ cryptoKey[i % cryptoKey.Length]);
		}
		fakeValue = value;
		inited = true;
	}

	public void SetValue(string value)
	{
		cryptoKey = CryptoRandom.random.Next(int.MinValue, int.MaxValue).ToString();
		hiddenValue = GetBytes(value);
		for (int i = 0; i < hiddenValue.Length; i++)
		{
			hiddenValue[i] = (byte)(hiddenValue[i] ^ cryptoKey[i % cryptoKey.Length]);
		}
		fakeValue = value;
	}

	private string GetValue()
	{
		if (!inited)
		{
			cryptoKey = CryptoRandom.random.Next(int.MinValue, int.MaxValue).ToString();
			hiddenValue = new byte[0];
			fakeValue = string.Empty;
			inited = true;
		}
		byte[] array = new byte[hiddenValue.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (byte)(hiddenValue[i] ^ cryptoKey[i % cryptoKey.Length]);
		}
		string text = GetString(array);
		if (!string.IsNullOrEmpty(fakeValue) && fakeValue != text)
		{
			Debug.Log("Detected String");
			Application.Quit();
		}
		return text;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is CryptoString))
		{
			return false;
		}
		return Equals((CryptoString)obj);
	}

	public bool Equals(CryptoString obj)
	{
		return GetValue() == obj.GetValue();
	}

	public override int GetHashCode()
	{
		return GetValue().GetHashCode();
	}

	public override string ToString()
	{
		return GetValue().ToString();
	}

	private static byte[] GetBytes(string str)
	{
		byte[] array = new byte[str.Length * 2];
		Buffer.BlockCopy(str.ToCharArray(), 0, array, 0, array.Length);
		return array;
	}

	private static string GetString(byte[] bytes)
	{
		char[] array = new char[bytes.Length / 2];
		Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
		return new string(array);
	}

	public static implicit operator CryptoString(string value)
	{
		return new CryptoString(value);
	}

	public static implicit operator string(CryptoString value)
	{
		return value.GetValue();
	}
}
