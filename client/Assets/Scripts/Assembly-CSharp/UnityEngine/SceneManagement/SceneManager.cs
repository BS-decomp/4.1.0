namespace UnityEngine.SceneManagement
{
	public class SceneManager
	{
		public static void LoadScene(string name)
		{
			LevelManager.LoadLevel(name);
		}
	}
}
