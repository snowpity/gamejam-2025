using UnityEngine;

public class KitchenPickupZone : MonoBehaviour
{
    public static KitchenPickupZone Instance;



    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public int GetReadyOrderInRange(Vector3 playerPos, float radius)
    {
        float closestDistance = float.MaxValue;
        int closestReadyTable = -1;

        foreach (int tableID in GameStateManager.GetReadyOrders())
        {
            float dist = Vector3.Distance(playerPos, transform.position);
            if (dist < radius && dist < closestDistance)
            {
                closestDistance = dist;
                closestReadyTable = tableID;
            }
        }

        return closestReadyTable;
    }
}
