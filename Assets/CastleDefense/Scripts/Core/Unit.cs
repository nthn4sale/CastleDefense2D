using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A soldier that walks toward the enemy castle, stops at the first enemy
/// in range and attacks it. Created at runtime by BattleManager.Spawn.
/// </summary>
public class Unit : MonoBehaviour, IDamageable
{
    enum State { Walking, Attacking, KnockedBack, Dead }

    public UnitData Data { get; private set; }
    public Team Team { get; private set; }
    public int Health { get; private set; }
    public bool IsAlive => Health > 0;
    public float X => transform.position.x;
    public float HalfWidth => Data.width * 0.5f;
    float Dir => Team == Team.Player ? 1f : -1f;

    State state;
    float attackTimer;
    int knockbacksTaken;
    float animTime, lungeTimer, flashTimer;
    SpriteRenderer sr;
    Transform visual;
    Vector3 visualBase;

    static readonly List<IDamageable> hitBuffer = new List<IDamageable>();

    public void Init(UnitData data, Team team)
    {
        Data = data;
        Team = team;
        Health = data.maxHealth;
        attackTimer = 0.3f; // short wind-up before the first hit

        visual = new GameObject("Visual").transform;
        visual.SetParent(transform, false);
        sr = visual.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = data.sprite;
        sr.flipX = (team == Team.Player) != data.spriteFacesRight;
        sr.sortingOrder = 500 - Mathf.RoundToInt(transform.position.y * 100f);

        if (data.sprite != null)
        {
            // scale sprite to the wanted height and put its bottom on our feet
            float s = data.height / data.sprite.bounds.size.y;
            visual.localScale = new Vector3(s, s, 1f);
            visualBase = new Vector3(0f, -data.sprite.bounds.min.y * s, 0f);
            visual.localPosition = visualBase;
        }

        animTime = Random.value * 10f;
        BattleManager.Instance.Register(this);
    }

    void Update()
    {
        if (state == State.Dead || state == State.KnockedBack) return;
        var bm = BattleManager.Instance;
        float dt = Time.deltaTime;
        animTime += dt;

        if (bm == null || bm.IsOver)
        {
            state = State.Attacking; // stand still
            Animate(dt);
            return;
        }

        attackTimer -= dt;
        var target = bm.FindClosestTarget(this, Data.attackRange);
        if (target != null)
        {
            state = State.Attacking;
            if (attackTimer <= 0f)
            {
                Attack(target);
                attackTimer = Data.attackInterval;
            }
        }
        else
        {
            state = State.Walking;
            attackTimer = Mathf.Max(attackTimer, 0.3f);
            transform.position += Vector3.right * (Dir * Data.moveSpeed * dt);
        }
        Animate(dt);
    }

    void Attack(IDamageable primary)
    {
        lungeTimer = 0.2f;
        if (Data.areaAttack)
        {
            hitBuffer.Clear();
            BattleManager.Instance.CollectTargets(this, Data.attackRange, hitBuffer);
            foreach (var t in hitBuffer) t.TakeDamage(Data.damage);
        }
        else
        {
            primary.TakeDamage(Data.damage);
        }
    }

    void Animate(float dt)
    {
        float bob = state == State.Walking ? Mathf.Abs(Mathf.Sin(animTime * 12f)) * 0.08f : 0f;
        float lunge = 0f;
        if (lungeTimer > 0f)
        {
            lungeTimer -= dt;
            lunge = Mathf.Sin((1f - lungeTimer / 0.2f) * Mathf.PI) * 0.25f;
        }
        visual.localPosition = visualBase + new Vector3(lunge * Dir, bob, 0f);

        if (flashTimer > 0f) flashTimer -= dt;
        sr.color = flashTimer > 0f ? new Color(1f, 0.45f, 0.45f) : Color.white;
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;
        Health = Mathf.Max(0, Health - amount);
        flashTimer = 0.1f;

        if (Health <= 0)
        {
            StartCoroutine(DieRoutine());
            return;
        }

        // knockback when health crosses a threshold (e.g. 2 knockbacks -> at 66% and 33%)
        if (Data.knockbacks > 0)
        {
            float step = Data.maxHealth / (float)(Data.knockbacks + 1);
            int crossed = Mathf.Min(Data.knockbacks, Mathf.FloorToInt((Data.maxHealth - Health) / step));
            if (crossed > knockbacksTaken)
            {
                knockbacksTaken = crossed;
                Knockback();
            }
        }
    }

    public void Knockback()
    {
        if (state == State.Dead || state == State.KnockedBack) return;
        StartCoroutine(KnockbackRoutine());
    }

    IEnumerator KnockbackRoutine()
    {
        state = State.KnockedBack;
        const float duration = 0.45f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            var p = transform.position;
            p.x = BattleManager.Instance.ClampToField(Team, p.x - Dir * 3f * Time.deltaTime);
            transform.position = p;
            visual.localPosition = visualBase + Vector3.up * (Mathf.Sin(t / duration * Mathf.PI) * 0.4f);
            if (flashTimer > 0f) flashTimer -= Time.deltaTime;
            sr.color = flashTimer > 0f ? new Color(1f, 0.45f, 0.45f) : Color.white;
            yield return null;
        }
        if (state == State.KnockedBack) state = State.Walking;
    }

    IEnumerator DieRoutine()
    {
        state = State.Dead;
        BattleManager.Instance.OnUnitDied(this);
        const float duration = 0.6f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;
            transform.position += new Vector3(-Dir * 2f, 2.5f, 0f) * Time.deltaTime;
            visual.localRotation = Quaternion.Euler(0f, 0f, Dir * 360f * k);
            sr.color = new Color(1f, 1f, 1f, 1f - k);
            yield return null;
        }
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (BattleManager.Instance != null) BattleManager.Instance.Unregister(this);
    }
}
