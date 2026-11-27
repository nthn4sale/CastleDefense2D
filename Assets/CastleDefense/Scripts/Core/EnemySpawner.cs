using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns enemies from the enemy castle. Each entry is an independent stream,
/// optionally activated only after the enemy castle drops below some health.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Serializable]
    public class SpawnEntry
    {
        public UnitData unit;
        public float firstDelay = 3f;
        public float interval = 8f;
        [Tooltip("0 = unlimited")] public int count = 0;
        [Range(0f, 1f)]
        [Tooltip("Starts once the enemy castle health % is at or below this. 1 = from the start.")]
        public float startWhenCastleBelow = 1f;

        [NonSerialized] public bool active;
        [NonSerialized] public float timer;
        [NonSerialized] public int spawned;
    }

    public List<SpawnEntry> entries = new List<SpawnEntry>();

    void Update()
    {
        var bm = BattleManager.Instance;
        if (bm == null || bm.IsOver) return;

        foreach (var e in entries)
        {
            if (e.unit == null) continue;
            if (!e.active)
            {
                if (bm.enemyCastle.HealthPercent > e.startWhenCastleBelow) continue;
                e.active = true;
                e.timer = e.firstDelay;
            }
            if (e.count > 0 && e.spawned >= e.count) continue;

            e.timer -= Time.deltaTime;
            if (e.timer <= 0f)
            {
                bm.Spawn(e.unit, Team.Enemy);
                e.spawned++;
                e.timer = e.interval;
            }
        }
    }
}
