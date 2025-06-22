using UnityEngine;
using UnityEngine.UIElements;

public class TrailFollower : MonoBehaviour
{
    private enum Directions { UP, DOWN, LEFT, RIGHT };
    private float animCrossFade = 0;
    private Vector3 previousPosition;

    public int TrailPosition;
    [SerializeField] private FollowerTrail trail;
    [SerializeField] private float LerpSpeed = .3f;

    private Directions facingDirection = Directions.RIGHT;

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private readonly int animMoveRight = Animator.StringToHash("Anim_character_move_left");
    private readonly int animIdleRight = Animator.StringToHash("Anim_character_idle_left");

    void Start() 
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        previousPosition = transform.position;
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 targetPosition = trail.locationMemory[TrailPosition];

        // Determine facing direction
        if (targetPosition.x < transform.position.x)
        {
            facingDirection = Directions.LEFT;
        }
        else if (targetPosition.x > transform.position.x)
        {
            facingDirection = Directions.RIGHT;
        }

        Vector3 oldPosition = transform.position;
        //transform.position = Vector3.MoveTowards(transform.position, trail.positions[followerID], speed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, LerpSpeed);

        // Get animation
        updateAnimation(transform.position - oldPosition);
    }

    private void updateAnimation(Vector3 movementVector)
    {
        spriteRenderer.flipX = (facingDirection == Directions.RIGHT);

        if (movementVector.sqrMagnitude > 0.001f)
            animator.CrossFade(animMoveRight, animCrossFade);
        else
            animator.CrossFade(animIdleRight, animCrossFade);
    }
}
