using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

public class BunnySpawn : MonoBehaviour
{
	public bool FinishSpawn;

	public ObscuredInt Money;

	public ObscuredInt XP;

	private void OnTriggerEnter(Collider other)
	{
		PlayerInput component = other.GetComponent<PlayerInput>();
		if (component != null)
		{
			if (FinishSpawn)
			{
				BunnyHop.FinishMap(Money, XP);
				return;
			}
			GameManager.GetTeamSpawn().GetTransform().position = base.transform.position;
			GameManager.GetTeamSpawn().GetTransform().rotation = base.transform.rotation;
		}
	}
}
