using UnityEngine;

public class vp_PlayerRespawner : vp_Respawner
{
	private vp_PlayerEventHandler m_Player;

	protected vp_PlayerEventHandler Player
	{
		get
		{
			if (m_Player == null)
			{
				m_Player = base.transform.GetComponent<vp_PlayerEventHandler>();
			}
			return m_Player;
		}
	}

	protected override void Awake()
	{
		base.Awake();
	}

	protected override void OnEnable()
	{
		if (Player != null)
		{
			Player.Register(this);
		}
		base.OnEnable();
	}

	protected override void OnDisable()
	{
		if (Player != null)
		{
			Player.Unregister(this);
		}
	}

	public override void Reset()
	{
		if (Application.isPlaying && !(Player == null))
		{
		}
	}
}
