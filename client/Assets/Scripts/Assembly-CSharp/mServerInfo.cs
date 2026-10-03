using UnityEngine;

public class mServerInfo : MonoBehaviour
{
	public UILabel ServerNameLabel;

	public UILabel ModeLabel;

	public UILabel MapNameLabel;

	public UILabel PlayersLabel;

	public GameObject Password;

	private RoomInfo m_RoomInfo;

	public void SetData(RoomInfo info)
	{
		m_RoomInfo = info;
		ServerNameLabel.text = info.Name;
		if (info.GetGameMode() == GameMode.Only)
		{
			ModeLabel.text = Localization.Get(info.GetGameMode().ToString()) + " (" + WeaponManager.GetWeaponName(info.GetOnlyWeapon()) + ")";
		}
		else
		{
			ModeLabel.text = Localization.Get(info.GetGameMode().ToString());
		}
		MapNameLabel.text = info.GetSceneName();
		PlayersLabel.text = info.PlayerCount + "/" + info.MaxPlayers;
		if (string.IsNullOrEmpty(info.GetPassword()))
		{
			Password.SetActive(false);
		}
		else
		{
			Password.SetActive(true);
		}
	}

	public void OnClick()
	{
		if (m_RoomInfo.PlayerCount != m_RoomInfo.MaxPlayers && !UIKick.KickedServers.Contains(m_RoomInfo.Name))
		{
			if (string.IsNullOrEmpty(m_RoomInfo.GetPassword()))
			{
				mPhotonSettings.OnJoinServer(m_RoomInfo);
			}
			else
			{
				mPopUp.ShowInput(string.Empty, Localization.Get("Password"), 4, UIInput.KeyboardType.NumberPad, null, null, Localization.Get("Back"), OnBack, "Ok", OnNext);
			}
		}
	}

	private void OnBack()
	{
		mPopUp.HideAll("ServerList", false);
	}

	private void OnNext()
	{
		if (m_RoomInfo.GetPassword() == mPopUp.GetInputText())
		{
			mPhotonSettings.OnJoinServer(m_RoomInfo);
		}
		else
		{
			UIToast.Show(Localization.Get("Password is incorrect"));
		}
	}
}
