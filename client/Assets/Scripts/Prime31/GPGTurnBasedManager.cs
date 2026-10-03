using System;
using System.Collections.Generic;

namespace Prime31
{
	public class GPGTurnBasedManager : AbstractManager
	{
		public static event Action<string> onInvitationReceivedEvent;

		public static event Action<string> onInvitationRemovedEvent;

		public static event Action<GPGTurnBasedMatch> matchChangedEvent;

		public static event Action<string> matchFailedEvent;

		public static event Action<GPGTurnBasedMatch> matchEndedEvent;

		public static event Action playerSelectorCanceledEvent;

		public static event Action<bool, string, List<GPGTurnBasedMatch>> loadMatchesCompletedEvent;

		public static event Action<bool, string> takeTurnCompletedEvent;

		public static event Action<bool, string> finishMatchCompletedEvent;

		public static event Action<bool, string> dismissMatchCompletedEvent;

		public static event Action<bool, string> leaveDuringTurnCompletedEvent;

		public static event Action<bool, string> leaveOutOfTurnCompletedEvent;

		public static event Action<GPGTurnBasedInvitation> invitationReceivedEvent;

		static GPGTurnBasedManager()
		{
			AbstractManager.initialize(typeof(GPGTurnBasedManager));
		}

		private void onInvitationReceived(string invitationId)
		{
			onInvitationReceivedEvent.fire(invitationId);
		}

		private void onInvitationRemoved(string invitationId)
		{
			onInvitationRemovedEvent.fire(invitationId);
		}

		private void matchChanged(string json)
		{
			if (matchChangedEvent != null)
			{
				matchChangedEvent(Json.decode<GPGTurnBasedMatch>(json));
			}
		}

		private void matchFailed(string error)
		{
			matchFailedEvent.fire(error);
		}

		private void matchEnded(string json)
		{
			if (matchEndedEvent != null)
			{
				matchEndedEvent(Json.decode<GPGTurnBasedMatch>(json));
			}
		}

		private void playerSelectorCanceled(string empty)
		{
			playerSelectorCanceledEvent.fire();
		}

		private void loadMatchesFailed(string error)
		{
			loadMatchesCompletedEvent(false, error, null);
		}

		private void loadMatchesSucceeded(string json)
		{
			if (loadMatchesCompletedEvent != null)
			{
				loadMatchesCompletedEvent.fire(true, null, Json.decode<List<GPGTurnBasedMatch>>(json));
			}
		}

		private void takeTurnFailed(string error)
		{
			takeTurnCompletedEvent.fire(false, error);
		}

		private void takeTurnSucceeded(string empty)
		{
			takeTurnCompletedEvent.fire(true, null);
		}

		private void finishMatchFailed(string error)
		{
			finishMatchCompletedEvent.fire(false, error);
		}

		private void finishMatchSucceeded(string empty)
		{
			finishMatchCompletedEvent.fire(true, null);
		}

		private void dismissMatchFailed(string error)
		{
			dismissMatchCompletedEvent.fire(false, error);
		}

		private void dismissMatchSucceeded(string empty)
		{
			dismissMatchCompletedEvent.fire(true, null);
		}

		private void leaveDuringTurnFailed(string error)
		{
			leaveDuringTurnCompletedEvent.fire(false, error);
		}

		private void leaveDuringTurnSucceeded(string empty)
		{
			leaveDuringTurnCompletedEvent.fire(true, null);
		}

		private void leaveOutOfTurnFailed(string error)
		{
			leaveOutOfTurnCompletedEvent.fire(false, error);
		}

		private void leaveOutOfTurnSucceeded(string empty)
		{
			leaveOutOfTurnCompletedEvent.fire(true, null);
		}

		private void invitationReceived(string json)
		{
			if (invitationReceivedEvent != null)
			{
				invitationReceivedEvent(Json.decode<GPGTurnBasedInvitation>(json));
			}
		}
	}
}
