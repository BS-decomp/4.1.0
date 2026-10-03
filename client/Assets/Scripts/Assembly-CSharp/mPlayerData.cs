using UnityEngine;

public class mPlayerData : MonoBehaviour
{
	public UILabel PlayerNameLabel;

	public UILabel PlayerLevelLabel;

	public UIProgressBar PlayerXP;

	public UILabel MoneyLabel;

	public UITexture MoneyTexture;

	public Texture2D[] MoneyTextures;

	public UILabel GoldLabel;

	public UITexture GoldTexture;

	public Texture2D[] GoldTextures;

	private void Start()
	{
		EventManager.AddListener("AccountUpdate", AccountUpdate);
		AccountUpdate();
	}

	private void AccountUpdate()
	{
		PlayerNameLabel.text = AccountManager.AccountName;
		PlayerLevelLabel.text = AccountManager.GetLevel().ToString();
		PlayerXP.value = (float)AccountManager.GetXP() / (float)AccountManager.GetMaxXP();
		MoneyLabel.text = AccountManager.GetMoney().ToString("n0");
		GoldLabel.text = AccountManager.GetGold().ToString("n0");
		int money = AccountManager.GetMoney();
		if (money < 5000)
		{
			MoneyTexture.mainTexture = MoneyTextures[0];
		}
		else if (money < 10000)
		{
			MoneyTexture.mainTexture = MoneyTextures[1];
		}
		else if (money < 20000)
		{
			MoneyTexture.mainTexture = MoneyTextures[2];
		}
		else
		{
			MoneyTexture.mainTexture = MoneyTextures[3];
		}
		int gold = AccountManager.GetGold();
		if (gold < 100)
		{
			GoldTexture.mainTexture = GoldTextures[0];
		}
		else if (gold < 250)
		{
			GoldTexture.mainTexture = GoldTextures[1];
		}
		else if (gold < 600)
		{
			GoldTexture.mainTexture = GoldTextures[2];
		}
		else
		{
			GoldTexture.mainTexture = GoldTextures[3];
		}
	}
}
