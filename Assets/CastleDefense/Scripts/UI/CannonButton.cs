using UnityEngine;
using UnityEngine.UI;

public class CannonButton : MonoBehaviour
{
    public Cannon cannon;
    public Button button;
    public Image icon;
    public Image chargeOverlay; // Image Type = Filled, Radial 360
    public Text label;

    void Awake() => button.onClick.AddListener(Press);

    public void Press() => cannon.Fire();

    void Update()
    {
        if (cannon == null) return;
        chargeOverlay.fillAmount = cannon.IsReady ? 0f : 1f - cannon.Charge01;
        label.text = cannon.IsReady ? "FIRE!" : $"Ready in\n{cannon.Remaining:0.0}s";
        icon.color = cannon.IsReady ? Color.white : new Color(0.6f, 0.6f, 0.6f);
    }
}
