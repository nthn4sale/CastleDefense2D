using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Top bar, pause menu, result screen and keyboard shortcuts.</summary>
public class BattleUI : MonoBehaviour
{
    [Header("Top bar")]
    public Text stageText;
    public Button pauseButton;
    public Button speedButton;
    public Image speedButtonImage;

    [Header("Pause")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button pauseRestartButton;

    [Header("Result")]
    public GameObject resultPanel;
    public Text resultText;
    public Button resultRestartButton;

    [Header("Shortcuts (1-5 = units, Q = wallet, Space = cannon, Esc = pause, F = speed)")]
    public UnitButton[] unitButtons;
    public WalletButton walletButton;
    public CannonButton cannonButton;

    static readonly Color SpeedOff = new Color(1f, 0.8f, 0.15f);
    static readonly Color SpeedOn = new Color(1f, 0.45f, 0.1f);

    void Start()
    {
        var bm = BattleManager.Instance;
        stageText.text = bm.stageName;
        pausePanel.SetActive(false);
        resultPanel.SetActive(false);

        pauseButton.onClick.AddListener(() => SetPaused(true));
        resumeButton.onClick.AddListener(() => SetPaused(false));
        speedButton.onClick.AddListener(ToggleSpeed);
        pauseRestartButton.onClick.AddListener(Restart);
        resultRestartButton.onClick.AddListener(Restart);
        bm.BattleEnded += OnBattleEnded;
    }

    void SetPaused(bool paused)
    {
        var bm = BattleManager.Instance;
        if (bm.IsOver) return;
        bm.SetPaused(paused);
        pausePanel.SetActive(paused);
    }

    void ToggleSpeed()
    {
        var bm = BattleManager.Instance;
        bm.ToggleFast();
        speedButtonImage.color = bm.IsFast ? SpeedOn : SpeedOff;
    }

    void OnBattleEnded(bool victory)
    {
        pausePanel.SetActive(false);
        resultPanel.SetActive(true);
        resultText.text = victory ? "VICTORY!" : "DEFEAT...";
        resultText.color = victory ? new Color(1f, 0.85f, 0.2f) : new Color(0.9f, 0.3f, 0.3f);
    }

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void Update()
    {
        var bm = BattleManager.Instance;
        if (bm == null || bm.IsOver) return;

        if (KeyDown(Key.Escape)) SetPaused(!bm.IsPaused);
        if (bm.IsPaused) return;
        if (KeyDown(Key.F)) ToggleSpeed();
        if (KeyDown(Key.Q) && walletButton) walletButton.Press();
        if (KeyDown(Key.Space) && cannonButton) cannonButton.Press();
        for (int i = 0; i < unitButtons.Length && i < 5; i++)
            if (KeyDown(Key.Digit1 + i)) unitButtons[i].Press();
    }

    // Works with both the old Input Manager and the new Input System.
    enum Key { Escape, F, Q, Space, Digit1, Digit2, Digit3, Digit4, Digit5 }

    static bool KeyDown(Key k)
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return false;
        switch (k)
        {
            case Key.Escape: return kb.escapeKey.wasPressedThisFrame;
            case Key.F: return kb.fKey.wasPressedThisFrame;
            case Key.Q: return kb.qKey.wasPressedThisFrame;
            case Key.Space: return kb.spaceKey.wasPressedThisFrame;
            case Key.Digit1: return kb.digit1Key.wasPressedThisFrame;
            case Key.Digit2: return kb.digit2Key.wasPressedThisFrame;
            case Key.Digit3: return kb.digit3Key.wasPressedThisFrame;
            case Key.Digit4: return kb.digit4Key.wasPressedThisFrame;
            case Key.Digit5: return kb.digit5Key.wasPressedThisFrame;
        }
        return false;
#else
        switch (k)
        {
            case Key.Escape: return Input.GetKeyDown(KeyCode.Escape);
            case Key.F: return Input.GetKeyDown(KeyCode.F);
            case Key.Q: return Input.GetKeyDown(KeyCode.Q);
            case Key.Space: return Input.GetKeyDown(KeyCode.Space);
            default: return Input.GetKeyDown(KeyCode.Alpha1 + (k - Key.Digit1));
        }
#endif
    }
}
