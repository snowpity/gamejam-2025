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


        // if holding food, check for nearby correct table to deliver
        if (isHoldingFood)
        {
            foreach (var hit in hits)
            {
                TableZone table = hit.GetComponent<TableZone>();
                if (table != null && table.GetTableID() == heldFoodTableID)
                {
                    Debug.Log($"[DeliverySystem] Delivered food to Table {heldFoodTableID}");

                    if (tableOrders.TryGetValue(heldFoodTableID, out var party))
                    {
                        foreach (var member in party.members)
                        {
                            member.ReceiveFood();
                        }
                    }

                    isHoldingFood = false;
                    heldFoodTableID = -1;

                    if (heldFoodObject != null)
                    {
                        Destroy(heldFoodObject);
                        heldFoodObject = null;
                    }

                    return; // End interaction after delivery
                }
            }
        }

        // Check if player is near a table that wants to order
        foreach (var hit in hits)
        {
            TableZone table = hit.GetComponent<TableZone>();
            if (table != null)
            {
                int tableID = table.GetTableID();

                if (GameStateManager.TableWantsToOrder(tableID))
                {
                    Debug.Log($"[Player] Took receipt from Table {tableID}, submitting to kitchen...");

                    GameStateManager.SubmitOrderToKitchen(tableID);
                    Kitchen.Instance.ReceiveOrder(tableID, GetCustomerPartyAtTable(tableID));

                    return; // End interaction after handling the order
                }
            }
        }

        // Check if player is near the kitchen and there's a ready order
        int readyTableID = KitchenPickupZone.Instance.GetReadyOrderInRange(transform.position, interactionRadius);
        if (readyTableID != -1)
        {
            GameStateManager.ClearOrder(readyTableID);
            Debug.Log($"[PickupSystem] Picked up order for Table {readyTableID}");

            // Store that we're now holding food
            isHoldingFood = true;
            heldFoodTableID = readyTableID;

            // Optionally spawn a visual food object to show it's being carried
            heldFoodObject = Instantiate(Kitchen.Instance.foodPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            heldFoodObject.transform.SetParent(transform);  // Attach to player
        }

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

                foreach (var obj in allCustomers)
                {
                    var c = obj.GetComponent<CustomerBehavior>();
                    if (c != null && c.partyID == closestPartyID && isCustomerInteractible(c))
                    {
                        if(c.state == CustomerBehavior.CustomerState.waiting)
                        {
                            c.startFollowing();
                        }
                        else if(c.state == CustomerBehavior.CustomerState.ordering)
                        {
                            c.startWaitingFood();
                        }
                    }
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

                var seats = table.GetSeatPositions().Where(s => s.childCount == 0).ToArray();
                if (seats.Length < partySize)
                    continue;

                float distance = Vector2.Distance(transform.position, table.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTable = table;
                }
            }

            // Seat the party at the closest table if found
            if (closestTable != null)
            {
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
                        customerSprite.flipX = !seatSprite.flipX;
                        additiveSpacing = new Vector3(xShift,yShift,0);
                    }
                    else
                    {
                        additiveSpacing = new Vector3(-xShift,yShift,0);
                    }

                    customer.transform.position = seat.position + additiveSpacing;
                    customer.transform.SetParent(seat); // optional, keeps them attached
                    customer.seatedTableID = closestTable.tableID;
                    customer.SitDown();



                }

                

                //GameStateManager.SetFollowingParty(false);
            }
        }
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

    private void UpdateTableHighlighting() // TODO: Figure out a flag to set table if filled????
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
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius);


        

        TableZone closestTable = null;
        float closestDistance = float.MaxValue;

        // Find the closest table with enough available seats
        foreach (var hit in hits)
        {
            TableZone table = hit.GetComponent<TableZone>();
            if (table == null) continue;

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
                customer.state == CustomerBehavior.CustomerState.ordering);
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

        SpriteRenderer customerRenderer = customer.GetComponent<SpriteRenderer>();
        if (customerRenderer != null && customerRenderer.material != null)
        {
            if (customerRenderer.material.name.Contains("(Instance)") == false)
                customerRenderer.material = new Material(customerRenderer.material);

            if (customerRenderer.material.HasProperty("_Outline_Thickness"))
                customerRenderer.material.SetFloat("_Outline_Thickness", thickness);
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

    private CustomerBehavior.CustomerParty GetCustomerPartyAtTable(int tableID)
    {
        CustomerBehavior.CustomerParty party = new CustomerBehavior.CustomerParty();
        party.partyID = -1;

        CustomerBehavior[] allCustomers = FindObjectsByType<CustomerBehavior>(FindObjectsSortMode.None);
        foreach (var customer in allCustomers)
        {
            if (customer.seatedTableID == tableID)
            {
                if (party.partyID == -1)
                    party.partyID = customer.partyID;

                party.members.Add(customer);
            }
        }

        return party;
    }
}
