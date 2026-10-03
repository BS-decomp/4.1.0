using System.Linq;
using UnityEngine;

namespace ProGrids2
{
	public static class pg_Util
	{
		private const float APPROX_ZERO = 0.0001f;

		public static Color ColorWithString(string value)
		{
			string valid = "01234567890.,";
			value = new string(value.Where((char c) => valid.Contains(c)).ToArray());
			string[] array = value.Split(',');
			if (array.Length < 4)
			{
				return new Color(1f, 0f, 1f, 1f);
			}
			return new Color(float.Parse(array[0]), float.Parse(array[1]), float.Parse(array[2]), float.Parse(array[3]));
		}

		public static float ValueFromMask(Vector3 val, Vector3 mask)
		{
			if (Mathf.Abs(mask.x) > 0.0001f)
			{
				return val.x;
			}
			if (Mathf.Abs(mask.y) > 0.0001f)
			{
				return val.y;
			}
			return val.z;
		}

		public static Vector3 SnapValue(Vector3 val, float snapValue)
		{
			float x = val.x;
			float y = val.y;
			float z = val.z;
			return new Vector3(Snap(x, snapValue), Snap(y, snapValue), Snap(z, snapValue));
		}

		public static Vector3 SnapValue(Vector3 val, Vector3 mask, float snapValue)
		{
			float x = val.x;
			float y = val.y;
			float z = val.z;
			return new Vector3((!(Mathf.Abs(mask.x) < 0.0001f)) ? Snap(x, snapValue) : x, (!(Mathf.Abs(mask.y) < 0.0001f)) ? Snap(y, snapValue) : y, (!(Mathf.Abs(mask.z) < 0.0001f)) ? Snap(z, snapValue) : z);
		}

		public static Vector3 SnapToCeil(Vector3 val, Vector3 mask, float snapValue)
		{
			float x = val.x;
			float y = val.y;
			float z = val.z;
			return new Vector3((!(Mathf.Abs(mask.x) < 0.0001f)) ? SnapToCeil(x, snapValue) : x, (!(Mathf.Abs(mask.y) < 0.0001f)) ? SnapToCeil(y, snapValue) : y, (!(Mathf.Abs(mask.z) < 0.0001f)) ? SnapToCeil(z, snapValue) : z);
		}

		public static Vector3 SnapToFloor(Vector3 val, float snapValue)
		{
			float x = val.x;
			float y = val.y;
			float z = val.z;
			return new Vector3(SnapToFloor(x, snapValue), SnapToFloor(y, snapValue), SnapToFloor(z, snapValue));
		}

		public static Vector3 SnapToFloor(Vector3 val, Vector3 mask, float snapValue)
		{
			float x = val.x;
			float y = val.y;
			float z = val.z;
			return new Vector3((!(Mathf.Abs(mask.x) < 0.0001f)) ? SnapToFloor(x, snapValue) : x, (!(Mathf.Abs(mask.y) < 0.0001f)) ? SnapToFloor(y, snapValue) : y, (!(Mathf.Abs(mask.z) < 0.0001f)) ? SnapToFloor(z, snapValue) : z);
		}

		public static float Snap(float val, float round)
		{
			return round * Mathf.Round(val / round);
		}

		public static float SnapToFloor(float val, float snapValue)
		{
			return snapValue * Mathf.Floor(val / snapValue);
		}

		public static float SnapToCeil(float val, float snapValue)
		{
			return snapValue * Mathf.Ceil(val / snapValue);
		}

		public static Vector3 CeilFloor(Vector3 v)
		{
			v.x = ((!(v.x < 0f)) ? 1 : (-1));
			v.y = ((!(v.y < 0f)) ? 1 : (-1));
			v.z = ((!(v.z < 0f)) ? 1 : (-1));
			return v;
		}
	}
}
