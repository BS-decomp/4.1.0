using System;
using UnityEngine;

[Serializable]
public class vp_Respawner : MonoBehaviour
{
	public enum SpawnMode
	{
		SamePosition = 0,
		SpawnPoint = 1
	}

	public enum ObstructionSolver
	{
		Wait = 0,
		AdjustPlacement = 1
	}

	public SpawnMode m_SpawnMode;

	public string SpawnPointTag = string.Empty;

	public ObstructionSolver m_ObstructionSolver;

	public float ObstructionRadius = 1f;

	public float MinRespawnTime = 3f;

	public float MaxRespawnTime = 3f;

	public bool SpawnOnAwake;

	public AudioClip SpawnSound;

	public GameObject[] SpawnFXPrefabs;

	protected Vector3 m_InitialPosition = Vector3.zero;

	protected Quaternion m_InitialRotation;

	protected vp_Placement Placement = new vp_Placement();

	protected Transform m_Transform;

	protected AudioSource m_Audio;

	protected bool m_IsInitialSpawnOnAwake;

	protected vp_Timer.Handle m_RespawnTimer = new vp_Timer.Handle();

	protected virtual void Awake()
	{
		m_Transform = base.transform;
		m_Audio = base.GetComponent<AudioSource>();
		Placement.Position = (m_InitialPosition = m_Transform.position);
		Placement.Rotation = (m_InitialRotation = m_Transform.rotation);
		if (m_SpawnMode == SpawnMode.SamePosition)
		{
			SpawnPointTag = string.Empty;
		}
		if (SpawnOnAwake)
		{
			m_IsInitialSpawnOnAwake = true;
			vp_Utility.Activate(base.gameObject, false);
			PickSpawnPoint();
		}
	}

	protected virtual void OnEnable()
	{
	}

	protected virtual void OnDisable()
	{
	}

	protected virtual void SpawnFX()
	{
		if (!m_IsInitialSpawnOnAwake)
		{
			if (m_Audio != null)
			{
				m_Audio.pitch = Time.timeScale;
				m_Audio.PlayOneShot(SpawnSound);
			}
			if (SpawnFXPrefabs != null && SpawnFXPrefabs.Length > 0)
			{
				GameObject[] spawnFXPrefabs = SpawnFXPrefabs;
				foreach (GameObject gameObject in spawnFXPrefabs)
				{
					if (gameObject != null)
					{
						vp_Utility.Instantiate(gameObject, m_Transform.position, m_Transform.rotation);
					}
				}
			}
		}
		m_IsInitialSpawnOnAwake = false;
	}

	protected virtual void Die()
	{
		vp_Timer.In(UnityEngine.Random.Range(MinRespawnTime, MaxRespawnTime), PickSpawnPoint, m_RespawnTimer);
	}

	protected virtual void PickSpawnPoint()
	{
		if (this == null)
		{
			return;
		}
		if (m_SpawnMode == SpawnMode.SamePosition || vp_SpawnPoint.SpawnPoints.Count < 1)
		{
			Placement.Position = m_InitialPosition;
			Placement.Rotation = m_InitialRotation;
			if (Placement.IsObstructed(ObstructionRadius))
			{
				switch (m_ObstructionSolver)
				{
				case ObstructionSolver.Wait:
					vp_Timer.In(UnityEngine.Random.Range(MinRespawnTime, MaxRespawnTime), PickSpawnPoint, m_RespawnTimer);
					return;
				case ObstructionSolver.AdjustPlacement:
					if (!vp_Placement.AdjustPosition(Placement, ObstructionRadius))
					{
						vp_Timer.In(UnityEngine.Random.Range(MinRespawnTime, MaxRespawnTime), PickSpawnPoint, m_RespawnTimer);
						return;
					}
					break;
				}
			}
		}
		else
		{
			switch (m_ObstructionSolver)
			{
			case ObstructionSolver.Wait:
				Placement = vp_SpawnPoint.GetRandomPlacement(0f, SpawnPointTag);
				if (Placement == null)
				{
					Placement = new vp_Placement();
					m_SpawnMode = SpawnMode.SamePosition;
					PickSpawnPoint();
				}
				if (Placement.IsObstructed(ObstructionRadius))
				{
					vp_Timer.In(UnityEngine.Random.Range(MinRespawnTime, MaxRespawnTime), PickSpawnPoint, m_RespawnTimer);
					return;
				}
				break;
			case ObstructionSolver.AdjustPlacement:
				Placement = vp_SpawnPoint.GetRandomPlacement(ObstructionRadius, SpawnPointTag);
				if (Placement == null)
				{
					vp_Timer.In(UnityEngine.Random.Range(MinRespawnTime, MaxRespawnTime), PickSpawnPoint, m_RespawnTimer);
					return;
				}
				break;
			}
		}
		Respawn();
	}

	protected virtual void PickSpawnPoint(Vector3 position, Quaternion rotation)
	{
		Placement.Position = position;
		Placement.Rotation = rotation;
		Respawn();
	}

	protected virtual void Respawn()
	{
		vp_Utility.Activate(base.gameObject);
		SpawnFX();
		SendMessage("Reset");
		Placement.Position = m_InitialPosition;
		Placement.Rotation = m_InitialRotation;
	}

	public virtual void Reset()
	{
		if (Application.isPlaying)
		{
			m_Transform.position = Placement.Position;
			if (base.GetComponent<Rigidbody>() != null && !base.GetComponent<Rigidbody>().isKinematic)
			{
				base.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
				base.GetComponent<Rigidbody>().velocity = Vector3.zero;
			}
		}
	}
}
