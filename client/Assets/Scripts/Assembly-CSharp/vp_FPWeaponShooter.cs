using UnityEngine;

[RequireComponent(typeof(vp_FPWeapon))]
public class vp_FPWeaponShooter : vp_Shooter
{
	protected vp_FPWeapon m_FPSWeapon;

	protected vp_FPCamera m_FPSCamera;

	public float ProjectileTapFiringRate = 0.1f;

	protected float m_LastFireTime;

	public Vector3 MotionPositionRecoil = new Vector3(0f, 0f, -0.035f);

	public Vector3 MotionRotationRecoil = new Vector3(-10f, 0f, 0f);

	public float MotionRotationRecoilDeadZone = 0.5f;

	public float MotionRotationRecoilCameraFactor;

	public float MotionPositionRecoilCameraFactor;

	public float MotionPositionReset = 0.5f;

	public float MotionRotationReset = 0.5f;

	public float MotionPositionPause = 1f;

	public float MotionRotationPause = 1f;

	public float MotionDryFireRecoil = -0.1f;

	public float MotionRecoilDelay;

	public AnimationClip AnimationFire;

	public AudioClip SoundDryFire;

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

	protected override void Awake()
	{
		base.Awake();
		m_FPSCamera = base.transform.root.GetComponentInChildren<vp_FPCamera>();
		m_OperatorTransform = m_FPSCamera.transform;
		m_NextAllowedFireTime = Time.time;
		ProjectileSpawnDelay = Mathf.Min(ProjectileSpawnDelay, ProjectileFiringRate - 0.1f);
	}

	protected override void Start()
	{
		base.Start();
		if (ProjectileFiringRate == 0f && AnimationFire != null)
		{
			ProjectileFiringRate = AnimationFire.length;
		}
		m_FPSWeapon = base.transform.GetComponent<vp_FPWeapon>();
		if (ProjectileFiringRate == 0f && AnimationFire != null)
		{
			ProjectileFiringRate = AnimationFire.length;
		}
	}

	protected override void Update()
	{
		base.Update();
		if (Player.Attack.Active)
		{
			TryFire();
		}
	}

	public override void TryFire()
	{
		if (!(Time.time < m_NextAllowedFireTime) && !Player.SetWeapon.Active && m_FPSWeapon.Wielded)
		{
			if (!Player.DepleteAmmo.Try())
			{
				DryFire();
			}
			else
			{
				Fire();
			}
		}
	}

	protected override void Fire()
	{
		m_LastFireTime = Time.time;
		if (AnimationFire != null)
		{
			m_FPSWeapon.WeaponModel.GetComponent<Animation>()[AnimationFire.name].time = 0f;
			m_FPSWeapon.WeaponModel.GetComponent<Animation>().Sample();
			m_FPSWeapon.WeaponModel.GetComponent<Animation>().Play(AnimationFire.name);
		}
		if (MotionRecoilDelay == 0f)
		{
			ApplyRecoil();
		}
		else
		{
			TimerManager.In(MotionRecoilDelay, ApplyRecoil);
		}
		base.Fire();
	}

	protected virtual void ApplyRecoil()
	{
		m_FPSWeapon.ResetSprings(MotionPositionReset, MotionRotationReset, MotionPositionPause, MotionRotationPause);
		if (MotionRotationRecoil.z == 0f)
		{
			m_FPSWeapon.AddForce2(MotionPositionRecoil, MotionRotationRecoil);
			if (MotionPositionRecoilCameraFactor != 0f)
			{
				m_FPSCamera.AddForce2(MotionPositionRecoil * MotionPositionRecoilCameraFactor);
			}
			return;
		}
		m_FPSWeapon.AddForce2(MotionPositionRecoil, Vector3.Scale(MotionRotationRecoil, Vector3.one + Vector3.back) + ((!(Random.value < 0.5f)) ? Vector3.back : Vector3.forward) * Random.Range(MotionRotationRecoil.z * MotionRotationRecoilDeadZone, MotionRotationRecoil.z));
		if (MotionPositionRecoilCameraFactor != 0f)
		{
			m_FPSCamera.AddForce2(MotionPositionRecoil * MotionPositionRecoilCameraFactor);
		}
		if (MotionRotationRecoilCameraFactor != 0f)
		{
			m_FPSCamera.AddRollForce(Random.Range(MotionRotationRecoil.z * MotionRotationRecoilDeadZone, MotionRotationRecoil.z) * MotionRotationRecoilCameraFactor * ((!(Random.value < 0.5f)) ? (-1f) : 1f));
		}
	}

	public virtual void DryFire()
	{
		m_LastFireTime = Time.time;
		m_FPSWeapon.AddForce2(MotionPositionRecoil * MotionDryFireRecoil, MotionRotationRecoil * MotionDryFireRecoil);
		if (base.Audio != null)
		{
			base.Audio.pitch = Time.timeScale;
			base.Audio.PlayOneShot(SoundDryFire);
			DisableFiring(10000000f);
		}
	}

	protected virtual void OnStop_Attack()
	{
		if (ProjectileFiringRate == 0f)
		{
			EnableFiring();
		}
		else
		{
			DisableFiring(ProjectileTapFiringRate - (Time.time - m_LastFireTime));
		}
	}
}
