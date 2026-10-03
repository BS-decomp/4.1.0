using DG.Tweening;
using UnityEngine;

public class TrapPath : MonoBehaviour
{
	public Transform Target;

	public Transform[] Points;

	public float Duration = 60f;

	public float Delay = 10f;

	public Color GizmosColor = Color.white;

	private int WaypointIndex;

	private Tween tween;

	private Vector3 StartPosition;

	private Quaternion StartRotation;

	private void Start()
	{
		StartPosition = Target.position;
		StartRotation = Target.rotation;
		EventManager.AddListener("StartRound", StartRound);
	}

	private void OnWaypointChange(int waypointIndex)
	{
		if (Points.Length != waypointIndex)
		{
			Target.DOLookAt(Points[waypointIndex].position, 0.3f);
		}
	}

	private void StartRound()
	{
		if (tween != null)
		{
			tween.Kill();
			Target.position = StartPosition;
			Target.rotation = StartRotation;
		}
		Vector3[] array = new Vector3[Points.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = Points[i].position;
		}
		tween = Target.DOPath(array, Duration).SetDelay(Delay).OnWaypointChange(OnWaypointChange);
	}
}
