using System.Collections.Generic;
using UnityEngine;

namespace Prime31
{
	public class GPGSEventListener : MonoBehaviour
	{
		private void OnEnable()
		{
			GPGManager.authenticationSucceededEvent += authenticationSucceededEvent;
			GPGManager.authenticationFailedEvent += authenticationFailedEvent;
			GPGManager.licenseCheckFailedEvent += licenseCheckFailedEvent;
			GPGManager.profileImageLoadedAtPathEvent += profileImageLoadedAtPathEvent;
			GPGManager.finishedSharingEvent += finishedSharingEvent;
			GPGManager.loadPlayerCompletedEvent += loadPlayerCompletedEvent;
			GPGManager.userSignedOutEvent += userSignedOutEvent;
			GPGManager.loadPlayerStatsSucceededEvent += loadPlayerStatsSucceededEvent;
			GPGManager.loadPlayerStatsFailedEvent += loadPlayerStatsFailedEvent;
			GPGManager.reloadDataForKeyFailedEvent += reloadDataForKeyFailedEvent;
			GPGManager.reloadDataForKeySucceededEvent += reloadDataForKeySucceededEvent;
			GPGManager.unlockAchievementFailedEvent += unlockAchievementFailedEvent;
			GPGManager.unlockAchievementSucceededEvent += unlockAchievementSucceededEvent;
			GPGManager.incrementAchievementFailedEvent += incrementAchievementFailedEvent;
			GPGManager.incrementAchievementSucceededEvent += incrementAchievementSucceededEvent;
			GPGManager.revealAchievementFailedEvent += revealAchievementFailedEvent;
			GPGManager.revealAchievementSucceededEvent += revealAchievementSucceededEvent;
			GPGManager.submitScoreFailedEvent += submitScoreFailedEvent;
			GPGManager.submitScoreSucceededEvent += submitScoreSucceededEvent;
			GPGManager.loadScoresFailedEvent += loadScoresFailedEvent;
			GPGManager.loadScoresSucceededEvent += loadScoresSucceededEvent;
			GPGManager.loadCurrentPlayerLeaderboardScoreSucceededEvent += loadCurrentPlayerLeaderboardScoreSucceededEvent;
			GPGManager.loadCurrentPlayerLeaderboardScoreFailedEvent += loadCurrentPlayerLeaderboardScoreFailedEvent;
			GPGManager.allEventsLoadedEvent += allEventsLoadedEvent;
			GPGManager.questListLauncherAcceptedQuestEvent += questListLauncherAcceptedQuestEvent;
			GPGManager.questClaimedRewardsForQuestMilestoneEvent += questClaimedRewardsForQuestMilestoneEvent;
			GPGManager.questCompletedEvent += questCompletedEvent;
			GPGManager.allQuestsLoadedEvent += allQuestsLoadedEvent;
			GPGManager.snapshotListUserSelectedSnapshotEvent += snapshotListUserSelectedSnapshotEvent;
			GPGManager.snapshotListUserRequestedNewSnapshotEvent += snapshotListUserRequestedNewSnapshotEvent;
			GPGManager.snapshotListCanceledEvent += snapshotListCanceledEvent;
			GPGManager.loadSnapshotSucceededEvent += loadSnapshotSucceededEvent;
			GPGManager.loadSnapshotFailedEvent += loadSnapshotFailedEvent;
			GPGManager.saveSnapshotSucceededEvent += saveSnapshotSucceededEvent;
			GPGManager.saveSnapshotFailedEvent += saveSnapshotFailedEvent;
		}

