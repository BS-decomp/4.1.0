using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(vp_FPPlayerEventHandler))]
public class vp_FPController : vp_Component
{
	public vp_FPCamera FPCamera;

	public Action<float> FallImpactEvent;

	private vp_PlayerEventHandler m_Player;

	private CharacterController m_CharacterController;

	protected Vector3 m_FixedPosition = Vector3.zero;

	protected Vector3 m_SmoothPosition = Vector3.zero;

	protected CryptoBool m_Grounded = false;

	protected RaycastHit m_GroundHit;

	protected RaycastHit m_LastGroundHit;

	protected RaycastHit m_CeilingHit;

	protected RaycastHit m_WallHit;

	protected float m_FallImpact;

	protected Terrain m_CurrentTerrain;

	public float MotorAcceleration = 0.18f;

	public float MotorDamping = 0.17f;

	public float MotorBackwardsSpeed = 0.65f;

	public float MotorAirSpeed = 0.35f;

	public float MotorSlopeSpeedUp = 1f;

	public float MotorSlopeSpeedDown = 1f;

	public CryptoBool MotorFreeFly = false;

	protected Vector3 m_MoveDirection = Vector3.zero;

	protected float m_SlopeFactor = 1f;

	protected float m_SlopeFactor2 = 90f;

	protected Vector3 m_MotorThrottle = Vector3.zero;

	protected float m_MotorAirSpeedModifier = 1f;

	protected float m_CurrentAntiBumpOffset;

	protected Vector2 m_MoveVector = Vector2.zero;

	public float MotorJumpForce = 0.18f;

	public float MotorJumpForceDamping = 0.08f;

	public float MotorJumpForceHold = 0.003f;

	public float MotorJumpForceHoldDamping = 0.5f;

	protected int m_MotorJumpForceHoldSkipFrames;

	protected float m_MotorJumpForceAcc;

	protected bool m_MotorJumpDone = true;

	public bool MotorDoubleJump;

	private bool MotorDoubleJumpFirst;

	private bool MotorDoubleJumpFirstDown;

	private bool MotorDoubleJumpSecond;

	protected float m_FallSpeed;

	protected float m_LastFallSpeed;

	protected float m_HighestFallSpeed;

	public float PhysicsForceDamping = 0.05f;

	public float PhysicsPushForce = 5f;

	public CryptoFloat PhysicsGravityModifier = 0.2f;

	public CryptoFloat PhysicsGravityModifierFixed = 0.002f;

	public float PhysicsSlopeSlideLimit = 30f;

	public float PhysicsSlopeSlidiness = 0.15f;

	public float PhysicsWallBounce;

	public float PhysicsWallFriction;

	public bool PhysicsHasCollisionTrigger = true;

	protected GameObject m_Trigger;

	protected Vector3 m_ExternalForce = Vector3.zero;

	protected Vector3[] m_SmoothForceFrame = new Vector3[120];

	protected bool m_Slide;

	protected bool m_SlideFast;

	protected float m_SlideFallSpeed;

	protected float m_OnSteepGroundSince;

	protected float m_SlopeSlideSpeed;

	protected Vector3 m_PredictedPos = Vector3.zero;

	protected Vector3 m_PrevPos = Vector3.zero;

	protected Vector3 m_PrevDir = Vector3.zero;

	protected Vector3 m_NewDir = Vector3.zero;

	protected float m_ForceImpact;

	protected float m_ForceMultiplier;

	protected Vector3 CapsuleBottom = Vector3.zero;

	protected Vector3 CapsuleTop = Vector3.zero;

	protected float m_SkinWidth = 0.08f;

	protected Transform m_Platform;

	protected Vector3 m_PositionOnPlatform = Vector3.zero;

	protected float m_LastPlatformAngle;

	protected Vector3 m_LastPlatformPos = Vector3.zero;

	protected vp_PlayerEventHandler Player
	{
		get
		{
			if (m_Player == null && EventHandler != null)
			{
				m_Player = (vp_PlayerEventHandler)EventHandler;
			}
			return m_Player;
		}
	}

	public CharacterController mCharacterController
	{
		get
		{
			if (m_CharacterController == null)
			{
				m_CharacterController = base.gameObject.GetComponent<CharacterController>();
			}
			return m_CharacterController;
		}
	}

