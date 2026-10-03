using UnityEngine;

namespace Prime31
{
	public class GPGSMultiplayerEventListener : MonoBehaviour
	{
		private void OnEnable()
		{
			GPGMultiplayerManager.onInvitationReceivedEvent += onInvitationReceivedEvent;
			GPGMultiplayerManager.onInvitationRemovedEvent += onInvitationRemovedEvent;
			GPGMultiplayerManager.onWaitingRoomCompletedEvent += onWaitingRoomCompletedEvent;
			GPGMultiplayerManager.onInvitationInboxCompletedEvent += onInvitationInboxCompletedEvent;
			GPGMultiplayerManager.onInvitePlayersCompletedEvent += onInvitePlayersCompletedEvent;
			GPGMultiplayerManager.onJoinedRoomEvent += onJoinedRoomEvent;
			GPGMultiplayerManager.onLeftRoomEvent += onLeftRoomEvent;
			GPGMultiplayerManager.onRoomConnectedEvent += onRoomConnectedEvent;
			GPGMultiplayerManager.onRoomCreatedEvent += onRoomCreatedEvent;
			GPGMultiplayerManager.onRealTimeMessageReceivedEvent += onRealTimeMessageReceivedEvent;
			GPGMultiplayerManager.onConnectedToRoomEvent += onConnectedToRoomEvent;
			GPGMultiplayerManager.onDisconnectedFromRoomEvent += onDisconnectedFromRoomEvent;
			GPGMultiplayerManager.onP2PConnectedEvent += onP2PConnectedEvent;
			GPGMultiplayerManager.onP2PDisconnectedEvent += onP2PDisconnectedEvent;
			GPGMultiplayerManager.onPeerDeclinedEvent += onPeerDeclinedEvent;
			GPGMultiplayerManager.onPeerInvitedToRoomEvent += onPeerInvitedToRoomEvent;
			GPGMultiplayerManager.onPeerJoinedEvent += onPeerJoinedEvent;
			GPGMultiplayerManager.onPeerLeftEvent += onPeerLeftEvent;
			GPGMultiplayerManager.onPeerConnectedEvent += onPeerConnectedEvent;
			GPGMultiplayerManager.onPeerDisconnectedEvent += onPeerDisconnectedEvent;
			GPGMultiplayerManager.onRoomAutoMatchingEvent += onRoomAutoMatchingEvent;
			GPGMultiplayerManager.onRoomConnectingEvent += onRoomConnectingEvent;
		}

		private void OnDisable()
		{
			GPGMultiplayerManager.onInvitationReceivedEvent -= onInvitationReceivedEvent;
			GPGMultiplayerManager.onInvitationRemovedEvent -= onInvitationRemovedEvent;
			GPGMultiplayerManager.onWaitingRoomCompletedEvent -= onWaitingRoomCompletedEvent;
			GPGMultiplayerManager.onInvitationInboxCompletedEvent -= onInvitationInboxCompletedEvent;
			GPGMultiplayerManager.onInvitePlayersCompletedEvent -= onInvitePlayersCompletedEvent;
			GPGMultiplayerManager.onJoinedRoomEvent -= onJoinedRoomEvent;
			GPGMultiplayerManager.onLeftRoomEvent -= onLeftRoomEvent;
			GPGMultiplayerManager.onRoomConnectedEvent -= onRoomConnectedEvent;
			GPGMultiplayerManager.onRoomCreatedEvent -= onRoomCreatedEvent;
			GPGMultiplayerManager.onRealTimeMessageReceivedEvent -= onRealTimeMessageReceivedEvent;
			GPGMultiplayerManager.onConnectedToRoomEvent -= onConnectedToRoomEvent;
			GPGMultiplayerManager.onDisconnectedFromRoomEvent -= onDisconnectedFromRoomEvent;
			GPGMultiplayerManager.onP2PConnectedEvent -= onP2PConnectedEvent;
			GPGMultiplayerManager.onP2PDisconnectedEvent -= onP2PDisconnectedEvent;
			GPGMultiplayerManager.onPeerDeclinedEvent -= onPeerDeclinedEvent;
			GPGMultiplayerManager.onPeerInvitedToRoomEvent -= onPeerInvitedToRoomEvent;
			GPGMultiplayerManager.onPeerJoinedEvent -= onPeerJoinedEvent;
			GPGMultiplayerManager.onPeerLeftEvent -= onPeerLeftEvent;
			GPGMultiplayerManager.onPeerConnectedEvent -= onPeerConnectedEvent;
			GPGMultiplayerManager.onPeerDisconnectedEvent -= onPeerDisconnectedEvent;
			GPGMultiplayerManager.onRoomAutoMatchingEvent -= onRoomAutoMatchingEvent;
			GPGMultiplayerManager.onRoomConnectingEvent -= onRoomConnectingEvent;
		}

