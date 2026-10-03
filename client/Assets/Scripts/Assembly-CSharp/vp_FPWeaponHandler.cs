using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class vp_FPWeaponHandler : MonoBehaviour
{
	protected class WeaponComparer : IComparer
	{
		int IComparer.Compare(object x, object y)
		{
			return new CaseInsensitiveComparer().Compare(((vp_FPWeapon)x).gameObject.name, ((vp_FPWeapon)y).gameObject.name);
		}
	}

	public int StartWeapon;

	public float AttackStateDisableDelay = 0.5f;

	public float SetWeaponRefreshStatesDelay = 0.5f;

	public float SetWeaponDuration = 0.1f;

	public float SetWeaponReloadSleepDuration = 0.3f;

	public float SetWeaponZoomSleepDuration = 0.3f;

	public float SetWeaponAttackSleepDuration = 0.3f;

	public float ReloadAttackSleepDuration = 0.3f;

	public bool ReloadAutomatically = true;

	protected vp_FPPlayerEventHandler m_Player;

	protected List<vp_FPWeapon> m_Weapons = new List<vp_FPWeapon>();

	protected int m_CurrentWeaponIndex = -1;

	protected vp_FPWeapon m_CurrentWeapon;

	protected vp_Timer.Handle m_SetWeaponTimer = new vp_Timer.Handle();

	protected vp_Timer.Handle m_SetWeaponRefreshTimer = new vp_Timer.Handle();

	protected vp_Timer.Handle m_DisableAttackStateTimer = new vp_Timer.Handle();

	protected vp_Timer.Handle m_DisableReloadStateTimer = new vp_Timer.Handle();

	public List<vp_FPWeapon> Weapons
	{
		get
		{
			return m_Weapons;
		}
	}

	public vp_FPWeapon CurrentWeapon
	{
		get
		{
			return m_CurrentWeapon;
		}
	}

	[Obsolete("Please use the 'CurrentWeaponIndex' parameter instead.")]
	public int CurrentWeaponID
	{
		get
		{
			return m_CurrentWeaponIndex;
		}
	}

	public int CurrentWeaponIndex
	{
		get
		{
			return m_CurrentWeaponIndex;
		}
	}

	protected virtual bool OnValue_CurrentWeaponWielded
	{
		get
		{
			if (m_CurrentWeapon == null)
			{
				return false;
			}
			return m_CurrentWeapon.Wielded;
		}
	}

	protected virtual string OnValue_CurrentWeaponName
	{
		get
		{
			if (m_CurrentWeapon == null || m_Weapons == null)
			{
				return string.Empty;
			}
			return m_CurrentWeapon.name;
		}
	}

	protected virtual int OnValue_CurrentWeaponID
	{
		get
		{
			return m_CurrentWeaponIndex;
		}
	}

	protected virtual int OnValue_CurrentWeaponIndex
	{
		get
		{
			return m_CurrentWeaponIndex;
		}
	}

	protected virtual void Awake()
	{
		if ((bool)GetComponent<vp_FPWeapon>())
		{
			Debug.LogError(string.Concat("Error: (", this, ") Hierarchy error. This component should sit above any vp_FPWeapons in the gameobject hierarchy."));
			return;
		}
		m_Player = (vp_FPPlayerEventHandler)base.transform.root.GetComponentInChildren(typeof(vp_FPPlayerEventHandler));
		vp_FPWeapon[] componentsInChildren = GetComponentsInChildren<vp_FPWeapon>(true);
		foreach (vp_FPWeapon item in componentsInChildren)
		{
			m_Weapons.Insert(m_Weapons.Count, item);
		}
		if (m_Weapons.Count == 0)
		{
			Debug.LogError(string.Concat("Error: (", this, ") Hierarchy error. This component must be added to a gameobject with vp_FPWeapon components in child gameobjects."));
			return;
		}
		IComparer comparer = new WeaponComparer();
		m_Weapons.Sort(comparer.Compare);
		StartWeapon = Mathf.Clamp(StartWeapon, 0, m_Weapons.Count);
	}

	protected virtual void OnEnable()
	{
		if (m_Player != null)
		{
			m_Player.Register(this);
		}
	}

	protected virtual void OnDisable()
	{
		if (m_Player != null)
		{
			m_Player.Unregister(this);
		}
	}

	protected virtual void Update()
	{
		if (m_CurrentWeaponIndex != -1)
		{
			return;
		}
		SetWeapon(0);
		vp_Timer.In(SetWeaponDuration + 0.1f, () =>
		{
			if (StartWeapon != 0 && !m_Player.SetWeapon.TryStart(StartWeapon))
			{
				Debug.LogWarning(string.Concat("Warning (", this, ") Requested 'StartWeapon' (", m_Weapons[StartWeapon - 1].name, ") was denied, likely by the inventory. Make sure it's present in the inventory from the beginning."));
			}
		});
	}

	public virtual void SetWeapon(int weaponIndex)
	{
		if (m_Weapons.Count < 1)
		{
			Debug.LogError(string.Concat("Error: (", this, ") Tried to set weapon with an empty weapon list."));
			return;
		}
		if (weaponIndex < 0 || weaponIndex > m_Weapons.Count)
		{
			Debug.LogError(string.Concat("Error: (", this, ") Weapon list does not have a weapon with index: ", weaponIndex));
			return;
		}
		if (m_CurrentWeapon != null)
		{
			m_CurrentWeapon.ResetState();
		}
		foreach (vp_FPWeapon weapon in m_Weapons)
		{
			weapon.ActivateGameObject(false);
		}
		m_CurrentWeaponIndex = weaponIndex;
		m_CurrentWeapon = null;
		if (m_CurrentWeaponIndex > 0)
		{
			m_CurrentWeapon = m_Weapons[m_CurrentWeaponIndex - 1];
			if (m_CurrentWeapon != null)
			{
				m_CurrentWeapon.ActivateGameObject();
			}
		}
	}

	public virtual void CancelTimers()
	{
		vp_Timer.CancelAll("EjectShell");
		m_DisableAttackStateTimer.Cancel();
		m_SetWeaponTimer.Cancel();
		m_SetWeaponRefreshTimer.Cancel();
	}

	public virtual void SetWeaponLayer(int layer)
	{
		if (m_CurrentWeaponIndex >= 1 && m_CurrentWeaponIndex <= m_Weapons.Count)
		{
			vp_Layer.Set(m_Weapons[m_CurrentWeaponIndex - 1].gameObject, layer, true);
		}
	}

	protected virtual void OnStart_Reload()
	{
		m_Player.Attack.Stop(m_Player.CurrentWeaponReloadDuration.Get() + ReloadAttackSleepDuration);
	}

	protected virtual void OnStart_SetWeapon()
	{
		CancelTimers();
		m_Player.Reload.Stop(SetWeaponDuration + SetWeaponReloadSleepDuration);
		m_Player.Attack.Stop(SetWeaponDuration + SetWeaponAttackSleepDuration);
		if (m_CurrentWeapon != null)
		{
			m_CurrentWeapon.Wield(false);
		}
		m_Player.SetWeapon.AutoDuration = SetWeaponDuration;
	}

	protected virtual void OnStop_SetWeapon()
	{
		int weapon = (int)m_Player.SetWeapon.Argument;
		SetWeapon(weapon);
		if (m_CurrentWeapon != null)
		{
			m_CurrentWeapon.Wield();
		}
		vp_Timer.In(SetWeaponRefreshStatesDelay, () =>
		{
			m_Player.RefreshActivityStates();
			if (m_CurrentWeapon != null && m_Player.CurrentWeaponAmmoCount.Get() == 0)
			{
				m_Player.AutoReload.Try();
			}
		}, m_SetWeaponRefreshTimer);
	}

	protected virtual bool CanStart_SetWeapon()
	{
		int num = (int)m_Player.SetWeapon.Argument;
		if (num == m_CurrentWeaponIndex)
		{
			return false;
		}
		if (num < 0 || num > m_Weapons.Count)
		{
			return false;
		}
		if (m_Player.Reload.Active)
		{
			return false;
		}
		return true;
	}

	protected virtual bool CanStart_Attack()
	{
		if (m_CurrentWeapon == null)
		{
			return false;
		}
		if (m_Player.Attack.Active)
		{
			return false;
		}
		if (m_Player.SetWeapon.Active)
		{
			return false;
		}
		if (m_Player.Reload.Active)
		{
			return false;
		}
		return true;
	}

	protected virtual void OnStop_Attack()
	{
		vp_Timer.In(AttackStateDisableDelay, () =>
		{
			if (!m_Player.Attack.Active && m_CurrentWeapon != null)
			{
				m_CurrentWeapon.SetState("Attack", false);
			}
		}, m_DisableAttackStateTimer);
	}

	protected virtual bool OnAttempt_SetPrevWeapon()
	{
		int num = m_CurrentWeaponIndex - 1;
		if (num < 1)
		{
			num = m_Weapons.Count;
		}
		int num2 = 0;
		while (!m_Player.SetWeapon.TryStart(num))
		{
			num--;
			if (num < 1)
			{
				num = m_Weapons.Count;
			}
			num2++;
			if (num2 > m_Weapons.Count)
			{
				return false;
			}
		}
		return true;
	}

	protected virtual bool OnAttempt_SetNextWeapon()
	{
		int num = m_CurrentWeaponIndex + 1;
		int num2 = 0;
		while (!m_Player.SetWeapon.TryStart(num))
		{
			if (num > m_Weapons.Count + 1)
			{
				num = 0;
			}
			num++;
			num2++;
			if (num2 > m_Weapons.Count)
			{
				return false;
			}
		}
		return true;
	}

	protected virtual bool OnAttempt_SetWeaponByName(string name)
	{
		for (int i = 0; i < m_Weapons.Count; i++)
		{
			if (m_Weapons[i].name == name)
			{
				return m_Player.SetWeapon.TryStart(i + 1);
			}
		}
		return false;
	}

	protected virtual bool OnAttempt_AutoReload()
	{
		if (!ReloadAutomatically)
		{
			return false;
		}
		return m_Player.Reload.TryStart();
	}
}
