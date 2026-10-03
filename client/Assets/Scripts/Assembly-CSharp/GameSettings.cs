using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSettings : ScriptableObject
{
	public string PhotonID;

	public GameObject PlayerController;

	public GameObject PlayerSkin;

	public UIAtlas WeaponAtlas;

	public UIAtlas WeaponIconAtlas;

	public UIAtlas StickersAtlas;

	public AudioClip ConnectDeveloperAudio;

	public List<AmbientSettings> Ambients = new List<AmbientSettings>();

	public List<WeaponData> Weapons = new List<WeaponData>();

	public List<WeaponStoreData> WeaponsStore = new List<WeaponStoreData>();

	public List<StickerData> Stickers = new List<StickerData>();

	public List<Vector2> WeaponsCaseSize = new List<Vector2>();

	public List<PlayerStoreSkinData> PlayerStoreSkins = new List<PlayerStoreSkinData>();

	private static GameSettings Instance;

	public static GameSettings instance
	{
		get
		{
			if (Instance == null)
			{
				Instance = Resources.Load("Others/GameSettings") as GameSettings;
			}
			return Instance;
		}
	}
}
