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

	// BS-decomp 4.1.0 reconstruction.
	// The shipped game stores every scene under a DES-encrypted name and the key
	// is derived from the byte sizes of classes.dex and Assembly-CSharp.dll
	// *inside the APK* (Utils.test, built in UIFontControl.GenerateFont).
	// A Unity project has neither file, so Utils.Encrypt would produce a name
	// that does not exist and every LoadLevel would fail.
	// The scenes are therefore stored under their decrypted names
	// (tools/recover_scene_names.py, docs/scene-names-410.md) and this flag
	// switches the two helpers below to plain names. Set it to false to get the
	// exact original behaviour back.
	public static bool PlainSceneNames = true;

	public static string GetSceneName()
	{
		string loadedLevelName = Application.loadedLevelName;
		loadedLevelName = loadedLevelName.Replace("#", "/");
		if (PlainSceneNames)
		{
			return loadedLevelName;
		}
		return Utils.Decrypt(loadedLevelName);
	}

	private static string GetEncryptSceneName(string name)
	{
		if (PlainSceneNames)
		{
			return name.Replace("/", "#");
		}
		string text = Utils.Encrypt(name);
		return text.Replace("/", "#");
	}

	public static void LoadLevel(string name)
	{
		Application.LoadLevel(GetEncryptSceneName(name));
	}
}
