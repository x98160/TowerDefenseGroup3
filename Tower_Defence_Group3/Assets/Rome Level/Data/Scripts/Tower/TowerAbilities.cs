using UnityEngine;

public class TowerAbilities : MonoBehaviour
{

    private bool multishotAbility = false;
    private bool slowmovementAbility = false;
    private bool poisonAbility = false;
    private bool splashdamageAbility = false;
    [SerializeField] private float poisonDamage = 3f; // damage per second for the poison ability, can be adjusted in the inspector
    [SerializeField] private float slowmovementSpeed = 0.7f;
    [SerializeField] private float splashDamage = 15f;
    [SerializeField] private float splashDamageRadius = 5f;
    [SerializeField] private GameObject explosionEffectPrefab;

    [SerializeField] public TowerData towerData;

    private Abilities ability;

    public void Start()
    {
        if(towerData != null && towerData.objectsData.Count > 0)
        {
            ApplyAbility(towerData.objectsData[0].Ability);
        }

    }

    public void Update()
    {
        if (slowmovementAbility)
        {
            SlowMovement();
        }

        if (poisonAbility)
        {
            PoisonAbility();
        }

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

                break;


            case Abilities.splashdamageAbility:
                if (splashdamageAbility) return;
                splashdamageAbility = true;

                break;


        }
    }

    public void MultiShotAbility()
    {
        Debug.Log("testing Multishot");
        multishotAbility = true;
    }

    // Slow movement ability will slow down the movement speed of all enemies within a certain radius of the tower. The radius and slow down percentage can be adjusted in the inspector of the player track script.
    #region Slow movement ability
    public void SlowMovement()
    {
        PlayerTrack playerTrack = FindAnyObjectByType<PlayerTrack>();
        float maxDistance = playerTrack.maxDistance;
        Debug.Log("testing slowmovement");


        GameObject[] enemies = GameObject.FindGameObjectsWithTag("AI");

        foreach (GameObject ai in enemies)
        {
            AiPathfinding aiPathFinding = ai.GetComponent<AiPathfinding>();
            if (aiPathFinding == null) continue;

            float distance = Vector3.Distance(transform.position, ai.transform.position);

            if (distance <= maxDistance)
            {
                aiPathFinding.speed = aiPathFinding.originalSpeed * slowmovementSpeed;
            }
            else
            {
                aiPathFinding.speed = aiPathFinding.originalSpeed;
            }
        }
        slowmovementAbility = true;

    }
    #endregion

    // Poison ability will apply damage over time to all enemies within a certain radius of the tower. The damage per second can be adjusted in the inspector of this script.
    #region Posion ability
    public void PoisonAbility()
    {
        PlayerTrack playerTrack = FindAnyObjectByType<PlayerTrack>();
        float maxDistance = playerTrack.maxDistance;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("AI");

        foreach (GameObject ai in enemies)
        {

            float distance = Vector3.Distance(transform.position, ai.transform.position);

            if(distance< maxDistance)
            {
                AIController aiController = ai.GetComponent<AIController>();
                aiController.TakeDamage(poisonDamage * Time.deltaTime);
            }
            Debug.Log("testing poison");
            poisonAbility = true;
        }
    }

    #endregion

    #region Splash Damage ability
    public void SplashDamageAbility(Vector3 hitPosition)
    {
        // Spawn explosion effect at hit position
        if (explosionEffectPrefab != null)
        {

            GameObject explosion =Instantiate(explosionEffectPrefab, hitPosition, Quaternion.identity);
            ParticleSystem ps = explosion.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(explosion, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(explosion, 2f);
            }

        }

        // Damage all enemies in radius
        Collider[] enemies = Physics.OverlapSphere(hitPosition, splashDamageRadius, LayerMask.GetMask("AI"));

        foreach (Collider col in enemies)
        {
            AIController aiController = col.GetComponent<AIController>();
            if (aiController == null) continue;
            aiController.TakeDamage(splashDamage);
        }
    }

    // Visualise splash radius in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, splashDamageRadius);
    }
    #endregion
}
