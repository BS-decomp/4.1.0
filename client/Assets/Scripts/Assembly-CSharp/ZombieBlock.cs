using Photon;
using UnityEngine;

public class ZombieBlock : Photon.MonoBehaviour
{
	[Range(1f, 50f)]
	public int ID = 1;

	public int CountAttack = 50;

	[Range(1f, 20f)]
	public int Button = 1;

	public GameObject ActiveBlock;

	private int StartCountAttack = 50;

	private GameObject mCacheGameObject;

	public GameObject CacheGameObject
	{
		get
		{
			if (mCacheGameObject == null)
			{
				mCacheGameObject = base.gameObject;
			}
			return mCacheGameObject;
		}
	}

	private void Start()
	{
		StartCountAttack = CountAttack;
		EventManager.AddListener("Button" + Button, ButtonClick);
		EventManager.AddListener("StartRound", StartRound);
		EventManager.AddListener("WaitPlayer", StartRound);
	}

	private void StartRound()
	{
		SetActive(false);
	}

	private void ButtonClick()
	{
		SetActive(true);
		CountAttack = StartCountAttack;
	}

	public void Damage(DamageInfo info)
	{
		if (info.AttackerTeam == Team.Red)
		{
			UICrosshair.Hit();
			ZombieMode.AddDamage((byte)ID);
		}
	}

	public void Attack()
	{
		CountAttack--;
		CountAttack = Mathf.Clamp(CountAttack, 0, StartCountAttack);
	}

	public void SetActive(bool active)
	{
		CacheGameObject.SetActive(active);
		if (ActiveBlock != null)
		{
			ActiveBlock.SetActive(!active);
		}
	}

	public bool GetActive()
	{
		return CacheGameObject.activeSelf;
	}
}
