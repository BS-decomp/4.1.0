using Lean;
using UnityEngine;

public class SimpleSnapshots : MonoBehaviour
{
	public LineRenderer[] LineRenderers;

	protected virtual void LateUpdate()
	{
		if (LineRenderers == null)
		{
			return;
		}
		int i;
		for (i = 0; i < LineRenderers.Length; i++)
		{
			LineRenderer lineRenderer = LineRenderers[i];
			if (!(lineRenderer != null))
			{
				continue;
			}
			LeanFinger leanFinger = LeanTouch.Fingers.Find((LeanFinger f) => f.Index == i);
			if (leanFinger != null)
			{
				lineRenderer.SetVertexCount(leanFinger.Snapshots.Count);
				for (int num = 0; num < leanFinger.Snapshots.Count; num++)
				{
					LeanFinger.Snapshot snapshot = leanFinger.Snapshots[num];
					if (snapshot != null)
					{
						lineRenderer.SetPosition(num, snapshot.GetWorldPosition(1f));
					}
				}
			}
			else
			{
				lineRenderer.SetVertexCount(0);
			}
		}
	}
}
