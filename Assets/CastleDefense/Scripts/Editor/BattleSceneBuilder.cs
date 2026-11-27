using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Menu: Castle Defense > Build Battle Scene.
/// Generates placeholder sprites, UnitData assets and a complete battle scene.
/// Safe to run again: existing sprites and UnitData assets are NOT overwritten,
/// so you can replace the PNGs with your own art and tweak stats in the Inspector.
/// </summary>
public static class BattleSceneBuilder
{
    const string Root = "Assets/CastleDefense";
    const string ArtDir = Root + "/Art";
    const string UnitDir = Root + "/Units";
    const string SceneDir = Root + "/Scenes";
    const string ScenePath = SceneDir + "/Battle.unity";

    static Font font;

    [MenuItem("Castle Defense/Build Battle Scene")]
    public static void Build()
    {
        Directory.CreateDirectory(ArtDir);
        Directory.CreateDirectory(UnitDir);
        Directory.CreateDirectory(SceneDir);
        AssetDatabase.Refresh();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ---------- art ----------
        var pixel = MakeSprite("Pixel", 4, 4, (x, y) => Color.white, new Vector2(0.5f, 0.5f), 4);
        var circle = MakeSprite("Circle", 128, 128, (x, y) => Disc(x, y, 64, 64, 63), new Vector2(0.5f, 0.5f), 128);
        var roundBox = MakeSprite("UI_RoundBox", 64, 64, (x, y) => RoundBoxPixel(x, y, 64, 64, 20, 0), new Vector2(0.5f, 0.5f), 100, 24);
        var cardBox = MakeSprite("UI_CardBox", 64, 64, (x, y) => RoundBoxPixel(x, y, 64, 64, 20, 6), new Vector2(0.5f, 0.5f), 100, 24);
        var uiCircle = MakeSprite("UI_Circle", 128, 128, (x, y) => OutlinedDisc(x, y, 64, 64, 62, 6), new Vector2(0.5f, 0.5f), 128);
        var iconPause = MakeSprite("Icon_Pause", 64, 64, PausePixel, new Vector2(0.5f, 0.5f), 64);
        var iconFast = MakeSprite("Icon_Fast", 64, 64, FastPixel, new Vector2(0.5f, 0.5f), 64);
        var iconCannon = MakeSprite("Icon_Cannon", 128, 128, CannonPixel, new Vector2(0.5f, 0.5f), 128);
        var iconWallet = MakeSprite("Icon_Wallet", 128, 128, WalletPixel, new Vector2(0.5f, 0.5f), 128);
        var castleBlue = MakeSprite("Castle_Player", 256, 320, (x, y) => CastlePixel(x, y, 256, 320,
            new Color(0.72f, 0.74f, 0.78f), new Color(0.2f, 0.4f, 0.9f)), new Vector2(0.5f, 0f), 100);
        var castleRed = MakeSprite("Castle_Enemy", 256, 320, (x, y) => CastlePixel(x, y, 256, 320,
            new Color(0.93f, 0.82f, 0.62f), new Color(0.85f, 0.15f, 0.15f)), new Vector2(0.5f, 0f), 100);

        Sprite Char(string n, int w, int h, Color body, Color accent, Color eyes) =>
            MakeSprite(n, w, h, (x, y) => CharacterPixel(x, y, w, h, body, accent, eyes), new Vector2(0.5f, 0f), 100);

        var sKnight = Char("Unit_Knight", 110, 120, new Color(0.85f, 0.87f, 0.92f), new Color(0.55f, 0.58f, 0.66f), Color.black);
        var sCavalry = Char("Unit_Cavalry", 170, 140, new Color(0.35f, 0.5f, 0.9f), new Color(0.62f, 0.64f, 0.72f), Color.black);
        var sWizard = Char("Unit_Wizard", 110, 150, new Color(0.3f, 0.42f, 0.85f), new Color(0.15f, 0.2f, 0.55f), Color.black);
        var sKing = Char("Unit_King", 130, 170, new Color(0.85f, 0.2f, 0.2f), new Color(1f, 0.8f, 0.2f), Color.black);
        var sDoggo = Char("Enemy_Doggo", 120, 95, new Color(0.98f, 0.98f, 0.98f), new Color(0.98f, 0.98f, 0.98f), Color.black);
        var sDark = Char("Enemy_DarkKnight", 120, 140, new Color(0.3f, 0.3f, 0.36f), new Color(0.8f, 0.15f, 0.15f), new Color(1f, 0.3f, 0.2f));

        // ---------- unit stats ----------
        //                         name         sprite   cost  cd  hp  dmg range  interval area  speed kb  height width reward
        var knight = MakeUnit("Knight", sKnight, 100, 2f, 150, 18, 0.5f, 1.0f, false, 1.6f, 2, 0.9f, 0.7f, 0);
        var cavalry = MakeUnit("Cavalry", sCavalry, 200, 4f, 240, 30, 0.9f, 1.2f, false, 3.0f, 2, 1.3f, 1.2f, 0);
        var wizard = MakeUnit("Wizard", sWizard, 300, 6f, 110, 28, 3.0f, 1.8f, true, 1.1f, 1, 1.25f, 0.7f, 0);
        var king = MakeUnit("King", sKing, 500, 15f, 900, 70, 0.8f, 2.0f, true, 0.9f, 3, 1.5f, 0.9f, 0);
        var doggo = MakeUnit("Doggo", sDoggo, 0, 0f, 90, 10, 0.4f, 1.0f, false, 1.4f, 1, 0.75f, 0.8f, 40);
        var darkKnight = MakeUnit("Dark Knight", sDark, 0, 0f, 450, 35, 0.6f, 1.5f, false, 1.0f, 3, 1.2f, 0.8f, 150);

        // ---------- scene ----------
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.36f, 0.68f, 0.96f);
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        camGo.AddComponent<CameraFit>();
        camGo.AddComponent<UniversalAdditionalCameraData>();

