using System.Collections.Generic;
using Photon;
using UnityEngine;

public class BombManager : PunBehaviour
{
	public GameObject Bomb;

	public Transform ZoneA;

	public Transform ZoneB;

	private int PlayerBomb = -1;

	private int PlayerDeactiveBomb = -1;

	private int Zone = -1;

	private byte ZoneBomb = 10;

	public static bool BombPlaced;

	private bool BombPlacing;

	public BombAudio BombAudio;

	public ParticleSystem Effect;

	private static BombManager instance;

	private void Awake()
	{
		if (PhotonNetwork.offlineMode)
		{
			Object.Destroy(this);
			return;
		}
		if (PhotonNetwork.room.GetGameMode() != GameMode.Bomb)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		PhotonClassesManager.Add(this);
		instance = this;
		ZoneA.gameObject.SetActive(true);
		ZoneB.gameObject.SetActive(true);
	}

	private void Start()
	{
		PhotonEvent.AddListener(1, SetStartRoundBomb);
		PhotonEvent.AddListener(2, PhotonMasterPickupBomb);
		PhotonEvent.AddListener(3, PhotonPickupBomb);
		PhotonEvent.AddListener(4, PhotonSetBomb);
		PhotonEvent.AddListener(5, PhotonDeactiveBomb);
		PhotonEvent.AddListener(6, PhotonDeactiveBoom);
		PhotonEvent.AddListener(7, PhotonDeactiveBombExit);
		PhotonEvent.AddListener(8, PhotonSetPosition);
		PhotonEvent.AddListener(9, PhotonOnPhotonPlayerConnected);
		EventManager.AddListener("StartRound", StartRound);
		EventManager.AddListener("WaitPlayer", StartRound);
	}

	private void OnEnable()
	{
		InputManager.GetButtonDownEvent += GetButtonDown;
		InputManager.GetButtonUpEvent += GetButtonUp;
	}

	private void OnDisable()
	{
		InputManager.GetButtonDownEvent -= GetButtonDown;
		InputManager.GetButtonUpEvent -= GetButtonUp;
	}

	private void GetButtonDown(string name)
	{
		if (name == "Use")
		{
			if (PhotonNetwork.player.GetTeam() == Team.Red && Zone != -1 && PlayerBomb == PhotonNetwork.player.ID && !BombPlaced)
			{
				UIGameManager.StartDuration(Random.Range(4f, 6f), SetBomb);
				PlayerInput.instance.SetMove(false);
				BombPlacing = true;
			}
			else if (PhotonNetwork.player.GetTeam() == Team.Blue && Zone != -1 && PlayerDeactiveBomb == PhotonNetwork.player.ID && BombPlaced)
			{
				UIGameManager.StartDuration(Random.Range(4f, 7f), DeactiveBoom);
				PlayerInput.instance.SetMove(false);
				BombPlacing = true;
			}
		}
		else if (BombPlacing)
		{
			UIGameManager.StopDuration();
			if (PlayerInput.instance != null)
			{
				PlayerInput.instance.SetMove(true);
			}
			BombPlacing = false;
		}
	}

	private void GetButtonUp(string name)
	{
		if (name == "Use")
		{
			UIGameManager.StopDuration();
			PlayerInput.instance.SetMove(true);
			BombPlacing = false;
		}
	}

	private void StartRound()
	{
		BombPlaced = false;
		BombPlacing = false;
		Bomb.SetActive(false);
		PlayerBomb = -1;
		PlayerDeactiveBomb = -1;
		Zone = -1;
		ZoneBomb = 10;
		BombAudio.Stop();
		Effect.Stop();
		Effect.GetComponent<AudioSource>().Stop();
		if (GameManager.GetRoundState() == RoundState.WaitPlayer)
		{
			return;
		}
		TimerManager.In(1f, () =>
		{
			if (GameManager.GetRoundState() != RoundState.WaitPlayer && PhotonNetwork.isMasterClient)
			{
				List<PhotonPlayer> list = new List<PhotonPlayer>();
				for (int i = 0; i < PhotonNetwork.playerList.Length; i++)
				{
					if (PhotonNetwork.playerList[i].GetTeam() == Team.Red)
					{
						list.Add(PhotonNetwork.playerList[i]);
					}
				}
				if (list.Count > 0)
				{
					PhotonPlayer photonPlayer = list[Random.Range(0, list.Count)];
					PhotonEvent.RPC(1, PhotonTargets.All, photonPlayer.ID);
				}
			}
		});
	}

