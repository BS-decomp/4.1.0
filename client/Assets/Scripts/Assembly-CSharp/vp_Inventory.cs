using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class vp_Inventory : MonoBehaviour
{
	[Serializable]
	public class ItemRecordsSection
	{
	}

	[Serializable]
	public class ItemCapsSection
	{
	}

	[Serializable]
	public class SpaceLimitSection
	{
	}

	[Serializable]
	public class ItemCap
	{
		[SerializeField]
		public vp_ItemType Type;

		[SerializeField]
		public int Cap;

		[SerializeField]
		public ItemCap(vp_ItemType type, int cap)
		{
			Type = type;
			Cap = cap;
		}
	}

	public enum Mode
	{
		Weight = 0,
		Volume = 1
	}

	protected struct StartItemRecord
	{
		public vp_ItemType Type;

		public int ID;

		public int Amount;

		public StartItemRecord(vp_ItemType type, int id, int amount)
		{
			Type = type;
			ID = id;
			Amount = amount;
		}
	}

	private const int UNLIMITED = -1;

	private const int UNIDENTIFIED = -1;

	private const int MAXCAPACITY = -1;

	[SerializeField]
	protected ItemRecordsSection m_ItemRecords;

	[SerializeField]
	protected ItemCapsSection m_ItemCaps;

	[SerializeField]
	protected SpaceLimitSection m_SpaceLimit;

	protected Transform m_Transform;

	[SerializeField]
	[HideInInspector]
	public List<vp_ItemInstance> ItemInstances = new List<vp_ItemInstance>();

	[HideInInspector]
	[SerializeField]
	public List<ItemCap> m_ItemCapInstances = new List<ItemCap>();

	[SerializeField]
	[HideInInspector]
	protected List<vp_UnitBankInstance> m_UnitBankInstances = new List<vp_UnitBankInstance>();

	[SerializeField]
	[HideInInspector]
	protected List<vp_UnitBankInstance> m_InternalUnitBanks = new List<vp_UnitBankInstance>();

	[SerializeField]
	[HideInInspector]
	public bool CapsEnabled;

	[HideInInspector]
	[SerializeField]
	public bool SpaceEnabled;

	[SerializeField]
	[HideInInspector]
	public Mode SpaceMode;

	[HideInInspector]
	[SerializeField]
	public bool AllowOnlyListed;

	[HideInInspector]
	[SerializeField]
	protected float m_TotalSpace = 100f;

	[SerializeField]
	[HideInInspector]
	protected float m_UsedSpace;

	protected bool m_Result;

	protected List<StartItemRecord> m_StartItems = new List<StartItemRecord>();

	protected bool m_FirstItemsDirty = true;

	protected Dictionary<vp_ItemType, vp_ItemInstance> m_FirstItemsOfType = new Dictionary<vp_ItemType, vp_ItemInstance>(100);

	protected vp_ItemInstance m_GetFirstItemInstanceResult;

	protected bool m_ItemDictionaryDirty = true;

	protected Dictionary<int, vp_ItemInstance> m_ItemDictionary = new Dictionary<int, vp_ItemInstance>();

	protected vp_ItemInstance m_GetItemResult;

	public List<vp_UnitBankInstance> UnitBankInstances
	{
		get
		{
			return m_UnitBankInstances;
		}
	}

	public List<vp_UnitBankInstance> InternalUnitBanks
	{
		get
		{
			return m_InternalUnitBanks;
		}
	}

	public float TotalSpace
	{
		get
		{
			return Mathf.Max(-1f, m_TotalSpace);
		}
		set
		{
			m_TotalSpace = value;
		}
	}

	public float UsedSpace
	{
		get
		{
			return Mathf.Max(0f, m_UsedSpace);
		}
		set
		{
			m_UsedSpace = Mathf.Max(0f, value);
		}
	}

	[HideInInspector]
	[SerializeField]
	public float RemainingSpace
	{
		get
		{
			return Mathf.Max(0f, TotalSpace - UsedSpace);
		}
	}

	protected virtual void Awake()
	{
		m_Transform = base.transform;
	}

	protected virtual void Start()
	{
		Refresh();
		SaveInitialState();
	}

	protected virtual void OnEnable()
	{
		vp_TargetEventReturn<vp_Inventory>.Register(m_Transform, "GetInventory", GetInventory);
		vp_TargetEventReturn<vp_ItemType, int, bool>.Register(m_Transform, "TryGiveItem", TryGiveItem);
		vp_TargetEventReturn<vp_ItemType, int, bool>.Register(m_Transform, "TryGiveItems", TryGiveItems);
		vp_TargetEventReturn<vp_UnitBankType, int, int, bool>.Register(m_Transform, "TryGiveUnitBank", TryGiveUnitBank);
		vp_TargetEventReturn<vp_UnitType, int, bool>.Register(m_Transform, "TryGiveUnits", TryGiveUnits);
		vp_TargetEventReturn<vp_UnitBankType, int, int, bool>.Register(m_Transform, "TryDeduct", TryDeduct);
		vp_TargetEventReturn<vp_ItemType, int>.Register(m_Transform, "GetItemCount", GetItemCount);
	}

	protected virtual void OnDisable()
	{
		vp_TargetEventReturn<vp_ItemType, int, bool>.Unregister(m_Transform, "TryGiveItem", TryGiveItem);
		vp_TargetEventReturn<vp_ItemType, int, bool>.Unregister(m_Transform, "TryGiveItems", TryGiveItems);
		vp_TargetEventReturn<vp_UnitBankType, int, int, bool>.Unregister(m_Transform, "TryGiveUnitBank", TryGiveUnitBank);
		vp_TargetEventReturn<vp_UnitType, int, bool>.Unregister(m_Transform, "TryGiveUnits", TryGiveUnits);
		vp_TargetEventReturn<vp_UnitBankType, int, int, bool>.Unregister(m_Transform, "TryDeduct", TryDeduct);
		vp_TargetEventReturn<vp_ItemType, int>.Unregister(m_Transform, "GetItemCount", GetItemCount);
		vp_TargetEventReturn<vp_Inventory>.Unregister(m_Transform, "HasInventory", GetInventory);
	}

	protected virtual vp_Inventory GetInventory()
	{
		return this;
	}

	public virtual bool TryGiveItems(vp_ItemType type, int amount)
	{
		bool result = false;
		while (amount > 0)
		{
			if (TryGiveItem(type, 0))
			{
				result = true;
			}
			amount--;
		}
		return result;
	}

	public virtual bool TryGiveItem(vp_ItemType itemType, int id)
	{
		if (itemType == null)
		{
			Debug.LogError("Error (" + vp_Utility.GetErrorLocation(2) + ") Item type was null.");
			return false;
		}
		vp_UnitType vp_UnitType2 = itemType as vp_UnitType;
		if (vp_UnitType2 != null)
		{
			return TryGiveUnits(vp_UnitType2, id);
		}
		vp_UnitBankType vp_UnitBankType2 = itemType as vp_UnitBankType;
		if (vp_UnitBankType2 != null)
		{
			return TryGiveUnitBank(vp_UnitBankType2, vp_UnitBankType2.Capacity, id);
		}
		if (CapsEnabled)
		{
			int itemCap = GetItemCap(itemType);
			if (itemCap != -1 && GetItemCount(itemType) >= itemCap)
			{
				return false;
			}
		}
		if (SpaceEnabled && UsedSpace + itemType.Space > TotalSpace)
		{
			return false;
		}
		DoAddItem(itemType, id);
		return true;
	}

	protected virtual void DoAddItem(vp_ItemType type, int id)
	{
		ItemInstances.Add(new vp_ItemInstance(type, id));
		m_FirstItemsDirty = true;
		if (SpaceEnabled)
		{
			m_UsedSpace += type.Space;
		}
		SetDirty();
	}

	protected virtual void DoRemoveItem(vp_ItemInstance item)
	{
		ItemInstances.RemoveAt(ItemInstances.IndexOf(item));
		m_FirstItemsDirty = true;
		if (SpaceEnabled)
		{
			m_UsedSpace = Mathf.Max(0f, m_UsedSpace - item.Type.Space);
		}
		SetDirty();
	}

	protected virtual void DoAddUnitBank(vp_UnitBankType unitBankType, int id, int unitsLoaded)
	{
		vp_UnitBankInstance vp_UnitBankInstance2 = new vp_UnitBankInstance(unitBankType, id, this);
		m_UnitBankInstances.Add(vp_UnitBankInstance2);
		m_FirstItemsDirty = true;
		m_ItemDictionaryDirty = true;
		if (SpaceEnabled && !vp_UnitBankInstance2.IsInternal)
		{
			m_UsedSpace += unitBankType.Space;
		}
		vp_UnitBankInstance2.TryGiveUnits(unitsLoaded);
		if (SpaceEnabled && !vp_UnitBankInstance2.IsInternal && SpaceMode == Mode.Weight && unitBankType.Unit != null)
		{
			m_UsedSpace += unitBankType.Unit.Space * (float)vp_UnitBankInstance2.Count;
		}
		SetDirty();
	}

	protected virtual void DoRemoveUnitBank(vp_UnitBankInstance bank)
	{
		if (!bank.IsInternal)
		{
			m_UnitBankInstances.RemoveAt(m_UnitBankInstances.IndexOf(bank));
			m_FirstItemsDirty = true;
			m_ItemDictionaryDirty = true;
			if (SpaceEnabled)
			{
				m_UsedSpace -= bank.Type.Space;
				if (SpaceMode == Mode.Weight)
				{
					m_UsedSpace -= bank.UnitType.Space * (float)bank.Count;
				}
			}
		}
		else
		{
			m_InternalUnitBanks.RemoveAt(m_InternalUnitBanks.IndexOf(bank));
		}
		SetDirty();
	}

	public virtual bool DoAddUnits(vp_UnitBankInstance bank, int amount)
	{
		return bank.DoAddUnits(amount);
	}

	public virtual bool DoRemoveUnits(vp_UnitBankInstance bank, int amount)
	{
		return bank.DoRemoveUnits(amount);
	}

	public virtual bool TryGiveUnits(vp_UnitType unitType, int amount)
	{
		if (GetItemCap(unitType) == 0)
		{
			return false;
		}
		return TryGiveUnits(GetInternalUnitBank(unitType), amount);
	}

	public virtual bool TryGiveUnits(vp_UnitBankInstance bank, int amount)
	{
		if (bank == null)
		{
			return false;
		}
		amount = Mathf.Max(0, amount);
		if (SpaceEnabled && (bank.IsInternal || SpaceMode == Mode.Weight) && RemainingSpace < (float)amount * bank.UnitType.Space)
		{
			amount = (int)(RemainingSpace / bank.UnitType.Space);
			return DoAddUnits(bank, amount);
		}
		return DoAddUnits(bank, amount);
	}

	public virtual bool TryRemoveUnits(vp_UnitType unitType, int amount)
	{
		vp_UnitBankInstance internalUnitBank = GetInternalUnitBank(unitType);
		if (internalUnitBank == null)
		{
			return false;
		}
		return DoRemoveUnits(internalUnitBank, amount);
	}

	public virtual bool TryGiveUnitBank(vp_UnitBankType unitBankType, int unitsLoaded, int id)
	{
		if (unitBankType == null)
		{
			Debug.LogError("Error (" + vp_Utility.GetErrorLocation() + ") 'unitBankType' was null.");
			return false;
		}
		if (CapsEnabled)
		{
			int itemCap = GetItemCap(unitBankType);
			if (itemCap != -1 && GetItemCount(unitBankType) >= itemCap)
			{
				return false;
			}
			if (unitBankType.Capacity != -1)
			{
				unitsLoaded = Mathf.Min(unitsLoaded, unitBankType.Capacity);
			}
		}
		if (SpaceEnabled)
		{
			switch (SpaceMode)
			{
			case Mode.Weight:
				if (unitBankType.Unit == null)
				{
					Debug.LogError(string.Concat("Error (vp_Inventory) UnitBank item type ", unitBankType, " can't be added because its unit type has not been set."));
					return false;
				}
				if (UsedSpace + unitBankType.Space + unitBankType.Unit.Space * (float)unitsLoaded > TotalSpace)
				{
					return false;
				}
				break;
			case Mode.Volume:
				if (UsedSpace + unitBankType.Space > TotalSpace)
				{
					return false;
				}
				break;
			}
		}
		DoAddUnitBank(unitBankType, id, unitsLoaded);
		return true;
	}

	public virtual bool TryRemoveItems(vp_ItemType type, int amount)
	{
		bool result = false;
		while (amount > 0)
		{
			if (TryRemoveItem(type, -1))
			{
				result = true;
			}
			amount--;
		}
		return result;
	}

	public virtual bool TryRemoveItem(vp_ItemType type, int id)
	{
		return TryRemoveItem(GetItem(type, id));
	}

	public virtual bool TryRemoveItem(vp_ItemInstance item)
	{
		if (item == null)
		{
			return false;
		}
		DoRemoveItem(item);
		SetDirty();
		return true;
	}

	public virtual bool TryRemoveUnitBanks(vp_UnitBankType type, int amount)
	{
		bool result = false;
		while (amount > 0)
		{
			if (TryRemoveUnitBank(type, -1))
			{
				result = true;
			}
			amount--;
		}
		return result;
	}

	public virtual bool TryRemoveUnitBank(vp_UnitBankType type, int id)
	{
		return TryRemoveUnitBank(GetItem(type, id) as vp_UnitBankInstance);
	}

	public virtual bool TryRemoveUnitBank(vp_UnitBankInstance unitBank)
	{
		if (unitBank == null)
		{
			return false;
		}
		DoRemoveUnitBank(unitBank);
		SetDirty();
		return true;
	}

	public virtual bool TryReload(vp_ItemType itemType, int unitBankId)
	{
		return TryReload(GetItem(itemType, unitBankId) as vp_UnitBankInstance, -1);
	}

	public virtual bool TryReload(vp_ItemType itemType, int unitBankId, int amount)
	{
		return TryReload(GetItem(itemType, unitBankId) as vp_UnitBankInstance, amount);
	}

	public virtual bool TryReload(vp_UnitBankInstance bank)
	{
		return TryReload(bank, -1);
	}

	public virtual bool TryReload(vp_UnitBankInstance bank, int amount)
	{
		if (bank == null || bank.IsInternal || bank.ID == -1)
		{
			Debug.LogWarning("Warning (" + vp_Utility.GetErrorLocation() + ") 'TryReloadUnitBank' could not identify a target item. If you are trying to add units to the main inventory please instead use 'TryGiveUnits'.");
			return false;
		}
		int count = bank.Count;
		if (count >= bank.Capacity)
		{
			return false;
		}
		int unitCount = GetUnitCount(bank.UnitType);
		if (unitCount < 1)
		{
			return false;
		}
		if (amount == -1)
		{
			amount = bank.Capacity;
		}
		TryRemoveUnits(bank.UnitType, amount);
		int num = Mathf.Max(0, unitCount - GetUnitCount(bank.UnitType));
		if (!DoAddUnits(bank, num))
		{
			return false;
		}
		int num2 = Mathf.Max(0, bank.Count - count);
		if (num2 < 1)
		{
			return false;
		}
		if (num2 > 0 && num2 < num)
		{
			TryGiveUnits(bank.UnitType, num - num2);
		}
		return true;
	}

	public virtual bool TryDeduct(vp_UnitBankType unitBankType, int unitBankId, int amount)
	{
		vp_UnitBankInstance vp_UnitBankInstance2 = ((unitBankId >= 1) ? (GetItem(unitBankType, unitBankId) as vp_UnitBankInstance) : (GetItem(unitBankType) as vp_UnitBankInstance));
		if (vp_UnitBankInstance2 == null)
		{
			return false;
		}
		if (!DoRemoveUnits(vp_UnitBankInstance2, amount))
		{
			return false;
		}
		if (vp_UnitBankInstance2.Count <= 0 && (vp_UnitBankInstance2.Type as vp_UnitBankType).RemoveWhenDepleted)
		{
			DoRemoveUnitBank(vp_UnitBankInstance2);
		}
		return true;
	}

	protected virtual vp_ItemInstance GetItem(vp_ItemType itemType)
	{
		if (m_FirstItemsDirty)
		{
			m_FirstItemsOfType.Clear();
			foreach (vp_ItemInstance itemInstance in ItemInstances)
			{
				if (itemInstance != null && !m_FirstItemsOfType.ContainsKey(itemInstance.Type))
				{
					m_FirstItemsOfType.Add(itemInstance.Type, itemInstance);
				}
			}
			foreach (vp_UnitBankInstance unitBankInstance in UnitBankInstances)
			{
				if (unitBankInstance != null && !m_FirstItemsOfType.ContainsKey(unitBankInstance.Type))
				{
					m_FirstItemsOfType.Add(unitBankInstance.Type, unitBankInstance);
				}
			}
			m_FirstItemsDirty = false;
		}
		if (!m_FirstItemsOfType.TryGetValue(itemType, out m_GetFirstItemInstanceResult))
		{
			return null;
		}
		if (m_GetFirstItemInstanceResult == null)
		{
			m_FirstItemsDirty = true;
			return GetItem(itemType);
		}
		return m_GetFirstItemInstanceResult;
	}

	protected vp_ItemInstance GetItem(vp_ItemType itemType, int id)
	{
		if (itemType == null)
		{
			Debug.LogError("Error (" + vp_Utility.GetErrorLocation(1, true) + ") Sent a null itemType to 'GetItem'.");
			return null;
		}
		if (id < 1)
		{
			return GetItem(itemType);
		}
		if (m_ItemDictionaryDirty)
		{
			m_ItemDictionary.Clear();
			m_ItemDictionaryDirty = false;
		}
		if (!m_ItemDictionary.TryGetValue(id, out m_GetItemResult))
		{
			m_GetItemResult = GetItemFromList(itemType, id);
			if (m_GetItemResult != null && id > 0)
			{
				m_ItemDictionary.Add(id, m_GetItemResult);
			}
		}
		else if (m_GetItemResult != null)
		{
			if (m_GetItemResult.Type != itemType)
			{
				Debug.LogWarning("Warning: (vp_Inventory) Player has vp_FPWeapons with identical, non-zero vp_ItemIdentifier IDs! This is much slower than using zero or differing IDs.");
				m_GetItemResult = GetItemFromList(itemType, id);
			}
		}
		else
		{
			m_ItemDictionary.Remove(id);
			GetItem(itemType, id);
		}
		return m_GetItemResult;
	}

	public virtual vp_ItemInstance GetItem(string itemTypeName)
	{
		for (int i = 0; i < InternalUnitBanks.Count; i++)
		{
			if (InternalUnitBanks[i].UnitType.name == itemTypeName)
			{
				return InternalUnitBanks[i];
			}
		}
		for (int j = 0; j < m_UnitBankInstances.Count; j++)
		{
			if (m_UnitBankInstances[j].Type.name == itemTypeName)
			{
				return m_UnitBankInstances[j];
			}
		}
		for (int k = 0; k < ItemInstances.Count; k++)
		{
			if (ItemInstances[k].Type.name == itemTypeName)
			{
				return ItemInstances[k];
			}
		}
		return null;
	}

	protected virtual vp_ItemInstance GetItemFromList(vp_ItemType itemType, int id = -1)
	{
		for (int i = 0; i < m_UnitBankInstances.Count; i++)
		{
			if (!(m_UnitBankInstances[i].Type != itemType) && (id == -1 || m_UnitBankInstances[i].ID == id))
			{
				return m_UnitBankInstances[i];
			}
		}
		for (int j = 0; j < ItemInstances.Count; j++)
		{
			if (!(ItemInstances[j].Type != itemType) && (id == -1 || ItemInstances[j].ID == id))
			{
				return ItemInstances[j];
			}
		}
		return null;
	}

	public virtual bool HaveItem(vp_ItemType itemType, int id = -1)
	{
		if (itemType == null)
		{
			return false;
		}
		return GetItem(itemType, id) != null;
	}

	protected virtual vp_UnitBankInstance GetInternalUnitBank(vp_UnitType unitType)
	{
		for (int i = 0; i < m_InternalUnitBanks.Count; i++)
		{
			if (m_InternalUnitBanks[i].GetType() == typeof(vp_UnitBankInstance) && !(m_InternalUnitBanks[i].Type != null))
			{
				vp_UnitBankInstance vp_UnitBankInstance2 = m_InternalUnitBanks[i];
				if (!(vp_UnitBankInstance2.UnitType != unitType))
				{
					return vp_UnitBankInstance2;
				}
			}
		}
		SetDirty();
		vp_UnitBankInstance vp_UnitBankInstance3 = new vp_UnitBankInstance(unitType, this);
		vp_UnitBankInstance3.Capacity = GetItemCap(unitType);
		m_InternalUnitBanks.Add(vp_UnitBankInstance3);
		return vp_UnitBankInstance3;
	}

	public virtual bool HaveInternalUnitBank(vp_UnitType unitType)
	{
		for (int i = 0; i < m_InternalUnitBanks.Count; i++)
		{
			if (m_InternalUnitBanks[i].GetType() == typeof(vp_UnitBankInstance) && !(m_InternalUnitBanks[i].Type != null))
			{
				vp_UnitBankInstance vp_UnitBankInstance2 = m_InternalUnitBanks[i];
				if (!(vp_UnitBankInstance2.UnitType != unitType))
				{
					return true;
				}
			}
		}
		return false;
	}

	public virtual void Refresh()
	{
		for (int i = 0; i < m_InternalUnitBanks.Count; i++)
		{
			m_InternalUnitBanks[i].Capacity = GetItemCap(m_InternalUnitBanks[i].UnitType);
		}
		if (!SpaceEnabled)
		{
			return;
		}
		m_UsedSpace = 0f;
		for (int j = 0; j < ItemInstances.Count; j++)
		{
			m_UsedSpace += ItemInstances[j].Type.Space;
		}
		for (int k = 0; k < m_UnitBankInstances.Count; k++)
		{
			switch (SpaceMode)
			{
			case Mode.Weight:
				m_UsedSpace += m_UnitBankInstances[k].Type.Space + m_UnitBankInstances[k].UnitType.Space * (float)m_UnitBankInstances[k].Count;
				break;
			case Mode.Volume:
				m_UsedSpace += m_UnitBankInstances[k].Type.Space;
				break;
			}
		}
		for (int l = 0; l < m_InternalUnitBanks.Count; l++)
		{
			m_UsedSpace += m_InternalUnitBanks[l].UnitType.Space * (float)m_InternalUnitBanks[l].Count;
		}
	}

	public virtual int GetItemCount(vp_ItemType type)
	{
		vp_UnitType vp_UnitType2 = type as vp_UnitType;
		if (vp_UnitType2 != null)
		{
			return GetUnitCount(vp_UnitType2);
		}
		int num = 0;
		for (int i = 0; i < ItemInstances.Count; i++)
		{
			if (ItemInstances[i].Type == type)
			{
				num++;
			}
		}
		for (int j = 0; j < m_UnitBankInstances.Count; j++)
		{
			if (m_UnitBankInstances[j].Type == type)
			{
				num++;
			}
		}
		return num;
	}

	public virtual void SetItemCount(vp_ItemType type, int amount)
	{
		if (type is vp_UnitType)
		{
			SetUnitCount((vp_UnitType)type, amount);
			return;
		}
		bool capsEnabled = CapsEnabled;
		bool spaceEnabled = SpaceEnabled;
		CapsEnabled = false;
		SpaceEnabled = false;
		int num = amount - GetItemCount(type);
		if (num > 0)
		{
			TryGiveItems(type, amount);
		}
		else if (num < 0)
		{
			TryRemoveItems(type, -amount);
		}
		CapsEnabled = capsEnabled;
		SpaceEnabled = spaceEnabled;
	}

	public virtual void SetUnitCount(vp_UnitType unitType, int amount)
	{
		TrySetUnitCount(GetInternalUnitBank(unitType), amount);
	}

	public virtual void SetUnitCount(vp_UnitBankInstance bank, int amount)
	{
		if (bank == null)
		{
			return;
		}
		amount = Mathf.Max(0, amount);
		if (amount != bank.Count)
		{
			int count = bank.Count;
			if (!DoRemoveUnits(bank, bank.Count))
			{
				bank.Count = count;
			}
			if (amount != 0 && !DoAddUnits(bank, amount))
			{
				bank.Count = count;
			}
		}
	}

	public virtual bool TrySetUnitCount(vp_UnitType unitType, int amount)
	{
		return TrySetUnitCount(GetInternalUnitBank(unitType), amount);
	}

	public virtual bool TrySetUnitCount(vp_UnitBankInstance bank, int amount)
	{
		if (bank == null)
		{
			return false;
		}
		amount = Mathf.Max(0, amount);
		if (amount == bank.Count)
		{
			return true;
		}
		int count = bank.Count;
		if (!DoRemoveUnits(bank, bank.Count))
		{
			bank.Count = count;
		}
		if (amount == 0)
		{
			return true;
		}
		if (bank.IsInternal)
		{
			m_Result = TryGiveUnits(bank.UnitType, amount);
			if (!m_Result)
			{
				bank.Count = count;
			}
			return m_Result;
		}
		m_Result = TryGiveUnits(bank, amount);
		if (!m_Result)
		{
			bank.Count = count;
		}
		return m_Result;
	}

	public virtual int GetItemCap(vp_ItemType type)
	{
		if (!CapsEnabled)
		{
			return -1;
		}
		for (int i = 0; i < m_ItemCapInstances.Count; i++)
		{
			if (m_ItemCapInstances[i].Type == type)
			{
				return m_ItemCapInstances[i].Cap;
			}
		}
		if (AllowOnlyListed)
		{
			return 0;
		}
		return -1;
	}

	public virtual void SetItemCap(vp_ItemType type, int cap, bool clamp = false)
	{
		SetDirty();
		int num = 0;
		while (true)
		{
			if (num < m_ItemCapInstances.Count)
			{
				if (m_ItemCapInstances[num].Type == type)
				{
					m_ItemCapInstances[num].Cap = cap;
					break;
				}
				num++;
				continue;
			}
			m_ItemCapInstances.Add(new ItemCap(type, cap));
			break;
		}
		if (type is vp_UnitType)
		{
			for (int i = 0; i < m_InternalUnitBanks.Count; i++)
			{
				if (m_InternalUnitBanks[i].UnitType != null && m_InternalUnitBanks[i].UnitType == type)
				{
					m_InternalUnitBanks[i].Capacity = cap;
					if (clamp)
					{
						m_InternalUnitBanks[i].ClampToCapacity();
					}
				}
			}
		}
		else if (clamp && GetItemCount(type) > cap)
		{
			TryRemoveItems(type, GetItemCount(type) - cap);
		}
	}

	public virtual int GetUnitCount(vp_UnitType unitType)
	{
		vp_UnitBankInstance internalUnitBank = GetInternalUnitBank(unitType);
		if (internalUnitBank == null)
		{
			return 0;
		}
		return internalUnitBank.Count;
	}

	public virtual void SaveInitialState()
	{
		for (int i = 0; i < InternalUnitBanks.Count; i++)
		{
			m_StartItems.Add(new StartItemRecord(InternalUnitBanks[i].UnitType, 0, InternalUnitBanks[i].Count));
		}
		for (int j = 0; j < m_UnitBankInstances.Count; j++)
		{
			m_StartItems.Add(new StartItemRecord(m_UnitBankInstances[j].Type, m_UnitBankInstances[j].ID, m_UnitBankInstances[j].Count));
		}
		for (int k = 0; k < ItemInstances.Count; k++)
		{
			m_StartItems.Add(new StartItemRecord(ItemInstances[k].Type, ItemInstances[k].ID, 1));
		}
	}

	public virtual void Reset()
	{
		Clear();
		for (int i = 0; i < m_StartItems.Count; i++)
		{
			if (m_StartItems[i].Type.GetType() == typeof(vp_ItemType))
			{
				TryGiveItem(m_StartItems[i].Type, m_StartItems[i].ID);
			}
			else if (m_StartItems[i].Type.GetType() == typeof(vp_UnitBankType))
			{
				TryGiveUnitBank(m_StartItems[i].Type as vp_UnitBankType, m_StartItems[i].Amount, m_StartItems[i].ID);
			}
			else if (m_StartItems[i].Type.GetType() == typeof(vp_UnitType))
			{
				TryGiveUnits(m_StartItems[i].Type as vp_UnitType, m_StartItems[i].Amount);
			}
		}
	}

	protected virtual void Clear()
	{
		for (int num = InternalUnitBanks.Count - 1; num > -1; num--)
		{
			DoRemoveUnitBank(InternalUnitBanks[num]);
		}
		for (int num2 = m_UnitBankInstances.Count - 1; num2 > -1; num2--)
		{
			DoRemoveUnitBank(m_UnitBankInstances[num2]);
		}
		for (int num3 = ItemInstances.Count - 1; num3 > -1; num3--)
		{
			DoRemoveItem(ItemInstances[num3]);
		}
	}

	public virtual void SetTotalSpace(float spaceLimitation)
	{
		SetDirty();
		TotalSpace = Mathf.Max(0f, spaceLimitation);
	}

	public virtual void SetDirty()
	{
	}

	public virtual void ClearItemDictionaries()
	{
	}
}
