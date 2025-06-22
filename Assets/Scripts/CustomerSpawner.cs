using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("customer settings")]
    public GameObject customerPrefab; // the prefab to spawn (should have customerbehavior on it)
    public int maxCustomers = 10;     // how many total customers can exist at once
    public float spawnInterval = 2f;  // how long to wait before trying to spawn again

    [Header("queue settings")]
    public Transform queueStartPoint; // where the first customer starts
    public float spacing = 1.1f;      // vertical space between each customer in the queue

    [Header("group settings")]
    [SerializeField] private int minPartySize = 2; // minimum group size to spawn
    [SerializeField] private int maxPartySize = 6; // max group size to spawn

    // internal memory
    private List<GameObject> customerQueue = new List<GameObject>(); // actual lineup order
    private HashSet<GameObject> trackedCustomers = new HashSet<GameObject>(); // all spawned, even if not in queue
    private float timer;

    void Start()
    {
        // clear any leftover tracking data
        trackedCustomers.Clear();
        Debug.Log("[spawner] initialized.");
    }

    void Update()
    {
        timer += Time.deltaTime;

        // only try to spawn after timer is up
        if (timer >= spawnInterval)
        {
            // if we still have room to spawn more
            if (trackedCustomers.Count < maxCustomers)
            {
                int slotsLeft = maxCustomers - trackedCustomers.Count;
                Debug.Log($"[spawner] timer hit {timer:F2}, currently tracking {trackedCustomers.Count}, max is {maxCustomers}");
                SpawnCustomerGroup(slotsLeft); // try to spawn a group
            }

            // reset timer either way
            timer = 0f;
        }
    }

    void SpawnCustomerGroup(int availableSlots)
    {
        Debug.Log("[spawner] trying to spawn a new group");

        // roll party size, but make sure it fits within our allowed customer count
        int partySize = Mathf.Min(Random.Range(minPartySize, maxPartySize + 1), availableSlots);
        Debug.Log("[spawner] rolled party size: " + partySize);

        for (int i = 0; i < partySize; i++)
        {
            // position each new customer based on queue position
            Vector3 spawnPos = queueStartPoint.position + Vector3.down * spacing * customerQueue.Count;

            // spawn the prefab at that spot
            GameObject customerGO = Instantiate(customerPrefab, spawnPos, Quaternion.identity);
            Debug.Log("[spawner] spawned: " + customerGO.name);

            // make sure it has the behavior script
            CustomerBehavior behavior = customerGO.GetComponent<CustomerBehavior>();
            if (behavior == null)
            {
                Debug.LogError("[spawner] uh oh, missing CustomerBehavior on: " + customerGO.name);
                continue;
            }

            // tell who spawned it and where it belongs in line
            behavior.Initialize(this, spawnPos);

            // add to both lists, one for lineup, one for tracking total
            customerQueue.Add(customerGO);
            trackedCustomers.Add(customerGO);
        }
    }

    // called when a customer starts following the player or leaves the scene
    public void RemoveFromQueue(GameObject customer)
    {
        if (customerQueue.Contains(customer))
        {
            customerQueue.Remove(customer);
            Debug.Log("[spawner] removed from visible queue.");
        }

        if (trackedCustomers.Contains(customer))
        {
            trackedCustomers.Remove(customer);
            Debug.Log($"[spawner] removed from tracking list. {trackedCustomers.Count} left.");
        }

        // update everyone elses position in line
        RepositionQueue();
    }

    void RepositionQueue()
    {
        // move each customer down to match the new list order
        for (int i = 0; i < customerQueue.Count; i++)
        {
            Vector3 newPos = queueStartPoint.position + Vector3.down * spacing * i;
            customerQueue[i].transform.position = newPos;
        }
    }
}
