using UnityEngine;

public class DrawElements : MonoBehaviour
{
	public Color m_Color = Color.red;

	public Vector3 Size = Vector3.one;

	private Transform m_Transform;

	private void Start()
	{
		m_Transform = base.transform;
	}

	public Transform GetTransform()
	{
		return m_Transform;
	}

	public Vector3 GetSpawnPosition()
	{
		Vector3 position = m_Transform.position;
		position.x += Random.Range((0f - Size.x) / 2f, Size.x / 2f);
		position.z += Random.Range((0f - Size.z) / 2f, Size.z / 2f);
		return position;
	}

	public Vector3 GetSpawnRotation()
	{
		return m_Transform.eulerAngles;
	}
}
