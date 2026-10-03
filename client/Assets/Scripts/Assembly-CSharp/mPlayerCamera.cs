using DG.Tweening;
using UnityEngine;

public class mPlayerCamera : MonoBehaviour
{
	public GameObject Player;

	public SkinnedMeshAtlas PlayerAtlas;

	public Transform Point;

	public float RotateSpeed = 200f;

	private Camera mCamera;

	private static mPlayerCamera instance;

	private void Awake()
	{
		instance = this;
		mCamera = base.GetComponent<Camera>();
		RotateSpeed = Mathf.Sqrt(RotateSpeed) / Mathf.Sqrt(Screen.dpi);
	}

	public static void Show()
	{
		instance.mCamera.enabled = true;
		instance.Player.SetActive(true);
	}

	public static void Close()
	{
		instance.mCamera.enabled = false;
		instance.Player.SetActive(false);
	}

	public static void Rotate(Vector2 rotate)
	{
		instance.Point.Rotate(new Vector2(0f, (0f - rotate.x) * instance.RotateSpeed));
	}

	public static void SetSkin(Team team, int skin)
	{
		instance.PlayerAtlas.spriteName = (int)team + "-" + skin;
	}

	public static void ResetRotateX()
	{
		instance.Point.DOLocalRotate(Vector3.zero, 0.2f);
	}

	public static void EasterEggs()
	{
		Animator anim = instance.Player.GetComponent<Animator>();
		anim.SetBool("Anim", true);
		TimerManager.In(0.1f, () =>
		{
			anim.SetBool("Anim", false);
		});
	}
}
