using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class vp_SimpleHUD : MonoBehaviour
{
	public Texture DamageFlashTexture;

	public bool ShowHUD = true;

	private Color m_MessageColor = new Color(2f, 2f, 0f, 2f);

	private Color m_InvisibleColor = new Color(1f, 1f, 0f, 0f);

	private Color m_DamageFlashColor = new Color(0.8f, 0f, 0f, 0f);

	private Color m_DamageFlashInvisibleColor = new Color(1f, 0f, 0f, 0f);

	private string m_PickupMessage = string.Empty;

	protected static GUIStyle m_MessageStyle;

	private vp_FPPlayerEventHandler m_Player;

	public static GUIStyle MessageStyle
	{
		get
		{
			if (m_MessageStyle == null)
			{
				m_MessageStyle = new GUIStyle("Label");
				m_MessageStyle.alignment = TextAnchor.MiddleCenter;
			}
			return m_MessageStyle;
		}
	}

	private void Awake()
	{
		m_Player = base.transform.GetComponent<vp_FPPlayerEventHandler>();
	}

	protected virtual void OnEnable()
	{
		if (m_Player != null)
		{
			m_Player.Register(this);
		}
		vp_GlobalEvent<string>.Register("HUDText", OnMessage_HUDText);
	}

	protected virtual void OnDisable()
	{
		if (m_Player != null)
		{
			m_Player.Unregister(this);
		}
		vp_GlobalEvent<string>.Unregister("HUDText", OnMessage_HUDText);
	}

	protected virtual void OnGUI()
	{
		if (ShowHUD)
		{
			GUI.Box(new Rect(Screen.width - 120, Screen.height - 30, 110f, 22f), "Ammo: " + m_Player.CurrentWeaponAmmoCount.Get() + " / " + m_Player.CurrentWeaponClipCount.Get());
			if (m_PickupMessage != null && m_MessageColor.a > 0.01f)
			{
				m_MessageColor = Color.Lerp(m_MessageColor, m_InvisibleColor, Time.deltaTime * 0.4f);
				GUI.color = m_MessageColor;
				GUI.Box(new Rect(200f, 150f, Screen.width - 400, Screen.height - 400), m_PickupMessage, MessageStyle);
				GUI.color = Color.white;
			}
			if (DamageFlashTexture != null && m_DamageFlashColor.a > 0.01f)
			{
				m_DamageFlashColor = Color.Lerp(m_DamageFlashColor, m_DamageFlashInvisibleColor, Time.deltaTime * 0.4f);
				GUI.color = m_DamageFlashColor;
				GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), DamageFlashTexture);
				GUI.color = Color.white;
			}
		}
	}

	protected virtual void OnMessage_HUDText(string message)
	{
		m_MessageColor = Color.white;
		m_PickupMessage = message;
	}

	protected virtual void OnMessage_HUDDamageFlash(float intensity)
	{
		if (!(DamageFlashTexture == null))
		{
			if (intensity == 0f)
			{
				m_DamageFlashColor.a = 0f;
			}
			else
			{
				m_DamageFlashColor.a += intensity;
			}
		}
	}
}
