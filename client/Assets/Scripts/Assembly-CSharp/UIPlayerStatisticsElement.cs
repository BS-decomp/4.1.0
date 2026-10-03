using UnityEngine;

public class UIPlayerStatisticsElement : MonoBehaviour
{
	public UILabel PlayerNameLabel;

	public UILabel LevelLabel;

	public UILabel ClanTagLabel;

	public UILabel KillsLabel;

	public UILabel DeathsLabel;

	public UILabel PingLabel;

	public UISprite Background;

	public UIWidget Widget;

	private Color32 AdminColor = new Color32(60, 181, 232, byte.MaxValue);

	private Color32 LocalPlayerColor = Color.green;

	public PhotonPlayer PlayerInfo;

	private int Timer;

	private Transform CacheTransform;

	public Transform Transform
	{
		get
		{
			if (CacheTransform == null)
			{
				CacheTransform = base.transform;
			}
			return CacheTransform;
		}
	}

	private void OnDisable()
	{
		TimerManager.Cancel(Timer);
	}

	public void SetData(PhotonPlayer playerInfo)
	{
		PlayerInfo = playerInfo;
		string text = playerInfo.GetClan().ToUpper();
		if (string.IsNullOrEmpty(text))
		{
			ClanTagLabel.alpha = 0f;
		}
		else
		{
			ClanTagLabel.alpha = 1f;
		}
		ClanTagLabel.text = text;
		PlayerNameLabel.text = PlayerInfo.NickName;
		LevelLabel.text = playerInfo.GetLevel().ToString();
		KillsLabel.text = PlayerInfo.GetKills().ToString();
		DeathsLabel.text = PlayerInfo.GetDeaths().ToString();
		PingLabel.text = PlayerInfo.GetPing().ToString();
		base.name = PlayerNameLabel.text;
		if (playerInfo.GetDead())
		{
			Widget.alpha = 0.5f;
		}
		else
		{
			Widget.alpha = 1f;
		}
		Widget.Update();
		if (playerInfo.IsLocal)
		{
			PlayerNameLabel.color = LocalPlayerColor;
			LevelLabel.color = LocalPlayerColor;
			KillsLabel.color = LocalPlayerColor;
			DeathsLabel.color = LocalPlayerColor;
			PingLabel.color = LocalPlayerColor;
		}
		else if (playerInfo.IsMasterClient)
		{
			PlayerNameLabel.color = AdminColor;
			LevelLabel.color = AdminColor;
			KillsLabel.color = AdminColor;
			DeathsLabel.color = AdminColor;
			PingLabel.color = AdminColor;
		}
		else
		{
			PlayerNameLabel.color = Color.white;
			LevelLabel.color = Color.white;
			KillsLabel.color = Color.white;
			DeathsLabel.color = Color.white;
			PingLabel.color = Color.white;
		}
		if (!TimerManager.IsActive(Timer))
		{
			Timer = TimerManager.In(3f, -1, 3f, UpdateData);
		}
	}

	private void UpdateData()
	{
		SetData(PlayerInfo);
	}
}
