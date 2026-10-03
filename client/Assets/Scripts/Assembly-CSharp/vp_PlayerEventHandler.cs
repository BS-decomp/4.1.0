public class vp_PlayerEventHandler : vp_StateEventHandler
{
	public vp_Activity Dead;

	public vp_Activity Attack;

	public vp_Activity Reload;

	public vp_Activity Climb;

	public vp_Activity Interact;

	public vp_Activity<int> SetWeapon;

	public vp_Message<string, int> GetItemCount;

	protected override void Awake()
	{
		base.Awake();
		BindStateToActivity(Reload);
		BindStateToActivity(Dead);
		BindStateToActivity(Climb);
		BindStateToActivityOnStart(Attack);
		SetWeapon.AutoDuration = 1f;
		Reload.AutoDuration = 1f;
		SetWeapon.MinPause = 0.2f;
	}
}
