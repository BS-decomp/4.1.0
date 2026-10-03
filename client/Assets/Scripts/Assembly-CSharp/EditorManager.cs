using System;
using System.Collections.Generic;
using System.IO;
using Lean;
using UnityEngine;

public class EditorManager : MonoBehaviour
{
	public enum EditList
	{
		Paint = 0,
		Move = 1,
		Look = 2
	}

	[Serializable]
	public class PaintElement
	{
		public GameObject[] Targets;

		public Color SelectColor = Color.white;

		private Material DefaultMaterial;

		private Material CustomMaterial;

		private Vector2 LastSetColor;

		public UISprite ColorSprite;

		private List<Vector2> IgnoreCoord = new List<Vector2>();

		public List<Texture2D> SnapshotTexture = new List<Texture2D>();

		private bool isPaint;

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
			CustomMaterial.mainTexture = UnityEngine.Object.Instantiate(DefaultMaterial.mainTexture) as Texture2D;
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

		public void UpdateCoord()
		{
			if (!Input.GetMouseButton(0) || !(UICamera.currentCamera.tag == "MainCamera"))
			{
				return;
			}
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hitInfo;
			if (Physics.Raycast(ray, out hitInfo, 100f))
			{
				isPaint = true;
				if (hitInfo.textureCoord != LastSetColor)
				{
					LastSetColor = hitInfo.textureCoord;
					SetColor(hitInfo.textureCoord);
				}
			}
		}

		public void UpdateSnapshot()
		{
			if (Input.GetMouseButtonUp(0))
			{
				if (isPaint)
				{
					RedoTexture();
				}
				isPaint = false;
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
				UnityEngine.Object.Destroy(SnapshotTexture[i]);
			}
			SnapshotTexture.Clear();
		}

		private void RedoTexture()
		{
			if (!Application.isEditor && SnapshotTexture.Count >= 10)
			{
				SnapshotTexture.RemoveAt(0);
			}
			SnapshotTexture.Add((Texture2D)UnityEngine.Object.Instantiate(CustomMaterial.mainTexture));
			SnapshotTexture[SnapshotTexture.Count - 1].name = SnapshotTexture.Count.ToString();
		}

		public void UndoTexture()
		{
			if (SnapshotTexture.Count > 1)
			{
				Texture2D obj = SnapshotTexture[SnapshotTexture.Count - 1];
				SnapshotTexture.RemoveAt(SnapshotTexture.Count - 1);
				if (SnapshotTexture.Count == 1)
				{
					CustomMaterial.mainTexture = (Texture2D)UnityEngine.Object.Instantiate(SnapshotTexture[0]);
				}
				else
				{
					CustomMaterial.mainTexture = SnapshotTexture[SnapshotTexture.Count - 1];
				}
				UnityEngine.Object.Destroy(obj);
			}
		}

		public byte[] GetBytes()
		{
			Texture2D texture2D = CustomMaterial.mainTexture as Texture2D;
			return texture2D.EncodeToPNG();
		}

		public void SetCustomTexture(Texture2D texture)
		{
			ClearSnapshot();
			CustomMaterial.mainTexture = texture;
			RedoTexture();
		}

		public Texture2D GetCustomTexture()
		{
			return CustomMaterial.mainTexture as Texture2D;
		}
	}

	public EditList SelectEdit;

	public EditElement SelectElement;

	public PaintElement PaintSettings;

	public EditElement SelectElementTest;

	private static EditorManager instance;

	private void Awake()
	{
		instance = this;
		SetElement(SelectElementTest);
	}

	public static EditList GetSelectEdit()
	{
		return instance.SelectEdit;
	}

	private void Update()
	{
		if (SelectEdit == EditList.Paint)
		{
			PaintSettings.UpdateCoord();
			PaintSettings.UpdateSnapshot();
		}
		else if (SelectEdit == EditList.Move)
		{
			LeanTouch.MoveObject(SelectElement.RootMesh, LeanTouch.DragDelta);
			LeanTouch.ScaleObject(SelectElement.RootMesh, Vector3.one, SelectElement.MaxScale, LeanTouch.PinchScale);
		}
		else if (SelectEdit == EditList.Look)
		{
			SelectElement.RootMesh.localEulerAngles -= new Vector3(LeanTouch.DragDelta.y, LeanTouch.DragDelta.x, 0f);
			LeanTouch.ScaleObject(SelectElement.RootMesh, Vector3.one, SelectElement.MaxScale, LeanTouch.PinchScale);
		}
		if (Input.GetKeyDown(KeyCode.F))
		{
			SaveTexture();
		}
		if (Input.GetKeyDown(KeyCode.G))
		{
			LoadTexure();
		}
	}

	public void SetEdit(int edit)
	{
		SetEdit((EditList)edit);
	}

	public void SetEdit(EditList edit)
	{
		if (edit == EditList.Look || SelectEdit == EditList.Look)
		{
			SetDefaultPosition();
		}
		SelectEdit = edit;
	}

	public void SetDefaultPosition()
	{
		SetDefaultPosition(true);
	}

	public void SetDefaultPosition(bool rotation)
	{
		SelectElement.SetDefaultPosition(rotation);
	}

	public static void SetElement(EditElement element)
	{
		if (instance.SelectElement != null)
		{
			instance.SelectElement.RootMesh.gameObject.SetActive(false);
		}
		instance.SelectElement = element;
		instance.SelectElement.RootMesh.gameObject.SetActive(true);
		instance.PaintSettings.SetTarget(element.Meshes, element.DefaultMaterial);
		instance.SetDefaultPosition();
	}

	public void SetPaintColor(UISprite sprite)
	{
		SetPaintColor(sprite.color);
	}

	public void SetPaintColor(Color color)
	{
		PaintSettings.SelectColor = color;
		PaintSettings.ColorSprite.color = color;
	}

	public void UndoPaint()
	{
		PaintSettings.UndoTexture();
	}

	public void SaveTexture()
	{
		string text = SelectElement.ID + "$#";
		byte[] bytes = PaintSettings.GetBytes();
		text += Convert.ToBase64String(bytes);
		MonoBehaviour.print(text);
		File.WriteAllText(Application.dataPath + "/test.txt", text);
	}

	public void LoadTexure()
	{
		string text = File.ReadAllText(Application.dataPath + "/test.txt");
		string[] array = text.Split("\n"[0]);
		string[] array2 = array[1].Split("|"[0]);
		MonoBehaviour.print("Name: " + array[0]);
		byte[] array3 = new byte[array2.Length - 1];
		for (int i = 0; i < array3.Length; i++)
		{
			array3[i] = byte.Parse(array2[i]);
		}
		Texture2D texture2D = new Texture2D(2, 2);
		texture2D.filterMode = FilterMode.Point;
		texture2D.LoadImage(array3);
		PaintSettings.SetCustomTexture(texture2D);
	}
}
