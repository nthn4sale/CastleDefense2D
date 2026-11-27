using UnityEngine;

/// <summary>
/// Stats for one unit type. Create new ones with
/// Right click > Create > Castle Defense > Unit Data.
/// </summary>
[CreateAssetMenu(menuName = "Castle Defense/Unit Data", fileName = "NewUnit")]
public class UnitData : ScriptableObject
{
    [Header("Look")]
    public string displayName = "Unit";
    public Sprite sprite;
    public Sprite icon;                 // shown on the deploy button (falls back to sprite)
    public bool spriteFacesRight = true;
    public float height = 1f;           // world units, the sprite is scaled to this
    public float width = 0.8f;          // body width used for hit distance

    [Header("Shop (player units)")]
    public int cost = 100;
    public float cooldown = 2f;

    [Header("Reward (enemy units)")]
    public int moneyReward = 50;

    [Header("Combat")]
    public int maxHealth = 100;
    public int damage = 10;
    public float attackRange = 0.6f;    // gap between bodies needed to attack
    public float attackInterval = 1f;   // seconds between hits
    public bool areaAttack = false;     // hits every enemy in range
    public float moveSpeed = 1.5f;
    [Tooltip("How many times the unit is knocked back before it dies.")]
    public int knockbacks = 1;
}
