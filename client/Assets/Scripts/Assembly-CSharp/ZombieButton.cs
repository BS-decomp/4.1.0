using UnityEngine;

public class ZombieButton : MonoBehaviour
{
	public Team PlayerTeam = Team.Blue;

	[Range(1f, 20f)]
	public int Button = 1;

	public KeyCode Keycode;

	public MeshRenderer ButtonRenderer;

	public Transform ButtonRedBlock;

	private bool isTrigger;

	private bool isClickButton;

	private bool Active = true;

	private void Start()
	{
		EventManager.AddListener("StartRound", StartRound);
		EventManager.AddListener("WaitPlayer", StartRound);
		PhotonEvent.AddListener(PhotonEventTag.ClickButton, DeactiveButton);
	}

	private void OnEnable()
	{
		InputManager.GetButtonDownEvent += GetButtonDown;
	}

	private void OnDisable()
	{
		InputManager.GetButtonDownEvent -= GetButtonDown;
	}

	private void GetButtonDown(string name)
	{
		if (name == "Fire" && isTrigger && !isClickButton && ButtonRenderer.isVisible)
		{
			ClickButton();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!isClickButton)
		{
			PlayerInput component = other.GetComponent<PlayerInput>();
			if (component != null && component.PlayerTeam == PlayerTeam)
			{
				isTrigger = true;
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (!isClickButton)
		{
			PlayerInput component = other.GetComponent<PlayerInput>();
			if (component != null && component.PlayerTeam == PlayerTeam)
			{
				isTrigger = false;
			}
		}
	}

	private void StartRound()
	{
		isClickButton = false;
		isTrigger = false;
		Active = false;
		TimerManager.In(0.2f, () =>
		{
			Active = true;
			if (ButtonRedBlock != null)
			{
				ButtonRedBlock.localPosition = new Vector3(-0.1f, 0.2f, 0f);
			}
		});
	}

	private void ClickButton()
	{
		if (Active)
		{
			PhotonEvent.RPC(PhotonEventTag.ClickButton, PhotonTargets.All, Button);
			isClickButton = true;
			isTrigger = false;
		}
	}

	public void DeactiveButton(PhotonEventData data)
	{
		if ((int)data.parameters[0] == Button)
		{
			isClickButton = true;
			isTrigger = false;
			EventManager.Dispatch("Button" + Button);
			if (ButtonRedBlock != null)
			{
				ButtonRedBlock.localPosition = new Vector3(-0.1f, 0.198f, -0.02f);
			}
		}
	}

	[ContextMenu("Click Button")]
	private void GetClickButton()
	{
		ClickButton();
	}
}
