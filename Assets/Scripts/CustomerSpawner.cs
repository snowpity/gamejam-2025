using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("customer settings")]
    public GameObject customerPrefab; // the prefab to spawn (should have CustomerBehavior on it)
    public int maxCustomers = 10;     // how many total customers can exist at once
    public float spawnInterval = 2f;  // how long to wait before trying to spawn again

    [Header("queue settings")]
    public Transform queueStartPoint; // where the first customer starts
    public float spacing = 1.1f;      // vertical space between each full group in the queue

    [Header("group settings")]
    [SerializeField] private int minPartySize = 2; // minimum group size to spawn
    [SerializeField] private int maxPartySize = 6; // max group size to spawn

    // internal memory
    private List<GameObject> customerQueue = new List<GameObject>(); // actual lineup order
    private HashSet<GameObject> trackedCustomers = new HashSet<GameObject>(); // all spawned, even if not in queue
    private float timer;
    private int partySpawnCount = 0; // how many groups we've spawned so far

    void Start()
    {
        trackedCustomers.Clear();
        Debug.Log("[spawner] initialized.");
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            if (trackedCustomers.Count < maxCustomers)
            {
                int slotsLeft = maxCustomers - trackedCustomers.Count;
                Debug.Log($"[spawner] timer hit {timer:F2}, currently tracking {trackedCustomers.Count}, max is {maxCustomers}");
                SpawnCustomerGroup(slotsLeft);
            }

            timer = 0f;
        }
    }

    void SpawnCustomerGroup(int availableSlots)
    {
        Debug.Log("[spawner] trying to spawn a new group");

        int partySize = Mathf.Min(Random.Range(minPartySize, maxPartySize + 1), availableSlots);
        Debug.Log("[spawner] rolled party size: " + partySize);

        int newPartyID = Random.Range(1000, 9999);
        List<CustomerBehavior> partyMembers = new List<CustomerBehavior>();

        float verticalOffset = spacing * partySpawnCount;
        partySpawnCount++; // increment for next group

        float horizontalSpacing = 0.5f;

        for (int i = 0; i < partySize; i++)
        {
            Vector3 spawnPos = queueStartPoint.position
                             + Vector3.down * verticalOffset
                             + Vector3.right * (i * horizontalSpacing);

            GameObject customerGO = Instantiate(customerPrefab, spawnPos, Quaternion.identity);
            CustomerBehavior behavior = customerGO.GetComponent<CustomerBehavior>();

            if (behavior == null)
            {
                Debug.LogError("[spawner] missing CustomerBehavior on: " + customerGO.name);
                continue;
            }

            behavior.Initialize(this, spawnPos);
            behavior.partyID = newPartyID;
            partyMembers.Add(behavior);

            customerQueue.Add(customerGO);
            trackedCustomers.Add(customerGO);

            SpriteRenderer sprite = customerGO.GetComponent<SpriteRenderer>();
            if (sprite != null)
                sprite.flipX = false;

            Animator anim = customerGO.GetComponent<Animator>();
            if (anim != null)
                anim.Play("Anim_character_idle_right");
        }
    }

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

        RepositionQueue();
    }

    void RepositionQueue()
    {
        for (int i = 0; i < customerQueue.Count; i++)
        {
            Vector3 newPos = queueStartPoint.position + Vector3.down * spacing * i;
            customerQueue[i].transform.position = newPos;
        }
    }
}
