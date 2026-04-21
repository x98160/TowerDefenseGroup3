using System.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

//Buff types
#region 
[System.Flags]
public enum Buffs
{
    None = 0,
    Speed = 1 << 0,
    Commander = 1 << 1,
    SecondChance = 1 << 2,
}
#endregion

public class AIController : MonoBehaviour
{
    private AiPathfinding ai;

    [SerializeField] public Image healthBar;
    [SerializeField] public Image healthBar2;

    [Header("Avaliable Buffs")]
    [SerializeField] public Buffs buff;

    public float health = 100;
    private float maxHealth = 100;

    public int damage = 5;
    public int value = 10;

    private bool speedBuff = false;
    private bool commanderBuff = false;
    private bool secondChanceBuff = false;

    private bool isDead = false;
    private bool isReviving = false;

    [Header("Commander Buffs")]
    public int commanderRadius = 8;

    void Start()
    {
        ai = GetComponent<AiPathfinding>();
        maxHealth = health;
        UpdateUI();

        //rb = GetComponent<Rigidbody>(); //
    }

    void Update()
    {
        UpdateUI();
        Death();
        DamageTaken();

        ApplyBuff(Buffs.Commander);
        ApplyBuff(Buffs.SecondChance);
    }

    //private Rigidbody rb; //
    void Death()
    {
        //if (isReviving == true)
        //{
        //    rb.isKinematic = false; //
        //    rb.constraints = RigidbodyConstraints.None; //
        //    rb.AddForce(Vector3.forward * 4f); //
        //    rb.AddForce(Vector3.up * 20f); //
            
        //}
        
        if (health > 0 || isDead) return;

        // Prevent re-entry during revive
        if (isReviving) return;

        // SECOND CHANCE TRIGGER
        if (buff.HasFlag(Buffs.SecondChance) && !secondChanceBuff)
        {
            isReviving = true; // IMPORTANT: set immediately
            StartCoroutine(ReviveDelay());
            return;
        }

        // FINAL DEATH
        isDead = true;

        DamagePopupManager.Instance.ShowDamage(value, transform.position, Color.green);
        Currency.main.IncreaseCurrency(value);

        Debug.Log("added " + value + " to the currency");

        Destroy(gameObject);
    }

    IEnumerator ReviveDelay()
    {
        if (ai != null)
            ai.enabled = false;

        yield return StartCoroutine(DeathAnimation());
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(ReviveAnimation());

        SecondChance();

        // small invulnerability buffer to avoid instant re-death
        yield return new WaitForSeconds(0.2f);

        isReviving = false;
    }

    void DamageTaken()
    {
        if (health <= 50)
        {
            ApplyBuff(Buffs.Speed);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isReviving || isDead) return;

        health -= amount;
        health = Mathf.Max(health, 0f);

        DamagePopupManager.Instance.ShowDamage(amount, transform.position);
    }

    //Buff Logic
    #region 
    void ApplyBuff(Buffs avaliableBuffs)
    {
        if (!buff.HasFlag(avaliableBuffs))
            return;

        switch (avaliableBuffs)
        {
            case Buffs.Speed:
                if (speedBuff) return;
                SpeedBuff();
                break;

            case Buffs.Commander:
                if (commanderBuff) return;
                CommanderBuff();
                break;

            case Buffs.SecondChance:
                // handled in Death()
                break;
        }
    }

    void SpeedBuff()
    {
        if (speedBuff) return;

        speedBuff = true;

        if (ai != null)
        {
            ai.ApplySpeedBuff();
        }
    }

    void CommanderBuff()
    {
        // FIXED FLAG CHECK
        if (!buff.HasFlag(Buffs.Commander)) return;

        foreach (Collider hit in Physics.OverlapSphere(transform.position, commanderRadius))
        {
            AIController aiTarget = hit.GetComponentInParent<AIController>();

            if (aiTarget == null) continue;
            if (aiTarget == this) continue;
            if (aiTarget.commanderBuff) continue;

            if (!aiTarget.buff.HasFlag(Buffs.Commander))
            {
                aiTarget.health += 10;
                aiTarget.commanderBuff = true;
                Debug.Log("healed");
            }
        }
    }

    void SecondChance()
    {
        secondChanceBuff = true;

        isDead = false;

        if (ai != null && ai.currentDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(ai.currentDirection);
            ai.enabled = true;
        }

        health = maxHealth * 0.5f;
    }
    #endregion

    //debugging
    #region
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, commanderRadius);
    }
    #endregion

    //UI logic
    #region
    void UpdateUI()
    {
        if (healthBar == null || healthBar2 == null)
        {
            Debug.LogWarning("Health bars not assigned!");
            return;
        }

        float ratio = health / maxHealth;
        healthBar.fillAmount = ratio;
        healthBar2.fillAmount = ratio;
    }
    #endregion

    // death and revive animations
    #region
    IEnumerator DeathAnimation()
    {
        float duration = 0.4f;
        float time = 0f;

        Quaternion startRot = transform.rotation;

        // KEEP current Y rotation
        float y = transform.eulerAngles.y;
        Quaternion endRot = Quaternion.Euler(90f, y, 0f);

        Vector3 startScale = transform.localScale;
        Vector3 endScale = new Vector3(1f, 0.8f, 1f);

        Renderer renderer = GetComponentInChildren<Renderer>();
        Color startColor = renderer != null ? renderer.material.color : Color.white;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0.5f);

        while (time < duration)
        {
            float t = time / duration;

            transform.rotation = Quaternion.Lerp(startRot, endRot, t);
            transform.localScale = Vector3.Lerp(startScale, endScale, t);

            if (renderer != null)
                renderer.material.color = Color.Lerp(startColor, endColor, t);

            time += Time.deltaTime;
            yield return null;
        }

        transform.rotation = endRot;
        transform.localScale = endScale;
    }

    IEnumerator ReviveAnimation()
    {
        float duration = 0.4f;
        float time = 0f;

        Quaternion startRot = transform.rotation;
        float y = transform.eulerAngles.y;
        Quaternion endRot = Quaternion.Euler(0f, y, 0f);

        Vector3 startScale = transform.localScale;
        Vector3 endScale = Vector3.one;

        Renderer renderer = GetComponentInChildren<Renderer>();
        Color startColor = renderer != null ? renderer.material.color : Color.white;
        Color endColor = new Color(1f, 0f, 0f, 0.8f);

        while (time < duration)
        {
            float t = time / duration;

            transform.rotation = Quaternion.Lerp(startRot, endRot, t);
            transform.localScale = Vector3.Lerp(startScale, endScale, t);

            if (renderer != null)
                renderer.material.color = Color.Lerp(startColor, endColor, t);

            time += Time.deltaTime;
            yield return null;
        }

        transform.rotation = endRot;
        transform.localScale = endScale;
    }
    #endregion
}