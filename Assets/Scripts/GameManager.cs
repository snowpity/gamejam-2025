using UnityEngine;

public class GameManager : MonoBehaviour
{
    void Awake()
    {
        GameStateManager.SetDefault();
    }
} 