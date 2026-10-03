using Prime31;

public class AchievementsManager
{
	public static void UpdateLevel()
	{
		switch (AccountManager.GetLevel())
		{
		case 5:
			PlayGameServices.unlockAchievement("CgkIm8KC0IsDEAIQAA");
			break;
		case 25:
			PlayGameServices.unlockAchievement("CgkIm8KC0IsDEAIQAQ");
			break;
		case 50:
			PlayGameServices.unlockAchievement("CgkIm8KC0IsDEAIQAg");
			break;
		case 75:
			PlayGameServices.unlockAchievement("CgkIm8KC0IsDEAIQAw");
			break;
		case 100:
			PlayGameServices.unlockAchievement("CgkIm8KC0IsDEAIQBA");
			break;
		}
	}

	public static void UpdateMoney()
	{
		if (AccountManager.GetMoney() > 10000)
		{
			PlayGameServices.unlockAchievement("CgkIm8KC0IsDEAIQDg");
		}
		if (AccountManager.GetGold() > 1000)
		{
			PlayGameServices.unlockAchievement("CgkIm8KC0IsDEAIQDw");
		}
	}

	public static void UpdateKills(DamageInfo damageInfo)
	{
		PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQBQ", 1);
		PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQBg", 1);
		PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQBw", 1);
		PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQCA", 1);
		if (damageInfo.HeadShot)
		{
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQCQ", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQCg", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQCw", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQDA", 1);
		}
		if (!PlayerInput.instance.Grounded)
		{
			PlayGameServices.unlockAchievement("CgkIm8KC0IsDEAIQDQ");
		}
		switch (damageInfo.WeaponID)
		{
		case 1:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQEw", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQFA", 1);
			break;
		case 2:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQGQ", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQGg", 1);
			break;
		case 3:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQEQ", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQEg", 1);
			break;
		case 4:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQFw", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQGA", 1);
			break;
		case 5:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQFQ", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQFg", 1);
			break;
		case 6:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQGw", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQHA", 1);
			break;
		case 7:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQFw", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQGA", 1);
			break;
		case 8:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQHw", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQIA", 1);
			break;
		case 9:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQIQ", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQIg", 1);
			break;
		case 10:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQJg", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQJw", 1);
			break;
		case 11:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQKA", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQKQ", 1);
			break;
		case 12:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQIw", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQJQ", 1);
			break;
		case 13:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQKg", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQKw", 1);
			break;
		case 14:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQLw", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQMA", 1);
			break;
		case 15:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQLQ", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQLg", 1);
			break;
		case 16:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQMQ", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQMg", 1);
			break;
		case 18:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQMw", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQNA", 1);
			break;
		case 19:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQNQ", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQNg", 1);
			break;
		case 21:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQNw", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQOA", 1);
			break;
		case 22:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQOg", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQOQ", 1);
			break;
		case 23:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQOw", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQPA", 1);
			break;
		case 24:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQPQ", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQPg", 1);
			break;
		case 25:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQPw", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQQA", 1);
			break;
		case 26:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQQQ", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQQg", 1);
			break;
		case 27:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQQw", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQRA", 1);
			break;
		case 28:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQRQ", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQRg", 1);
			break;
		case 29:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQRw", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQSA", 1);
			break;
		case 30:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQSQ", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQSg", 1);
			break;
		case 36:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQSw", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQTA", 1);
			break;
		case 37:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQTQ", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQTg", 1);
			break;
		case 38:
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQTw", 1);
			PlayGameServices.incrementAchievement("CgkIm8KC0IsDEAIQUA", 1);
			break;
		case 17:
		case 20:
		case 31:
		case 32:
		case 33:
		case 34:
		case 35:
			break;
		}
	}
}
