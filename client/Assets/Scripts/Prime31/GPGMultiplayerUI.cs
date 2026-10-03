using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Prime31
{
	public class GPGMultiplayerUI : MonoBehaviourGUI
	{
		private bool _isGameInProgress;

		private string _lastReceivedMessage = string.Empty;

		private void Start()
		{
			PlayGameServices.enableDebugLog(true);
		}

		private void OnGUI()
		{
			GUI.skin.label.alignment = TextAnchor.MiddleCenter;
			beginColumn();
			if (!_isGameInProgress)
			{
				if (GUILayout.Button("Authenticate"))
				{
					PlayGameServices.authenticate();
				}
				GUILayout.Label("Room Creation");
				if (GUILayout.Button("Show Invitation Inbox"))
				{
					GPGMultiplayer.showInvitationInbox();
				}
				if (GUILayout.Button("Start Quick Match"))
				{
					GPGMultiplayer.startQuickMatch(1, 1);
				}
				if (GUILayout.Button("Create Room Programmatically"))
				{
					GPGMultiplayer.createRoomProgrammatically(1, 1);
				}
				if (GUILayout.Button("Show Player Selector"))
				{
					GPGMultiplayer.showPlayerSelector(1, 1);
				}
			}
			else
			{
				GUILayout.Label("In Real-time Match");
				if (GUILayout.Button("Send Unreliable Message to All"))
				{
					byte[] bytes = Encoding.UTF8.GetBytes("howdy. current time: " + DateTime.Now);
					GPGMultiplayer.sendUnreliableRealtimeMessageToAll(bytes);
				}
				if (GUILayout.Button("Get Room Participants"))
				{
					List<GPGMultiplayerParticipant> participants = GPGMultiplayer.getParticipants();
					Utils.logObject(participants);
				}
				if (GUILayout.Button("Leave Room"))
				{
					GPGMultiplayer.leaveRoom();
				}
				GUILayout.Space(40f);
				GUILayout.Label(_lastReceivedMessage);
			}
			endColumn(false);
			if (bottomLeftButton("Back to Standard Demo Scene"))
			{
				MonoBehaviourGUI.loadLevel("PlayGameServicesDemoScene");
			}
		}

		private void OnEnable()
		{
			GPGMultiplayerManager.onRoomConnectedEvent += onRoomConnectedEvent;
			GPGMultiplayerManager.onRealTimeMessageReceivedEvent += onRealTimeMessageReceivedEvent;
			GPGMultiplayerManager.onDisconnectedFromRoomEvent += onDisconnectedOrLeftRoom;
			GPGMultiplayerManager.onLeftRoomEvent += onDisconnectedOrLeftRoom;
		}

		private void OnDisable()
		{
			GPGMultiplayerManager.onRoomConnectedEvent -= onRoomConnectedEvent;
			GPGMultiplayerManager.onRealTimeMessageReceivedEvent -= onRealTimeMessageReceivedEvent;
			GPGMultiplayerManager.onDisconnectedFromRoomEvent -= onDisconnectedOrLeftRoom;
			GPGMultiplayerManager.onLeftRoomEvent -= onDisconnectedOrLeftRoom;
		}

		private void onRoomConnectedEvent(GPGRoom room, GPGRoomUpdateStatus status)
		{
			_isGameInProgress = true;
		}

		private void onWaitingRoomCompletedEvent(bool didSucceed)
		{
			_isGameInProgress = didSucceed;
		}

		private void onDisconnectedOrLeftRoom()
		{
			_isGameInProgress = false;
		}

		private void onRealTimeMessageReceivedEvent(string senderParticipantId, byte[] message)
		{
			string text = Encoding.UTF8.GetString(message);
			_lastReceivedMessage = string.Format("Last Message: " + text);
		}
	}
}
