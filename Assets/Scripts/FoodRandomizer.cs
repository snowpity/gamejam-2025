using UnityEngine;

public class FoodRandomizer : MonoBehaviour
{
    [Header("Food Sprites")]
    public Sprite[] foodSprites;

    [Header("Settings")]
    public bool randomizeOnStart = true;
    public int selectedSprite = -1;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (randomizeOnStart) // For Soiree to spawn in originally
        {
            RandomizeFood();
        }
        else if (selectedSprite != -1) // For the player to pick up, create a new food object, but references the 
        {
            spriteRenderer.sprite = foodSprites[selectedSprite];
        }
    }

    public void RandomizeFood()
    {
        // Pick a random sprite from the array
        selectedSprite = Random.Range(0, foodSprites.Length);
        spriteRenderer.sprite = foodSprites[selectedSprite];
    }
}