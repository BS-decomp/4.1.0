using UnityEngine;

[RequireComponent(typeof(ParticleSystemRenderer))]
public class WFX_ParticleMeshBillboard : MonoBehaviour
{
	private Mesh mesh;

	private Vector3[] vertices;

	private Vector3[] rvertices;

	private void Awake()
	{
		mesh = (Mesh)Object.Instantiate(GetComponent<ParticleSystemRenderer>().mesh);
		GetComponent<ParticleSystemRenderer>().mesh = mesh;
		vertices = new Vector3[mesh.vertices.Length];
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i] = mesh.vertices[i];
		}
		rvertices = new Vector3[vertices.Length];
	}

	private void OnWillRenderObject()
	{
		if (!(mesh == null) && !(Camera.current == null))
		{
			Quaternion quaternion = Quaternion.LookRotation(Camera.current.transform.forward, Camera.current.transform.up);
			Quaternion quaternion2 = Quaternion.Inverse(base.transform.rotation);
			for (int i = 0; i < rvertices.Length; i++)
			{
				rvertices[i] = quaternion * vertices[i];
				rvertices[i] = quaternion2 * rvertices[i];
			}
			mesh.vertices = rvertices;
		}
	}
}