	public Vector3 SmoothPosition
	{
		get
		{
			return m_SmoothPosition;
		}
	}

	public bool Grounded
	{
		get
		{
			return m_Grounded;
		}
	}

	public Vector3 GroundNormal
	{
		get
		{
			return m_GroundHit.normal;
		}
	}

	public float GroundAngle
	{
		get
		{
			return Vector3.Angle(m_GroundHit.normal, Vector3.up);
		}
	}

	public Transform GroundTransform
	{
		get
		{
			return m_GroundHit.transform;
		}
	}

	public Vector2 OnValue_InputMoveVector
	{
		get
		{
			return m_MoveVector;
		}
		set
		{
			m_MoveVector = ((!(value.y < 0f)) ? value.normalized : (value.normalized * MotorBackwardsSpeed));
		}
	}

	protected override void Awake()
	{
		base.Awake();
		mCharacterController.center = new Vector3(0f, mCharacterController.height * 0.5f, 0f);
		m_CharacterController.radius = 0.375f;
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		vp_TargetEvent<Vector3>.Register(m_Transform, "ForceImpact", AddForce);
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		vp_TargetEvent<Vector3>.Unregister(m_Root, "ForceImpact", AddForce);
		m_Platform = null;
	}

	protected override void Start()
	{
		base.Start();
		SetPosition(base.Transform.position);
		if (PhysicsHasCollisionTrigger)
		{
			m_Trigger = new GameObject("Trigger");
			m_Trigger.transform.parent = m_Transform;
			CapsuleCollider capsuleCollider = m_Trigger.AddComponent<CapsuleCollider>();
			capsuleCollider.isTrigger = true;
			capsuleCollider.radius = mCharacterController.radius + m_SkinWidth;
			capsuleCollider.height = mCharacterController.height + m_SkinWidth * 2f;
			capsuleCollider.center = mCharacterController.center;
			m_Trigger.layer = 30;
			m_Trigger.transform.localPosition = Vector3.zero;
		}
	}

	protected override void Update()
	{
		base.Update();
		SmoothMove();
	}

	protected override void LateUpdate()
	{
		base.LateUpdate();
		if (m_SlopeFactor2 > 92f || 88f > m_SlopeFactor2)
		{
			Application.Quit();
		}
	}

	protected override void FixedUpdate()
	{
		UpdateMotor();
		UpdateJump();
		UpdateForces();
		UpdateSliding();
		FixedMove();
		UpdateCollisions();
		UpdatePlatformMove();
		m_PrevPos = base.Transform.position;
	}

	protected virtual void UpdateMotor()
	{
		if (!MotorFreeFly)
		{
			UpdateThrottleWalk();
		}
		else
		{
			UpdateThrottleFree();
		}
		m_MotorThrottle = vp_MathUtility.SnapToZero(m_MotorThrottle);
	}

	protected virtual void UpdateThrottleWalk()
	{
		UpdateSlopeFactor();
		m_MotorAirSpeedModifier = ((!m_Grounded) ? MotorAirSpeed : 1f);
		m_MotorThrottle += m_MoveVector.y * (base.Transform.TransformDirection(Vector3.forward * (MotorAcceleration * 0.1f) * m_MotorAirSpeedModifier) * m_SlopeFactor);
		m_MotorThrottle += m_MoveVector.x * (base.Transform.TransformDirection(Vector3.right * (MotorAcceleration * 0.1f) * m_MotorAirSpeedModifier) * m_SlopeFactor);
		m_MotorThrottle.x /= 1f + MotorDamping * m_MotorAirSpeedModifier;
		m_MotorThrottle.z /= 1f + MotorDamping * m_MotorAirSpeedModifier;
	}

	protected virtual void UpdateThrottleFree()
	{
		m_MotorThrottle += m_MoveVector.y * base.Transform.TransformDirection(base.Transform.InverseTransformDirection(FPCamera.Forward) * (MotorAcceleration * 0.1f));
		m_MotorThrottle += m_MoveVector.x * base.Transform.TransformDirection(Vector3.right * (MotorAcceleration * 0.1f));
		m_MotorThrottle.x /= 1f + MotorDamping;
		m_MotorThrottle.z /= 1f + MotorDamping;
	}

