using UnityEngine;

public class vp_SimpleCrosshair : MonoBehaviour
{
	public Texture m_ImageCrosshair;

	protected vp_FPPlayerEventHandler m_Player;

	protected virtual Texture OnValue_Crosshair
	{
		get
		{
			return m_ImageCrosshair;
		}
		set
		{
			m_ImageCrosshair = value;
		}
	}

	protected virtual void Awake()
	{
		m_Player = Object.FindObjectOfType(typeof(vp_FPPlayerEventHandler)) as vp_FPPlayerEventHandler;
	}

	protected virtual void OnEnable()
	{
		if (m_Player != null)
		{
			m_Player.Register(this);
		}
	}

	protected virtual void OnDisable()
	{
		if (m_Player != null)
		{
			m_Player.Unregister(this);
		}
	}

	private void OnGUI()
	{
		if (m_ImageCrosshair != null)
		{
			GUI.color = new Color(1f, 1f, 1f, 0.8f);
			GUI.DrawTexture(new Rect((float)Screen.width * 0.5f - (float)m_ImageCrosshair.width * 0.5f, (float)Screen.height * 0.5f - (float)m_ImageCrosshair.height * 0.5f, m_ImageCrosshair.width, m_ImageCrosshair.height), m_ImageCrosshair);
			GUI.color = Color.white;
		}
	}
}
