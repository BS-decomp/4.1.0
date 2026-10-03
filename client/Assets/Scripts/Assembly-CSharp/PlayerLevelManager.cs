using UnityEngine;

public static class PlayerLevelManager
{
	public static void UpdatePlayerXP(int xp)
	{
		int playerLevel = GetPlayerLevel();
		int num = GetPlayerXP() + xp;
		int maxPlayerXP = GetMaxPlayerXP();
		if (num >= maxPlayerXP)
		{
			if (playerLevel == 100)
			{
				num = maxPlayerXP;
				maxPlayerXP = 150 + 150 * playerLevel;
			}
			else
			{
				playerLevel++;
				num -= maxPlayerXP;
				num = Mathf.Max(num, 0);
				maxPlayerXP = 150 + 150 * playerLevel;
				AccountManager.SetLevel(playerLevel);
				if (LevelManager.GetSceneName() == "Menu")
				{
					UIToast.Show(Localization.Get("New Level") + " " + playerLevel);
					AccountManager.SetMoney1(150);
					AccountManager.SetGold1(5);
					AchievementsManager.UpdateLevel();
				}
			}
		}
		AccountManager.SetXP(num);
	}

	public static int GetPlayerLevel()
	{
		return AccountManager.GetLevel();
	}

	public static int GetPlayerXP()
	{
		int playerLevel = GetPlayerLevel();
		if (playerLevel == 100)
		{
			return GetMaxPlayerXP();
		}
		return AccountManager.GetXP();
	}

	public static int GetMaxPlayerXP()
	{
		return 150 + 150 * GetPlayerLevel();
	}

	public static float GetPlayerXPPercent()
	{
		return (float)GetPlayerXP() / (float)GetMaxPlayerXP();
	}
}