	protected virtual void UpdateJump()
	{
		if (!MotorFreeFly)
		{
			UpdateJumpForceWalk();
		}
		else
		{
			UpdateJumpForceFree();
		}
		m_MotorThrottle.y += m_MotorJumpForceAcc;
		m_MotorJumpForceAcc /= 1f + MotorJumpForceHoldDamping;
		m_MotorThrottle.y /= 1f + MotorJumpForceDamping;
	}

	protected virtual void UpdateJumpForceWalk()
	{
		if (!CanStartJump() || (bool)m_Grounded)
		{
			return;
		}
		if (m_MotorJumpForceHoldSkipFrames > 2)
		{
			if (!(mCharacterController.velocity.y < 0f))
			{
				m_MotorJumpForceAcc += MotorJumpForceHold;
			}
		}
		else
		{
			m_MotorJumpForceHoldSkipFrames++;
		}
	}

	protected virtual void UpdateJumpForceFree()
	{
		if (CanStartJump())
		{
			m_MotorJumpForceAcc += MotorJumpForceHold;
		}
	}

	protected virtual void UpdateForces()
	{
		if ((bool)m_Grounded && m_FallSpeed <= 0f)
		{
			m_FallSpeed = Physics.gravity.y * ((float)PhysicsGravityModifier * (float)PhysicsGravityModifierFixed) * vp_TimeUtility.AdjustedTimeScale;
		}
		else
		{
			m_FallSpeed += Physics.gravity.y * ((float)PhysicsGravityModifier * (float)PhysicsGravityModifierFixed) * vp_TimeUtility.AdjustedTimeScale;
		}
		if (m_FallSpeed < m_LastFallSpeed)
		{
			m_HighestFallSpeed = m_FallSpeed;
		}
		m_LastFallSpeed = m_FallSpeed;
		if (m_SmoothForceFrame[0] != Vector3.zero)
		{
			AddForceInternal(m_SmoothForceFrame[0]);
			for (int i = 0; i < 120; i++)
			{
				m_SmoothForceFrame[i] = ((i >= 119) ? Vector3.zero : m_SmoothForceFrame[i + 1]);
				if (m_SmoothForceFrame[i] == Vector3.zero)
				{
					break;
				}
			}
		}
		m_ExternalForce /= 1f + PhysicsForceDamping * vp_TimeUtility.AdjustedTimeScale;
	}

	protected virtual void UpdateSliding()
	{
		bool slideFast = m_SlideFast;
		bool slide = m_Slide;
		m_Slide = false;
		if (!m_Grounded)
		{
			m_OnSteepGroundSince = 0f;
			m_SlideFast = false;
		}
		else if (GroundAngle > PhysicsSlopeSlideLimit)
		{
			m_Slide = true;
			if (GroundAngle <= mCharacterController.slopeLimit)
			{
				m_SlopeSlideSpeed = Mathf.Max(m_SlopeSlideSpeed, PhysicsSlopeSlidiness * 0.01f);
				m_OnSteepGroundSince = 0f;
				m_SlideFast = false;
				m_SlopeSlideSpeed = ((!(Mathf.Abs(m_SlopeSlideSpeed) < 0.0001f)) ? (m_SlopeSlideSpeed / (1f + 0.05f * vp_TimeUtility.AdjustedTimeScale)) : 0f);
			}
			else
			{
				if (m_SlopeSlideSpeed > 0.01f)
				{
					m_SlideFast = true;
				}
				if (m_OnSteepGroundSince == 0f)
				{
					m_OnSteepGroundSince = Time.time;
				}
				m_SlopeSlideSpeed += PhysicsSlopeSlidiness * 0.01f * ((Time.time - m_OnSteepGroundSince) * 0.125f) * vp_TimeUtility.AdjustedTimeScale;
				m_SlopeSlideSpeed = Mathf.Max(PhysicsSlopeSlidiness * 0.01f, m_SlopeSlideSpeed);
			}
			AddForce(Vector3.Cross(Vector3.Cross(GroundNormal, Vector3.down), GroundNormal) * m_SlopeSlideSpeed * vp_TimeUtility.AdjustedTimeScale);
		}
		else
		{
			m_OnSteepGroundSince = 0f;
			m_SlideFast = false;
			m_SlopeSlideSpeed = 0f;
		}
		if (m_MotorThrottle != Vector3.zero)
		{
			m_Slide = false;
		}
		if (m_SlideFast)
		{
			m_SlideFallSpeed = base.Transform.position.y;
		}
		else if (slideFast && !Grounded)
		{
			m_FallSpeed = base.Transform.position.y - m_SlideFallSpeed;
			m_FallSpeed = Mathf.Clamp(m_FallSpeed, 0f, 0.01f);
		}
		if (slide != m_Slide)
		{
			Player.SetState("Slide", m_Slide);
		}
		if (slideFast != m_SlideFast)
		{
			Player.SetState("SlideFast", m_SlideFast);
		}
	}

