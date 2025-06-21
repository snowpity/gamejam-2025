using UnityEngine;
using UnityEngine.UIElements;

public class TrailFollower : MonoBehaviour
{
    public int TrailPosition;
    [SerializeField] private FollowerTrail trail;
    [SerializeField] private float LerpSpeed = .3f;

    // Update is called once per frame
    void FixedUpdate()
    {
        //transform.position = Vector3.MoveTowards(transform.position, trail.positions[followerID], speed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, trail.locationMemory[TrailPosition], LerpSpeed);
    }
}
