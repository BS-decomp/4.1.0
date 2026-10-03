using UnityEngine;

public class BunnyAutoJump : MonoBehaviour
{
	private PlayerInput Player;

	public float jumpTime = 2f;

	private int TimerID;

	private void OnTriggerEnter(Collider other)
	{
		Player = other.GetComponent<PlayerInput>();
		if (Player != null)
		{
			Player.SetBunnyHopAutoJump(true);
			TimerID = TimerManager.In(jumpTime, () =>
			{
				BunnyHop.SpawnDead();
			});
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (Player != null)
		{
			Player.SetBunnyHopAutoJump(false);
			Player = null;
			TimerManager.Cancel(TimerID);
		}
	}
}
