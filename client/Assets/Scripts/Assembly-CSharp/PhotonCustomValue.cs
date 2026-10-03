using ExitGames.Client.Photon;

internal static class PhotonCustomValue
{
	private static string teamKey = "t";

	public static string levelKey = "l";

	private static string clanKey = "c";

	private static string pingKey = "p";

	private static string deathsKey = "d";

	private static string killsKey = "k";

	private static string deadKey = "de";

	public static string playerIDKey = "pl";

	public static string kickKey = "ki";

	public static string gameModeKey = "g";

	public static string onlyWeaponKey = "o";

	public static string roundStateKey = "r";

	public static string sceneNameKey = "s";

	public static string passwordKey = "p";

	public static void ClearProperties(this PhotonPlayer player)
	{
		Hashtable hashtable = new Hashtable();
		hashtable[teamKey] = (byte)0;
		hashtable[deathsKey] = (byte)0;
		hashtable[killsKey] = (byte)0;
		hashtable[deadKey] = true;
		player.SetCustomProperties(hashtable);
	}

	public static void SetTeam(this PhotonPlayer player, Team team)
	{
		Hashtable hashtable = new Hashtable();
		hashtable[teamKey] = (byte)team;
		player.SetCustomProperties(hashtable);
	}

	public static Team GetTeam(this PhotonPlayer player)
	{
		object value;
		if (player.CustomProperties.TryGetValue(teamKey, out value))
		{
			return (Team)(byte)value;
		}
		return Team.None;
	}

	public static void SetLevel(this PhotonPlayer player, int level)
	{
		Hashtable hashtable = new Hashtable();
		hashtable[levelKey] = (byte)level;
		player.SetCustomProperties(hashtable);
	}

	public static int GetLevel(this PhotonPlayer player)
	{
		object value;
		if (player.CustomProperties.TryGetValue(levelKey, out value))
		{
			return (byte)value;
		}
		return 1;
	}

	public static void SetClan(this PhotonPlayer player, string clan)
	{
		Hashtable hashtable = new Hashtable();
		hashtable[clanKey] = clan;
		player.SetCustomProperties(hashtable);
	}

	public static string GetClan(this PhotonPlayer player)
	{
		object value;
		if (player.CustomProperties.TryGetValue(clanKey, out value))
		{
			return (string)value;
		}
		return string.Empty;
	}

	public static void UpdatePing(this PhotonPlayer player)
	{
		Hashtable hashtable = new Hashtable();
		hashtable[pingKey] = (short)PhotonNetwork.GetPing();
		player.SetCustomProperties(hashtable);
	}

	public static int GetPing(this PhotonPlayer player)
	{
		object value;
		if (player.CustomProperties.TryGetValue(pingKey, out value))
		{
			return (short)value;
		}
		return 200;
	}

	public static void SetDeaths(this PhotonPlayer player, int deaths)
	{
		Hashtable hashtable = new Hashtable();
		if (deaths < 255)
		{
			hashtable[deathsKey] = (byte)deaths;
		}
		else
		{
			hashtable[deathsKey] = deaths;
		}
		player.SetCustomProperties(hashtable);
	}

	public static void SetDeaths1(this PhotonPlayer player)
	{
		int deaths = player.GetDeaths();
		deaths++;
		player.SetDeaths(deaths);
	}

	public static int GetDeaths(this PhotonPlayer player)
	{
		object value;
		if (player.CustomProperties.TryGetValue(deathsKey, out value))
		{
			try
			{
				return (byte)value;
			}
			catch
			{
				return (int)value;
			}
		}
		return 0;
	}

	public static void SetKills(this PhotonPlayer player, int kills)
	{
		Hashtable hashtable = new Hashtable();
		if (kills < 255)
		{
			hashtable[killsKey] = (byte)kills;
		}
		else
		{
			hashtable[killsKey] = kills;
		}
		player.SetCustomProperties(hashtable);
	}

	public static void SetKills1(this PhotonPlayer player)
	{
		int kills = player.GetKills();
		kills++;
		player.SetKills(kills);
	}

