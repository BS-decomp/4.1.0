using System.Collections.Generic;
using UnityEngine;

namespace Prime31
{
	public class GPGTBMultiplayerEventListener : MonoBehaviour
	{
		private void OnEnable()
		{
			GPGTurnBasedManager.onInvitationReceivedEvent += onInvitationReceivedEvent;
			GPGTurnBasedManager.onInvitationRemovedEvent += onInvitationRemovedEvent;
			GPGTurnBasedManager.matchChangedEvent += matchChangedEvent;
			GPGTurnBasedManager.matchFailedEvent += matchFailedEvent;
			GPGTurnBasedManager.matchEndedEvent += matchEndedEvent;
			GPGTurnBasedManager.playerSelectorCanceledEvent += playerSelectorCanceledEvent;
			GPGTurnBasedManager.loadMatchesCompletedEvent += loadMatchesCompletedEvent;
			GPGTurnBasedManager.takeTurnCompletedEvent += takeTurnCompletedEvent;
			GPGTurnBasedManager.finishMatchCompletedEvent += finishMatchCompletedEvent;
			GPGTurnBasedManager.dismissMatchCompletedEvent += dismissMatchCompletedEvent;
			GPGTurnBasedManager.leaveDuringTurnCompletedEvent += leaveDuringTurnCompletedEvent;
			GPGTurnBasedManager.leaveOutOfTurnCompletedEvent += leaveOutOfTurnCompletedEvent;
			GPGTurnBasedManager.invitationReceivedEvent += invitationReceivedEvent;
		}

		private void OnDisable()
		{
			GPGTurnBasedManager.onInvitationReceivedEvent -= onInvitationReceivedEvent;
			GPGTurnBasedManager.onInvitationRemovedEvent -= onInvitationRemovedEvent;
			GPGTurnBasedManager.matchChangedEvent -= matchChangedEvent;
			GPGTurnBasedManager.matchFailedEvent -= matchFailedEvent;
			GPGTurnBasedManager.matchEndedEvent -= matchEndedEvent;
			GPGTurnBasedManager.playerSelectorCanceledEvent -= playerSelectorCanceledEvent;
			GPGTurnBasedManager.loadMatchesCompletedEvent -= loadMatchesCompletedEvent;
			GPGTurnBasedManager.takeTurnCompletedEvent -= takeTurnCompletedEvent;
			GPGTurnBasedManager.finishMatchCompletedEvent -= finishMatchCompletedEvent;
			GPGTurnBasedManager.dismissMatchCompletedEvent -= dismissMatchCompletedEvent;
			GPGTurnBasedManager.leaveDuringTurnCompletedEvent -= leaveDuringTurnCompletedEvent;
			GPGTurnBasedManager.leaveOutOfTurnCompletedEvent -= leaveOutOfTurnCompletedEvent;
			GPGTurnBasedManager.invitationReceivedEvent -= invitationReceivedEvent;
		}

		private void onInvitationReceivedEvent(string invitationId)
		{
			Debug.Log("onInvitationReceivedEvent: " + invitationId);
		}

		private void onInvitationRemovedEvent(string invitationId)
		{
			Debug.Log("onInvitationRemovedEvent: " + invitationId);
		}

		private void matchChangedEvent(GPGTurnBasedMatch match)
		{
			Debug.Log("matchChangedEvent");
			Debug.Log(match);
		}

		private void matchFailedEvent(string error)
		{
			Debug.Log("matchFailedEvent: " + error);
		}

		private void matchEndedEvent(GPGTurnBasedMatch match)
		{
			Debug.Log("matchEndedEvent");
			Debug.Log(match);
		}

		private void playerSelectorCanceledEvent()
		{
			Debug.Log("playerSelectorCanceledEvent");
		}

		private void loadMatchesCompletedEvent(bool didSucceed, string error, List<GPGTurnBasedMatch> matches)
		{
			if (didSucceed)
			{
				Debug.Log("loadMatchesCompletedEvent");
				Utils.logObject(matches);
				return;
			}
			Debug.Log("loadMatchesCompletedEvent. didSucceed: " + didSucceed + ", error: " + error);
		}

		private void takeTurnCompletedEvent(bool didSucceed, string error)
		{
			Debug.Log("takeTurnCompletedEvent. didSucceed: " + didSucceed + ", error: " + error);
		}

		private void finishMatchCompletedEvent(bool didSucceed, string error)
		{
			Debug.Log("finishMatchCompletedEvent. didSucceed: " + didSucceed + ", error: " + error);
		}

		private void dismissMatchCompletedEvent(bool didSucceed, string error)
		{
			Debug.Log("dismissMatchCompletedEvent. didSucceed: " + didSucceed + ", error: " + error);
		}

		private void leaveDuringTurnCompletedEvent(bool didSucceed, string error)
		{
			Debug.Log("leaveDuringTurnCompletedEvent. didSucceed: " + didSucceed + ", error: " + error);
		}

		private void leaveOutOfTurnCompletedEvent(bool didSucceed, string error)
		{
			Debug.Log("leaveOutOfTurnCompletedEvent. didSucceed: " + didSucceed + ", error: " + error);
		}

		private void invitationReceivedEvent(GPGTurnBasedInvitation invitation)
		{
			Debug.Log("invitationReceivedEvent");
			Debug.Log(invitation);
		}
	}
}
