using UnityEngine;

public class TrailFollower : MonoBehaviour
{
    private enum Directions { UP, DOWN, LEFT, RIGHT };
    private float animCrossFade = 0;

    public int TrailPosition;

    [SerializeField] private FollowerTrail trail;
    [SerializeField] private float LerpSpeed = .3f;

    private Directions facingDirection = Directions.RIGHT;

    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private readonly int animMoveRight = Animator.StringToHash("Anim_character_move_left");
    private readonly int animIdleRight = Animator.StringToHash("Anim_character_idle_left");

    public FollowerTrail Trail
    {
        get { return trail; }
        set { trail = value; }
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        if (trail == null || trail.locationMemory.Length <= TrailPosition) return;

        Vector3 targetPosition = trail.locationMemory[TrailPosition];
        Vector3 oldPosition = transform.position;

        transform.position = Vector3.Lerp(transform.position, targetPosition, LerpSpeed);

        // determine facing direction
        if (targetPosition.x < transform.position.x)
            facingDirection = Directions.LEFT;
        else if (targetPosition.x > transform.position.x)
            facingDirection = Directions.RIGHT;

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
