using UnityEngine;

[RequireComponent(typeof(vp_Shooter))]
public class vp_SimpleAITurret : MonoBehaviour
{
	public float ViewRange = 10f;

	public float AimSpeed = 50f;

	public float WakeInterval = 2f;

	protected vp_Shooter m_Shooter;

	protected Transform m_Transform;

	protected Transform m_Target;

	protected vp_Timer.Handle m_Timer = new vp_Timer.Handle();

	private void Start()
	{
		m_Shooter = GetComponent<vp_Shooter>();
		m_Transform = base.transform;
	}

	private void Update()
	{
		if (!m_Timer.Active)
		{
			vp_Timer.In(WakeInterval, () =>
			{
				if (m_Target == null)
				{
					m_Target = ScanForLocalPlayer();
				}
				else
				{
					m_Target = null;
				}
			}, m_Timer);
		}
		if (m_Target != null)
		{
			AttackTarget();
		}
	}

	private Transform ScanForLocalPlayer()
	{
		Collider[] array = Physics.OverlapSphere(m_Transform.position, ViewRange, 1073741824);
		Collider[] array2 = array;
		foreach (Collider collider in array2)
		{
			RaycastHit hitInfo;
			Physics.Linecast(m_Transform.position, collider.transform.position + Vector3.up, out hitInfo);
			if (!(hitInfo.collider != null) || !(hitInfo.collider != collider))
			{
				return collider.transform;
			}
		}
		return null;
	}

	private void AttackTarget()
	{
		Vector3 forward = m_Target.collider.bounds.center - m_Transform.position;
		Quaternion to = Quaternion.LookRotation(forward);
		m_Transform.rotation = Quaternion.RotateTowards(m_Transform.rotation, to, Time.deltaTime * AimSpeed);
		m_Shooter.TryFire();
	}
}
