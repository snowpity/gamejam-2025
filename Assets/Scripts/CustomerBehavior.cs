using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerBehavior : MonoBehaviour
{
    int spacingPosition = 10;
    private float animCrossFade = 0;

    public enum CustomerState
    {
        waiting,
        inLine,
        following,
        seated,
        readingMenu,
        ordering,
        eating,
        finished
    }

    public CustomerState state;
    public int partyID;

    private CustomerSpawner spawner;
    private float menuReadingTime;  // Time spent reading menu
    private bool isPartyLeader = false;  // Only the leader sets the timer

    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float stoppingDistance = 0.2f;

    private TrailFollower trailFollower;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    [SerializeField] private FollowerTrail followerTrail;

    private readonly int animMoveLeft = Animator.StringToHash("Anim_character_move_left");
    private readonly int animIdleLeft = Animator.StringToHash("Anim_character_idle_left");
    private readonly int animEatingLeft = Animator.StringToHash("Anim_character_eating_left");
    private readonly int animMenuLeft = Animator.StringToHash("Anim_character_menu_left");
    private readonly int animOrderingLeft = Animator.StringToHash("Anim_character_ordering_left");
    private readonly int animOrderingImpatientLeft = Animator.StringToHash("Anim_character_ordering_impatient_left");
    private readonly int animOrderingAngryLeft = Animator.StringToHash("Anim_character_ordering_angry_left");

    private void FindFollowerTrail()
    {
        if (followerTrail == null)
        {
            GameObject playerObject = GameObject.Find("Player");
            if (playerObject != null)
            {
                followerTrail = playerObject.GetComponent<FollowerTrail>();
            }
        }
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    public void Initialize(CustomerSpawner sourceSpawner, Vector3 startPos)
    {
        spawner = sourceSpawner;
        transform.position = startPos;
        state = CustomerState.waiting;
        FindFollowerTrail();
    }

    public void StartFollowing()
    {
        GameStateManager.SetFollowingParty(true);
        EnableTrailFollowing();
        state = CustomerState.following;
        Debug.Log("[customer] now following");
    }

    public void SitDownAt(Transform seat)
    {
        if (trailFollower != null)
        {
            Destroy(trailFollower);
            trailFollower = null;
        }

        transform.position = seat.position;
        state = CustomerState.seated;
        Debug.Log($"[customer] now seated at {seat.name}");
    }

    public class CustomerParty
    {
        public int partyID;
        public List<CustomerBehavior> members = new List<CustomerBehavior>();
    }

    private void EnableTrailFollowing()
    {
        trailFollower = GetComponent<TrailFollower>();

        if (trailFollower == null)
        {
            trailFollower = gameObject.AddComponent<TrailFollower>();
        }

        if (followerTrail != null)
        {
            trailFollower.Trail = followerTrail;
            trailFollower.TrailPosition = spacingPosition + (GetPositionInParty() * spacingPosition);
            trailFollower.LerpSpeed = 0.1f;
        }
    }

    private CustomerBehavior GetPartyLeader()
    {
        CustomerBehavior[] allCustomers = FindObjectsOfType<CustomerBehavior>();
        CustomerBehavior leader = null;

        foreach (var customer in allCustomers)
        {
            if (customer.partyID == this.partyID)
            {
                if (leader == null || customer.GetInstanceID() < leader.GetInstanceID())
                {
                    leader = customer;
                }
            }
        }

        return leader;
    }

    private int GetPositionInParty()
    {
        CustomerBehavior[] allCustomers = FindObjectsOfType<CustomerBehavior>();

        var partyMembers = new List<CustomerBehavior>();
        foreach (var customer in allCustomers)
        {
            if (customer.partyID == this.partyID)
            {
                partyMembers.Add(customer);
            }
        }

        partyMembers.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));
        return partyMembers.IndexOf(this);
    }

    private void DisableTrailFollowing()
    {
        if (trailFollower != null)
        {
            Destroy(trailFollower);
            trailFollower = null;
            GameStateManager.SetFollowingParty(false);
        }
    }

    public void SitDown()
    {
        state = CustomerState.seated;
    }

    void FixedUpdate()
    {
        // Switch to reading menu when seated
        if (state == CustomerState.seated)
        {
            toReadingMenu();
        }

        // Handle menu reading timer
        if (state == CustomerState.readingMenu)
        {
            CustomerBehavior leader = GetPartyLeader();

            if (leader != null)
            {
                // If I'm the leader, countdown my own timer
                if (leader == this)
                {
                    menuReadingTime -= Time.deltaTime;

                    if (menuReadingTime <= 0)
                    {
                        toReadyToOrder();
                    }
                }
                // If I'm not the leader, check if the leader is done
                else if (leader.state == CustomerState.ordering)
                {
                    toReadyToOrder();
                }
            }
        }

        if (state == CustomerState.ordering)
        {
            // Handle ordering logic here
        }


    }

    void toReadingMenu()
    {
        state = CustomerState.readingMenu;

        CustomerBehavior leader = GetPartyLeader();

        // Only the party leader sets the timer
        if (leader == this)
        {
            isPartyLeader = true;
            menuReadingTime = Random.Range(10f, 21f); // 10 to 20 seconds (inclusive)
            Debug.Log($"[customer] Party {partyID} leader will read menu for {menuReadingTime:F1} seconds");
        }
        else
        {
            isPartyLeader = false;
            Debug.Log($"[customer] PartyID {partyID} following leader's timing");
        }

        animator.CrossFade(animMenuLeft, animCrossFade);

        if (trailFollower != null)
        {
            Destroy(trailFollower);
            trailFollower = null;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
        }

        GameStateManager.SetFollowingParty(false);

        Debug.Log($"[customer] PartyID {partyID} now reading menu");
    }

    void toReadyToOrder()
    {
        state = CustomerState.ordering;
        animator.CrossFade(animOrderingLeft, animCrossFade);

        if (isPartyLeader)
        {
            Debug.Log($"[customer] Party {partyID} leader finished reading menu, ready to order!");
        }
        else
        {
            Debug.Log($"[customer] PartyID {partyID} following leader, ready to order!");
        }

        // Add any additional logic for when customer is ready to order
        // For example, showing an indicator or notifying the game manager
    }

    void Update() { }

    public void Exit()
    {
        if (spawner != null)
        {
            spawner.RemoveFromQueue(this.gameObject);
        }

        Destroy(this.gameObject);
    }
}