using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CustomerFollowActivator : MonoBehaviour
{
    [SerializeField] private float interactionRadius = 1.5f;
    [SerializeField] private LayerMask customerLayer;
    [SerializeField] private FollowerTrail playerTrail;
    [SerializeField] private int trailPositionSpacing = 20;

    private InputAction interactAction;

    private void OnEnable()
    {
        interactAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/space");
        interactAction.performed += OnInteract;
        interactAction.Enable();
    }

    private void OnDisable()
    {
        interactAction.performed -= OnInteract;
        interactAction.Disable();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        TryActivateParty();
    }

    private void TryActivateParty()
    {
        Debug.Log("Attempting to activate party...");

        // attempt block if any follower is already using this trail
        var allFollowers = FindObjectsOfType<TrailFollower>();
        foreach (var follower in allFollowers)
        {
            if (follower != null && follower.Trail == playerTrail)
            {
                Debug.LogWarning("Party activation blocked: someone is already following this trail.");
                return;
            }
        }

        // check nearby customers
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius, customerLayer);
        Debug.Log($"Found {hits.Length} potential followers in radius.");

        if (hits.Length == 0) return;

        List<TrailFollower> followers = new List<TrailFollower>();

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Customer"))
            {
                TrailFollower tf = hit.GetComponent<TrailFollower>();

                if (tf == null)
                {
                    Debug.LogWarning($"Object {hit.name} tagged 'Customer' has no TrailFollower!");
                    continue;
                }

                if (tf.Trail == null)
                {
                    tf.Trail = playerTrail;
                    tf.TrailPosition = trailPositionSpacing * (followers.Count + 1);
                    tf.enabled = true;
                    followers.Add(tf);

                    Debug.Log($"✅ Activated follower: {tf.name} with trailPos {tf.TrailPosition}");
                }
                else
                {
                    Debug.Log($"Skipping {tf.name}, already has a trail: {tf.Trail.name}");
                }
            }
        }

        Debug.Log($"Finished party activation. Total new followers: {followers.Count}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
