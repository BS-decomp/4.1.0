using System.Collections.Generic;
using UnityEngine;

public class vp_PlayerDamageHandler : vp_DamageHandler
{
	private vp_PlayerEventHandler m_Player;

	public bool AllowFallDamage = true;

	public float FallImpactThreshold = 0.15f;

	public bool DeathOnFallImpactThreshold;

	public Vector2 FallImpactPitch = new Vector2(1f, 1.5f);

	public List<AudioClip> FallImpactSounds = new List<AudioClip>();

	protected float m_FallImpactMultiplier = 2f;

	protected vp_PlayerEventHandler Player
	{
		get
		{
			if (m_Player == null)
			{
				m_Player = base.transform.GetComponent<vp_PlayerEventHandler>();
			}
			return m_Player;
		}
	}

	protected virtual float OnValue_Health
	{
		get
		{
			return m_CurrentHealth;
		}
		set
		{
			m_CurrentHealth = Mathf.Min(value, MaxHealth);
		}
	}

	protected override void OnEnable()
	{
		if (Player != null)
		{
			Player.Register(this);
		}
	}

	protected override void OnDisable()
	{
		if (Player != null)
		{
			Player.Unregister(this);
		}
	}

	public override void Die()
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
		GameObject[] deathSpawnObjects = DeathSpawnObjects;
		foreach (GameObject gameObject in deathSpawnObjects)
		{
			if (gameObject != null)
			{
				vp_Utility.Instantiate(gameObject, base.transform.position, base.transform.rotation);
			}
		}
	}

	protected override void Reset()
	{
		base.Reset();
		if (Application.isPlaying)
		{
			Player.Dead.Stop();
			if (m_Audio != null)
			{
				m_Audio.pitch = Time.timeScale;
				m_Audio.PlayOneShot(RespawnSound);
			}
		}
	}

	protected virtual void OnMessage_FallImpact(float impact)
	{
		if (!Player.Dead.Active && AllowFallDamage && !(impact <= FallImpactThreshold))
		{
			vp_AudioUtility.PlayRandomSound(m_Audio, FallImpactSounds, FallImpactPitch);
			float damage = Mathf.Abs((!DeathOnFallImpactThreshold) ? (MaxHealth * impact) : MaxHealth);
			Damage(damage);
		}
	}
}
