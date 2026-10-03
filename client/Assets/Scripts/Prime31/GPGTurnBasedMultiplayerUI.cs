using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Prime31
{
	public class GPGTurnBasedMultiplayerUI : MonoBehaviourGUI
	{
		internal enum TurnBasedDemoState
		{
			NotAuthenticated = 0,
			AuthenticatedWithNoMatchLoaded = 1,
			AuthenticatedWithActiveMatch = 2,
			AuthenticatedWithInactiveMatch = 3
		}

		private TurnBasedDemoState _demoState;

		private GPGTurnBasedMatch _currentMatch;

		private GPGTurnBasedMatch currentMatch
		{
			get
			{
				return _currentMatch;
			}
			set
			{
				_currentMatch = value;
				if (_currentMatch == null)
				{
					_demoState = TurnBasedDemoState.AuthenticatedWithNoMatchLoaded;
				}
				else if (_currentMatch.status == GPGTurnBasedMatchStatus.Active)
				{
					_demoState = TurnBasedDemoState.AuthenticatedWithActiveMatch;
				}
				else
				{
					_demoState = TurnBasedDemoState.AuthenticatedWithInactiveMatch;
				}
			}
		}

		private void Start()
		{
			PlayGameServices.enableDebugLog(true);
		}

		private void OnGUI()
		{
			GUI.skin.label.alignment = TextAnchor.MiddleCenter;
			beginColumn();
			switch (_demoState)
			{
			case TurnBasedDemoState.NotAuthenticated:
				notauthenticatedGUI();
				break;
			case TurnBasedDemoState.AuthenticatedWithNoMatchLoaded:
				authenticatedWithNoMatchLoadedGUI();
				break;
			case TurnBasedDemoState.AuthenticatedWithActiveMatch:
				authenticatedWithActiveMatchGUI();
				break;
			case TurnBasedDemoState.AuthenticatedWithInactiveMatch:
				authenticatedWithInactiveMatchGUI();
				break;
			}
			endColumn();
		}

		private void notauthenticatedGUI()
		{
			if (GUILayout.Button("Authenticate"))
			{
				PlayGameServices.authenticate();
			}
		}

		private void authenticatedWithNoMatchLoadedGUI()
		{
			if (GUILayout.Button("Check for Invites and Matches after Launch"))
			{
				GPGTurnBasedMultiplayer.checkForInvitesAndMatches();
			}
			GUILayout.Label("Match Creation and Management");
			if (GUILayout.Button("Show Match Inbox"))
			{
				GPGTurnBasedMultiplayer.showInbox();
			}
			if (GUILayout.Button("Show Player Selector"))
			{
				GPGTurnBasedMultiplayer.showPlayerSelector(1, 2);
			}
			if (GUILayout.Button("Create Match Programmatically"))
			{
				GPGTurnBasedMultiplayer.createMatchProgrammatically(1, 1);
			}
			if (GUILayout.Button("Load All Matches"))
			{
				GPGTurnBasedMultiplayer.loadAllMatches();
			}
		}

		private void authenticatedWithActiveMatchGUI()
		{
			GUILayout.Label(string.Concat("Match Status: ", currentMatch.status, ", Description: ", currentMatch.matchDescription));
			if (currentMatch.isLocalPlayersTurn)
			{
				GUILayout.Label("It is Our Turn");
				if (GUILayout.Button("Take Turn"))
				{
					GPGTurnBasedMultiplayer.takeTurn(currentMatch.matchId, getMatchDataWithNewDataAppended(), getPendingParticipantId());
					currentMatch = null;
				}
				if (GUILayout.Button("Finish Match Without Data"))
				{
					GPGTurnBasedMultiplayer.finishMatchWithoutData(currentMatch.matchId);
					currentMatch = null;
				}
				if (GUILayout.Button("Finish Match With Data"))
				{
					List<GPGTurnBasedParticipantResult> list = new List<GPGTurnBasedParticipantResult>();
					foreach (GPGTurnBasedParticipant player in currentMatch.players)
					{
						list.Add(new GPGTurnBasedParticipantResult(player.participantId, GPGTurnBasedParticipantResultStatus.Tie));
					}
					GPGTurnBasedMultiplayer.finishMatchWithData(currentMatch.matchId, getMatchDataWithNewDataAppended(), list);
					currentMatch = null;
				}
				if (GUILayout.Button("Leave Match During Turn"))
				{
					GPGTurnBasedMultiplayer.leaveDuringTurn(currentMatch.matchId, getPendingParticipantId());
					currentMatch = null;
				}
				dismissMatchGuiButton();
			}
			else
			{
				GUILayout.Label("It is Not Our Turn");
				if (GUILayout.Button("Leave Match Out of Turn"))
				{
					GPGTurnBasedMultiplayer.leaveOutOfTurn(currentMatch.matchId);
					currentMatch = null;
				}
				dismissMatchGuiButton();
			}
		}

		private void authenticatedWithInactiveMatchGUI()
		{
			GUILayout.Label(string.Concat("Match Status: ", currentMatch.status, ", Description: ", currentMatch.matchDescription));
			if (currentMatch.isLocalPlayersTurn)
			{
				if (GUILayout.Button("Finish Match Without Data"))
				{
					GPGTurnBasedMultiplayer.finishMatchWithoutData(currentMatch.matchId);
					currentMatch = null;
				}
			}
			else if (currentMatch.canRematch && GUILayout.Button("Rematch"))
			{
				GPGTurnBasedMultiplayer.rematch(currentMatch.matchId);
				currentMatch = null;
			}
			dismissMatchGuiButton();
		}

		private string getPendingParticipantId()
		{
			string text = null;
			if (currentMatch.availableAutoMatchSlots == 0)
			{
				GPGTurnBasedParticipant gPGTurnBasedParticipant = currentMatch.players.Where((GPGTurnBasedParticipant p) => p.participantId != currentMatch.localParticipantId).FirstOrDefault();
				Debug.Log("no auto match slots left so looking for a participant that is not us out of a total player count of " + currentMatch.players.Count);
				if (gPGTurnBasedParticipant != null)
				{
					text = gPGTurnBasedParticipant.participantId;
				}
			}
			Debug.Log("using pendingParticipantId: " + text);
			return text;
		}

		private byte[] getMatchDataWithNewDataAppended()
		{
			string text = string.Empty;
			if (currentMatch.hasDataAvailable)
			{
				text = Encoding.UTF8.GetString(currentMatch.matchData);
			}
			text += Utils.randomString(1);
			Debug.Log("using match data: " + text);
			return Encoding.UTF8.GetBytes(text);
		}

		private void dismissMatchGuiButton()
		{
			if (GUILayout.Button("Dismiss Match"))
			{
				GPGTurnBasedMultiplayer.dismissMatch(currentMatch.matchId);
				currentMatch = null;
			}
			if (GUILayout.Button("Clear Current Local Match"))
			{
				currentMatch = null;
			}
		}

		private void OnEnable()
		{
			GPGManager.authenticationSucceededEvent += authenticationSucceededEvent;
			GPGTurnBasedManager.matchChangedEvent += matchChanged;
			GPGTurnBasedManager.matchEndedEvent += matchChanged;
		}

		private void OnDisable()
		{
			GPGManager.authenticationSucceededEvent -= authenticationSucceededEvent;
			GPGTurnBasedManager.matchChangedEvent -= matchChanged;
			GPGTurnBasedManager.matchEndedEvent -= matchChanged;
		}

		private void authenticationSucceededEvent(string playerId)
		{
			currentMatch = null;
		}

		private void matchChanged(GPGTurnBasedMatch match)
		{
			currentMatch = match;
		}
	}
}
