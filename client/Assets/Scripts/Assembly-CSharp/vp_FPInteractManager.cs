using System.Collections.Generic;
using UnityEngine;

public class vp_FPInteractManager : MonoBehaviour
{
	public float InteractDistance = 2f;

	public float MaxInteractDistance = 25f;

	protected vp_FPPlayerEventHandler m_Player;

	protected vp_FPCamera m_Camera;

	protected vp_Interactable m_CurrentInteractable;

	protected Texture m_OriginalCrosshair;

	protected vp_Interactable m_LastInteractable;

	protected Dictionary<Collider, vp_Interactable> m_Interactables = new Dictionary<Collider, vp_Interactable>();

	protected vp_Interactable m_CurrentCrosshairInteractable;

	protected vp_Timer.Handle m_ShowTextTimer = new vp_Timer.Handle();

	protected bool m_CanInteract;

	public float CrosshairTimeoutTimer { get; set; }

	protected virtual vp_Interactable OnValue_Interactable
	{
		get
		{
			return m_CurrentInteractable;
		}
		set
		{
			m_CurrentInteractable = value;
		}
	}

	protected virtual bool OnValue_CanInteract
	{
		get
		{
			return m_CanInteract;
		}
		set
		{
			m_CanInteract = value;
		}
	}

	protected virtual void Awake()
	{
		m_Player = GetComponent<vp_FPPlayerEventHandler>();
		m_Camera = GetComponentInChildren<vp_FPCamera>();
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

	public virtual void OnStart_Dead()
	{
		ShouldFinishInteraction();
	}

	public virtual void LateUpdate()
	{
		if (!m_Player.Dead.Active)
		{
			InteractCrosshair();
		}
	}

	protected virtual bool CanStart_Interact()
	{
		if (ShouldFinishInteraction())
		{
			return false;
		}
		if (m_Player.SetWeapon.Active)
		{
			return false;
		}
		vp_Interactable interactable = null;
		if (FindInteractable(out interactable))
		{
			if (interactable.InteractType != vp_Interactable.vp_InteractType.Normal)
			{
				return false;
			}
			if (!interactable.TryInteract(m_Player))
			{
				return false;
			}
			ResetCrosshair(false);
			m_LastInteractable = interactable;
			return true;
		}
		return false;
	}

	protected virtual bool ShouldFinishInteraction()
	{
		if (m_Player.Interactable.Get() != null)
		{
			m_CurrentCrosshairInteractable = null;
			ResetCrosshair();
			m_Player.Interactable.Get().FinishInteraction();
			m_Player.Interactable.Set(null);
			return true;
		}
		return false;
	}

	protected virtual void InteractCrosshair()
	{
		if (m_Player.Interactable.Get() != null)
		{
			return;
		}
		vp_Interactable interactable = null;
		if (FindInteractable(out interactable))
		{
			if (interactable != m_CurrentCrosshairInteractable && (!(CrosshairTimeoutTimer > Time.time) || !(m_LastInteractable != null) || interactable.GetType() != m_LastInteractable.GetType()))
			{
				m_CanInteract = true;
				m_CurrentCrosshairInteractable = interactable;
				if (!(interactable.InteractText != string.Empty) || !m_ShowTextTimer.Active)
				{
				}
				if (!(interactable.m_InteractCrosshair == null))
				{
				}
			}
		}
		else
		{
			m_CanInteract = false;
			ResetCrosshair();
		}
	}

	protected virtual bool FindInteractable(out vp_Interactable interactable)
	{
		interactable = null;
		RaycastHit hitInfo;
		if (Physics.Raycast(m_Camera.Transform.position, m_Camera.Transform.forward, out hitInfo, MaxInteractDistance, -1828716565))
		{
			if (!m_Interactables.TryGetValue(hitInfo.GetComponent<Collider>(), out interactable))
			{
				m_Interactables.Add(hitInfo.GetComponent<Collider>(), interactable = hitInfo.GetComponent<Collider>().GetComponent<vp_Interactable>());
			}
			if (interactable == null)
			{
				return false;
			}
			if (interactable.InteractDistance == 0f && hitInfo.distance >= InteractDistance)
			{
				return false;
			}
			if (interactable.InteractDistance > 0f && hitInfo.distance >= interactable.InteractDistance)
			{
				return false;
			}
			return true;
		}
		return false;
	}

	protected virtual void ResetCrosshair(bool reset = true)
	{
		m_ShowTextTimer.Cancel();
		m_CurrentCrosshairInteractable = null;
	}

	protected virtual void OnControllerColliderHit(ControllerColliderHit hit)
	{
		Rigidbody attachedRigidbody = hit.GetComponent<Collider>().attachedRigidbody;
		if (!(attachedRigidbody == null) && !attachedRigidbody.isKinematic)
		{
			vp_Interactable value = null;
			if (!m_Interactables.TryGetValue(hit.GetComponent<Collider>(), out value))
			{
				m_Interactables.Add(hit.GetComponent<Collider>(), value = hit.GetComponent<Collider>().GetComponent<vp_Interactable>());
			}
			if (!(value == null) && value.InteractType == vp_Interactable.vp_InteractType.CollisionTrigger)
			{
				hit.gameObject.SendMessage("TryInteract", m_Player, SendMessageOptions.DontRequireReceiver);
			}
		}
	}
}
