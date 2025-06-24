using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerBehavior : MonoBehaviour
{
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

    // called right after the customer is spawned by the spawner
    public void Initialize(CustomerSpawner sourceSpawner, Vector3 startPos)
    {
        spawner = sourceSpawner;
        transform.position = startPos;
        state = CustomerState.Waiting;
    }

    // this makes the customer start following
    public void StartFollowing()
    {
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

    void Update()
    {
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