        // URP 2D: sprites use the Sprite-Lit material, so the scene needs a light or everything is black
        var lightGo = new GameObject("Global Light 2D");
        var light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
        light.color = Color.white;

        var env = new GameObject("Environment").transform;
        // clouds
        foreach (var c in new[] { new Vector3(-4.5f, 3.2f), new Vector3(2.5f, 3.6f), new Vector3(6.5f, 2.4f), new Vector3(-1f, 1.6f) })
            for (int i = 0; i < 3; i++)
                Quad("Cloud", env, circle, Color.white, c + new Vector3(i * 0.6f - 0.6f, i == 1 ? 0.25f : 0f), new Vector2(i == 1 ? 1.2f : 0.9f, i == 1 ? 1.0f : 0.75f), -70);
        // mountains and hills
        Quad("Mountain", env, circle, new Color(0.42f, 0.58f, 0.86f), new Vector3(-5f, -3.2f), new Vector2(9f, 7f), -60);
        Quad("Mountain", env, circle, new Color(0.45f, 0.6f, 0.88f), new Vector3(4f, -3.5f), new Vector2(11f, 7.5f), -60);
        Quad("Hill", env, circle, new Color(0.4f, 0.72f, 0.45f), new Vector3(-6f, -3.6f), new Vector2(10f, 6f), -50);
        Quad("Hill", env, circle, new Color(0.36f, 0.68f, 0.42f), new Vector3(1f, -3.8f), new Vector2(12f, 6f), -50);
        Quad("Hill", env, circle, new Color(0.4f, 0.72f, 0.45f), new Vector3(8f, -3.6f), new Vector2(10f, 6f), -50);
        // ground
        Quad("Grass", env, pixel, new Color(0.47f, 0.78f, 0.32f), new Vector3(0f, -1.75f), new Vector2(40f, 1.9f), -10);
        Quad("Path", env, pixel, new Color(0.86f, 0.77f, 0.52f), new Vector3(0f, -1.6f), new Vector2(40f, 0.45f), -9);
        Quad("Dirt", env, pixel, new Color(0.5f, 0.33f, 0.2f), new Vector3(0f, -4.35f), new Vector2(40f, 3.3f), -10);

        var playerCastle = MakeCastle("Player Castle", Team.Player, castleBlue, new Vector3(-7.6f, -1.25f));
        var enemyCastle = MakeCastle("Enemy Castle", Team.Enemy, castleRed, new Vector3(7.6f, -1.25f));

        var managers = new GameObject("Battle");
        var bm = managers.AddComponent<BattleManager>();
        bm.playerCastle = playerCastle;
        bm.enemyCastle = enemyCastle;
        bm.laneY = -1.6f;
        bm.laneJitter = 0.15f;

