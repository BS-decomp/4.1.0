using System.Collections.Generic;
using UnityEngine;

public class PickColorManager : MonoBehaviour
{
	public GameObject[] Targets;

	public Color SelectColor = Color.white;

	private Material DefaultMaterial;

	private Material CustomMaterial;

	private Vector2 LastSetColor;

	private List<Vector2> IgnoreCoord = new List<Vector2>();

	public List<Texture2D> SnapshotTexture = new List<Texture2D>();

	private void Start()
	{
	}

	public void SetTarget(GameObject[] go, Material mat)
	{
		if (Targets.Length != 0)
		{
			for (int i = 0; i < Targets.Length; i++)
			{
				Targets[i].GetComponent<Renderer>().material = DefaultMaterial;
			}
		}
		Targets = go;
		DefaultMaterial = mat;
		CustomMaterial = new Material(DefaultMaterial);
		ClearSnapshot();
		CustomMaterial.mainTexture = Object.Instantiate(DefaultMaterial.mainTexture) as Texture2D;
		Texture2D texture2D = CustomMaterial.mainTexture as Texture2D;
		for (int j = 0; j < texture2D.height; j++)
		{
			for (int k = 0; k < texture2D.width; k++)
			{
				if (texture2D.GetPixel(j, k) == Color.black)
				{
					IgnoreCoord.Add(new Vector2(j, k));
				}
			}
		}
		CustomMaterial.name += "_Custom";
		for (int l = 0; l < Targets.Length; l++)
		{
			Targets[l].GetComponent<Renderer>().material = CustomMaterial;
		}
		RedoTexture();
	}

	private void Update()
	{
		if (EditWeaponManager.GetSelectEdit() == EditWeaponManager.EditList.Paint)
		{
			UpdateCoord();
			UpdateSnapshot();
		}
	}

	private void UpdateCoord()
	{
		if (Input.GetMouseButton(0))
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hitInfo;
			if (Physics.Raycast(ray, out hitInfo, 100f) && hitInfo.textureCoord != LastSetColor)
			{
				LastSetColor = hitInfo.textureCoord;
				SetColor(hitInfo.textureCoord);
			}
		}
	}

	private void UpdateSnapshot()
	{
		if (Input.GetMouseButtonUp(0))
		{
			RedoTexture();
		}
		if (Input.GetKeyDown(KeyCode.Z))
		{
			UndoTexture();
		}
	}

	private void SetColor(Vector2 point)
	{
		Texture2D texture2D = CustomMaterial.mainTexture as Texture2D;
		point.x *= texture2D.height;
		point.y *= texture2D.width;
		if (!IgnoreCoord.Contains(new Vector2((int)point.x, (int)point.y)))
		{
			texture2D.SetPixel((int)point.x, (int)point.y, SelectColor);
			texture2D.Apply();
		}
	}

	private void ClearSnapshot()
	{
		for (int i = 0; i < SnapshotTexture.Count; i++)
		{
			Object.Destroy(SnapshotTexture[i]);
		}
		SnapshotTexture.Clear();
	}

	private void RedoTexture()
	{
		if (!Application.isEditor && SnapshotTexture.Count >= 10)
		{
			SnapshotTexture.RemoveAt(0);
		}
		SnapshotTexture.Add((Texture2D)Object.Instantiate(CustomMaterial.mainTexture));
		SnapshotTexture[SnapshotTexture.Count - 1].name = SnapshotTexture.Count.ToString();
	}

	private void UndoTexture()
	{
		if (SnapshotTexture.Count > 1)
		{
			Texture2D obj = SnapshotTexture[SnapshotTexture.Count - 1];
			SnapshotTexture.RemoveAt(SnapshotTexture.Count - 1);
			if (SnapshotTexture.Count == 1)
			{
				CustomMaterial.mainTexture = (Texture2D)Object.Instantiate(SnapshotTexture[0]);
			}
			else
			{
				CustomMaterial.mainTexture = SnapshotTexture[SnapshotTexture.Count - 1];
			}
			Object.Destroy(obj);
		}
	}
}
