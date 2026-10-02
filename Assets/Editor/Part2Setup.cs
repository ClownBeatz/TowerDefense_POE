using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Sets up Part 2 prefabs and the scene.</summary>
public static class Part2Setup
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string Part1PrefabFolder = "Assets/Prefabs/Prac";
    private const string PrefabFolder = "Assets/Prefabs/Part2";
    private const string MaterialFolder = "Assets/Materials/Part2";
    private const string HudName = "Part2 HUD";

    // Part 1 text elements replaced by the visual HUD (hidden, not deleted).
    private static readonly string[] ReplacedPart1Ui = { "health bar", "health label", "resources label", "cost label" };

    private static readonly Color PanelColor = new Color(0.06f, 0.07f, 0.1f, 0.8f);
    private static readonly Color GunnerColor = new Color(0.2f, 0.5f, 1f);
    private static readonly Color MortarColor = new Color(1f, 0.55f, 0.1f);
    private static readonly Color FrostColor = new Color(0.35f, 0.85f, 1f);

    private static Material materialTemplate;
    private static Dictionary<string, Sprite> icons;
    private static Sprite roundedSprite;
    private static Sprite circleSprite;

    [MenuItem("Tools/Setup Part 2")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Setup Part 2", "Exit Play mode first, then run the setup again.", "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder("Assets/Prefabs", "Part2");
        EnsureFolder("Assets/Materials", "Part2");
        materialTemplate = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/EnemMat.mat");
        roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        icons = IconFactory.BuildAll();

        GameObject gruntAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Part1PrefabFolder + "/Enemy.prefab");
        GameObject gunnerAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Part1PrefabFolder + "/Defender.prefab");

        TuneTower(AssetDatabase.LoadAssetAtPath<GameObject>(Part1PrefabFolder + "/Tower.prefab"));
        Enemy grunt = TuneGrunt(gruntAsset);
        Defender gunner = TuneGunner(gunnerAsset);
        MortarShell shell = BuildMortarShell();
        Enemy runner = BuildRunner(gruntAsset);
        Enemy brute = BuildBrute(gruntAsset);
        Defender mortar = BuildMortar(gunnerAsset, shell);
        Defender frost = BuildFrost(gunnerAsset);
        AssetDatabase.SaveAssets();

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath);

        WireScene(new[] { grunt, runner, brute }, new[] { gunner, mortar, frost });

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[Part2Setup] Prefabs built and SampleScene wired. Press Play.");
        EditorUtility.DisplayDialog("Setup Part 2", "Part 2 is set up and the scene is saved.\nPress Play to test.", "OK");
    }

    //Balance for the Part 1 units
    // Enemies need the range and health to actually reach defenders (Part 1 feedback).

    private static void TuneTower(GameObject asset)
    {
        Tower tower = asset.GetComponent<Tower>();
        tower.maxHealth = 250f;
        tower.damage = 15f;
        tower.attacksPerSecond = 1.2f;
        PrefabUtility.SavePrefabAsset(asset);
    }

    private static Enemy TuneGrunt(GameObject asset)
    {
        Enemy grunt = asset.GetComponent<Enemy>();
        grunt.type = EnemyType.Grunt;
        grunt.reward = 12;
        grunt.maxHealth = 40f;
        grunt.moveSpeed = 2.5f;
        // Defender spots sit ~4 units from the path; 1.5 never reached them.
        grunt.attackRange = 4.5f;
        grunt.damage = 6f;
        grunt.attacksPerSecond = 1f;
        PrefabUtility.SavePrefabAsset(asset);
        return grunt;
    }

    private static Defender TuneGunner(GameObject asset)
    {
        Defender gunner = asset.GetComponent<Defender>();
        gunner.type = DefenderType.Gunner;
        gunner.displayName = "Gunner";
        gunner.cost = 20;
        gunner.description = "Fast single-target shots";
        gunner.icon = icons["gunner"];
        gunner.accentColor = GunnerColor;
        gunner.maxHealth = 50f;
        gunner.attackRange = 6f;
        gunner.damage = 10f;
        gunner.attacksPerSecond = 1.2f;
        PrefabUtility.SavePrefabAsset(asset);
        return gunner;
    }

    //New enemies

    private static Enemy BuildRunner(GameObject gruntAsset)
    {
        GameObject root = CloneWithoutMesh(gruntAsset, "RunnerEnemy");
        RunnerEnemy runner = SwapEnemyScript<RunnerEnemy>(root);
        runner.type = EnemyType.Runner;
        runner.reward = 8;
        runner.maxHealth = 18f;
        runner.healthBarHeight = 1f;
        runner.moveSpeed = 5f;
        runner.attackRange = 5f;
        runner.damage = 4f;
        runner.attacksPerSecond = 1.5f;
        runner.heightOffset = 0.4f;
        root.GetComponent<SphereCollider>().radius = 0.4f;

        Material body = MakeMaterial("RunnerMat", new Color(1f, 0.82f, 0.1f));
        Material accent = MakeMaterial("RunnerAccentMat", new Color(0.95f, 0.45f, 0.05f));
        AddPart(root.transform, "Body", PrimitiveType.Capsule, body, Vector3.zero, Vector3.one * 0.45f, new Vector3(90f, 0f, 0f));
        AddPart(root.transform, "Nose", PrimitiveType.Sphere, accent, new Vector3(0f, 0.05f, 0.42f), Vector3.one * 0.25f);
        AddPart(root.transform, "FinLeft", PrimitiveType.Cube, accent, new Vector3(-0.3f, 0.1f, -0.25f), new Vector3(0.35f, 0.05f, 0.25f), new Vector3(0f, -25f, 0f));
        AddPart(root.transform, "FinRight", PrimitiveType.Cube, accent, new Vector3(0.3f, 0.1f, -0.25f), new Vector3(0.35f, 0.05f, 0.25f), new Vector3(0f, 25f, 0f));

        return SavePrefab(root, "RunnerEnemy").GetComponent<Enemy>();
    }

    private static Enemy BuildBrute(GameObject gruntAsset)
    {
        GameObject root = CloneWithoutMesh(gruntAsset, "BruteEnemy");
        BruteEnemy brute = SwapEnemyScript<BruteEnemy>(root);
        brute.type = EnemyType.Brute;
        brute.reward = 30;
        brute.maxHealth = 140f;
        brute.healthBarHeight = 1.7f;
        brute.moveSpeed = 1.5f;
        brute.slowResistance = 0.6f;
        brute.attackRange = 5f;
        brute.damage = 18f;
        brute.attacksPerSecond = 0.4f;
        brute.heightOffset = 0.75f;
        root.GetComponent<SphereCollider>().radius = 0.75f;

        Material body = MakeMaterial("BruteMat", new Color(0.42f, 0.12f, 0.6f));
        Material accent = MakeMaterial("BruteAccentMat", new Color(0.2f, 0.05f, 0.3f));
        AddPart(root.transform, "Body", PrimitiveType.Cube, body, Vector3.zero, new Vector3(1.2f, 1f, 1.1f));
        AddPart(root.transform, "Head", PrimitiveType.Cube, accent, new Vector3(0f, 0.7f, 0.15f), new Vector3(0.55f, 0.45f, 0.55f));
        AddPart(root.transform, "FistLeft", PrimitiveType.Cube, accent, new Vector3(-0.8f, -0.1f, 0.2f), new Vector3(0.45f, 0.6f, 0.45f));
        AddPart(root.transform, "FistRight", PrimitiveType.Cube, accent, new Vector3(0.8f, -0.1f, 0.2f), new Vector3(0.45f, 0.6f, 0.45f));

        return SavePrefab(root, "BruteEnemy").GetComponent<Enemy>();
    }

    //New defenders

    private static MortarShell BuildMortarShell()
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "MortarShell";
        Object.DestroyImmediate(root.GetComponent<Collider>());
        root.transform.localScale = Vector3.one * 0.35f;
        root.GetComponent<MeshRenderer>().sharedMaterial = MakeMaterial("MortarShellMat", new Color(0.15f, 0.15f, 0.15f));
        root.AddComponent<MortarShell>();

        return SavePrefab(root, "MortarShell").GetComponent<MortarShell>();
    }

    private static Defender BuildMortar(GameObject gunnerAsset, MortarShell shell)
    {
        GameObject root = CloneWithoutMesh(gunnerAsset, "MortarDefender");
        MortarDefender mortar = SwapDefenderScript<MortarDefender>(root);
        mortar.type = DefenderType.Mortar;
        mortar.displayName = "Mortar";
        mortar.cost = 35;
        mortar.description = "Long-range splash, aims at groups";
        mortar.icon = icons["mortar"];
        mortar.accentColor = MortarColor;
        mortar.maxHealth = 30f;
        mortar.attackRange = 9f;
        mortar.damage = 18f;
        mortar.attacksPerSecond = 0.4f;
        mortar.splashRadius = 2.2f;
        mortar.shellPrefab = shell;

        Material metal = MakeMaterial("MortarMat", new Color(0.3f, 0.32f, 0.36f));
        Material accent = MakeMaterial("MortarAccentMat", MortarColor);
        AddPart(root.transform, "Base", PrimitiveType.Cylinder, metal, new Vector3(0f, -0.75f, 0f), new Vector3(1.2f, 0.25f, 1.2f));
        AddPart(root.transform, "Body", PrimitiveType.Cylinder, metal, new Vector3(0f, -0.25f, 0f), new Vector3(0.8f, 0.3f, 0.8f));
        AddPart(root.transform, "Band", PrimitiveType.Cylinder, accent, new Vector3(0f, -0.05f, 0f), new Vector3(0.85f, 0.05f, 0.85f));
        AddPart(root.transform, "Barrel", PrimitiveType.Cylinder, metal, new Vector3(0f, 0.3f, 0.15f), new Vector3(0.35f, 0.45f, 0.35f), new Vector3(35f, 0f, 0f));

        return SavePrefab(root, "MortarDefender").GetComponent<Defender>();
    }

    private static Defender BuildFrost(GameObject gunnerAsset)
    {
        GameObject root = CloneWithoutMesh(gunnerAsset, "FrostDefender");
        FrostDefender frost = SwapDefenderScript<FrostDefender>(root);
        frost.type = DefenderType.Frost;
        frost.displayName = "Frost";
        frost.cost = 30;
        frost.description = "Tanky, pulses slow all nearby";
        frost.icon = icons["frost"];
        frost.accentColor = FrostColor;
        frost.maxHealth = 130f;
        frost.attackRange = 4.5f;
        frost.damage = 6f;
        frost.attacksPerSecond = 0.5f;
        frost.slowStrength = 0.5f;
        frost.slowDuration = 2f;

        Material ice = MakeMaterial("FrostMat", new Color(0.78f, 0.88f, 0.96f));
        Material crystal = MakeMaterial("FrostCrystalMat", FrostColor);
        AddPart(root.transform, "Base", PrimitiveType.Cube, ice, new Vector3(0f, -0.65f, 0f), new Vector3(0.9f, 0.6f, 0.9f));
        AddPart(root.transform, "Pillar", PrimitiveType.Cylinder, ice, new Vector3(0f, -0.15f, 0f), new Vector3(0.35f, 0.25f, 0.35f));
        frost.crystal = AddPart(root.transform, "Crystal", PrimitiveType.Cube, crystal, new Vector3(0f, 0.4f, 0f), Vector3.one * 0.55f, new Vector3(45f, 0f, 45f));

        return SavePrefab(root, "FrostDefender").GetComponent<Defender>();
    }

    //Scene wiring

    private static void WireScene(Enemy[] enemies, Defender[] defenders)
    {
        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        EnemySpawner spawner = gameManager.enemySpawner;

        PlayerTracker tracker = gameManager.GetComponent<PlayerTracker>();
        if (tracker == null)
            tracker = gameManager.gameObject.AddComponent<PlayerTracker>();

        tracker.gameManager = gameManager;
        tracker.terrainGenerator = gameManager.terrainGenerator;

        spawner.terrainGenerator = gameManager.terrainGenerator;
        spawner.tracker = tracker;
        spawner.defencePerUnit = 5f;
        spawner.enemyOptions = new List<EnemySpawner.EnemyOption>
        {
            new EnemySpawner.EnemyOption { prefab = enemies[0], cost = 2, unlockWave = 1, baseWeight = 1f },
            new EnemySpawner.EnemyOption { prefab = enemies[1], cost = 2, unlockWave = 2, baseWeight = 0.8f },
            new EnemySpawner.EnemyOption { prefab = enemies[2], cost = 6, unlockWave = 3, baseWeight = 0.5f }
        };

        DefenderPlacer placer = Object.FindFirstObjectByType<DefenderPlacer>();
        placer.defenderPrefabs = new List<Defender>(defenders);

        Camera mainCamera = Camera.main;
        CameraController cameraController = mainCamera.GetComponent<CameraController>();
        if (cameraController == null)
            cameraController = mainCamera.gameObject.AddComponent<CameraController>();
        cameraController.terrainGenerator = gameManager.terrainGenerator;

        UIManager ui = Object.FindFirstObjectByType<UIManager>();
        ui.enemySpawner = spawner;
        BuildHud(ui);

        EditorUtility.SetDirty(tracker);
        EditorUtility.SetDirty(spawner);
        EditorUtility.SetDirty(placer);
        EditorUtility.SetDirty(cameraController);
        EditorUtility.SetDirty(ui);
    }

    //Visual HUD

    private static void BuildHud(UIManager ui)
    {
        Canvas canvas = ui.gameOverPanel.GetComponentInParent<Canvas>(true).rootCanvas;

        // Layout is designed at 1920x1080 and scales evenly to other screen sizes.
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        EditorUtility.SetDirty(scaler);

        foreach (string objectName in ReplacedPart1Ui)
        {
            Transform old = FindDeep(canvas.transform, objectName);
            if (old != null)
                old.gameObject.SetActive(false);
        }

        Transform oldHud = canvas.transform.Find(HudName);
        if (oldHud != null)
            Object.DestroyImmediate(oldHud.gameObject);

        // Sits behind the Part 1 buttons and game over panel so those stay on top.
        RectTransform hud = CreateRect(HudName, canvas.transform);
        Stretch(hud, 0f);
        hud.SetAsFirstSibling();

        BuildStatusPanel(ui, hud);
        BuildWavePanel(ui, hud);
        BuildDirectorPanel(ui, hud);
        BuildShop(ui, hud);

        ui.bannerLabel = CreateText("Banner", hud, "", 72f, TextAlignmentOptions.Center, FontStyles.Bold);
        ui.bannerLabel.color = new Color(1f, 0.85f, 0.3f);
        ui.bannerLabel.alpha = 0f;
        Place(ui.bannerLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(1200f, 120f));

        // Built last so it draws over everything else in the HUD.
        BuildPauseScreen(ui, hud);
        BuildGameOverExtras(ui);
    }

    // Top left: big tower health bar with a heart icon, and a coin counter.
    private static void BuildStatusPanel(UIManager ui, RectTransform hud)
    {
        Image panel = CreateImage("StatusPanel", hud, PanelColor, roundedSprite);
        Place(panel.rectTransform, new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(450f, 130f));

        Image heart = CreateImage("HeartIcon", panel.transform, Color.white, icons["heart"]);
        Place(heart.rectTransform, new Vector2(0f, 1f), new Vector2(14f, -14f), new Vector2(48f, 48f));

        ui.towerHealthBar = CreateBar("TowerHealthBar", panel.transform, new Vector2(72f, -18f), new Vector2(360f, 40f));
        ui.towerHealthText = CreateText("TowerHealthText", ui.towerHealthBar.transform, "250 / 250", 22f, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(ui.towerHealthText.rectTransform, 0f);

        Image coin = CreateImage("CoinIcon", panel.transform, Color.white, icons["coin"]);
        Place(coin.rectTransform, new Vector2(0f, 1f), new Vector2(18f, -74f), new Vector2(42f, 42f));

        ui.resourcesText = CreateText("ResourcesText", panel.transform, "100", 40f, TextAlignmentOptions.Left, FontStyles.Bold);
        ui.resourcesText.color = new Color(1f, 0.85f, 0.3f);
        Place(ui.resourcesText.rectTransform, new Vector2(0f, 1f), new Vector2(72f, -70f), new Vector2(200f, 50f));
    }

    // Top centre: wave number plus a bar that fills up to the next wave, then
    // drains as that wave's enemies die.
    private static void BuildWavePanel(UIManager ui, RectTransform hud)
    {
        Image panel = CreateImage("WavePanel", hud, PanelColor, roundedSprite);
        Place(panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(380f, 100f));

        ui.waveLabel = CreateText("WaveLabel", panel.transform, "GET READY", 36f, TextAlignmentOptions.Center, FontStyles.Bold);
        Place(ui.waveLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(360f, 44f));

        Image background = CreateImage("WaveProgress", panel.transform, new Color(0.15f, 0.15f, 0.18f), roundedSprite);
        Place(background.rectTransform, new Vector2(0f, 1f), new Vector2(18f, -62f), new Vector2(250f, 20f));
        ui.waveProgressFill = CreateImage("Fill", background.transform, Color.white, roundedSprite);
        MakeFilled(ui.waveProgressFill);
        Stretch(ui.waveProgressFill.rectTransform, 2f);

        ui.waveStatusIcon = CreateImage("StatusIcon", panel.transform, Color.white, icons["clock"]);
        Place(ui.waveStatusIcon.rectTransform, new Vector2(0f, 1f), new Vector2(278f, -58f), new Vector2(28f, 28f));

        ui.waveStatusText = CreateText("StatusText", panel.transform, "15s", 24f, TextAlignmentOptions.Left, FontStyles.Bold);
        Place(ui.waveStatusText.rectTransform, new Vector2(0f, 1f), new Vector2(312f, -56f), new Vector2(60f, 32f));

        ui.countdownSprite = icons["clock"];
        ui.enemySprite = icons["enemy"];
    }

    // Right side: difficulty meter plus what the director decided and why.
    private static void BuildDirectorPanel(UIManager ui, RectTransform hud)
    {
        Image panel = CreateImage("DirectorPanel", hud, PanelColor, roundedSprite);
        Place(panel.rectTransform, new Vector2(1f, 1f), new Vector2(-20f, -160f), new Vector2(380f, 0f));
        ui.directorPanel = panel.gameObject;

        // Grows downward to fit its text so it covers as little of the map as possible.
        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateText("Title", panel.transform, "WAVE DIRECTOR  <size=70%><color=#AAAAAA>[Tab] hide</color></size>", 20f, TextAlignmentOptions.Left, FontStyles.Bold);

        Image meter = CreateImage("DifficultyMeter", panel.transform, new Color(0.15f, 0.15f, 0.18f), roundedSprite);
        meter.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
        ui.difficultyFill = CreateImage("Fill", meter.transform, Color.white, roundedSprite);
        MakeFilled(ui.difficultyFill);
        Stretch(ui.difficultyFill.rectTransform, 2f);
        ui.difficultyText = CreateText("Label", meter.transform, "1.00x  Balanced", 18f, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(ui.difficultyText.rectTransform, 0f);

        ui.directorLabel = CreateText("DirectorText", panel.transform, "", 16f, TextAlignmentOptions.TopLeft);
    }

    // Bottom centre: one card per defender with icon, colour and coin cost.
    private static void BuildShop(UIManager ui, RectTransform hud)
    {
        RectTransform shop = CreateRect("ShopPanel", hud);
        Place(shop, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(3 * 230f + 2 * 14f, 140f));
        HorizontalLayoutGroup layout = shop.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        ui.shopCards = new ShopCard[3];
        for (int i = 0; i < 3; i++)
            ui.shopCards[i] = BuildShopCard(shop, i);
    }

    private static ShopCard BuildShopCard(Transform parent, int index)
    {
        Image border = CreateImage("ShopCard" + (index + 1), parent, Color.black, roundedSprite);
        ShopCard card = border.gameObject.AddComponent<ShopCard>();
        card.border = border;
        card.canvasGroup = border.gameObject.AddComponent<CanvasGroup>();

        Image background = CreateImage("Background", border.transform, new Color(0.1f, 0.12f, 0.16f, 0.95f), roundedSprite);
        Stretch(background.rectTransform, 4f);
        card.button = border.gameObject.AddComponent<Button>();
        card.button.targetGraphic = background;

        card.accentStrip = CreateImage("AccentStrip", background.transform, Color.white, roundedSprite);
        RectTransform strip = card.accentStrip.rectTransform;
        strip.anchorMin = new Vector2(0f, 1f);
        strip.anchorMax = new Vector2(1f, 1f);
        strip.pivot = new Vector2(0.5f, 1f);
        strip.offsetMin = new Vector2(6f, -12f);
        strip.offsetMax = new Vector2(-6f, -4f);

        card.icon = CreateImage("Icon", background.transform, Color.white, null);
        card.icon.preserveAspect = true;
        card.icon.enabled = false; // ShopCard.Bind turns it on with the defender's icon
        Place(card.icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(12f, -4f), new Vector2(80f, 80f));

        card.nameLabel = CreateText("Name", background.transform, "", 26f, TextAlignmentOptions.Left, FontStyles.Bold);
        Place(card.nameLabel.rectTransform, new Vector2(0f, 1f), new Vector2(100f, -22f), new Vector2(120f, 36f));

        Image coin = CreateImage("CoinIcon", background.transform, Color.white, icons["coin"]);
        Place(coin.rectTransform, new Vector2(0f, 1f), new Vector2(100f, -68f), new Vector2(30f, 30f));
        card.costLabel = CreateText("Cost", background.transform, "", 30f, TextAlignmentOptions.Left, FontStyles.Bold);
        Place(card.costLabel.rectTransform, new Vector2(0f, 1f), new Vector2(136f, -64f), new Vector2(80f, 38f));

        Image badge = CreateImage("HotkeyBadge", background.transform, new Color(0f, 0f, 0f, 0.7f), circleSprite);
        Place(badge.rectTransform, new Vector2(1f, 0f), new Vector2(-8f, 8f), new Vector2(30f, 30f));
        card.hotkeyLabel = CreateText("Hotkey", badge.transform, "", 18f, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(card.hotkeyLabel.rectTransform, 0f);

        return card;
    }

    private static void BuildPauseScreen(UIManager ui, RectTransform hud)
    {
        Image dim = CreateImage("PausedOverlay", hud, new Color(0f, 0f, 0f, 0.6f), null);
        Stretch(dim.rectTransform, 0f);

        Image icon = CreateImage("PauseIcon", dim.transform, Color.white, icons["pause"]);
        Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(120f, 120f));

        TextMeshProUGUI title = CreateText("Title", dim.transform, "PAUSED", 80f, TextAlignmentOptions.Center, FontStyles.Bold);
        Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(600f, 100f));

        ui.resumeButton = CreateButton("ResumeButton", dim.transform, "RESUME", new Color(0.25f, 0.7f, 0.35f), new Vector2(0f, -60f));
        ui.pausedRestartButton = CreateButton("RestartButton", dim.transform, "NEW MAP", new Color(0.35f, 0.37f, 0.42f), new Vector2(0f, -145f));

        ui.pausedOverlay = dim.gameObject;
        ui.pausedOverlay.SetActive(false);
    }

    private static void BuildGameOverExtras(UIManager ui)
    {
        Transform panel = ui.gameOverPanel.transform;
        foreach (string objectName in new[] { "GameOverStats", "PlayAgainButton" })
        {
            Transform old = panel.Find(objectName);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
        }

        ui.gameOverStatsLabel = CreateText("GameOverStats", panel, "", 36f, TextAlignmentOptions.Center);
        Place(ui.gameOverStatsLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(900f, 60f));

        ui.playAgainButton = CreateButton("PlayAgainButton", panel, "PLAY AGAIN", new Color(0.25f, 0.7f, 0.35f), new Vector2(0f, -160f));
    }

    //Prefab helpers

    private static GameObject CloneWithoutMesh(GameObject template, string name)
    {
        GameObject clone = Object.Instantiate(template);
        clone.name = name;
        clone.transform.position = Vector3.zero;
        Object.DestroyImmediate(clone.GetComponent<MeshRenderer>());
        Object.DestroyImmediate(clone.GetComponent<MeshFilter>());
        return clone;
    }

    private static T SwapEnemyScript<T>(GameObject root) where T : Enemy
    {
        Enemy old = root.GetComponent<Enemy>();
        GameObject projectile = old.projectilePrefab;
        Object.DestroyImmediate(old);

        T replacement = root.AddComponent<T>();
        replacement.projectilePrefab = projectile;
        return replacement;
    }

    private static T SwapDefenderScript<T>(GameObject root) where T : Defender
    {
        Defender old = root.GetComponent<Defender>();
        GameObject projectile = old.projectilePrefab;
        Object.DestroyImmediate(old);

        T replacement = root.AddComponent<T>();
        replacement.projectilePrefab = projectile;
        return replacement;
    }

    private static GameObject SavePrefab(GameObject root, string name)
    {
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/{name}.prefab");
        Object.DestroyImmediate(root);
        return saved;
    }

    private static Transform AddPart(Transform parent, string name, PrimitiveType shape, Material material,
        Vector3 position, Vector3 scale, Vector3 rotation = default)
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        part.name = name;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.Euler(rotation);
        part.transform.localScale = scale;
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
        return part.transform;
    }

    private static Material MakeMaterial(string name, Color color)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(materialTemplate);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
            AssetDatabase.CreateFolder(parent, name);
    }

    //UI helpers

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize,
        TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal)
    {
        TextMeshProUGUI label = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }

    private static Image CreateImage(string name, Transform parent, Color color, Sprite sprite)
    {
        Image image = CreateRect(name, parent).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        if (sprite != null && sprite == roundedSprite)
            image.type = Image.Type.Sliced;
        return image;
    }

    private static Button CreateButton(string name, Transform parent, string label, Color color, Vector2 position)
    {
        Image background = CreateImage(name, parent, color, roundedSprite);
        Place(background.rectTransform, new Vector2(0.5f, 0.5f), position, new Vector2(280f, 66f));

        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        TextMeshProUGUI text = CreateText("Label", background.transform, label, 30f, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(text.rectTransform, 0f);
        return button;
    }

    // Border, background, damage chip and fill, driven by HealthBarFill.
    private static HealthBarFill CreateBar(string name, Transform parent, Vector2 position, Vector2 size)
    {
        Image border = CreateImage(name, parent, new Color(0.95f, 0.75f, 0.2f), roundedSprite);
        Place(border.rectTransform, new Vector2(0f, 1f), position, size);

        Image background = CreateImage("Background", border.transform, new Color(0.12f, 0.12f, 0.14f), roundedSprite);
        Stretch(background.rectTransform, 3f);

        HealthBarFill bar = border.gameObject.AddComponent<HealthBarFill>();
        bar.chip = CreateImage("Chip", border.transform, new Color(1f, 0.9f, 0.6f), roundedSprite);
        MakeFilled(bar.chip);
        Stretch(bar.chip.rectTransform, 3f);
        bar.fill = CreateImage("Fill", border.transform, Color.white, roundedSprite);
        MakeFilled(bar.fill);
        Stretch(bar.fill.rectTransform, 3f);
        return bar;
    }

    private static void MakeFilled(Image image)
    {
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillAmount = 1f;
    }

    // Anchors and pivots at the same point, e.g. (1,1) = top right corner.
    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect, float padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;

            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }

        return null;
    }
}
