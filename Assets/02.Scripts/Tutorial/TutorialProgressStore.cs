using UnityEngine;
using UnityEngine.InputSystem;

public static class TutorialProgressStore
{
    private const string KeyPrefix = "Tutorial.Completed";

    public static bool IsCompleted(string dialogueId)
    {
        if (string.IsNullOrWhiteSpace(dialogueId)) return false;
        var key = KeyPrefix + dialogueId;
        // 저장값이 1이면 완료, 0이면 미완료
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    public static void MarkCompleted(string dialogueId)
    {
        if (string.IsNullOrWhiteSpace(dialogueId)) return;

        var key = KeyPrefix + dialogueId;
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
    }

    public static void ResetCompleted(string dialogueId)
    {
        if (string.IsNullOrWhiteSpace(dialogueId)) return;

        var key = KeyPrefix + dialogueId;
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }
}