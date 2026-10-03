using System.Linq;
using UnityEngine;

public class mClanManager : MonoBehaviour
{
	[Header("Create")]
	public GameObject CreatePanel;

	public UIInput CreateTagInput;

	public UIInput CreateClanNameInput;

	public UILabel CreateExampleLabel;

	[Header("Main")]
	public GameObject MainPanel;

	public GameObject InAppPanel;

	public void StartMainPanel()
	{
		MainPanel.SetActive(true);
		CreatePanel.SetActive(false);
	}

	public void StartCreatePanel()
	{
		MainPanel.SetActive(false);
		CreatePanel.SetActive(true);
	}

	public void UpdateExample()
	{
		CreateTagInput.value = ConvertText(CreateTagInput.value.ToUpper(), false);
		CreateClanNameInput.value = ConvertText(CreateClanNameInput.value, true);
		string text = ((!string.IsNullOrEmpty(CreateTagInput.value)) ? ("[" + CreateTagInput.value + "] ") : string.Empty) + CreateClanNameInput.value;
		CreateExampleLabel.text = text;
	}

	public void CreateClan()
	{
		if (CreateTagInput.value.Length == 0)
		{
			CreateTagInput.value = Utils.NameGenerator(3).ToUpper();
			return;
		}
		if (CreateClanNameInput.value.Length < 3)
		{
			CreateClanNameInput.value = Utils.NameGenerator(Random.Range(5, 9));
			return;
		}
		if (AccountManager.GetGold() < 150)
		{
			InAppPanel.SetActive(true);
			UIToast.Show(Localization.Get("Not enough money"));
			return;
		}
		mPopUp.ShowText(Localization.Get("Please wait") + "...");
		ClansManager.Check(CreateTagInput.value, CreateClanNameInput.value, () =>
		{
			ClansManager.Create(CreateTagInput.value, CreateClanNameInput.value, () =>
			{
				AccountManager.SetClan(CreateClanNameInput.value);
				AccountManager.SetGold1(-150, true);
				StartMainPanel();
			}, (string error) =>
			{
				CreatePanel.SetActive(true);
				mPopUp.HideAll("Clan");
				UIToast.Show(Localization.Get("Error") + ": " + error, 3f);
			});
		}, (string error) =>
		{
			CreatePanel.SetActive(true);
			mPopUp.HideAll("Clan");
			UIToast.Show(Localization.Get("Error") + ": " + error, 3f);
		});
	}

	private string ConvertText(string text, bool space)
	{
		string text2 = text.ToLower();
		string[] source = new string[36]
		{
			"b", "c", "d", "f", "g", "h", "j", "k", "l", "m",
			"n", "p", "q", "r", "s", "t", "v", "w", "x", "z",
			"0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
			"a", "e", "i", "o", "u", "y"
		};
		if (space)
		{
			source = new string[37]
			{
				"b", "c", "d", "f", "g", "h", "j", "k", "l", "m",
				"n", "p", "q", "r", "s", "t", "v", "w", "x", "z",
				"0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
				"a", "e", "i", "o", "u", "y", " "
			};
		}
		for (int i = 0; i < text.Length; i++)
		{
			if (!source.Contains(text2[i].ToString()))
			{
				text = text.Remove(i, 1);
				break;
			}
		}
		return text;
	}
}
