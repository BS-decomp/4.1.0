using System.Linq;
using UnityEngine;

public class mChangeName : MonoBehaviour
{
	public void ChangeName()
	{
		if (mPanelManager.GetActivePanel() == "Weapons" || mPanelManager.GetActivePanel() == "PlayerSkin")
		{
			return;
		}
		if (!AccountManager.isConnect)
		{
			if (AccountManager.GetLevel() < 3)
			{
				UIToast.Show(Localization.Get("Requires Level") + " 3");
			}
			else
			{
				UIToast.Show(Localization.Get("Connection account"));
			}
		}
		else
		{
			mPopUp.ShowPopup(Localization.Get("Cost of change name 100 gold"), Localization.Get("ChangeName"), Localization.Get("Back"), ChangeNameCancel, "Ok", ChangeNameInput);
		}
	}

	private void ChangeNameInput()
	{
		if (AccountManager.GetGold() < 100)
		{
			UIToast.Show(Localization.Get("Not enough money"));
		}
		else
		{
			mPopUp.ShowInput(AccountManager.AccountName, Localization.Get("ChangeName"), 12, UIInput.KeyboardType.Default, OnSubmit, OnChange, Localization.Get("Back"), ChangeNameCancel, "Ok", OnYes);
		}
	}

	private void ChangeNameCancel()
	{
		mPopUp.HideAll();
	}

	private void OnSubmit()
	{
		string text = mPopUp.GetInputText();
		if (text.Length <= 3 || text == "Null" || text[0].ToString() == " " || text[text.Length - 1].ToString() == " ")
		{
			text = "Player " + Random.Range(0, 99999);
		}
		text = NGUIText.StripSymbols(text);
		mPopUp.SetInputText(text);
	}

	private void OnChange()
	{
		string inputText = mPopUp.GetInputText();
		string text = UpdateSymbols(inputText);
		if (inputText != text)
		{
			mPopUp.SetInputText(text);
		}
	}

	private void OnYes()
	{
		string inputText = mPopUp.GetInputText();
		string text = UpdateSymbols(inputText);
		if (inputText != text)
		{
			mPopUp.SetInputText(text);
		}
		else if (inputText != NGUIText.StripSymbols(inputText))
		{
			mPopUp.SetInputText(NGUIText.StripSymbols(inputText));
		}
		else if (inputText.Length <= 3 || inputText == "Null" || inputText[0].ToString() == " " || inputText[inputText.Length - 1].ToString() == " ")
		{
			inputText = "Player " + Random.Range(0, 99999);
			mPopUp.SetInputText(inputText);
		}
		else if (!(AccountManager.AccountName == inputText))
		{
			mPopUp.HideAll("Menu");
			AccountManager.UpdateName(mPopUp.GetInputText(), SetPlayerNameComplete, SetPlayerNameError);
			mPopUp.ShowText(Localization.Get("Please wait") + "...");
			EventManager.Dispatch("AccountUpdate");
		}
	}

	private void SetPlayerNameComplete(string playerName)
	{
		AccountManager.AccountName = playerName;
		mPopUp.HideAll("Menu");
		AccountManager.SetGold1(-100, true);
		EventManager.Dispatch("AccountUpdate");
	}

	private void SetPlayerNameError(string error)
	{
		mPopUp.ShowInput(AccountManager.AccountName, Localization.Get("ChangeName"), 12, UIInput.KeyboardType.Default, OnSubmit, OnChange, Localization.Get("Back"), ChangeNameCancel, "Ok", OnYes);
		string text = error;
		if (text == "Name already taken")
		{
			text = Localization.Get("Name already taken");
		}
		UIToast.Show(Localization.Get("Error") + ": " + text, 3f);
	}

	public static string UpdateSymbols(string text)
	{
		string text2 = string.Empty;
		for (int i = 0; i < text.Length; i++)
		{
			if (CheckSymbol(text[i].ToString()))
			{
				text2 += text[i];
			}
		}
		return text2;
	}

	public static bool CheckSymbols(string text)
	{
		for (int i = 0; i < text.Length; i++)
		{
			if (!CheckSymbol(text[i].ToString()))
			{
				return false;
			}
		}
		return true;
	}

	public static bool CheckSymbol(string symbol)
	{
		string[] source = new string[14]
		{
			"-", "_", "'", " ", "!", "@", "№", ";", "%", "^",
			":", "&", "?", "*"
		};
		string[] source2 = new string[10] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
		string[] source3 = new string[6] { "a", "e", "i", "o", "u", "y" };
		string[] source4 = new string[20]
		{
			"b", "c", "d", "f", "g", "h", "j", "k", "l", "m",
			"n", "p", "q", "r", "s", "t", "v", "w", "x", "z"
		};
		symbol = symbol.ToLower();
		if (source.Contains(symbol))
		{
			return true;
		}
		if (source2.Contains(symbol))
		{
			return true;
		}
		if (source3.Contains(symbol))
		{
			return true;
		}
		if (source4.Contains(symbol))
		{
			return true;
		}
		return false;
	}
}
