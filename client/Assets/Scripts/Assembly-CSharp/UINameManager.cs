using UnityEngine;

public class UINameManager : MonoBehaviour
{
	public Camera m_Camera;

	private UILabel Label;

	private int TimerID;

	private string LastName;

	private string PlayerName;

	private string HitName;

	private Ray _Ray;

	private RaycastHit _RaycastHit;

	private void Start()
	{
		PlayerName = PhotonNetwork.playerName;
		Label = UIGameManager.instance.NameLabel;
	}

	private void OnEnable()
	{
		TimerID = TimerManager.In(0.2f, -1, 0.1f, UpdateName);
	}

	private void OnDisable()
	{
		try
		{
			TimerManager.Cancel(TimerID);
			Label.text = string.Empty;
		}
		catch
		{
		}
	}

	private void UpdateName()
	{
		_Ray = m_Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
		if (Physics.Raycast(_Ray, out _RaycastHit, 100f))
		{
			if (_RaycastHit.collider.GetComponent<Collider>().CompareTag("PlayerSkin"))
			{
				HitName = _RaycastHit.transform.root.name;
				if (HitName != LastName)
				{
					if (HitName != PlayerName)
					{
						ControllerManager component = _RaycastHit.transform.root.GetComponent<ControllerManager>();
						if (component.PlayerSkin.PlayerTeam == Team.Blue)
						{
							Label.effectColor = Color.blue;
						}
						else
						{
							Label.effectColor = Color.red;
						}
						LastName = HitName;
						Label.text = LastName;
					}
				}
				else
				{
					Label.text = LastName;
				}
			}
			else
			{
				Label.text = string.Empty;
			}
		}
		else
		{
			Label.text = string.Empty;
		}
	}
}
