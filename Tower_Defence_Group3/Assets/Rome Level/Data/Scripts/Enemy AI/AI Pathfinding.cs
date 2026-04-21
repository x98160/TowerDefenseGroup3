using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class AiPathfinding : MonoBehaviour
{
    AIController AIController;
    public float speed = 5;
    public float originalSpeed = 5;
    private int index = 0;
    public List<Transform> aiPathNodes;

    private bool reachedEnd = false;
    private float timer = 0f;
    public float damageInterval = 3f;
    public Vector3 currentDirection { get; private set; }
    private Coroutine speedBoostRoutine;
    private Vector3 originalScale;
    private TrailRenderer trail;
    private Renderer rend;

    private void Awake()
    {
        aiPathNodes = FindObjectsByType<PathNode>(FindObjectsSortMode.None)
                .OrderBy(n => n.order)
                .Select(n => n.transform)
                .ToList();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AIController = GetComponent<AIController>();
        originalSpeed = speed;

        originalScale = transform.localScale;
        if (trail == null) trail = GetComponentInChildren<TrailRenderer>();
        if (rend == null) rend = GetComponentInChildren<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        // When the player reaches the last node, player takes damage
        if (reachedEnd)
        {
            timer += Time.deltaTime;

            if (timer >= damageInterval)
            {
                PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();

                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(AIController.damage);
                    Destroy(gameObject);
                }

                timer = 0f;
            }
        }

        WayPoints();
    }

    void WayPoints()
    {
        if (aiPathNodes == null || aiPathNodes.Count == 0) return;

        Vector3 destination = aiPathNodes[index].position;

        Vector3 direction = (destination - transform.position);
        direction.y = 0f;

        currentDirection = direction;
        // ALWAYS update rotation from movement
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }

        // Move
        transform.position = Vector3.MoveTowards(
            transform.position,
            destination,
            speed * Time.deltaTime
        );

        float distance = Vector3.Distance(transform.position, destination);

        if (distance <= 0.05f)
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

    public void ApplySpeedBuff()
    {
        StartCoroutine(SpeedBoostAnimation());
    }
    IEnumerator SpeedBoostAnimation()
    {
        float duration = 0.4f;
        float time = 0f;

        float originalSpeed = speed;
        speed += 2f;

        Vector3 originalScale = transform.localScale;
        Vector3 boostedScale = originalScale * 1.2f;

        Renderer rend = GetComponentInChildren<Renderer>();
        TrailRenderer trail = GetComponentInChildren<TrailRenderer>();

        if (trail) trail.emitting = true;

        while (time < duration)
        {
            float t = time / duration;

            transform.localScale = Vector3.Lerp(
                originalScale,
                boostedScale,
                Mathf.PingPong(t * 2f, 1f)
            );

            time += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;

        //  PERMANENT CYAN FACE (no reset)
        if (rend)
            rend.material.color = Color.cyan;

        yield return new WaitForSeconds(1.5f);

        if (trail) trail.emitting = false;

    }
}
