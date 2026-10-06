using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class DriveBossAuthoring
{
    [Serializable] private sealed class Receipt { public string path; public long[] modifiedIds; }
    [Serializable] private sealed class Receipts { public List<Receipt> assets = new List<Receipt>(); }
    private static readonly HashSet<long> touched = new HashSet<long>();
    private static readonly Receipts receipts = new Receipts();
    private static string output;
    private static Sprite white;
    private static TMP_FontAsset font;
    private static Material particleMaterial;
    private static readonly Color Cream = new Color32(248, 237, 219, 255);
    private static readonly Color Track = new Color32(65, 61, 72, 255);

    public static void Run()
    {
        output = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
        try
        {
            if (!Application.dataPath.Replace('\\', '/').EndsWith("/outputs/drive-boss-validation/project/Assets", StringComparison.OrdinalIgnoreCase) ||
                !Application.productName.StartsWith("DriveBossPreview_", StringComparison.Ordinal))
                throw new InvalidOperationException("Refuse authoring outside isolated task project.");
            Directory.CreateDirectory(Path.Combine(output, "before"));
            Directory.CreateDirectory(Path.Combine(output, "after"));
            MakeArtwork();
            RewardPickup flesh = MakePickup("FleshPickup", RewardPickupKind.Flesh);
            RewardPickup soul = MakePickup("SoulPickup", RewardPickupKind.Soul);
            RewardPickup fuel = MakePickup("FuelPickup", RewardPickupKind.Fuel);
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Enemy" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                if (root.GetComponentInChildren<Enemy>(true) == null) { PrefabUtility.UnloadPrefabContents(root); continue; }
                Begin(path);
                foreach (Enemy enemy in root.GetComponentsInChildren<Enemy>(true))
                {
                    Set(enemy, "fleshPickupPrefab", flesh); Set(enemy, "soulPickupPrefab", soul);
                    if (enemy is FuelBarrel)
                    {
                        Touch(enemy.gameObject);
                        SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>();
                        if (renderer == null) renderer = enemy.gameObject.AddComponent<SpriteRenderer>();
                        Touch(renderer); renderer.sprite = SpriteAt("Assets/Sprites/UI/Driving/Barrel.png"); renderer.color = Color.white; renderer.sortingOrder = 5;
                        BoxCollider2D box = enemy.GetComponent<BoxCollider2D>();
                        if (box == null) box = enemy.gameObject.AddComponent<BoxCollider2D>();
                        Touch(box); box.size = new Vector2(.65f, .85f); box.isTrigger = true;
                        Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
                        if (body == null) body = enemy.gameObject.AddComponent<Rigidbody2D>();
                        Touch(body); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0f;
                        Set(enemy, "fuelPickupPrefab", fuel);
                    }
                }
                EyeBoss eye = root.GetComponent<EyeBoss>();
                if (eye != null)
                {
                    Transform[] points = root.transform.Cast<Transform>().ToArray();
                    Touch(root);
                    EyeBossBelt belt = root.GetComponent<EyeBossBelt>();
                    if (belt == null) belt = root.AddComponent<EyeBossBelt>();
                    SpriteRenderer source = root.GetComponent<SpriteRenderer>();
                    var tiles = new SpriteRenderer[4];
                    string[] names = { "BeltLeft1", "BeltRight1", "BeltLeft2", "BeltRight2" };
                    for (int index = 0; index < tiles.Length; index++)
                    {
                        var tile = new GameObject(names[index], typeof(SpriteRenderer));
                        tile.transform.SetParent(root.transform, false);
                        tiles[index] = tile.GetComponent<SpriteRenderer>();
                        tiles[index].sprite = source.sprite; tiles[index].sharedMaterial = source.sharedMaterial;
                        tiles[index].sortingLayerID = source.sortingLayerID; tiles[index].sortingOrder = source.sortingOrder;
                    }
                    SetArray(belt, "repeatTiles", tiles); Set(eye, "belt", belt); SetArray(eye, "tentacleSpawnPoints", points);
                }
                TrainBoss boss = root.GetComponent<TrainBoss>();
                if (boss != null) { Touch(root.transform); root.transform.localScale *= 2f; }
                PrefabUtility.SaveAsPrefabAsset(root, path);
                End(path);
                PrefabUtility.UnloadPrefabContents(root);
            }
            AuthorScene();
            AssetDatabase.SaveAssets();
            File.WriteAllText(Path.Combine(output, "authoring-receipt.json"), JsonUtility.ToJson(receipts, true));
            File.WriteAllText(Path.Combine(output, "authoring-result.txt"), "PASS: Unity-authored quarter gauge, fuel/XP fills, chase HUD, camera rig, pickup prefabs and enemy references.\nAssets: " + receipts.assets.Count);
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            File.WriteAllText(Path.Combine(output, "authoring-result.txt"), "FAIL: " + exception);
            EditorApplication.Exit(1);
        }
    }

    private static void AuthorScene()
    {
        const string path = "Assets/Scenes/Junmo.unity";
        Begin(path);
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Transform[] all = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        RectTransform parent = all.Single(tr => tr.name == "OtherUI") as RectTransform;
        TMP_Text timer = all.Single(tr => tr.name == "TimeText").GetComponent<TMP_Text>(); font = timer.font;
        Train train = UnityEngine.Object.FindFirstObjectByType<Train>();
        TrainController controller = train.GetComponent<TrainController>();
        TrainLevelManager level = train.GetComponent<TrainLevelManager>();
        StageManager stage = UnityEngine.Object.FindFirstObjectByType<StageManager>();
        Camera camera = Camera.main;
        Touch(camera.transform);
        if (camera.transform.parent != null) Touch(camera.transform.parent);
        var rig = new GameObject("ForwardCameraRig");
        rig.transform.position = new Vector3(camera.transform.position.x, 0f, 0f);
        camera.transform.SetParent(rig.transform, true);
        ForwardCameraFollow follow = rig.AddComponent<ForwardCameraFollow>();
        Set(follow, "trainController", controller); Set(controller, "cameraFollow", follow);
        var trainFields = new SerializedObject(train);
        var handObject = trainFields.FindProperty("handObject").objectReferenceValue as GameObject;
        if (handObject == null) throw new InvalidOperationException("Existing game over hand is absent.");
        Touch(handObject);
        PursuingHand hand = handObject.GetComponent<PursuingHand>();
        if (hand == null) hand = handObject.AddComponent<PursuingHand>();
        Set(hand, "train", train); Set(hand, "stageManager", stage);
        Float(hand, "distanceToWorldScale", .1f); Float(hand, "stageSpeedIncrease", 30f);
        Set(stage, "cameraFollow", follow); Set(stage, "pursuingHand", hand);
        Set(UnityEngine.Object.FindFirstObjectByType<Spawner>(), "cameraFollow", follow);
        Set(UnityEngine.Object.FindFirstObjectByType<EventObjectSpawner>(), "cameraFollow", follow);
        Float(train, "decelerationDelay", .5f);

        var windObject = new GameObject("AccelerationWind", typeof(ParticleSystem));
        windObject.transform.SetParent(camera.transform, false); windObject.transform.localPosition = new Vector3(0f, 0f, 10f);
        ParticleSystem wind = windObject.GetComponent<ParticleSystem>();
        var main = wind.main; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 128; main.startSpeed = 0f; main.startSize3D = true; main.startSizeX = 1f; main.startSizeY = .035f;
        var emission = wind.emission; emission.enabled = false;
        var shape = wind.shape; shape.enabled = false;
        var windRenderer = wind.GetComponent<ParticleSystemRenderer>();
        windRenderer.sharedMaterial = particleMaterial; windRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        windRenderer.sortingLayerName = "Default"; windRenderer.sortingOrder = 25;
        var windView = windObject.AddComponent<AccelerationWindEffect>();
        Set(windView, "train", train); Set(windView, "wind", wind); Set(windView, "viewCamera", camera);

        Touch(parent);
        SpeedMeterUI meter = UnityEngine.Object.FindFirstObjectByType<SpeedMeterUI>();
        var meterFields = new SerializedObject(meter);
        RectTransform oldLine = meterFields.FindProperty("lineRectTransform").objectReferenceValue as RectTransform;
        LevelUI levelView = UnityEngine.Object.FindFirstObjectByType<LevelUI>();
        TMP_Text levelText = all.Single(tr => tr.name == "LevelText").GetComponent<TMP_Text>();
        TMP_Text speedText = all.Single(tr => tr.name == "SpeedText").GetComponent<TMP_Text>();
        RectTransform meterRoot = meter.transform as RectTransform;
        Touch(meterRoot); Place(meterRoot, Vector2.zero, Vector2.zero, new Vector2(30f, 24f), new Vector2(136f, 84f));
        Move(speedText.rectTransform, meterRoot); Place(speedText.rectTransform, Vector2.zero, new Vector2(.5f, .5f), new Vector2(74f, 16f), new Vector2(58f, 26f));
        Touch(speedText); speedText.fontSize = 20f; speedText.enableAutoSizing = false; speedText.color = Cream; speedText.alignment = TextAlignmentOptions.Center;
        speedText.text = "320";

        RectTransform levelRoot = levelView.transform as RectTransform;
        Move(levelRoot, parent); Place(levelRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(160f, 48f));
        foreach (Transform child in levelRoot.Cast<Transform>().ToArray()) { Touch(child.gameObject); child.gameObject.SetActive(false); }
        var oldSlider = levelRoot.GetComponent<Slider>(); Touch(oldSlider); oldSlider.enabled = false;
        Move(levelText.rectTransform, levelRoot); Place(levelText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(160f, 32f));
        Touch(levelText); levelText.fontSize = timer.fontSize; levelText.enableAutoSizing = false; levelText.color = Cream; levelText.alignment = TextAlignmentOptions.TopLeft;
        levelText.text = "Lv. 1";
        RectTransform xpTrack = Rect("ExperienceTrack", levelRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -36f), new Vector2(160f, 6f));
        Graphic(xpTrack, white, Track);
        Image xp = Graphic(Rect("ExperienceFill", xpTrack, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(160f, 6f)), white, new Color32(107, 218, 136, 255));
        xp.type = Image.Type.Filled; xp.fillMethod = Image.FillMethod.Horizontal; xp.fillOrigin = 0; xp.fillAmount = 0f;
        Set(levelView, "xpFill", xp); Set(levelView, "xpBarSlider", null);
        Touch(oldLine.gameObject); oldLine.gameObject.SetActive(false);

        RectTransform fuelTrack = Rect("FuelTrack", meterRoot, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(14f, 74f));
        Graphic(fuelTrack, white, Track);
        Image fuel = Graphic(Rect("FuelFill", fuelTrack, Vector2.zero, Vector2.zero, new Vector2(2f, 2f), new Vector2(10f, 70f)), white, new Color32(230, 169, 95, 255));
        fuel.type = Image.Type.Filled; fuel.fillMethod = Image.FillMethod.Vertical; fuel.fillOrigin = 0; fuel.fillAmount = 1f;
        RectTransform arc = Rect("QuarterDial", meterRoot, Vector2.zero, Vector2.zero, new Vector2(30f, 0f), new Vector2(84f, 84f));
        Image arcImage = Graphic(arc, SpriteAt("Assets/Sprites/UI/Driving/QuarterDial.png"), Cream);
        RectTransform needle = Rect("QuarterNeedle", arc, Vector2.zero, new Vector2(0f, .5f), new Vector2(2.6f, 2.6f), new Vector2(69f, 2f));
        Graphic(needle, white, new Color32(244, 209, 141, 255)); needle.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Graphic(Rect("NeedlePivot", arc, Vector2.zero, new Vector2(.5f, .5f), new Vector2(2.6f, 2.6f), new Vector2(6f, 6f)), SpriteAt("Assets/Sprites/UI/Driving/Soul.png"), Cream);
        Set(meter, "needleRectTransform", needle); Set(meter, "fuelFill", fuel); Set(meter, "lineRectTransform", arc); Set(meter, "panelRectTransform", meterRoot);
        Set(meter, "lineImage", arcImage); Set(meter, "panelImage", null);
        Float(meter, "angleAtZeroSpeed", 0f); Float(meter, "angleAtThreshold", 45f); Float(meter, "angleAtMaxSpeed", 90f);
        Float(meter, "needleSmoothSpeed", 12f); Float(meter, "shakeStrength", 3f);

        StageProgressUI routeView = UnityEngine.Object.FindFirstObjectByType<StageProgressUI>();
        RectTransform route = routeView.transform as RectTransform;
        RectTransform line = route.Find("RouteLine") as RectTransform;
        Touch(line); Touch(route);
        RectTransform red = Rect("PursuitRoute", line, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        red.anchorMax = new Vector2(0f, 1f); red.offsetMin = red.offsetMax = Vector2.zero;
        Image redImage = Graphic(red, white, new Color32(218, 65, 73, 255));
        RectTransform icon = Rect("PursuingHandPosition", route, new Vector2(0f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -5f), new Vector2(24f, 16f));
        Graphic(icon, handObject.GetComponent<SpriteRenderer>().sprite, new Color32(237, 97, 104, 255));
        Set(routeView, "pursuingHand", hand); Set(routeView, "pursuitFill", redImage); Set(routeView, "pursuitPosition", icon);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed.");
        End(path);
    }

    private static RewardPickup MakePickup(string name, RewardPickupKind kind)
    {
        Directory.CreateDirectory("Assets/Prefabs/Gameplay");
        var root = new GameObject(name, typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(RewardPickup));
        SpriteRenderer renderer = root.GetComponent<SpriteRenderer>(); renderer.sortingOrder = 15;
        renderer.sprite = kind == RewardPickupKind.Soul ? SpriteAt("Assets/Sprites/UI/Driving/Soul.png") : white;
        renderer.color = kind == RewardPickupKind.Soul ? new Color32(132, 219, 250, 255) : kind == RewardPickupKind.Fuel ? new Color32(211, 168, 120, 255) : Color.white;
        root.transform.localScale = Vector3.one * (kind == RewardPickupKind.Flesh ? .5f : .38f);
        var body = root.GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0f; body.simulated = false;
        var collider = root.GetComponent<CircleCollider2D>(); collider.isTrigger = true; collider.radius = .4f;
        var pickup = root.GetComponent<RewardPickup>();
        Set(pickup, "spriteRenderer", renderer); Set(pickup, "body", body); Set(pickup, "pickupCollider", collider);
        var settings = new SerializedObject(pickup); settings.FindProperty("kind").enumValueIndex = (int)kind; settings.FindProperty("trackMask").intValue = 1; settings.ApplyModifiedPropertiesWithoutUndo();
        if (kind == RewardPickupKind.Flesh)
        {
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Enemy/Effects/meat set-Sheet.png").OfType<Sprite>().ToArray();
            if (sprites.Length != 5) throw new InvalidOperationException("Expected five existing flesh sprites.");
            SetArray(pickup, "fleshSprites", sprites); renderer.sprite = sprites[0];
        }
        if (kind == RewardPickupKind.Soul)
        {
            TrailRenderer trail = root.AddComponent<TrailRenderer>(); trail.sharedMaterial = particleMaterial; trail.time = .35f;
            trail.startWidth = .16f; trail.endWidth = 0f; trail.startColor = new Color(.52f, .86f, 1f, .8f); trail.endColor = new Color(.52f, .86f, 1f, 0f);
            trail.sortingOrder = 14; trail.minVertexDistance = .08f; trail.emitting = false; Set(pickup, "trail", trail);
        }
        string path = "Assets/Prefabs/Gameplay/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path); UnityEngine.Object.DestroyImmediate(root);
        return prefab.GetComponent<RewardPickup>();
    }

    private static void MakeArtwork()
    {
        const string folder = "Assets/Sprites/UI/Driving"; Directory.CreateDirectory(folder);
        WriteSprite(folder + "/WhiteSquare.png", 16, 16, (x, y) => Color.white, 16f);
        WriteSprite(folder + "/Soul.png", 64, 64, (x, y) => Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) <= 29f ? Color.white : Color.clear, 64f);
        WriteSprite(folder + "/QuarterDial.png", 256, 256, (x, y) =>
        {
            float dx = x - 8f, dy = y - 8f, r = Mathf.Sqrt(dx * dx + dy * dy);
            if (dx < 0f || dy < 0f) return Color.clear;
            if (r >= 235f && r <= 241f) return Color.white;
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            bool tick = Mathf.Abs(angle / 7.5f - Mathf.Round(angle / 7.5f)) < .045f;
            return tick && r >= 218f && r <= 234f ? new Color(1f, 1f, 1f, .72f) : Color.clear;
        }, 256f);
        WriteSprite(folder + "/Barrel.png", 16, 20, (x, y) => x < 2 || x > 13 || y < 1 || y > 18 ? Color.clear :
            x == 2 || x == 13 || y == 1 || y == 18 || y == 5 || y == 14 ? new Color(.31f, .18f, .1f) : new Color(.62f, .35f, .15f), 22f);
        white = SpriteAt(folder + "/WhiteSquare.png");
        particleMaterial = AssetDatabase.LoadAssetAtPath<Material>(folder + "/DrivingParticles.mat");
        if (particleMaterial == null)
        {
            particleMaterial = new Material(Shader.Find("Sprites/Default")); particleMaterial.name = "DrivingParticles";
            AssetDatabase.CreateAsset(particleMaterial, folder + "/DrivingParticles.mat");
        }
    }

    private static void WriteSprite(string path, int width, int height, Func<int, int, Color> pixel, float ppu)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color[width * height];
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) pixels[y * width + x] = pixel(x, y);
        texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = ppu; importer.spriteImportMode = SpriteImportMode.Single; importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.alphaIsTransparency = true; importer.SaveAndReimport();
    }

    private static Sprite SpriteAt(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
    private static long Id(UnityEngine.Object obj) => obj == null ? 0 : unchecked((long)GlobalObjectId.GetGlobalObjectIdSlow(obj).targetObjectId);
    private static void Touch(UnityEngine.Object obj) { if (obj != null && Id(obj) != 0) touched.Add(Id(obj)); }
    private static void Begin(string path) { touched.Clear(); File.Copy(path, Snapshot("before", path), true); }
    private static void End(string path) { File.Copy(path, Snapshot("after", path), true); receipts.assets.Add(new Receipt { path = path, modifiedIds = touched.ToArray() }); }
    private static string Snapshot(string section, string path) { string result = Path.Combine(output, section, path); Directory.CreateDirectory(Path.GetDirectoryName(result)); return result; }
    private static void Set(UnityEngine.Object owner, string field, UnityEngine.Object value) { if (owner == null) throw new InvalidOperationException("Missing owner " + field); Touch(owner); var so = new SerializedObject(owner); var property = so.FindProperty(field); if (property == null) throw new InvalidOperationException(owner.name + ": missing field " + field); property.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    private static void Float(UnityEngine.Object owner, string field, float value) { Touch(owner); var so = new SerializedObject(owner); so.FindProperty(field).floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    private static void SetArray<T>(UnityEngine.Object owner, string field, T[] values) where T : UnityEngine.Object { Touch(owner); var so = new SerializedObject(owner); var property = so.FindProperty(field); property.arraySize = values.Length; for (int index = 0; index < values.Length; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index]; so.ApplyModifiedPropertiesWithoutUndo(); }
    private static void Move(RectTransform tr, Transform parent) { Touch(tr); if (tr.parent != null) Touch(tr.parent); tr.SetParent(parent, false); }
    private static void Place(RectTransform tr, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size) { Touch(tr); tr.anchorMin = tr.anchorMax = anchor; tr.pivot = pivot; tr.anchoredPosition = position; tr.sizeDelta = size; tr.localScale = Vector3.one; tr.localRotation = Quaternion.identity; }
    private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size) { var go = new GameObject(name, typeof(RectTransform)); go.layer = 5; var tr = go.GetComponent<RectTransform>(); tr.SetParent(parent, false); Place(tr, anchor, pivot, position, size); return tr; }
    private static Image Graphic(RectTransform tr, Sprite sprite, Color color) { var graphic = tr.gameObject.AddComponent<Image>(); graphic.sprite = sprite; graphic.color = color; graphic.raycastTarget = false; return graphic; }
}
