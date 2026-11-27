using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public Castle castle;
    public Image fill;   // Image Type = Filled, Horizontal
    public Text label;

    void Update()
    {
        if (castle == null) return;
        fill.fillAmount = castle.HealthPercent;
        label.text = $"{castle.Health} / {castle.maxHealth}";
    }
}
