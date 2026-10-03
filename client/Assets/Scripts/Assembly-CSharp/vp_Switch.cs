using System.Collections.Generic;
using UnityEngine;

public class vp_Switch : vp_Interactable
{
	public GameObject Target;

	public string TargetMessage = string.Empty;

	public AudioSource AudioSource;

	public Vector2 SwitchPitchRange = new Vector2(1f, 1.5f);

	public List<AudioClip> SwitchSounds = new List<AudioClip>();

	protected override void Start()
	{
		base.Start();
		if (AudioSource == null)
		{
			AudioSource = ((!(base.GetComponent<AudioSource>() == null)) ? base.GetComponent<AudioSource>() : base.gameObject.AddComponent<AudioSource>());
		}
	}

	public override bool TryInteract(vp_FPPlayerEventHandler player)
	{
		if (Target == null)
		{
			return false;
		}
		if (m_Player == null)
		{
			m_Player = player;
		}
		PlaySound();
		Target.SendMessage(TargetMessage, SendMessageOptions.DontRequireReceiver);
		return true;
	}

	public virtual void PlaySound()
	{
		if (!(AudioSource == null) && SwitchSounds.Count != 0)
		{
			AudioClip audioClip = SwitchSounds[Random.Range(0, SwitchSounds.Count)];
			if (!(audioClip == null))
			{
				AudioSource.pitch = Random.Range(SwitchPitchRange.x, SwitchPitchRange.y);
				AudioSource.PlayOneShot(audioClip);
			}
		}
	}

	protected override void OnTriggerEnter(Collider col)
	{
		if (InteractType != vp_InteractType.Trigger)
		{
			return;
		}
		using (List<string>.Enumerator enumerator = RecipientTags.GetEnumerator())
		{
			string current;
			do
			{
				if (enumerator.MoveNext())
				{
					current = enumerator.Current;
					continue;
				}
				return;
			}
			while (!(col.gameObject.tag == current));
		}
		if (m_Player == null)
		{
			m_Player = Object.FindObjectOfType(typeof(vp_FPPlayerEventHandler)) as vp_FPPlayerEventHandler;
		}
		TryInteract(m_Player);
	}
}
