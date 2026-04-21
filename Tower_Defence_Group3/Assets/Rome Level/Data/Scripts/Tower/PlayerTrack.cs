using UnityEngine;

public class PlayerTrack : MonoBehaviour
{
    [Header("Tracking")]
    Transform enemy;
    float distance;
    public float maxDistance;
    public Transform player;

    [Header("Shooting")]
    public GameObject projectile;
    public Transform shootPoint;
    public float projectileSpeed = 15f;
    public float fireRate = 1f;
    private float nextFire;
    public float secsToDestroy = 5f;

    public int damage = 10;
    public int damageIncrease = 5;

    public bool isCannon = false;

    private TowerAbilities towerAbilities;

    void Start()
    {
        towerAbilities = GetComponentInParent<TowerAbilities>();
    }
    void Update()
    {
        // Find an enemy if we don't have one
        if (enemy == null)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("AI"); // Creates an array of all the enemies in the scene

            float closestDistance = Mathf.Infinity; 
            Transform closestEnemy = null;

            foreach (GameObject ai in enemies) // Loop through each enemy to find the closest one
            {
                float distance = Vector3.Distance(transform.position, ai.transform.position); // Calculate the distance from the tower to the enemy

                if (distance < closestDistance && distance <= maxDistance) // If this enemy is closer than the closest one we've found so far and within range
                {
                    closestDistance = distance;
                    closestEnemy = ai.transform;
                }
            }

            enemy = closestEnemy;
        }

        if (enemy == null)
            return;

        distance = Vector3.Distance(transform.position, enemy.position);

        if (distance < maxDistance)
        {
            if (isCannon == true)
            {
                transform.LookAt(enemy);
                transform.Rotate(0f, 90f, 0f);
            }
            else
            {
                transform.LookAt(enemy);
            }

            if (Time.time >= nextFire)
            {
                nextFire = Time.time + 1f / fireRate;
                Shoot();
            }
        }
        else
        {
            enemy = null;
        }
    }

    //Shooting method to instantiate the projectile and set its properties
    #region
    void Shoot()
    {
        GameObject bullet = Instantiate(projectile, shootPoint.position, shootPoint.rotation);

        BulletDamage bulletScript = bullet.GetComponent<BulletDamage>();

        if (bulletScript != null)
        {
            bulletScript.target = enemy;
            bulletScript.speed = projectileSpeed;
            bulletScript.damage = damage;
            bulletScript.SetTower(towerAbilities);
        }
        Destroy(bullet, secsToDestroy);
    }
    #endregion

    //deugging
    #region
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
    #endregion
}