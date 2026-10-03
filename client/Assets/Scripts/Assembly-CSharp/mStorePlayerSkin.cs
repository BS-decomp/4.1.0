using System.Collections.Generic;
using Photon;
using UnityEngine;

public class mStorePlayerSkin : PunBehaviour
{
	[Header("Player Skin")]
	public GameObject PlayerSkinPanel;

	public GameObject PlayerSkinBackground;

	public GameObject SelectSkinButton;

	public UILabel SelectSkinButtonLabel;

	public GameObject BuySkinButton;

	public UILabel BuySkinButtonLabel;

	public UITexture BuySkinButtonTexture;

	public UISprite ChangeTeamButton;

	[Header("Others")]
	public Texture2D MoneyTexture;

	public Texture2D GoldTexture;

	public GameObject InAppPanel;

	[Header("Easter Eggs")]
	public bool[] EggsSequence;

	private int Eggs;

	private int Skin;

	private Team SkinTeam = Team.Blue;

	private PlayerStoreSkinData SkinData;

	private List<string> SkinsList = new List<string>();

	private bool Active;

	private void Awake()
	{
		PhotonClassesManager.Add(this);
	}

	private void Start()
	{
		UIEventListener uIEventListener = UIEventListener.Get(PlayerSkinBackground);
		uIEventListener.onDrag = RotateWeapon;
	}

	public override void OnDisconnectedFromPhoton()
	{
		Close();
	}

	private void RotateWeapon(GameObject go, Vector2 drag)
	{
		mPlayerCamera.Rotate(drag);
	}

	public void Show()
	{
		Active = true;
		mPanelManager.SetActivePlayerData(false);
		Skin = 0;
		GetSkinsList();
		UpdateSkin();
	}

	public void Close()
	{
		if (Active)
		{
			mPanelManager.SetActivePlayerData(true);
			mPlayerCamera.Close();
			Active = false;
		}
	}

	private void UpdateSkin()
	{
		GetPlayerSkinData();
		mPlayerCamera.Show();
		mPlayerCamera.ResetRotateX();
		mPlayerCamera.SetSkin(SkinTeam, SkinData.ID);
		UpdateButtons();
	}

	private void UpdateButtons()
	{
		bool flag = AccountManager.GetPlayerSkin(SkinData.ID);
		if ((int)SkinData.ID == 0)
		{
			flag = true;
		}
		if (flag)
		{
			SelectSkinButton.SetActive(true);
			BuySkinButton.SetActive(false);
			if (AccountManager.GetPlayerSkinSelected() == (int)SkinData.ID)
			{
				SelectSkinButtonLabel.text = Localization.Get("Selected");
				SelectSkinButton.GetComponent<UISprite>().alpha = 0.5f;
			}
			else
			{
				SelectSkinButtonLabel.text = Localization.Get("Select");
				SelectSkinButton.GetComponent<UISprite>().alpha = 1f;
			}
		}
		else
		{
			SelectSkinButton.SetActive(false);
			BuySkinButton.SetActive(true);
			BuySkinButtonLabel.text = SkinData.Price.ToString("n0");
			BuySkinButtonTexture.mainTexture = ((SkinData.Currency != GameCurrency.Money) ? GoldTexture : MoneyTexture);
		}
	}

	public void NextSkin()
	{
		Skin++;
		if (Skin >= SkinsList.Count)
		{
			Skin = 0;
		}
		UpdateSkin();
		UpdateEasterEggs(true);
	}

	public void LastSkin()
	{
		Skin--;
		if (Skin <= -1)
		{
			Skin = SkinsList.Count - 1;
		}
		UpdateSkin();
		UpdateEasterEggs(false);
	}

	public void ChangeTeam()
	{
		if (SkinTeam == Team.Blue)
		{
			SkinTeam = Team.Red;
			ChangeTeamButton.color = new Color32(237, 44, 45, byte.MaxValue);
		}
		else
		{
			SkinTeam = Team.Blue;
			ChangeTeamButton.color = new Color32(70, 136, 231, byte.MaxValue);
		}
		UpdateSkin();
	}

	public void SelectSkin()
	{
		if (AccountManager.GetPlayerSkinSelected() != (int)SkinData.ID)
		{
			AccountManager.SetPlayerSkinSelected(SkinData.ID);
			UpdateButtons();
		}
	}

	public void BuySkin()
	{
		if (!AccountManager.isConnect)
		{
			UIToast.Show(Localization.Get("Connection account"));
			return;
		}
		int num = ((SkinData.Currency != GameCurrency.Gold) ? AccountManager.GetMoney() : AccountManager.GetGold());
		if ((int)SkinData.Price > num)
		{
			InAppPanel.SetActive(true);
			UIToast.Show(Localization.Get("Not enough money"));
			return;
		}
		if (SkinData.Currency == GameCurrency.Gold)
		{
			AccountManager.SetGold(num - (int)SkinData.Price);
		}
		else
		{
			AccountManager.SetMoney(num - (int)SkinData.Price);
		}
		AccountManager.SetPlayerSkin(SkinData.ID);
		AccountManager.SetPlayerSkinSelected(SkinData.ID);
		UpdateButtons();
		EventManager.Dispatch("AccountUpdate");
	}

	private void GetSkinsList()
	{
		SkinsList.Clear();
		for (int i = 0; i < GameSettings.instance.PlayerStoreSkins.Count; i++)
		{
			SkinsList.Add(GameSettings.instance.PlayerStoreSkins[i].Name);
		}
	}

	private void GetPlayerSkinData()
	{
		for (int i = 0; i < GameSettings.instance.PlayerStoreSkins.Count; i++)
		{
			if (SkinsList[Skin] == GameSettings.instance.PlayerStoreSkins[i].Name)
			{
				SkinData = GameSettings.instance.PlayerStoreSkins[i];
				break;
			}
		}
	}

	private void UpdateEasterEggs(bool right)
	{
		if (EggsSequence[Eggs] == right)
		{
			Eggs++;
			if (Eggs > EggsSequence.Length - 1)
			{
				mPlayerCamera.EasterEggs();
				Eggs = 0;
			}
		}
		else
		{
			Eggs = 0;
		}
	}
}
