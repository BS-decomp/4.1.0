using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon;
using UnityEngine;

public class UIKick : PunBehaviour
{
	private float lastKickTime;

	private List<int> kickPlayer = new List<int>();

	public static List<string> KickedServers = new List<string>();

	private void Start()
	{
		PhotonClassesManager.Add(this);
		PhotonEvent.AddListener(PhotonEventTag.KickPlayer, PhotonKickPlayer);
	}

	public override void OnPhotonPlayerPropertiesChanged(object[] playerAndUpdatedProps)
	{
		PhotonPlayer photonPlayer = (PhotonPlayer)playerAndUpdatedProps[0];
		Hashtable hashtable = (Hashtable)playerAndUpdatedProps[1];
		if (!hashtable.ContainsKey(PhotonCustomValue.kickKey))
		{
			return;
		}
		if (photonPlayer.IsLocal && (byte)hashtable[PhotonCustomValue.kickKey] == 0)
		{
			PhotonNetwork.LeaveRoom();
		}
		if (PhotonNetwork.isMasterClient)
		{
			int num = photonPlayer.GetKick() * 100;
			if (num / PhotonNetwork.room.PlayerCount >= 60)
			{
				KickPlayer(photonPlayer);
			}
		}
	}

	public void Kick()
	{
		if (lastKickTime > Time.time)
		{
			UIToast.Show(Utils.ToTimeString(lastKickTime - Time.time));
		}
		else if (!kickPlayer.Contains(UIPlayerStatistics.SelectPlayer.ID))
		{
			UIPlayerStatistics.SelectPlayer.SetKick1();
			kickPlayer.Add(UIPlayerStatistics.SelectPlayer.ID);
			lastKickTime = Time.time + 300f;
		}
	}

	private void KickPlayer(PhotonPlayer player)
	{
		if (PhotonNetwork.isMasterClient)
		{
			PhotonEvent.RPC(PhotonEventTag.KickPlayer, player);
			TimerManager.In(1f, () =>
			{
				PhotonNetwork.CloseConnection(player);
			});
		}
	}

	private void PhotonKickPlayer(PhotonEventData data)
	{
		KickedServers.Add(PhotonNetwork.room.Name);
		GameManager.SetLeaveRoomText(Localization.Get("You kicked from the server"));
	}
}
