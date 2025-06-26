using UnityEngine;

public class SpriteDepth : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Primary sorting by Y position (top-down)
        // Secondary sorting by X position (left-right)
        // Multiply Y by a larger factor to ensure it takes priority
        int ySort = -(int)(transform.position.y * 1000f);
        int xSort = (int)(transform.position.x * 10f);

        spriteRenderer.sortingOrder = ySort + xSort;
    }
}