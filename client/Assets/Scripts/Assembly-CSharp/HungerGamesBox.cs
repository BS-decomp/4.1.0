using System;
using System.Collections.Generic;
using UnityEngine;

public class HungerGamesBox : MonoBehaviour
{
	[Serializable]
	public class WeaponData
	{
		[SelectedWeapon]
		public int Weapon;
	}

	[Range(1f, 50f)]
	public int ID = 1;

	public bool Used;

	public List<WeaponData> Weapons = new List<WeaponData>();

	private void Start()
	{
		EventManager.AddListener("StartRound", StartRound);
		EventManager.AddListener<int, int>("EventPickupBox", Pickup);
	}

	private void StartRound()
	{
		base.gameObject.SetActive(true);
	}

	private void SelectWeapon()
	{
		Used = true;
		WeaponData weaponData = Weapons[UnityEngine.Random.Range(0, Weapons.Count)];
		HungerGames.SetWeapon(weaponData.Weapon);
	}

	private void Pickup(int id, int pickupPlayer)
	{
		if (id == ID)
		{
			base.gameObject.SetActive(false);
			if (pickupPlayer == PhotonNetwork.player.ID)
			{
				SelectWeapon();
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (GameManager.GetRoundState() != RoundState.EndRound && other.CompareTag("Player"))
		{
			PlayerInput component = other.GetComponent<PlayerInput>();
			if (component != null)
			{
				HungerGames.PickupBox(ID);
			}
		}
	}
}
