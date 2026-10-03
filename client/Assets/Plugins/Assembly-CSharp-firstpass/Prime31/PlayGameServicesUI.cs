using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Prime31
{
	public class PlayGameServicesUI : MonoBehaviourGUI
	{
		private void Start()
		{
			PlayGameServices.enableDebugLog(true);
			PlayGameServices.init("160040154367.apps.googleusercontent.com", true);
		}

		private void OnGUI()
		{
			GUI.skin.label.alignment = TextAnchor.MiddleCenter;
			beginColumn();
			if (toggleButtonState("Achievements/Events/Quests/Snapshots"))
			{
				showAuthAndSetttingsButtons();
			}
			else
			{
				achievementsEventsAndQuestsButtons();
			}
			toggleButton("Achievements/Events/Quests/Snapshots", "Toggle Buttons");
			endColumn(true);
			leaderboardsAndAchievementsButtons();
			endColumn(false);
			if (bottomRightButton("Real Time MP Scene"))
			{
				MonoBehaviourGUI.loadLevel("PlayGameServicesMultiplayerDemoScene");
			}
		}

		private void showAuthAndSetttingsButtons()
		{
			GUILayout.Label("Authentication and Settings");
			if (GUILayout.Button("Authenticate Silently (with no UI)"))
			{
				PlayGameServices.attemptSilentAuthentication();
			}
			if (GUILayout.Button("Get Auth Token"))
			{
				string gamesServerAuthCode = PlayGameServices.getGamesServerAuthCode(null);
				Debug.Log("token: " + gamesServerAuthCode);
			}
			if (GUILayout.Button("Authenticate"))
			{
				PlayGameServices.authenticate();
			}
			if (GUILayout.Button("Sign Out"))
			{
				PlayGameServices.signOut();
			}
			if (GUILayout.Button("Is Signed In"))
			{
				Debug.Log("is signed in? " + PlayGameServices.isSignedIn());
			}
			if (GUILayout.Button("Get Player Info"))
			{
				GPGPlayerInfo localPlayerInfo = PlayGameServices.getLocalPlayerInfo();
				Utils.logObject(localPlayerInfo);
				if (Application.platform == RuntimePlatform.Android && localPlayerInfo.avatarUri != null)
				{
					PlayGameServices.loadProfileImageForUri(localPlayerInfo.avatarUri);
				}
			}
			if (GUILayout.Button("Load Remote Player Info"))
			{
				PlayGameServices.loadPlayer("110453866202127902712");
			}
			if (GUILayout.Button("Load Player Stats"))
			{
				PlayGameServices.loadPlayerStats();
			}
		}

		private void achievementsEventsAndQuestsButtons()
		{
			GUILayout.Label("Achievements");
			if (GUILayout.Button("Show Achievements"))
			{
				PlayGameServices.showAchievements();
			}
			if (GUILayout.Button("Increment Achievement"))
			{
				PlayGameServices.incrementAchievement("CgkI_-mLmdQEEAIQAQ", 2);
			}
			if (GUILayout.Button("Unlock Achievement"))
			{
				PlayGameServices.unlockAchievement("CgkI_-mLmdQEEAIQAw");
			}
			GUILayout.Label("Events and Quests");
			if (GUILayout.Button("Load All Events"))
			{
				PlayGameServices.loadAllEvents();
			}
			if (GUILayout.Button("Increment Event"))
			{
				PlayGameServices.incrementEvent("CgkI_-mLmdQEEAIQCg", 1);
			}
			if (GUILayout.Button("Show Quest List"))
			{
				PlayGameServices.showQuestList();
			}
			if (GUILayout.Button("Load All Quests"))
			{
				PlayGameServices.loadAllQuests();
				PlayGameServices.showStateChangedPopup("CgkI_-mLmdQEEAIQDw");
			}
			GUILayout.Label("Snapshots");
			if (GUILayout.Button("Show Snapshot List"))
			{
				PlayGameServices.showSnapshotList(5, "Your Saved Games!", true, true);
			}
			if (GUILayout.Button("Save Snapshot"))
			{
				byte[] bytes = Encoding.UTF8.GetBytes("my saved data");
				PlayGameServices.saveSnapshot("snappy", true, bytes, "The description of the data");
			}
			if (GUILayout.Button("Load Snapshot"))
			{
				PlayGameServices.loadSnapshot("snappy");
			}
			if (GUILayout.Button("Delete Snapshot"))
			{
				PlayGameServices.deleteSnapshot("snappy");
			}
		}

		private void leaderboardsAndAchievementsButtons()
		{
			GUILayout.Label("Leaderboards");
			if (GUILayout.Button("Show Leaderboard"))
			{
				PlayGameServices.showLeaderboard("CgkI_-mLmdQEEAIQBQ");
			}
			if (GUILayout.Button("Show All Leaderboards"))
			{
				PlayGameServices.showLeaderboards();
			}
			if (GUILayout.Button("Submit Score"))
			{
				PlayGameServices.submitScore("CgkI_-mLmdQEEAIQBQ", 566L, string.Empty);
			}
			if (GUILayout.Button("Load Raw Score Data"))
			{
				PlayGameServices.loadScoresForLeaderboard("CgkI_-mLmdQEEAIQBQ", GPGLeaderboardTimeScope.AllTime, false, false);
			}
			if (GUILayout.Button("Load More Raw Score Data"))
			{
				PlayGameServices.loadMoreScoresForLeaderboard("CgkI_-mLmdQEEAIQBQ");
			}
			if (GUILayout.Button("Get Leaderboard Metadata"))
			{
				List<GPGLeaderboardMetadata> allLeaderboardMetadata = PlayGameServices.getAllLeaderboardMetadata();
				Utils.logObject(allLeaderboardMetadata);
			}
			if (GUILayout.Button("Get Achievement Metadata"))
			{
				List<GPGAchievementMetadata> allAchievementMetadata = PlayGameServices.getAllAchievementMetadata();
				Utils.logObject(allAchievementMetadata);
			}
			if (GUILayout.Button("Reload All Metadata"))
			{
				PlayGameServices.reloadAchievementAndLeaderboardData();
			}
			if (GUILayout.Button("Show Share Dialog"))
			{
				PlayGameServices.showShareDialog("I LOVE this game!", "http://prime31.com");
			}
		}
	}
}
