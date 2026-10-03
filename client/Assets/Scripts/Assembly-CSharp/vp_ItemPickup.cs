using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[RequireComponent(typeof(SphereCollider))]
public class vp_ItemPickup : MonoBehaviour
{
	[Serializable]
	public class ItemSection
	{
		public vp_ItemType Type;
	}

	[Serializable]
	public class RecipientTagsSection
	{
		public List<string> Tags = new List<string>();
	}

	[Serializable]
	public class SoundSection
	{
		public AudioClip PickupSound;

		public bool PickupSoundSlomo = true;

		public AudioClip PickupFailSound;

		public bool FailSoundSlomo = true;
	}

	[Serializable]
	public class MessageSection
	{
		public string SuccessSingle = "Picked up {2}.";

		public string SuccessMultiple = "Picked up {4} {1}s.";

		public string FailSingle = "Can't pick up {2} right now.";

		public string FailMultiple = "Can't pick up {4} {1}s right now.";
	}

	public int ID;

	public int Amount;

	protected Type m_ItemType;

	protected AudioSource m_Audio;

	[SerializeField]
	protected ItemSection m_Item;

	[SerializeField]
	protected RecipientTagsSection m_Recipient;

	[SerializeField]
	protected SoundSection m_Sound;

	[SerializeField]
	protected MessageSection m_Messages;

	protected bool m_Depleted;

	protected int m_PickedUpAmount;

	protected Rigidbody m_Rigidbody;

	protected Transform m_Transform;

	protected string MissingItemTypeError = "Warning: {0} has no ItemType object!";

	protected bool m_AlreadyFailed;

	private static Dictionary<Collider, vp_Inventory> m_ColliderInventories = new Dictionary<Collider, vp_Inventory>();

	protected Type ItemType
	{
		get
		{
			if (m_ItemType == null)
			{
				m_ItemType = m_Item.Type.GetType();
			}
			return m_ItemType;
		}
	}

	protected AudioSource Audio
	{
		get
		{
			if (m_Audio == null)
			{
				if (base.GetComponent<AudioSource>() == null)
				{
					base.gameObject.AddComponent<AudioSource>();
				}
				m_Audio = base.GetComponent<AudioSource>();
			}
			return m_Audio;
		}
	}

	protected virtual void Awake()
	{
		if (ItemType == typeof(vp_UnitType))
		{
			Amount = Mathf.Max(1, Amount);
		}
		base.GetComponent<Collider>().isTrigger = true;
		m_Rigidbody = base.GetComponent<Rigidbody>();
		m_Transform = base.transform;
		if (m_Sound.PickupSound != null || m_Sound.PickupFailSound != null)
		{
			Audio.clip = m_Sound.PickupSound;
			Audio.playOnAwake = false;
		}
	}

	protected virtual void Update()
	{
		if (m_Depleted && !Audio.isPlaying)
		{
			SendMessage("Die", SendMessageOptions.DontRequireReceiver);
		}
		if (m_Depleted || !(m_Rigidbody != null) || !m_Rigidbody.IsSleeping() || m_Rigidbody.isKinematic)
		{
			return;
		}
		m_Rigidbody.isKinematic = true;
		Collider[] components = GetComponents<Collider>();
		foreach (Collider collider in components)
		{
			if (!collider.isTrigger)
			{
				collider.enabled = false;
			}
		}
	}

	protected virtual void OnEnable()
	{
		if (m_Rigidbody != null)
		{
			m_Rigidbody.isKinematic = false;
			Collider[] components = GetComponents<Collider>();
			foreach (Collider collider in components)
			{
				if (!collider.isTrigger)
				{
					collider.enabled = true;
				}
			}
		}
		base.GetComponent<Renderer>().enabled = true;
		m_Depleted = false;
		m_AlreadyFailed = false;
	}

