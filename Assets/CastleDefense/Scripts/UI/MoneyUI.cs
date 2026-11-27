using UnityEngine;
using UnityEngine.UI;

public class MoneyUI : MonoBehaviour
{
    public Text label;

    void Update()
    {
        var bm = BattleManager.Instance;
        if (bm == null) return;
        label.text = $"{Mathf.FloorToInt(bm.Money)}/{bm.MaxMoney}¢";
    }
}
