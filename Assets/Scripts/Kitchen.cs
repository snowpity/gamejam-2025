using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Kitchen : MonoBehaviour
{
    public static Kitchen Instance;

    [SerializeField] private Transform kitchenCounterTransform;
    [SerializeField] public GameObject foodPrefab;
    [SerializeField] private Transform foodSpawnPoint;

    [SerializeField] private GameObject SoireeIdle;
    [SerializeField] private GameObject SoireeActive;

    [Header("Variables")]
    [SerializeField] private float cookBaseTime = 2f;
    [SerializeField] private float cookPartyMultiplier = 2f;

    [Header("Food Sprites")]
    public Sprite[] foodSprites;

    public int selectedSprite = -1;

    private Dictionary<int, GameObject> spawnedFoodObjects = new Dictionary<int, GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ReceiveOrder(int tableID, CustomerBehavior.CustomerParty party)
    {
        Debug.Log($"[Kitchen] Received order from Table {tableID}. Preparing food...");
        SetSoireeState(true);
        StartCoroutine(PrepareOrderCoroutine(tableID, party));
    }

    private IEnumerator PrepareOrderCoroutine(int tableID, CustomerBehavior.CustomerParty party)
    {
        float prepTime = cookBaseTime + party.members.Count * cookPartyMultiplier; // More customers = longer prep time
        yield return new WaitForSeconds(prepTime);

        Debug.Log($"[Kitchen] Order for Table {tableID} is ready.");
        GameStateManager.MarkOrderReady(tableID); // Ensure this method exists

        // Select sprite and store it in GameStateManager
        selectedSprite = Random.Range(0, foodSprites.Length);
        GameStateManager.SetTableFoodSprite(tableID, selectedSprite);

        // Instantiate the food prefab
        GameObject spawnedFood = Instantiate(foodPrefab, foodSpawnPoint.position, Quaternion.identity);

        SpriteRenderer spriteRenderer = spawnedFood.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = foodSprites[selectedSprite];

        // Store the spawned food object so we can destroy it later
        spawnedFoodObjects[tableID] = spawnedFood;

        SetSoireeState(false);
    }

    // Method to destroy the spawned food when picked up
    public void DestroySpawnedFood(int tableID)
    {
        if (spawnedFoodObjects.ContainsKey(tableID))
        {
            if (spawnedFoodObjects[tableID] != null)
            {
                Destroy(spawnedFoodObjects[tableID]);
            }
            spawnedFoodObjects.Remove(tableID);
        }
    }

    private void SetSoireeState(bool isCooking)
    {
        if (SoireeIdle == null || SoireeActive == null) return;
        SoireeIdle.SetActive(!isCooking);
        SoireeActive.SetActive(isCooking);
    }
}
