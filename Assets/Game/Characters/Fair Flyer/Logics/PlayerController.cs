using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

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
    [SerializeField] private float highlightOutlineThickness = 2f / 64f; // 2 pixels
    [SerializeField] private float normalOutlineThickness = 0f;


    private readonly int animMoveLeft = Animator.StringToHash("Anim_character_move_left");
    private readonly int animIdleLeft = Animator.StringToHash("Anim_character_idle_left");

    // Keep track of currently highlighted customers
    private HashSet<CustomerBehavior> highlightedCustomers = new HashSet<CustomerBehavior>();

    private void OnEnable()
    {
        interact.action.performed += TryInteract;
    }

    private void OnDisable()
    {
        interact.action.performed -= TryInteract;
    }

    // MOVEMENT
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

    // INTERACT SYSTEM
    private void TryInteract(InputAction.CallbackContext context)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius);
        foreach (var hit in hits)
        {
            // Selecting a party to follow
            CustomerBehavior customer = hit.GetComponent<CustomerBehavior>();
            if (customer != null && customer.state == CustomerBehavior.CustomerState.Waiting && !GameStateManager.hasFollowingParty)
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

                break; // Only trigger one group
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
        UpdateCustomerHighlighting();
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



    // CUSTOMER OUTLINING SYSTEM
    private void UpdateCustomerHighlighting()
    {
        // Get all colliders within interaction range
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius);

        // Find all waiting customers in range and group them by partyID
        Dictionary<int, List<CustomerBehavior>> partiesInRange = new Dictionary<int, List<CustomerBehavior>>();

        foreach (var hit in hits)
        {
            CustomerBehavior customer = hit.GetComponent<CustomerBehavior>();
            if (customer != null && customer.state == CustomerBehavior.CustomerState.Waiting && !GameStateManager.hasFollowingParty)
            {
                if (!partiesInRange.ContainsKey(customer.partyID))
                {
                    partiesInRange[customer.partyID] = new List<CustomerBehavior>();
                }
                partiesInRange[customer.partyID].Add(customer);
            }
        }

        // Find the closest party (by finding the closest member of each party)
        int closestPartyID = -1;
        float closestDistance = float.MaxValue;

        foreach (var party in partiesInRange)
        {
            // Find the closest member of this party
            float partyClosestDistance = float.MaxValue;
            foreach (var customer in party.Value)
            {
                float distance = Vector2.Distance(transform.position, customer.transform.position);
                if (distance < partyClosestDistance)
                {
                    partyClosestDistance = distance;
                }
            }

            // Check if this party is closer than our current closest
            if (partyClosestDistance < closestDistance)
            {
                closestDistance = partyClosestDistance;
                closestPartyID = party.Key;
            }
        }

        // Get all customers that should be highlighted (the closest party)
        HashSet<CustomerBehavior> customersToHighlight = new HashSet<CustomerBehavior>();
        if (closestPartyID != -1 && partiesInRange.ContainsKey(closestPartyID))
        {
            // Add ALL members of the closest party to highlight, even if they're outside interaction range
            CustomerBehavior[] allCustomers = FindObjectsOfType<CustomerBehavior>();
            foreach (var customer in allCustomers)
            {
                if (customer.partyID == closestPartyID && customer.state == CustomerBehavior.CustomerState.Waiting)
                {
                    customersToHighlight.Add(customer);
                }
            }
        }

        // Remove highlight from customers no longer supposed to be highlighted
        var customersToUnhighlight = new HashSet<CustomerBehavior>(highlightedCustomers);
        customersToUnhighlight.ExceptWith(customersToHighlight);

        foreach (var customer in customersToUnhighlight)
        {
            SetCustomerOutline(customer, normalOutlineThickness);
            highlightedCustomers.Remove(customer);
        }

        // Add highlight to customers that should be highlighted
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
            // Create a new material instance to avoid modifying the shared material
            if (customerRenderer.material.name.Contains("(Instance)") == false)
            {
                customerRenderer.material = new Material(customerRenderer.material);
            }

            // Set the outline thickness
            if (customerRenderer.material.HasProperty("_Outline_Thickness"))
            {
                customerRenderer.material.SetFloat("_Outline_Thickness", thickness);
            }
        }
    }

    private void ClearAllHighlights()
    {
        foreach (var customer in highlightedCustomers)
        {
            SetCustomerOutline(customer, normalOutlineThickness);
        }
        highlightedCustomers.Clear();
    }
}
