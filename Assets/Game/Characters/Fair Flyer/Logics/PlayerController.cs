using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[SelectionBase]
public class PlayerController : MonoBehaviour
{
    private enum Directions { UP, DOWN, LEFT, RIGHT }

    private Vector2 movementInput;
    private Directions facingDirection = Directions.RIGHT;
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

    private readonly int animMoveRight = Animator.StringToHash("Anim_character_move_left");
    private readonly int animIdleRight = Animator.StringToHash("Anim_character_idle_left");

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
            animator.CrossFade(animMoveRight, animCrossFade);
        else
            animator.CrossFade(animIdleRight, animCrossFade);
    }

    // INTERACT SYSTEM
    private void TryInteract(InputAction.CallbackContext context)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius);
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
                        c.StartFollowing(this.transform);
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
}
