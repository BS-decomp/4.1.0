using UnityEngine;

public class DeadTrigger : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			PlayerInput component = other.GetComponent<PlayerInput>();
			if (component != null)
			{
				DamageInfo damageInfo = DamageInfo.Get(1000, Vector3.zero, Team.None, 0, -1, false);
				component.Damage(damageInfo);
			}
		}
	}
}
