using UnityEngine;

public class SpriteDepth : MonoBehaviour
{
    [SerializeField] private bool isStatic = false;
    [SerializeField] private string parentName = "Seat_";
    [SerializeField] private int zDepthOffset = 1;

    private bool hasParent = false;
    private bool isStaticCalculated = false;

    SpriteRenderer spriteRenderer;
    Transform currentParent;
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Assume Parent is static, no reason to update
        if ((hasParent || isStaticCalculated))
        {
            if(checkHigherThanParent())
                return;
        }

        // Primary sorting by Y position (top-down)
        // Secondary sorting by X position (left-right)
        // Multiply Y by a larger factor to ensure it takes priority
        if(checkHasParent())
        {
            SpriteRenderer parentSpriteRenderer = currentParent.GetComponent<SpriteRenderer>();
            int parentSortingOrder = parentSpriteRenderer != null ? parentSpriteRenderer.sortingOrder : 0;

            spriteRenderer.sortingOrder = parentSortingOrder + zDepthOffset;
        }
        else
        {
            int ySort = -(int)(transform.position.y * 1000f);
            int xSort = (int)(transform.position.x * 10f);
            spriteRenderer.sortingOrder = ySort + xSort;
            if (isStatic)
            {
                isStaticCalculated = true;
            }
        }

    }

    private bool checkHasParent()
    {
        currentParent = this.transform.parent;
        if(currentParent != null && currentParent.gameObject.name.ToLower().StartsWith(parentName.ToLower()))
        {
            //Debug.Log(currentParent.gameObject.name.ToLower());
            hasParent = true;
            return hasParent;
        }
        return false;
    }

    // Make sure the child is always one above the parent.
    private bool checkHigherThanParent()
    {
        currentParent = this.transform.parent;
        if(currentParent != null)
        {
            SpriteRenderer parentSpriteRenderer = currentParent.GetComponent<SpriteRenderer>();
            return spriteRenderer.sortingOrder - zDepthOffset == parentSpriteRenderer.sortingOrder;
        }
        return false;
    }
}