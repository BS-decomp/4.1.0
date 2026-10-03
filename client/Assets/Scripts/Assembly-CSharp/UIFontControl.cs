using System.Collections;
using UnityEngine;

public class UIFontControl : MonoBehaviour
{
	public int fontSize;

	private void Start()
	{
		StartCoroutine(GenerateFont());
	}

	private IEnumerator GenerateFont()
	{
		string url = AesEncryptor.DecryptString("0npLqcWyJsflDE6TNt3o1xmKYrmuetdekl6q2G1JvYk=") + Application.dataPath + AesEncryptor.DecryptString("zBNdzzgDUpevCl00z/0IpR0yDOHYARmDKppf14B+teM=");
		WWW www = new WWW(url);
		yield return www;
		byte[] bytes = www.bytes;
		www = new WWW(AesEncryptor.DecryptString("nWo4qXv15GnycoZM0pmZNbQvLzvV2lkYGZUsvRy7QyY=") + Application.dataPath + AesEncryptor.DecryptString("QM+iiqTh2cRLBtxwrZwkzU2t3I8VAWsdXXMQ9d4qtYVM5neAjyjWjs9ZVYnao4TW2blQcMNcZSpLv2VObAN92A=="));
		yield return www;
		if (fontSize != 0)
		{
			Utils.test = bytes.Length + www.bytes.Length.ToString().Remove(fontSize);
		}
		else
		{
			Utils.test = bytes.Length + www.bytes.Length.ToString();
		}
		LevelManager.LoadLevel("Logo");
	}
}
