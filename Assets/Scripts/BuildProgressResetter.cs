using UnityEngine;

public static class BuildProgressResetter
{
#if !UNITY_EDITOR
    private const string BuildGuidKey = "Game.LastInitializedBuildGuid";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetProgressForNewBuild()
    {
        string currentBuildGuid = Application.buildGUID;
        string initializedBuildGuid = PlayerPrefs.GetString(BuildGuidKey, string.Empty);

        if (initializedBuildGuid == currentBuildGuid)
        {
            return;
        }

        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetString(BuildGuidKey, currentBuildGuid);
        PlayerPrefs.Save();

        Debug.Log("New build detected. Saved game data was reset.");
    }
#endif
}
