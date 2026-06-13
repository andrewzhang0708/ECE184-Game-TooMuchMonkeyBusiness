using System;
using UnityEngine;

public static class PowerUpProgress
{
    private const string DoubleJumpKey = "PowerUp.DoubleJump";

    public static event Action DoubleJumpChanged;

    public static bool HasDoubleJump =>
        PlayerPrefs.GetInt(DoubleJumpKey, 0) == 1;

    public static bool UnlockDoubleJump()
    {
        if (HasDoubleJump)
        {
            return false;
        }

        PlayerPrefs.SetInt(DoubleJumpKey, 1);
        PlayerPrefs.Save();
        DoubleJumpChanged?.Invoke();
        return true;
    }

    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(DoubleJumpKey);
        PlayerPrefs.Save();
        DoubleJumpChanged?.Invoke();
    }
}
