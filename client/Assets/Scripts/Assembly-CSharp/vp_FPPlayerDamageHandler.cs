using UnityEngine;

[RequireComponent(typeof(vp_FPPlayerEventHandler))]
public class vp_FPPlayerDamageHandler : vp_PlayerDamageHandler
{
	private vp_FPPlayerEventHandler m_FPPlayer;

	protected vp_FPPlayerEventHandler FPPlayer
	{
		get
		{
			if (m_FPPlayer == null)
			{
				m_FPPlayer = base.transform.GetComponent<vp_FPPlayerEventHandler>();
			}
			return m_FPPlayer;
		}
	}

	protected override void OnEnable()
	{
		if (FPPlayer != null)
		{
			FPPlayer.Register(this);
		}
	}

	protected override void OnDisable()
	{
		if (FPPlayer != null)
		{
			FPPlayer.Unregister(this);
		}
	}

	protected virtual void Update()
	{
		if (FPPlayer.Dead.Active && Time.timeScale < 1f)
		{
			vp_TimeUtility.FadeTimeScale(1f, 0.05f);
		}
	}

	public override void Damage(float damage)
	{
		if (base.enabled && vp_Utility.IsActive(base.gameObject))
		{
			base.Damage(damage);
		}
	}

	public override void Die()
	{
		base.Die();
		if (base.enabled && vp_Utility.IsActive(base.gameObject))
		{
			FPPlayer.AllowGameplayInput.Set(false);
		}
	}

	protected override void Reset()
	{
		base.Reset();
		if (Application.isPlaying)
		{
			FPPlayer.AllowGameplayInput.Set(true);
		}
	}
}
