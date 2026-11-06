using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Editor tool: adds the menu "Space Invaders > Build Game Scene".
// It draws the pixel-art sprites, makes the prefabs and builds a ready-to-play scene.
// Scripts in a folder named "Editor" only run inside the Unity Editor, never in the finished game.
public static class SpaceInvadersSetup
{
    const string Root = "Assets/SpaceInvaders";
    const string SpritesDir = Root + "/Sprites";
    const string PrefabsDir = Root + "/Prefabs";
    const string ScenesDir = Root + "/Scenes";
    const string ScenePath = ScenesDir + "/SpaceInvaders.unity";
    const float PixelsPerUnit = 16f;

    static readonly Color Green = new Color(0.30f, 1f, 0.30f);
    static readonly Color Pink = new Color(1f, 0.37f, 0.97f);
    static readonly Color Cyan = new Color(0.37f, 0.91f, 1f);
    static readonly Color Lime = new Color(0.49f, 1f, 0.37f);
    static readonly Color Red = new Color(1f, 0.25f, 0.25f);

    static Material spriteMaterial;

    [MenuItem("Space Invaders/Build Game Scene")]
    public static void BuildGameScene()
    {
        if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Space Invaders",
                "The game scene already exists. Rebuild it?\n\nThe scene, sprites and prefabs made by this tool will be replaced (your scripts are not touched).",
                "Rebuild", "Cancel"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Directory.CreateDirectory(SpritesDir);
        Directory.CreateDirectory(PrefabsDir);
        Directory.CreateDirectory(ScenesDir);
        AssetDatabase.Refresh();

        // Unlit sprites look the same with or without 2D lights.
        spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---------- 1. Sprites ----------
        Sprite pixel = MakeSprite("Pixel", new[] { "#" }, 1f);
        Sprite player = MakeSprite("Player", Patterns.Player);
        Sprite squidA = MakeSprite("InvaderSquid_A", Patterns.SquidA);
        Sprite squidB = MakeSprite("InvaderSquid_B", Patterns.SquidB);
        Sprite crabA = MakeSprite("InvaderCrab_A", Patterns.CrabA);
        Sprite crabB = MakeSprite("InvaderCrab_B", Patterns.CrabB);
        Sprite octoA = MakeSprite("InvaderOctopus_A", Patterns.OctopusA);
        Sprite octoB = MakeSprite("InvaderOctopus_B", Patterns.OctopusB);
        Sprite ufo = MakeSprite("MysteryShip", Patterns.Ufo);
        Sprite boom = MakeSprite("Explosion", Patterns.Explosion);
        Sprite playerShot = MakeSprite("PlayerBullet", Patterns.PlayerBullet);
        Sprite enemyShot = MakeSprite("EnemyBullet", Patterns.EnemyBullet);
        Sprite block = MakeSprite("ShieldBlock", Patterns.Block);

        // ---------- 2. Prefabs ----------
        GameObject explosionGo = NewSprite("Explosion", boom, Color.white, 5);
        explosionGo.AddComponent<Explosion>();
        GameObject explosionPrefab = SavePrefab(explosionGo);

        Bullet playerBullet = SavePrefab(MakeBullet("PlayerBullet", playerShot, Color.white, Bullet.Owner.Player, Vector2.up, 14f)).GetComponent<Bullet>();
        Bullet enemyBullet = SavePrefab(MakeBullet("EnemyBullet", enemyShot, new Color(1f, 0.9f, 0.5f), Bullet.Owner.Enemy, Vector2.down, 6f)).GetComponent<Bullet>();

        Invader squid = SavePrefab(MakeInvader("InvaderSquid", squidA, squidB, Pink, 30, explosionPrefab)).GetComponent<Invader>();
        Invader crab = SavePrefab(MakeInvader("InvaderCrab", crabA, crabB, Cyan, 20, explosionPrefab)).GetComponent<Invader>();
        Invader octopus = SavePrefab(MakeInvader("InvaderOctopus", octoA, octoB, Lime, 10, explosionPrefab)).GetComponent<Invader>();

        GameObject ufoGo = NewSprite("MysteryShip", ufo, Red, 2);
        AddTrigger(ufoGo, ufo);
        ufoGo.AddComponent<MysteryShip>().explosionPrefab = explosionPrefab;
        MysteryShip mysteryShip = SavePrefab(ufoGo).GetComponent<MysteryShip>();

        GameObject blockGo = NewSprite("ShieldBlock", block, Green, 0);
        AddTrigger(blockGo, block);
        blockGo.AddComponent<ShieldBlock>();
        ShieldBlock shieldBlock = SavePrefab(blockGo).GetComponent<ShieldBlock>();

        // ---------- 3. Scene ----------
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.06f);
        camGo.AddComponent<AudioListener>();

