using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runs the battle: money/wallet, spawning, target search, pause/speed, win/lose.
/// </summary>
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("Stage")]
    public string stageName = "Stage 1-1";
    public Castle playerCastle;
    public Castle enemyCastle;
    public float laneY = -1.5f;
    public float laneJitter = 0.2f;

    [Header("Wallet (index = level - 1)")]
    public float startingMoney = 250f;
    public int[] walletMax = { 1000, 1500, 2000, 2500, 3000, 3500, 4000, 5000 };
    public float[] incomePerSecond = { 25, 35, 45, 55, 65, 75, 85, 100 };
    [Tooltip("Cost to go from level i+1 to level i+2.")]
    public int[] upgradeCost = { 100, 200, 300, 400, 500, 600, 700 };

    public float Money { get; private set; }
    public int WalletLevel { get; private set; } // 0-based
    public int MaxMoney => walletMax[WalletLevel];
    public bool CanUpgradeWallet => WalletLevel < walletMax.Length - 1;
    public int WalletUpgradeCost => CanUpgradeWallet ? upgradeCost[WalletLevel] : 0;

    public bool IsOver { get; private set; }
    public bool Victory { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsFast { get; private set; }

    public event Action<bool> BattleEnded; // true = victory

    readonly List<Unit> playerUnits = new List<Unit>();
    readonly List<Unit> enemyUnits = new List<Unit>();

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        Money = startingMoney;
    }

    void Start()
    {
        playerCastle.Destroyed += OnCastleDestroyed;
        enemyCastle.Destroyed += OnCastleDestroyed;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (IsOver) return;
        Money = Mathf.Min(MaxMoney, Money + incomePerSecond[WalletLevel] * Time.deltaTime);
    }

    // ---------- money ----------

    public bool CanAfford(int amount) => Money >= amount;

    public bool TrySpend(int amount)
    {
        if (IsOver || IsPaused || Money < amount) return false;
        Money -= amount;
        return true;
    }

    public bool TryUpgradeWallet()
    {
        if (!CanUpgradeWallet || !TrySpend(WalletUpgradeCost)) return false;
        WalletLevel++;
        return true;
    }

    public bool TryDeploy(UnitData data)
    {
        if (!TrySpend(data.cost)) return false;
        Spawn(data, Team.Player);
        return true;
    }

    // ---------- units ----------

    public Unit Spawn(UnitData data, Team team)
    {
        if (IsOver || data == null) return null;
        var castle = team == Team.Player ? playerCastle : enemyCastle;
        float dir = team == Team.Player ? 1f : -1f;
        var pos = new Vector3(castle.X + dir * castle.halfWidth * 0.7f,
                              laneY + UnityEngine.Random.Range(-laneJitter, laneJitter), 0f);
        var go = new GameObject(data.displayName);
        go.transform.position = pos;
        var unit = go.AddComponent<Unit>();
        unit.Init(data, team);
        return unit;
    }

    public void Register(Unit u) => ListFor(u.Team).Add(u);
    public void Unregister(Unit u) => ListFor(u.Team).Remove(u);
    public IReadOnlyList<Unit> UnitsOf(Team team) => ListFor(team);
    List<Unit> ListFor(Team team) => team == Team.Player ? playerUnits : enemyUnits;

    public void OnUnitDied(Unit u)
    {
        Unregister(u);
        if (u.Team == Team.Enemy && !IsOver)
            Money = Mathf.Min(MaxMoney, Money + u.Data.moneyReward);
    }

    /// <summary>Gap between the attacker's front and the target's body (negative = overlapping).</summary>
    static float Gap(Unit from, IDamageable t, float dir) => (t.X - from.X) * dir - t.HalfWidth - from.HalfWidth;

    static bool InRange(Unit from, IDamageable t, float range, float dir)
    {
        if (!t.IsAlive) return false;
        if ((t.X - from.X) * dir < -0.3f) return false; // target is behind us
        return Gap(from, t, dir) <= range;
    }

    public IDamageable FindClosestTarget(Unit from, float range)
    {
        float dir = from.Team == Team.Player ? 1f : -1f;
        IDamageable best = null;
        float bestGap = float.MaxValue;
        foreach (var u in ListFor(Opposite(from.Team)))
        {
            if (!InRange(from, u, range, dir)) continue;
            float g = Gap(from, u, dir);
            if (g < bestGap) { bestGap = g; best = u; }
        }
        var castle = from.Team == Team.Player ? enemyCastle : playerCastle;
        if (best == null && InRange(from, castle, range, dir)) best = castle;
        return best;
    }

    public void CollectTargets(Unit from, float range, List<IDamageable> results)
    {
        float dir = from.Team == Team.Player ? 1f : -1f;
        foreach (var u in ListFor(Opposite(from.Team)))
            if (InRange(from, u, range, dir)) results.Add(u);
        var castle = from.Team == Team.Player ? enemyCastle : playerCastle;
        if (InRange(from, castle, range, dir)) results.Add(castle);
    }

    /// <summary>Keeps knocked-back units from flying behind their own castle.</summary>
    public float ClampToField(Team team, float x)
    {
        return team == Team.Player ? Mathf.Max(x, playerCastle.X) : Mathf.Min(x, enemyCastle.X);
    }

    static Team Opposite(Team t) => t == Team.Player ? Team.Enemy : Team.Player;

    // ---------- flow ----------

    public void SetPaused(bool paused)
    {
        if (IsOver) return;
        IsPaused = paused;
        ApplyTimeScale();
    }

    public void ToggleFast()
    {
        IsFast = !IsFast;
        ApplyTimeScale();
    }

    void ApplyTimeScale() => Time.timeScale = IsPaused ? 0f : (IsFast ? 2f : 1f);

    void OnCastleDestroyed(Castle c)
    {
        if (IsOver) return;
        IsOver = true;
        Victory = c.Team == Team.Enemy;
        IsPaused = false;
        Time.timeScale = 1f;

        // the losing side's units are knocked away
        var losers = new List<Unit>(ListFor(c.Team));
        foreach (var u in losers) u.TakeDamage(int.MaxValue / 2);

        BattleEnded?.Invoke(Victory);
    }
}
