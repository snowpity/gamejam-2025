using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Kitchen : MonoBehaviour
{
    public static Kitchen Instance;

    [SerializeField] private Transform kitchenCounterTransform;
    [SerializeField] public GameObject foodPrefab;
    [SerializeField] private Transform foodSpawnPoint;

    [Header("Variables")]
    [SerializeField] private float cookBaseTime = 2f;
    [SerializeField] private float cookPartyMultiplier = 2f;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ReceiveOrder(int tableID, CustomerBehavior.CustomerParty party)
    {
        Debug.Log($"[Kitchen] Received order from Table {tableID}. Preparing food...");

        StartCoroutine(PrepareOrderCoroutine(tableID, party));
    }

    private IEnumerator PrepareOrderCoroutine(int tableID, CustomerBehavior.CustomerParty party)
    {
        float prepTime = cookBaseTime + party.members.Count * cookPartyMultiplier; // More customers = longer prep time
        yield return new WaitForSeconds(prepTime);

        Debug.Log($"[Kitchen] Order for Table {tableID} is ready.");
        GameStateManager.MarkOrderReady(tableID); // Ensure this method exists
        Instantiate(foodPrefab, foodSpawnPoint.position, Quaternion.identity);
    }
}
