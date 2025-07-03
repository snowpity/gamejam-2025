using UnityEngine;
using System.Collections.Generic;

public class TableZone : MonoBehaviour
{
    [SerializeField] public int tableID;
    private Transform[] seatPositions;

    private GameObject foodOrigin;
    private GameObject tableTagObj;
    private SpriteRenderer tableTagSprite;

    [Header("Table Tag Sprites")]
    [SerializeField] public Sprite[] tableTag;
    [SerializeField] public GameObject tableTagPrefab;

    private void Awake()
    {
        // Auto-load seats
        List<Transform> seats = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.name.ToLower().StartsWith("seat"))
                seats.Add(child);
        }

        seatPositions = seats.ToArray();
    }

    public int GetTableID() => tableID;
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

    private GameObject FindFoodOriginForTable()
    {
        Transform foodOriginTransform = this.transform.Find("FoodOrigin");
        if (foodOriginTransform != null)
        {
            return foodOriginTransform.gameObject;
        }
        return null;
    }

    public void createTableTag()
    {
        foodOrigin = FindFoodOriginForTable();

        if (foodOrigin == null) return;

        // Create the tag obj
        tableTagObj = Instantiate(tableTagPrefab, foodOrigin.transform.position, Quaternion.identity);
        tableTagObj.transform.SetParent(this.transform);

        // Set sprite
        tableTagSprite = tableTagObj.GetComponent<SpriteRenderer>();
        tableTagSprite.sprite = tableTag[tableID - 1];
    }

    public void deleteTableTag()
    {
        if (tableTagObj != null)
        {
            Destroy(tableTagObj);
            tableTagObj = null;
            tableTagSprite = null;
        }
    }
}