	private void SetStartRoundBomb(PhotonEventData data)
	{
		PlayerBomb = (int)data.parameters[0];
		if (PlayerBomb == PhotonNetwork.player.ID)
		{
			UIToast.Show(Localization.Get("You have a bomb"));
		}
		else if (PhotonNetwork.player.GetTeam() == Team.Red)
		{
			UIToast.Show(PhotonPlayer.Find(PlayerBomb).NickName + " " + Localization.Get("picked up a bomb"));
		}
	}

	public override void OnPhotonPlayerConnected(PhotonPlayer playerConnect)
	{
		if (!PhotonNetwork.isMasterClient)
		{
			return;
		}
		TimerManager.In(0.5f, () =>
		{
			if (BombPlaced)
			{
				float num = UIGameManager.instance.ScoreTimer - Time.time;
				PhotonEvent.RPC(9, true, playerConnect, BombPlaced, ZoneBomb, num);
			}
			else
			{
				PhotonEvent.RPC(9, true, playerConnect, BombPlaced, ZoneBomb);
			}
		});
	}

	private void PhotonOnPhotonPlayerConnected(PhotonEventData data)
	{
		BombPlaced = (bool)data.parameters[0];
		ZoneBomb = (byte)data.parameters[1];
		if (BombPlaced)
		{
			float time = (float)data.parameters[2] - (float)(PhotonNetwork.time - data.timestamp);
			UIGameManager.StartScoreTimer(time, Boom);
			BombAudio.Play(time);
		}
		if (ZoneBomb == 1)
		{
			SetPosition(ZoneA.position);
		}
		else if (ZoneBomb == 2)
		{
			SetPosition(ZoneB.position);
		}
	}

	public override void OnPhotonPlayerDisconnected(PhotonPlayer playerDisconnect)
	{
		if (PhotonNetwork.isMasterClient && playerDisconnect.ID == PlayerBomb)
		{
			SetRandomPlayer();
		}
	}

	private void SetRandomPlayer()
	{
		List<PhotonPlayer> list = new List<PhotonPlayer>();
		for (int i = 0; i < PhotonNetwork.playerList.Length; i++)
		{
			if (PhotonNetwork.playerList[i].GetTeam() == Team.Red && !PhotonNetwork.playerList[i].GetDead())
			{
				list.Add(PhotonNetwork.playerList[i]);
			}
		}
		if (list.Count > 0)
		{
			PhotonPlayer photonPlayer = list[Random.Range(0, list.Count)];
			PhotonEvent.RPC(1, PhotonTargets.All, photonPlayer.ID);
		}
	}

	public static void DeadPlayer()
	{
		if (PhotonNetwork.player.ID == instance.PlayerBomb)
		{
			RaycastHit hitInfo;
			if (Physics.Raycast(PlayerInput.instance.PlayerTransform.position, Vector3.down, out hitInfo, 50f))
			{
				PhotonEvent.RPC(8, PhotonTargets.All, hitInfo.point, hitInfo.normal);
			}
			else
			{
				instance.SetRandomPlayer();
			}
		}
		instance.BombPlacing = false;
		UIControllerList.Use.cachedGameObject.SetActive(false);
		UIGameManager.StopDuration();
		PlayerInput.instance.SetMove(true);
	}

	private void PhotonSetPosition(PhotonEventData data)
	{
		if (PlayerBomb != -1 && PhotonNetwork.player.GetTeam() == Team.Red)
		{
			UIToast.Show(PhotonPlayer.Find(PlayerBomb).NickName + " " + Localization.Get("lost the bomb"));
		}
		PlayerBomb = -1;
		SetPosition((Vector3)data.parameters[0], (Vector3)data.parameters[1]);
	}

	public static void SetPosition(Vector3 pos, Vector3 normal)
	{
		instance.Bomb.transform.position = pos + normal * 0.01f;
		instance.Bomb.transform.rotation = Quaternion.LookRotation(normal);
		instance.Bomb.SetActive(true);
	}

	public static void SetPosition(Vector3 pos)
	{
		instance.Bomb.transform.position = pos;
		instance.Bomb.transform.rotation = Quaternion.Euler(-90f, Random.Range(0, 360), 0f);
		instance.Bomb.SetActive(true);
	}

	public void OnTriggerEnterBomb()
	{
		if (GameManager.GetRoundState() == RoundState.PlayRound)
		{
			PhotonEvent.RPC(2, PhotonTargets.MasterClient);
		}
	}

	public void OnTriggerExitBomb()
	{
		if (BombPlaced)
		{
			UIControllerList.Use.cachedGameObject.SetActive(false);
			if (PhotonNetwork.player.ID == PlayerDeactiveBomb)
			{
				PhotonEvent.RPC(7, PhotonTargets.All);
			}
		}
	}

