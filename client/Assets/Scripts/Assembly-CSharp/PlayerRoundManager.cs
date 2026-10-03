using System.Collections.Generic;
using FreeJSON;
using UnityEngine;

public class PlayerRoundManager
{
	public class RoundData
	{
		public string Map;

		public CryptoInt XP = 0;

		public CryptoInt Money = 0;

		public CryptoInt Kills = 0;

		public CryptoInt Headshot = 0;

		public CryptoInt Deaths = 0;

		public float StartTime;

		public float FinishTime;
	}

	private static List<RoundData> DataList = new List<RoundData>();

	private static GameMode Mode;

	public static RoundData GetRoundData(int index)
	{
		return DataList[index];
	}

	public static GameMode GetMode()
	{
		return Mode;
	}

	public static int GetXP()
	{
		int num = 0;
		for (int i = 0; i < DataList.Count; i++)
		{
			num += (int)DataList[i].XP;
		}
		return num;
	}

	public static int GetMoney()
	{
		int num = 0;
		for (int i = 0; i < DataList.Count; i++)
		{
			num += (int)DataList[i].Money;
		}
		return num;
	}

	public static int GetKills()
	{
		int num = 0;
		for (int i = 0; i < DataList.Count; i++)
		{
			num += (int)DataList[i].Kills;
		}
		return num;
	}

	public static int GetHeadshot()
	{
		int num = 0;
		for (int i = 0; i < DataList.Count; i++)
		{
			num += (int)DataList[i].Headshot;
		}
		return num;
	}

	public static int GetDeaths()
	{
		int num = 0;
		for (int i = 0; i < DataList.Count; i++)
		{
			num += (int)DataList[i].Deaths;
		}
		return num;
	}

	public static float GetTime()
	{
		return DataList[GetCount() - 1].FinishTime - DataList[0].StartTime;
	}

	public static int GetCount()
	{
		return DataList.Count;
	}

	public static void NewScene(string map)
	{
		if (GetCount() != 0)
		{
			DataList[GetCount() - 1].FinishTime = Time.time;
		}
		Mode = PhotonNetwork.room.GetGameMode();
		RoundData roundData = new RoundData();
		roundData.StartTime = Time.time;
		roundData.Map = map;
		DataList.Add(roundData);
	}

	public static void SetXP(int xp)
	{
		RoundData roundData = DataList[DataList.Count - 1];
		roundData.XP = (int)roundData.XP + xp;
	}

	public static void SetMoney(int money)
	{
		RoundData roundData = DataList[DataList.Count - 1];
		roundData.Money = (int)roundData.Money + money;
	}

	public static void SetKills1()
	{
		RoundData roundData = DataList[DataList.Count - 1];
		roundData.Kills = (int)roundData.Kills + 1;
	}

	public static void SetHeadshot1()
	{
		RoundData roundData = DataList[DataList.Count - 1];
		roundData.Headshot = (int)roundData.Headshot + 1;
	}

	public static void SetDeaths1()
	{
		RoundData roundData = DataList[DataList.Count - 1];
		roundData.Deaths = (int)roundData.Deaths + 1;
	}

	public static bool HasValue()
	{
		if (GetXP() != 0)
		{
			return true;
		}
		if (GetMoney() != 0)
		{
			return true;
		}
		if (GetKills() != 0)
		{
			return true;
		}
		if (GetHeadshot() != 0)
		{
			return true;
		}
		if (GetDeaths() != 0)
		{
			return true;
		}
		return false;
	}

	public static void Show()
	{
		if (PhotonNetwork.offlineMode)
		{
			return;
		}
		if (GetCount() != 0)
		{
			DataList[DataList.Count - 1].FinishTime = Time.time;
		}
		TimerManager.In(0.5f, false, () =>
		{
			if (AccountManager.GetInAppPurchase().Count > 0)
			{
				ShowPopup();
			}
			else
			{
				AdsManager.ShowAny("ExitServer");
				TimerManager.In(0.5f, ShowPopup);
			}
		});
	}

	private static void ShowPopup()
	{
		if (HasValue())
		{
			mPlayerRoundManager.Show(ToJson(), GetMode());
			SetData();
		}
	}

	private static void SetData()
	{
		AccountManager.SetMoney1(GetMoney());
		AccountManager.SetXP1(GetXP());
		AccountManager.SetKills1(GetKills());
		AccountManager.SetDeaths1(GetDeaths());
		AccountManager.SetHeadshot1(GetHeadshot());
		AccountManager.UpdateDefaultAndWeaponsData();
		Clear();
	}

	public static void Clear()
	{
		DataList.Clear();
	}

	private static JsonArray ToJson()
	{
		JsonArray jsonArray = new JsonArray();
		for (int i = -1; i < DataList.Count; i++)
		{
			JsonObject jsonObject = new JsonObject();
			if (i == -1)
			{
				jsonObject.Add("map", Localization.Get("Total"));
				jsonObject.Add("xp", GetXP());
				jsonObject.Add("money", GetMoney());
				jsonObject.Add("kills", GetKills());
				jsonObject.Add("headshot", GetHeadshot());
				jsonObject.Add("deaths", GetDeaths());
				jsonObject.Add("time", GetTime());
			}
			else
			{
				jsonObject.Add("map", DataList[i].Map);
				jsonObject.Add("xp", (int)DataList[i].XP);
				jsonObject.Add("money", (int)DataList[i].Money);
				jsonObject.Add("kills", (int)DataList[i].Kills);
				jsonObject.Add("headshot", (int)DataList[i].Headshot);
				jsonObject.Add("deaths", (int)DataList[i].Deaths);
				jsonObject.Add("time", DataList[i].FinishTime - DataList[i].StartTime);
			}
			jsonArray.Add(jsonObject);
		}
		return jsonArray;
	}
}
