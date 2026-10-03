using UnityEngine;

public class ZombieSounds : TimerBehaviour
{
	public AudioClip[] Clips;

	public AudioSource Source;

	private int id;

	private void OnEnable()
	{
		PlaySound();
	}

	private void OnDisable()
	{
		TimerManager.Cancel(id);
		Source.Stop();
	}

	private void PlaySound()
	{
		id = TimerManager.In(Random.Range(5, 10), -1, Random.Range(5, 10), () =>
		{
			if (!Source.isPlaying && GameManager.GetRoundState() != RoundState.EndRound)
			{
				AudioClip clip = Clips[Random.Range(0, Clips.Length)];
				Source.clip = clip;
				Source.Play();
			}
		});
	}
}
