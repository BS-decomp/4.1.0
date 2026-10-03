using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
	public static bool DebugAction;

	private static FirebaseManager instance;

	public static FirebaseManager Instance
	{
		get
		{
			if (instance == null)
			{
				GameObject gameObject = new GameObject("Firebase");
				instance = gameObject.AddComponent<FirebaseManager>();
				gameObject.hideFlags = HideFlags.HideInHierarchy;
				Object.DontDestroyOnLoad(gameObject);
			}
			return instance;
		}
	}
}
