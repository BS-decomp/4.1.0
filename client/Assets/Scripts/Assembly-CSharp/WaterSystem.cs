using UnityEngine;

public class WaterSystem : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		PlayerInput component = other.GetComponent<PlayerInput>();
		if (!(component == null))
		{
			component.SetWater(true);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		PlayerInput component = other.GetComponent<PlayerInput>();
		if (!(component == null))
		{
			component.SetWater(false);
		}
	}
}
