using System.Collections;
using FreeJSON;
using UnityEngine;

public class mVideosManager : MonoBehaviour
{
	public int MaxLoad = 5;

	public GameObject VideoElement;

	public UIScrollView VideoScroll;

	public GameObject NextLoadButton;

	public GameObject VideosBackButton;

	public GameObject NewVideoIcon;

	private bool isLoadedVideos;

	private JsonObject VideosJson = new JsonObject();

	private int LastIndex = -2;

	private int StartIndex = -2;

	private void Start()
	{
	}

	private void GetLastVideosUpdateSuccess(string value)
	{
		if (PlayerPrefs.GetString("LastVideosUpdate") != value)
		{
			PlayerPrefs.SetString("LastVideosUpdate", value);
			PlayerPrefs.SetInt("LastVideosUpdateClick", 1);
			NewVideoIcon.SetActive(true);
		}
	}

	public void Load()
	{
		if (!AccountManager.isConnect)
		{
			UIToast.Show(Localization.Get("Connection account"));
			return;
		}
		mPanelManager.ShowPanel("Videos", true);
		if (!isLoadedVideos)
		{
			Firebase firebase = new Firebase();
			firebase.Child("Videos").GetValue(FirebaseParam.Default.OrderByKey().LimitToLast(MaxLoad), LoadSuccess, LoadFailed);
			mPopUp.SetActiveWait(true, Localization.Get("Loading") + "...");
			VideosBackButton.SetActive(false);
		}
		else
		{
			UpdateOthersVideos(string.Empty);
		}
		if (PlayerPrefs.HasKey("LastVideosUpdateClick"))
		{
			NewVideoIcon.SetActive(false);
			PlayerPrefs.DeleteKey("LastVideosUpdateClick");
		}
	}

	public void LoadNext()
	{
		isLoadedVideos = false;
		NextLoadButton.SetActive(false);
		Firebase firebase = new Firebase();
		firebase.Child("Videos").GetValue(FirebaseParam.Default.OrderByKey().StartAt((LastIndex - MaxLoad + 1).ToString()).EndAt(LastIndex.ToString()), LoadSuccess, LoadFailed);
		mPopUp.SetActiveWait(true, Localization.Get("Loading") + "...");
		VideosBackButton.SetActive(false);
	}

	private void UpdateOthersVideos(string data)
	{
		StartCoroutine(UpdateOthersVideosCorountine(data));
	}

	private IEnumerator UpdateOthersVideosCorountine(string data)
	{
		if (isLoadedVideos)
		{
			yield break;
		}
		mPopUp.SetActiveWait(false);
		isLoadedVideos = true;
		JsonObject json = JsonObject.Parse(data);
		if (LastIndex < 0)
		{
			LastIndex = int.Parse(json.GetKey(json.Length - 1));
			StartIndex = LastIndex;
		}
		for (int i = 0; i < json.Length; i++)
		{
			yield return new WaitForSeconds(0.02f);
			GameObject go = NGUITools.AddChild(VideoScroll.gameObject, VideoElement);
			go.transform.localPosition = new Vector3((StartIndex - LastIndex) * 240 - 240, 0f, 0f);
			string key = LastIndex.ToString();
			VideosJson.Add(key, json.Get<JsonObject>(key));
			go.GetComponent<mVideoElement>().SetData(json.Get<string>(key), (float)i + 1f);
			LastIndex--;
			if (i == MaxLoad - 1 && LastIndex > 0)
			{
				NextLoadButton.SetActive(true);
				NextLoadButton.transform.localPosition = new Vector3((VideosJson.Length - 1) * 240 - 80, 0f, 0f);
			}
		}
	}

	private void LoadSuccess(string json)
	{
		UpdateOthersVideos(json);
		VideosBackButton.SetActive(true);
	}

	private void LoadFailed(string error)
	{
		VideosBackButton.SetActive(true);
		mPopUp.SetActiveWait(false);
		UIToast.Show("Error: " + error, 3f);
	}
}
