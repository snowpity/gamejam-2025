using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Kitchen : MonoBehaviour
{
    public static Kitchen Instance;

    [SerializeField] private Transform kitchenCounterTransform;
    [SerializeField] public GameObject foodPrefab;
    [SerializeField] private Transform foodSpawnPoint;

    [Header("Soiree")]
    [SerializeField] private GameObject SoireeIdle;
    [SerializeField] private GameObject SoireeActive;

    [Header("Matinee")]
    [SerializeField] private GameObject MatineeIdle;
    [SerializeField] private GameObject MatineeActive;

    [Header("Variables")]
    [SerializeField] private float cookBaseTime = 2f;
    [SerializeField] private float cookPartyMultiplier = 2f;

    [Header("Food Sprites")]
    public Sprite[] foodSprites;

    public int selectedSprite = -1;

    private int nextFoodSlot = 0;
    private const int maxFoodSlots = 4;
    private readonly float slotOffsetX = 1.2f; // tweak this spacing to match the counter

    // Dictionary to track spawned food objects by table ID
    private Dictionary<int, GameObject> spawnedFoodObjects = new Dictionary<int, GameObject>();

    // Track how many orders are currently being prepared
    private int activeOrderCount = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ReceiveOrder(int tableID, CustomerBehavior.CustomerParty party)
    {
        //Debug.Log($"[Kitchen] Received order from Table {tableID}. Preparing food...");

        // Increment active order count and update state
        activeOrderCount++;
        UpdateSoireeState();
        UpdateMatineeState();

        StartCoroutine(PrepareOrderCoroutine(tableID, party));
    }

    private IEnumerator PrepareOrderCoroutine(int tableID, CustomerBehavior.CustomerParty party)
    {
        float prepTime = cookBaseTime + party.members.Count * cookPartyMultiplier; // More customers = longer prep time
        yield return new WaitForSeconds(prepTime);

        Debug.Log($"[Kitchen] Order for Table {tableID} is ready.");
        GameStateManager.MarkOrderReady(tableID);

        // Instantiate the food prefab
        // Calculate spawn position based on slot index
        Vector3 spawnPosition = foodSpawnPoint.position + new Vector3(slotOffsetX * nextFoodSlot, 0, 0);
        GameObject spawnedFood = Instantiate(foodPrefab, spawnPosition, Quaternion.identity);

        // Update slot index
        nextFoodSlot = (nextFoodSlot + 1) % maxFoodSlots;

        SpriteRenderer spriteRenderer = spawnedFood.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = foodSprites[tableID-1];

        // Store the spawned food object so we can destroy it later
        spawnedFoodObjects[tableID] = spawnedFood;
        Debug.Log("SPAWNED:" + spawnedFoodObjects[tableID]);

        // Decrement active order count and update state
        activeOrderCount--;
        UpdateSoireeState();
        UpdateMatineeState();
    }

    // Method to get the spawned food object for a table
    public GameObject GetSpawnedFood(int tableID)
    {
        Debug.Log("GET OBJECT: " + spawnedFoodObjects[tableID]);
        return spawnedFoodObjects.ContainsKey(tableID) ? spawnedFoodObjects[tableID] : null;
    }

    // Method to remove food from tracking without destroying it
    public void RemoveSpawnedFood(int tableID)
    {
        if (spawnedFoodObjects.ContainsKey(tableID))
        {
            spawnedFoodObjects.Remove(tableID);
        }
    }

    private void UpdateSoireeState()
    {
        bool shouldBeActive = activeOrderCount > 0;
        SetSoireeState(shouldBeActive);
    }

    private void SetSoireeState(bool isCooking)
    {
        if (SoireeIdle == null || SoireeActive == null) return;
        SoireeIdle.SetActive(!isCooking);
        SoireeActive.SetActive(isCooking);
    }

    private void UpdateMatineeState()
    {
        bool shouldBeActive = activeOrderCount > 0;
        SetMatineeState(shouldBeActive);
    }

    private void SetMatineeState(bool isCooking)
    {
        if (MatineeIdle == null || SoireeActive == null) return;
        MatineeIdle.SetActive(!isCooking);
        MatineeActive.SetActive(isCooking);
    }

    public void DestroyFoodForTable(int tableID)
    {
        if (spawnedFoodObjects.ContainsKey(tableID))
        {
            GameObject food = spawnedFoodObjects[tableID];
            if (food != null)
            {
                Destroy(food);
            }
            spawnedFoodObjects.Remove(tableID);
            Debug.Log($"[Kitchen] destroyed food for Table {tableID} because the party left.");
        }
        // also destroy food held by the player for this table
        var player = GameObject.FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            var heldFoodTableIDField = player.GetType().GetField("heldFoodTableID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var heldFoodObjectField = player.GetType().GetField("heldFoodObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isHoldingFoodField = player.GetType().GetField("isHoldingFood", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (heldFoodTableIDField != null && heldFoodObjectField != null && isHoldingFoodField != null)
            {
                int heldTableID = (int)heldFoodTableIDField.GetValue(player);
                GameObject heldObj = (GameObject)heldFoodObjectField.GetValue(player);
                if (heldTableID == tableID && heldObj != null)
                {
                    Object.Destroy(heldObj);
                    heldFoodObjectField.SetValue(player, null);
                    heldFoodTableIDField.SetValue(player, -1);
                    isHoldingFoodField.SetValue(player, false);
                    Debug.Log($"[Kitchen] destroyed food held by player for Table {tableID} and reset isHoldingFood.");
                }
            }
        }
    }
}