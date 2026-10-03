using UnityEngine;

[ExecuteInEditMode]
public class SkyboxManager : MonoBehaviour
{
	public Transform Target;

	private Transform TargetParent;

	private bool isParent;

	[Range(0f, 1f)]
	public float TimeDay;

	public bool Moon;

	public bool Stars;

	public bool Sun;

	public Material SkyboxMaterial;

	public Transform SkyboxCamera;

	public Transform SkyboxCameraParent;

	public GameObject MoonObject;

	public GameObject StarsObject;

	public GameObject SunObject;

	private bool isFixedUpdate;

	private float TimerFixedUpdate;

	private bool Activate;

	private static SkyboxManager instance;

	private void Start()
	{
		if (Application.isPlaying)
		{
			instance = this;
			SkyboxMaterial.mainTextureOffset = new Vector2(TimeDay, 0f);
			MoonObject.SetActive(Moon);
			StarsObject.SetActive(Stars);
			SunObject.SetActive(Sun);
		}
	}

	private void Reset()
	{
		SkyboxMaterial.mainTextureOffset = new Vector2(TimeDay, 0f);
	}

	private void OnDisable()
	{
		isFixedUpdate = true;
	}

	private void FixedUpdate()
	{
		if (isFixedUpdate && Activate)
		{
			SkyboxCamera.rotation = Target.rotation;
			if (isParent)
			{
				SkyboxCameraParent.rotation = TargetParent.rotation;
			}
		}
	}

	private void Update()
	{
		if (isFixedUpdate && Activate)
		{
			if (TimerFixedUpdate < Time.time)
			{
				isFixedUpdate = false;
			}
			return;
		}
		SkyboxCamera.rotation = Target.rotation;
		if (isParent)
		{
			SkyboxCameraParent.rotation = TargetParent.rotation;
		}
	}

	public static void SetTarget(Transform target)
	{
		if (target == null)
		{
			instance.Activate = false;
			return;
		}
		instance.Activate = true;
		instance.isParent = false;
		instance.Target = target;
		if (instance.SkyboxCamera != null)
		{
			instance.SkyboxCamera.rotation = target.rotation;
		}
	}

	public static void SetParent(Transform target)
	{
		instance.TargetParent = target;
		instance.isParent = true;
		instance.SkyboxCameraParent.rotation = target.rotation;
	}

	public static void SetFixedUpdate(float time)
	{
		instance.isFixedUpdate = true;
		instance.TimerFixedUpdate = Time.time + time;
	}

	public static void Deactive()
	{
		instance.Activate = false;
	}
}
