using UnityEngine;
using System.Collections;

public class TowerAbilities : MonoBehaviour
{
    private bool multishotAbility = false;
    private bool slowmovementAbility = false;
    private bool poisonAbility = false;
    private bool splashdamageAbility = false;

    [SerializeField] private float poisonDamage = 3f;
    [SerializeField] private float poisonRadius = 10f;
    [SerializeField] private float poisonTickRate = 1f; // NEW: delay between poison ticks
    [SerializeField] private float slowmovementSpeed = 0.7f;
    [SerializeField] private float slowmovementRadius = 10f;
    [SerializeField] private float splashDamage = 15f;
    [SerializeField] private float splashDamageRadius = 5f;
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] public TowerData towerData;

    private Abilities ability;
    private static TowerAbilitiesManager _manager;

    void Start()
    {
        if (_manager == null)
        {
            GameObject managerObj = new GameObject("TowerAbilitiesManager");
            _manager = managerObj.AddComponent<TowerAbilitiesManager>();
            DontDestroyOnLoad(managerObj);
        }

        if (towerData != null && towerData.objectsData.Count > 0)
        {
            foreach (var obj in towerData.objectsData)
                ApplyAbility(obj.Ability);
        }
    }

    void Update()
    {
        if (slowmovementAbility) SlowMovement();
        // Poison is now handled by coroutine, removed from Update
    }

    public void ApplyAbility(Abilities ability)
    {
        switch (ability)
        {
            case Abilities.multishotAbility:
                if (multishotAbility) return;
                MultiShotAbility();
                break;

            case Abilities.slowmovementAbility:
                if (slowmovementAbility) return;
                slowmovementAbility = true;
                break;

            case Abilities.poisonAbility:
                if (poisonAbility) return;
                poisonAbility = true;
                StartCoroutine(PoisonCoroutine()); // NEW: start the coroutine
                break;

            case Abilities.splashdamageAbility:
                if (splashdamageAbility) return;
                splashdamageAbility = true;
                break;
        }
    }

    public void MultiShotAbility()
    {
        multishotAbility = true;
    }

    #region Slow Movement Ability
    public void SlowMovement()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, slowmovementRadius, LayerMask.GetMask("AI"));

        foreach (Collider col in enemies)
        {
            AiPathfinding aiPathfinding = col.GetComponent<AiPathfinding>();
            if (aiPathfinding == null) continue;
            aiPathfinding.speed = aiPathfinding.originalSpeed * slowmovementSpeed;
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
            aiController.TakeDamage(poisonDamage); // No longer multiplied by Time.deltaTime
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

    private class TowerAbilitiesManager : MonoBehaviour
    {
        void LateUpdate()
        {
            GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("AI");
            foreach (GameObject ai in allEnemies)
            {
                AiPathfinding aiPathfinding = ai.GetComponent<AiPathfinding>();
                if (aiPathfinding == null) continue;
                aiPathfinding.speed = aiPathfinding.originalSpeed;
            }
        }
    }
}