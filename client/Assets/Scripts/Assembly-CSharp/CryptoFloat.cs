using System;
using UnityEngine;

[Serializable]
public struct CryptoFloat : IFormattable, IEquatable<CryptoFloat>
{
	[SerializeField]
	private int cryptoKey;

	[SerializeField]
	private byte[] hiddenValue;

	[SerializeField]
	private float fakeValue;

	[SerializeField]
	private bool inited;

	private CryptoFloat(float value)
	{
		cryptoKey = CryptoRandom.random.Next(int.MinValue, int.MaxValue);
		hiddenValue = BitConverter.GetBytes(value);
		for (int i = 0; i < hiddenValue.Length; i++)
		{
			hiddenValue[i] = (byte)(hiddenValue[i] ^ cryptoKey);
		}
		fakeValue = value;
		inited = true;
	}

	public void SetValue(float value)
	{
		cryptoKey = CryptoRandom.random.Next(int.MinValue, int.MaxValue);
		hiddenValue = BitConverter.GetBytes(value);
		for (int i = 0; i < hiddenValue.Length; i++)
		{
			hiddenValue[i] = (byte)(hiddenValue[i] ^ cryptoKey);
		}
		fakeValue = value;
	}

	private float GetValue()
	{
		if (!inited)
		{
			cryptoKey = CryptoRandom.random.Next(int.MinValue, int.MaxValue);
			hiddenValue = new byte[4];
			fakeValue = 0f;
			inited = true;
		}
		byte[] array = new byte[4];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (byte)(hiddenValue[i] ^ cryptoKey);
		}
		float num = BitConverter.ToSingle(array, 0);
		if (fakeValue != 0f && fakeValue != num)
		{
			Debug.Log("Detected Float");
			Application.Quit();
		}
		return num;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is CryptoFloat))
		{
			return false;
		}
		return Equals((CryptoFloat)obj);
	}

	public bool Equals(CryptoFloat obj)
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

	public string ToString(string format)
	{
		return GetValue().ToString(format);
	}

	public string ToString(IFormatProvider provider)
	{
		return GetValue().ToString(provider);
	}

	public string ToString(string format, IFormatProvider provider)
	{
		return GetValue().ToString(format, provider);
	}

	public static implicit operator CryptoFloat(float value)
	{
		return new CryptoFloat(value);
	}

	public static implicit operator float(CryptoFloat value)
	{
		return value.GetValue();
	}

	public static CryptoFloat operator ++(CryptoFloat value)
	{
		value.SetValue(value.GetValue() + 1f);
		return value;
	}

	public static CryptoFloat operator --(CryptoFloat value)
	{
		value.SetValue(value.GetValue() - 1f);
		return value;
	}
}