	protected virtual void OnTriggerEnter(Collider col)
	{
		if (ItemType == null || m_Depleted)
		{
			return;
		}
		vp_Inventory value;
		if (!m_ColliderInventories.TryGetValue(col, out value))
		{
			value = vp_TargetEventReturn<vp_Inventory>.SendUpwards(col, "GetInventory");
			m_ColliderInventories.Add(col, value);
		}
		if (!(value == null) && (m_Recipient.Tags.Count <= 0 || m_Recipient.Tags.Contains(col.gameObject.tag)))
		{
			bool flag = false;
			int num = vp_TargetEventReturn<vp_ItemType, int>.SendUpwards(col, "GetItemCount", m_Item.Type);
			if (ItemType == typeof(vp_ItemType))
			{
				flag = vp_TargetEventReturn<vp_ItemType, int, bool>.SendUpwards(col, "TryGiveItem", m_Item.Type, ID);
			}
			else if (ItemType == typeof(vp_UnitBankType))
			{
				flag = vp_TargetEventReturn<vp_UnitBankType, int, int, bool>.SendUpwards(col, "TryGiveUnitBank", m_Item.Type as vp_UnitBankType, Amount, ID);
			}
			else if (ItemType == typeof(vp_UnitType))
			{
				flag = vp_TargetEventReturn<vp_UnitType, int, bool>.SendUpwards(col, "TryGiveUnits", m_Item.Type as vp_UnitType, Amount);
			}
			if (flag)
			{
				m_PickedUpAmount = vp_TargetEventReturn<vp_ItemType, int>.SendUpwards(col, "GetItemCount", m_Item.Type) - num;
				OnSuccess();
			}
			else
			{
				OnFail();
			}
		}
	}

	protected virtual void OnTriggerExit()
	{
		m_AlreadyFailed = false;
	}

	protected virtual void OnSuccess()
	{
		m_Depleted = true;
		if (m_Sound.PickupSound != null)
		{
			Audio.pitch = ((!m_Sound.PickupSoundSlomo) ? 1f : Time.timeScale);
			Audio.Play();
			base.GetComponent<Renderer>().enabled = false;
		}
		string empty = string.Empty;
		empty = ((m_PickedUpAmount >= 2 && ItemType != typeof(vp_UnitBankType)) ? string.Format(m_Messages.SuccessMultiple, m_Item.Type.IndefiniteArticle, m_Item.Type.DisplayName, m_Item.Type.DisplayNameFull, m_Item.Type.Description, m_PickedUpAmount.ToString()) : string.Format(m_Messages.SuccessSingle, m_Item.Type.IndefiniteArticle, m_Item.Type.DisplayName, m_Item.Type.DisplayNameFull, m_Item.Type.Description, m_PickedUpAmount.ToString()));
		vp_GlobalEvent<string>.Send("HUDText", empty);
	}

	protected virtual void Die()
	{
		vp_Utility.Activate(base.gameObject, false);
	}

	protected virtual void OnFail()
	{
		if (!m_AlreadyFailed && m_Sound.PickupFailSound != null)
		{
			Audio.pitch = ((!m_Sound.FailSoundSlomo) ? 1f : Time.timeScale);
			Audio.PlayOneShot(m_Sound.PickupFailSound);
		}
		m_AlreadyFailed = true;
		string empty = string.Empty;
		empty = ((m_PickedUpAmount >= 2 && ItemType != typeof(vp_UnitBankType)) ? string.Format(m_Messages.FailMultiple, m_Item.Type.IndefiniteArticle, m_Item.Type.DisplayName, m_Item.Type.DisplayNameFull, m_Item.Type.Description, Amount.ToString()) : string.Format(m_Messages.FailSingle, m_Item.Type.IndefiniteArticle, m_Item.Type.DisplayName, m_Item.Type.DisplayNameFull, m_Item.Type.Description, Amount.ToString()));
		vp_GlobalEvent<string>.Send("HUDText", empty);
	}
}