		private void OnDisable()
		{
			GPGManager.authenticationSucceededEvent -= authenticationSucceededEvent;
			GPGManager.authenticationFailedEvent -= authenticationFailedEvent;
			GPGManager.licenseCheckFailedEvent -= licenseCheckFailedEvent;
			GPGManager.profileImageLoadedAtPathEvent -= profileImageLoadedAtPathEvent;
			GPGManager.finishedSharingEvent -= finishedSharingEvent;
			GPGManager.loadPlayerCompletedEvent -= loadPlayerCompletedEvent;
			GPGManager.userSignedOutEvent -= userSignedOutEvent;
			GPGManager.loadPlayerStatsSucceededEvent -= loadPlayerStatsSucceededEvent;
			GPGManager.loadPlayerStatsFailedEvent -= loadPlayerStatsFailedEvent;
			GPGManager.reloadDataForKeyFailedEvent -= reloadDataForKeyFailedEvent;
			GPGManager.reloadDataForKeySucceededEvent -= reloadDataForKeySucceededEvent;
			GPGManager.unlockAchievementFailedEvent -= unlockAchievementFailedEvent;
			GPGManager.unlockAchievementSucceededEvent -= unlockAchievementSucceededEvent;
			GPGManager.incrementAchievementFailedEvent -= incrementAchievementFailedEvent;
			GPGManager.incrementAchievementSucceededEvent -= incrementAchievementSucceededEvent;
			GPGManager.revealAchievementFailedEvent -= revealAchievementFailedEvent;
			GPGManager.revealAchievementSucceededEvent -= revealAchievementSucceededEvent;
			GPGManager.submitScoreFailedEvent -= submitScoreFailedEvent;
			GPGManager.submitScoreSucceededEvent -= submitScoreSucceededEvent;
			GPGManager.loadScoresFailedEvent -= loadScoresFailedEvent;
			GPGManager.loadScoresSucceededEvent -= loadScoresSucceededEvent;
			GPGManager.loadCurrentPlayerLeaderboardScoreSucceededEvent -= loadCurrentPlayerLeaderboardScoreSucceededEvent;
			GPGManager.loadCurrentPlayerLeaderboardScoreFailedEvent -= loadCurrentPlayerLeaderboardScoreFailedEvent;
			GPGManager.allEventsLoadedEvent -= allEventsLoadedEvent;
			GPGManager.questListLauncherAcceptedQuestEvent -= questListLauncherAcceptedQuestEvent;
			GPGManager.questClaimedRewardsForQuestMilestoneEvent -= questClaimedRewardsForQuestMilestoneEvent;
			GPGManager.questCompletedEvent -= questCompletedEvent;
			GPGManager.allQuestsLoadedEvent -= allQuestsLoadedEvent;
			GPGManager.snapshotListUserSelectedSnapshotEvent -= snapshotListUserSelectedSnapshotEvent;
			GPGManager.snapshotListUserRequestedNewSnapshotEvent -= snapshotListUserRequestedNewSnapshotEvent;
			GPGManager.snapshotListCanceledEvent -= snapshotListCanceledEvent;
			GPGManager.loadSnapshotSucceededEvent -= loadSnapshotSucceededEvent;
			GPGManager.loadSnapshotFailedEvent -= loadSnapshotFailedEvent;
			GPGManager.saveSnapshotSucceededEvent -= saveSnapshotSucceededEvent;
			GPGManager.saveSnapshotFailedEvent -= saveSnapshotFailedEvent;
		}

		private void authenticationSucceededEvent(string param)
		{
			Debug.Log("authenticationSucceededEvent: " + param);
		}

		private void authenticationFailedEvent(string error)
		{
			Debug.Log("authenticationFailedEvent: " + error);
		}

		private void licenseCheckFailedEvent()
		{
			Debug.Log("licenseCheckFailedEvent");
		}

		private void profileImageLoadedAtPathEvent(string path)
		{
			Debug.Log("profileImageLoadedAtPathEvent: " + path);
		}

		private void finishedSharingEvent(string errorOrNull)
		{
			Debug.Log("finishedSharingEvent. errorOrNull param: " + errorOrNull);
		}

		private void loadPlayerCompletedEvent(GPGPlayerInfo playerInfo, string error)
		{
			Debug.Log("loadPlayerCompletedEvent: ");
			if (playerInfo != null)
			{
				Utils.logObject(playerInfo);
			}
			else
			{
				Debug.Log(error);
			}
		}

		private void userSignedOutEvent()
		{
			Debug.Log("userSignedOutEvent");
		}

		private void loadPlayerStatsSucceededEvent(Dictionary<string, object> stats)
		{
			Debug.Log("loadPlayerStatsSucceededEvent");
			Utils.logObject(stats);
		}

		private void loadPlayerStatsFailedEvent(string error)
		{
			Debug.Log("loadPlayerStatsFailedEvent: " + error);
		}

		private void reloadDataForKeyFailedEvent(string error)
		{
			Debug.Log("reloadDataForKeyFailedEvent: " + error);
		}

		private void reloadDataForKeySucceededEvent(string param)
		{
			Debug.Log("reloadDataForKeySucceededEvent: " + param);
		}

		private void unlockAchievementFailedEvent(string achievementId, string error)
		{
			Debug.Log("unlockAchievementFailedEvent. achievementId: " + achievementId + ", error: " + error);
		}