	public static int GetKills(this PhotonPlayer player)
	{
		object value;
		if (player.CustomProperties.TryGetValue(killsKey, out value))
		{
			try
			{
				return (byte)value;
			}
			catch
			{
				return (int)value;
			}
		}
		return 0;
	}

	public static void SetDead(this PhotonPlayer player, bool dead)
	{
		Hashtable hashtable = new Hashtable();
		hashtable[deadKey] = dead;
		player.SetCustomProperties(hashtable);
	}

	public static bool GetDead(this PhotonPlayer player)
	{
		object value;
		if (player.CustomProperties.TryGetValue(deadKey, out value))
		{
			return (bool)value;
		}
		return false;
	}

	public static void SetPlayerID(this PhotonPlayer player, string id)
	{
		Hashtable hashtable = new Hashtable();
		hashtable[playerIDKey] = id;
		player.SetCustomProperties(hashtable);
	}

	public static string GetPlayerID(this PhotonPlayer player)
	{
		object value;
		if (player.CustomProperties.TryGetValue(playerIDKey, out value))
		{
			return (string)value;
		}
		return string.Empty;
	}

	public static void SetKick(this PhotonPlayer player, byte count)
	{
		Hashtable hashtable = new Hashtable();
		hashtable[kickKey] = count;
		player.SetCustomProperties(hashtable);
	}

	public static void SetKick1(this PhotonPlayer player)
	{
		byte kick = player.GetKick();
		kick++;
		player.SetKick(kick);
	}

	public static byte GetKick(this PhotonPlayer player)
	{
		object value;
		if (player.CustomProperties.TryGetValue(kickKey, out value))
		{
			return (byte)value;
		}
		return 0;
	}

	public static void SetGameMode(this Room room, GameMode mode)
	{
		Hashtable hashtable = new Hashtable();
		hashtable[gameModeKey] = (byte)mode;
		room.SetCustomProperties(hashtable);
	}

	public static GameMode GetGameMode(this Room room)
	{
		object value;
		if (room.CustomProperties.TryGetValue(gameModeKey, out value))
		{
			return (GameMode)(byte)value;
		}
		return GameMode.TeamDeathmatch;
	}

	public static void SetOnlyWeapon(this Room room, int weaponID)
	{
		Hashtable hashtable = new Hashtable();
		hashtable[onlyWeaponKey] = (byte)weaponID;
		room.SetCustomProperties(hashtable);
	}

	public static int GetOnlyWeapon(this Room room)
	{
		object value;
		if (room.CustomProperties.TryGetValue(onlyWeaponKey, out value))
		{
			return (byte)value;
		}
		return 1;
	}

	public static void SetRoundState(this Room room, RoundState state)
	{
		Hashtable hashtable = new Hashtable();
		hashtable[roundStateKey] = (byte)state;
		room.SetCustomProperties(hashtable);
	}

	public static RoundState GetRoundState(this Room room)
	{
		object value;
		if (room.CustomProperties.TryGetValue(roundStateKey, out value))
		{
			return (RoundState)(byte)value;
		}
		return RoundState.WaitPlayer;
	}

	public static GameMode GetGameMode(this RoomInfo room)
	{
		object value;
		if (room.CustomProperties.TryGetValue(gameModeKey, out value))
		{
			return (GameMode)(byte)value;
		}
		return GameMode.TeamDeathmatch;
	}

	public static string GetSceneName(this RoomInfo room)
	{
		object value;
		if (room.CustomProperties.TryGetValue(sceneNameKey, out value))
		{
			return (string)value;
		}
		return string.Empty;
	}

	public static int GetOnlyWeapon(this RoomInfo room)
	{
		object value;
		if (room.CustomProperties.TryGetValue(onlyWeaponKey, out value))
		{
			return (byte)value;
		}
		return 1;
	}

	public static Hashtable CreateRoomHashtable(this RoomInfo photonNetwork, string password, GameMode mode)
	{
		Hashtable hashtable = new Hashtable();
		hashtable[passwordKey] = password;
		hashtable[gameModeKey] = (byte)mode;
		return hashtable;
	}

	public static string GetPassword(this RoomInfo room)
	{
		object value;
		if (room.CustomProperties.TryGetValue(passwordKey, out value))
		{
			return (string)value;
		}
		return string.Empty;
	}
}
