using System.Collections;
using UnityEngine;

public class PlayerSkinRagdoll : TimerBehaviour
{
	public PlayerSkin Player;

	public float Force = 100f;

	public Collider[] Colliders;

	public Rigidbody[] Rigidbodies;

	public Transform[] Transforms;

	public Vector3[] Positions;

	public Quaternion[] Rotations;

	private bool Actived;

	private void Start()
	{
		for (int i = 0; i < Rigidbodies.Length; i++)
		{
			Rigidbodies[i].detectCollisions = false;
		}
	}

	public void Active()
	{
		Active(Vector3.zero, false);
	}

	public void Active(Vector3 force, bool head)
	{
		if (Player.PlayerRenderer.isVisible)
		{
			StartCoroutine(ActiveCoroutine(force, head));
			return;
		}
		if (Actived)
		{
			Deactive();
		}
		base.gameObject.SetActive(false);
	}

	private IEnumerator ActiveCoroutine(Vector3 force, bool head)
	{
		if (!Actived)
		{
			base.addTimer = TimerManager.In(2f, () =>
			{
				if (Player.Dead)
				{
					if (Actived)
					{
						Deactive();
						base.gameObject.SetActive(false);
					}
				}
				else
				{
					Deactive();
				}
			});
		}
		Actived = true;
		if (force.magnitude < 0.5f)
		{
			force = new Vector3(0f, 0f, Random.Range(-1, 1));
		}
		Player.PlayerAnimator.enabled = false;
		for (int i = 0; i < Colliders.Length; i++)
		{
			Colliders[i].isTrigger = false;
		}
		for (int i2 = 0; i2 < Rigidbodies.Length; i2++)
		{
			Rigidbodies[i2].detectCollisions = true;
			Rigidbodies[i2].isKinematic = false;
			Rigidbodies[i2].velocity = Vector3.zero;
		}
		for (int i3 = 0; i3 < Rigidbodies.Length; i3++)
		{
			Rigidbodies[i3].AddForce(force * Force);
			yield return new WaitForEndOfFrame();
		}
	}

	public void Deactive()
	{
		StartCoroutine(DeactiveCoroutine());
	}

	private IEnumerator DeactiveCoroutine()
	{
		Actived = false;
		Player.PlayerAnimator.enabled = true;
		for (int i = 0; i < Rigidbodies.Length; i++)
		{
			Rigidbodies[i].detectCollisions = false;
			Rigidbodies[i].isKinematic = true;
			yield return new WaitForEndOfFrame();
		}
		for (int j = 0; j < Colliders.Length; j++)
		{
			Colliders[j].isTrigger = true;
		}
	}
}
