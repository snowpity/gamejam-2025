using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

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

        if (!GameStateManager.hasFollowingParty)
        {
            // try to select a party to follow
            foreach (var hit in hits)
            {
                CustomerBehavior customer = hit.GetComponent<CustomerBehavior>();
                if (customer != null && customer.state == CustomerBehavior.CustomerState.Waiting)
                {
                    int partyToFollow = customer.partyID;
                    GameObject[] allCustomers = GameObject.FindGameObjectsWithTag("Customer");

                    foreach (var obj in allCustomers)
                    {
                        var c = obj.GetComponent<CustomerBehavior>();
                        if (c != null && c.partyID == partyToFollow && c.state == CustomerBehavior.CustomerState.Waiting)
                        {
                            c.StartFollowing();
                        }
                    }
                    break;
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

            foreach (var hit in hits)
            {
                TableZone table = hit.GetComponent<TableZone>();
                if (table == null) continue;

                var seats = table.GetSeatPositions().Where(s => s.childCount == 0).ToArray();
                if (seats.Length < partySize)
                    continue;

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
                    customer.SitDown();
                }

                //GameStateManager.SetFollowingParty(false);
                break;
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
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius);
        HashSet<TableZone> tablesToHighlight = new HashSet<TableZone>();

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

        foreach (var hit in hits)
        {
            TableZone table = hit.GetComponent<TableZone>();
            if (table == null) continue;

            // Check if table has enough available seats
            var availableSeats = table.GetSeatPositions().Where(s => s.childCount == 0).ToArray();
            if (availableSeats.Length >= partySize)
            {
                tablesToHighlight.Add(table);
            }
        }

        // Remove highlight from tables that should no longer be highlighted
        var tablesToUnhighlight = new HashSet<TableZone>(highlightedTables);
        tablesToUnhighlight.ExceptWith(tablesToHighlight);

        foreach (var table in tablesToUnhighlight)
        {
            SetTableOutline(table, normalOutlineThickness);
            highlightedTables.Remove(table);
        }

        foreach (var table in tablesToHighlight)
        {
            if (!highlightedTables.Contains(table))
            {
                SetTableOutline(table, highlightOutlineThickness);
                highlightedTables.Add(table);
            }
        }
    }

    private void SetTableOutline(TableZone table, float thickness)
    {
        if (table == null) return;

        // Get all SpriteRenderers in the table and its children
        SpriteRenderer[] allRenderers = table.GetComponentsInChildren<SpriteRenderer>();
        Debug.Log("TABLE SELECTED");

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

    private void UpdateCustomerHighlighting()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius);
        Dictionary<int, List<CustomerBehavior>> partiesInRange = new Dictionary<int, List<CustomerBehavior>>();

        foreach (var hit in hits)
        {
            CustomerBehavior customer = hit.GetComponent<CustomerBehavior>();
            if (customer != null && customer.state == CustomerBehavior.CustomerState.Waiting)
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
                if (customer.partyID == closestPartyID && customer.state == CustomerBehavior.CustomerState.Waiting)
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
}
