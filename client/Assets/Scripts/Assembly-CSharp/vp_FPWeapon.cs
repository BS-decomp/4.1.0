using System;
using System.Collections.Generic;
using UnityEngine;

public class vp_FPWeapon : vp_Component
{
	public GameObject WeaponPrefab;

	protected GameObject m_WeaponModel;

	public CharacterController Controller;

	public float RenderingZoomDamping = 0.5f;

	protected float m_FinalZoomTime;

	public float RenderingFieldOfView = 35f;

	public Vector2 RenderingClippingPlanes = new Vector2(0.01f, 10f);

	public float RenderingZScale = 1f;

	public Vector3 PositionOffset = new Vector3(0.15f, -0.15f, -0.15f);

	public float PositionSpringStiffness = 0.01f;

	public float PositionSpringDamping = 0.25f;

	public float PositionFallRetract = 1f;

	public float PositionPivotSpringStiffness = 0.01f;

	public float PositionPivotSpringDamping = 0.25f;

	public float PositionSpring2Stiffness = 0.95f;

	public float PositionSpring2Damping = 0.25f;

	public float PositionKneeling = 0.06f;

	public int PositionKneelingSoftness = 1;

	public Vector3 PositionWalkSlide = new Vector3(0.5f, 0.75f, 0.5f);

	public Vector3 PositionPivot = Vector3.zero;

	public Vector3 RotationPivot = Vector3.zero;

	public float PositionInputVelocityScale = 1f;

	public float PositionMaxInputVelocity = 25f;

	protected vp_Spring m_PositionSpring;

	protected vp_Spring m_PositionSpring2;

	protected vp_Spring m_PositionPivotSpring;

	protected vp_Spring m_RotationPivotSpring;

	protected GameObject m_WeaponCamera;

	protected GameObject m_WeaponGroup;

	protected GameObject m_Pivot;

	protected Transform m_WeaponGroupTransform;

	public Vector3 RotationOffset = Vector3.zero;

	public float RotationSpringStiffness = 0.01f;

	public float RotationSpringDamping = 0.25f;

	public float RotationPivotSpringStiffness = 0.01f;

	public float RotationPivotSpringDamping = 0.25f;

	public float RotationSpring2Stiffness = 0.95f;

	public float RotationSpring2Damping = 0.25f;

	public float RotationKneeling;

	public int RotationKneelingSoftness = 1;

	public Vector3 RotationLookSway = new Vector3(1f, 0.7f, 0f);

	public Vector3 RotationStrafeSway = new Vector3(0.3f, 1f, 1.5f);

	public Vector3 RotationFallSway = new Vector3(1f, -0.5f, -3f);

	public float RotationSlopeSway = 0.5f;

	public float RotationInputVelocityScale = 1f;

	public float RotationMaxInputVelocity = 15f;

	protected vp_Spring m_RotationSpring;

	protected vp_Spring m_RotationSpring2;

	protected Vector3 m_SwayVel = Vector3.zero;

	protected Vector3 m_FallSway = Vector3.zero;

	public float RetractionDistance;

	public Vector2 RetractionOffset = new Vector2(0f, 0f);

	public float RetractionRelaxSpeed = 0.25f;

	protected bool m_DrawRetractionDebugLine;

	public float ShakeSpeed = 0.05f;

	public Vector3 ShakeAmplitude = new Vector3(0.25f, 0f, 2f);

	protected Vector3 m_Shake = Vector3.zero;

	public Vector4 BobRate = new Vector4(0.9f, 0.45f, 0f, 0f);

	public Vector4 BobAmplitude = new Vector4(0.35f, 0.5f, 0f, 0f);

	public float BobInputVelocityScale = 1f;

	public float BobMaxInputVelocity = 100f;

	public bool BobRequireGroundContact = true;

	protected float m_LastBobSpeed;

	protected Vector4 m_CurrentBobAmp = Vector4.zero;

	protected Vector4 m_CurrentBobVal = Vector4.zero;

	protected float m_BobSpeed;