        GameObject managerGo = new GameObject("GameManager");
        GameManager manager = managerGo.AddComponent<GameManager>();
        managerGo.AddComponent<SoundFX>(); // also adds an AudioSource automatically

        GameObject playerGo = NewSprite("Player", player, Green, 1);
        playerGo.transform.position = new Vector3(0f, -4f, 0f);
        AddTrigger(playerGo, player);
        PlayerController playerController = playerGo.AddComponent<PlayerController>();
        playerController.bulletPrefab = playerBullet;
        playerController.explosionPrefab = explosionPrefab;

        GameObject ground = NewSprite("Ground", pixel, Green, 0);
        ground.transform.position = new Vector3(0f, -4.6f, 0f);
        ground.transform.localScale = new Vector3(40f, 0.05f, 1f);

        GameObject gridGo = new GameObject("InvaderGrid");
        gridGo.transform.position = new Vector3(0f, 3f, 0f);
        InvaderGrid grid = gridGo.AddComponent<InvaderGrid>();
        grid.rowPrefabs = new[] { squid, crab, crab, octopus, octopus };
        grid.bulletPrefab = enemyBullet;
        grid.mysteryShipPrefab = mysteryShip;

        GameObject shieldsGo = new GameObject("Shields");
        shieldsGo.transform.position = new Vector3(0f, -2.6f, 0f);
        ShieldSpawner shields = shieldsGo.AddComponent<ShieldSpawner>();
        shields.blockPrefab = shieldBlock;

