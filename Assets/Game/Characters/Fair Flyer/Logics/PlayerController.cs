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

                    customer.transform.position = seat.position;
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

    private void UpdateCustomerHighlighting()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius);
        Dictionary<int, List<CustomerBehavior>> partiesInRange = new Dictionary<int, List<CustomerBehavior>>();

        foreach (var hit in hits)
        {
            CustomerBehavior customer = hit.GetComponent<CustomerBehavior>();
            if (customer != null && customer.state == CustomerBehavior.CustomerState.Waiting && !GameStateManager.hasFollowingParty)
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

    private void ClearAllHighlights()
    {
        foreach (var customer in highlightedCustomers)
        {
            SetCustomerOutline(customer, normalOutlineThickness);
        }
        highlightedCustomers.Clear();
    }
}