	public Vector3 StepPositionForce = new Vector3(0f, -0.0012f, -0.0012f);

	public Vector3 StepRotationForce = new Vector3(0f, 0f, 0f);

	public int StepSoftness = 4;

	public float StepMinVelocity;

	public float StepPositionBalance;

	public float StepRotationBalance;

	public float StepForceScale = 1f;

	protected float m_LastUpBob;

	protected bool m_BobWasElevating;

	protected Vector3 m_PosStep = Vector3.zero;

	protected Vector3 m_RotStep = Vector3.zero;

	public AudioClip SoundWield;

	public AudioClip SoundUnWield;

	public AnimationClip AnimationWield;

	public AnimationClip AnimationUnWield;

	public List<UnityEngine.Object> AnimationAmbient = new List<UnityEngine.Object>();

	protected List<bool> m_AmbAnimPlayed = new List<bool>();

	public Vector2 AmbientInterval = new Vector2(2.5f, 7.5f);

	protected int m_CurrentAmbientAnimation;

	protected int m_AnimationAmbientTimer;

	public Vector3 PositionExitOffset = new Vector3(0f, -1f, 0f);

	public Vector3 RotationExitOffset = new Vector3(40f, 0f, 0f);

	protected bool m_Wielded = true;

	protected Vector2 m_MouseMove = Vector2.zero;

	private vp_FPPlayerEventHandler m_Player;

	private Vector2 LookAxis;

	public bool Wielded
	{
		get
		{
			return m_Wielded && base.Rendering;
		}
	}

	public GameObject WeaponCamera
	{
		get
		{
			return m_WeaponCamera;
		}
	}

	public GameObject WeaponModel
	{
		get
		{
			return m_WeaponModel;
		}
	}

	public Vector3 DefaultPosition
	{
		get
		{
			return (Vector3)base.DefaultState.Preset.GetFieldValue("PositionOffset");
		}
	}

	public Vector3 DefaultRotation
	{
		get
		{
			return (Vector3)base.DefaultState.Preset.GetFieldValue("RotationOffset");
		}
	}

