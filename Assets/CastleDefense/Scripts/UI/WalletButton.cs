using UnityEngine;
using UnityEngine.UI;

/// <summary>Upgrades the wallet: higher max money and faster income.</summary>
public class WalletButton : MonoBehaviour
{
    public Button button;
    public Image icon;
    public Text levelText;
    public Text costText;

    void Awake() => button.onClick.AddListener(Press);

    public void Press() => BattleManager.Instance?.TryUpgradeWallet();

    void Update()
    {
        var bm = BattleManager.Instance;
        if (bm == null) return;
        levelText.text = bm.CanUpgradeWallet ? $"Level {bm.WalletLevel + 1}" : "Level MAX";
        costText.text = bm.CanUpgradeWallet ? $"{bm.WalletUpgradeCost}¢" : "MAX";
        bool usable = bm.CanUpgradeWallet && bm.CanAfford(bm.WalletUpgradeCost);
        icon.color = usable ? Color.white : new Color(0.65f, 0.65f, 0.65f);
    }
}
