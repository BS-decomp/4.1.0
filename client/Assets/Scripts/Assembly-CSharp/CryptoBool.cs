using System;
using UnityEngine;

[Serializable]
public struct CryptoBool : IEquatable<CryptoBool>
{
	[SerializeField]
	private byte cryptoKey;

	[SerializeField]
	private byte hiddenValue;

	[SerializeField]
	private bool fakeValue;

	[SerializeField]
	private bool fakeValueChanged;

	[SerializeField]
	private bool inited;

	private CryptoBool(bool value)
	{
		cryptoKey = (byte)CryptoRandom.random.Next(1, 200);
		hiddenValue = (byte)(((!value) ? 32 : 18) ^ cryptoKey);
		fakeValue = value;
		fakeValueChanged = false;
		inited = true;
	}

	public void SetValue(bool value)
	{
		cryptoKey = (byte)CryptoRandom.random.Next(1, 200);
		hiddenValue = (byte)(((!value) ? 32 : 18) ^ cryptoKey);
		fakeValue = value;
		fakeValueChanged = true;
	}

	private bool GetValue()
	{
		if (!inited)
		{
			cryptoKey = (byte)CryptoRandom.random.Next(1, 200);
			hiddenValue = (byte)(0x20 ^ cryptoKey);
			fakeValue = false;
			fakeValueChanged = false;
			inited = true;
		}
		bool flag = (hiddenValue ^ cryptoKey) != 32;
		if (fakeValueChanged && fakeValue != flag)
		{
			Debug.Log("Detected bool");
			Application.Quit();
		}
		return flag;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is CryptoBool))
		{
			return false;
		}
		return Equals((CryptoBool)obj);
	}

	public bool Equals(CryptoBool obj)
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

	public static implicit operator CryptoBool(bool value)
	{
		return new CryptoBool(value);
	}

	public static implicit operator bool(CryptoBool value)
	{
		return value.GetValue();
	}
}
