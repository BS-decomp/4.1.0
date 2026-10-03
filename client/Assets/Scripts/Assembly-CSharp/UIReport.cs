using System.Collections.Generic;
using FreeJSON;
using UnityEngine;

public class UIReport : MonoBehaviour
{
	public UIInput InputText;

	private static List<string> SendPlayerReports = new List<string>();

	public void StartWrite()
	{
		InputText.value = string.Empty;
	}

	public void SendReport()
	{
		if (AccountManager.GetLevel() < 15)
		{
			UIToast.Show(Localization.Get("Required level greater than 15"));
		}
		else if (InputText.value.Length > 10 && !SendPlayerReports.Contains(UIPlayerStatistics.SelectPlayer.GetPlayerID()))
		{
			Firebase firebase = new Firebase();
			string target = UIPlayerStatistics.SelectPlayer.GetPlayerID();
			JsonObject jsonObject = new JsonObject();
			jsonObject.Add("player", (string)AccountManager.AccountID);
			jsonObject.Add("text", InputText.value);
			firebase.Child("Players").Child("Reports").Child(target)
				.Push(jsonObject.ToString(), (string result) =>
				{
					SendPlayerReports.Add(target);
				}, null);
		}
	}
}
