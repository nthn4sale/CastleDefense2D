using UnityEngine;

public enum Team
{
    Player, // walks to the right
    Enemy   // walks to the left
}

/// <summary>Anything that can be hit: units and castles.</summary>
public interface IDamageable
{
    Team Team { get; }
    bool IsAlive { get; }
    float X { get; }
    float HalfWidth { get; }
    void TakeDamage(int amount);
}
