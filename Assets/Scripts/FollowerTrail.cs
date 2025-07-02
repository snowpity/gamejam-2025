using System.Collections.Generic;
using UnityEngine;

public class FollowerTrail : MonoBehaviour
{
    [SerializeField] private bool showDebugTrail = false;

    public Vector2[] locationMemory = new Vector2[121];
    [SerializeField] private float minIncrement = .005f;
    [SerializeField] private int spacingReset = 10;

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

    public void UpdateTrail(GameObject[] customerList, int closestPartyID)
    {
        var dynamicList = new List<GameObject>();
        foreach (var customer in customerList)
        {
            if (customer == null)
            {
                Debug.LogWarning("FollowerTrail: Found null customer in customerList");
                continue;
            }

            CustomerBehavior behavior = customer.GetComponent<CustomerBehavior>();
            if (behavior == null)
            {
                Debug.LogWarning($"FollowerTrail: CustomerBehavior component missing on {customer.name}");
                continue;
            }

            if (behavior.partyID == closestPartyID)
            {
                dynamicList.Add(customer);
            }
        }

        dynamicList.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));
        var partyMembers = new List<CustomerBehavior>();
        float interpolationIncrement = 1f / spacingReset;

        int updatePoint = 0;
        float interpolation = 0f;
        Vector2 anchor = leader.position;

        for (int i = 0; i < locationMemory.Length; i++)
        {
            if (i == (updatePoint+1)*10)
            {
                if (updatePoint+1 < dynamicList.Count)
                {
                    locationMemory[i] = dynamicList[updatePoint].transform.position;
                    interpolation = 0f;
                    updatePoint++;
                    anchor = dynamicList[updatePoint-1].transform.position;
                }
                else
                {
                    locationMemory[i] = dynamicList[dynamicList.Count-1].transform.position;
                    i = locationMemory.Length;
                }

            }
            else
            {
                locationMemory[i] = Vector2.Lerp(anchor, dynamicList[updatePoint].transform.position, interpolation);
                interpolation += interpolationIncrement;
            }
        }
    }
    private void OnDrawGizmos()
    {
        if (showDebugTrail)
        {
            foreach (Vector2 point in locationMemory)
            {
                Gizmos.DrawSphere(point, .2f);
            }
        }
    }
}
