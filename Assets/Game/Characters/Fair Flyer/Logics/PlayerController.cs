using UnityEngine;
using UnityEngine.InputSystem;

[SelectionBase]
public class PlayerController : MonoBehaviour
{
    private Vector2 movementInput; // Movement direction

    // Input logics
    [Header("Player Attributes")]
    [SerializeField]
    private float movementSpeed = 50f;

    [Header("Dependencies")]
    [SerializeField] // Reveal some input when you click on the "Player" object
    private InputActionReference movement;
    [SerializeField]
    private InputActionReference interact;
    [SerializeField]
    private Rigidbody2D rigidBody;

    // Movement logics
    private void getInput()
    {
        movementInput = movement.action.ReadValue<Vector2>();
        print(movementInput);
    }
    private void updateMovement()
    {
        rigidBody.linearVelocity = movementInput * movementSpeed * Time.fixedDeltaTime;
    }

    // Loop
    private void Update()
    {
        getInput();
    }

    private void FixedUpdate()
    {
        updateMovement();
    }
}