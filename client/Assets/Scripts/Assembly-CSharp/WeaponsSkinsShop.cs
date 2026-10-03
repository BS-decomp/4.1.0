using System;
using System.IO;
using FreeJSON;
using Lean;
using UnityEngine;

public class WeaponsSkinsShop : MonoBehaviour
{
	[Serializable]
	public class WeaponData
	{
		[SelectedWeapon]
		public string Weapon;

		public GameObject Model;

		public bool isCustom;

		public string CustomName;

		public Texture2D CustomTexture;
	}

	public Transform Point;

	public float RotateSpeed = 200f;

	public GameObject DragBackground;

	public Material WeaponMaterial;

	public UIInput WeaponNameInput;

	public WeaponData[] Weapons;

	private int SelectWeapon;

	private int SelectTexture;

	private void Start()
	{
		UIEventListener uIEventListener = UIEventListener.Get(DragBackground);
		uIEventListener.onDrag = Rotate;
		RotateSpeed = Mathf.Sqrt(RotateSpeed) / Mathf.Sqrt(Screen.dpi);
		OnSelectWeapon();
		for (int i = 0; i < Weapons.Length; i++)
		{
			if (Weapons[i].isCustom)
			{
				Weapons[i].Weapon = Weapons[i].CustomName;
			}
		}
	}

	private void OnDisable()
	{
		WeaponMaterial.mainTexture = null;
	}

	private void Update()
	{
		LeanTouch.ScaleObject(Point, Vector3.one * 0.5f, Vector3.one * 2f, LeanTouch.PinchScale);
		if (Input.GetKeyDown(KeyCode.F))
		{
			SendTextureToServer();
		}
	}

	public void OnExit()
	{
		LevelManager.LoadLevel("Menu");
	}

	private void OnSelectWeapon()
	{
		DeactiveWeaponAll();
		Weapons[SelectWeapon].Model.SetActive(true);
		OnUpdateTexture();
		Point.localEulerAngles = Vector3.zero;
		Point.localScale = Vector3.one;
	}

	private void DeactiveWeaponAll()
	{
		for (int i = 0; i < Weapons.Length; i++)
		{
			Weapons[i].Model.SetActive(false);
		}
	}

	public void OnNextWeapon()
	{
		SelectWeapon++;
		if (SelectWeapon >= Weapons.Length)
		{
			SelectWeapon = 0;
		}
		OnSelectWeapon();
	}

	public void OnLastWeapon()
	{
		SelectWeapon--;
		if (SelectWeapon < 0)
		{
			SelectWeapon = Weapons.Length - 1;
		}
		OnSelectWeapon();
	}

	private void OnUpdateTexture()
	{
		Texture2D targetTexture = GetTargetTexture();
		WeaponMaterial.mainTexture = targetTexture;
	}

	public void OnNextTexture()
	{
		SelectTexture++;
		OnUpdateTexture();
	}

	public void OnLastTexture()
	{
		SelectTexture--;
		OnUpdateTexture();
	}

	public void Rotate(GameObject go, Vector2 rotate)
	{
		if (LeanTouch.PinchScale == 1f)
		{
			Point.Rotate(new Vector2(rotate.y * RotateSpeed, (0f - rotate.x) * RotateSpeed), Space.World);
		}
	}

	public void SendTextureToServer()
	{
		UIToast.Show("Sent to the server");
		byte[] inArray = (WeaponMaterial.mainTexture as Texture2D).EncodeToPNG();
		string base64 = Convert.ToBase64String(inArray);
		string md5 = Utils.Md5(base64);
		Firebase firebase = new Firebase();
		firebase.Child("Workshop").Child("Preview").Child(Weapons[SelectWeapon].Weapon)
			.Child(md5)
			.GetValue((string result) =>
			{
				if (result == "null" || result == "{}")
				{
					JsonObject jsonObject = new JsonObject();
					jsonObject.Add("skin", base64);
					jsonObject.Add("player", (string)AccountManager.AccountID);
					jsonObject.Add("time", JsonObject.Parse(Firebase.GetTimeStamp()));
					jsonObject.Add("name", WeaponNameInput.value);
					firebase.Child("Workshop").Child("Preview").Child(Weapons[SelectWeapon].Weapon)
						.Child(md5)
						.UpdateValue(jsonObject.ToString(), (string text) =>
						{
							UIToast.Show("Complete");
						}, (string error) =>
						{
							UIToast.Show("Error: " + error);
						});
				}
				else
				{
					UIToast.Show("Error: Skin already exists");
				}
			}, (string error) =>
			{
				UIToast.Show("Error: " + error);
			});
	}

	private bool CheckDirectory()
	{
		return CheckDirectory(SelectWeapon);
	}

	private bool CheckDirectory(int index)
	{
		string path = GetPath();
		if (!Directory.Exists(path + "/" + Weapons[index].Weapon))
		{
			Directory.CreateDirectory(path + "/" + Weapons[index].Weapon);
			File.WriteAllBytes(bytes: (!Weapons[index].isCustom) ? Weapons[index].CustomTexture.EncodeToPNG() : Weapons[index].CustomTexture.EncodeToPNG(), path: path + "/" + Weapons[index].Weapon + "/Default.png");
			return false;
		}
		if (!File.Exists(path + "/" + Weapons[index].Weapon + "/Default.png"))
		{
			File.WriteAllBytes(bytes: (!Weapons[index].isCustom) ? Weapons[index].CustomTexture.EncodeToPNG() : Weapons[index].CustomTexture.EncodeToPNG(), path: path + "/" + Weapons[index].Weapon + "/Default.png");
			return false;
		}
		return true;
	}

	private Texture2D GetTargetTexture()
	{
		CheckDirectory();
		string path = GetPath();
		string[] files = Directory.GetFiles(path + "/" + Weapons[SelectWeapon].Weapon, "*png");
		if (SelectTexture >= files.Length)
		{
			SelectTexture = 0;
		}
		if (SelectTexture < 0)
		{
			SelectTexture = files.Length - 1;
		}
		Texture2D texture2D = new Texture2D(5, 5);
		texture2D.LoadImage(File.ReadAllBytes(files[SelectTexture]));
		texture2D.filterMode = FilterMode.Point;
		UIToast.Show(Path.GetFileNameWithoutExtension(files[SelectTexture]));
		return texture2D;
	}

	private string GetPath()
	{
		string result = Application.dataPath;
		if (Application.isEditor)
		{
			result = Directory.GetParent(Application.dataPath).FullName + "/WeaponSkins/";
		}
		if (Application.platform == RuntimePlatform.Android)
		{
			try
			{
				string text = new AndroidJavaClass("android.os.Environment").CallStatic<AndroidJavaObject>("getExternalStorageDirectory", new object[0]).Call<string>("getAbsolutePath", new object[0]);
				text += "/Android/data/";
				text += new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity").Call<string>("getPackageName", new object[0]);
				if (Directory.Exists(text))
				{
					if (Directory.Exists(text + "/weaponskins"))
					{
						Directory.CreateDirectory(text + "/weaponskins");
					}
					result = text + "/weaponskins/";
				}
				else
				{
					result = Application.dataPath;
				}
			}
			catch
			{
			}
		}
		return result;
	}
}
