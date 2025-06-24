using UnityEngine;

public static class GameStateManager
{
    public static bool IsPaused { get; private set; } = false;
    public static bool IsGameStarted { get; private set; } = false;
    public static bool hasFollowingParty {get; private set; } = false;

    public static void SetPaused(bool paused)
    {
        IsPaused = paused;
        //Debug.Log($"Game pause state changed to: {paused}");
    }

    public static void SetGameStarted(bool gameStarted)
    {
        IsGameStarted = gameStarted;
    }

    public static void SetFollowingParty(bool party)
    {
        hasFollowingParty = party;
    }

    public static void SetDefault()
    {
        IsPaused = false;
        IsGameStarted = false;
        hasFollowingParty = false;
    }
}