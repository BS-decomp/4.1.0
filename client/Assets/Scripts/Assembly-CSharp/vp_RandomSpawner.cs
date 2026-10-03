using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class vp_RandomSpawner : MonoBehaviour
{
	private AudioSource m_Audio;

	public AudioClip Sound;

	public float SoundMinPitch = 0.8f;

	public float SoundMaxPitch = 1.2f;

	public bool RandomAngle = true;

	public List<GameObject> SpawnObjects;

	private void Awake()
	{
		if (SpawnObjects == null)
		{
			return;
		}
		int index = Random.Range(0, SpawnObjects.Count);
		if (!(SpawnObjects[index] == null))
		{
			GameObject gameObject = (GameObject)vp_Utility.Instantiate(SpawnObjects[index], base.transform.position, base.transform.rotation);
			gameObject.transform.Rotate(Random.rotation.eulerAngles);
			m_Audio = base.GetComponent<AudioSource>();
			m_Audio.playOnAwake = true;
			if (Sound != null)
			{
				m_Audio.rolloffMode = AudioRolloffMode.Linear;
				m_Audio.clip = Sound;
				m_Audio.pitch = Random.Range(SoundMinPitch, SoundMaxPitch) * Time.timeScale;
				m_Audio.Play();
			}
		}
	}
}
