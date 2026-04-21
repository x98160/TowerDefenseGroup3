using UnityEngine;

public class BulletDamage : MonoBehaviour
{
    public Transform target;
    public float speed = 15f;
    public int damage = 10;
    public bool isBomb = false;
    private TowerAbilities towerAbilities;
    private bool hasHit = false;
    public void SetTower(TowerAbilities tower)
    {
        towerAbilities = tower;
    }
    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        transform.LookAt(target);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        AIController ai = other.GetComponentInParent<AIController>();
        if (ai == null) return;

        hasHit = true;
        if (isBomb)
        {
            towerAbilities.SplashDamageAbility(transform.position);
        }
        else
        {
            ai.TakeDamage(damage);
        }
        Destroy(gameObject);
    }
}