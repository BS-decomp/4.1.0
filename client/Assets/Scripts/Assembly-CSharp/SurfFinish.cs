using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

public class SurfFinish : MonoBehaviour
{
	public ObscuredInt Money;

	public ObscuredInt XP;

	private void OnTriggerEnter(Collider other)
	{
		PlayerInput component = other.GetComponent<PlayerInput>();
		if (component != null)
		{
			SurfMode.FinishMap(Money, XP);
		}
	}
}