	private void PhotonMasterPickupBomb(PhotonEventData data)
	{
		PhotonPlayer player = PhotonPlayer.Find(data.senderID);
		if (BombPlaced)
		{
			if (player.GetTeam() == Team.Blue && PlayerDeactiveBomb == -1)
			{
				PhotonEvent.RPC(5, PhotonTargets.All, data.senderID);
			}
		}
		else if (player.GetTeam() == Team.Red && PlayerBomb == -1)
		{
			PhotonEvent.RPC(3, PhotonTargets.All, data.senderID);
		}
	}

	private void PhotonPickupBomb(PhotonEventData data)
	{
		Bomb.SetActive(false);
		PlayerBomb = (int)data.parameters[0];
		if (PlayerBomb == PhotonNetwork.player.ID)
		{
			UIToast.Show(Localization.Get("You have a bomb"));
		}
	}

	private void PhotonDeactiveBomb(PhotonEventData data)
	{
		PlayerDeactiveBomb = (int)data.parameters[0];
		if (PlayerDeactiveBomb == PhotonNetwork.player.ID)
		{
			UIControllerList.Use.cachedGameObject.SetActive(true);
		}
	}

	private void PhotonDeactiveBombExit(PhotonEventData data)
	{
		PlayerDeactiveBomb = -1;
	}

	public void OnTriggerEnterZone(int zone)
	{
		Zone = zone;
		if (PhotonNetwork.player.GetTeam() == Team.Red && PlayerBomb == PhotonNetwork.player.ID && GameManager.GetRoundState() == RoundState.PlayRound)
		{
			UIControllerList.Use.cachedGameObject.SetActive(true);
		}
	}

	public void OnTriggerExitZone()
	{
		Zone = -1;
		UIControllerList.Use.cachedGameObject.SetActive(false);
		UIGameManager.StopDuration();
		PlayerInput.instance.SetMove(true);
		BombPlacing = false;
	}

	private void SetBomb()
	{
		UIControllerList.Use.cachedGameObject.SetActive(false);
		UIGameManager.StopDuration();
		PlayerInput.instance.SetMove(true);
		if (GameManager.GetRoundState() == RoundState.PlayRound)
		{
			PhotonEvent.RPC(4, true, PhotonTargets.All, (byte)Zone);
		}
	}

	private void PhotonSetBomb(PhotonEventData data)
	{
		if (GameManager.GetRoundState() == RoundState.PlayRound)
		{
			UIToast.Show(Localization.Get("The bomb has been planted"));
			BombPlacing = false;
			BombPlaced = true;
			PlayerBomb = -1;
			float time = 35f - (float)(PhotonNetwork.time - data.timestamp);
			UIGameManager.StartScoreTimer(time, Boom);
			ZoneBomb = (byte)data.parameters[0];
			if (ZoneBomb == 1)
			{
				SetPosition(ZoneA.position);
			}
			else
			{
				SetPosition(ZoneB.position);
			}
			BombAudio.Play(time);
		}
	}

	private void Boom()
	{
		BombAudio.Boom();
		Effect.Play();
		Effect.GetComponent<AudioSource>().Play();
		Effect.transform.position = Bomb.transform.position;
		BombAudio.Stop();
		Bomb.SetActive(false);
		BombPlacing = false;
		BombPlaced = false;
		PlayerBomb = -1;
		PlayerDeactiveBomb = -1;
		UIGameManager.instance.isScoreTimer = false;
		BombMode.instance.Boom();
		UIGameManager.StopDuration();
		if (PlayerInput.instance.isAwake)
		{
			PlayerInput.instance.SetMove(true);
			int value = (50 - (int)Vector3.Distance(PlayerInput.instance.PlayerTransform.position, Bomb.transform.position)) * 5;
			value = Mathf.Clamp(value, 0, 100);
			if (value > 0)
			{
				DamageInfo damageInfo = DamageInfo.Get(value, Vector3.zero, Team.None, 0, -1, false);
				PlayerInput.instance.Damage(damageInfo);
			}
			PlayerInput.instance.FPCamera.AddRollForce(Random.Range(-3, 3));
		}
	}

	private void DeactiveBoom()
	{
		UIControllerList.Use.cachedGameObject.SetActive(false);
		UIGameManager.StopDuration();
		PlayerInput.instance.SetMove(true);
		PhotonEvent.RPC(6, PhotonTargets.All);
	}

	private void PhotonDeactiveBoom(PhotonEventData data)
	{
		BombAudio.Stop();
		Bomb.SetActive(false);
		Bomb.SetActive(false);
		BombPlacing = false;
		BombPlaced = false;
		PlayerBomb = -1;
		PlayerDeactiveBomb = -1;
		UIGameManager.instance.isScoreTimer = false;
		BombMode.instance.DeactiveBoom();
	}
}
