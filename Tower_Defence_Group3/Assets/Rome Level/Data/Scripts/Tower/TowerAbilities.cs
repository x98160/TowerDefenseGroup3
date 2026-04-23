using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TowerAbilities : MonoBehaviour
{
    private bool multishotAbility = false;
    private bool slowmovementAbility = false;
    private bool poisonAbility = false;
    private bool splashdamageAbility = false;

    [SerializeField] private float poisonDamage = 3f;
    [SerializeField] private float poisonRadius = 10f;
    [SerializeField] private float poisonTickRate = 1f;
    [SerializeField] private float slowmovementSpeed = 0.7f;
    [SerializeField] private float slowmovementRadius = 10f;
    [SerializeField] private float splashDamage = 15f;
    [SerializeField] private float splashDamageRadius = 5f;
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] public TowerData towerData;
    [SerializeField] private int towerID = -1;

    // Track enemies THIS tower is currently slowing
    private HashSet<AiPathfinding> slowedEnemies = new HashSet<AiPathfinding>();

    void Start()
    {
        if (towerData != null && towerID >= 0)
        {
            ObjectData myData = towerData.objectsData.Find(obj => obj.ID == towerID);
            if (myData != null)
                ApplyAbility(myData.Ability);
            else
                Debug.LogWarning($"[TowerAbilities] No ObjectData with ID {towerID} found.");
        }
    }

    void Update()
    {
        if (slowmovementAbility) SlowMovement();
    }

    void OnDestroy()
    {
        // Restore all slowed enemies when tower is removed
        foreach (AiPathfinding ai in slowedEnemies)
        {
            if (ai != null)
                ai.speed = ai.originalSpeed;
        }
        slowedEnemies.Clear();
    }

    public void ApplyAbility(Abilities ability)
    {
        switch (ability)
        {
            case Abilities.multishotAbility:
                if (multishotAbility) return;
                multishotAbility = true;
                break;

            case Abilities.slowmovementAbility:
                if (slowmovementAbility) return;
                slowmovementAbility = true;
                break;

            case Abilities.poisonAbility:
                if (poisonAbility) return;
                poisonAbility = true;
                StartCoroutine(PoisonCoroutine());
                break;

            case Abilities.splashdamageAbility:
                if (splashdamageAbility) return;
                splashdamageAbility = true;
                break;
        }
    }

    #region Slow Movement Ability
    public void SlowMovement()
    {
        HashSet<AiPathfinding> inRangeNow = new HashSet<AiPathfinding>();

        // Find all enemies currently in range
        Collider[] hits = Physics.OverlapSphere(transform.position, slowmovementRadius, LayerMask.GetMask("AI"));
        foreach (Collider col in hits)
        {
            AiPathfinding ai = col.GetComponent<AiPathfinding>();
            if (ai == null) continue;

            inRangeNow.Add(ai);

            // Apply slow if not already slowed by this tower
            if (!slowedEnemies.Contains(ai))
            {
                slowedEnemies.Add(ai);
                ai.speed = ai.originalSpeed * slowmovementSpeed;
            }
        }

        // Find enemies that left this tower's range and restore their speed
        List<AiPathfinding> leftRange = new List<AiPathfinding>();
        foreach (AiPathfinding ai in slowedEnemies)
        {
            if (!inRangeNow.Contains(ai))
                leftRange.Add(ai);
        }

        foreach (AiPathfinding ai in leftRange)
        {
            slowedEnemies.Remove(ai);
            if (ai != null)
                ai.speed = ai.originalSpeed;
        }
    }
    #endregion

    #region Poison Ability
    private IEnumerator PoisonCoroutine()
    {
        while (poisonAbility)
        {
            PoisonAbility();
            yield return new WaitForSeconds(poisonTickRate);
        }
    }

    public void PoisonAbility()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, poisonRadius, LayerMask.GetMask("AI"));
        foreach (Collider col in enemies)
        {
            AIController aiController = col.GetComponent<AIController>();
            if (aiController == null) continue;
            aiController.TakeDamage(poisonDamage);
        }
    }
    #endregion

    #region Splash Damage Ability
    public void SplashDamageAbility(Vector3 hitPosition)
    {
        if (explosionEffectPrefab != null)
        {
            GameObject explosion = Instantiate(explosionEffectPrefab, hitPosition, Quaternion.identity);
            ParticleSystem ps = explosion.GetComponent<ParticleSystem>();
            Destroy(explosion, ps != null ? ps.main.duration + ps.main.startLifetime.constantMax : 2f);
        }

        Collider[] enemies = Physics.OverlapSphere(hitPosition, splashDamageRadius, LayerMask.GetMask("AI"));
        foreach (Collider col in enemies)
        {
            AIController aiController = col.GetComponent<AIController>();
            if (aiController == null) continue;
            aiController.TakeDamage(splashDamage);
        }
    }
    #endregion

    #region Gizmos
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, splashDamageRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, slowmovementRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, poisonRadius);
    }
    #endregion
}