        var spawner = managers.AddComponent<EnemySpawner>();
        spawner.entries.Add(new EnemySpawner.SpawnEntry { unit = doggo, firstDelay = 3f, interval = 7f });
        spawner.entries.Add(new EnemySpawner.SpawnEntry { unit = doggo, firstDelay = 20f, interval = 11f });
        spawner.entries.Add(new EnemySpawner.SpawnEntry { unit = darkKnight, firstDelay = 35f, interval = 28f });
        // "boss rush" when the enemy castle drops to half health
        spawner.entries.Add(new EnemySpawner.SpawnEntry { unit = darkKnight, firstDelay = 0f, interval = 1f, count = 1, startWhenCastleBelow = 0.5f });
        spawner.entries.Add(new EnemySpawner.SpawnEntry { unit = doggo, firstDelay = 0.5f, interval = 2.5f, count = 8, startWhenCastleBelow = 0.5f });

        var cannon = managers.AddComponent<Cannon>();
        cannon.beamSprite = pixel;

        BuildUI(bm, cannon, new[] { knight, cavalry, wizard, king }, pixel, roundBox, cardBox, uiCircle, iconPause, iconFast, iconCannon, iconWallet);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[Castle Defense] Battle scene built at " + ScenePath + ". Press Play!");
    }

    // ================= UI =================

    static void BuildUI(BattleManager bm, Cannon cannon, UnitData[] deck, Sprite pixel, Sprite roundBox, Sprite cardBox,
                        Sprite uiCircle, Sprite iconPause, Sprite iconFast, Sprite iconCannon, Sprite iconWallet)
    {
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif

        var canvasGo = new GameObject("Canvas", typeof(RectTransform));
        canvasGo.layer = 5;
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        var root = canvasGo.transform;

        var ui = canvasGo.AddComponent<BattleUI>();
        Color yellow = new Color(1f, 0.8f, 0.15f);
        Color gold = new Color(1f, 0.85f, 0.2f);

        // --- top left: pause + stage
        var pauseRt = Rect("PauseButton", root, TL, TL, TL, new Vector2(25, -20), new Vector2(90, 90));
        var pauseImg = Img(pauseRt, uiCircle, yellow);
        Img(Stretch("Icon", pauseRt, 24), iconPause, Color.white).raycastTarget = false;
        ui.pauseButton = Btn(pauseRt, pauseImg);
        var stageRt = Rect("StageText", root, TL, TL, TL, new Vector2(130, -25), new Vector2(500, 80));
        ui.stageText = Txt(stageRt, "Stage 1-1", 52, Color.white, TextAnchor.MiddleLeft);

        // --- top right: money + speed
        var speedRt = Rect("SpeedButton", root, TR, TR, TR, new Vector2(-25, -20), new Vector2(90, 90));
        ui.speedButtonImage = Img(speedRt, uiCircle, yellow);
        Img(Stretch("Icon", speedRt, 24), iconFast, Color.white).raycastTarget = false;
        ui.speedButton = Btn(speedRt, ui.speedButtonImage);
        var moneyRt = Rect("MoneyText", root, TR, TR, TR, new Vector2(-130, -25), new Vector2(600, 80));
        var money = moneyRt.gameObject.AddComponent<MoneyUI>();
        money.label = Txt(moneyRt, "250/1000¢", 56, gold, TextAnchor.MiddleRight);

        // --- health bars
        MakeHealthBar(root, "PlayerHealth", "Player Base Health", bm.playerCastle, TL, new Vector2(20, -230), new Color(0.2f, 0.5f, 1f), roundBox, pixel);
        MakeHealthBar(root, "EnemyHealth", "Enemy Base Health", bm.enemyCastle, TR, new Vector2(-20, -230), new Color(0.9f, 0.2f, 0.2f), roundBox, pixel);

        // --- bottom panel
        var panel = Rect("BottomPanel", root, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 215));
        Img(panel, pixel, new Color(0.4f, 0.26f, 0.16f));
        var edge = Rect("Edge", panel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 10));
        Img(edge, pixel, new Color(0.3f, 0.19f, 0.11f));

        // wallet
        var walletRt = Rect("WalletButton", panel, BL, BL, BL, new Vector2(15, 8), new Vector2(210, 200));
        var walletBg = Img(walletRt, uiCircle, yellow);
        var wallet = walletRt.gameObject.AddComponent<WalletButton>();
        wallet.button = Btn(walletRt, walletBg);
        var wIcon = Rect("Icon", walletRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 5), new Vector2(110, 110));
        wallet.icon = Img(wIcon, iconWallet, Color.white);
        wallet.icon.raycastTarget = false;
        wallet.levelText = Txt(Rect("Level", walletRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 22), new Vector2(0, 50)),
                               "Level 1", 36, Color.white, TextAnchor.MiddleCenter);
        wallet.costText = Txt(Rect("Cost", walletRt, BL, new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(0, 55)),
                              "100¢", 44, gold, TextAnchor.MiddleCenter);

        // unit cards
        var buttons = new UnitButton[deck.Length];
        for (int i = 0; i < deck.Length; i++)
        {
            var card = Rect("Card_" + deck[i].displayName, panel, BL, BL, BL, new Vector2(245 + i * 205, 22), new Vector2(190, 165));
            var bg = Img(card, cardBox, new Color(0.96f, 0.91f, 0.78f), true);
            var ub = card.gameObject.AddComponent<UnitButton>();
            ub.unit = deck[i];
            ub.button = Btn(card, bg);
            var iconRt = Stretch("Icon", card, 0);
            iconRt.offsetMin = new Vector2(14, 14);
            iconRt.offsetMax = new Vector2(-14, -34);
            ub.icon = Img(iconRt, deck[i].sprite, Color.white);
            ub.icon.preserveAspect = true;
            ub.icon.raycastTarget = false;
            Txt(Rect("Name", card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(-16, 32)),
                deck[i].displayName, 22, Color.white, TextAnchor.MiddleCenter);
            ub.costText = Txt(Rect("Cost", card, BL, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-12, 4), new Vector2(0, 50)),
                              deck[i].cost + "¢", 40, gold, TextAnchor.MiddleRight);
            var cd = Img(Stretch("Cooldown", card, 6), pixel, new Color(0f, 0f, 0f, 0.55f));
            cd.type = Image.Type.Filled;
            cd.fillMethod = Image.FillMethod.Vertical;
            cd.fillOrigin = (int)Image.OriginVertical.Bottom;
            cd.fillAmount = 0f;
            cd.raycastTarget = false;
            ub.cooldownOverlay = cd;
            buttons[i] = ub;
        }

        // cannon
        var cannonRt = Rect("CannonButton", panel, BR, BR, BR, new Vector2(-15, 8), new Vector2(200, 200));
        var cannonBg = Img(cannonRt, uiCircle, new Color(0.38f, 0.38f, 0.42f));
        var cb = cannonRt.gameObject.AddComponent<CannonButton>();
        cb.cannon = cannon;
        cb.button = Btn(cannonRt, cannonBg);
        cb.icon = Img(Stretch("Icon", cannonRt, 28), iconCannon, Color.white);
        cb.icon.raycastTarget = false;
        var ov = Img(Stretch("Charge", cannonRt, 6), uiCircle, new Color(0f, 0f, 0f, 0.5f));
        ov.type = Image.Type.Filled;
        ov.fillMethod = Image.FillMethod.Radial360;
        ov.fillOrigin = (int)Image.Origin360.Top;
        ov.raycastTarget = false;
        cb.chargeOverlay = ov;
        var lbl = Rect("Label", cannonRt, BL, new Vector2(1, 0.6f), new Vector2(0.5f, 0), new Vector2(0, 12), Vector2.zero);
        cb.label = Txt(lbl, "Ready in\n5.0s", 32, Color.white, TextAnchor.MiddleCenter);

        ui.unitButtons = buttons;
        ui.walletButton = wallet;
        ui.cannonButton = cb;

        // --- pause panel
        var pausePanel = MakeOverlay(root, "PausePanel", pixel, cardBox, out var pauseBox);
        Txt(Rect("Title", pauseBox, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(0, 90)),
            "PAUSED", 64, Color.white, TextAnchor.MiddleCenter);
        ui.resumeButton = MakeTextButton(pauseBox, "Resume", new Vector2(0, 0), roundBox, yellow);
        ui.pauseRestartButton = MakeTextButton(pauseBox, "Restart", new Vector2(0, -110), roundBox, new Color(0.9f, 0.45f, 0.3f));
        ui.pausePanel = pausePanel;

        // --- result panel
        var resultPanel = MakeOverlay(root, "ResultPanel", pixel, cardBox, out var resultBox);
        ui.resultText = Txt(Rect("Title", resultBox, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(0, 120)),
            "VICTORY!", 80, gold, TextAnchor.MiddleCenter);
        ui.resultRestartButton = MakeTextButton(resultBox, "Play Again", new Vector2(0, -70), roundBox, yellow);
        ui.resultPanel = resultPanel;

        pausePanel.SetActive(false);
        resultPanel.SetActive(false);
    }

    static void MakeHealthBar(Transform root, string name, string title, Castle castle, Vector2 corner, Vector2 pos, Color color, Sprite roundBox, Sprite pixel)
    {
        var bar = Rect(name, root, corner, corner, corner, pos, new Vector2(270, 36));
        Img(bar, roundBox, new Color(0.08f, 0.08f, 0.12f), true);
        var fill = Img(Stretch("Fill", bar, 5), pixel, color);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = corner.x < 0.5f ? (int)Image.OriginHorizontal.Left : (int)Image.OriginHorizontal.Right;
        var hb = bar.gameObject.AddComponent<HealthBarUI>();
        hb.castle = castle;
        hb.fill = fill;
        hb.label = Txt(Stretch("Label", bar, 0), "1000 / 1000", 22, Color.white, TextAnchor.MiddleCenter);
        Txt(Rect("Title", bar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(0, 34)),
            title, 24, Color.white, TextAnchor.MiddleCenter);
    }

    static GameObject MakeOverlay(Transform root, string name, Sprite pixel, Sprite box, out RectTransform content)
    {
        var dim = Stretch(name, root, 0);
        Img(dim, pixel, new Color(0f, 0f, 0f, 0.6f));
        content = Rect("Box", dim, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 420));
        Img(content, box, new Color(0.45f, 0.3f, 0.2f), true);
        return dim.gameObject;
    }

    static Button MakeTextButton(Transform parent, string label, Vector2 pos, Sprite box, Color color)
    {
        var rt = Rect(label + "Button", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(320, 90));
        var img = Img(rt, box, color, true);
        Txt(Stretch("Text", rt, 0), label, 44, Color.white, TextAnchor.MiddleCenter);
        return Btn(rt, img);
    }

    // ================= UI helpers =================

    static readonly Vector2 TL = new Vector2(0, 1), TR = new Vector2(1, 1), BL = new Vector2(0, 0), BR = new Vector2(1, 0);

    static RectTransform Rect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static RectTransform Stretch(string name, Transform parent, float inset)
    {
        var rt = Rect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
        return rt;
    }

    static Image Img(RectTransform rt, Sprite s, Color c, bool sliced = false)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = s;
        img.color = c;
        if (sliced) img.type = Image.Type.Sliced;
        return img;
    }

    static Text Txt(RectTransform rt, string content, int size, Color c, TextAnchor anchor)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.font = font;
        t.text = content;
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.color = c;
        t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var o = rt.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0.1f, 0.06f, 0.03f, 0.9f);
        o.effectDistance = new Vector2(2.5f, -2.5f);
        return t;
    }

    static Button Btn(RectTransform rt, Image target)
    {
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = target;
        return b;
    }

    // ================= world helpers =================

    static void Quad(string name, Transform parent, Sprite sprite, Color color, Vector3 pos, Vector2 worldSize, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        var b = sprite.bounds.size;
        go.transform.localScale = new Vector3(worldSize.x / b.x, worldSize.y / b.y, 1f);
    }

    static Castle MakeCastle(string name, Team team, Sprite sprite, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.flipX = team == Team.Enemy;
        var castle = go.AddComponent<Castle>();
        castle.visual = visual.transform;
        var so = new SerializedObject(castle);
        so.FindProperty("team").enumValueIndex = (int)team;
        so.ApplyModifiedPropertiesWithoutUndo();
        return castle;
    }

    static UnitData MakeUnit(string name, Sprite sprite, int cost, float cooldown, int hp, int dmg, float range, float interval,
                             bool area, float speed, int kb, float height, float width, int reward)
    {
        string path = $"{UnitDir}/{name.Replace(" ", "")}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<UnitData>(path);
        if (existing != null) return existing; // keep the user's tweaks

        var d = ScriptableObject.CreateInstance<UnitData>();
        d.displayName = name; d.sprite = sprite; d.cost = cost; d.cooldown = cooldown;
        d.maxHealth = hp; d.damage = dmg; d.attackRange = range; d.attackInterval = interval;
        d.areaAttack = area; d.moveSpeed = speed; d.knockbacks = kb; d.height = height; d.width = width;
        d.moneyReward = reward; d.spriteFacesRight = true;
        AssetDatabase.CreateAsset(d, path);
        return d;
    }

    // ================= procedural placeholder art =================

    static Sprite MakeSprite(string name, int w, int h, Func<int, int, Color> pixel, Vector2 pivot, float ppu, int border = 0)
    {
        string path = $"{ArtDir}/{name}.png";
        if (!File.Exists(path))
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = pixel(x, y);
            tex.SetPixels(px);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = ppu;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Bilinear;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            s.spritePivot = pivot;
            s.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);
            if (border > 0) ti.spriteBorder = new Vector4(border, border, border, border);
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static readonly Color Ink = new Color(0.13f, 0.11f, 0.12f);

    static float RoundRect(int x, int y, float x0, float y0, float x1, float y1, float r)
    {
        float px = x + 0.5f, py = y + 0.5f;
        float dx = Mathf.Max(x0 + r - px, px - (x1 - r), 0f);
        float dy = Mathf.Max(y0 + r - py, py - (y1 - r), 0f);
        if (px < x0 || px > x1 || py < y0 || py > y1) return 0f;
        return Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
    }

    static float DiscA(int x, int y, float cx, float cy, float r)
    {
        float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
        return Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
    }

    static Color Disc(int x, int y, float cx, float cy, float r) => new Color(1, 1, 1, DiscA(x, y, cx, cy, r));

    static Color OutlinedDisc(int x, int y, float cx, float cy, float r, float outline)
    {
        float outer = DiscA(x, y, cx, cy, r), inner = DiscA(x, y, cx, cy, r - outline);
        var c = Color.Lerp(new Color(0.25f, 0.2f, 0.15f), Color.white, inner);
        c.a = outer;
        return c;
    }

    static Color RoundBoxPixel(int x, int y, int w, int h, float r, float outline)
    {
        float outer = RoundRect(x, y, 0, 0, w, h, r);
        if (outline <= 0f) return new Color(1, 1, 1, outer);
        float inner = RoundRect(x, y, outline, outline, w - outline, h - outline, r - outline);
        var c = Color.Lerp(new Color(0.25f, 0.18f, 0.12f), Color.white, inner);
        c.a = outer;
        return c;
    }

    static Color Over(Color under, Color over, float a)
    {
        if (a <= 0f) return under;
        var c = Color.Lerp(under, over, a);
        c.a = Mathf.Max(under.a, a);
        return c;
    }

    static Color CharacterPixel(int x, int y, int w, int h, Color body, Color accent, Color eyes)
    {
        const float m = 3f, o = 5f;
        float r = Mathf.Min(w, h) * 0.32f;
        float outer = RoundRect(x, y, m, m + 6, w - m, h - m, r);
        float inner = RoundRect(x, y, m + o, m + 6 + o, w - m - o, h - m - o, r - o);
        Color fill = y > h * 0.74f ? accent : body;
        var c = Color.Lerp(Ink, fill, inner);
        c.a = outer;
        // feet
        c = Over(c, Ink, DiscA(x, y, w * 0.3f, 9, 8));
        c = Over(c, Ink, DiscA(x, y, w * 0.7f, 9, 8));
        // eyes (facing right)
        float ey = h * 0.55f, er = Mathf.Max(4f, h * 0.05f);
        c = Over(c, eyes, DiscA(x, y, w * 0.66f, ey, er));
        c = Over(c, eyes, DiscA(x, y, w * 0.84f, ey, er));
        // blush
        c = Over(c, new Color(1f, 0.55f, 0.6f), DiscA(x, y, w * 0.75f, ey - er * 2.4f, er * 0.9f) * 0.6f);
        return c;
    }

    static bool CastleShape(int x, int y, int w, int h)
    {
        float fx = x / (float)w, fy = y / (float)h;
        bool merlon = ((int)(x / (w * 0.07f)) % 2) == 0;
        bool tower = (fx < 0.3f || fx > 0.7f) && (fy < 0.84f || (fy < 0.94f && merlon));
        bool keep = fx > 0.38f && fx < 0.62f && (fy < 0.9f || (fy < 0.98f && merlon));
        bool wall = fx > 0.1f && fx < 0.9f && (fy < 0.62f || (fy < 0.7f && merlon));
        return tower || keep || wall;
    }

    static Color CastlePixel(int x, int y, int w, int h, Color stone, Color banner)
    {
        if (!CastleShape(x, y, w, h)) return Color.clear;
        const int o = 4;
        bool edge = !CastleShape(x - o, y, w, h) || !CastleShape(x + o, y, w, h) ||
                    !CastleShape(x, y + o, w, h) || (y < o) || x < o || x >= w - o;
        if (edge) return Ink;
        float fx = x / (float)w, fy = y / (float)h;
        // door
        float doorX = Mathf.Abs(fx - 0.5f);
        if (doorX < 0.1f && (fy < 0.22f || (fy < 0.3f && doorX * doorX + (fy - 0.22f) * (fy - 0.22f) * 1.6f < 0.01f)))
            return new Color(0.3f, 0.18f, 0.1f);
        // banner
        if (doorX < 0.08f && fy > 0.42f && fy < 0.66f) return banner;
        // tower windows
        float wx = fx < 0.5f ? Mathf.Abs(fx - 0.15f) : Mathf.Abs(fx - 0.85f);
        if (wx < 0.03f && ((fy > 0.6f && fy < 0.7f) || (fy > 0.35f && fy < 0.45f))) return Ink;
        // brick shading
        bool brickLine = (y % 22) < 2 || ((x + ((y / 22) % 2) * 18) % 36) < 2;
        return brickLine ? stone * 0.85f + new Color(0, 0, 0, 0.15f) : stone;
    }

    static Color PausePixel(int x, int y)
    {
        float a = Mathf.Max(RoundRect(x, y, 14, 10, 27, 54, 3), RoundRect(x, y, 37, 10, 50, 54, 3));
        return new Color(Ink.r, Ink.g, Ink.b, a);
    }

    static Color FastPixel(int x, int y)
    {
        float Tri(float x0) { float px = x + 0.5f - x0, py = Mathf.Abs(y + 0.5f - 32f); return (px >= 0 && px <= 24 && py <= 20f * (1f - px / 24f)) ? 1f : 0f; }
        float a = Mathf.Max(Tri(8), Tri(32));
        return new Color(Ink.r, Ink.g, Ink.b, a);
    }

    static Color CannonPixel(int x, int y)
    {
        var c = Color.clear;
        c = Over(c, Ink, RoundRect(x, y, 50, 58, 124, 92, 10));                       // barrel outline
        c = Over(c, new Color(0.25f, 0.25f, 0.3f), RoundRect(x, y, 55, 63, 119, 87, 6));
        c = Over(c, Ink, DiscA(x, y, 50, 60, 40));                                    // body
        c = Over(c, new Color(0.45f, 0.45f, 0.5f), DiscA(x, y, 50, 60, 34));
        c = Over(c, Ink, DiscA(x, y, 40, 24, 22));                                    // wheel
        c = Over(c, new Color(0.55f, 0.35f, 0.2f), DiscA(x, y, 40, 24, 16));
        return c;
    }

    static Color WalletPixel(int x, int y)
    {
        var c = Color.clear;
        c = Over(c, Ink, DiscA(x, y, 64, 64, 56));
        c = Over(c, new Color(1f, 0.92f, 0.55f), DiscA(x, y, 64, 64, 49));
        c = Over(c, new Color(0.9f, 0.65f, 0.1f), DiscA(x, y, 64, 64, 36));
        // simple "¢" mark: ring with a gap + vertical stroke
        float dx = x + 0.5f - 64f, dy = y + 0.5f - 64f, d = Mathf.Sqrt(dx * dx + dy * dy);
        bool ring = d > 16f && d < 26f && !(dx > 6f && Mathf.Abs(dy) < 12f);
        bool stroke = Mathf.Abs(dx - 2f) < 4f && Mathf.Abs(dy) < 32f;
        if (ring || stroke) c = Over(c, new Color(0.45f, 0.28f, 0.05f), 1f);
        return c;
    }
}