		private void unlockAchievementSucceededEvent(string achievementId, bool newlyUnlocked)
		{
			Debug.Log("unlockAchievementSucceededEvent. achievementId: " + achievementId + ", newlyUnlocked: " + newlyUnlocked);
		}

		private void incrementAchievementFailedEvent(string achievementId, string error)
		{
			Debug.Log("incrementAchievementFailedEvent. achievementId: " + achievementId + ", error: " + error);
		}

		private void incrementAchievementSucceededEvent(string achievementId, bool newlyUnlocked)
		{
			Debug.Log("incrementAchievementSucceededEvent. achievementId: " + achievementId + ", newlyUnlocked: " + newlyUnlocked);
		}

		private void revealAchievementFailedEvent(string achievementId, string error)
		{
			Debug.Log("revealAchievementFailedEvent. achievementId: " + achievementId + ", error: " + error);
		}

		private void revealAchievementSucceededEvent(string achievementId)
		{
			Debug.Log("revealAchievementSucceededEvent: " + achievementId);
		}

		private void submitScoreFailedEvent(string leaderboardId, string error)
		{
			Debug.Log("submitScoreFailedEvent. leaderboardId: " + leaderboardId + ", error: " + error);
		}

		private void submitScoreSucceededEvent(string leaderboardId, Dictionary<string, object> scoreReport)
		{
			Debug.Log("submitScoreSucceededEvent");
			Utils.logObject(scoreReport);
		}

		private void loadScoresFailedEvent(string leaderboardId, string error)
		{
			Debug.Log("loadScoresFailedEvent. leaderboardId: " + leaderboardId + ", error: " + error);
		}

		private void loadScoresSucceededEvent(List<GPGScore> scores)
		{
			Debug.Log("loadScoresSucceededEvent");
			Utils.logObject(scores);
		}

		private void loadCurrentPlayerLeaderboardScoreSucceededEvent(GPGScore score)
		{
			Debug.Log("loadCurrentPlayerLeaderboardScoreSucceededEvent");
			Utils.logObject(score);
		}

		private void loadCurrentPlayerLeaderboardScoreFailedEvent(string leaderboardId, string error)
		{
			Debug.Log("loadCurrentPlayerLeaderboardScoreFailedEvent. leaderboardId: " + leaderboardId + ", error: " + error);
		}

		private void allEventsLoadedEvent(List<GPGEvent> events)
		{
			Debug.Log("allEventsLoadedEvent");
			Utils.logObject(events);
		}

		private void questListLauncherAcceptedQuestEvent(GPGQuest quest)
		{
			Debug.Log("questListLauncherAcceptedQuestEvent");
			Utils.logObject(quest);
		}

		private void questClaimedRewardsForQuestMilestoneEvent(GPGQuestMilestone milestone)
		{
			Debug.Log("questClaimedRewardsForQuestMilestoneEvent");
			Utils.logObject(milestone);
		}

		private void questCompletedEvent(GPGQuest quest)
		{
			Debug.Log("questCompletedEvent");
			Utils.logObject(quest);
		}

		private void allQuestsLoadedEvent(List<GPGQuest> quests)
		{
			Debug.Log("allQuestsLoadedEvent");
			Utils.logObject(quests);
		}

		private void snapshotListUserSelectedSnapshotEvent(GPGSnapshotMetadata metadata)
		{
			Debug.Log("snapshotListUserSelectedSnapshotEvent");
			Utils.logObject(metadata);
		}

		private void snapshotListUserRequestedNewSnapshotEvent()
		{
			Debug.Log("snapshotListUserRequestedNewSnapshotEvent");
		}

		private void snapshotListCanceledEvent()
		{
			Debug.Log("snapshotListCanceledEvent");
		}

		private void loadSnapshotSucceededEvent(GPGSnapshot snapshot)
		{
			Debug.Log("loadSnapshotSucceededEvent");
			Utils.logObject(snapshot);
		}

		private void loadSnapshotFailedEvent(string error)
		{
			Debug.Log("loadSnapshotFailedEvent: " + error);
		}

		private void saveSnapshotSucceededEvent()
		{
			Debug.Log("saveSnapshotSucceededEvent");
		}

		private void saveSnapshotFailedEvent(string error)
		{
			Debug.Log("saveSnapshotFailedEvent: " + error);
		}
	}
}
