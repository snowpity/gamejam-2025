using UnityEngine;
using System.Collections.Generic;

public class TableZone : MonoBehaviour
{
    private Transform[] seatPositions;

    private void Awake()
    {
        // automatically grab all child transforms named "Seat_*"
        List<Transform> seats = new List<Transform>();

        foreach (Transform child in transform)
        {
            if (child.name.ToLower().StartsWith("seat"))
                seats.Add(child);
        }

        seatPositions = seats.ToArray();
    }

    public Transform[] GetSeatPositions() => seatPositions;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        foreach (Transform child in transform)
        {
            if (child.name.ToLower().StartsWith("seat") && child != null)
                Gizmos.DrawSphere(child.position, 0.15f);
        }
    }
}
