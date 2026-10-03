using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class vp_HitscanBullet : MonoBehaviour
{
	public bool IgnoreLocalPlayer = true;

	public float Range = 100f;

	public float Force = 100f;

	public float Damage = 1f;

	public string DamageMethodName = "Damage";

	public float m_SparkFactor = 0.5f;

	public GameObject m_ImpactPrefab;

	public GameObject m_DustPrefab;

	public GameObject m_SparkPrefab;

	public GameObject m_DebrisPrefab;

	protected AudioSource m_Audio;

	public List<AudioClip> m_ImpactSounds = new List<AudioClip>();

	public Vector2 SoundImpactPitch = new Vector2(1f, 1.5f);

	public int[] NoDecalOnTheseLayers;

	protected Transform m_Transform;

	protected Renderer m_Renderer;

	protected bool m_Initialized;

	private void Awake()
	{
		m_Transform = base.transform;
		m_Renderer = base.GetComponent<Renderer>();
		m_Audio = base.GetComponent<AudioSource>();
	}

	private void Start()
	{
		m_Initialized = true;
		DoHit();
	}

	private void OnEnable()
	{
		if (m_Initialized)
		{
			DoHit();
		}
	}

	private void DoHit()
	{
		Ray ray = new Ray(m_Transform.position, base.transform.forward);
		RaycastHit hitInfo;
		if (Physics.Raycast(ray, out hitInfo, Range, (!IgnoreLocalPlayer) ? (-738197525) : (-1828716565)))
		{
			Vector3 localScale = m_Transform.localScale;
			m_Transform.parent = hitInfo.transform;
			m_Transform.localPosition = hitInfo.transform.InverseTransformPoint(hitInfo.point);
			m_Transform.rotation = Quaternion.LookRotation(hitInfo.normal);
			if (hitInfo.transform.lossyScale == Vector3.one)
			{
				m_Transform.Rotate(Vector3.forward, Random.Range(0, 360), Space.Self);
			}
			else
			{
				m_Transform.parent = null;
				m_Transform.localScale = localScale;
				m_Transform.parent = hitInfo.transform;
			}
			Rigidbody attachedRigidbody = hitInfo.GetComponent<Collider>().attachedRigidbody;
			if (attachedRigidbody != null && !attachedRigidbody.isKinematic)
			{
				attachedRigidbody.AddForceAtPosition(ray.direction * Force / Time.timeScale / vp_TimeUtility.AdjustedTimeScale, hitInfo.point);
			}
			if (m_ImpactPrefab != null)
			{
				vp_Utility.Instantiate(m_ImpactPrefab, m_Transform.position, m_Transform.rotation);
			}
			if (m_DustPrefab != null)
			{
				vp_Utility.Instantiate(m_DustPrefab, m_Transform.position, m_Transform.rotation);
			}
			if (m_SparkPrefab != null && Random.value < m_SparkFactor)
			{
				vp_Utility.Instantiate(m_SparkPrefab, m_Transform.position, m_Transform.rotation);
			}
			if (m_DebrisPrefab != null)
			{
				vp_Utility.Instantiate(m_DebrisPrefab, m_Transform.position, m_Transform.rotation);
			}
			if (m_ImpactSounds.Count > 0)
			{
				m_Audio.pitch = Random.Range(SoundImpactPitch.x, SoundImpactPitch.y) * Time.timeScale;
				m_Audio.clip = m_ImpactSounds[Random.Range(0, m_ImpactSounds.Count)];
				m_Audio.Stop();
				m_Audio.Play();
			}
			hitInfo.GetComponent<Collider>().SendMessageUpwards(DamageMethodName, Damage, SendMessageOptions.DontRequireReceiver);
			if (NoDecalOnTheseLayers.Length > 0)
			{
				int[] noDecalOnTheseLayers = NoDecalOnTheseLayers;
				foreach (int num in noDecalOnTheseLayers)
				{
					if (hitInfo.transform.gameObject.layer == num)
					{
						TryDestroy();
						return;
					}
				}
			}
			if (m_Renderer != null)
			{
				vp_DecalManager.Add(base.gameObject);
			}
			else
			{
				vp_Timer.In(1f, TryDestroy);
			}
		}
		else
		{
			vp_Utility.Destroy(base.gameObject);
		}
	}

	protected void TryDestroy()
	{
		if (!(this == null))
		{
			if (!m_Audio.isPlaying)
			{
				vp_Utility.Destroy(base.gameObject);
			}
			else
			{
				vp_Timer.In(1f, TryDestroy);
			}
		}
	}
}
