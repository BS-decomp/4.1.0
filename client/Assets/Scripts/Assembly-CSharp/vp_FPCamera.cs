using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(AudioListener))]
public class vp_FPCamera : vp_Component
{
	public delegate void BobStepDelegate();

	public vp_FPController FPController;

	public Vector2 MouseSensitivity = new Vector2(5f, 5f);

	public int MouseSmoothSteps = 10;

	public float MouseSmoothWeight = 0.5f;

	public bool MouseAcceleration;

	public float MouseAccelerationThreshold = 0.4f;

	protected Vector2 m_MouseMove = Vector2.zero;

	protected List<Vector2> m_MouseSmoothBuffer = new List<Vector2>();

	public float RenderingFieldOfView = 60f;

	public float RenderingZoomDamping = 0.2f;

	protected float m_FinalZoomTime;

	public Vector3 PositionOffset = new Vector3(0f, 1.75f, 0.1f);

	public float PositionGroundLimit = 0.1f;

	public float PositionSpringStiffness = 0.01f;

	public float PositionSpringDamping = 0.25f;

	public float PositionSpring2Stiffness = 0.95f;

	public float PositionSpring2Damping = 0.25f;

	public float PositionKneeling = 0.025f;

	public int PositionKneelingSoftness = 1;

	public float PositionEarthQuakeFactor = 1f;

	protected vp_Spring m_PositionSpring;

	protected vp_Spring m_PositionSpring2;

	protected bool m_DrawCameraCollisionDebugLine;

	public Vector2 RotationPitchLimit = new Vector2(90f, -90f);

	public Vector2 RotationYawLimit = new Vector2(-360f, 360f);

	public float RotationSpringStiffness = 0.01f;

	public float RotationSpringDamping = 0.25f;

	public float RotationKneeling = 0.025f;

	public int RotationKneelingSoftness = 1;

	public float RotationStrafeRoll = 0.01f;

	public float RotationEarthQuakeFactor;

	protected float m_Pitch;

	protected float m_Yaw;

	protected vp_Spring m_RotationSpring;

	protected Vector2 m_InitialRotation = Vector2.zero;

	public float ShakeSpeed;

	public Vector3 ShakeAmplitude = new Vector3(10f, 10f, 0f);

	protected Vector3 m_Shake = Vector3.zero;

	public Vector4 BobRate = new Vector4(0f, 1.4f, 0f, 0.7f);

	public Vector4 BobAmplitude = new Vector4(0f, 0.25f, 0f, 0.5f);

	public float BobInputVelocityScale = 1f;

	public float BobMaxInputVelocity = 100f;

	public bool BobRequireGroundContact = true;

	protected float m_LastBobSpeed;

	protected Vector4 m_CurrentBobAmp = Vector4.zero;

	protected Vector4 m_CurrentBobVal = Vector4.zero;

	protected float m_BobSpeed;

	public BobStepDelegate BobStepCallback;

	public float BobStepThreshold = 10f;

	protected float m_LastUpBob;

	protected bool m_BobWasElevating;

	protected Vector3 m_CameraCollisionStartPos = Vector3.zero;

	protected Vector3 m_CameraCollisionEndPos = Vector3.zero;

	protected RaycastHit m_CameraHit;

	private vp_FPPlayerEventHandler m_Player;

	public bool DrawCameraCollisionDebugLine
	{
		get
		{
			return m_DrawCameraCollisionDebugLine;
		}
		set
		{
			m_DrawCameraCollisionDebugLine = value;
		}
	}

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

	public Vector2 Angle
	{
		get
		{
			return new Vector2(m_Pitch, m_Yaw);
		}
		set
		{
			Pitch = value.x;
			Yaw = value.y;
		}
	}

	public Vector3 Forward
	{
		get
		{
			return m_Transform.forward;
		}
	}

	public float Pitch
	{
		get
		{
			return m_Pitch;
		}
		set
		{
			if (value > 90f)
			{
				value -= 360f;
			}
			m_Pitch = value;
		}
	}

