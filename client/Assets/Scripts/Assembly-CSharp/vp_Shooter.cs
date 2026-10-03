using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class vp_Shooter : vp_Component
{
	protected CharacterController m_CharacterController;

	protected Transform m_OperatorTransform;

	public GameObject ProjectilePrefab;

	public float ProjectileScale = 1f;

	public float ProjectileFiringRate = 0.3f;

	public float ProjectileSpawnDelay;

	public int ProjectileCount = 1;

	public float ProjectileSpread;

	protected float m_NextAllowedFireTime;

	public Vector3 MuzzleFlashPosition = Vector3.zero;

	public Vector3 MuzzleFlashScale = Vector3.one;

	public float MuzzleFlashFadeSpeed = 0.075f;

	public GameObject MuzzleFlashPrefab;

	public float MuzzleFlashDelay;

	protected GameObject m_MuzzleFlash;

	public GameObject ShellPrefab;

	public float ShellScale = 1f;

	public Vector3 ShellEjectDirection = new Vector3(1f, 1f, 1f);

	public Vector3 ShellEjectPosition = new Vector3(1f, 0f, 1f);

	public float ShellEjectVelocity = 0.2f;

	public float ShellEjectDelay;

	public float ShellEjectSpin;

	public AudioClip SoundFire;

	public float SoundFireDelay;

	public Vector2 SoundFirePitch = new Vector2(1f, 1f);

	public GameObject MuzzleFlash
	{
		get
		{
			return m_MuzzleFlash;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		m_OperatorTransform = base.Transform;
		m_CharacterController = m_OperatorTransform.root.GetComponentInChildren<CharacterController>();
		m_NextAllowedFireTime = Time.time;
		ProjectileSpawnDelay = Mathf.Min(ProjectileSpawnDelay, ProjectileFiringRate - 0.1f);
	}

	protected override void Start()
	{
		base.Start();
		if (MuzzleFlashPrefab != null)
		{
			m_MuzzleFlash = (GameObject)vp_Utility.Instantiate(MuzzleFlashPrefab, m_OperatorTransform.position, m_OperatorTransform.rotation);
			m_MuzzleFlash.name = base.transform.name + "MuzzleFlash";
			m_MuzzleFlash.transform.parent = m_OperatorTransform;
		}
		base.Audio.playOnAwake = false;
		base.Audio.dopplerLevel = 0f;
		RefreshDefaultState();
		Refresh();
	}

	public virtual void TryFire()
	{
		if (!(Time.time < m_NextAllowedFireTime))
		{
			Fire();
		}
	}

	protected virtual void Fire()
	{
		m_NextAllowedFireTime = Time.time + ProjectileFiringRate;
		if (SoundFireDelay == 0f)
		{
			PlayFireSound();
		}
		else
		{
			TimerManager.In(SoundFireDelay, PlayFireSound);
		}
		if (ProjectileSpawnDelay == 0f)
		{
			SpawnProjectiles();
		}
		else
		{
			TimerManager.In(ProjectileSpawnDelay, SpawnProjectiles);
		}
		if (ShellEjectDelay == 0f)
		{
			EjectShell();
		}
		else
		{
			TimerManager.In(ShellEjectDelay, EjectShell);
		}
		if (MuzzleFlashDelay == 0f)
		{
			ShowMuzzleFlash();
		}
		else
		{
			TimerManager.In(MuzzleFlashDelay, ShowMuzzleFlash);
		}
	}

	protected virtual void PlayFireSound()
	{
		if (!(base.Audio == null))
		{
			base.Audio.pitch = Random.Range(SoundFirePitch.x, SoundFirePitch.y) * Time.timeScale;
			base.Audio.clip = SoundFire;
			base.Audio.Play();
		}
	}

	protected virtual void SpawnProjectiles()
	{
		for (int i = 0; i < ProjectileCount; i++)
		{
			if (ProjectilePrefab != null)
			{
				GameObject gameObject = null;
				gameObject = (GameObject)vp_Utility.Instantiate(ProjectilePrefab, m_OperatorTransform.position, m_OperatorTransform.rotation);
				gameObject.transform.localScale = new Vector3(ProjectileScale, ProjectileScale, ProjectileScale);
				gameObject.transform.Rotate(0f, 0f, Random.Range(0, 360));
				gameObject.transform.Rotate(0f, Random.Range(0f - ProjectileSpread, ProjectileSpread), 0f);
			}
		}
	}

	protected virtual void ShowMuzzleFlash()
	{
		if (!(m_MuzzleFlash == null))
		{
			m_MuzzleFlash.SendMessage("Shoot", SendMessageOptions.DontRequireReceiver);
		}
	}

	protected virtual void EjectShell()
	{
		if (ShellPrefab == null)
		{
			return;
		}
		GameObject gameObject = null;
		gameObject = (GameObject)vp_Utility.Instantiate(ShellPrefab, m_OperatorTransform.position + m_OperatorTransform.TransformDirection(ShellEjectPosition), m_OperatorTransform.rotation);
		gameObject.transform.localScale = new Vector3(ShellScale, ShellScale, ShellScale);
		vp_Layer.Set(gameObject.gameObject, 29);
		if ((bool)gameObject.rigidbody)
		{
			Vector3 force = base.transform.TransformDirection(ShellEjectDirection) * ShellEjectVelocity;
			gameObject.rigidbody.AddForce(force, ForceMode.Impulse);
		}
		if ((bool)m_CharacterController)
		{
			Vector3 velocity = m_CharacterController.velocity;
			gameObject.rigidbody.AddForce(velocity, ForceMode.VelocityChange);
		}
		if (ShellEjectSpin > 0f)
		{
			if (Random.value > 0.5f)
			{
				gameObject.rigidbody.AddRelativeTorque(-Random.rotation.eulerAngles * ShellEjectSpin);
			}
			else
			{
				gameObject.rigidbody.AddRelativeTorque(Random.rotation.eulerAngles * ShellEjectSpin);
			}
		}
	}

	public virtual void DisableFiring(float seconds = 10000000f)
	{
		m_NextAllowedFireTime = Time.time + seconds;
	}

	public virtual void EnableFiring()
	{
		m_NextAllowedFireTime = Time.time;
	}

	public override void Refresh()
	{
		if (m_MuzzleFlash != null)
		{
			m_MuzzleFlash.transform.localPosition = MuzzleFlashPosition;
			m_MuzzleFlash.transform.localScale = MuzzleFlashScale;
			m_MuzzleFlash.SendMessage("SetFadeSpeed", MuzzleFlashFadeSpeed, SendMessageOptions.DontRequireReceiver);
		}
	}

	public override void Activate()
	{
		base.Activate();
		if (m_MuzzleFlash != null)
		{
			vp_Utility.Activate(m_MuzzleFlash);
		}
	}

	public override void Deactivate()
	{
		base.Deactivate();
		if (m_MuzzleFlash != null)
		{
			vp_Utility.Activate(m_MuzzleFlash, false);
		}
	}
}
