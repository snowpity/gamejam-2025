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

    private readonly int animMoveLeft = Animator.StringToHash("Anim_character_move_left");
    private readonly int animIdleLeft = Animator.StringToHash("Anim_character_idle_left");
    private readonly int animMoveRight = Animator.StringToHash("Anim_character_move_right");
    private readonly int animIdleRight = Animator.StringToHash("Anim_character_idle_right");

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
        bool isMoving = movementVector.sqrMagnitude > 0.001f;

        switch (facingDirection)
        {
            case Directions.LEFT:
                animator.CrossFade(isMoving ? animMoveLeft : animIdleLeft, animCrossFade);
                break;
            case Directions.RIGHT:
                animator.CrossFade(isMoving ? animMoveRight : animIdleRight, animCrossFade);
                break;
        }
    }
}
