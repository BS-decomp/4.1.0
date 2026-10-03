using System;

public class GameModeManager
{
	public static GameMode[] GetGameModeList()
	{
		return new GameMode[18]
		{
			GameMode.TeamDeathmatch,
			GameMode.Classic,
			GameMode.Bomb,
			GameMode.ZombieSurvival,
			GameMode.KnifeMode,
			GameMode.AWPMode,
			GameMode.GunGame,
			GameMode.DeathRun,
			GameMode.Hunter,
			GameMode.RandomMode,
			GameMode.Deathmatch,
			GameMode.BunnyHop,
			GameMode.HungerGames,
			GameMode.Only,
			GameMode.Football,
			GameMode.Juggernaut,
			GameMode.Surf,
			GameMode.MiniGames
		};
	}

	public static GameMode Get(string value)
	{
		return (GameMode)(int)Enum.Parse(typeof(GameMode), value);
	}
}
