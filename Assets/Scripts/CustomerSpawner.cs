using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Customer Settings")]
    public GameObject customerPrefab;
    public int maxCustomers = 5;
    public float spawnInterval = 2f;

    [Header("Queue Settings")]
    public Transform queueStartPoint;
    public float spacing = 1.1f;

    private List<GameObject> customerQueue = new List<GameObject>();
    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval && customerQueue.Count < maxCustomers)
        {
            SpawnCustomer();
            timer = 0f;
        }
    }

    void SpawnCustomer()
    {
        Vector3 spawnPos = queueStartPoint.position + Vector3.down * spacing * customerQueue.Count;
        GameObject newCustomer = Instantiate(customerPrefab, spawnPos, Quaternion.identity, transform);
        customerQueue.Add(newCustomer);
    }

    public void RemoveCustomer(GameObject customer)
    {
        if (customerQueue.Contains(customer))
        {
            customerQueue.Remove(customer);
            Destroy(customer);
            RepositionQueue();
        }
    }

    void RepositionQueue()
    {
        for (int i = 0; i < customerQueue.Count; i++)
        {
            Vector3 newPos = queueStartPoint.position + Vector3.down * spacing * i;
            customerQueue[i].transform.position = newPos;
        }
    }
}
