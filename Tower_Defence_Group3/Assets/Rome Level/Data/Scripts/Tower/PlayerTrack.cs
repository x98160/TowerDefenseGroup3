using UnityEngine;
using System.Collections;

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
    [Header("Audio")]
    public AudioClip ShootSound;
    public float volume = 1f;
    private AudioSource audioSource;

    void Start()
    {
        towerAbilities = GetComponentInParent<TowerAbilities>();

        audioSource = GetComponent<AudioSource>();
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (enemy == null)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("AI");
            float closestDistance = Mathf.Infinity;
            Transform closestEnemy = null;

            foreach (GameObject ai in enemies)
            {
                float distance = Vector3.Distance(transform.position, ai.transform.position);
                if (distance < closestDistance && distance <= maxDistance)
                {
                    closestDistance = distance;
                    closestEnemy = ai.transform;
                }
            }
            enemy = closestEnemy;
        }

        if (enemy == null) return;

        distance = Vector3.Distance(transform.position, enemy.position);

        if (distance < maxDistance)
        {
            if (isCannon)
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
                nextFire = Time.time + Mathf.Max(1f / fireRate, Time.deltaTime);
                StartCoroutine(ShootNextFrame());
            }
        }
        else
        {
            enemy = null;
        }
    }

    IEnumerator ShootNextFrame()
    {
        yield return null; // Wait one frame for transform to update
        Shoot();
    }

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

        audioSource.PlayOneShot(ShootSound, volume);

        Destroy(bullet, secsToDestroy);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}