	public bool DrawRetractionDebugLine
	{
		get
		{
			return m_DrawRetractionDebugLine;
		}
		set
		{
			m_DrawRetractionDebugLine = value;
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

	protected override void Awake()
	{
		base.Awake();
		if (base.transform.parent == null)
		{
			Debug.LogError(string.Concat("Error (", this, ") Must not be placed in scene root. Disabling self."));
			vp_Utility.Activate(base.gameObject, false);
			return;
		}
		if (PlayerInput.instance != null)
		{
			Controller = PlayerInput.instance.mCharacterController;
		}
		else
		{
			Controller = base.Transform.root.GetComponent<CharacterController>();
		}
		if (Controller == null)
		{
			Debug.LogError(string.Concat("Error (", this, ") Could not find CharacterController. Disabling self."));
			vp_Utility.Activate(base.gameObject, false);
			return;
		}
		base.Transform.eulerAngles = Vector3.zero;
		Camera camera = null;
		foreach (Transform item in base.Transform.parent)
		{
			camera = (Camera)item.GetComponent(typeof(Camera));
			if (camera != null)
			{
				m_WeaponCamera = camera.gameObject;
				break;
			}
		}
		if (base.collider != null)
		{
			base.collider.enabled = false;
		}
	}

	protected override void Start()
	{
		base.Start();
		InstantiateWeaponModel();
		m_WeaponGroup = new GameObject(base.name + "Transform");
		m_WeaponGroupTransform = m_WeaponGroup.transform;
		m_WeaponGroupTransform.parent = base.Transform.parent;
		m_WeaponGroupTransform.localPosition = PositionOffset;
		vp_Layer.Set(m_WeaponGroup, 31);
		base.Transform.parent = m_WeaponGroupTransform;
		base.Transform.localPosition = Vector3.zero;
		m_WeaponGroupTransform.localEulerAngles = RotationOffset;
		if (m_WeaponCamera != null && vp_Utility.IsActive(m_WeaponCamera.gameObject))
		{
			vp_Layer.Set(base.gameObject, 31, true);
		}
		m_Pivot = new GameObject("Pivot");
		m_Pivot.gameObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
		m_Pivot.transform.parent = m_WeaponGroupTransform;
		m_Pivot.transform.localPosition = Vector3.zero;
		m_Pivot.layer = 31;
		vp_Utility.Activate(m_Pivot.gameObject, false);
		m_PositionSpring = new vp_Spring(m_WeaponGroup.gameObject.transform, vp_Spring.UpdateMode.Position);
		m_PositionSpring.RestState = PositionOffset;
		m_PositionPivotSpring = new vp_Spring(base.Transform, vp_Spring.UpdateMode.Position);
		m_PositionPivotSpring.RestState = PositionPivot;
		m_PositionSpring2 = new vp_Spring(base.Transform, vp_Spring.UpdateMode.PositionAdditive);
		m_PositionSpring2.MinVelocity = 1E-05f;
		m_RotationSpring = new vp_Spring(m_WeaponGroup.gameObject.transform, vp_Spring.UpdateMode.Rotation);
		m_RotationSpring.RestState = RotationOffset;
		m_RotationPivotSpring = new vp_Spring(base.Transform, vp_Spring.UpdateMode.Rotation);
		m_RotationPivotSpring.RestState = RotationPivot;
		m_RotationSpring2 = new vp_Spring(m_WeaponGroup.gameObject.transform, vp_Spring.UpdateMode.RotationAdditive);
		m_RotationSpring2.MinVelocity = 1E-05f;
		SnapSprings();
		Refresh();
	}

	public virtual void InstantiateWeaponModel()
	{
		if (WeaponPrefab != null)
		{
			if (m_WeaponModel != null && m_WeaponModel != base.gameObject)
			{
				UnityEngine.Object.Destroy(m_WeaponModel);
			}
			m_WeaponModel = (GameObject)UnityEngine.Object.Instantiate(WeaponPrefab);
			m_WeaponModel.transform.parent = base.transform;
			m_WeaponModel.transform.localPosition = Vector3.zero;
			m_WeaponModel.transform.localScale = new Vector3(1f, 1f, RenderingZScale);
			m_WeaponModel.transform.localEulerAngles = Vector3.zero;
			if (m_WeaponCamera != null && vp_Utility.IsActive(m_WeaponCamera.gameObject))
			{
				vp_Layer.Set(m_WeaponModel, 31, true);
			}
		}
		else
		{
			m_WeaponModel = base.gameObject;
		}
		CacheRenderers();
	}

	protected override void Init()
	{
		base.Init();
		ScheduleAmbientAnimation();
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		InputManager.GetAxisEvent += GetAxis;
		vp_FPController fPController = PlayerInput.instance.FPController;
		fPController.FallImpactEvent = (Action<float>)Delegate.Combine(fPController.FallImpactEvent, new Action<float>(OnMessage_FallImpact));
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		InputManager.GetAxisEvent -= GetAxis;
		vp_FPController fPController = PlayerInput.instance.FPController;
		fPController.FallImpactEvent = (Action<float>)Delegate.Remove(fPController.FallImpactEvent, new Action<float>(OnMessage_FallImpact));
		LookAxis = Vector2.zero;
	}

	private void GetAxis(string name, float value)
	{
		switch (name)
		{
		case "Mouse X":
			LookAxis.x = value;
			break;
		case "Mouse Y":
			LookAxis.y = value;
			break;
		}
	}

	protected override void Update()
	{
		base.Update();
		UpdateMouseLook();
	}

	protected override void FixedUpdate()
	{
		base.FixedUpdate();
		if (Time.timeScale != 0f)
		{
			UpdateSwaying();
			UpdateBob();
			UpdateRetraction();
			UpdateSprings();
		}
	}

	public virtual void AddForce2(Vector3 positional, Vector3 angular)
	{
		m_PositionSpring2.AddForce(positional);
		m_RotationSpring2.AddForce(angular);
	}

	public virtual void AddForce2(float xPos, float yPos, float zPos, float xRot, float yRot, float zRot)
	{
		AddForce2(new Vector3(xPos, yPos, zPos), new Vector3(xRot, yRot, zRot));
	}

	public virtual void AddForce(Vector3 force)
	{
		m_PositionSpring.AddForce(force);
	}

	public virtual void AddForce(float x, float y, float z)
	{
		AddForce(new Vector3(x, y, z));
	}

	public virtual void AddForce(Vector3 positional, Vector3 angular)
	{
		m_PositionSpring.AddForce(positional);
		m_RotationSpring.AddForce(angular);
	}

	public virtual void AddForce(float xPos, float yPos, float zPos, float xRot, float yRot, float zRot)
	{
		AddForce(new Vector3(xPos, yPos, zPos), new Vector3(xRot, yRot, zRot));
	}

	public virtual void AddSoftForce(Vector3 force, int frames)
	{
		m_PositionSpring.AddSoftForce(force, frames);
	}

	public virtual void AddSoftForce(float x, float y, float z, int frames)
	{
		AddSoftForce(new Vector3(x, y, z), frames);
	}

	public virtual void AddSoftForce(Vector3 positional, Vector3 angular, int frames)
	{
		m_PositionSpring.AddSoftForce(positional, frames);
		m_RotationSpring.AddSoftForce(angular, frames);
	}

	public virtual void AddSoftForce(float xPos, float yPos, float zPos, float xRot, float yRot, float zRot, int frames)
	{
		AddSoftForce(new Vector3(xPos, yPos, zPos), new Vector3(xRot, yRot, zRot), frames);
	}

	protected virtual void UpdateMouseLook()
	{
		m_MouseMove.x = LookAxis.x / base.Delta;
		m_MouseMove.y = LookAxis.y / base.Delta;
		m_MouseMove *= RotationInputVelocityScale;
		m_MouseMove = Vector3.Min(m_MouseMove, Vector3.one * RotationMaxInputVelocity);
		m_MouseMove = Vector3.Max(m_MouseMove, Vector3.one * (0f - RotationMaxInputVelocity));
	}

	protected virtual void UpdateZoom()
	{
		if (!(m_FinalZoomTime <= Time.time) && m_Wielded)
		{
			RenderingZoomDamping = Mathf.Max(RenderingZoomDamping, 0.01f);
			float t = 1f - (m_FinalZoomTime - Time.time) / RenderingZoomDamping;
			if (m_WeaponCamera != null && vp_Utility.IsActive(m_WeaponCamera.gameObject))
			{
				m_WeaponCamera.camera.fieldOfView = Mathf.SmoothStep(m_WeaponCamera.gameObject.camera.fieldOfView, RenderingFieldOfView, t);
			}
		}
	}

	public virtual void Zoom()
	{
		m_FinalZoomTime = Time.time + RenderingZoomDamping;
	}

	public virtual void SnapZoom()
	{
		if (m_WeaponCamera != null && vp_Utility.IsActive(m_WeaponCamera.gameObject))
		{
			m_WeaponCamera.camera.fieldOfView = RenderingFieldOfView;
		}
	}

	protected virtual void UpdateShakes()
	{
		if (ShakeSpeed != 0f)
		{
			m_Shake = Vector3.Scale(vp_SmoothRandom.GetVector3Centered(ShakeSpeed), ShakeAmplitude);
			m_RotationSpring.AddForce(m_Shake * Time.timeScale);
		}
	}

	protected virtual void UpdateRetraction(bool firstIteration = true)
	{
		if (RetractionDistance != 0f)
		{
			Vector3 vector = WeaponModel.transform.TransformPoint(RetractionOffset);
			Vector3 end = vector + WeaponModel.transform.forward * RetractionDistance;
			RaycastHit hitInfo;
			if (Physics.Linecast(vector, end, out hitInfo, -1749041173) && !hitInfo.collider.isTrigger)
			{
				WeaponModel.transform.position = hitInfo.point - (hitInfo.point - vector).normalized * (RetractionDistance * 0.99f);
				WeaponModel.transform.localPosition = Vector3.forward * Mathf.Min(WeaponModel.transform.localPosition.z, 0f);
			}
			else if (firstIteration && WeaponModel.transform.localPosition != Vector3.zero && WeaponModel != base.gameObject)
			{
				WeaponModel.transform.localPosition = Vector3.forward * Mathf.SmoothStep(WeaponModel.transform.localPosition.z, 0f, RetractionRelaxSpeed * Time.timeScale);
				UpdateRetraction(false);
			}
		}
	}

	protected virtual void UpdateBob()
	{
		if (!(BobAmplitude == Vector4.zero) && !(BobRate == Vector4.zero))
		{
			m_BobSpeed = ((!BobRequireGroundContact || Controller.isGrounded) ? Controller.velocity.sqrMagnitude : 0f);
			m_BobSpeed = Mathf.Min(m_BobSpeed * BobInputVelocityScale, BobMaxInputVelocity);
			m_BobSpeed = Mathf.Round(m_BobSpeed * 1000f) / 1000f;
			if (m_BobSpeed == 0f)
			{
				m_BobSpeed = Mathf.Min(m_LastBobSpeed * 0.93f, BobMaxInputVelocity);
			}
			m_CurrentBobAmp.x = m_BobSpeed * (BobAmplitude.x * -0.0001f);
			m_CurrentBobVal.x = Mathf.Cos(Time.time * (BobRate.x * 10f)) * m_CurrentBobAmp.x;
			m_CurrentBobAmp.y = m_BobSpeed * (BobAmplitude.y * 0.0001f);
			m_CurrentBobVal.y = Mathf.Cos(Time.time * (BobRate.y * 10f)) * m_CurrentBobAmp.y;
			m_PositionSpring.AddForce(m_CurrentBobVal);
			m_LastBobSpeed = m_BobSpeed;
		}
	}

	protected virtual void UpdateEarthQuake()
	{
		if (!(Player == null) && Player.Earthquake.Active && Controller.isGrounded)
		{
			Vector3 vector = Player.EarthQuakeForce.Get();
			AddForce(new Vector3(0f, 0f, (0f - vector.z) * 0.015f), new Vector3(vector.y * 2f, 0f - vector.x, vector.x * 2f));
		}
	}

	protected virtual void UpdateSprings()
	{
		m_PositionSpring.FixedUpdate();
		m_PositionPivotSpring.FixedUpdate();
		m_RotationPivotSpring.FixedUpdate();
		m_PositionSpring2.FixedUpdate();
		m_RotationSpring.FixedUpdate();
		m_RotationSpring2.FixedUpdate();
	}

	protected virtual void UpdateStep()
	{
		if (StepMinVelocity <= 0f || (BobRequireGroundContact && !Controller.isGrounded) || Controller.velocity.sqrMagnitude < StepMinVelocity)
		{
			return;
		}
		bool flag = ((m_LastUpBob < m_CurrentBobVal.x) ? true : false);
		m_LastUpBob = m_CurrentBobVal.x;
		if (flag && !m_BobWasElevating)
		{
			if (Mathf.Cos(Time.time * (BobRate.x * 5f)) > 0f)
			{
				m_PosStep = StepPositionForce - StepPositionForce * StepPositionBalance;
				m_RotStep = StepRotationForce - StepPositionForce * StepRotationBalance;
			}
			else
			{
				m_PosStep = StepPositionForce + StepPositionForce * StepPositionBalance;
				m_RotStep = Vector3.Scale(StepRotationForce - StepPositionForce * StepRotationBalance, -Vector3.one + Vector3.right * 2f);
			}
			AddSoftForce(m_PosStep * StepForceScale, m_RotStep * StepForceScale, StepSoftness);
		}
		m_BobWasElevating = flag;
	}

	protected virtual void UpdateSwaying()
	{
		m_SwayVel = Controller.velocity * PositionInputVelocityScale;
		m_SwayVel = Vector3.Min(m_SwayVel, Vector3.one * PositionMaxInputVelocity);
		m_SwayVel = Vector3.Max(m_SwayVel, Vector3.one * (0f - PositionMaxInputVelocity));
		m_SwayVel *= Time.timeScale;
		Vector3 vector = base.Transform.InverseTransformDirection(m_SwayVel / 60f);
		m_RotationSpring.AddForce(new Vector3(m_MouseMove.y * (RotationLookSway.x * 0.025f), m_MouseMove.x * (RotationLookSway.y * -0.025f), m_MouseMove.x * (RotationLookSway.z * -0.025f)));
		m_FallSway = RotationFallSway * (m_SwayVel.y * 0.005f);
		if (Controller.isGrounded)
		{
			m_FallSway *= RotationSlopeSway;
		}
		m_FallSway.z = Mathf.Max(0f, m_FallSway.z);
		m_RotationSpring.AddForce(m_FallSway);
		m_PositionSpring.AddForce(Vector3.forward * (0f - Mathf.Abs(m_SwayVel.y * (PositionFallRetract * 2.5E-05f))));
		m_PositionSpring.AddForce(new Vector3(vector.x * (PositionWalkSlide.x * 0.0016f), 0f - Mathf.Abs(vector.x * (PositionWalkSlide.y * 0.0016f)), (0f - vector.z) * (PositionWalkSlide.z * 0.0016f)));
		m_RotationSpring.AddForce(new Vector3(0f - Mathf.Abs(vector.x * (RotationStrafeSway.x * 0.16f)), 0f - vector.x * (RotationStrafeSway.y * 0.16f), vector.x * (RotationStrafeSway.z * 0.16f)));
	}

	public virtual void ResetSprings(float positionReset, float rotationReset, float positionPauseTime = 0f, float rotationPauseTime = 0f)
	{
		m_PositionSpring.State = Vector3.Lerp(m_PositionSpring.State, m_PositionSpring.RestState, positionReset);
		m_RotationSpring.State = Vector3.Lerp(m_RotationSpring.State, m_RotationSpring.RestState, rotationReset);
		m_PositionPivotSpring.State = Vector3.Lerp(m_PositionPivotSpring.State, m_PositionPivotSpring.RestState, positionReset);
		m_RotationPivotSpring.State = Vector3.Lerp(m_RotationPivotSpring.State, m_RotationPivotSpring.RestState, rotationReset);
		if (positionPauseTime != 0f)
		{
			m_PositionSpring.ForceVelocityFadeIn(positionPauseTime);
		}
		if (rotationPauseTime != 0f)
		{
			m_RotationSpring.ForceVelocityFadeIn(rotationPauseTime);
		}
		if (positionPauseTime != 0f)
		{
			m_PositionPivotSpring.ForceVelocityFadeIn(positionPauseTime);
		}
		if (rotationPauseTime != 0f)
		{
			m_RotationPivotSpring.ForceVelocityFadeIn(rotationPauseTime);
		}
	}

	public override void Refresh()
	{
		if (!Application.isPlaying)
		{
			return;
		}
		if (m_PositionSpring != null)
		{
			m_PositionSpring.Stiffness = new Vector3(PositionSpringStiffness, PositionSpringStiffness, PositionSpringStiffness);
			m_PositionSpring.Damping = Vector3.one - new Vector3(PositionSpringDamping, PositionSpringDamping, PositionSpringDamping);
			m_PositionSpring.RestState = PositionOffset - PositionPivot;
		}
		if (m_PositionPivotSpring != null)
		{
			m_PositionPivotSpring.Stiffness = new Vector3(PositionPivotSpringStiffness, PositionPivotSpringStiffness, PositionPivotSpringStiffness);
			m_PositionPivotSpring.Damping = Vector3.one - new Vector3(PositionPivotSpringDamping, PositionPivotSpringDamping, PositionPivotSpringDamping);
			m_PositionPivotSpring.RestState = PositionPivot;
		}
		if (m_RotationPivotSpring != null)
		{
			m_RotationPivotSpring.Stiffness = new Vector3(RotationPivotSpringStiffness, RotationPivotSpringStiffness, RotationPivotSpringStiffness);
			m_RotationPivotSpring.Damping = Vector3.one - new Vector3(RotationPivotSpringDamping, RotationPivotSpringDamping, RotationPivotSpringDamping);
			m_RotationPivotSpring.RestState = RotationPivot;
		}
		if (m_PositionSpring2 != null)
		{
			m_PositionSpring2.Stiffness = new Vector3(PositionSpring2Stiffness, PositionSpring2Stiffness, PositionSpring2Stiffness);
			m_PositionSpring2.Damping = Vector3.one - new Vector3(PositionSpring2Damping, PositionSpring2Damping, PositionSpring2Damping);
			m_PositionSpring2.RestState = Vector3.zero;
		}
		if (m_RotationSpring != null)
		{
			m_RotationSpring.Stiffness = new Vector3(RotationSpringStiffness, RotationSpringStiffness, RotationSpringStiffness);
			m_RotationSpring.Damping = Vector3.one - new Vector3(RotationSpringDamping, RotationSpringDamping, RotationSpringDamping);
			m_RotationSpring.RestState = RotationOffset;
		}
		if (m_RotationSpring2 != null)
		{
			m_RotationSpring2.Stiffness = new Vector3(RotationSpring2Stiffness, RotationSpring2Stiffness, RotationSpring2Stiffness);
			m_RotationSpring2.Damping = Vector3.one - new Vector3(RotationSpring2Damping, RotationSpring2Damping, RotationSpring2Damping);
			m_RotationSpring2.RestState = Vector3.zero;
		}
		if (base.Rendering)
		{
			if (m_WeaponCamera != null && vp_Utility.IsActive(m_WeaponCamera.gameObject))
			{
				m_WeaponCamera.camera.nearClipPlane = RenderingClippingPlanes.x;
				m_WeaponCamera.camera.farClipPlane = RenderingClippingPlanes.y;
			}
			Zoom();
		}
	}

	public override void Activate()
	{
		m_Wielded = true;
		base.Rendering = true;
		TimerManager.Cancel(m_DeactivationTimer);
		SnapZoom();
		if (m_WeaponGroup != null && !vp_Utility.IsActive(m_WeaponGroup))
		{
			vp_Utility.Activate(m_WeaponGroup);
		}
		SetPivotVisible(false);
	}

	public override void Deactivate()
	{
		m_Wielded = false;
		if (m_WeaponGroup != null && vp_Utility.IsActive(m_WeaponGroup))
		{
			vp_Utility.Activate(m_WeaponGroup, false);
		}
	}

	public virtual void SnapPivot()
	{
		if (m_PositionSpring != null)
		{
			m_PositionSpring.RestState = PositionOffset - PositionPivot;
			m_PositionSpring.State = PositionOffset - PositionPivot;
		}
		if (m_WeaponGroup != null)
		{
			m_WeaponGroupTransform.localPosition = PositionOffset - PositionPivot;
		}
		if (m_PositionPivotSpring != null)
		{
			m_PositionPivotSpring.RestState = PositionPivot;
			m_PositionPivotSpring.State = PositionPivot;
		}
		if (m_RotationPivotSpring != null)
		{
			m_RotationPivotSpring.RestState = RotationPivot;
			m_RotationPivotSpring.State = RotationPivot;
		}
		base.Transform.localPosition = PositionPivot;
		base.Transform.localEulerAngles = RotationPivot;
	}

	public virtual void SetPivotVisible(bool visible)
	{
		if (!(m_Pivot == null))
		{
			vp_Utility.Activate(m_Pivot.gameObject, visible);
		}
	}

	public virtual void SnapToExit()
	{
		RotationOffset = RotationExitOffset;
		PositionOffset = PositionExitOffset;
		SnapSprings();
		SnapPivot();
	}

	public virtual void SnapSprings()
	{
		if (m_PositionSpring != null)
		{
			m_PositionSpring.RestState = PositionOffset - PositionPivot;
			m_PositionSpring.State = PositionOffset - PositionPivot;
			m_PositionSpring.Stop(true);
		}
		if (m_WeaponGroup != null)
		{
			m_WeaponGroupTransform.localPosition = PositionOffset - PositionPivot;
		}
		if (m_PositionPivotSpring != null)
		{
			m_PositionPivotSpring.RestState = PositionPivot;
			m_PositionPivotSpring.State = PositionPivot;
			m_PositionPivotSpring.Stop(true);
		}
		base.Transform.localPosition = PositionPivot;
		if (m_PositionSpring2 != null)
		{
			m_PositionSpring2.RestState = Vector3.zero;
			m_PositionSpring2.State = Vector3.zero;
			m_PositionSpring2.Stop(true);
		}
		if (m_RotationPivotSpring != null)
		{
			m_RotationPivotSpring.RestState = RotationPivot;
			m_RotationPivotSpring.State = RotationPivot;
			m_RotationPivotSpring.Stop(true);
		}
		base.Transform.localEulerAngles = RotationPivot;
		if (m_RotationSpring != null)
		{
			m_RotationSpring.RestState = RotationOffset;
			m_RotationSpring.State = RotationOffset;
			m_RotationSpring.Stop(true);
		}
		if (m_RotationSpring2 != null)
		{
			m_RotationSpring2.RestState = Vector3.zero;
			m_RotationSpring2.State = Vector3.zero;
			m_RotationSpring2.Stop(true);
		}
	}

	public virtual void StopSprings()
	{
		if (m_PositionSpring != null)
		{
			m_PositionSpring.Stop(true);
		}
		if (m_PositionPivotSpring != null)
		{
			m_PositionPivotSpring.Stop(true);
		}
		if (m_PositionSpring2 != null)
		{
			m_PositionSpring2.Stop(true);
		}
		if (m_RotationSpring != null)
		{
			m_RotationSpring.Stop(true);
		}
		if (m_RotationPivotSpring != null)
		{
			m_RotationPivotSpring.Stop(true);
		}
		if (m_RotationSpring2 != null)
		{
			m_RotationSpring2.Stop(true);
		}
	}

	public virtual void Wield(bool showWeapon = true)
	{
		if (showWeapon)
		{
			SnapToExit();
		}
		PositionOffset = ((!showWeapon) ? PositionExitOffset : DefaultPosition);
		RotationOffset = ((!showWeapon) ? RotationExitOffset : DefaultRotation);
		m_Wielded = showWeapon;
		Refresh();
		base.StateManager.CombineStates();
		if (base.Audio != null)
		{
		}
		if (((!showWeapon) ? AnimationUnWield : AnimationWield) != null && vp_Utility.IsActive(base.gameObject))
		{
			m_WeaponModel.animation.CrossFade(((!showWeapon) ? AnimationUnWield : AnimationWield).name);
		}
	}

	public virtual void ScheduleAmbientAnimation()
	{
		if (AnimationAmbient.Count == 0 || !vp_Utility.IsActive(base.gameObject))
		{
			return;
		}
		m_AnimationAmbientTimer = TimerManager.In(UnityEngine.Random.Range(AmbientInterval.x, AmbientInterval.y), () =>
		{
			if (vp_Utility.IsActive(base.gameObject))
			{
				m_CurrentAmbientAnimation = UnityEngine.Random.Range(0, AnimationAmbient.Count);
				if (AnimationAmbient[m_CurrentAmbientAnimation] != null)
				{
					m_WeaponModel.animation.CrossFadeQueued(AnimationAmbient[m_CurrentAmbientAnimation].name);
					ScheduleAmbientAnimation();
				}
			}
		});
	}

	private void OnMessage_FallImpact(float impact)
	{
		if (m_PositionSpring != null)
		{
			m_PositionSpring.AddSoftForce(Vector3.down * impact * PositionKneeling, PositionKneelingSoftness);
		}
		if (m_RotationSpring != null)
		{
			m_RotationSpring.AddSoftForce(Vector3.right * impact * RotationKneeling, RotationKneelingSoftness);
		}
	}
}
