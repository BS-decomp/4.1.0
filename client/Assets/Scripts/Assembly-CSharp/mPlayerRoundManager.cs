using FreeJSON;
using UnityEngine;

public class mPlayerRoundManager : MonoBehaviour
{
	public GameObject Panel;

	public UILabel ModeLabel;

	public UILabel MapLabel;

	public UILabel MoneyLabel;

	public UILabel XpLabel;

	public UILabel KillsLabel;

	public UILabel HeadshotLabel;

	public UILabel DeathsLabel;

	public UILabel TimeLabel;

	public UILabel TotalXpLabel;

	private int Index = -1;

	private JsonArray Data;

	private static mPlayerRoundManager instance;

	private void Start()
	{
		instance = this;
	}

	public static void Show(JsonArray json, GameMode mode)
	{
		instance.Data = json;
		instance.Panel.SetActive(true);
		instance.Index = 0;
		instance.ModeLabel.text = Localization.Get(mode.ToString());
		instance.ShowData();
	}

	private void ShowData()
	{
		JsonObject jsonObject = Data.Get<JsonObject>(Index);
		MapLabel.text = jsonObject.Get<string>("map");
		MoneyLabel.text = jsonObject.Get<string>("money");
		XpLabel.text = jsonObject.Get<string>("xp");
		KillsLabel.text = jsonObject.Get<string>("kills");
		HeadshotLabel.text = jsonObject.Get<string>("headshot");
		DeathsLabel.text = jsonObject.Get<string>("deaths");
		TimeLabel.text = ConvertTime(jsonObject.Get<float>("time"));
		TotalXpLabel.text = AccountManager.GetXP() + "/" + AccountManager.GetMaxXP() + " xp";
	}

	public void Next()
	{
		Index++;
		if (Index > Data.Length - 1)
		{
			Index = 0;
		}
		ShowData();
	}

	public void Last()
	{
		Index--;
		if (Index < 0)
		{
			Index = Data.Length - 1;
		}
		ShowData();
	}

	public void Close()
	{
		PlayerRoundManager.Clear();
		EventManager.Dispatch("AccountUpdate");
	}

	private static string ConvertTime(float time)
	{
		int num = (int)time / 3600;
		int num2 = (int)time / 60;
		int num3 = (int)time - num2 * 60;
		return string.Format("{0:0}:{1:00}:{2:00}", num, num2, num3);
	}
}
