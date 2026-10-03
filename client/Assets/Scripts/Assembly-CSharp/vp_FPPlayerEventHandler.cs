using System;
using UnityEngine;

public class vp_FPPlayerEventHandler : vp_PlayerEventHandler
{
	public vp_Value<bool> AllowGameplayInput;

	public vp_Value<bool> Pause;

	public vp_Value<Vector3> EarthQuakeForce;

	public vp_Activity<Vector3> Earthquake;

	public vp_Attempt SetPrevWeapon;

	public vp_Attempt SetNextWeapon;

	public vp_Attempt<string> SetWeaponByName;

	[Obsolete("Please use the 'CurrentWeaponIndex' vp_Value instead.")]
	public vp_Value<int> CurrentWeaponID;

	public vp_Value<int> CurrentWeaponIndex;

	public vp_Value<string> CurrentWeaponName;

	public vp_Value<bool> CurrentWeaponWielded;

	public vp_Attempt AutoReload;

	public vp_Value<float> CurrentWeaponReloadDuration;

	public vp_Value<vp_Interactable> Interactable;

	public vp_Value<bool> CanInteract;

	public vp_Attempt RefillCurrentWeapon;

	public vp_Value<int> CurrentWeaponAmmoCount;

	public vp_Value<int> CurrentWeaponMaxAmmoCount;

	public vp_Value<int> CurrentWeaponClipCount;

	public vp_Attempt<object> AddItem;

	public vp_Attempt<object> RemoveItem;

	public vp_Attempt DepleteAmmo;

	public vp_Value<string> CurrentWeaponClipType;

	public vp_Attempt<object> AddAmmo;

	public vp_Attempt RemoveClip;
}
