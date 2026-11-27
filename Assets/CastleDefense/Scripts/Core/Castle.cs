using System;
using UnityEngine;

/// <summary>A base. When its health reaches 0 the battle ends.</summary>
public class Castle : MonoBehaviour, IDamageable
{
    [SerializeField] Team team = Team.Player;
    public int maxHealth = 1000;
    public float halfWidth = 1.1f;
    public Transform visual; // child with the SpriteRenderer, used for shake

    public event Action<Castle> Destroyed;

    public int Health { get; private set; }
    public float HealthPercent => maxHealth > 0 ? Health / (float)maxHealth : 0f;
    public Team Team => team;
    public bool IsAlive => Health > 0;
    public float X => transform.position.x;
    public float HalfWidth => halfWidth;

    SpriteRenderer sr;
    Vector3 visualBase;
    float shakeTimer;

    void Awake()
    {
        Health = maxHealth;
        if (visual != null)
        {
            visualBase = visual.localPosition;
            sr = visual.GetComponent<SpriteRenderer>();
        }
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;
        Health = Mathf.Max(0, Health - amount);
        shakeTimer = 0.15f;
        if (Health <= 0) Destroyed?.Invoke(this);
    }

    void Update()
    {
        if (visual == null) return;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            visual.localPosition = visualBase + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.06f);
            if (sr) sr.color = new Color(1f, 0.75f, 0.75f);
        }
        else
        {
            visual.localPosition = visualBase;
            if (sr) sr.color = IsAlive ? Color.white : new Color(0.4f, 0.4f, 0.4f);
        }
    }
}
