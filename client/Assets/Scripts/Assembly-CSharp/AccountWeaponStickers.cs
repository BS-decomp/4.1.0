using System;
using System.Collections.Generic;

[Serializable]
public class AccountWeaponStickers
{
	public CryptoInt SkinID;

	public List<AccountWeaponStickerData> StickerData = new List<AccountWeaponStickerData>();

	public void SortWeaponStickerData()
	{
		StickerData.Sort(SortWeaponStickerDataComparer);
	}

	private int SortWeaponStickerDataComparer(AccountWeaponStickerData a, AccountWeaponStickerData b)
	{
		return ((int)a.Index).CompareTo(b.Index);
	}
}