		private void onInvitationReceivedEvent(string invitationId)
		{
			Debug.Log("onInvitationReceivedEvent: " + invitationId);
		}

		private void onInvitationRemovedEvent(string invitationId)
		{
			Debug.Log("onInvitationRemovedEvent: " + invitationId);
		}

		private void onWaitingRoomCompletedEvent(bool didSucceed)
		{
			Debug.Log("onWaitingRoomCompletedEvent. didSucceed: " + didSucceed);
		}

		private void onInvitationInboxCompletedEvent(bool didSucceed)
		{
			Debug.Log("onInvitationInboxCompletedEvent. didSucceed: " + didSucceed);
		}

		private void onInvitePlayersCompletedEvent(bool didSucceed)
		{
			Debug.Log("onInvitePlayersCompletedEvent. didSucceed: " + didSucceed);
		}

		private void onJoinedRoomEvent(GPGRoom room, GPGRoomUpdateStatus statusCode)
		{
			Debug.Log(string.Concat("onJoinedRoomEvent. room: ", room, ", statusCode: ", statusCode));
		}

		private void onLeftRoomEvent()
		{
			Debug.Log("onLeftRoomEvent");
		}

		private void onRoomConnectedEvent(GPGRoom room, GPGRoomUpdateStatus statusCode)
		{
			Debug.Log(string.Concat("onRoomConnectedEvent. room: ", room, ", statusCode: ", statusCode));
		}

		private void onRoomCreatedEvent(GPGRoom room, GPGRoomUpdateStatus statusCode)
		{
			Debug.Log(string.Concat("onRoomCreatedEvent. room: ", room, ", statusCode: ", statusCode));
		}

		private void onRealTimeMessageReceivedEvent(string senderParticipantId, byte[] bytes)
		{
			Debug.Log("onRealTimeMessageReceivedEvent. senderParticipantId: " + senderParticipantId + ", message length: " + bytes.Length);
		}

		private void onConnectedToRoomEvent()
		{
			Debug.Log("onConnectedToRoomEvent");
		}

		private void onDisconnectedFromRoomEvent()
		{
			Debug.Log("onDisconnectedFromRoomEvent");
		}

		private void onP2PConnectedEvent(string participantId)
		{
			Debug.Log("onP2PConnectedEvent: " + participantId);
		}

		private void onP2PDisconnectedEvent(string participantId)
		{
			Debug.Log("onP2PDisconnectedEvent: " + participantId);
		}

		private void onPeerDeclinedEvent(string participantId)
		{
			Debug.Log("onPeerDeclinedEvent: " + participantId);
		}

		private void onPeerInvitedToRoomEvent(string participantId)
		{
			Debug.Log("onPeerInvitedToRoomEvent: " + participantId);
		}

		private void onPeerJoinedEvent(string participantId)
		{
			Debug.Log("onPeerJoinedEvent: " + participantId);
		}

		private void onPeerLeftEvent(string participantId)
		{
			Debug.Log("onPeerLeftEvent: " + participantId);
		}

		private void onPeerConnectedEvent(string participantId)
		{
			Debug.Log("onPeerConnectedEvent: " + participantId);
		}

		private void onPeerDisconnectedEvent(string participantId)
		{
			Debug.Log("onPeerDisconnectedEvent: " + participantId);
		}

		private void onRoomAutoMatchingEvent(GPGRoom room)
		{
			Debug.Log("onRoomAutoMatchingEvent: " + room);
		}

		private void onRoomConnectingEvent(GPGRoom room)
		{
			Debug.Log("onRoomConnectingEvent: " + room);
		}
	}
}
