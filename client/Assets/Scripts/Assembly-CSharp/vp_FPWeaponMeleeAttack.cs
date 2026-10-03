using System.Collections.Generic;
using UnityEngine;

public class vp_FPWeaponMeleeAttack : vp_Component
{
	protected vp_FPWeapon m_Weapon;

	protected vp_FPController m_Controller;

	protected vp_FPCamera m_Camera;

	public string WeaponStatePull = "Pull";

	public string WeaponStateSwing = "Swing";

	public float SwingDelay = 0.5f;

	public float SwingDuration = 0.5f;

	public float SwingRate = 1f;

	protected float m_NextAllowedSwingTime;

	public int SwingSoftForceFrames = 50;

	public Vector3 SwingPositionSoftForce = new Vector3(-0.5f, -0.1f, 0.3f);

	public Vector3 SwingRotationSoftForce = new Vector3(50f, -25f, 0f);

	public float ImpactTime = 0.11f;

	public Vector3 ImpactPositionSpringRecoil = new Vector3(0.01f, 0.03f, -0.05f);

	public Vector3 ImpactPositionSpring2Recoil = Vector3.zero;

	public Vector3 ImpactRotationSpringRecoil = Vector3.zero;

	public Vector3 ImpactRotationSpring2Recoil = new Vector3(0f, 0f, 10f);

	public string DamageMethodName = "Damage";

	public float Damage = 5f;

	public float DamageRadius = 0.3f;

	public float DamageRange = 2f;

	public float DamageForce = 1000f;

	public bool AttackPickRandomState = true;

	protected int m_AttackCurrent;

	public float SparkFactor = 0.1f;

	public GameObject m_DustPrefab;

	public GameObject m_SparkPrefab;

	public GameObject m_DebrisPrefab;

	public List<Object> SoundSwing = new List<Object>();

	public List<Object> SoundImpact = new List<Object>();

	public Vector2 SoundSwingPitch = new Vector2(0.5f, 1.5f);

	public Vector2 SoundImpactPitch = new Vector2(1f, 1.5f);

	private vp_Timer.Handle SwingDelayTimer = new vp_Timer.Handle();

	private vp_Timer.Handle ImpactTimer = new vp_Timer.Handle();

	private vp_Timer.Handle SwingDurationTimer = new vp_Timer.Handle();

	private vp_Timer.Handle ResetTimer = new vp_Timer.Handle();

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

	protected override void Start()
	{
		base.Start();
		m_Controller = (vp_FPController)base.Root.GetComponent(typeof(vp_FPController));
		m_Camera = (vp_FPCamera)base.Root.GetComponentInChildren(typeof(vp_FPCamera));
		m_Weapon = (vp_FPWeapon)base.Transform.GetComponent(typeof(vp_FPWeapon));
	}

	protected override void Update()
	{
		base.Update();
		UpdateAttack();
	}

	protected void UpdateAttack()
	{
		if (!Player.Attack.Active || Player.SetWeapon.Active || m_Weapon == null || !m_Weapon.Wielded || Time.time < m_NextAllowedSwingTime)
		{
			return;
		}
		m_NextAllowedSwingTime = Time.time + SwingRate;
		if (AttackPickRandomState)
		{
			PickAttack();
		}
		m_Weapon.SetState(WeaponStatePull);
		m_Weapon.Refresh();
		vp_Timer.In(SwingDelay, () =>
		{
			if (SoundSwing.Count > 0)
			{
				base.Audio.pitch = Random.Range(SoundSwingPitch.x, SoundSwingPitch.y) * Time.timeScale;
				base.Audio.clip = (AudioClip)SoundSwing[Random.Range(0, SoundSwing.Count)];
				base.Audio.Play();
			}
			m_Weapon.SetState(WeaponStatePull, false);
			m_Weapon.SetState(WeaponStateSwing);
			m_Weapon.Refresh();
			m_Weapon.AddSoftForce(SwingPositionSoftForce, SwingRotationSoftForce, SwingSoftForceFrames);
			vp_Timer.In(ImpactTime, () =>
			{
				Ray ray = new Ray(new Vector3(m_Controller.Transform.position.x, m_Camera.Transform.position.y, m_Controller.Transform.position.z), m_Camera.Transform.forward);
				RaycastHit hitInfo;
				Physics.SphereCast(ray, DamageRadius, out hitInfo, DamageRange, -1828716565);
				if (hitInfo.collider != null)
				{
					SpawnImpactFX(hitInfo);
					ApplyDamage(hitInfo);
					ApplyRecoil();
				}
				else
				{
					vp_Timer.In(SwingDuration - ImpactTime, () =>
					{
						m_Weapon.StopSprings();
						Reset();
					}, SwingDurationTimer);
				}
			}, ImpactTimer);
		}, SwingDelayTimer);
	}

	private void PickAttack()
	{
		int num = States.Count - 1;
		do
		{
			num = Random.Range(0, States.Count - 1);
		}
		while (States.Count > 1 && num == m_AttackCurrent && Random.value < 0.5f);
		m_AttackCurrent = num;
		SetState(States[m_AttackCurrent].Name);
	}

	private void Attack()
	{
	}

	private void SpawnImpactFX(RaycastHit hit)
	{
		Quaternion rotation = Quaternion.LookRotation(hit.normal);
		if (m_DustPrefab != null)
		{
			vp_Utility.Instantiate(m_DustPrefab, hit.point, rotation);
		}
		if (m_SparkPrefab != null && Random.value < SparkFactor)
		{
			vp_Utility.Instantiate(m_SparkPrefab, hit.point, rotation);
		}
		if (m_DebrisPrefab != null)
		{
			vp_Utility.Instantiate(m_DebrisPrefab, hit.point, rotation);
		}
		if (SoundImpact.Count > 0)
		{
			base.Audio.pitch = Random.Range(SoundImpactPitch.x, SoundImpactPitch.y) * Time.timeScale;
			base.Audio.PlayOneShot((AudioClip)SoundImpact[Random.Range(0, SoundImpact.Count)]);
		}
	}

	private void ApplyDamage(RaycastHit hit)
	{
		hit.collider.SendMessage(DamageMethodName, Damage, SendMessageOptions.DontRequireReceiver);
		Rigidbody attachedRigidbody = hit.collider.attachedRigidbody;
		if (attachedRigidbody != null && !attachedRigidbody.isKinematic)
		{
			attachedRigidbody.AddForceAtPosition(m_Camera.Transform.forward * DamageForce / Time.timeScale / vp_TimeUtility.AdjustedTimeScale, hit.point);
		}
	}

	private void ApplyRecoil()
	{
		m_Weapon.StopSprings();
		m_Weapon.AddForce(ImpactPositionSpringRecoil, ImpactRotationSpringRecoil);
		m_Weapon.AddForce2(ImpactPositionSpring2Recoil, ImpactRotationSpring2Recoil);
		Reset();
	}

	private void Reset()
	{
		vp_Timer.In(0.05f, () =>
		{
			if (m_Weapon != null)
			{
				m_Weapon.SetState(WeaponStatePull, false);
				m_Weapon.SetState(WeaponStateSwing, false);
				m_Weapon.Refresh();
				if (AttackPickRandomState)
				{
					ResetState();
				}
			}
		}, ResetTimer);
	}
}
