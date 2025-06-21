using UnityEngine;

public class FollowerTrail : MonoBehaviour
{
    public Vector2[] locationMemory = new Vector2[121];
    [SerializeField] private float minIncrement = .005f;

    private Transform leader;
    private Rigidbody2D rb;
    [SerializeField] private Vector2 previousPos;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        leader = this.transform;
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //once the player moves a certain amount of distance, record new location and push down the array
        if (Vector2.Distance(transform.position, previousPos) > minIncrement)
        {
            for (int i = locationMemory.Length - 1; i >= 0; i--)
            {
                if (i != 0)
                {
                    locationMemory[i] = locationMemory[i - 1];
                }
                else
                {
                    locationMemory[i] = previousPos;
                }
            }
            previousPos = leader.position;
        }
    }
}
