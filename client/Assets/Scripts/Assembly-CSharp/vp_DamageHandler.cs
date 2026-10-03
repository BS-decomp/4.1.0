using UnityEngine;

public class vp_DamageHandler : MonoBehaviour
{
	public float MaxHealth = 1f;

	public GameObject[] DeathSpawnObjects;

	public float MinDeathDelay;

	public float MaxDeathDelay;

	public float m_CurrentHealth;

	protected AudioSource m_Audio;

	public AudioClip DeathSound;

	public float ImpactDamageThreshold = 10f;

	public float ImpactDamageMultiplier;

	[HideInInspector]
	public bool Respawns;

	[HideInInspector]
	public float MinRespawnTime = -99999f;

	[HideInInspector]
	public float MaxRespawnTime = -99999f;

	[HideInInspector]
	public float RespawnCheckRadius = -99999f;

	[HideInInspector]
	public AudioClip RespawnSound;

	[HideInInspector]
	public GameObject DeathEffect;

	protected Vector3 m_StartPosition;

	protected Quaternion m_StartRotation;

	protected virtual void Awake()
	{
		m_Audio = base.GetComponent<AudioSource>();
		m_CurrentHealth = MaxHealth;
		CheckForObsoleteParams();
	}

	protected virtual void OnEnable()
	{
	}

	protected virtual void OnDisable()
	{
	}

	public virtual void Damage(float damage)
	{
		if (!base.enabled || !vp_Utility.IsActive(base.gameObject) || m_CurrentHealth <= 0f)
		{
			return;
		}
		m_CurrentHealth = Mathf.Min(m_CurrentHealth - damage, MaxHealth);
		if (m_CurrentHealth <= 0f)
		{
			vp_Timer.In(Random.Range(MinDeathDelay, MaxDeathDelay), () =>
			{
				SendMessage("Die");
			});
		}
	}

	public virtual void Die()
	{
		if (!base.enabled || !vp_Utility.IsActive(base.gameObject))
		{
			return;
		}
		if (m_Audio != null)
		{
			m_Audio.pitch = Time.timeScale;
			m_Audio.PlayOneShot(DeathSound);
		}
		RemoveBulletHoles();
		vp_Utility.Activate(base.gameObject, false);
		GameObject[] deathSpawnObjects = DeathSpawnObjects;
		foreach (GameObject gameObject in deathSpawnObjects)
		{
			if (gameObject != null)
			{
				vp_Utility.Instantiate(gameObject, base.transform.position, base.transform.rotation);
			}
		}
	}

	protected virtual void Reset()
	{
		m_CurrentHealth = MaxHealth;
	}

	protected virtual void RemoveBulletHoles()
	{
		vp_HitscanBullet[] componentsInChildren = GetComponentsInChildren<vp_HitscanBullet>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			vp_Utility.Destroy(componentsInChildren[i].gameObject);
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		float num = collision.relativeVelocity.sqrMagnitude * 0.1f;
		float num2 = ((!(num > ImpactDamageThreshold)) ? 0f : (num * ImpactDamageMultiplier));
		if (num2 > 0f)
		{
			if (m_CurrentHealth - num2 <= 0f)
			{
				MaxDeathDelay = (MinDeathDelay = 0f);
			}
			Damage(num2);
		}
	}

	protected virtual void Respawn()
	{
	}

	protected virtual void Reactivate()
	{
	}

	private void CheckForObsoleteParams()
	{
		if (DeathEffect != null)
		{
			Debug.LogWarning(string.Concat(this, "'DeathEffect' is obsolete! Please use the 'DeathSpawnObjects' array instead."));
		}
		string text = string.Empty;
		if (Respawns)
		{
			text += "Respawns, ";
		}
		if (MinRespawnTime != -99999f)
		{
			text += "MinRespawnTime, ";
		}
		if (MaxRespawnTime != -99999f)
		{
			text += "MaxRespawnTime, ";
		}
		if (RespawnCheckRadius != -99999f)
		{
			text += "RespawnCheckRadius, ";
		}
		if (RespawnSound != null)
		{
			text += "RespawnSound, ";
		}
		if (text != string.Empty)
		{
			text = text.Remove(text.LastIndexOf(", "));
			Debug.LogWarning(string.Format(string.Concat("Warning + (", this, ") The following parameters are obsolete: \"{0}\". Creating a temp vp_Respawner component. To remove this warning, see the UFPS menu -> Wizards -> Convert Old DamageHandlers."), text));
			CreateTempRespawner();
		}
	}

	public bool CreateTempRespawner()
	{
		if ((bool)GetComponent<vp_Respawner>() || (bool)GetComponent<vp_PlayerRespawner>())
		{
			DisableOldParams();
			return false;
		}
		CreateRespawnerForDamageHandler(this);
		DisableOldParams();
		return true;
	}

