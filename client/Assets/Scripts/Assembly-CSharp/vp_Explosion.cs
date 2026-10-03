using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class vp_Explosion : MonoBehaviour
{
	public float Radius = 15f;

	public float Force = 1000f;

	public float UpForce = 10f;

	public float Damage = 10f;

	public float CameraShake = 1f;

	public string DamageMessageName = "Damage";

	private AudioSource m_Audio;

	public AudioClip Sound;

	public float SoundMinPitch = 0.8f;

	public float SoundMaxPitch = 1.2f;

	public List<GameObject> FXPrefabs = new List<GameObject>();

	protected Transform m_Transform;

	protected virtual void Awake()
	{
		m_Transform = base.transform;
		m_Audio = base.GetComponent<AudioSource>();
	}

	private void OnEnable()
	{
		foreach (GameObject fXPrefab in FXPrefabs)
		{
			if (fXPrefab != null)
			{
				Component[] components = fXPrefab.GetComponents<vp_Explosion>();
				if (components.Length == 0)
				{
					vp_Utility.Instantiate(fXPrefab, m_Transform.position, m_Transform.rotation);
				}
				else
				{
					Debug.LogError("Error: vp_Explosion->FXPrefab must not be a vp_Explosion (risk of infinite loop).");
				}
			}
		}
		Collider[] array = Physics.OverlapSphere(m_Transform.position, Radius, -738197525);
		Collider[] array2 = array;
		foreach (Collider collider in array2)
		{
			if (!(collider != base.GetComponent<Collider>()))
			{
				continue;
			}
			float num = 1f - Vector3.Distance(m_Transform.position, collider.transform.position) / Radius;
			if ((bool)collider.GetComponent<Rigidbody>())
			{
				Ray ray = new Ray(collider.transform.position, -Vector3.up);
				RaycastHit hitInfo;
				if (!Physics.Raycast(ray, out hitInfo, 1f))
				{
					UpForce = 0f;
				}
				collider.GetComponent<Rigidbody>().AddExplosionForce(Force / Time.timeScale / vp_TimeUtility.AdjustedTimeScale, m_Transform.position, Radius, UpForce);
			}
			else
			{
				vp_TargetEvent<Vector3>.Send(collider.transform.root, "ForceImpact", (collider.transform.position - m_Transform.position).normalized * Force * 0.001f * num);
				vp_TargetEvent<float>.Send(collider.transform.root, "BombShake", num * CameraShake);
			}
			if (collider.gameObject.layer != 29)
			{
				collider.gameObject.BroadcastMessage(DamageMessageName, num * Damage, SendMessageOptions.DontRequireReceiver);
			}
		}
		m_Audio.clip = Sound;
		m_Audio.pitch = Random.Range(SoundMinPitch, SoundMaxPitch) * Time.timeScale;
		if (!m_Audio.playOnAwake)
		{
			m_Audio.Play();
		}
	}

	private void Update()
	{
		if (!m_Audio.isPlaying)
		{
			vp_Utility.Destroy(base.gameObject);
		}
	}
}
