using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Narrow authoring in the existing disposable copy; original assets are merged by document ID.
public static class EffectVisibilityAuthoring
{
    [Serializable] private sealed class Receipt { public string path; public long[] modifiedIds; }
    [Serializable] private sealed class Receipts { public List<Receipt> assets = new List<Receipt>(); }
    private static readonly HashSet<long> touched = new HashSet<long>();
    private static readonly Receipts receipts = new Receipts();
    private static string output;

    public static void Run()
    {
        output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../effect-visibility-validation"));
        try
        {
            if (!Application.dataPath.Replace('\\', '/').EndsWith("/outputs/drive-boss-validation/project/Assets", StringComparison.OrdinalIgnoreCase) ||
                !Application.productName.StartsWith("DriveBossPreview_", StringComparison.Ordinal))
                throw new InvalidOperationException("Refuse authoring outside the isolated task copy.");
            Directory.CreateDirectory(output);
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Sprites/UI/Driving/PickupOutline.shader");
            if (shader == null || !shader.isSupported || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("White silhouette shader did not compile.");
            const string materialPath = "Assets/Sprites/UI/Driving/PickupOutline.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
            Vector2[] directions = { Vector2.left, Vector2.right, Vector2.up, Vector2.down,
                new Vector2(-1, -1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(1, 1) };
            foreach (string name in new[] { "FleshPickup", "SoulPickup", "FuelPickup" })
            {
                string path = "Assets/Prefabs/Gameplay/" + name + ".prefab";
                Begin(path);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                var main = root.GetComponent<SpriteRenderer>(); Touch(main); main.sortingLayerName = "ForeGround";
                foreach (TrailRenderer trail in root.GetComponentsInChildren<TrailRenderer>(true)) { Touch(trail); trail.sortingLayerName = "ForeGround"; }
                var existing = root.transform.Find("WhiteOutline");
                if (existing != null) throw new InvalidOperationException("Refuse adding outlines twice.");
                Touch(root.transform);
                var outlineRoot = new GameObject("WhiteOutline"); outlineRoot.transform.SetParent(root.transform, false);
                var renderers = new SpriteRenderer[directions.Length];
                for (int index = 0; index < directions.Length; index++)
                {
                    var child = new GameObject("Edge" + index, typeof(SpriteRenderer)); child.transform.SetParent(outlineRoot.transform, false);
                    child.transform.localPosition = (Vector3)(directions[index].normalized * .16f);
                    var renderer = child.GetComponent<SpriteRenderer>();
                    renderer.sprite = main.sprite; renderer.sharedMaterial = material; renderer.color = Color.white;
                    renderer.sortingLayerID = main.sortingLayerID; renderer.sortingOrder = main.sortingOrder - 1;
                    renderers[index] = renderer;
                }
                var pickup = root.GetComponent<RewardPickup>(); Touch(pickup);
                var so = new SerializedObject(pickup); var outlines = so.FindProperty("outlineRenderers"); outlines.arraySize = renderers.Length;
                for (int index = 0; index < renderers.Length; index++) outlines.GetArrayElementAtIndex(index).objectReferenceValue = renderers[index];
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path); End(path); PrefabUtility.UnloadPrefabContents(root);
            }
            const string barrelPath = "Assets/Prefabs/Enemy/Mobs/FuelBarrel.prefab";
            Begin(barrelPath);
            var barrel = PrefabUtility.LoadPrefabContents(barrelPath); var barrelRenderer = barrel.GetComponent<SpriteRenderer>();
            Touch(barrelRenderer); barrelRenderer.sortingLayerName = "Monster";
            PrefabUtility.SaveAsPrefabAsset(barrel, barrelPath); End(barrelPath); PrefabUtility.UnloadPrefabContents(barrel);

            const string scenePath = "Assets/Scenes/Junmo.unity";
            Begin(scenePath);
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var effect = UnityEngine.Object.FindFirstObjectByType<AccelerationWindEffect>();
            if (effect == null) throw new InvalidOperationException("Authored wind effect is missing.");
            ParticleSystemRenderer wind = effect.GetComponent<ParticleSystemRenderer>(); Touch(wind); wind.sortingLayerName = "ForeGround";
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed.");
            End(scenePath);
            AssetDatabase.SaveAssets();
            File.WriteAllText(Path.Combine(output, "authoring-receipt.json"), JsonUtility.ToJson(receipts, true));
            File.WriteAllText(Path.Combine(output, "authoring-result.txt"), "PASS: ForeGround pickup sprites/trail/wind, Monster barrel, eight authored white outline edges per pickup with original sprite assets preserved.");
            EditorApplication.Exit(0);
        }
        catch (Exception exception) { Debug.LogException(exception); File.WriteAllText(Path.Combine(output, "authoring-result.txt"), "FAIL: " + exception); EditorApplication.Exit(1); }
    }

    private static long Id(UnityEngine.Object obj) => unchecked((long)GlobalObjectId.GetGlobalObjectIdSlow(obj).targetObjectId);
    private static void Touch(UnityEngine.Object obj) { if (obj != null && Id(obj) != 0) touched.Add(Id(obj)); }
    private static string Snapshot(string section, string path) { string result = Path.Combine(output, section, path); Directory.CreateDirectory(Path.GetDirectoryName(result)); return result; }
    private static void Begin(string path) { touched.Clear(); File.Copy(path, Snapshot("before", path), true); }
    private static void End(string path) { File.Copy(path, Snapshot("after", path), true); receipts.assets.Add(new Receipt { path = path, modifiedIds = touched.ToArray() }); }
}
