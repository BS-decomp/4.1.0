using DG.Tweening;
using UnityEngine;

public class BombAudio : MonoBehaviour
{
	public AudioSource BombAudioSource;

	public AudioClip BombAudioClip;

	public Light BombLight;

	private int BombAudioID;

	private float BombTime;

	private int BombCount;

	private void OnEnable()
	{
		if (BombManager.BombPlaced)
		{
			BombLight.intensity = 0f;
			BombLight.DOIntensity(8f, 0.7f).SetLoops(-1, LoopType.Yoyo);
		}
	}

	private void OnDisable()
	{
		BombLight.DOKill();
	}

	public void Play(float time)
	{
		BombTime = time;
	}

	public void Boom()
	{
		Stop();
	}

	public void Stop()
	{
		BombCount = 0;
		BombTime = -1f;
		TimerManager.Cancel(BombAudioID);
		BombAudioID = 0;
	}

	private void Update()
	{
		if (BombTime > 0.8f)
		{
			if (BombTime > 20f && BombTime < 35f && BombCount != 1)
			{
				BombCount = 1;
				TimerManager.Cancel(BombAudioID);
				BombAudioID = TimerManager.In(0f, -1, 1f, () =>
				{
					BombAudioSource.PlayOneShot(BombAudioClip);
				});
			}
			else if (BombTime > 10f && BombTime < 20f && BombCount != 2)
			{
				BombCount = 2;
				TimerManager.Cancel(BombAudioID);
				BombAudioID = TimerManager.In(0f, -1, 0.5f, () =>
				{
					BombAudioSource.PlayOneShot(BombAudioClip);
				});
			}
			else if (BombTime > 0f && BombTime < 10f && BombCount != 3)
			{
				BombCount = 3;
				TimerManager.Cancel(BombAudioID);
				BombAudioID = TimerManager.In(0f, -1, 0.25f, () =>
				{
					BombAudioSource.PlayOneShot(BombAudioClip);
				});
			}
			BombTime -= Time.deltaTime;
		}
		else if (BombCount != 0)
		{
			BombCount = 0;
			BombTime = -1f;
			TimerManager.Cancel(BombAudioID);
			BombAudioID = 0;
		}
	}
}
