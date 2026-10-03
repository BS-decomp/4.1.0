using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeshEditor : MonoBehaviour
{
	public Mesh originalMesh;

	private Mesh editMesh;

	private MeshFilter mMeshFilter;

	public Vector3 mScale = Vector3.one;

	public Vector3 mPivot = Vector3.zero;

	public bool mMirrorX;

	public bool mMirrorY;

	public bool mMirrorZ;

	public bool mFlip;

	private GameObject mGameObject;

	private Transform mTransform;

	public GameObject cachedGameObject
	{
		get
		{
			if (mGameObject == null)
			{
				mGameObject = base.gameObject;
			}
			return mGameObject;
		}
	}

	public Transform cachedTransform
	{
		get
		{
			if (mTransform == null)
			{
				mTransform = base.transform;
			}
			return mTransform;
		}
	}

	public Vector3 scale
	{
		get
		{
			return mScale;
		}
		set
		{
			if (mScale != value)
			{
				mScale = value;
				UpdateVertices();
				MonoBehaviour.print(value);
			}
		}
	}

	public Vector3 pivot
	{
		get
		{
			return mPivot;
		}
		set
		{
			if (mPivot != value)
			{
				mPivot = value;
				UpdateVertices();
			}
		}
	}

	public bool mirrorX
	{
		get
		{
			return mMirrorX;
		}
		set
		{
			if (mMirrorX != value)
			{
				mMirrorX = value;
				UpdateVertices();
			}
		}
	}

	public bool mirrorY
	{
		get
		{
			return mMirrorY;
		}
		set
		{
			if (mMirrorY != value)
			{
				mMirrorY = value;
				UpdateVertices();
			}
		}
	}

	public bool mirrorZ
	{
		get
		{
			return mMirrorZ;
		}
		set
		{
			if (mMirrorZ != value)
			{
				mMirrorZ = value;
				UpdateVertices();
			}
		}
	}

	public bool flip
	{
		get
		{
			return mFlip;
		}
		set
		{
			if (mFlip != value)
			{
				mFlip = value;
				UpdateFlip();
			}
		}
	}

	public MeshFilter meshFilter
	{
		get
		{
			if (mMeshFilter == null)
			{
				mMeshFilter = GetComponent<MeshFilter>();
			}
			return mMeshFilter;
		}
	}

	private void OnEnable()
	{
		UpdateMesh();
	}

	private void Reset()
	{
		DisableMesh();
		if (Application.isEditor)
		{
			UpdateMesh();
		}
	}

	public void UpdateMesh()
	{
		if (originalMesh == null)
		{
			originalMesh = meshFilter.mesh;
		}
		if (base.enabled)
		{
			EnableMesh();
		}
		else
		{
			DisableMesh();
		}
	}

	public void EnableMesh()
	{
		editMesh = new Mesh();
		editMesh.name = originalMesh.name + "_Atlas";
		UpdateVertices();
		UpdateFlip();
		editMesh.uv = originalMesh.uv;
		editMesh.uv1 = originalMesh.uv1;
		editMesh.normals = originalMesh.normals;
		editMesh.tangents = originalMesh.tangents;
		meshFilter.mesh = editMesh;
	}

	public void DisableMesh()
	{
		if (editMesh != null)
		{
			Object.DestroyImmediate(editMesh);
			editMesh = null;
		}
		if (originalMesh != null)
		{
			meshFilter.mesh = originalMesh;
		}
	}

	public void UpdateSettings()
	{
		UpdateVertices();
		UpdateFlip();
	}

	private void UpdateFlip()
	{
		if (!(editMesh == null))
		{
			if (flip)
			{
				editMesh.triangles = originalMesh.triangles.Reverse().ToArray();
			}
			else
			{
				editMesh.triangles = originalMesh.triangles;
			}
		}
	}

	private void UpdateVertices()
	{
		if (!(editMesh == null))
		{
			Vector3[] vertices = originalMesh.vertices;
			for (int i = 0; i < vertices.Length; i++)
			{
				vertices[i].x = (vertices[i].x + mPivot.x) * mScale.x * (float)((!mMirrorX) ? 1 : (-1));
				vertices[i].y = (vertices[i].y + mPivot.y) * mScale.y * (float)((!mMirrorY) ? 1 : (-1));
				vertices[i].z = (vertices[i].z + mPivot.z) * mScale.z * (float)((!mMirrorZ) ? 1 : (-1));
			}
			editMesh.vertices = vertices;
		}
	}
}
