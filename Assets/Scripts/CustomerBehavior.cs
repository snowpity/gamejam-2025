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
        waitingFood,
        eating,
        finished
    }

    public CustomerState state;
    public int partyID;

    private CustomerSpawner spawner;
    private float menuReadingTime;  // Time spent reading menu
    private bool isPartyLeader = false;  // Only the leader sets the timer

    private TrailFollower trailFollower;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    [Header("Dependencies")]
    [SerializeField] private FollowerTrail followerTrail;

    [Header("Character Variables")]
    [SerializeField] private float orderingImpatienceTimer = 30f;
    [SerializeField] private float foodImpatienceTimer = 30f;

    // put these in var so we don't recalc every time, optimization
    private float orderingImpatientTime, orderingAngryTime; // Timer for hoof-raised ordering
    private float foodImpatientTime, foodAngryTime; // Timer for waiting for food

    private readonly int animMoveLeft = Animator.StringToHash("Anim_character_move_left");
    private readonly int animIdleLeft = Animator.StringToHash("Anim_character_idle_left");

    private readonly int animEatingLeft = Animator.StringToHash("Anim_character_eating_left");
    private readonly int animMenuLeft = Animator.StringToHash("Anim_character_menu_left");

    private readonly int animOrderingLeft = Animator.StringToHash("Anim_character_ordering_left");
    private readonly int animOrderingImpatientLeft = Animator.StringToHash("Anim_character_ordering_impatient_left");
    private readonly int animOrderingAngryLeft = Animator.StringToHash("Anim_character_ordering_angry_left");

    private readonly int animWaitingLeft = Animator.StringToHash("Anim_character_waiting_left");
    private readonly int animWaitingImpatientLeft = Animator.StringToHash("Anim_character_waiting_impatient_left");
    private readonly int animWaitingAngryLeft = Animator.StringToHash("Anim_character_waiting_angry_left");

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

        orderingImpatientTime = orderingImpatienceTimer * 0.66f;
        orderingAngryTime = orderingImpatienceTimer * 0.33f;

        foodImpatientTime = foodImpatienceTimer * 0.66f;
        foodAngryTime = foodImpatienceTimer * 0.33f;
    }

    public void Initialize(CustomerSpawner sourceSpawner, Vector3 startPos)
    {
        spawner = sourceSpawner;
        transform.position = startPos;
        state = CustomerState.waiting;
        FindFollowerTrail();
    }

    public void startFollowing()
    {
        GameStateManager.SetFollowingParty(true);
        EnableTrailFollowing();
        state = CustomerState.following;
        //Debug.Log("[customer] now following");
    }

    public void startWaitingFood()
    {
        state = CustomerState.waitingFood;
        animator.CrossFade(animWaitingLeft, animCrossFade);
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
        //Debug.Log($"[customer] now seated at {seat.name}");
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
        CustomerBehavior[] allCustomers = FindObjectsByType<CustomerBehavior>(FindObjectsSortMode.None);
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
        CustomerBehavior[] allCustomers = FindObjectsByType<CustomerBehavior>(FindObjectsSortMode.None);

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


        // FILLIES ARE HOLDING THEIR HOOVES UP TO GRAB YOUR ATTENTION
        if (state == CustomerState.ordering)
        {
            orderingImpatienceTimer -= Time.deltaTime;

            if (orderingImpatienceTimer <= 0)
            {
                // Make the filly disappear lol!!!
            }
            else if (orderingImpatienceTimer <= orderingAngryTime)
            {
                animator.CrossFade(animOrderingAngryLeft, animCrossFade);
            }
            else if(orderingImpatienceTimer <= orderingImpatientTime)
            {
                animator.CrossFade(animOrderingImpatientLeft, animCrossFade);
            }
        }

        // FILLIES ARE WAITING FOR THE FOOD THEY ORDERED
        if (state == CustomerState.waitingFood)
        {
            foodImpatienceTimer -= Time.deltaTime;

            if (foodImpatienceTimer <= 0)
            {
                // Make the filly disappear lol!!!
            }
            else if (foodImpatienceTimer <= foodImpatientTime)
            {
                animator.CrossFade(animWaitingAngryLeft, animCrossFade);
            }
            else if(foodImpatienceTimer <= foodAngryTime)
            {
                animator.CrossFade(animWaitingImpatientLeft, animCrossFade);
            }
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
            //Debug.Log($"[customer] Party {partyID} leader will read menu for {menuReadingTime:F1} seconds");
        }
        else
        {
            isPartyLeader = false;
            //Debug.Log($"[customer] PartyID {partyID} following leader's timing");
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
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        GameStateManager.SetFollowingParty(false);

        //Debug.Log($"[customer] PartyID {partyID} now reading menu");
    }

    void toReadyToOrder()
    {
        state = CustomerState.ordering;
        animator.CrossFade(animOrderingLeft, animCrossFade);

        if (isPartyLeader)
        {
            //Debug.Log($"[customer] Party {partyID} leader finished reading menu, ready to order!");
        }
        else
        {
            //Debug.Log($"[customer] PartyID {partyID} following leader, ready to order!");
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