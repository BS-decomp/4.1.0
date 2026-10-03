using UnityEngine;

public class UIDeathScreen : MonoBehaviour
{
	public UIPanel Panel;

	public UILabel PlayerLabel;

	public UILabel WeaponLabel;

	public UILabel DamageLabel;

	public UILabel HeadshotLabel;

	public UISprite WeaponSprite;

	private int Timer;

	private static UIDeathScreen instance;

	private void Start()
	{
		instance = this;
	}

	public static void Show(DamageInfo damageInfo)
	{
		if (damageInfo.PlayerID != -1 && damageInfo.PlayerID != PhotonNetwork.player.ID)
		{
			TimerManager.Cancel(instance.Timer);
			instance.Panel.alpha = 0f;
			TweenAlpha.Begin(instance.Panel.cachedGameObject, 0.2f, 1f);
			instance.PlayerLabel.text = PhotonPlayer.Find(damageInfo.PlayerID).NickName;
			instance.DamageLabel.text = Localization.Get("Damage") + ": " + damageInfo.Damage + "  [" + (int)Vector3.Distance(damageInfo.AttackPosition, PlayerInput.instance.PlayerTransform.position) + "m]";
			instance.HeadshotLabel.text = ((!damageInfo.HeadShot) ? string.Empty : Localization.Get("Headshot"));
			instance.SetWeaponData(damageInfo);
			instance.Timer = TimerManager.In(3f, () =>
			{
				TweenAlpha.Begin(instance.Panel.cachedGameObject, 0.2f, 0f);
			});
		}
	}

	private void SetWeaponData(DamageInfo damageInfo)
	{
		WeaponData weaponData = WeaponManager.GetWeaponData(damageInfo.WeaponID);
		WeaponSkinData weaponSkin = WeaponManager.GetWeaponSkin(damageInfo.WeaponID, damageInfo.WeaponSkinID);
		WeaponLabel.text = string.Concat(weaponData.Name, " | ", weaponSkin.Name);
		WeaponLabel.color = GetWeaponSkinQualityColor(weaponSkin.Quality);
		WeaponSprite.spriteName = damageInfo.WeaponID + "-" + damageInfo.WeaponSkinID;
		WeaponSprite.width = (int)GameSettings.instance.WeaponsCaseSize[(int)weaponData.ID - 1].x;
		WeaponSprite.height = (int)GameSettings.instance.WeaponsCaseSize[(int)weaponData.ID - 1].y;
	}

	private Color GetWeaponSkinQualityColor(WeaponSkinQuality quality)
	{
		switch (quality)
		{
		case WeaponSkinQuality.Default:
		case WeaponSkinQuality.Normal:
			return new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		case WeaponSkinQuality.Basic:
			return new Color32(54, 189, byte.MaxValue, byte.MaxValue);
		case WeaponSkinQuality.Professional:
			return new Color32(byte.MaxValue, 0, 0, byte.MaxValue);
		case WeaponSkinQuality.Legendary:
			return new Color32(byte.MaxValue, 0, byte.MaxValue, byte.MaxValue);
		default:
			return Color.white;
		}
	}
}
