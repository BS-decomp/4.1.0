using System;
using System.Collections.Generic;
using FreeJSON;

public class ClansManager
{
	public static ClanData Clan;

	public static Dictionary<string, string> PlayersNames;

	public static bool HasClan
	{
		get
		{
			if (Clan != null)
			{
				return true;
			}
			return false;
		}
	}

	public static bool HasPlayersNames
	{
		get
		{
			if (PlayersNames != null)
			{
				return true;
			}
			return false;
		}
	}

	public static void Create(string tag, string clanName, Action complete, Action<string> failed)
	{
		Firebase firebase = new Firebase();
		JsonObject jsonObject = new JsonObject();
		jsonObject.Add("Name", clanName);
		jsonObject.Add("Admin", (string)AccountManager.AccountID);
		jsonObject.Add("Tag", tag);
		JsonArray jsonArray = new JsonArray();
		jsonArray.Add((string)AccountManager.AccountID);
		jsonObject.Add("Players", jsonArray);
		firebase.Child("Players").Child("Clans").Child(clanName.ToLower())
			.SetValue(jsonObject.ToString(), (string result2) =>
			{
				Clan = new ClanData();
				Clan.Admin = AccountManager.AccountID;
				Clan.Name = clanName;
				Clan.Tag = tag;
				Clan.Players = new List<string> { AccountManager.AccountID };
				if (complete != null)
				{
					complete();
				}
			}, (string error) =>
			{
				if (failed != null)
				{
					failed(error);
				}
			});
	}

	public static void Check(string tag, string clanName, Action complete, Action<string> failed)
	{
		FirebaseManager.DebugAction = true;
		Firebase firebase = new Firebase();
		firebase.Child("Players").Child("Clans").GetValue(FirebaseParam.Default.OrderBy("tag").EqualTo(tag), (string result) =>
		{
			if (result == "{}" || result == "null")
			{
				firebase = new Firebase();
				firebase.Child("Players").Child("Clans").Child(clanName.ToLower())
					.GetValue((string text) =>
					{
						if (text == "{}" || text == "null")
						{
							if (complete != null)
							{
								complete();
							}
						}
						else if (failed != null)
						{
							failed(Localization.Get("Clan with this name exists"));
						}
					}, (string error) =>
					{
						if (failed != null)
						{
							failed(error);
						}
					});
			}
			else if (failed != null)
			{
				failed(Localization.Get("Tag with this name exists"));
			}
		}, (string error) =>
		{
			if (failed != null)
			{
				failed(error);
			}
		});
	}

	public static void GetData(Action complete, Action<string> failed)
	{
		Firebase firebase = new Firebase();
		firebase.Child("Players").Child("Clans").Child(AccountManager.GetClan().ToLower())
			.GetValue((string result) =>
			{
				if (result != "{}" && result != "null")
				{
					Clan = JsonConvert.Deserialize(typeof(ClanData), result) as ClanData;
					if (Clan.Players.Contains(AccountManager.AccountID))
					{
						if (complete != null)
						{
							complete();
						}
					}
					else
					{
						AccountManager.SetClan(string.Empty, true);
						if (failed != null)
						{
							failed(Localization.Get("You are no longer a member of a clan"));
						}
					}
				}
				else
				{
					AccountManager.SetClan(string.Empty, true);
					if (failed != null)
					{
						failed(Localization.Get("Clan does not exist"));
					}
				}
			}, (string error) =>
			{
				if (failed != null)
				{
					failed(error);
				}
			});
	}

	public static void AddPlayer(string id)
	{
		Firebase firebase = new Firebase();
		firebase.Child("Players").Child("Clans").Child(Clan.Name.ToLower())
			.Child("players")
			.GetValue((string result) =>
			{
				JsonArray jsonArray = JsonArray.Parse(result);
				if (jsonArray.Length != 0 && !jsonArray.Contains(id))
				{
					jsonArray.Add(id);
					firebase = new Firebase();
					firebase.Child("Players").Child("Clans").Child(Clan.Name.ToLower())
						.Child("players")
						.SetValue(jsonArray.ToString());
				}
			}, null);
	}

	public static void DeletePlayer(string id)
	{
		Firebase firebase = new Firebase();
		firebase.Child("Players").Child("Clans").Child(Clan.Name.ToLower())
			.Child("players")
			.GetValue((string result) =>
			{
				JsonArray jsonArray = JsonArray.Parse(result);
				if (jsonArray.Length != 0 && jsonArray.Remove(id))
				{
					firebase = new Firebase();
					firebase.Child("Players").Child("Clans").Child(Clan.Name.ToLower())
						.Child("players")
						.SetValue(jsonArray.ToString());
				}
			}, null);
	}

	public static void Delete()
	{
	}

	public static void GetPlayersNames(Action complete)
	{
		int count = Clan.Players.Count;
		int count2 = 0;
		for (int i = 0; i < Clan.Players.Count; i++)
		{
			Firebase firebase = new Firebase();
			firebase.Auth = AccountManager.AccountToken;
			string key = Clan.Players[i][0].ToString().ToUpper();
			string name = Clan.Players[i];
			firebase.Child("Players").Child("Accounts").Child(key)
				.Child(name)
				.Child("AccountName")
				.GetValue((string result) =>
				{
					if (!HasPlayersNames)
					{
						PlayersNames = new Dictionary<string, string>();
					}
					PlayersNames.Add(name, result);
					count2++;
					if (count2 >= count && complete != null)
					{
						complete();
					}
				}, (string error) =>
				{
					count2++;
					if (count2 >= count && complete != null)
					{
						complete();
					}
				});
		}
	}
}
