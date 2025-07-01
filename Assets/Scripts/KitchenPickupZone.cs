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
        // Check if player is within the BoxCollider2D bounds
        Collider2D pickupCollider = GetComponent<Collider2D>();
        if (pickupCollider != null && pickupCollider.bounds.Contains(playerPos))
        {
            // Player is inside the pickup zone, return any ready order
            var readyOrders = GameStateManager.GetReadyOrders();
            foreach (int tableID in readyOrders)
            {
                return tableID; // Return the first ready order found
            }
        }

        return -1; // No ready orders or player not in range
    }
}