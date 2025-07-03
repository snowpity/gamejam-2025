using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using static UnityEngine.Rendering.DebugUI;

[SelectionBase]
public class PlayerController : MonoBehaviour
{
    private enum Directions { UP, DOWN, LEFT, RIGHT }

    private Vector2 movementInput;
    private Directions facingDirection = Directions.LEFT;
    private float animCrossFade = 0;

    [Header("Player Attributes")]
    [SerializeField] private float movementSpeed = 50f;

    [Header("Dependencies")]
    [SerializeField] private InputActionReference movement;
    [SerializeField] private InputActionReference interact;
    [SerializeField] private Rigidbody2D rigidBody;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private FollowerTrail followerTrail;

    [Header("Customer Interaction")]
    [SerializeField] private float interactionRadius = 1.5f;

    [Header("Customer Highlighting")]
    [SerializeField] private float highlightOutlineThickness = 2f / 64f;
    [SerializeField] private float normalOutlineThickness = 0f;

    private readonly int animMoveLeft = Animator.StringToHash("Anim_character_move_left");
    private readonly int animIdleLeft = Animator.StringToHash("Anim_character_idle_left");

    private HashSet<CustomerBehavior> highlightedCustomers = new HashSet<CustomerBehavior>();
    private HashSet<TableZone> highlightedTables = new HashSet<TableZone>();

    public static Dictionary<int, CustomerBehavior.CustomerParty> tableOrders = new();

    // food delivery tracking
    private bool isHoldingFood = false;
    private int heldFoodTableID = -1;
    private GameObject heldFoodObject = null;

    private void OnEnable()
    {
        interact.action.performed += TryInteract;
    }

    private void OnDisable()
    {
        interact.action.performed -= TryInteract;
    }

    private void getInput()
    {
        movementInput = movement.action.ReadValue<Vector2>();
    }

    private void updateMovement()
    {
        rigidBody.linearVelocity = movementInput.normalized * movementSpeed * Time.fixedDeltaTime;
    }

    private void getFacingDirection()
    {
        if (movementInput.y != 0 && Mathf.Abs(movementInput.y) > Mathf.Abs(movementInput.x))
            facingDirection = (movementInput.y > 0) ? Directions.UP : Directions.DOWN;
        else if (movementInput.x != 0)
            facingDirection = (movementInput.x > 0) ? Directions.RIGHT : Directions.LEFT;
    }

    private void updateAnimation()
    {
        spriteRenderer.flipX = (facingDirection == Directions.RIGHT);

        if (movementInput.SqrMagnitude() > 0)
            animator.CrossFade(animMoveLeft, animCrossFade);
        else
            animator.CrossFade(animIdleLeft, animCrossFade);
    }

