using UnityEngine;
using UnityEngine.UI;

/// <summary>A deploy card: costs money, then goes on cooldown.</summary>
public class UnitButton : MonoBehaviour
{
    public UnitData unit;
    public Button button;
    public Image icon;
    public Text costText;
    public Image cooldownOverlay; // Image Type = Filled, Vertical

    float cooldownLeft;

    void Awake()
    {
        button.onClick.AddListener(Press);
        if (unit != null)
        {
            costText.text = $"{unit.cost}¢";
            icon.sprite = unit.icon != null ? unit.icon : unit.sprite;
            icon.preserveAspect = true;
        }
    }

    public void Press()
    {
        var bm = BattleManager.Instance;
        if (unit == null || bm == null || cooldownLeft > 0f) return;
        if (bm.TryDeploy(unit)) cooldownLeft = unit.cooldown;
    }

    void Update()
    {
        var bm = BattleManager.Instance;
        if (unit == null || bm == null) return;

        if (cooldownLeft > 0f) cooldownLeft -= Time.deltaTime;
        cooldownOverlay.fillAmount = cooldownLeft > 0f ? cooldownLeft / unit.cooldown : 0f;

        bool usable = cooldownLeft <= 0f && bm.CanAfford(unit.cost) && !bm.IsOver;
        icon.color = usable ? Color.white : new Color(0.55f, 0.55f, 0.55f);
        costText.color = bm.CanAfford(unit.cost) ? new Color(1f, 0.85f, 0.2f) : new Color(0.9f, 0.3f, 0.3f);
    }
}