        // ---------- 4. HUD (score, lives, messages) ----------
        GameObject canvasGo = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        manager.scoreText = MakeText(canvasGo.transform, "ScoreText", font, 40, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(40f, -20f), new Vector2(600f, 60f));
        manager.hiScoreText = MakeText(canvasGo.transform, "HiScoreText", font, 40, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(600f, 60f));
        manager.livesText = MakeText(canvasGo.transform, "LivesText", font, 40, TextAnchor.UpperRight,
            new Vector2(1f, 1f), new Vector2(-40f, -20f), new Vector2(700f, 60f));
        manager.messageText = MakeText(canvasGo.transform, "MessageText", font, 56, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1600f, 600f));

        manager.player = playerController;
        manager.invaderGrid = grid;
        manager.shields = shields;

        // ---------- 5. Save ----------
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();

        Debug.Log("Space Invaders scene built! Press Play to try it.");
        EditorUtility.DisplayDialog("Space Invaders", "The game scene is ready!\n\nPress the Play button at the top of the editor to play.", "OK");
    }

    // ---------- Helpers ----------

    // Turns a text "drawing" into a PNG sprite file. Row 0 is the top of the picture.
    static Sprite MakeSprite(string name, string[] rows, float pixelsPerUnit = PixelsPerUnit)
    {
        int height = rows.Length;
        int width = 0;
        foreach (string r in rows) width = Mathf.Max(width, r.Length);

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool on = x < rows[y].Length && rows[y][x] == '#';
                pixels[(height - 1 - y) * width + x] = on ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();

        string path = SpritesDir + "/" + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;            // keeps pixels sharp, not blurry
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static GameObject NewSprite(string name, Sprite sprite, Color color, int sortingOrder)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        if (spriteMaterial != null) sr.sharedMaterial = spriteMaterial;
        return go;
    }

    static void AddTrigger(GameObject go, Sprite sprite)
    {
        var box = go.AddComponent<BoxCollider2D>();
        box.size = sprite.bounds.size;
        box.isTrigger = true;
    }

    static GameObject MakeBullet(string name, Sprite sprite, Color color, Bullet.Owner owner, Vector2 direction, float speed)
    {
        GameObject go = NewSprite(name, sprite, color, 3);
        AddTrigger(go, sprite);
        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        var bullet = go.AddComponent<Bullet>();
        bullet.owner = owner;
        bullet.direction = direction;
        bullet.speed = speed;
        return go;
    }

    static GameObject MakeInvader(string name, Sprite frameA, Sprite frameB, Color color, int points, GameObject explosionPrefab)
    {
        GameObject go = NewSprite(name, frameA, color, 1);
        AddTrigger(go, frameA);
        var invader = go.AddComponent<Invader>();
        invader.points = points;
        invader.animationFrames = new[] { frameA, frameB };
        invader.explosionPrefab = explosionPrefab;
        return go;
    }

    static GameObject SavePrefab(GameObject go)
    {
        string path = PrefabsDir + "/" + go.name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static Text MakeText(Transform parent, string name, Font font, int size, TextAnchor alignment,
        Vector2 anchor, Vector2 position, Vector2 boxSize)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = boxSize;

        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.text = name;
        return text;
    }

    // Pixel-art drawings: '#' = white pixel, '.' = transparent.
    static class Patterns
    {
        public static readonly string[] Player =
        {
            "......#......",
            ".....###.....",
            ".....###.....",
            ".###########.",
            "#############",
            "#############",
            "#############",
            "#############",
        };

        public static readonly string[] SquidA =
        {
            "...##...",
            "..####..",
            ".######.",
            "##.##.##",
            "########",
            "..#..#..",
            ".#.##.#.",
            "#.#..#.#",
        };

        public static readonly string[] SquidB =
        {
            "...##...",
            "..####..",
            ".######.",
            "##.##.##",
            "########",
            ".#.##.#.",
            "#......#",
            ".#....#.",
        };

        public static readonly string[] CrabA =
        {
            "..#.....#..",
            "...#...#...",
            "..#######..",
            ".##.###.##.",
            "###########",
            "#.#######.#",
            "#.#.....#.#",
            "...##.##...",
        };

        public static readonly string[] CrabB =
        {
            "..#.....#..",
            "#..#...#..#",
            "#.#######.#",
            "###.###.###",
            "###########",
            ".#########.",
            "..#.....#..",
            ".#.......#.",
        };

        public static readonly string[] OctopusA =
        {
            "....####....",
            ".##########.",
            "############",
            "###..##..###",
            "############",
            "...##..##...",
            "..##.##.##..",
            "##........##",
        };

        public static readonly string[] OctopusB =
        {
            "....####....",
            ".##########.",
            "############",
            "###..##..###",
            "############",
            "..###..###..",
            ".##..##..##.",
            "..##....##..",
        };

        public static readonly string[] Ufo =
        {
            ".....######.....",
            "...##########...",
            "..############..",
            ".##.##.##.##.##.",
            "################",
            "..###..##..###..",
            "...#........#...",
        };

        public static readonly string[] Explosion =
        {
            ".#..#...#..#.",
            "..#..#.#..#..",
            "...#.....#...",
            "##.........##",
            "...#.....#...",
            "..#..#.#..#..",
            ".#..#...#..#.",
        };

        public static readonly string[] PlayerBullet =
        {
            "#",
            "#",
            "#",
            "#",
            "#",
        };

        public static readonly string[] EnemyBullet =
        {
            ".#.",
            "#..",
            ".#.",
            "..#",
            ".#.",
            "#..",
            ".#.",
        };

        public static readonly string[] Block =
        {
            "###",
            "###",
            "###",
        };
    }
}
