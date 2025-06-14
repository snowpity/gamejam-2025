using UnityEngine;

public static class GameStateManager
{
    public static bool IsPaused { get; private set; } = false;

    public static void SetPaused(bool paused)
    {
        IsPaused = paused;
        //Debug.Log($"Game pause state changed to: {paused}");
    }
}