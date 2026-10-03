using UnityEngine;

public class vp_Earthquake : MonoBehaviour
{
	protected Vector3 m_EarthQuakeForce = default(Vector3);

	protected float m_Endtime;

	protected Vector2 m_Magnitude = Vector2.zero;

	protected vp_EventHandler EventHandler;

	private vp_FPPlayerEventHandler m_Player;

	private vp_FPPlayerEventHandler Player
	{
		get
		{
			if (m_Player == null && EventHandler != null)
			{
				m_Player = (vp_FPPlayerEventHandler)EventHandler;
			}
			return m_Player;
		}
	}

	protected virtual Vector3 OnValue_EarthQuakeForce
	{
		get
		{
			return m_EarthQuakeForce;
		}
		set
		{
			m_EarthQuakeForce = value;
		}
	}

	protected virtual void Awake()
	{
		EventHandler = (vp_EventHandler)Object.FindObjectOfType(typeof(vp_EventHandler));
	}

	protected virtual void OnEnable()
	{
		if (EventHandler != null)
		{
			EventHandler.Register(this);
		}
	}

	protected virtual void OnDisable()
	{
		if (EventHandler != null)
		{
			EventHandler.Unregister(this);
		}
	}

	protected void FixedUpdate()
	{
		if (Time.timeScale != 0f)
		{
			UpdateEarthQuake();
		}
	}

	protected void UpdateEarthQuake()
	{
		if (!Player.Earthquake.Active)
		{
			m_EarthQuakeForce = Vector3.zero;
			return;
		}
		m_EarthQuakeForce = Vector3.Scale(vp_SmoothRandom.GetVector3Centered(1f), m_Magnitude.x * (Vector3.right + Vector3.forward) * Mathf.Min(m_Endtime - Time.time, 1f) * Time.timeScale);
		m_EarthQuakeForce.y = 0f;
		if (Random.value < 0.3f * Time.timeScale)
		{
			m_EarthQuakeForce.y = Random.Range(0f, m_Magnitude.y * 0.35f) * Mathf.Min(m_Endtime - Time.time, 1f);
		}
	}

	protected virtual void OnStart_Earthquake()
	{
		Vector3 vector = (Vector3)Player.Earthquake.Argument;
		m_Magnitude.x = vector.x;
		m_Magnitude.y = vector.y;
		m_Endtime = Time.time + vector.z;
		Player.Earthquake.AutoDuration = vector.z;
	}

	protected virtual void OnMessage_BombShake(float impact)
	{
		Player.Earthquake.TryStart(new Vector3(impact * 0.5f, impact * 0.5f, 1f));
	}
}
