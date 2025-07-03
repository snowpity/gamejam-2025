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
    public float horizontalSpacing = 0.5f; // Horizontal spacing between each filly in a group
    public int[] partySizes;

    // internal memory
    private List<GameObject> customerQueue = new List<GameObject>(); // actual lineup order
    private List<int> partyQueue = new List<int>(); // List of queued parties
    private HashSet<GameObject> trackedCustomers = new HashSet<GameObject>(); // all spawned, even if not in queue
    private float timer;
    private int partySpawnCount = 0; // how many groups we've spawned so far

    private readonly int animIdleLeft = Animator.StringToHash("Anim_character_idle_left");

    [Header("Audio")]
    [SerializeField] AudioController AudioController;
    [SerializeField] private AudioClip bellSound;

    [Header("Spawning Randomness")]
    public float minSpawnInterval = 1.5f;
    public float maxSpawnInterval = 4f;
    public float rushHourMinInterval = 0.5f;
    public float rushHourMaxInterval = 1.2f;
    private float nextSpawnTime = 0f;
    private float rushHourTimer = 0f;
    private float nextRushHourCheck = 0f;
    private bool isRushHour = false;

    void Start()
    {
        trackedCustomers.Clear();
        nextSpawnTime = Random.Range(minSpawnInterval, maxSpawnInterval);
        nextRushHourCheck = Time.time + Random.Range(30f, 60f);
        //Debug.Log("[spawner] initialized.");
    }

    void Update()
    {
        if (GameStateManager.IsPaused || !GameStateManager.IsGameStarted) return;
        timer += Time.deltaTime;

        // Rush hour logic
        if (!isRushHour && Time.time >= nextRushHourCheck)
        {
            if (Random.value < 0.5f) // 50% chance to trigger rush hour
            {
                isRushHour = true;
                rushHourTimer = Random.Range(10f, 15f);
                Debug.Log("[RushHour] Rush hour started!");
            }
            nextRushHourCheck = Time.time + Random.Range(30f, 40f);
        }

        if (isRushHour)
        {
            rushHourTimer -= Time.deltaTime;
            if (rushHourTimer <= 0f)
            {
                isRushHour = false;
                Debug.Log("[RushHour] Rush hour ended.");
            }
        }

        if (timer >= nextSpawnTime)
        {
            if (trackedCustomers.Count < maxCustomers)
            {
                int slotsLeft = maxCustomers - trackedCustomers.Count;
                SpawnCustomerGroup(slotsLeft);
            }
            timer = 0f;
            if (isRushHour)
            {
                nextSpawnTime = Random.Range(rushHourMinInterval, rushHourMaxInterval);
            }
            else
            {
                nextSpawnTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            }
            Debug.Log($"[Spawner] Next spawn in {nextSpawnTime:F2} seconds. Rush hour: {isRushHour}");
        }
    }

    void SpawnCustomerGroup(int availableSlots)
    {
        //Debug.Log("[spawner] trying to spawn a new group");

        int partySize = partySizes[Random.Range(0, partySizes.Length)];
        //Debug.Log("[spawner] rolled party size: " + partySize);

        int newPartyID = Random.Range(1000, 9999);
        List<CustomerBehavior> partyMembers = new List<CustomerBehavior>();

        float verticalOffset = spacing * partyQueue.Count;

        partyQueue.Add(newPartyID);

        for (int i = 0; i < partySize; i++)
        {
            Vector3 spawnPos = queueStartPoint.position
                             + Vector3.down * verticalOffset
                             + Vector3.right * (i * horizontalSpacing);

            GameObject customerGO = Instantiate(customerPrefab, spawnPos, Quaternion.identity);
            CustomerBehavior behavior = customerGO.GetComponent<CustomerBehavior>();

            if (behavior == null)
            {
                //Debug.LogError("[spawner] missing CustomerBehavior on: " + customerGO.name);
                continue;
            }

            behavior.Initialize(this, spawnPos);
            behavior.partyID = newPartyID;
            partyMembers.Add(behavior);

            customerQueue.Add(customerGO);
            trackedCustomers.Add(customerGO);

            SpriteRenderer sprite = customerGO.GetComponent<SpriteRenderer>();

            Animator anim = customerGO.GetComponent<Animator>();
            if (anim != null)
                anim.CrossFade(animIdleLeft, 0);
        }

        AudioController.playFX(bellSound);
        UpdateQueuePositions();
    }

    private void UpdateQueuePositions()
    {
        for (int i = 0; i < partyQueue.Count; i++)
        {
            int partyID = partyQueue[i];
            List<GameObject> partyMembers = customerQueue.FindAll(
                customer => customer.GetComponent<CustomerBehavior>().partyID == partyID
            );
            for (int j = 0; j < partyMembers.Count; j++)
            {
                partyMembers[j].transform.position =
                    queueStartPoint.position
                    + Vector3.down * (i * spacing)
                    + Vector3.right * (j * horizontalSpacing);
            }
        }
    }

    public void RemoveTrackedParty(int partyID)
    {
        if (partyQueue.Remove(partyID))
        {
            UpdateQueuePositions();
        }
    }

    public void RemoveFromQueue(GameObject customer)
    {
        if (customerQueue.Contains(customer))
        {
            customerQueue.Remove(customer);
            //Debug.Log("[spawner] removed from visible queue.");
        }

        if (trackedCustomers.Contains(customer))
        {
            trackedCustomers.Remove(customer);
            //Debug.Log($"[spawner] removed from tracking list. {trackedCustomers.Count} left.");
        }

        CustomerBehavior behavior = customer.GetComponent<CustomerBehavior>();
        RemoveTrackedParty(behavior.partyID);
    }

    public void RequeueParty(List<CustomerBehavior> partyMembers, int partyID)
    {
        // Remove the party from queue first if it exists to avoid duplicates
        if (partyQueue.Contains(partyID))
        {
            partyQueue.Remove(partyID);
        }

        // Remove party members from customerQueue to avoid duplicates
        foreach (var member in partyMembers)
        {
            if (customerQueue.Contains(member.gameObject))
            {
                customerQueue.Remove(member.gameObject);
            }
        }

        float verticalOffset = spacing * partyQueue.Count;
        partyQueue.Add(partyID);
        UpdateQueuePositions();

        for (int i = 0; i < partyMembers.Count; i++)
        {
            var customer = partyMembers[i];
            Vector3 targetPos = queueStartPoint.position
                              + Vector3.down * verticalOffset
                              + Vector3.right * (i * horizontalSpacing);

            customer.transform.position = targetPos;
            customer.transform.SetParent(null); // detach if they were seated
            customer.state = CustomerBehavior.CustomerState.inLine;
            customer.Initialize(this, targetPos);

            Rigidbody2D rb = customer.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            var trail = customer.GetComponent<TrailFollower>();
            if (trail != null)
            {
                Destroy(trail);
            }

            trackedCustomers.Add(customer.gameObject);
            Animator anim = customer.GetComponent<Animator>();
            if (anim != null)
                anim.Play("Anim_character_idle_left");
            SpriteRenderer spriteRenderer = customer.GetComponent<SpriteRenderer>();
            spriteRenderer.flipX = true;
            customerQueue.Add(customer.gameObject);
        }
    }


    public Vector3 GetLinePosition(CustomerBehavior customer)
    {
        // and each customer has an index or is tracked by party ID
        return transform.position + Vector3.down * 0.5f; // temp placeholder
    }
}
