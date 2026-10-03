using UnityEngine;

public class TrapAddForce : MonoBehaviour
{
	[Range(1f, 20f)]
	public int Key = 1;

	public Vector3 Force;

	public float Delay;

	private bool Activated;

	private bool ActiveForce;

	private bool isTrigger;

	private void Start()
	{
		EventManager.AddListener("StartRound", StartRound);
		EventManager.AddListener("WaitPlayer", StartRound);
		EventManager.AddListener("Button" + Key, ActiveTrap);
	}

	private void ActiveTrap()
	{
		if (!Activated)
		{
			Activated = true;
			ActiveForce = true;
			if (isTrigger)
			{
				PlayerInput.instance.FPController.AddForce(Force);
			}
			TimerManager.In(Delay, () =>
			{
				ActiveForce = false;
			});
		}
	}

	private void StartRound()
	{
		Activated = false;
		isTrigger = false;
		ActiveForce = false;
	}

	private void OnTriggerEnter(Collider other)
	{
		PlayerInput component = other.GetComponent<PlayerInput>();
		if (component != null)
		{
			isTrigger = true;
			if (ActiveForce)
			{
				component.FPController.AddForce(Force);
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (isTrigger)
		{
			PlayerInput component = other.GetComponent<PlayerInput>();
			if (component != null)
			{
				isTrigger = false;
			}
		}
	}
}
