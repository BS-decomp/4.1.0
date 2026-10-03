using Photon;
using UnityEngine;

public class RigidbodyObject : PunBehaviour, IPunObservable
{
	public Rigidbody mRigidbody;

	public float Speed = 10f;

	private Transform mTransform;

	private Vector3 PhotonPosition;

	private Quaternion PhotonRotation;

	private PhotonPlayer LastContactPlayer;

	private void Awake()
	{
		PhotonClassesManager.Add(this);
		mRigidbody.isKinematic = !base.photonView.isMine;
		mTransform = base.transform;
	}

	private void Start()
	{
		TimerManager.In(0.1f, -1, 0.01f, UpdatePosition);
	}

	public void Force(Vector3 force)
	{
		base.photonView.RPC("PhotonForce", PhotonTargets.All, force, PhotonNetwork.player);
	}

	public void Force(Vector3 force, PhotonPlayer player)
	{
		base.photonView.RPC("PhotonForce", PhotonTargets.All, force, player);
	}

	[PunRPC]
	private void PhotonForce(Vector3 force, PhotonPlayer player, PhotonMessageInfo info)
	{
		if (info.timestamp + 0.6000000238418579 > PhotonNetwork.time)
		{
			LastContactPlayer = player;
			mRigidbody.AddForce(force);
		}
	}

	public override void OnPhotonPlayerConnected(PhotonPlayer playerConnect)
	{
		mRigidbody.isKinematic = !base.photonView.isMine;
	}

	public override void OnPhotonPlayerDisconnected(PhotonPlayer playerDisconnect)
	{
		mRigidbody.isKinematic = !base.photonView.isMine;
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.isWriting)
		{
			stream.SendNext(mTransform.position);
			stream.SendNext(mTransform.rotation);
		}
		else
		{
			PhotonPosition = (Vector3)stream.ReceiveNext();
			PhotonRotation = (Quaternion)stream.ReceiveNext();
		}
	}

	private void UpdatePosition()
	{
		if (base.photonView.isMine)
		{
			PhotonPosition = mTransform.position;
			PhotonRotation = mTransform.rotation;
		}
		else
		{
			mRigidbody.MovePosition(Vector3.Lerp(mTransform.position, PhotonPosition, Time.deltaTime * Speed));
			mRigidbody.MoveRotation(Quaternion.Lerp(mTransform.rotation, PhotonRotation, Time.deltaTime * Speed));
		}
	}

	public PhotonPlayer GetLastContactPlayer()
	{
		return LastContactPlayer;
	}
}
