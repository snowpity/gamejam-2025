using UnityEngine;
using UnityEngine.InputSystem;

[SelectionBase]
public class PlayerController : MonoBehaviour
{
    private enum Directions { UP, DOWN, LEFT, RIGHT }

    private Vector2 movementInput;
    private Directions facingDirection = Directions.RIGHT;
    private float animCrossFade = 0;

    // Input logics
    [Header("Player Attributes")]
    [SerializeField] private float movementSpeed = 50f;

    [Header("Dependencies")] // Reveal some input when you click on the "Player" object
    [SerializeField] private InputActionReference movement;
    [SerializeField] private InputActionReference interact;
    [SerializeField] private Rigidbody2D rigidBody;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private readonly int animMoveRight = Animator.StringToHash("Anim_character_move_right");
    private readonly int animIdleRight = Animator.StringToHash("Anim_character_idle_right");

    // Movement logics
    private void getInput()
    {
        movementInput = movement.action.ReadValue<Vector2>();
    }
    private void updateMovement()
    {
        rigidBody.linearVelocity = movementInput.normalized * movementSpeed * Time.fixedDeltaTime;
    }

    // Animation logics
    private void getFacingDirection()
    {
        if (movementInput.y != 0 && Mathf.Abs(movementInput.y) > Mathf.Abs(movementInput.x)) // Check y diretion
        {
            if (movementInput.y > 0)
            {
                facingDirection = Directions.UP;
            }
            else if (movementInput.y < 0)
            {
                facingDirection = Directions.DOWN;
            }
        }
        else if (movementInput.x != 0)
        {
            if (movementInput.x > 0) // Moving right
            {
                facingDirection = Directions.RIGHT;
            }
            else if (movementInput.x < 0) // Moving left
            {
                facingDirection = Directions.LEFT;
            }
        }
    }

    private void updateAnimation()
    {
        if (facingDirection == Directions.RIGHT)
        {
            spriteRenderer.flipX = true;
        }
        else if (facingDirection == Directions.LEFT)
        {
            spriteRenderer.flipX = false;
        }

        if (movementInput.SqrMagnitude() > 0)
        {
            animator.CrossFade(animMoveRight, animCrossFade);
        }
        else
        {
            animator.CrossFade(animIdleRight, animCrossFade);
        }
        // Todo: eventually we'll have front and back, right?
    }

    // Loop
    private void Update()
    {
        getInput();
        getFacingDirection();
        updateAnimation();
    }

    private void FixedUpdate()
    {
        updateMovement();
    }
}