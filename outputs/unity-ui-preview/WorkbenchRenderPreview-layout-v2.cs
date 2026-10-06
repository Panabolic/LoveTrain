using System;
using System.IO;
using System.Reflection;
using DG.Tweening;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

// Disposable clone only. Produces exact authored UI renders without entering Play Mode.
public static class WorkbenchRenderPreview
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static Camera camera;
    private static RenderTexture target;
    private static Canvas canvas;
    private static LevelUpUIManager owner;

    public static void Run()
    {
        string destination = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
        try
        {
            if (Application.companyName != "CodexUIRenderPreview" || !Application.productName.StartsWith("WorkbenchPreview_"))
                throw new InvalidOperationException("Isolated PlayerPrefs namespace is not verified; refusing preview.");
            EnglishLocalization.SetLanguage(false);
            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Junmo.unity", OpenSceneMode.Single);
            // OptionUI is authored active and normally hidden by gameplay startup.
            // The UI-only fixture does not run that startup or apply user display preferences.
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform tr in root.GetComponentsInChildren<Transform>(true))
                    if (tr.name == "OptionUI") tr.gameObject.SetActive(false);
            owner = Find<LevelUpUIManager>();
            var hud = Get<InventoryUI>(owner, "inventoryUI");
            var inventory = Get<Inventory>(owner, "playerInventory");
            var wallet = inventory.GetComponent<TrainLevelManager>();
            canvas = hud.GetComponentInParent<Canvas>();
            if (canvas == null) throw new Exception("Inventory parent Canvas missing.");

            var economy = new RunPartEconomy(7);
            economy.AddReward(100, 0);
            Set(wallet, "economy", economy); // No gameplay reward/persistence callback.
            inventory.items.Clear();
            Item_SO revolver = Item("Revolver");
            Item_SO heart = Item("BeatingHeart");
            Item_SO gear = Item("BlueGear");
            Item_SO bible = Item("BloodyBible");
            Item_SO missile = Item("PoisonMissileLauncher");
            Item_SO rearGun = Item("RearGun");
            inventory.items.Add(new ItemInstance(bible) { equippedSlotIndex = 0 });
            inventory.items.Add(new ItemInstance(rearGun) { equippedSlotIndex = 3 });
            inventory.items.Add(new ItemInstance(missile) { equippedSlotIndex = 6 });
            Call(inventory, "Awake");
            for (int i = 0; i < 10; i++) Call(hud.GetEquipmentSlot(i), "Awake");
            LevelUpUIManager.Instance = null;
            Call(owner, "Awake");
            Call(hud, "Start");
            Call(owner, "Start");

            // UI-only rendering uses the actual prefab and actual scene HUD. World gameplay is not run.
            GraphicsSettings.defaultRenderPipeline = null;
            QualitySettings.renderPipeline = null;
            foreach (Camera existing in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) existing.enabled = false;
            foreach (Canvas other in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (other != canvas && other.isRootCanvas) other.gameObject.SetActive(false);
            foreach (Transform tr in canvas.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = 5;
            var cameraObject = new GameObject("PreviewCamera");
            camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.08f, 0.105f, 1f);
            camera.cullingMask = 1 << 5;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            target.Create();
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvas.pixelPerfect = false;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                // Equivalent to authored width-matched scaling at the fixed 1280px preview width.
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1280f / 800f;
                Call(scaler, "Handle");
            }
            Layout();

            if (!(bool)Call(owner, "EnsureView")) throw new Exception("Workbench view did not instantiate.");
            Set(owner, "offers", new Item_SO[] { revolver, heart, gear });
            Open("Creation", wallet);
            Call(owner, "ShowOffers");
            FinishTransition();
            TrainItemWorkbenchView view = Get<TrainItemWorkbenchView>(owner, "view");
            foreach (Transform tr in view.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = 5;
            Layout();
            Capture(Path.Combine(destination, "creation-layout-v2.png"));
            var events = Find<EventSystem>();
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            var pointer = new PointerEventData(events)
            {
                position = camera.WorldToScreenPoint(view.Choices[0].transform.position),
                button = PointerEventData.InputButton.Left,
                pointerCurrentRaycast = new RaycastResult { module = raycaster },
                pointerPressRaycast = new RaycastResult { module = raycaster }
            };
            owner.ShowItemTooltip(revolver, 1, pointer);
            Layout();
            Capture(Path.Combine(destination, "creation-hover-layout-v2.png"));
            owner.HideItemTooltip();
            pointer.position = camera.WorldToScreenPoint(hud.GetEquipmentSlot(9).transform.position) + new Vector3(-30f, -50f, 0f);
            view.Choices[2].OnBeginDrag(pointer);
            Layout();
            Capture(Path.Combine(destination, "creation-drag-layout-v2.png"));
            view.Choices[2].OnEndDrag(pointer);
            owner.CloseWorkbench();
            FinishTransition();

            Open("Upgrade", wallet);
            Call(owner, "RefreshUpgrade");
            FinishTransition();
            owner.SelectUpgradeItem(inventory.items[0], hud.GetEquipmentSlot(0));
            FinishTransition();
            Layout();
            Capture(Path.Combine(destination, "upgrade-layout-v2.png"));
            File.WriteAllText(Path.Combine(destination, "render-result-v2.txt"),
                "PASS: four actual Unity 6000.2.3f1 UI-only EditMode renders from copied TrainItemWorkbench.prefab and Junmo InventoryUI. " +
                "No Play Mode, screen capture, live project change, gameplay, or GraphicRaycaster claim. Representative real item assets, 100 flesh and 7 souls fixture data. Built-in UI camera on neutral backdrop.");
            Debug.Log("Workbench preview PNGs written to " + destination);
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            File.WriteAllText(Path.Combine(destination, "render-result-v2.txt"), "FAIL: " + ex);
            EditorApplication.Exit(1);
        }
    }

    private static void Layout()
    {
        Canvas.ForceUpdateCanvases();
        foreach (RectTransform rect in canvas.GetComponentsInChildren<RectTransform>(true))
            if (rect.gameObject.activeInHierarchy && rect.GetComponent<LayoutGroup>() != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
            if (text.gameObject.activeInHierarchy) text.ForceMeshUpdate(true, true);
        Canvas.ForceUpdateCanvases();
    }
    private static void Capture(string path)
    {
        Layout();
        camera.Render();
        RenderTexture prior = RenderTexture.active;
        RenderTexture.active = target;
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        RenderTexture.active = prior;
    }
    private static void Open(string mode, TrainLevelManager wallet)
    {
        Type modeType = typeof(LevelUpUIManager).GetNestedType("Mode", BindingFlags.NonPublic);
        Call(owner, "Open", Enum.Parse(modeType, mode), wallet);
    }
    private static void FinishTransition() { var sequence = Get<Sequence>(owner, "transition"); sequence?.Complete(true); }
    private static Item_SO Item(string name)
    {
        var item = AssetDatabase.LoadAssetAtPath<Item_SO>("Assets/Datas/Items/" + name + ".asset");
        if (item == null || item.iconSprite == null) throw new Exception("Item asset/icon missing: " + name);
        return item;
    }
    private static T Find<T>() where T : UnityEngine.Object
    {
        var objects = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (T item in objects) if (!(item is Component component) || component.gameObject.scene.IsValid()) return item;
        throw new Exception("Scene object missing: " + typeof(T).Name);
    }
    private static T Get<T>(object targetObject, string field) => (T)targetObject.GetType().GetField(field, Members).GetValue(targetObject);
    private static void Set(object targetObject, string field, object value) => targetObject.GetType().GetField(field, Members).SetValue(targetObject, value);
    private static object Call(object targetObject, string method, params object[] args) => targetObject.GetType().GetMethod(method, Members).Invoke(targetObject, args);
}
