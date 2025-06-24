using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerBehavior : MonoBehaviour
{
    int spacingPosition = 10;

    // these are all the possible states a customer can be in
    public enum CustomerState
    {
        Waiting,    // standing still in line
        inLine,     // technically queued (may not be used)
        following,  // moving behind the player
        seated      // sitting at a table
    }

    // current state of this customer
    public CustomerState state;

    // used to track who this customer is following (optional for follow logic)
    private Transform followTarget;

    // the id for the group this customer is in
    public int partyID;

    // used to reference the spawner that made this customer
    private CustomerSpawner spawner;

    // how fast the customer moves when following
    [SerializeField] private float followSpeed = 5f;

    // how close the customer gets to their target before stopping
    [SerializeField] private float stoppingDistance = 0.2f;

    // Dynamically assign TrailFollower to the fillies and set up their variables.
    private TrailFollower trailFollower;
    [SerializeField] private FollowerTrail followerTrail; // Cannot assign FollowerTrail for objects in prefab, must find another method to get the component.

    private void FindFollowerTrail()
    {
        if (followerTrail == null)
        {
            // Find by GameObject name
            GameObject playerObject = GameObject.Find("Player");
            if (playerObject != null)
            {
                followerTrail = playerObject.GetComponent<FollowerTrail>();
            }
        }
    }

    // called right after the customer is spawned by the spawner
    public void Initialize(CustomerSpawner sourceSpawner, Vector3 startPos)
    {
        spawner = sourceSpawner;
        transform.position = startPos;
        state = CustomerState.Waiting;
        FindFollowerTrail();
    }

    // this makes the customer start following
    public void StartFollowing()
    {
        EnableTrailFollowing();
        state = CustomerState.following;
        Debug.Log("[customer] now following");
    }

    // overload in case player wants to assign a specific follow target
    public void StartFollowing(Transform target)
    {
        followTarget = target;
        StartFollowing();
    }

    // this sets the customer to seated
    public void SitDown()
    {
        state = CustomerState.seated;
        Debug.Log("[customer] now seated");
    }

    public class CustomerParty
    {
        public int partyID;
        public List<CustomerBehavior> members = new List<CustomerBehavior>();
    }

    // Enabling and disabling trail follower script dynamically
    private void EnableTrailFollowing()
    {
        // Check if TrailFollower component already exists
        trailFollower = GetComponent<TrailFollower>();

        if (trailFollower == null)
        {
            // Add the TrailFollower component dynamically
            trailFollower = gameObject.AddComponent<TrailFollower>();
        }

        // Set up the trail follower
        if (followerTrail != null)
        {
            trailFollower.Trail = followerTrail;
            // Set trail position based on party ID or some other logic
            trailFollower.TrailPosition = spacingPosition + (GetPositionInParty() * spacingPosition);


            // Set up the lerp speed so the fillies just don't snap immediately
            trailFollower.LerpSpeed = 0.1f; // default is 0.3f, the smaller, the more time the fillies take to form up a line behind you initially.
        }
    }

    // Get this customer's position within their party (0-based index)
    private int GetPositionInParty()
    {
        // Find all customers with the same party ID
        CustomerBehavior[] allCustomers = FindObjectsOfType<CustomerBehavior>();

        // Create a list of customers in the same party
        var partyMembers = new System.Collections.Generic.List<CustomerBehavior>();
        foreach (var customer in allCustomers)
        {
            if (customer.partyID == this.partyID)
            {
                partyMembers.Add(customer);
            }
        }

        // Sort by some instance ID (I assume the earliest spawn is to the furthest right)
        partyMembers.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));

        // Return this customer's index in the sorted list
        return partyMembers.IndexOf(this);
    }

    private void DisableTrailFollowing()
    {
        if (trailFollower != null)
        {
            Destroy(trailFollower);
            trailFollower = null;
        }
    }


    void Update()
    {
        /*
        // only do follow logic if were in following state and have a target
        if (state == CustomerState.following && followTarget != null)
        {
            float distance = Vector3.Distance(transform.position, followTarget.position);
            if (distance > stoppingDistance)
            {
                // move toward follow target
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    followTarget.position,
                    followSpeed * Time.deltaTime
                );
            }
        }
        */
    }

    // call this when the customer should be removed
    public void Exit()
    {
        if (spawner != null)
        {
            spawner.RemoveFromQueue(this.gameObject);
        }

        Destroy(this.gameObject);
    }
}
