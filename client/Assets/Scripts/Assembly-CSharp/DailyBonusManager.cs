using System;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

public class DailyBonusManager : MonoBehaviour
{
	[Serializable]
	public class BonusClass
	{
		public CryptoInt Money;

		public CryptoInt Gold;

		public CryptoInt BaseCase;

		public CryptoInt ProfessionalCase;

		public CryptoInt LegendaryCase;
	}

	public BonusClass[] Bonus;

	private static DailyBonusManager instance;

	private void Start()
	{
		if (instance == null)
		{
			instance = this;
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			EventManager.AddListener<long>("ServerTime", GetServertTime);
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void GetServertTime(long time)
	{
		if (ObscuredPrefs.HasKey("DailyBonus"))
		{
			long num = ObscuredPrefs.GetLong("DailyBonus");
			if (time > num + 86400000 && time >= num + 172800000)
			{
			}
		}
		else
		{
			ObscuredPrefs.SetLong("DailyBonus", time);
		}
	}
}