	public static int GenerateRespawnersForAllDamageHandlers()
	{
		vp_PlayerDamageHandler[] array = Object.FindObjectsOfType(typeof(vp_PlayerDamageHandler)) as vp_PlayerDamageHandler[];
		if (array != null && array.Length > 0)
		{
			vp_PlayerDamageHandler[] array2 = array;
			foreach (vp_PlayerDamageHandler vp_PlayerDamageHandler2 in array2)
			{
				if (!(vp_PlayerDamageHandler2.transform.GetComponent<vp_FPPlayerEventHandler>() == null))
				{
					vp_FPPlayerDamageHandler vp_FPPlayerDamageHandler2 = vp_PlayerDamageHandler2.gameObject.AddComponent<vp_FPPlayerDamageHandler>();
					vp_FPPlayerDamageHandler2.AllowFallDamage = vp_PlayerDamageHandler2.AllowFallDamage;
					vp_FPPlayerDamageHandler2.DeathEffect = vp_PlayerDamageHandler2.DeathEffect;
					vp_FPPlayerDamageHandler2.DeathSound = vp_PlayerDamageHandler2.DeathSound;
					vp_FPPlayerDamageHandler2.DeathSpawnObjects = vp_PlayerDamageHandler2.DeathSpawnObjects;
					vp_FPPlayerDamageHandler2.FallImpactPitch = vp_PlayerDamageHandler2.FallImpactPitch;
					vp_FPPlayerDamageHandler2.FallImpactSounds = vp_PlayerDamageHandler2.FallImpactSounds;
					vp_FPPlayerDamageHandler2.FallImpactThreshold = vp_PlayerDamageHandler2.FallImpactThreshold;
					vp_FPPlayerDamageHandler2.ImpactDamageMultiplier = vp_PlayerDamageHandler2.ImpactDamageMultiplier;
					vp_FPPlayerDamageHandler2.ImpactDamageThreshold = vp_PlayerDamageHandler2.ImpactDamageThreshold;
					vp_FPPlayerDamageHandler2.m_Audio = vp_PlayerDamageHandler2.m_Audio;
					vp_FPPlayerDamageHandler2.m_CurrentHealth = vp_PlayerDamageHandler2.m_CurrentHealth;
					vp_FPPlayerDamageHandler2.m_StartPosition = vp_PlayerDamageHandler2.m_StartPosition;
					vp_FPPlayerDamageHandler2.m_StartRotation = vp_PlayerDamageHandler2.m_StartRotation;
					vp_FPPlayerDamageHandler2.MaxDeathDelay = vp_PlayerDamageHandler2.MaxDeathDelay;
					vp_FPPlayerDamageHandler2.MaxHealth = vp_PlayerDamageHandler2.MaxHealth;
					vp_FPPlayerDamageHandler2.MaxRespawnTime = vp_PlayerDamageHandler2.MaxRespawnTime;
					vp_FPPlayerDamageHandler2.MinDeathDelay = vp_PlayerDamageHandler2.MinDeathDelay;
					vp_FPPlayerDamageHandler2.MinRespawnTime = vp_PlayerDamageHandler2.MinRespawnTime;
					vp_FPPlayerDamageHandler2.RespawnCheckRadius = vp_PlayerDamageHandler2.RespawnCheckRadius;
					vp_FPPlayerDamageHandler2.Respawns = vp_PlayerDamageHandler2.Respawns;
					vp_FPPlayerDamageHandler2.RespawnSound = vp_PlayerDamageHandler2.RespawnSound;
					Object.DestroyImmediate(vp_PlayerDamageHandler2);
				}
			}
		}
		vp_DamageHandler[] array3 = Object.FindObjectsOfType(typeof(vp_DamageHandler)) as vp_DamageHandler[];
		vp_DamageHandler[] array4 = Object.FindObjectsOfType(typeof(vp_FPPlayerDamageHandler)) as vp_DamageHandler[];
		int num = 0;
		vp_DamageHandler[] array5 = array3;
		foreach (vp_DamageHandler vp_DamageHandler2 in array5)
		{
			if (vp_DamageHandler2.CreateTempRespawner())
			{
				num++;
			}
		}
		vp_DamageHandler[] array6 = array4;
		foreach (vp_DamageHandler vp_DamageHandler3 in array6)
		{
			if (vp_DamageHandler3.CreateTempRespawner())
			{
				num++;
			}
		}
		return num;
	}

	private void DisableOldParams()
	{
		Respawns = false;
		MinRespawnTime = -99999f;
		MaxRespawnTime = -99999f;
		RespawnCheckRadius = -99999f;
		RespawnSound = null;
	}

	private static void CreateRespawnerForDamageHandler(vp_DamageHandler damageHandler)
	{
		if ((bool)damageHandler.gameObject.GetComponent<vp_Respawner>() || (bool)damageHandler.gameObject.GetComponent<vp_PlayerRespawner>())
		{
			return;
		}
		vp_Respawner vp_Respawner2 = null;
		vp_Respawner2 = ((!(damageHandler is vp_FPPlayerDamageHandler)) ? damageHandler.gameObject.AddComponent<vp_Respawner>() : damageHandler.gameObject.AddComponent<vp_PlayerRespawner>());
		if (!(vp_Respawner2 == null))
		{
			if (damageHandler.MinRespawnTime != -99999f)
			{
				vp_Respawner2.MinRespawnTime = damageHandler.MinRespawnTime;
			}
			if (damageHandler.MaxRespawnTime != -99999f)
			{
				vp_Respawner2.MaxRespawnTime = damageHandler.MaxRespawnTime;
			}
			if (damageHandler.RespawnCheckRadius != -99999f)
			{
				vp_Respawner2.ObstructionRadius = damageHandler.RespawnCheckRadius;
			}
			if (damageHandler.RespawnSound != null)
			{
				vp_Respawner2.SpawnSound = damageHandler.RespawnSound;
			}
		}
	}
}
