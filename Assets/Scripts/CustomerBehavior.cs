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

    public enum FoodType
    {
        Sketti,
        Tendies
    }

    public CustomerState state;
    public int partyID;
    public int seatedTableID = -1;

    public CustomerSpawner spawner;
    private float menuReadingTime;  // Time spent reading menu

    // Leader variables
    private float eatingTimeOriginal;
    private float eatingTime;
    private bool isPartyLeader = false;  // Only the leader sets the timer
    private GameObject storedFoodObject = null;
    private SpriteRenderer storedFoodSprite = null;
    private FoodType foodType = FoodType.Sketti;
    private TableZone assignedTable = null;

    // Penalty flags
    private bool orderingImpatient = false, orderingAngry = false;
    private bool foodWaitingImpatient = false, foodWaitingAngry = false;
    private bool dismissImpatient = false, dismissAngry = false;
    private int penaltyPoint = 0;

    private TrailFollower trailFollower;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    [Header("Dependencies")]
    [SerializeField] private FollowerTrail followerTrail;

    [Header("Character Variables")]
    [SerializeField] private float idleImpatienceTimer = 30f;
    [SerializeField] private float orderingImpatienceTimer = 30f;
    [SerializeField] private float foodImpatienceTimer = 35f;
    [SerializeField] private float dismissImpatienceTimer = 30f;
    [SerializeField] private int score = 100; // Max score for serving the filly
    [SerializeField] private int bonusScore = 10; // Score the player gets for perfectly serve the filly
    [SerializeField] private int quitPenaltyScore = -10; // Penalty for neglecting customer

    [Header("Audio")]
    [SerializeField] private AudioClip audioComplete;
    [SerializeField] private AudioClip audioFail;
    [SerializeField] private AudioClip audioRee;

    private AudioController AudioController; // Can't apply as a field because it is inside a prefab

    // put these in var so we don't recalc every time, optimization
    private float idleImpatienceTime, idleAngryTime;
    private float orderingImpatientTime, orderingAngryTime; // Timer for hoof-raised ordering
    private float foodImpatientTime, foodAngryTime; // Timer for waiting for food
    private float dismissImpatientTime, dismissAngryTime; // Timer for waiting for food

    private readonly int animMoveLeft = Animator.StringToHash("Anim_character_move_left");
    private readonly int animIdleLeft = Animator.StringToHash("Anim_character_idle_left");
    private readonly int animIdleImpatientLeft = Animator.StringToHash("Anim_character_idle_impatient_left");
    private readonly int animIdleAngryLeft = Animator.StringToHash("Anim_character_idle_angry_left");

    private readonly int animEatingLeft = Animator.StringToHash("Anim_character_eating_left");
    private readonly int animMenuLeft = Animator.StringToHash("Anim_character_menu_left");

    private readonly int animOrderingLeft = Animator.StringToHash("Anim_character_ordering_left");
    private readonly int animOrderingImpatientLeft = Animator.StringToHash("Anim_character_ordering_impatient_left");
    private readonly int animOrderingAngryLeft = Animator.StringToHash("Anim_character_ordering_angry_left");

    private readonly int animWaitingLeft = Animator.StringToHash("Anim_character_waiting_left");
    private readonly int animWaitingImpatientLeft = Animator.StringToHash("Anim_character_waiting_impatient_left");
    private readonly int animWaitingAngryLeft = Animator.StringToHash("Anim_character_waiting_angry_left");

    [Header("Food Sprites")]
    public Sprite[] foodSprites;

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
        AudioController = Object.FindFirstObjectByType<AudioController>();

        // Add 2 seconds to food impatience timer at the start
        foodImpatienceTimer += 2f;
        foodImpatientTime = foodImpatienceTimer * 0.66f;
        foodAngryTime = foodImpatienceTimer * 0.33f;

        idleImpatienceTime = idleImpatienceTimer * 0.66f;
        idleAngryTime = idleImpatienceTimer * 0.33f;

        orderingImpatientTime = orderingImpatienceTimer * 0.66f;
        orderingAngryTime = orderingImpatienceTimer * 0.33f;

        dismissImpatientTime = dismissImpatienceTimer * 0.66f;
        dismissAngryTime = dismissImpatienceTimer * 0.33f;
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
        CustomerBehavior leader = GetPartyLeader();
        if (leader == null) return;

        // Only the party leader should submit the order to the kitchen
        if (leader == this)
        {
            int tableID = assignedTable.GetTableID();
            if (tableID == leader.seatedTableID) // Just making sure it's the right ID
            {
                Debug.Log($"[Player] Took receipt from Table {tableID}, submitting to kitchen...");

                GameStateManager.SubmitOrderToKitchen(tableID);
                Kitchen.Instance.ReceiveOrder(tableID, GetCustomerPartyAtTable(tableID));

                assignedTable.createTableTag();
            }
        }

        // Scale impatience timer based on party size
        var party = GetCustomerPartyAtTable(seatedTableID);
        int partySize = party.members.Count;
        foodImpatienceTimer = 35f + 2f * (partySize - 1) + 2f; // base 35s + 2s per extra filly + 2s buffer
        foodImpatientTime = foodImpatienceTimer * 0.66f;
        foodAngryTime = foodImpatienceTimer * 0.33f;
        Debug.Log($"[Impatience] Table {seatedTableID} party size {partySize}, foodImpatienceTimer set to {foodImpatienceTimer}s");

        // All customers in the party should change their state to waitingFood
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

    public void DisableTrailFollowing()
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

        // Handle eating timer
        if (state == CustomerState.eating)
        {
            CustomerBehavior leader = GetPartyLeader();

            if (leader != null)
            {
                // If I'm the leader, countdown my own timer
                if (leader == this)
                {
                    eatingTime -= Time.deltaTime;

                    // Change the food sprite to reflect my stage
                    if (eatingTime <= eatingTimeOriginal * 0.66)
                    {
                        // Half eaten
                        if(foodType == FoodType.Tendies)
                            storedFoodSprite.sprite = foodSprites[5];
                        else
                            storedFoodSprite.sprite = foodSprites[1];
                    }
                    if (eatingTime <= eatingTimeOriginal * 0.33)
                    {
                        // Mostly eaten (overwrites previous)
                        if(foodType == FoodType.Tendies)
                            storedFoodSprite.sprite = foodSprites[6];
                        else
                            storedFoodSprite.sprite = foodSprites[2];
                    }
                    if (eatingTime <= 0)
                    {
                        // Completely eaten (overwrites previous)
                        if(foodType == FoodType.Tendies)
                            storedFoodSprite.sprite = foodSprites[7];
                        else
                            storedFoodSprite.sprite = foodSprites[3];
                        toDismiss();
                    }
                }
                // If I'm not the leader, check if the leader is done
                else if (leader.state == CustomerState.finished)
                {
                   toDismiss();
                }
            }
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

        // PENALTY SYSTEM //

        // FILLIES ARE WAITING TO BE GREETED AT THE FRONT
        if (state == CustomerState.waiting)
        {
            idleImpatienceTimer -= Time.deltaTime;

            if (idleImpatienceTimer <= 0)
            {
                // Make the filly disappear lol!!!
                Exit();
            }
            else if (idleImpatienceTimer <= idleAngryTime)
            {
                // No penalties for in line?
                animator.CrossFade(animIdleAngryLeft, animCrossFade);
            }
            else if(idleImpatienceTimer <= idleImpatienceTime)
            {
                // No penalties for in line?
                animator.CrossFade(animIdleImpatientLeft, animCrossFade);
            }
        }

        // FILLIES ARE HOLDING THEIR HOOVES UP TO GRAB YOUR ATTENTION
        if (state == CustomerState.ordering)
        {
            orderingImpatienceTimer -= Time.deltaTime;

            if (orderingImpatienceTimer <= 0)
            {
                // Make the filly disappear lol!!!
                customerQuitting();
            }
            else if (orderingImpatienceTimer <= orderingAngryTime)
            {
                if(!orderingAngry)
                {
                    orderingAngry = true;
                    penaltyPoint += 10;
                }
                animator.CrossFade(animOrderingAngryLeft, animCrossFade);
            }
            else if(orderingImpatienceTimer <= orderingImpatientTime)
            {
                if(!orderingImpatient)
                {
                    orderingImpatient = true;
                    penaltyPoint += 10;
                }
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
                customerQuitting();
            }
            else if (foodImpatienceTimer <= foodAngryTime)
            {
                if(!foodWaitingAngry)
                {
                    foodWaitingAngry = true;
                    penaltyPoint += 10;
                }
                animator.CrossFade(animWaitingAngryLeft, animCrossFade);
            }
            else if(foodImpatienceTimer <= foodImpatientTime)
            {
                if(!foodWaitingImpatient)
                {
                    foodWaitingImpatient = true;
                    penaltyPoint += 10;
                }
                animator.CrossFade(animWaitingImpatientLeft, animCrossFade);
            }
        }

        // FILLIES ARE WAITING TO BE DISMISSED
        if (state == CustomerState.finished)
        {
            dismissImpatienceTimer -= Time.deltaTime;

            if (dismissImpatienceTimer <= 0) // Filly still give you point, but with penalty because you have served them
            {
                penaltyPoint += 10;
                toDismiss();
            }
            else if (dismissImpatienceTimer <= dismissAngryTime)
            {
                if(!dismissAngry)
                {
                    dismissAngry = true;
                    penaltyPoint += 10;
                }
                animator.CrossFade(animOrderingAngryLeft, animCrossFade);
            }
            else if(dismissImpatienceTimer <= dismissImpatientTime)
            {
                if(!dismissImpatient)
                {
                    dismissImpatient = true;
                    penaltyPoint += 10;
                }
                animator.CrossFade(animOrderingImpatientLeft, animCrossFade);
            }
        }
    }

    void toReadingMenu()
    {
        state = CustomerState.readingMenu;

        CustomerBehavior leader = GetPartyLeader();

        // Only the party leader have the table zone
        if (leader == this)
        {
            TableZone[] allTables = FindObjectsByType<TableZone>(FindObjectsSortMode.None);
            foreach (var table in allTables)
            {
                int tableID = table.GetTableID();
                if (tableID == leader.seatedTableID)
                {
                    assignedTable = table;
                    break; // Found the table, no need to continue searching
                }
            }
        }

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
            // The party is ready to order, notify game state
            if (seatedTableID != -1)
            {
                GameStateManager.MarkTableWantsToOrder(seatedTableID);
                //Debug.Log($"[CustomerBehavior] Party leader at table {seatedTableID} marked as wanting to order.");
            }
        }

        //Debug.Log($"[customer] Party {partyID} leader finished reading menu, ready to order!");
    }

    void Update() { }

    public void ReceiveFood()
    {
        state = CustomerState.eating;

        CustomerBehavior leader = GetPartyLeader();

        // only the party leader sets the timer
        if (leader == this)
        {
            assignedTable.deleteTableTag();
            isPartyLeader = true;
            eatingTime = Random.Range(10f, 21f); // 10 to 20 seconds (inclusive)
            eatingTimeOriginal = eatingTime;
        }
        else
        {
            isPartyLeader = false;
        }
        animator.CrossFade(animEatingLeft, animCrossFade);
    }

    public void toDismiss()
    {
        state = CustomerState.finished;
        animator.CrossFade(animOrderingLeft, animCrossFade);
    }

    private bool isPerfectService()
    {
        if (orderingImpatient || orderingAngry || foodWaitingImpatient || foodWaitingAngry || dismissImpatient || dismissAngry)
            return false;
        return true;
    }

    public void dismissCustomer()
    {
        //int scoreAccumulated = score - penaltyPoint; // Only one bonus score should be awarded per party?
        bool isPerfect = isPerfectService();
        int scoreAccumulated = score - penaltyPoint + (isPerfect ?  bonusScore : 0); // Base score - penalties + bonus

        if (isPerfect && isPartyLeader && AudioController != null) // Only party leader to prevent multiple audio playing
        {
            AudioController.playFX(audioComplete);
        }

        GameStateManager.IncrementScore(scoreAccumulated);
        GameStateManager.IncrementCustomerServed(1);

        /*
        if (isPartyLeader) // Only one bonus score should be awarded per party?
        {
            GameStateManager.IncrementScore(isPerfectService ?  bonusScore : 0)
        }
        */
        Exit();
    }

    public void customerQuitting()
    {
        // Call other stuffs like "Subtracting score"?
        GameStateManager.IncrementScore(quitPenaltyScore);
        Exit();
    }

    public void customerLeaving()
    {
        Exit();
    }

    public void Exit()
    {
        if (spawner != null)
        {
            spawner.RemoveFromQueue(this.gameObject);
        }
        if (assignedTable != null)
        {
            assignedTable.deleteTableTag();
            if (isPartyLeader)
            {
                assignedTable.UnlockTable();
                Debug.Log($"[SeatingSystem] Table {assignedTable.GetTableID()} unlocked by party leader {partyID}.");
            }
        }
        if (isPartyLeader && seatedTableID > 0)
        {
            Kitchen.Instance.DestroyFoodForTable(seatedTableID);
            Debug.Log($"[CustomerBehavior] destroyed food for Table {seatedTableID} because party left.");
        }
        CleanupStoredFood();
        Destroy(this.gameObject);
    }

    public static CustomerBehavior.CustomerParty GetCustomerPartyAtTable(int tableID)
    {
        CustomerBehavior.CustomerParty party = new CustomerBehavior.CustomerParty();
        party.partyID = -1;

        CustomerBehavior[] allCustomers = FindObjectsByType<CustomerBehavior>(FindObjectsSortMode.None);
        List<int> foundPartyIDs = new List<int>();
        foreach (var customer in allCustomers)
        {
            if (customer.seatedTableID == tableID)
            {
                party.members.Add(customer);
                if (!foundPartyIDs.Contains(customer.partyID))
                    foundPartyIDs.Add(customer.partyID);
            }
        }

        if (party.members.Count > 0)
        {
            // Choose the lowest partyID as the unified partyID
            int unifiedPartyID = foundPartyIDs.Count > 0 ? Mathf.Min(foundPartyIDs.ToArray()) : -1;
            party.partyID = unifiedPartyID;
            // Update all customers at the table to have the unified partyID
            foreach (var member in party.members)
            {
                member.partyID = unifiedPartyID;
            }
        }

        return party;
    }

    // FOOD STORAGE
    public void StoreFoodObject(GameObject foodObject)
    {
        if(isPartyLeader)
        {
            storedFoodObject = foodObject;
            storedFoodSprite = storedFoodObject.GetComponent<SpriteRenderer>();

            int rand = Random.Range(0, 1);

            string spriteName = storedFoodSprite.sprite.name;
            if(rand == 0)
            {
                storedFoodSprite.sprite = foodSprites[4];
                foodType = FoodType.Tendies;
            }
            else
            {
                storedFoodSprite.sprite = foodSprites[0];
                foodType = FoodType.Sketti;
            }
            Debug.Log($"[Customer] Stored food object for customer at table {seatedTableID}");
        }
    }

    private void CleanupStoredFood()
    {
        if (storedFoodObject != null && isPartyLeader)
        {
            Debug.Log($"[Customer] Cleaning up stored food object for table {seatedTableID}");
            Destroy(storedFoodObject);
            storedFoodObject = null;
        }
    }
}