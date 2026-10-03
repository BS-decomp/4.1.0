using UnityEngine;

public class PickupWeaponTrigger : MonoBehaviour
{
	[SelectedWeapon(WeaponType.Rifle)]
	public int Weapon;

	private void OnTriggerEnter(Collider other)
	{
		PlayerInput player = other.GetComponent<PlayerInput>();
		if (player != null && !player.PlayerWeapon.GetWeaponData(WeaponType.Rifle).Enabled)
		{
			WeaponManager.SetSelectWeapon(WeaponType.Rifle, Weapon);
			player.PlayerWeapon.UpdateWeaponData(WeaponType.Rifle);
			TimerManager.In(0.05f, () =>
			{
				player.PlayerWeapon.SetWeapon(WeaponType.Rifle, false);
			});
		}
	}
}
