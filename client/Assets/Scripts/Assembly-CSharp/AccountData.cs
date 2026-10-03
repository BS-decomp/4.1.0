using System;
using System.Collections.Generic;

[Serializable]
public class AccountData
{
	public CryptoInt GameVersion = 0;

	public CryptoString AccountName;

	public CryptoInt Money = 100;

	public CryptoInt Gold = 10;

	public CryptoInt XP = 0;

	public CryptoInt Level = 1;

	public CryptoInt OpenCase = 0;

	public CryptoInt Deaths = 0;

	public CryptoInt Kills = 0;

	public CryptoInt Headshot = 0;

	public CryptoInt SelectedRifle = 12;

	public CryptoInt SelectedPistol = 3;

	public CryptoInt SelectedKnife = 4;

	public CryptoInt SelectedPlayerSkin = 0;

	public CryptoString Clan;

	public CryptoInt Session;

	public List<CryptoInt> PlayerSkins = new List<CryptoInt>();

	public List<AccountSticker> Stickers = new List<AccountSticker>();

	public List<AccountWeapon> Weapons = new List<AccountWeapon>();

	public List<CryptoString> InAppPurchase = new List<CryptoString>();

	public CryptoString AndroidID;

	public void SortStickers()
	{
		Stickers.Sort(SortStickersComparer);
	}

	private int SortStickersComparer(AccountSticker a, AccountSticker b)
	{
		return ((int)a.ID).CompareTo(b.ID);
	}
}