	public float Yaw
	{
		get
		{
			return m_Yaw;
		}
		set
		{
			m_InitialRotation = Vector2.zero;
			m_Yaw = value;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		if (FPController == null)
		{
			FPController = base.Root.GetComponent<vp_FPController>();
		}
		m_InitialRotation = new Vector2(base.Transform.eulerAngles.y, base.Transform.eulerAngles.x);
		base.Parent.gameObject.layer = 30;
		foreach (Transform item in base.Parent)
		{
			item.gameObject.layer = 30;
		}
		base.camera.cullingMask &= 1073741823;
		base.camera.depth = 0f;
		Camera camera = null;
		foreach (Transform item2 in base.Transform)
		{
			camera = (Camera)item2.GetComponent(typeof(Camera));
			if (camera != null)
			{
				camera.transform.localPosition = Vector3.zero;
				camera.transform.localEulerAngles = Vector3.zero;
				camera.clearFlags = CameraClearFlags.Depth;
				camera.cullingMask = int.MinValue;
				camera.depth = 1f;
				camera.farClipPlane = 100f;
				camera.nearClipPlane = 0.01f;
				camera.fieldOfView = 60f;
				break;
			}
		}
		m_PositionSpring = new vp_Spring(base.Transform, vp_Spring.UpdateMode.Position, false);
		m_PositionSpring.MinVelocity = 1E-05f;
		m_PositionSpring.RestState = PositionOffset;
		m_PositionSpring2 = new vp_Spring(base.Transform, vp_Spring.UpdateMode.PositionAdditive, false);
		m_PositionSpring2.MinVelocity = 1E-05f;
		m_RotationSpring = new vp_Spring(base.Transform, vp_Spring.UpdateMode.RotationAdditive, false);
		m_RotationSpring.MinVelocity = 1E-05f;
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		vp_FPController fPController = FPController;
		fPController.FallImpactEvent = (Action<float>)Delegate.Combine(fPController.FallImpactEvent, new Action<float>(OnMessage_FallImpact));
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		vp_FPController fPController = FPController;
		fPController.FallImpactEvent = (Action<float>)Delegate.Remove(fPController.FallImpactEvent, new Action<float>(OnMessage_FallImpact));
	}

	protected override void Start()
	{
		base.Start();
		Refresh();
		SnapSprings();
		SnapZoom();
	}

	protected override void Init()
	{
		base.Init();
	}

	protected override void FixedUpdate()
	{
		base.FixedUpdate();
		UpdateSwaying();
		UpdateBob();
		UpdateEarthQuake();
		UpdateShakes();
		UpdateSprings();
	}

	protected override void LateUpdate()
	{
		base.LateUpdate();
		m_Transform.position = FPController.SmoothPosition;
		m_Transform.localPosition += m_PositionSpring.State + m_PositionSpring2.State;
		DoCameraCollision();
		Quaternion quaternion = Quaternion.AngleAxis(m_Yaw + m_InitialRotation.x, Vector3.up);
		Quaternion quaternion2 = Quaternion.AngleAxis(0f, Vector3.left);
		base.Parent.rotation = vp_MathUtility.NaNSafeQuaternion(quaternion * quaternion2, base.Parent.rotation);
		quaternion2 = Quaternion.AngleAxis(0f - m_Pitch - m_InitialRotation.y, Vector3.left);
		base.Transform.rotation = vp_MathUtility.NaNSafeQuaternion(quaternion * quaternion2, base.Transform.rotation);
		base.Transform.localEulerAngles += vp_MathUtility.NaNSafeVector3(Vector3.forward * m_RotationSpring.State.z);
	}

	protected virtual void DoCameraCollision()
	{
		m_CameraCollisionStartPos = FPController.Transform.TransformPoint(0f, PositionOffset.y, 0f);
		m_CameraCollisionEndPos = base.Transform.position + (base.Transform.position - m_CameraCollisionStartPos).normalized * FPController.mCharacterController.radius;
		if (Physics.Linecast(m_CameraCollisionStartPos, m_CameraCollisionEndPos, out m_CameraHit, -1749041173) && !m_CameraHit.collider.isTrigger)
		{
			base.Transform.position = m_CameraHit.point - (m_CameraHit.point - m_CameraCollisionStartPos).normalized * FPController.mCharacterController.radius;
		}
		if (base.Transform.localPosition.y < PositionGroundLimit)
		{
			base.Transform.localPosition = new Vector3(base.Transform.localPosition.x, PositionGroundLimit, base.Transform.localPosition.z);
		}
	}

	public virtual void AddForce(Vector3 force)
	{
		m_PositionSpring.AddForce(force);
	}

	public virtual void AddForce(float x, float y, float z)
	{
		AddForce(new Vector3(x, y, z));
	}

	public virtual void AddForce2(Vector3 force)
	{
		m_PositionSpring2.AddForce(force);
	}

	public void AddForce2(float x, float y, float z)
	{
		AddForce2(new Vector3(x, y, z));
	}

	public virtual void AddRollForce(float force)
	{
		m_RotationSpring.AddForce(Vector3.forward * force);
	}

	public virtual void AddRotationForce(Vector3 force)
	{
		m_RotationSpring.AddForce(force);
	}

	public void AddRotationForce(float x, float y, float z)
	{
		AddRotationForce(new Vector3(x, y, z));
	}

	public void UpdateLook(Vector2 look)
	{
		UpdateMouseLook(look);
	}

	protected virtual void UpdateMouseLook(Vector2 look)
	{
		m_MouseMove.x = look.x * Time.timeScale;
		m_MouseMove.y = look.y * Time.timeScale;
		MouseSmoothSteps = Mathf.Clamp(MouseSmoothSteps, 1, 20);
		MouseSmoothWeight = Mathf.Clamp01(MouseSmoothWeight);
		while (m_MouseSmoothBuffer.Count > MouseSmoothSteps)
		{
			m_MouseSmoothBuffer.RemoveAt(0);
		}
		m_MouseSmoothBuffer.Add(m_MouseMove);
		float num = 1f;
		Vector2 zero = Vector2.zero;
		float num2 = 0f;
		for (int num3 = m_MouseSmoothBuffer.Count - 1; num3 > 0; num3--)
		{
			zero += m_MouseSmoothBuffer[num3] * num;
			num2 += 1f * num;
			num *= MouseSmoothWeight / base.Delta;
		}
		num2 = Mathf.Max(1f, num2);
		Vector2 vector = vp_MathUtility.NaNSafeVector2(zero / num2);
		float num4 = 0f;
		float num5 = Mathf.Abs(vector.x);
		float num6 = Mathf.Abs(vector.y);
		if (MouseAcceleration)
		{
			num4 = Mathf.Sqrt(num5 * num5 + num6 * num6) / base.Delta;
			num4 = ((!(num4 <= MouseAccelerationThreshold)) ? num4 : 0f);
		}
		m_Yaw += vector.x * (MouseSensitivity.x + num4);
		m_Pitch -= vector.y * (MouseSensitivity.y + num4);
		m_Yaw = ((!(m_Yaw < -360f)) ? m_Yaw : (m_Yaw += 360f));
		m_Yaw = ((!(m_Yaw > 360f)) ? m_Yaw : (m_Yaw -= 360f));
		m_Yaw = Mathf.Clamp(m_Yaw, RotationYawLimit.x, RotationYawLimit.y);
		m_Pitch = ((!(m_Pitch < -360f)) ? m_Pitch : (m_Pitch += 360f));
		m_Pitch = ((!(m_Pitch > 360f)) ? m_Pitch : (m_Pitch -= 360f));
		m_Pitch = Mathf.Clamp(m_Pitch, 0f - RotationPitchLimit.x, 0f - RotationPitchLimit.y);
	}

	protected virtual void UpdateZoom()
	{
		if (!(m_FinalZoomTime <= Time.time))
		{
			RenderingZoomDamping = Mathf.Max(RenderingZoomDamping, 0.01f);
			float t = 1f - (m_FinalZoomTime - Time.time) / RenderingZoomDamping;
			base.gameObject.camera.fieldOfView = Mathf.SmoothStep(base.gameObject.camera.fieldOfView, RenderingFieldOfView, t);
		}
	}

	public virtual void Zoom()
	{
		m_FinalZoomTime = Time.time + RenderingZoomDamping;
	}

	public virtual void SnapZoom()
	{
		base.gameObject.camera.fieldOfView = RenderingFieldOfView;
	}

	protected virtual void UpdateShakes()
	{
		if (ShakeSpeed != 0f)
		{
			m_Yaw -= m_Shake.y;
			m_Pitch -= m_Shake.x;
			m_Shake = Vector3.Scale(vp_SmoothRandom.GetVector3Centered(ShakeSpeed), ShakeAmplitude);
			m_Yaw += m_Shake.y;
			m_Pitch += m_Shake.x;
			m_RotationSpring.AddForce(Vector3.forward * m_Shake.z * Time.timeScale);
		}
	}

	protected virtual void UpdateBob()
	{
		if (!(BobAmplitude == Vector4.zero) && !(BobRate == Vector4.zero))
		{
			m_BobSpeed = ((!BobRequireGroundContact || FPController.Grounded) ? FPController.mCharacterController.velocity.sqrMagnitude : 0f);
			m_BobSpeed = Mathf.Min(m_BobSpeed * BobInputVelocityScale, BobMaxInputVelocity);
			m_BobSpeed = Mathf.Round(m_BobSpeed * 1000f) / 1000f;
			if (m_BobSpeed == 0f)
			{
				m_BobSpeed = Mathf.Min(m_LastBobSpeed * 0.93f, BobMaxInputVelocity);
			}
			m_CurrentBobAmp.y = m_BobSpeed * (BobAmplitude.y * -0.0001f);
			m_CurrentBobVal.y = Mathf.Cos(Time.time * (BobRate.y * 10f)) * m_CurrentBobAmp.y;
			m_CurrentBobAmp.w = m_BobSpeed * (BobAmplitude.w * 0.0001f);
			m_CurrentBobVal.w = Mathf.Cos(Time.time * (BobRate.w * 10f)) * m_CurrentBobAmp.w;
			m_PositionSpring.AddForce((Vector3)m_CurrentBobVal * Time.timeScale);
			AddRollForce(m_CurrentBobVal.w * Time.timeScale);
			m_LastBobSpeed = m_BobSpeed;
			DetectBobStep(m_BobSpeed, m_CurrentBobVal.y);
		}
	}

	protected virtual void DetectBobStep(float speed, float upBob)
	{
		if (BobStepCallback != null && !(speed < BobStepThreshold))
		{
			bool flag = ((m_LastUpBob < upBob) ? true : false);
			m_LastUpBob = upBob;
			if (flag && !m_BobWasElevating)
			{
				BobStepCallback();
			}
			m_BobWasElevating = flag;
		}
	}

	protected virtual void UpdateSwaying()
	{
		AddRollForce((base.Transform.InverseTransformDirection(FPController.mCharacterController.velocity * 0.016f) * Time.timeScale).x * RotationStrafeRoll);
	}

	protected virtual void UpdateEarthQuake()
	{
		if (!(Player == null) && Player.Earthquake.Active)
		{
			if (m_PositionSpring.State.y >= m_PositionSpring.RestState.y)
			{
				Vector3 o = Player.EarthQuakeForce.Get();
				o.y = 0f - o.y;
				Player.EarthQuakeForce.Set(o);
			}
			m_PositionSpring.AddForce(Player.EarthQuakeForce.Get() * PositionEarthQuakeFactor);
			m_RotationSpring.AddForce(Vector3.forward * ((0f - Player.EarthQuakeForce.Get().x) * 2f) * RotationEarthQuakeFactor);
		}
	}

	protected virtual void UpdateSprings()
	{
		m_PositionSpring.FixedUpdate();
		m_PositionSpring2.FixedUpdate();
		m_RotationSpring.FixedUpdate();
	}

	public virtual void DoBomb(Vector3 positionForce, float minRollForce, float maxRollForce)
	{
		AddForce2(positionForce);
		float num = UnityEngine.Random.Range(minRollForce, maxRollForce);
		if (UnityEngine.Random.value > 0.5f)
		{
			num = 0f - num;
		}
		AddRollForce(num);
	}

	public override void Refresh()
	{
		if (Application.isPlaying)
		{
			if (m_PositionSpring != null)
			{
				m_PositionSpring.Stiffness = new Vector3(PositionSpringStiffness, PositionSpringStiffness, PositionSpringStiffness);
				m_PositionSpring.Damping = Vector3.one - new Vector3(PositionSpringDamping, PositionSpringDamping, PositionSpringDamping);
				m_PositionSpring.MinState.y = PositionGroundLimit;
				m_PositionSpring.RestState = PositionOffset;
			}
			if (m_PositionSpring2 != null)
			{
				m_PositionSpring2.Stiffness = new Vector3(PositionSpring2Stiffness, PositionSpring2Stiffness, PositionSpring2Stiffness);
				m_PositionSpring2.Damping = Vector3.one - new Vector3(PositionSpring2Damping, PositionSpring2Damping, PositionSpring2Damping);
				m_PositionSpring2.MinState.y = 0f - PositionOffset.y + PositionGroundLimit;
			}
			if (m_RotationSpring != null)
			{
				m_RotationSpring.Stiffness = new Vector3(RotationSpringStiffness, RotationSpringStiffness, RotationSpringStiffness);
				m_RotationSpring.Damping = Vector3.one - new Vector3(RotationSpringDamping, RotationSpringDamping, RotationSpringDamping);
			}
			Zoom();
		}
	}

	public virtual void SnapSprings()
	{
		if (m_PositionSpring != null)
		{
			m_PositionSpring.RestState = PositionOffset;
			m_PositionSpring.State = PositionOffset;
			m_PositionSpring.Stop(true);
		}
		if (m_PositionSpring2 != null)
		{
			m_PositionSpring2.RestState = Vector3.zero;
			m_PositionSpring2.State = Vector3.zero;
			m_PositionSpring2.Stop(true);
		}
		if (m_RotationSpring != null)
		{
			m_RotationSpring.RestState = Vector3.zero;
			m_RotationSpring.State = Vector3.zero;
			m_RotationSpring.Stop(true);
		}
	}

	public virtual void StopSprings()
	{
		if (m_PositionSpring != null)
		{
			m_PositionSpring.Stop(true);
		}
		if (m_PositionSpring2 != null)
		{
			m_PositionSpring2.Stop(true);
		}
		if (m_RotationSpring != null)
		{
			m_RotationSpring.Stop(true);
		}
		m_BobSpeed = 0f;
		m_LastBobSpeed = 0f;
	}

	public virtual void Stop()
	{
		SnapSprings();
		SnapZoom();
		Refresh();
	}

	public virtual void SetRotation(Vector2 eulerAngles, bool stop = true, bool resetInitialRotation = true)
	{
		Angle = eulerAngles;
		if (stop)
		{
			Stop();
		}
		if (resetInitialRotation)
		{
			m_InitialRotation = Vector2.zero;
		}
	}

	public void OnMessage_FallImpact(float impact)
	{
		impact = Mathf.Abs(impact * 55f);
		float t = impact * PositionKneeling;
		float t2 = impact * RotationKneeling;
		t = Mathf.SmoothStep(0f, 1f, t);
		t2 = Mathf.SmoothStep(0f, 1f, t2);
		t2 = Mathf.SmoothStep(0f, 1f, t2);
		if (m_PositionSpring != null)
		{
			m_PositionSpring.AddSoftForce(Vector3.down * t, PositionKneelingSoftness);
		}
		if (m_RotationSpring != null)
		{
			float num = ((!(UnityEngine.Random.value > 0.5f)) ? (0f - t2 * 2f) : (t2 * 2f));
			m_RotationSpring.AddSoftForce(Vector3.forward * num, RotationKneelingSoftness);
		}
	}
}
