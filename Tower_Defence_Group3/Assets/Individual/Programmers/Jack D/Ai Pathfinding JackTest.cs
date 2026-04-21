using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class AiPathfindingJackTest : MonoBehaviour
{
    AIController AIController;
    public int speed = 5;
    private int index = 0;
    public List<GameObject> aiPathNodes;

    private bool reachedEnd = false;
    private float timer = 0f;
    public float damageInterval = 3f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AIController = GetComponent<AIController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (reachedEnd)
        {
            timer += Time.deltaTime;

            if (timer >= damageInterval)
            {
                PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
                
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(AIController.damage);
                }

                timer = 0f;
            }
        }

        WayPoints();
    }

    void WayPoints()
    {
        Vector3 destination = aiPathNodes[index].transform.position;
        Vector3 newPos = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
        transform.position = newPos;

        float distance = Vector3.Distance(transform.position, destination);
        if (distance <= 0.05)
        {
            if (index < aiPathNodes.Count - 1)
            {
                index++;
            }

            else
            {
                reachedEnd = true;
            }
        }
    }


}