	protected virtual void FixedMove()
	{
		m_MoveDirection = Vector3.zero;
		m_MoveDirection += m_ExternalForce;
		m_MoveDirection += m_MotorThrottle;
		m_MoveDirection.y += m_FallSpeed;
		if (MotorDoubleJump && MotorDoubleJumpFirst && Grounded)
		{
			MotorDoubleJumpFirst = false;
			MotorDoubleJumpFirstDown = false;
			MotorDoubleJumpSecond = false;
		}
		m_CurrentAntiBumpOffset = 0f;
		if ((bool)m_Grounded && m_MotorThrottle.y <= 0.001f)
		{
			m_CurrentAntiBumpOffset = Mathf.Max(mCharacterController.stepOffset, Vector3.Scale(m_MoveDirection, Vector3.one - Vector3.up).magnitude);
			m_MoveDirection += m_CurrentAntiBumpOffset * Vector3.down;
		}
		m_PredictedPos = base.Transform.position + m_MoveDirection * base.Delta;
		if (m_Platform != null && m_PositionOnPlatform != Vector3.zero)
		{
			mCharacterController.Move(m_Platform.TransformPoint(m_PositionOnPlatform) - m_Transform.position);
		}
		mCharacterController.Move(m_MoveDirection * base.Delta);
		Physics.SphereCast(new Ray(base.Transform.position + Vector3.up * mCharacterController.radius, Vector3.down), mCharacterController.radius, out m_GroundHit, m_SkinWidth + 0.001f, -1749041173);
		m_Grounded = m_GroundHit.GetComponent<Collider>() != null;
		if (m_GroundHit.transform == null && m_LastGroundHit.transform != null)
		{
			if (m_Platform != null && m_PositionOnPlatform != Vector3.zero)
			{
				AddForce(m_Platform.position - m_LastPlatformPos);
				m_Platform = null;
			}
			if (m_CurrentAntiBumpOffset != 0f)
			{
				mCharacterController.Move(m_CurrentAntiBumpOffset * Vector3.up * base.Delta);
				m_PredictedPos += m_CurrentAntiBumpOffset * Vector3.up * base.Delta;
				m_MoveDirection += m_CurrentAntiBumpOffset * Vector3.up;
			}
		}
	}

	protected virtual void SmoothMove()
	{
		m_FixedPosition = base.Transform.position;
		base.Transform.position = m_SmoothPosition;
		mCharacterController.Move(m_MoveDirection * base.Delta);
		m_SmoothPosition = base.Transform.position;
		base.Transform.position = m_FixedPosition;
		if (Vector3.Distance(base.Transform.position, m_SmoothPosition) > mCharacterController.radius)
		{
			m_SmoothPosition = base.Transform.position;
		}
		if (m_Platform != null && (m_LastPlatformPos.y < m_Platform.position.y || m_LastPlatformPos.y > m_Platform.position.y))
		{
			m_SmoothPosition.y = base.Transform.position.y;
		}
		m_SmoothPosition = Vector3.Lerp(m_SmoothPosition, base.Transform.position, Time.deltaTime);
	}

