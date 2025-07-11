using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameStateManager
{
    public static bool IsPaused { get; private set; } = false;
    public static bool IsGameStarted { get; private set; } = false;
    public static bool hasFollowingParty {get; private set; } = false;

    public static HashSet<int> readyTables = new HashSet<int>();

    public static HashSet<int> readyOrders = new HashSet<int>();

    public static HashSet<int> tablesAwaitingOrder = new HashSet<int>();

    public static HashSet<int> ordersInProgress = new HashSet<int>();

    public static int totalCustomerServed = 0;
    public static int totalScore = 0;

    public static int countdownTimer;

    private static int goldTrophyPoint = 3000;
    private static int silverTrophyPoint = 2000;
    private static int copperTrophyPoint = 1000;

    // Dictionary to store sprite ID for each table's order
    public static Dictionary<int, int> tableFoodSprites = new Dictionary<int, int>();

    public static bool isInfiniteTime = false;

    public static int nextLevel = -1;


    // TROPHY POINTS
    public static int getGoldTrophyPoint()
    {
        return goldTrophyPoint;
    }

    public static int getSilverTrophyPoint()
    {
        return silverTrophyPoint;
    }

    public static int getCopperTrophyPoint()
    {
        return copperTrophyPoint;
    }

    public static void setGoldTrophyPoint(int point)
    {
        goldTrophyPoint = point;
    }

    public static void setSilverTrophyPoint(int point)
    {
        silverTrophyPoint = point;
    }

    public static void setCopperTrophyPoint(int point)
    {
        copperTrophyPoint = point;
    }

    // STATISTICS
    public static void IncrementCustomerServed(int number)
    {
        totalCustomerServed += number;
    }


    public static void IncrementScore(int number)
    {
        totalScore += number;
    }


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

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Level 1")
        {
            countdownTimer = 120;
            goldTrophyPoint = 2400;
            silverTrophyPoint = 1600;
            copperTrophyPoint = 800;
            isInfiniteTime = false;
            nextLevel = 2;
            Debug.Log("[SetDefault] Level 1 settings applied");
        }
        else if (sceneName == "Level 2")
        {
            countdownTimer = 180;
            goldTrophyPoint = 3000;
            silverTrophyPoint = 2000;
            copperTrophyPoint = 1000;
            isInfiniteTime = false;
            nextLevel = 3;

            Debug.Log("[SetDefault] Level 2 settings applied");
        }
        else if (sceneName == "Level 3")
        {
            countdownTimer = 240;
            goldTrophyPoint = 5000;
            silverTrophyPoint = 3500;
            copperTrophyPoint = 2000;
            isInfiniteTime = false;
            nextLevel = -1;
            Debug.Log("[SetDefault] Level 3 settings applied");
        }
        else
        {
            countdownTimer = 0;
            goldTrophyPoint = 3000;
            silverTrophyPoint = 2000;
            copperTrophyPoint = 1000;
            isInfiniteTime = true;
            nextLevel = -1;
            Debug.Log("[SetDefault] Default settings applied");
        }
        totalCustomerServed = 0;
        totalScore = 0;

        readyTables = new HashSet<int>();
        readyOrders = new HashSet<int>();
        tablesAwaitingOrder = new HashSet<int>();
        ordersInProgress = new HashSet<int>();

        tableFoodSprites = new Dictionary<int, int>();
    }



    public static IEnumerable<int> GetReadyOrders()
    {
        return readyOrders;
    }
    public static void MarkOrderReady(int tableID)
    {
        if (!readyOrders.Contains(tableID))
            readyOrders.Add(tableID);
    }

    public static bool IsOrderReady(int tableID)
    {
        return readyOrders.Contains(tableID);
    }

    public static void ClearOrder(int tableID)
    {
        readyOrders.Remove(tableID);
    }


    public static void MarkTableWantsToOrder(int tableID)
    {
        tablesAwaitingOrder.Add(tableID);
    }

    public static bool TableWantsToOrder(int tableID)
    {
        return tablesAwaitingOrder.Contains(tableID);
    }

    public static void SubmitOrderToKitchen(int tableID)
    {
        if (tablesAwaitingOrder.Contains(tableID))
        {
            tablesAwaitingOrder.Remove(tableID);
            ordersInProgress.Add(tableID);
        }
    }

    public static bool IsOrderInProgress(int tableID)
    {
        return ordersInProgress.Contains(tableID);
    }

    // Kitchen
    public static void SetTableFoodSprite(int tableID, int spriteID)
    {
        tableFoodSprites[tableID] = spriteID;
    }

    public static int GetTableFoodSprite(int tableID)
    {
        return tableFoodSprites.ContainsKey(tableID) ? tableFoodSprites[tableID] : -1;
    }

    public static void ClearTableFoodSprite(int tableID)
    {
        tableFoodSprites.Remove(tableID);
    }
}