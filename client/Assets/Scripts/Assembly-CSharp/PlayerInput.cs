using System;
using DG.Tweening;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
	public CryptoInt Health = 100;

	public Team PlayerTeam;

	public CryptoFloat PlayerSpeed = 0.18f;

	public bool isAwake;

	public CryptoBool Dead = true;

	public CryptoBool NoDamage;

	public CryptoBool Move = true;

	public CryptoBool Zombie;

	public CryptoBool DamageSpeed = false;

	public CryptoBool DamageForce = false;

	public CryptoBool MoveIce = false;

	public CryptoInt MaxHealth = 100;

	public CryptoBool Climb = false;

	public CryptoBool Water = false;

	[Header("UFPS")]
	public vp_FPController FPController;

	public vp_FPPlayerEventHandler FPlayerEvent;

	public vp_FPCamera FPCamera;

	[Header("Player")]
	public CharacterController mCharacterController;

	public Transform PlayerTransform;

	public Camera PlayerCamera;

	public Camera PlayerWeaponCamera;

	public PlayerWeapons PlayerWeapon;

	public ControllerManager Controller;

	public AudioClip[] PlayerFoosteps;

	[Disabled]
	public Vector2 MoveAxis;

	[Disabled]
	public Vector2 LookAxis;

	[Disabled]
	public float RotateCamera;

	[Header("Fall Damage")]
	public CryptoBool FallDamage;

	public CryptoFloat FallDamageThreshold = 10f;

	private CryptoBool FallingDamage = false;

	private float StartFallDamage;

	[Header("Bunny Hop")]
	public CryptoBool BunnyHopEnabled;

	public CryptoFloat BunnyHopSpeed = 0.4f;

	public CryptoFloat BunnyHopLerp = 5f;

	public CryptoFloat BunnyHopDefaultLerp = 0.5f;

	public CryptoFloat BunnyHopDefaultSpeed = 0.18f;

	private Tween BunnyHopTween;

	private CryptoBool BunnyHopActive = false;

	private CryptoBool BunnyHopAutoJump;

	[Header("Surf")]
	public bool SurfEnabled;

	public float SurfAcceleration = 0.0001f;

	public float SurfMaxSpeed;

	public float SurfSpeed;

	public bool Surf;

	private bool isStopSurf;

	[Header("Controller Data")]
	public CryptoFloat StepOffset;

	public CryptoFloat MotorJumpForce = 0.15f;

	public CryptoFloat MotorJumpForceDamping = 0.1f;

	[Header("Others")]
	public AudioSource m_AudioSource;

	private bool isJump;

	private int MoveTimerID;

	private bool isCursor = true;

	public static PlayerInput instance;

	public bool Grounded
	{
		get
		{
			if ((bool)Water || Surf)
			{
				return false;
			}
			return mCharacterController.isGrounded;
		}
	}

	private void Start()
	{
		instance = this;
		isAwake = true;
		Controller = base.transform.root.GetComponent<ControllerManager>();
		SetHealth(Health);
		EventManager.AddListener("UpdateSettings", UpdateSettings);
		UpdateSettings();
		mCharacterController.stepOffset = StepOffset;
		FPController.MotorJumpForce = MotorJumpForce;
		FPController.MotorJumpForceDamping = MotorJumpForceDamping;
	}

	private void OnEnable()
	{
		SkyboxManager.SetTarget(PlayerCamera.transform);
		SkyboxManager.SetParent(PlayerTransform);
		vp_FPCamera fPCamera = FPCamera;
		fPCamera.BobStepCallback = (vp_FPCamera.BobStepDelegate)Delegate.Combine(fPCamera.BobStepCallback, new vp_FPCamera.BobStepDelegate(PlayFoosteps));
		InputManager.GetButtonDownEvent += GetButtonDown;
		InputManager.GetButtonUpEvent += GetButtonUp;
		InputManager.GetAxisEvent += GetAxis;
		if (GameManager.isStartDamage())
		{
			StartNoDamage();
		}
		if ((bool)Climb)
		{
			SetClimb(false);
		}
		if ((bool)Water)
		{
			SetWater(false);
		}
		if ((bool)MoveIce)
		{
			SetMoveIce(false);
		}
		Dead = false;
	}

	private void OnDisable()
	{
		vp_FPCamera fPCamera = FPCamera;
		fPCamera.BobStepCallback = (vp_FPCamera.BobStepDelegate)Delegate.Remove(fPCamera.BobStepCallback, new vp_FPCamera.BobStepDelegate(PlayFoosteps));
		InputManager.GetButtonDownEvent -= GetButtonDown;
		InputManager.GetButtonUpEvent -= GetButtonUp;
		InputManager.GetAxisEvent -= GetAxis;
		MoveAxis = Vector2.zero;
		LookAxis = Vector2.zero;
		if ((bool)BunnyHopActive)
		{
			BunnyHopAutoJump = false;
		}
		if (SurfEnabled)
		{
			SurfSpeed = 0f;
		}
		Dead = true;
		isJump = false;
	}

	private void OnDestroy()
	{
		SkyboxManager.Deactive();
	}

	private void OnApplicationFocus(bool pause)
	{
		if (pause)
		{
			if (mCharacterController.stepOffset != (float)StepOffset)
			{
				Application.Quit();
			}
			if (FPController.MotorJumpForce != (float)MotorJumpForce)
			{
				Application.Quit();
			}
			if (FPController.MotorJumpForceDamping != (float)MotorJumpForceDamping)
			{
				Application.Quit();
			}
		}
	}

	private void GetButtonDown(string name)
	{
		if (name == "Jump")
		{
			isJump = true;
		}
	}

	private void GetButtonUp(string name)
	{
		if (name == "Jump")
		{
			isJump = false;
		}
	}

	private void GetAxis(string name, float value)
	{
		switch (name)
		{
		case "Horizontal":
			MoveAxis.x = value;
			break;
		case "Vertical":
			MoveAxis.y = value;
			break;
		case "Mouse X":
			LookAxis.x = value;
			break;
		case "Mouse Y":
			LookAxis.y = value;
			break;
		}
	}

	private void Update()
	{
		UpdateMove();
		UpdateLook();
		UpdateJump();
		UpdateBunnyHop();
		UpdateSurf();
		UpdateFallDamage();
		UpdateVelocity();
	}

	private void UpdateCursor()
	{
		if (Input.GetKeyDown(KeyCode.P))
		{
			isCursor = !isCursor;
		}
		Screen.lockCursor = isCursor;
		Cursor.visible = !isCursor;
	}

	private void UpdateMove()
	{
		if ((bool)Move)
		{
			if ((bool)Climb || (bool)Water)
			{
				MoveAxis /= 2.5f;
			}
			FPController.OnValue_InputMoveVector = MoveAxis;
		}
	}

	private void UpdateLook()
	{
		if (PlayerWeapon.isScope)
		{
			LookAxis *= (float)PlayerWeapon.GetSelectedWeaponData().ScopeSensitivity;
		}
		FPCamera.UpdateLook(LookAxis);
		RotateCamera = (0f - FPCamera.Pitch) / 60f;
	}

	private void UpdateJump()
	{
		if ((bool)BunnyHopAutoJump)
		{
			if (Grounded)
			{
				if (FPController.CanStartJump())
				{
					FPController.OnStartJump();
				}
			}
			else
			{
				FPController.OnStopJump();
			}
		}
		else if (isJump && !Climb && !Water)
		{
			if (FPController.CanStartJump())
			{
				FPController.OnStartJump();
			}
		}
		else
		{
			FPController.OnStopJump();
		}
	}

	private void UpdateBunnyHop()
	{
		if (!BunnyHopEnabled)
		{
			return;
		}
		if (!Grounded && mCharacterController.velocity.sqrMagnitude > 20f)
		{
			if (!BunnyHopActive)
			{
				BunnyHopActive = true;
				if (BunnyHopTween != null)
				{
					BunnyHopTween.Kill();
				}
				BunnyHopTween = DOTween.To(() => FPController.MotorAcceleration, (float x) =>
				{
					FPController.MotorAcceleration = x;
				}, BunnyHopSpeed, BunnyHopLerp);
			}
		}
		else if ((bool)BunnyHopActive)
		{
			BunnyHopActive = false;
			if (BunnyHopTween != null)
			{
				BunnyHopTween.Kill();
			}
			BunnyHopTween = DOTween.To(() => FPController.MotorAcceleration, (float x) =>
			{
				FPController.MotorAcceleration = x;
			}, BunnyHopDefaultSpeed, BunnyHopDefaultLerp);
		}
	}

	private void UpdateSurf()
	{
		if (!SurfEnabled)
		{
			return;
		}
		if (isStopSurf)
		{
			Surf = false;
			SurfSpeed = 0f;
			FPController.Stop();
			isStopSurf = false;
			return;
		}
		if (FPController.GroundAngle > 30f && mCharacterController.isGrounded)
		{
			if (!Surf)
			{
				SurfSpeed += SurfAcceleration + mCharacterController.velocity.magnitude * SurfAcceleration;
			}
			else
			{
				SurfSpeed += SurfAcceleration;
			}
			Surf = true;
		}
		else if (FPController.GroundAngle < 30f && mCharacterController.isGrounded)
		{
			Surf = false;
			SurfSpeed = 0f;
		}
		else if (SurfSpeed > 0f)
		{
			Surf = false;
			SurfSpeed -= SurfAcceleration / 3f;
		}
		SurfSpeed = Mathf.Clamp(SurfSpeed, 0f, SurfMaxSpeed);
		if (SurfSpeed > 0f)
		{
			FPController.AddForce(FPCamera.Forward * (SurfSpeed * 0.0001f + MoveAxis.y / 100f));
		}
	}

	private void UpdateFallDamage()
	{
		if (!FallDamage)
		{
			return;
		}
		if (Grounded)
		{
			if ((bool)FallingDamage)
			{
				FallingDamage = false;
				if (PlayerTransform.position.y < StartFallDamage - (float)FallDamageThreshold)
				{
					int damage = (int)(StartFallDamage - PlayerTransform.position.y);
					DamageInfo damageInfo = DamageInfo.Get(damage, Vector3.zero, Team.None, 0, -1, false);
					Damage(damageInfo);
				}
			}
		}
		else if (!FallingDamage)
		{
			FallingDamage = true;
			StartFallDamage = PlayerTransform.position.y;
		}
	}

	private void UpdateVelocity()
	{
		if (mCharacterController.velocity.y < -100f)
		{
			DamageInfo damageInfo = DamageInfo.Get(1000, Vector3.zero, Team.None, 0, -1, false);
			Damage(damageInfo);
		}
	}

	public void SetBunnyHopAutoJump(bool active)
	{
		BunnyHopAutoJump = active;
	}

	public void Damage(DamageInfo damageInfo)
	{
		if ((bool)Dead || (bool)NoDamage)
		{
			damageInfo.Dispose();
			return;
		}
		Health = (int)Health - damageInfo.Damage;
		Health = Mathf.Clamp(Health, 0, MaxHealth);
		UIGameManager.SetHealthLabel(Health);
		if (damageInfo.AttackPosition != Vector3.zero)
		{
			UIDamage.Damage(damageInfo.AttackPosition, FPCamera.Transform);
			if ((bool)DamageForce)
			{
				float num = (1f - Vector3.Distance(PlayerTransform.position, damageInfo.AttackPosition) / 100f) / -10f;
				if (num > 0f)
				{
					num = 0f;
				}
				else if (num > 1f)
				{
					num = 1f;
				}
				Vector3 force = (damageInfo.AttackPosition - PlayerTransform.position).normalized * num;
				FPController.AddForce(force);
			}
		}
		if ((int)Health <= 0)
		{
			GameManager.OnDeadPlayer(damageInfo);
			PlayerWeapon.DeactiveScope();
		}
		else
		{
			if ((bool)DamageSpeed)
			{
				FPController.MotorAcceleration = 0.13f;
				if (DOTween.IsTweening("DamageSpeed"))
				{
					DOTween.Kill("DamageSpeed");
				}
				DOTween.To(() => FPController.MotorAcceleration, (float x) =>
				{
					FPController.MotorAcceleration = x;
				}, 0.19f, 1.5f).SetId("DamageSpeed");
			}
			FPCamera.AddRollForce(UnityEngine.Random.Range(-2, 2));
			SkyboxManager.SetFixedUpdate(1f);
		}
		damageInfo.Dispose();
	}

	private void PlayFoosteps()
	{
		if (!Water && !Climb && Grounded)
		{
			UpdateFoosteps();
		}
	}

	public void UpdateFoosteps()
	{
		if (Settings.Audio)
		{
			AudioClip clip = PlayerFoosteps[UnityEngine.Random.Range(0, PlayerFoosteps.Length)];
			m_AudioSource.pitch = UnityEngine.Random.Range(1f, 1.5f);
			m_AudioSource.clip = clip;
			m_AudioSource.Play();
		}
	}

	public void StartNoDamage()
	{
		NoDamage = true;
		try
		{
			float startDamageTime = GameManager.GetStartDamageTime();
			if (startDamageTime != -1f)
			{
				TimerManager.In(GameManager.GetStartDamageTime(), () =>
				{
					NoDamage = false;
				});
			}
		}
		catch
		{
			NoDamage = false;
		}
	}

	private void UpdateSettings()
	{
		TimerManager.In(0.1f, () =>
		{
			float num = Settings.Sensitivity * 16f;
			FPCamera.MouseSensitivity = new Vector2(num, num);
			PlayerWeaponCamera.enabled = Settings.ShowWeapon;
		});
	}

	public void SetHealth(int health)
	{
		Health = health;
		UIGameManager.SetHealthLabel(Health);
	}

	public void SetMove(bool move)
	{
		Move = move;
	}

	public void SetMove(bool move, float duration)
	{
		Move = move;
		if (TimerManager.IsActive(MoveTimerID))
		{
			TimerManager.Cancel(MoveTimerID);
		}
		MoveTimerID = TimerManager.In(duration, () =>
		{
			Move = !move;
		});
	}

	public void SetMoveIce(bool active)
	{
		MoveIce = active;
		if (active)
		{
			DOTween.To(() => FPController.MotorDamping, (float x) =>
			{
				FPController.MotorDamping = x;
			}, 0.02f, 0.2f);
		}
		else
		{
			DOTween.To(() => FPController.MotorDamping, (float x) =>
			{
				FPController.MotorDamping = x;
			}, 0.17f, 0.2f);
		}
	}

	public void UpdatePlayerSpeed(float speed)
	{
		PlayerSpeed = speed;
		FPController.MotorAcceleration = PlayerSpeed;
	}

	public void SetPlayerSpeed(float mass)
	{
		FPController.MotorAcceleration = (float)PlayerSpeed - mass;
	}

	public void SetClimb(bool active)
	{
		Climb = active;
		if ((bool)Climb)
		{
			FPController.Stop();
			FPController.PhysicsGravityModifier = 0f;
			FPController.MotorFreeFly = true;
		}
		else
		{
			FPController.PhysicsGravityModifier = 0.2f;
			FPController.MotorFreeFly = false;
		}
	}

	public void SetWater(bool active)
	{
		Water = active;
		if ((bool)Water)
		{
			FPController.PhysicsGravityModifier = 0.01f;
			FPController.MotorFreeFly = true;
		}
		else
		{
			FPController.PhysicsGravityModifier = 0.2f;
			FPController.MotorFreeFly = false;
		}
	}

	public void StopSurf()
	{
		if (Surf)
		{
			isStopSurf = true;
		}
	}
}