	protected virtual void UpdateCollisions()
	{
		if (m_GroundHit.transform != null && m_GroundHit.transform != m_LastGroundHit.transform)
		{
			if (!MotorFreeFly)
			{
				m_FallImpact = 0f - m_HighestFallSpeed;
			}
			else
			{
				m_FallImpact = 0f - mCharacterController.velocity.y * 0.01f;
			}
			m_SmoothPosition.y = base.Transform.position.y;
			DeflectDownForce();
			m_HighestFallSpeed = 0f;
			if (FallImpactEvent != null)
			{
				FallImpactEvent(m_FallImpact);
			}
			m_MotorThrottle.y = 0f;
			m_MotorJumpForceAcc = 0f;
			m_MotorJumpForceHoldSkipFrames = 0;
			if (m_GroundHit.GetComponent<Collider>().gameObject.layer == 28)
			{
				m_Platform = m_GroundHit.transform;
				m_LastPlatformAngle = m_Platform.eulerAngles.y;
			}
			else
			{
				m_Platform = null;
			}
		}
		else
		{
			m_FallImpact = 0f;
		}
		m_LastGroundHit = m_GroundHit;
	}

	protected virtual void UpdateSlopeFactor()
	{
		if (!m_Grounded)
		{
			m_SlopeFactor = 1f;
			return;
		}
		m_SlopeFactor = 1f + (1f - Vector3.Angle(m_GroundHit.normal, m_MotorThrottle) / m_SlopeFactor2);
		if (Mathf.Abs(1f - m_SlopeFactor) < 0.01f)
		{
			m_SlopeFactor = 1f;
		}
		else if (m_SlopeFactor > 1f)
		{
			if (MotorSlopeSpeedDown == 1f)
			{
				m_SlopeFactor = 1f / m_SlopeFactor;
				m_SlopeFactor *= 1.2f;
			}
			else
			{
				m_SlopeFactor *= MotorSlopeSpeedDown;
			}
		}
		else
		{
			if (MotorSlopeSpeedUp == 1f)
			{
				m_SlopeFactor *= 1.2f;
			}
			else
			{
				m_SlopeFactor *= MotorSlopeSpeedUp;
			}
			m_SlopeFactor = ((!(GroundAngle > mCharacterController.slopeLimit)) ? m_SlopeFactor : 0f);
		}
	}

	protected virtual void UpdatePlatformMove()
	{
		if (!(m_Platform == null))
		{
			m_PositionOnPlatform = m_Platform.InverseTransformPoint(m_Transform.position);
			FPCamera.Angle = new Vector2(FPCamera.Angle.x, FPCamera.Angle.y - Mathf.DeltaAngle(m_Platform.eulerAngles.y, m_LastPlatformAngle));
			m_LastPlatformAngle = m_Platform.eulerAngles.y;
			m_LastPlatformPos = m_Platform.position;
			m_SmoothPosition = base.Transform.position;
		}
	}

	public virtual void SetPosition(Vector3 position)
	{
		base.Transform.position = position;
		m_PrevPos = position;
		m_SmoothPosition = position;
		m_Platform = null;
	}

	protected virtual void AddForceInternal(Vector3 force)
	{
		m_ExternalForce += force;
	}

	public virtual void AddForce(float x, float y, float z)
	{
		AddForce(new Vector3(x, y, z));
	}

	public virtual void AddForce(Vector3 force)
	{
		AddForceInternal(force);
	}

	public virtual void AddSoftForce(Vector3 force, float frames)
	{
		frames = Mathf.Clamp(frames, 1f, 120f);
		AddForceInternal(force / frames);
		for (int i = 0; i < Mathf.RoundToInt(frames) - 1; i++)
		{
			m_SmoothForceFrame[i] += force / frames;
		}
	}

	public virtual void StopSoftForce()
	{
		for (int i = 0; i < 120 && !(m_SmoothForceFrame[i] == Vector3.zero); i++)
		{
			m_SmoothForceFrame[i] = Vector3.zero;
		}
	}

	public virtual void Stop()
	{
		mCharacterController.Move(Vector3.zero);
		m_MotorThrottle = Vector3.zero;
		m_ExternalForce = Vector3.zero;
		StopSoftForce();
		m_MoveVector = Vector2.zero;
		m_FallSpeed = 0f;
		m_LastFallSpeed = 0f;
		m_HighestFallSpeed = 0f;
		m_SmoothPosition = base.Transform.position;
	}