    private void TryInteract(InputAction.CallbackContext context)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius);

        if (GameStateManager.hasFollowingParty)
        {
            foreach (var hit in hits)
            {
                if (hit.GetComponent<LineZone>() != null)
                {
                    Debug.Log("[Player] Returning following party to line.");

                    // Cancel the following party (teleport them back)
                    CancelFollowingParty();

                    return; // Exit early after sending them back
                }
            }
        }


        // if holding food, check for nearby correct table to deliver
        if (isHoldingFood)
        {
            foreach (var hit in hits)
            {
                TableZone table = hit.GetComponent<TableZone>();
                if (table != null && table.GetTableID() == heldFoodTableID)
                {
                    Debug.Log($"[DeliverySystem] Delivered food to Table {heldFoodTableID}");

                    var party = CustomerBehavior.GetCustomerPartyAtTable(heldFoodTableID);
                    foreach (var member in party.members)
                    {
                        member.StoreFoodObject(heldFoodObject);
                        member.ReceiveFood();
                    }

                    isHoldingFood = false;

                    // Clean up the sprite data for this table
                    GameStateManager.ClearTableFoodSprite(heldFoodTableID);
                    heldFoodTableID = -1;

                    if (heldFoodObject != null)
                    {
                        SendFoodToOrigin(heldFoodObject, table);
                        heldFoodObject = null;
                    }

                    return; // End interaction after delivery
                }
            }
        }

        // prevent picking up if already holding food
        if (!isHoldingFood)
        {
            int readyTableID = KitchenPickupZone.Instance.GetReadyOrderInRange(transform.position, interactionRadius);
            if (readyTableID != -1)
            {
                GameStateManager.ClearOrder(readyTableID);
                Debug.Log($"[PickupSystem] Picked up order for Table {readyTableID}");

                // Store that we're now holding food
                isHoldingFood = true;
                heldFoodTableID = readyTableID;

                // Get the original spawned food object and move it to the player
                GameObject originalFood = Kitchen.Instance.GetSpawnedFood(readyTableID);

                heldFoodObject = originalFood;
                heldFoodObject.transform.position = transform.position + Vector3.up * 0.5f;
                heldFoodObject.transform.SetParent(transform);  // Attach to player

                // remove it from kitchens tracking
                Kitchen.Instance.RemoveSpawnedFood(readyTableID);
            }
        }

        // If has no following party...
        if (!GameStateManager.hasFollowingParty)
        {
            Dictionary<int, List<CustomerBehavior>> partiesInRange = new Dictionary<int, List<CustomerBehavior>>();

            foreach (var hit in hits)
            {
                CustomerBehavior customer = hit.GetComponent<CustomerBehavior>();
                if (isCustomerInteractible(customer))
                {
                    if (!partiesInRange.ContainsKey(customer.partyID))
                        partiesInRange[customer.partyID] = new List<CustomerBehavior>();

                    partiesInRange[customer.partyID].Add(customer);
                }
            }

            int closestPartyID = -1;
            float closestDistance = float.MaxValue;

            foreach (var party in partiesInRange)
            {
                float partyClosestDistance = float.MaxValue;
                foreach (var customer in party.Value)
                {
                    float distance = Vector2.Distance(transform.position, customer.transform.position);
                    if (distance < partyClosestDistance)
                    {
                        partyClosestDistance = distance;
                    }
                }

                if (partyClosestDistance < closestDistance)
                {
                    closestDistance = partyClosestDistance;
                    closestPartyID = party.Key;
                }
            }

            // Interact with the closest party if found
            if (closestPartyID != -1)
            {
                GameObject[] allCustomers = GameObject.FindGameObjectsWithTag("Customer");
                CustomerSpawner spawner = GameObject.FindObjectOfType(typeof(CustomerSpawner)) as CustomerSpawner;
                bool checkToResetTrail = false;

                foreach (var obj in allCustomers)
                {
                    var c = obj.GetComponent<CustomerBehavior>();
                    if (c != null && c.partyID == closestPartyID && isCustomerInteractible(c))
                    {
                        if(c.state == CustomerBehavior.CustomerState.waiting)
                        {
                            spawner.RemoveTrackedParty(c.partyID);
                            c.startFollowing();
                            checkToResetTrail = true;
                        }
                        else if(c.state == CustomerBehavior.CustomerState.ordering)
                        {
                            c.startWaitingFood();
                        }
                        else if(c.state == CustomerBehavior.CustomerState.finished)
                        {
                            c.dismissCustomer();
                        }
                    }
                }
                if (checkToResetTrail)
                {
                    followerTrail.UpdateTrail(allCustomers, closestPartyID);
                }
            }
        }
        else
        {
            // try to seat the party
            List<CustomerBehavior> followingParty = new List<CustomerBehavior>();
            GameObject[] allCustomers = GameObject.FindGameObjectsWithTag("Customer");

            foreach (var obj in allCustomers)
            {
                var c = obj.GetComponent<CustomerBehavior>();
                if (c != null && c.state == CustomerBehavior.CustomerState.following)
                {
                    followingParty.Add(c);
                }
            }
            int partySize = followingParty.Count;

            // Find the closest table with enough seats
            TableZone closestTable = null;
            float closestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                TableZone table = hit.GetComponent<TableZone>();
                if (table == null) continue;

                // Only consider tables that are available for seating
                if (!table.IsAvailableForSeating()) continue;

                // Check if table has enough available seats
                var availableSeats = table.GetSeatPositions().Where(s => s.childCount == 0).ToArray();
                if (availableSeats.Length >= partySize)
                {
                    float distance = Vector2.Distance(transform.position, table.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTable = table;
                    }
                }
            }

            // Seat the party at the closest table if found
            if (closestTable != null)
            {
                // Check if the table is available for seating
                if (!closestTable.IsAvailableForSeating())
                {
                    Debug.Log($"[SeatingSystem] Table {closestTable.GetTableID()} is already occupied by party {closestTable.GetOccupiedPartyID()}. Cannot seat new party.");
                    return;
                }
                // Lock the table for this party
                closestTable.LockTable(followingParty[0].partyID);

                var seats = closestTable.GetSeatPositions().Where(s => s.childCount == 0).ToArray();

                for (int i = 0; i < partySize; i++)
                {
                    CustomerBehavior customer = followingParty[i];
                    Transform seat = seats[i];

                    // Get the sprite renderers
                    SpriteRenderer customerSprite = customer.GetComponent<SpriteRenderer>();
                    SpriteRenderer seatSprite = seat.GetComponent<SpriteRenderer>();

                    Vector3 additiveSpacing;
                    float xShift = 0.13f / 2; // divide 2 because our sprite is scaled down by 0.5
                    float yShift = 1f / 2;
                    if(seatSprite.flipX)
                    {
                        additiveSpacing = new Vector3(xShift,yShift,0);
                    }
                    else
                    {
                        additiveSpacing = new Vector3(-xShift,yShift,0);
                    }
                    customerSprite.flipX = !seatSprite.flipX;

                    customer.transform.position = seat.position + additiveSpacing;
                    customer.transform.SetParent(seat); // optional, keeps them attached
                    customer.seatedTableID = closestTable.tableID;
                    customer.SitDown();
                }
            }
        }
    }





    private void SendFoodToOrigin(GameObject foodObject, TableZone table)
    {
        // Find the FoodOrigin object that belongs to this specific table
        GameObject foodOrigin = FindFoodOriginForTable(table);

        if (foodOrigin != null)
        {
            // Detach from player
            foodObject.transform.SetParent(table.transform);

            // Move to FoodOrigin position
            foodObject.transform.position = foodOrigin.transform.position;

            // Rot, Scale
            foodObject.transform.localScale = Vector3.one;
            foodObject.transform.rotation = Quaternion.Euler(0, 0, 90);

            Debug.Log($"[DeliverySystem] Sent food object to FoodOrigin for Table {table.GetTableID()}");
        }
        else
        {
            Debug.LogWarning($"[DeliverySystem] FoodOrigin not found for Table {table.GetTableID()}, destroying food object");
            Destroy(foodObject);
        }
    }
    private GameObject FindFoodOriginForTable(TableZone table)
    {
        // Look for FoodOrigin as a direct child of the table
        Transform foodOriginTransform = table.transform.Find("FoodOrigin");
        if (foodOriginTransform != null)
        {
            return foodOriginTransform.gameObject;
        }

        Debug.LogWarning($"[PlayerController] Could not find FoodOrigin for Table {table.GetTableID()}. Make sure there's a child GameObject named 'FoodOrigin'");
        return null;
    }

    private void Update()
    {
        if (GameStateManager.IsPaused || !GameStateManager.IsGameStarted)
            return;

        getInput();
        getFacingDirection();
        updateAnimation();
        UpdateHighLight();


        if (heldFoodObject != null)
        {
            heldFoodObject.transform.position = transform.position + Vector3.up * 0.5f;
        }

    }

    private void FixedUpdate()
    {
        updateMovement();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }

    // Highlighting systems
    private void UpdateHighLight()
    {
        // This might be the worst implementation ever, I might have to find a better way to clear highlight for the previous states
        ClearAllCustomerHighlights();
        ClearAllTableHighlights();

        if(!GameStateManager.hasFollowingParty) // Allowing highlight for customer group if nopony's following right now
        {
            UpdateCustomerHighlighting();
        }

        if (true)// Allow the player to highlight the table to seat the fillies // prob gonna have a condition in the future, just set it true for now
        {
            UpdateTableHighlighting();
        }
    }

    private void UpdateTableHighlighting()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius);
        TableZone closestTable = null;
        float closestDistance = float.MaxValue;

        // If holding food, only highlight the correct delivery table
        if (isHoldingFood)
        {
            foreach (var hit in hits)
            {
                TableZone table = hit.GetComponent<TableZone>();
                if (table != null && table.GetTableID() == heldFoodTableID)
                {
                    closestTable = table;
                    break;
                }
            }
        }
        else
        {
            // Get the following party size to check table compatibility
            List<CustomerBehavior> followingParty = new List<CustomerBehavior>();
            GameObject[] allCustomers = GameObject.FindGameObjectsWithTag("Customer");

            foreach (var obj in allCustomers)
            {
                var c = obj.GetComponent<CustomerBehavior>();
                if (c != null && c.state == CustomerBehavior.CustomerState.following)
                {
                    followingParty.Add(c);
                }
            }

            int partySize = followingParty.Count;

            // Early return if no followers - don't highlight any tables
            if (partySize == 0)
            {
                // Clear all current highlights before returning
                foreach (var table in highlightedTables)
                {
                    SetTableOutline(table, normalOutlineThickness);
                }
                highlightedTables.Clear();
                return;
            }

            // Find the closest table with enough available seats
            foreach (var hit in hits)
            {
                TableZone table = hit.GetComponent<TableZone>();
                if (table == null) continue;

                // Only consider tables that are available for seating
                if (!table.IsAvailableForSeating()) continue;

                // Check if table has enough available seats
                var availableSeats = table.GetSeatPositions().Where(s => s.childCount == 0).ToArray();
                if (availableSeats.Length >= partySize)
                {
                    float distance = Vector2.Distance(transform.position, table.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTable = table;
                    }
                }
            }
        }

        // Clear all current highlights
        foreach (var table in highlightedTables)
        {
            SetTableOutline(table, normalOutlineThickness);
        }
        highlightedTables.Clear();

        // Highlight only the closest table if one was found
        if (closestTable != null)
        {
            SetTableOutline(closestTable, highlightOutlineThickness);
            highlightedTables.Add(closestTable);
        }
    }

    private void SetTableOutline(TableZone table, float thickness)
    {
        if (table == null) return;

        // Get all SpriteRenderers in the table and its children
        SpriteRenderer[] allRenderers = table.GetComponentsInChildren<SpriteRenderer>();
        //Debug.Log("TABLE SELECTED");

        foreach (SpriteRenderer renderer in allRenderers)
        {
            if (renderer != null && renderer.material != null)
            {
                // Create material instance if it's not already instanced
                if (renderer.material.name.Contains("(Instance)") == false)
                    renderer.material = new Material(renderer.material);

                // Set the outline thickness
                if (renderer.material.HasProperty("_Outline_Thickness"))
                    renderer.material.SetFloat("_Outline_Thickness", thickness);
            }
        }
    }

    private bool isCustomerInteractible(CustomerBehavior customer)
    {
        return customer != null && (
                customer.state == CustomerBehavior.CustomerState.waiting ||
                customer.state == CustomerBehavior.CustomerState.ordering ||
                customer.state == CustomerBehavior.CustomerState.finished);
    }

    private void UpdateCustomerHighlighting()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius);

        Dictionary<int, List<CustomerBehavior>> partiesInRange = new Dictionary<int, List<CustomerBehavior>>();

        foreach (var hit in hits)
        {
            CustomerBehavior customer = hit.GetComponent<CustomerBehavior>();
            if (isCustomerInteractible(customer))
            {
                if (!partiesInRange.ContainsKey(customer.partyID))
                    partiesInRange[customer.partyID] = new List<CustomerBehavior>();

                partiesInRange[customer.partyID].Add(customer);
            }
        }

        int closestPartyID = -1;
        float closestDistance = float.MaxValue;

        foreach (var party in partiesInRange)
        {
            float partyClosestDistance = float.MaxValue;
            foreach (var customer in party.Value)
            {
                float distance = Vector2.Distance(transform.position, customer.transform.position);
                if (distance < partyClosestDistance)
                {
                    partyClosestDistance = distance;
                }
            }

            if (partyClosestDistance < closestDistance)
            {
                closestDistance = partyClosestDistance;
                closestPartyID = party.Key;
            }
        }

        HashSet<CustomerBehavior> customersToHighlight = new HashSet<CustomerBehavior>();
        if (closestPartyID != -1 && partiesInRange.ContainsKey(closestPartyID))
        {
            CustomerBehavior[] allCustomers = Object.FindObjectsByType<CustomerBehavior>(FindObjectsSortMode.None);

            foreach (var customer in allCustomers)
            {
                if (customer.partyID == closestPartyID && isCustomerInteractible(customer))
                {
                    customersToHighlight.Add(customer);
                }
            }
        }


        // Create a new material instance to avoid modifying the shared material
        var customersToUnhighlight = new HashSet<CustomerBehavior>(highlightedCustomers);
        customersToUnhighlight.ExceptWith(customersToHighlight);

        foreach (var customer in customersToUnhighlight)
        {
            SetCustomerOutline(customer, normalOutlineThickness);
            highlightedCustomers.Remove(customer);
        }

        foreach (var customer in customersToHighlight)
        {
            if (!highlightedCustomers.Contains(customer))
            {
                SetCustomerOutline(customer, highlightOutlineThickness);
                highlightedCustomers.Add(customer);
            }
        }
    }

    private void SetCustomerOutline(CustomerBehavior customer, float thickness)
    {
        if (customer == null) return;

        CustomerBehavior leader = GetPartyLeader(customer.partyID); // Find the party leader
        if (leader == null) return;

        SpriteRenderer customerRenderer = customer.GetComponent<SpriteRenderer>();

        if (leader.seatedTableID == -1) // If leader exists but unseated
        {
            if (customerRenderer != null && customerRenderer.material != null)
            {
                if (customerRenderer.material.name.Contains("(Instance)") == false)
                    customerRenderer.material = new Material(customerRenderer.material);

                if (customerRenderer.material.HasProperty("_Outline_Thickness"))
                    customerRenderer.material.SetFloat("_Outline_Thickness", thickness);
            }
        }
        else
        {
            // Find the table the leader is seated at
            TableZone[] allTables = FindObjectsByType<TableZone>(FindObjectsSortMode.None);
            foreach (var table in allTables)
            {
                if (table.GetTableID() == leader.seatedTableID)
                {
                    SetTableOutline(table, thickness);
                    break; // Found the table, no need to continue searching
                }
            }
        }
    }

    private void ClearAllCustomerHighlights()
    {
        foreach (var customer in highlightedCustomers)
        {
            SetCustomerOutline(customer, normalOutlineThickness);
        }
        highlightedCustomers.Clear();
    }

    private void ClearAllTableHighlights()
    {
        foreach (var table in highlightedTables)
        {
            SetTableOutline(table, normalOutlineThickness);
        }
        highlightedTables.Clear();
    }

    // Helper method to get party leader by party ID
    private CustomerBehavior GetPartyLeader(int partyID)
    {
        CustomerBehavior[] allCustomers = Object.FindObjectsByType<CustomerBehavior>(FindObjectsSortMode.None);
        CustomerBehavior leader = null;

        foreach (var customer in allCustomers)
        {
            if (customer.partyID == partyID)
            {
                if (leader == null || customer.GetInstanceID() < leader.GetInstanceID())
                {
                    leader = customer;
                }
            }
        }

        return leader;
    }

    private void CancelFollowingParty()
    {
        List<CustomerBehavior> followingParty = new List<CustomerBehavior>();

        GameObject[] allCustomers = GameObject.FindGameObjectsWithTag("Customer");

        foreach (GameObject obj in allCustomers)
        {
            CustomerBehavior customer = obj.GetComponent<CustomerBehavior>();
            if (customer != null && customer.state == CustomerBehavior.CustomerState.following)
            {
                customer.state = CustomerBehavior.CustomerState.inLine;

                // Reset movement
                Rigidbody2D rb = customer.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.bodyType = RigidbodyType2D.Dynamic;
                }

                // Clear TrailFollower
                var trail = customer.GetComponent<TrailFollower>();
                if (trail != null)
                {
                    Destroy(trail);
                }

                // Reset outline
                SpriteRenderer sr = customer.GetComponent<SpriteRenderer>();
                if (sr != null && sr.material != null && sr.material.HasProperty("_Outline_Thickness"))
                {
                    sr.material.SetFloat("_Outline_Thickness", 0f);
                }

                followingParty.Add(customer); // ← now this works
            }
        }

        if (followingParty.Count > 0)
        {
            int partyID = followingParty[0].partyID;
            CustomerSpawner spawner = FindObjectOfType<CustomerSpawner>();
            spawner.RequeueParty(followingParty, partyID);
        }

        GameStateManager.SetFollowingParty(false);
    }
}
