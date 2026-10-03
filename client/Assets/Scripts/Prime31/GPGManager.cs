using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prime31
{
	public class GPGManager : AbstractManager
	{
		public static event Action<string> authenticationSucceededEvent;

		public static event Action<string> authenticationFailedEvent;

		public static event Action userSignedOutEvent;

		public static event Action<string> reloadDataForKeyFailedEvent;

		public static event Action<string> reloadDataForKeySucceededEvent;

		public static event Action licenseCheckFailedEvent;

		public static event Action<string> profileImageLoadedAtPathEvent;

		public static event Action<string> finishedSharingEvent;

		public static event Action<GPGPlayerInfo, string> loadPlayerCompletedEvent;

		public static event Action<Dictionary<string, object>> loadPlayerStatsSucceededEvent;

		public static event Action<string> loadPlayerStatsFailedEvent;

		public static event Action<string, string> unlockAchievementFailedEvent;

		public static event Action<string, bool> unlockAchievementSucceededEvent;

		public static event Action<string, string> incrementAchievementFailedEvent;

		public static event Action<string, bool> incrementAchievementSucceededEvent;

		public static event Action<string, string> revealAchievementFailedEvent;

		public static event Action<string> revealAchievementSucceededEvent;

		public static event Action<string, string> submitScoreFailedEvent;

		public static event Action<string, Dictionary<string, object>> submitScoreSucceededEvent;

		public static event Action<string, string> loadScoresFailedEvent;

		public static event Action<List<GPGScore>> loadScoresSucceededEvent;

		public static event Action<GPGScore> loadCurrentPlayerLeaderboardScoreSucceededEvent;

		public static event Action<string, string> loadCurrentPlayerLeaderboardScoreFailedEvent;

		public static event Action<List<GPGEvent>> allEventsLoadedEvent;

		public static event Action<GPGQuest> questListLauncherAcceptedQuestEvent;

		public static event Action<GPGQuestMilestone> questClaimedRewardsForQuestMilestoneEvent;

		public static event Action<GPGQuest> questCompletedEvent;

		public static event Action<List<GPGQuest>> allQuestsLoadedEvent;

		public static event Action<GPGSnapshotMetadata> snapshotListUserSelectedSnapshotEvent;

		public static event Action snapshotListUserRequestedNewSnapshotEvent;

		public static event Action snapshotListCanceledEvent;

		public static event Action saveSnapshotSucceededEvent;

		public static event Action<string> saveSnapshotFailedEvent;

		public static event Action<GPGSnapshot> loadSnapshotSucceededEvent;

		public static event Action<string> loadSnapshotFailedEvent;

		static GPGManager()
		{
			AbstractManager.initialize(typeof(GPGManager));
		}

		private void fireEventWithIdentifierAndError(Action<string, string> theEvent, string json)
		{
			if (theEvent != null)
			{
				Dictionary<string, object> dictionary = json.dictionaryFromJson();
				if (dictionary != null && dictionary.ContainsKey("identifier") && dictionary.ContainsKey("error"))
				{
					theEvent(dictionary["identifier"].ToString(), dictionary["error"].ToString());
				}
				else
				{
					Debug.LogError("json could not be deserialized to an identifier and an error: " + json);
				}
			}
		}

		private void fireEventWithIdentifierAndBool(Action<string, bool> theEvent, string param)
		{
			if (theEvent != null)
			{
				string[] array = param.Split(',');
				if (array.Length == 2)
				{
					theEvent(array[0], array[1] == "1");
				}
				else
				{
					Debug.LogError("param could not be deserialized to an identifier and an error: " + param);
				}
			}
		}

		private void userSignedOut(string empty)
		{
			userSignedOutEvent.fire();
		}

		private void reloadDataForKeyFailed(string error)
		{
			reloadDataForKeyFailedEvent.fire(error);
		}

		private void reloadDataForKeySucceeded(string param)
		{
			reloadDataForKeySucceededEvent.fire(param);
		}

		private void licenseCheckFailed(string param)
		{
			licenseCheckFailedEvent.fire();
		}

		private void profileImageLoadedAtPath(string path)
		{
			profileImageLoadedAtPathEvent.fire(path);
		}

		private void finishedSharing(string errorOrNull)
		{
			finishedSharingEvent.fire(errorOrNull);
		}

		private void loadPlayerCompleted(string playerOrError)
		{
			if (loadPlayerCompletedEvent != null)
			{
				if (playerOrError.StartsWith("{"))
				{
					loadPlayerCompletedEvent(Json.decode<GPGPlayerInfo>(playerOrError), null);
				}
				else
				{
					loadPlayerCompletedEvent(null, playerOrError);
				}
			}
		}

		private void loadPlayerStatsSucceeded(string json)
		{
			if (loadPlayerStatsSucceededEvent != null)
			{
				loadPlayerStatsSucceededEvent(Json.decode<Dictionary<string, object>>(json));
			}
		}

		private void loadPlayerStatsFailed(string error)
		{
			loadPlayerStatsFailedEvent.fire(error);
		}

		private void unlockAchievementFailed(string json)
		{
			fireEventWithIdentifierAndError(unlockAchievementFailedEvent, json);
		}

		private void unlockAchievementSucceeded(string param)
		{
			fireEventWithIdentifierAndBool(unlockAchievementSucceededEvent, param);
		}

		private void incrementAchievementFailed(string json)
		{
			fireEventWithIdentifierAndError(incrementAchievementFailedEvent, json);
		}

		private void incrementAchievementSucceeded(string param)
		{
			string[] array = param.Split(',');
			if (array.Length == 2)
			{
				incrementAchievementSucceededEvent.fire(array[0], array[1] == "1");
			}
		}

		private void revealAchievementFailed(string json)
		{
			fireEventWithIdentifierAndError(revealAchievementFailedEvent, json);
		}

		private void revealAchievementSucceeded(string achievementId)
		{
			revealAchievementSucceededEvent.fire(achievementId);
		}

		private void submitScoreFailed(string json)
		{
			fireEventWithIdentifierAndError(submitScoreFailedEvent, json);
		}

		private void submitScoreSucceeded(string json)
		{
			if (submitScoreSucceededEvent != null)
			{
				Dictionary<string, object> dictionary = json.dictionaryFromJson();
				string arg = "Unknown";
				if (dictionary.ContainsKey("leaderboardId"))
				{
					arg = dictionary["leaderboardId"].ToString();
				}
				submitScoreSucceededEvent(arg, dictionary);
			}
		}

		private void loadScoresFailed(string json)
		{
			fireEventWithIdentifierAndError(loadScoresFailedEvent, json);
		}

		private void loadScoresSucceeded(string json)
		{
			if (loadScoresSucceededEvent != null)
			{
				loadScoresSucceededEvent(Json.decode<List<GPGScore>>(json));
			}
		}

		private void loadCurrentPlayerLeaderboardScoreSucceeded(string json)
		{
			if (loadCurrentPlayerLeaderboardScoreSucceededEvent != null)
			{
				loadCurrentPlayerLeaderboardScoreSucceededEvent(Json.decode<GPGScore>(json));
			}
		}

		private void loadCurrentPlayerLeaderboardScoreFailed(string json)
		{
			fireEventWithIdentifierAndError(loadCurrentPlayerLeaderboardScoreFailedEvent, json);
		}

		private void authenticationSucceeded(string param)
		{
			authenticationSucceededEvent.fire(param);
		}

		private void authenticationFailed(string error)
		{
			authenticationFailedEvent.fire(error);
		}

		private void allEventsLoaded(string json)
		{
			if (allEventsLoadedEvent != null)
			{
				allEventsLoadedEvent(Json.decode<List<GPGEvent>>(json));
			}
		}

		private void questListLauncherClaimedRewardsForQuestMilestone(string json)
		{
			if (questClaimedRewardsForQuestMilestoneEvent != null)
			{
				questClaimedRewardsForQuestMilestoneEvent(Json.decode<GPGQuestMilestone>(json));
			}
		}

		private void questCompleted(string json)
		{
			if (questCompletedEvent != null)
			{
				questCompletedEvent(Json.decode<GPGQuest>(json));
			}
		}

		private void questListLauncherAcceptedQuest(string json)
		{
			if (questListLauncherAcceptedQuestEvent != null)
			{
				questListLauncherAcceptedQuestEvent(Json.decode<GPGQuest>(json));
			}
		}

		private void allQuestsLoaded(string json)
		{
			if (allQuestsLoadedEvent != null)
			{
				allQuestsLoadedEvent(Json.decode<List<GPGQuest>>(json));
			}
		}

		private void snapshotListUserSelectedSnapshot(string json)
		{
			if (snapshotListUserSelectedSnapshotEvent != null)
			{
				snapshotListUserSelectedSnapshotEvent(Json.decode<GPGSnapshotMetadata>(json));
			}
		}

		private void snapshotListUserRequestedNewSnapshot(string empty)
		{
			snapshotListUserRequestedNewSnapshotEvent.fire();
		}

		private void snapshotListCanceled(string empty)
		{
			snapshotListCanceledEvent.fire();
		}

		private void saveSnapshotSucceeded(string empty)
		{
			saveSnapshotSucceededEvent.fire();
		}

		private void saveSnapshotFailed(string error)
		{
			saveSnapshotFailedEvent.fire(error);
		}

		private void loadSnapshotSucceeded(string json)
		{
			if (loadSnapshotSucceededEvent != null)
			{
				loadSnapshotSucceededEvent(Json.decode<GPGSnapshot>(json));
			}
		}

		private void loadSnapshotFailed(string error)
		{
			loadSnapshotFailedEvent.fire(error);
		}
	}
}
