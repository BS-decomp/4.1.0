using PigeonCoopToolkit.Effects.Trails;
using UnityEngine;

public class SmokePlumeManager : MonoBehaviour
{
	public SmokePlume Smoke;

	public float SmokeAfter;

	public float SmokeMax;

	public float SmokeIncrement;

	public float SmokeDecrease;

	private float SmokeValue;

	private bool isSmoke;

	private Transform mTransform;

	private bool Enabled;

	private static SmokePlumeManager instance;

	private void Start()
	{
		instance = this;
		mTransform = base.transform;
	}

	public static void ClearParent()
	{
		instance.mTransform.SetParent(null);
		instance.SmokeValue = 0f;
	}

	public static void SetParent(Transform parent, Vector3 pos)
	{
		if (parent == null)
		{
			instance.mTransform.SetParent(null);
		}
		else
		{
			instance.mTransform.SetParent(parent);
			instance.mTransform.localPosition = pos;
		}
		instance.SmokeValue = 0f;
	}

	public static void Fire()
	{
		if (Settings.SmokePlume)
		{
			instance.SmokeValue += instance.SmokeIncrement;
		}
	}

	private void LateUpdate()
	{
		if (SmokeValue > SmokeAfter)
		{
			if (!isSmoke)
			{
				isSmoke = true;
				Smoke.Emit = true;
			}
		}
		else if (isSmoke)
		{
			isSmoke = false;
			Smoke.Emit = false;
		}
		SmokeValue = Mathf.Min(SmokeValue, SmokeMax);
		if (SmokeValue > 0f)
		{
			SmokeValue -= SmokeDecrease;
		}
	}
}
