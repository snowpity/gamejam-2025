using UnityEngine;

public class CustomerBehavior : MonoBehaviour
{
    public enum CustomerState { Waiting, Following }
    public CustomerState state = CustomerState.Waiting;

    private Transform followTarget; // The player to follow
    public float followSpeed = 3f;

    void Update()
    {
        if (state == CustomerState.Following && followTarget != null)
        {
            transform.position = Vector2.MoveTowards(transform.position, followTarget.position, followSpeed * Time.deltaTime);
        }
    }

    public void StartFollowing(Transform player)
    {
        state = CustomerState.Following;
        followTarget = player;
    }
}
