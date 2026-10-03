using UnityEngine;

public class vp_FPSDemoManager : vp_DemoManager
{
	public GameObject Player;

	public vp_FPController Controller;

	public vp_FPCamera Camera;

	public vp_FPWeaponHandler WeaponHandler;

	public vp_FPInput Input;

	public vp_Earthquake Earthquake;

	public vp_FPPlayerEventHandler PlayerEventHandler;

	private Vector3 m_UnFreezePosition = Vector3.zero;

	private Vector3 m_CurrentLookPoint = Vector3.zero;

	private Vector3 m_LookVelocity = Vector3.zero;

	public float LookDamping = 0.3f;

	private vp_Shooter m_CurrentShooter;

	public vp_Shooter CurrentShooter
	{
		get
		{
			if (m_CurrentShooter == null || (m_CurrentShooter != null && (!m_CurrentShooter.enabled || !vp_Utility.IsActive(m_CurrentShooter.gameObject))))
			{
				m_CurrentShooter = Player.GetComponentInChildren<vp_Shooter>();
			}
			return m_CurrentShooter;
		}
	}

	public bool DrawCrosshair
	{
		get
		{
			vp_SimpleCrosshair vp_SimpleCrosshair2 = (vp_SimpleCrosshair)Player.GetComponent(typeof(vp_SimpleCrosshair));
			if (vp_SimpleCrosshair2 == null)
			{
				return false;
			}
			return vp_SimpleCrosshair2.enabled;
		}
		set
		{
			vp_SimpleCrosshair vp_SimpleCrosshair2 = (vp_SimpleCrosshair)Player.GetComponent(typeof(vp_SimpleCrosshair));
			if (vp_SimpleCrosshair2 != null)
			{
				vp_SimpleCrosshair2.enabled = value;
			}
		}
	}

	public vp_FPSDemoManager(GameObject player)
	{
		Player = player;
		Controller = Player.GetComponent<vp_FPController>();
		Camera = Player.GetComponentInChildren<vp_FPCamera>();
		WeaponHandler = Player.GetComponentInChildren<vp_FPWeaponHandler>();
		PlayerEventHandler = (vp_FPPlayerEventHandler)Player.GetComponentInChildren(typeof(vp_FPPlayerEventHandler));
		Input = Player.GetComponent<vp_FPInput>();
		Earthquake = (vp_Earthquake)Object.FindObjectOfType(typeof(vp_Earthquake));
		if (Screen.width < 1024)
		{
			EditorPreviewSectionExpanded = false;
		}
	}

	public void Teleport(Vector3 pos, Vector2 startAngle)
	{
		Controller.SetPosition(pos);
		Camera.SetRotation(startAngle);
	}

	public void SmoothLookAt(Vector3 lookPoint)
	{
		m_CurrentLookPoint = Vector3.SmoothDamp(m_CurrentLookPoint, lookPoint, ref m_LookVelocity, LookDamping);
		Camera.transform.LookAt(m_CurrentLookPoint);
		Camera.Angle = new Vector2(Camera.transform.eulerAngles.x, Camera.transform.eulerAngles.y);
	}

	public void SnapLookAt(Vector3 lookPoint)
	{
		m_CurrentLookPoint = lookPoint;
		Camera.transform.LookAt(m_CurrentLookPoint);
		Camera.Angle = new Vector2(Camera.transform.eulerAngles.x, Camera.transform.eulerAngles.y);
	}

	public void FreezePlayer(Vector3 pos, Vector2 startAngle, bool freezeCamera)
	{
		m_UnFreezePosition = Controller.transform.position;
		Teleport(pos, startAngle);
		Controller.SetState("Freeze");
		Controller.Stop();
		if (freezeCamera)
		{
			Camera.SetState("Freeze");
		}
	}

	public void FreezePlayer(Vector3 pos, Vector2 startAngle)
	{
		FreezePlayer(pos, startAngle, false);
	}

	public void UnFreezePlayer()
	{
		Controller.transform.position = m_UnFreezePosition;
		m_UnFreezePosition = Vector3.zero;
		Controller.SetState("Freeze", false);
		Camera.SetState("Freeze", false);
	}

	public void LockControls()
	{
		Input.AllowGameplayInput = false;
		Camera.MouseSensitivity = Vector2.zero;
		if (WeaponHandler.CurrentWeapon != null)
		{
			WeaponHandler.CurrentWeapon.RotationLookSway = Vector2.zero;
		}
	}

	public void SetWeaponPreset(TextAsset weaponPreset, TextAsset shooterPreset = null, bool smoothFade = true)
	{
		if (!(WeaponHandler.CurrentWeapon == null))
		{
			WeaponHandler.CurrentWeapon.Load(weaponPreset);
			if (!smoothFade)
			{
				WeaponHandler.CurrentWeapon.SnapSprings();
				WeaponHandler.CurrentWeapon.SnapPivot();
				WeaponHandler.CurrentWeapon.SnapZoom();
			}
			WeaponHandler.CurrentWeapon.Refresh();
			if (shooterPreset != null && CurrentShooter != null)
			{
				CurrentShooter.Load(shooterPreset);
			}
			CurrentShooter.Refresh();
		}
	}

	public void RefreshDefaultState()
	{
		if (Controller != null)
		{
			Controller.RefreshDefaultState();
		}
		if (Camera != null)
		{
			Camera.RefreshDefaultState();
			if (WeaponHandler.CurrentWeapon != null)
			{
				WeaponHandler.CurrentWeapon.RefreshDefaultState();
			}
			if (CurrentShooter != null)
			{
				CurrentShooter.RefreshDefaultState();
			}
		}
	}

	public void ResetState()
	{
		if (Controller != null)
		{
			Controller.ResetState();
		}
		if (Camera != null)
		{
			Camera.ResetState();
			if (WeaponHandler.CurrentWeapon != null)
			{
				WeaponHandler.CurrentWeapon.ResetState();
			}
			if (CurrentShooter != null)
			{
				CurrentShooter.ResetState();
			}
		}
	}

	protected override void Reset()
	{
		base.Reset();
		PlayerEventHandler.RefreshActivityStates();
		WeaponHandler.SetWeapon(0);
		PlayerEventHandler.Earthquake.Stop();
		Camera.BobStepCallback = null;
		Camera.SnapSprings();
		if (WeaponHandler.CurrentWeapon != null)
		{
			WeaponHandler.CurrentWeapon.SetPivotVisible(false);
			WeaponHandler.CurrentWeapon.SnapSprings();
			vp_Layer.Set(WeaponHandler.CurrentWeapon.gameObject, 31, true);
		}
		if (Screen.width < 1024)
		{
			EditorPreviewSectionExpanded = false;
		}
		else
		{
			EditorPreviewSectionExpanded = true;
		}
		if (m_UnFreezePosition != Vector3.zero)
		{
			UnFreezePlayer();
		}
	}

	public void ForceCameraShake(float speed, Vector3 amplitude)
	{
		Camera.ShakeSpeed = speed;
		Camera.ShakeAmplitude = amplitude;
	}

	public void ForceCameraShake()
	{
		ForceCameraShake(0.0727273f, new Vector3(-10f, 10f, 0f));
	}
}