	public virtual void DeflectDownForce()
	{
		if (GroundAngle > PhysicsSlopeSlideLimit)
		{
			m_SlopeSlideSpeed = m_FallImpact * 0.25f;
		}
		if (GroundAngle > 85f)
		{
			m_MotorThrottle += vp_3DUtility.HorizontalVector(GroundNormal * m_FallImpact);
			m_Grounded = false;
		}
	}

	protected virtual void DeflectHorizontalForce()
	{
		m_PredictedPos.y = base.Transform.position.y;
		m_PrevPos.y = base.Transform.position.y;
		m_PrevDir = (m_PredictedPos - m_PrevPos).normalized;
		CapsuleBottom = m_PrevPos + Vector3.up * mCharacterController.radius;
		CapsuleTop = CapsuleBottom + Vector3.up * (mCharacterController.height - mCharacterController.radius * 2f);
		if (Physics.CapsuleCast(CapsuleBottom, CapsuleTop, mCharacterController.radius, m_PrevDir, out m_WallHit, Vector3.Distance(m_PrevPos, m_PredictedPos), -1749041173))
		{
			m_NewDir = Vector3.Cross(m_WallHit.normal, Vector3.up).normalized;
			if (Vector3.Dot(Vector3.Cross(m_WallHit.point - base.Transform.position, m_PrevPos - base.Transform.position), Vector3.up) > 0f)
			{
				m_NewDir = -m_NewDir;
			}
			m_ForceMultiplier = Mathf.Abs(Vector3.Dot(m_PrevDir, m_NewDir)) * (1f - PhysicsWallFriction);
			if (PhysicsWallBounce > 0f)
			{
				m_NewDir = Vector3.Lerp(m_NewDir, Vector3.Reflect(m_PrevDir, m_WallHit.normal), PhysicsWallBounce);
				m_ForceMultiplier = Mathf.Lerp(m_ForceMultiplier, 1f, PhysicsWallBounce * (1f - PhysicsWallFriction));
			}
			m_ForceImpact = 0f;
			float y = m_ExternalForce.y;
			m_ExternalForce.y = 0f;
			m_ForceImpact = m_ExternalForce.magnitude;
			m_ExternalForce = m_NewDir * m_ExternalForce.magnitude * m_ForceMultiplier;
			m_ForceImpact -= m_ExternalForce.magnitude;
			for (int i = 0; i < 120 && !(m_SmoothForceFrame[i] == Vector3.zero); i++)
			{
				m_SmoothForceFrame[i] = m_SmoothForceFrame[i].magnitude * m_NewDir * m_ForceMultiplier;
			}
			m_ExternalForce.y = y;
		}
	}

	public bool CanStartJump()
	{
		if (MotorDoubleJump)
		{
			if (!MotorDoubleJumpFirst)
			{
				return true;
			}
			if (MotorDoubleJumpFirst && !MotorDoubleJumpFirstDown)
			{
				return false;
			}
			if (MotorDoubleJumpFirst && MotorDoubleJumpFirstDown && !MotorDoubleJumpSecond)
			{
				return true;
			}
			if (MotorDoubleJumpFirst && MotorDoubleJumpSecond)
			{
				return false;
			}
		}
		if ((bool)MotorFreeFly)
		{
			return true;
		}
		if (!m_Grounded)
		{
			return false;
		}
		if (!m_MotorJumpDone)
		{
			return false;
		}
		if (GroundAngle > mCharacterController.slopeLimit)
		{
			return false;
		}
		return true;
	}

	public void OnStartJump()
	{
		m_MotorJumpDone = false;
		if (MotorDoubleJump)
		{
			if (!MotorDoubleJumpFirst)
			{
				MotorDoubleJumpFirst = true;
			}
			else
			{
				m_FallSpeed = 0f;
				m_LastFallSpeed = 0f;
				m_HighestFallSpeed = 0f;
				MotorDoubleJumpSecond = true;
			}
		}
		if (!MotorFreeFly || Grounded)
		{
			m_MotorThrottle.y = MotorJumpForce;
			m_SmoothPosition.y = base.Transform.position.y;
		}
	}

	public void OnStopJump()
	{
		m_MotorJumpDone = true;
		if (MotorDoubleJump && MotorDoubleJumpFirst)
		{
			MotorDoubleJumpFirstDown = true;
		}
	}
}
