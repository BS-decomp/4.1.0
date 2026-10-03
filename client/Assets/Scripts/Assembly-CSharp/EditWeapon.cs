using UnityEngine;

public class EditWeapon : MonoBehaviour
{
	[SelectedWeapon]
	public int WeaponID;

	public Transform RootMesh;

	public GameObject[] Meshes;

	public Material DefaultMaterial;

	public Vector3 DefaultPosition;

	public Vector3 DefaultRotation;

	public Vector3 MaxScale;

	public void SetDefaultPosition(bool rotation)
	{
		RootMesh.localPosition = DefaultPosition;
		if (rotation)
		{
			RootMesh.localEulerAngles = DefaultRotation;
		}
	}
}
