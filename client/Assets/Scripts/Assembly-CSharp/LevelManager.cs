using System.Collections.Generic;
using FreeJSON;
using UnityEngine;

public class LevelManager
{
	private static Dictionary<GameMode, List<string>> SceneList = new Dictionary<GameMode, List<string>>();

	public static void LoadFile()
	{
		if (SceneList.Count != 0)
		{
			return;
		}
		TextAsset textAsset = Resources.Load("Others/SceneManager") as TextAsset;
		JsonArray jsonArray = JsonArray.Parse(textAsset.text);
		for (int i = 0; i < jsonArray.Length; i++)
		{
			GameMode key = jsonArray.Get<JsonObject>(i).Get<GameMode>("GameMode");
			List<string> list = new List<string>();
			JsonArray jsonArray2 = jsonArray.Get<JsonObject>(i).Get<JsonArray>("Scenes");
			for (int j = 0; j < jsonArray2.Length; j++)
			{
				list.Add(jsonArray2.Get<string>(j));
			}
			SceneList.Add(key, list);
		}
	}

	public static List<string> GetGameModeScenes(GameMode mode)
	{
		LoadFile();
		return SceneList[mode];
	}

	public static string GetNextScene(GameMode mode)
	{
		return GetNextScene(mode, GetSceneName());
	}

	public static string GetNextScene(GameMode mode, string scene)
	{
		LoadFile();
		List<string> list = SceneList[mode];
		for (int i = 0; i < list.Count; i++)
		{
			if (scene == list[i])
			{
				if (list.Count - 1 == i)
				{
					return list[0];
				}
				return list[i + 1];
			}
		}
		return string.Empty;
	}

	public static bool HasSceneInGameMode(GameMode mode)
	{
		List<string> gameModeScenes = GetGameModeScenes(mode);
		return gameModeScenes.Contains(GetSceneName());
	}

	public static string GetSceneName()
	{
		string loadedLevelName = Application.loadedLevelName;
		loadedLevelName = loadedLevelName.Replace("#", "/");
		return Utils.Decrypt(loadedLevelName);
	}

	private static string GetEncryptSceneName(string name)
	{
		string text = Utils.Encrypt(name);
		return text.Replace("/", "#");
	}

	public static void LoadLevel(string name)
	{
		Application.LoadLevel(GetEncryptSceneName(name));
	}
}
