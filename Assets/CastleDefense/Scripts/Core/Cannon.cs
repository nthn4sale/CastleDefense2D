using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>The player castle's cannon: charges up, then blasts and knocks back enemies.</summary>
public class Cannon : MonoBehaviour
{
    public float chargeTime = 20f;
    public float startRemaining = 5f;
    public int damage = 80;
    public float range = 7f;
    public Sprite beamSprite;
    public Color beamColor = new Color(1f, 0.9f, 0.3f, 0.9f);

    public float Remaining { get; private set; }
    public bool IsReady => Remaining <= 0f;
    public float Charge01 => 1f - Remaining / chargeTime;

    void Awake() => Remaining = startRemaining;

    void Update()
    {
        var bm = BattleManager.Instance;
        if (bm != null && !bm.IsOver) Remaining = Mathf.Max(0f, Remaining - Time.deltaTime);
    }

    public bool Fire()
    {
        var bm = BattleManager.Instance;
        if (!IsReady || bm == null || bm.IsOver || bm.IsPaused) return false;
        Remaining = chargeTime;

        float startX = bm.playerCastle.X;
        var targets = new List<Unit>(bm.UnitsOf(Team.Enemy));
        foreach (var u in targets)
        {
            if (u.X - startX > range) continue;
            u.TakeDamage(damage);
            if (u.IsAlive) u.Knockback();
        }
        StartCoroutine(BeamEffect(startX, bm.laneY));
        return true;
    }

    IEnumerator BeamEffect(float startX, float y)
    {
        if (beamSprite == null) yield break;
        var go = new GameObject("CannonBeam");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = beamSprite;
        sr.sortingOrder = 2000;
        go.transform.position = new Vector3(startX + range * 0.5f, y + 0.5f, 0f);
        float w = beamSprite.bounds.size.x, h = beamSprite.bounds.size.y;
        const float duration = 0.4f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            go.transform.localScale = new Vector3(range / w, Mathf.Lerp(1.2f, 0.1f, k) / h, 1f);
            sr.color = new Color(beamColor.r, beamColor.g, beamColor.b, beamColor.a * (1f - k));
            yield return null;
        }
        Destroy(go);
    }
}
