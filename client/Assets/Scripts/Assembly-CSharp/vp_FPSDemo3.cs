using UnityEngine;

public class vp_FPSDemo3 : MonoBehaviour
{
	private vp_FPCamera m_FPSCamera;

	public GameObject PlayerGameObject;

	private vp_FPSDemoManager m_Demo;

	public Texture ImageLeftArrow;

	public Texture ImageRightArrow;

	public Texture ImageCheckmark;

	public Texture ImagePresetDialogs;

	public Texture ImageShooter;

	public Texture ImageAllParams;

	private Vector3 m_StartPos = new Vector3(113f, 106f, -87f);

	private Vector3 m_OverviewPos = new Vector3(113f, 106f, -87f);

	private Vector3 m_OutroPos = new Vector3(135f, 105.8f, -70.7f);

	private Vector2 m_StartAngle = new Vector2(13f, 153.5f);

	private Vector2 m_OverviewAngle = new Vector2(13f, 153.5f);

	private Vector2 m_OutroAngle = new Vector2(-19.3f, 241.7f);

	private float m_OutroStartTime;

	private bool m_LoadingNextLevel;

	private void Start()
	{
		m_FPSCamera = (vp_FPCamera)Object.FindObjectOfType(typeof(vp_FPCamera));
		m_Demo = new vp_FPSDemoManager(PlayerGameObject);
		m_Demo.CurrentFullScreenFadeTime = Time.time;
		m_Demo.DrawCrosshair = false;
		m_Demo.FadeGUIOnCursorLock = false;
		m_Demo.Input.MouseCursorZones = new Rect[2];
		m_Demo.Input.MouseCursorZones[0] = new Rect((float)Screen.width * 0.5f - 370f, 40f, 80f, 80f);
		m_Demo.Input.MouseCursorZones[1] = new Rect((float)Screen.width * 0.5f + 290f, 40f, 80f, 80f);
		Screen.lockCursor = false;
	}

	private void Update()
	{
		m_Demo.Update();
		if (Vector3.Distance(PlayerGameObject.transform.position, m_StartPos) > 100f)
		{
			m_Demo.Teleport(m_StartPos, m_StartAngle);
		}
	}

	private void DemoIntro()
	{
		if (m_Demo.FirstFrame)
		{
			m_Demo.FirstFrame = false;
			m_Demo.DrawCrosshair = false;
			m_Demo.FreezePlayer(m_OverviewPos, m_OverviewAngle, true);
			m_Demo.Input.ForceCursor = true;
		}
		m_Demo.DrawBoxes("welcome", "Featuring the SMOOTHEST CONTROLS and the most POWERFUL FPS CAMERA\navailable for Unity, Ultimate FPS is an awesome script pack for achieving that special\n 'AAA FPS' feeling. This demo will walk you through some of its core features ...\n", null, ImageRightArrow);
		m_Demo.ForceCameraShake();
	}

	private void DemoGameplay()
	{
		if (m_Demo.FirstFrame)
		{
			m_Demo.FirstFrame = false;
			m_Demo.DrawCrosshair = true;
			m_Demo.UnFreezePlayer();
			m_Demo.Teleport(m_StartPos, m_StartAngle);
			Screen.lockCursor = true;
			m_Demo.Input.ForceCursor = false;
		}
		m_Demo.DrawBoxes("part i: some examples", "This level has some basic gameplay features.\n• Press SHIFT to SPRINT, C to CROUCH, and the RIGHT MOUSE BUTTON to AIM.\n• To SWITCH WEAPONS, press Q, E or 1-3.\n• Press R to RELOAD, and F to INTERACT.", ImageLeftArrow, ImageRightArrow, () =>
		{
			m_Demo.LoadLevel(1);
		});
		if (m_Demo.ShowGUI && Screen.lockCursor && !m_LoadingNextLevel && !m_Demo.ClosingDown)
		{
			GUI.color = new Color(1f, 1f, 1f, (!m_Demo.ClosingDown) ? 1f : m_Demo.GlobalAlpha);
			GUI.Label(new Rect(Screen.width / 2 - 200, 140f, 400f, 20f), "(Press ENTER to reenable mouse cursor.)", m_Demo.CenterStyle);
			GUI.color = new Color(1f, 1f, 1f, 1f * m_Demo.GlobalAlpha);
		}
	}

	private void DemoOutro()
	{
		if (m_Demo.FirstFrame)
		{
			m_Demo.FirstFrame = false;
			m_Demo.DrawCrosshair = false;
			m_Demo.FreezePlayer(m_OutroPos, m_OutroAngle, true);
			m_Demo.Input.ForceCursor = true;
			m_OutroStartTime = Time.time;
		}
		m_FPSCamera.Angle = new Vector2(m_OutroAngle.x, m_OutroAngle.y + Mathf.Cos((Time.time - m_OutroStartTime + 50f) * 0.03f) * 20f);
		m_Demo.DrawBoxes("putting it all together", "Included in the package is full, well commented C# source code, an in-depth 70-page MANUAL in PDF format, a game-ready FPS PLAYER prefab along with all the scripts and content used in this demo. A FANTASTIC starting point (or upgrade) for any FPS project.\nBest part? It can be yours in a minute. GET IT NOW on visionpunk.com!", ImageLeftArrow, ImageCheckmark, () =>
		{
			m_Demo.LoadLevel(0);
		});
		m_Demo.DrawImage(ImageAllParams);
	}

	private void OnGUI()
	{
		m_Demo.OnGUI();
		switch (m_Demo.CurrentScreen)
		{
		case 1:
			DemoIntro();
			break;
		case 2:
			DemoGameplay();
			break;
		case 3:
			DemoOutro();
			break;
		}
	}
}
