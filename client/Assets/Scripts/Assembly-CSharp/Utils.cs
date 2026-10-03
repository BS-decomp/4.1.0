using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class Utils : MonoBehaviour
{
	public static string test = string.Empty;

	public static string GetProjectPath()
	{
		return Directory.GetParent(Application.dataPath).FullName;
	}

	public static string GetFileName(string path)
	{
		return Path.GetFileNameWithoutExtension(path);
	}

	public static string Encrypt(string strPlain)
	{
		try
		{
			DESCryptoServiceProvider dESCryptoServiceProvider = new DESCryptoServiceProvider();
			Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(test, GetIV(), 555);
			byte[] bytes = rfc2898DeriveBytes.GetBytes(8);
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (CryptoStream cryptoStream = new CryptoStream(memoryStream, dESCryptoServiceProvider.CreateEncryptor(bytes, GetIV()), CryptoStreamMode.Write))
				{
					memoryStream.Write(GetIV(), 0, GetIV().Length);
					byte[] bytes2 = Encoding.UTF8.GetBytes(strPlain);
					cryptoStream.Write(bytes2, 0, bytes2.Length);
					cryptoStream.FlushFinalBlock();
					return Convert.ToBase64String(memoryStream.ToArray());
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("Encrypt Exception: " + ex);
			return strPlain;
		}
	}

	public static string Decrypt(string strEncript)
	{
		try
		{
			byte[] buffer = Convert.FromBase64String(strEncript);
			using (MemoryStream memoryStream = new MemoryStream(buffer))
			{
				DESCryptoServiceProvider dESCryptoServiceProvider = new DESCryptoServiceProvider();
				byte[] iV = GetIV();
				memoryStream.Read(iV, 0, iV.Length);
				Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(test, iV, 555);
				byte[] bytes = rfc2898DeriveBytes.GetBytes(8);
				using (CryptoStream stream = new CryptoStream(memoryStream, dESCryptoServiceProvider.CreateDecryptor(bytes, iV), CryptoStreamMode.Read))
				{
					using (StreamReader streamReader = new StreamReader(stream))
					{
						return streamReader.ReadToEnd();
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("Decrypt Exception: " + ex);
			return strEncript;
		}
	}

	private static byte[] GetIV()
	{
		return Encoding.UTF8.GetBytes("IvmD123A12");
	}

	public static Vector3 GetVector3(string vector)
	{
		string[] array = vector.Substring(1, vector.Length - 2).Split(',');
		float x = float.Parse(array[0]);
		float y = float.Parse(array[1]);
		float z = float.Parse(array[2]);
		return new Vector3(x, y, z);
	}

	public static Vector2 GetVector2(string vector)
	{
		string[] array = vector.Substring(1, vector.Length - 2).Split(',');
		float x = float.Parse(array[0]);
		float y = float.Parse(array[1]);
		return new Vector2(x, y);
	}

	public static GameObject AddChild(GameObject inst, GameObject parent, [Optional] Vector3 position, [Optional] Quaternion rotation)
	{
		return AddChild(inst, parent.transform, position, rotation);
	}

	public static GameObject AddChild(GameObject inst, Transform parent, [Optional] Vector3 position, [Optional] Quaternion rotation)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(inst, Vector3.zero, Quaternion.identity) as GameObject;
		gameObject.transform.SetParent(parent);
		gameObject.transform.localPosition = position;
		gameObject.transform.localRotation = rotation;
		gameObject.transform.localScale = Vector3.one;
		gameObject.name = gameObject.name.Replace("(Clone)", string.Empty);
		return gameObject;
	}

	public static Vector3 GetRagdollForce(Vector3 playerPosition, Vector3 attackPosition)
	{
		Vector3 result = playerPosition - attackPosition;
		result.x = Mathf.Clamp(result.x, -1f, 1f);
		result.y = Mathf.Clamp(result.y, -1f, 1f);
		result.z = Mathf.Clamp(result.z, -1f, 1f);
		return result;
	}

	public static string GetTeamHexColor(PhotonPlayer player)
	{
		return GetTeamHexColor(player.NickName, player.GetTeam());
	}

	public static string GetTeamHexColor(string text, Team team)
	{
		switch (team)
		{
		case Team.Blue:
			text = "[4688E7]" + text + "[-]";
			break;
		case Team.Red:
			text = "[ED2C2D]" + text + "[-]";
			break;
		}
		return text;
	}

	public static void SetActiveConsole(bool active)
	{
		if (active)
		{
			nConsole.Init();
		}
		else
		{
			nConsole.Destroy();
		}
	}

	public static string KillerStatus(DamageInfo damageInfo)
	{
		string empty = string.Empty;
		if (damageInfo.PlayerID != -1 && damageInfo.PlayerID != PhotonNetwork.player.ID)
		{
			PhotonPlayer player = PhotonPlayer.Find(damageInfo.PlayerID);
			string teamHexColor = GetTeamHexColor(player);
			string weaponName = WeaponManager.GetWeaponName(damageInfo.WeaponID);
			return teamHexColor + " [ " + weaponName + ((!damageInfo.HeadShot) ? string.Empty : "+Head") + " ] " + GetTeamHexColor(PhotonNetwork.player);
		}
		return PhotonNetwork.player.NickName + " " + Localization.Get("died");
	}

	public static string GetSpecialSymbol(string text)
	{
		switch (text)
		{
		case "AK-47":
			return 'ࠀ'.ToString();
		case "Deagle":
			return 'ࠁ'.ToString();
		case "Glock":
			return 'ࠂ'.ToString();
		case "Knife":
			return 'ࠃ'.ToString();
		case "M4A1":
			return 'ࠄ'.ToString();
		case "Magnum":
			return 'ࠅ'.ToString();
		case "Headshot":
			return '\u085f'.ToString();
		default:
			return text;
		}
	}

	public static Color GetColor(int color)
	{
		switch (color)
		{
		case 0:
			return Color.white;
		case 1:
			return Color.red;
		case 2:
			return Color.yellow;
		case 3:
			return Color.green;
		case 4:
			return Color.cyan;
		case 5:
			return Color.blue;
		case 6:
			return Color.magenta;
		case 7:
			return Color.gray;
		case 8:
			return Color.black;
		case 9:
			return Color.clear;
		default:
			return Color.white;
		}
	}

	public static string FloatToTime(float time)
	{
		int num = (int)time / 60;
		int num2 = (int)time - num * 60;
		return string.Format("{0:0}:{1:00}", num, num2);
	}

	public static string ArrayToString(int[] array)
	{
		string text = string.Empty;
		for (int i = 0; i < array.Length; i++)
		{
			text = text + array[i] + "#";
		}
		return text;
	}

	public static int[] StringToArrayInt(string text)
	{
		string[] array = text.Split("#"[0]);
		int[] array2 = new int[array.Length - 1];
		for (int i = 0; i < array.Length - 1; i++)
		{
			array2[i] = int.Parse(array[i]);
		}
		return array2;
	}

	public static Vector3 Clamp(Vector3 value, Vector3 min, Vector3 max)
	{
		value.x = Mathf.Clamp(value.x, min.x, max.x);
		value.y = Mathf.Clamp(value.y, min.y, max.y);
		value.z = Mathf.Clamp(value.z, min.z, max.z);
		return value;
	}

	public static string ColorToHex(Color32 color)
	{
		return "[" + color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2") + "]";
	}

	public static string ColorToHex(Color32 color, string text)
	{
		return "[" + color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2") + "]" + text + "[-]";
	}

	public static void Lerp(Vector3 from, Vector3 to, float t, Action<Vector3> action)
	{
		Loom.RunAsync(() =>
		{
			Vector3 vec = new Vector3(from.x + (to.x - from.x) * t, from.y + (to.y - from.y) * t, from.z + (to.z - from.z) * t);
			Loom.QueueOnMainThread(() =>
			{
				if (action != null)
				{
					action(vec);
				}
			});
		});
	}

	public static List<Vector3> GenerateBlockRoad(int maxBlocks)
	{
		List<Vector3> list = new List<Vector3>();
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < maxBlocks; i++)
		{
			int num = UnityEngine.Random.Range(1, 5);
			int num2 = UnityEngine.Random.Range(1, 5);
			int num3 = UnityEngine.Random.Range(1, 20);
			if (num >= 4)
			{
				zero.x++;
			}
			if (num <= 3)
			{
				zero.x--;
			}
			if (num2 >= 4)
			{
				zero.z++;
			}
			if (num2 <= 3)
			{
				zero.z--;
			}
			if (num3 == 1)
			{
				zero.y++;
			}
			if (num3 == 19)
			{
				zero.y--;
			}
			if (num3 >= 6)
			{
				list.Add(zero);
			}
		}
		return list;
	}

	public static string NameGenerator(int line)
	{
		string empty = string.Empty;
		string[] array = new string[6] { "a", "e", "i", "o", "u", "y" };
		string[] array2 = new string[20]
		{
			"b", "c", "d", "f", "g", "h", "j", "k", "l", "m",
			"n", "p", "q", "r", "s", "t", "v", "w", "x", "z"
		};
		bool flag = false;
		if (UnityEngine.Random.value > 0.5f)
		{
			empty = array2[UnityEngine.Random.Range(0, array2.Length)];
		}
		else
		{
			empty = array[UnityEngine.Random.Range(0, array.Length)];
			flag = true;
		}
		empty = empty.ToUpper();
		for (int i = 0; i < line - 1; i++)
		{
			if (flag)
			{
				if (UnityEngine.Random.value < 0.2f)
				{
					empty += array[UnityEngine.Random.Range(0, array.Length)];
					continue;
				}
				empty += array2[UnityEngine.Random.Range(0, array2.Length)];
				flag = false;
			}
			else if (UnityEngine.Random.value < 0.2f)
			{
				empty += array2[UnityEngine.Random.Range(0, array2.Length)];
			}
			else
			{
				empty += array[UnityEngine.Random.Range(0, array.Length)];
				flag = true;
			}
		}
		return empty;
	}

	public static string Md5(string text)
	{
		UTF8Encoding uTF8Encoding = new UTF8Encoding();
		byte[] bytes = uTF8Encoding.GetBytes(text);
		MD5CryptoServiceProvider mD5CryptoServiceProvider = new MD5CryptoServiceProvider();
		byte[] array = mD5CryptoServiceProvider.ComputeHash(bytes);
		string text2 = string.Empty;
		for (int i = 0; i < array.Length; i++)
		{
			text2 += Convert.ToString(array[i], 16).PadLeft(2, '0');
		}
		return text2.PadLeft(32, '0');
	}

	public static byte[] SerializeWeaponStickers(int weapon, int skin)
	{
		AccountWeaponStickers weaponStickers = AccountManager.GetWeaponStickers(weapon, skin);
		if (weaponStickers == null)
		{
			return new byte[0];
		}
		byte[] array = new byte[weaponStickers.StickerData.Count * 2];
		int num = 0;
		for (int i = 0; i < weaponStickers.StickerData.Count; i++)
		{
			array[num] = (byte)(int)weaponStickers.StickerData[i].Index;
			num++;
			array[num] = (byte)(int)weaponStickers.StickerData[i].StickerID;
			num++;
		}
		return array;
	}

	private static AccountWeaponStickers DeserializeWeaponStickers(byte[] bytes)
	{
		AccountWeaponStickers accountWeaponStickers = new AccountWeaponStickers();
		List<AccountWeaponStickerData> list = new List<AccountWeaponStickerData>();
		int num = 0;
		for (int i = 0; i < bytes.Length / 2; i++)
		{
			AccountWeaponStickerData accountWeaponStickerData = new AccountWeaponStickerData();
			accountWeaponStickerData.Index = bytes[num];
			num++;
			accountWeaponStickerData.StickerID = bytes[num];
			num++;
			list.Add(accountWeaponStickerData);
		}
		accountWeaponStickers.StickerData = list;
		return accountWeaponStickers;
	}

	public static bool IsNullOrWhiteSpace(string value)
	{
		if (value == null)
		{
			return true;
		}
		if (value.Length == 0)
		{
			return true;
		}
		for (int i = 0; i < value.Length; i++)
		{
			if (value[i].ToString() == " ")
			{
				return true;
			}
		}
		return false;
	}

	public static string ToTimeString(float time)
	{
		int num = (int)time / 60;
		int num2 = (int)time - num * 60;
		return string.Format("{0:0}:{1:00}", num, num2);
	}

	public static DateTime LongToDateTime(long time)
	{
		time += NTPManager.GetMilliSeconds(DateTime.Now) - NTPManager.GetMilliSeconds(DateTime.UtcNow);
		return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(time).ToLocalTime();
	}
}
