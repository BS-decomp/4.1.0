using System.Collections.Generic;
using UnityEngine;

public class CFX_SpawnSystem : MonoBehaviour
{
	private static CFX_SpawnSystem instance;

	public GameObject[] objectsToPreload = new GameObject[0];

	public int[] objectsToPreloadTimes = new int[0];

	public bool hideObjectsInHierarchy;

	private bool allObjectsLoaded;

	private Dictionary<int, List<GameObject>> instantiatedObjects = new Dictionary<int, List<GameObject>>();

	private Dictionary<int, int> poolCursors = new Dictionary<int, int>();

	public static bool AllObjectsLoaded
	{
		get
		{
			return instance.allObjectsLoaded;
		}
	}

	public static GameObject GetNextObject(GameObject sourceObj, bool activateObject = true)
	{
		int instanceID = sourceObj.GetInstanceID();
		if (!instance.poolCursors.ContainsKey(instanceID))
		{
			Debug.LogError("[CFX_SpawnSystem.GetNextPoolObject()] Object hasn't been preloaded: " + sourceObj.name + " (ID:" + instanceID + ")");
			return null;
		}
		int index = instance.poolCursors[instanceID];
		Dictionary<int, int> dictionary2;
		Dictionary<int, int> dictionary = (dictionary2 = instance.poolCursors);
		int key2;
		int key = (key2 = instanceID);
		key2 = dictionary2[key2];
		dictionary[key] = key2 + 1;
		if (instance.poolCursors[instanceID] >= instance.instantiatedObjects[instanceID].Count)
		{
			instance.poolCursors[instanceID] = 0;
		}
		GameObject gameObject = instance.instantiatedObjects[instanceID][index];
		if (activateObject)
		{
			gameObject.SetActive(true);
		}
		return gameObject;
	}

	public static void PreloadObject(GameObject sourceObj, int poolSize = 1)
	{
		instance.addObjectToPool(sourceObj, poolSize);
	}

	public static void UnloadObjects(GameObject sourceObj)
	{
		instance.removeObjectsFromPool(sourceObj);
	}

	private void addObjectToPool(GameObject sourceObject, int number)
	{
		int instanceID = sourceObject.GetInstanceID();
		if (!instantiatedObjects.ContainsKey(instanceID))
		{
			instantiatedObjects.Add(instanceID, new List<GameObject>());
			poolCursors.Add(instanceID, 0);
		}
		for (int i = 0; i < number; i++)
		{
			GameObject gameObject = (GameObject)Object.Instantiate(sourceObject);
			gameObject.SetActive(false);
			WFX_AutoDestructShuriken[] componentsInChildren = gameObject.GetComponentsInChildren<WFX_AutoDestructShuriken>(true);
			WFX_AutoDestructShuriken[] array = componentsInChildren;
			foreach (WFX_AutoDestructShuriken wFX_AutoDestructShuriken in array)
			{
				wFX_AutoDestructShuriken.OnlyDeactivate = true;
			}
			instantiatedObjects[instanceID].Add(gameObject);
			if (hideObjectsInHierarchy)
			{
				gameObject.hideFlags = HideFlags.HideInHierarchy;
			}
		}
	}

	private void removeObjectsFromPool(GameObject sourceObject)
	{
		int instanceID = sourceObject.GetInstanceID();
		if (!instantiatedObjects.ContainsKey(instanceID))
		{
			Debug.LogWarning("[CFX_SpawnSystem.removeObjectsFromPool()] There aren't any preloaded object for: " + sourceObject.name + " (ID:" + instanceID + ")");
			return;
		}
		for (int num = instantiatedObjects[instanceID].Count - 1; num >= 0; num--)
		{
			GameObject obj = instantiatedObjects[instanceID][num];
			instantiatedObjects[instanceID].RemoveAt(num);
			Object.Destroy(obj);
		}
		instantiatedObjects.Remove(instanceID);
		poolCursors.Remove(instanceID);
	}

	private void Awake()
	{
		if (instance != null)
		{
			Debug.LogWarning("CFX_SpawnSystem: There should only be one instance of CFX_SpawnSystem per Scene!");
		}
		instance = this;
	}

	private void Start()
	{
		allObjectsLoaded = false;
		for (int i = 0; i < objectsToPreload.Length; i++)
		{
			PreloadObject(objectsToPreload[i], objectsToPreloadTimes[i]);
		}
		allObjectsLoaded = true;
	}
}
