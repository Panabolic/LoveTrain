using System;
using System.IO;
using System.Reflection;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

// Disposable clone only. Actual source method probes and production logical-root geometry.
public static class OctoberFixValidation
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly StringBuilder Log = new StringBuilder();
    static int checks, failures;
    static string output;
    static LevelUpUIManager owner;
    static Inventory inventory;
    static InventoryUI equipment;
    static TrainLevelManager wallet;
    static FleshHud flesh;
    static RectTransform hud;
    static Canvas canvas;
    static Camera uiCamera, worldCamera;
    static RenderTexture target;
    static FixedAspectRatioController aspect;
    static GameObject[] stages;
    static Keyboard keyboard;

    public static void Run()
    {
        output = @"C:\Users\nadom\Desktop\졸업작품\LoveTrain\outputs\unity-ui-preview";
        try
        {
            Check(Application.companyName == "CodexUIRenderPreview" && Application.productName.StartsWith("WorkbenchPreview_"), "Isolated save identity verified before preference access");
            if (failures > 0) throw new InvalidOperationException("Wrong project identity");
            Check(!EditorApplication.isPlaying, "EditMode only");
            EnglishLocalization.SetLanguage(false);
            SetStatic(typeof(PermanentUpgradeProgress), "loaded", true);
            SetStatic(typeof(PermanentUpgradeProgress), "souls", 7);
            keyboard = InputSystem.AddDevice<Keyboard>("OctoberFixFixtureKeyboard"); keyboard.MakeCurrent();
            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Junmo.unity", OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform tr in root.GetComponentsInChildren<Transform>(true))
                    if (tr.name == "OptionUI") tr.gameObject.SetActive(false);
            owner = Find<LevelUpUIManager>(); inventory = Get<Inventory>(owner, "playerInventory");
            equipment = Get<InventoryUI>(owner, "inventoryUI"); wallet = inventory.GetComponent<TrainLevelManager>();
            flesh = Find<FleshHud>(); hud = (RectTransform)flesh.transform; canvas = hud.GetComponentInParent<Canvas>();
            worldCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
            var game = Find<GameManager>(); SetStatic(typeof(GameManager), "<Instance>k__BackingField", game);
            Set(game, "<CurrentState>k__BackingField", GameState.Playing); Time.timeScale = 1f;
            Seed(0); inventory.items.Clear(); Call(inventory, "Awake");
            for (int i = 0; i < 10; i++) Call(equipment.GetEquipmentSlot(i), "Awake");
            LevelUpUIManager.Instance = null; Call(owner, "Awake"); Call(owner, "OnEnable");
            Call(equipment, "Start"); Call(owner, "Start");
            ProbeLegacySockets(); ProbeFreeUpgrade();
            PrepareCameras();
            var manager = Find<StageManager>(); Transform parent = Get<Transform>(manager, "bgParent");
            Check(parent != null, "Actual StageManager bgParent resolves");
            foreach (Transform child in parent) child.gameObject.SetActive(false);
            string[] paths = { "Assets/Prefabs/Map/BeltScroll.prefab", "Assets/Prefabs/Map/BeltScroll 1.prefab" };
            stages = new GameObject[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
                stages[i] = UnityEngine.Object.Instantiate(prefab, parent);
                Check(Vector3.Distance(stages[i].transform.localPosition, prefab.transform.localPosition) < 0.0001f, "Stage " + i + " retains authored prefab root local position under actual bgParent");
            }
            Physics2D.SyncTransforms();
            foreach (Vector2Int size in new[] { new Vector2Int(1920,1080), new Vector2Int(1280,720), new Vector2Int(960,540), new Vector2Int(800,600), new Vector2Int(3440,1440), new Vector2Int(640,360) })
            {
                Configure(size.x, size.y); Seed(105); Refresh(); CheckGeometry(size);
                foreach (bool english in new[] { false, true })
                    foreach (int amount in new[] { 0, 105, 1170, int.MaxValue })
                    {
                        EnglishLocalization.SetLanguage(english); Seed(amount, amount == int.MaxValue ? int.MaxValue : 7); Refresh(); CheckText(size, english, amount);
                    }
                EnglishLocalization.SetLanguage(false); Seed(105); Refresh();
                if (size.x == 1280) Capture("flesh-static-rail-1280x720.png");
                if (size.x == 800) Capture("flesh-static-rail-800x600.png");
            }
            Log.AppendLine("RESULT " + (failures == 0 ? "PASS" : "FAIL") + ": " + checks + " checks, " + failures + " failures. Actual copied-source EditMode method probes and 6 RenderTexture dimensions using production FixedAspectRatioController roots; no PlayMode, user save, real input/raycast, camera-shake or gameplay-motion claim.");
            File.WriteAllText(Path.Combine(output, "october-fix-validation-result.txt"), Log.ToString());
            Debug.Log(Log.ToString()); EditorApplication.Exit(failures == 0 ? 0 : 1);
        }
        catch (Exception ex)
        {
            Log.AppendLine("EXCEPTION " + ex); File.WriteAllText(Path.Combine(output, "october-fix-validation-result.txt"), Log.ToString());
            Debug.LogException(ex); EditorApplication.Exit(1);
        }
        finally { if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard); }
    }

    static void ProbeLegacySockets()
    {
        int before = AnchorCount();
        MethodInfo method = typeof(Item_SO).GetMethod("InstantiateVisual", F);
        foreach (string name in new[] { "BloodyBible", "BeatingHeart", "RedGear", "PoisonMissileLauncher", "RearGun", "LaserGun" })
        {
            var item = AssetDatabase.LoadAssetAtPath<Item_SO>("Assets/Datas/Items/" + name + ".asset");
            Check(item != null && item.instantiatedPrefab != null, name + " visual source resolves");
            for (int slot = 0; slot < 10; slot++)
            {
                var instance = new ItemInstance(item) { equippedSlotIndex = slot };
                var spawned = (GameObject)method.Invoke(item, new object[] { inventory.gameObject, instance });
                Transform expected = string.IsNullOrEmpty(item.attachmentSocketName) ? inventory.transform : Named(inventory.transform, item.attachmentSocketName);
                Check(spawned != null && spawned.transform.parent == expected, name + ": UI slot " + slot + " preserves exact legacy socket/root");
                Check(spawned.transform.localPosition.sqrMagnitude < 0.000001f, name + ": local zero placement");
                UnityEngine.Object.DestroyImmediate(spawned);
            }
        }
        Check(AnchorCount() == before, "Common visual helper creates no EquipmentSlot_ transforms");
        var generated = Get<Transform[]>(inventory, "generatedAnchors");
        Check(Array.TrueForAll(generated, value => value == null), "Common visual helper never requests generated equipment anchors");
    }
    static int AnchorCount() { int n=0; foreach (Transform t in inventory.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("EquipmentSlot_")) n++; return n; }
    static void ProbeFreeUpgrade()
    {
        Check(Get<int>(owner, "upgradeCostPerCell") == 0, "Authored scene test upgrade cost is zero");
        var item = AssetDatabase.LoadAssetAtPath<Item_SO>("Assets/Datas/Items/BloodyBible.asset");
        var instance = new ItemInstance(item) { equippedSlotIndex = 2 }; inventory.items.Add(instance); Call(equipment, "RefreshUI");
        owner.ShowUpgradeEvent(); Finish(); owner.SelectUpgradeItem(instance, equipment.GetEquipmentSlot(2)); Finish();
        var view = Get<TrainItemWorkbenchView>(owner, "view");
        Check(Get<Button>(view, "upgradeButton").interactable, "Zero-flesh wallet enables zero-cost upgrade");
        for (int level = 2; level <= item.MaxUpgrade; level++)
        {
            owner.UpgradeSelectedItem(); Check(instance.currentUpgrade == level && wallet.Flesh == 0, "Free upgrade reaches " + level + " without charging zero-flesh wallet");
        }
        owner.UpgradeSelectedItem(); Check(instance.currentUpgrade == item.MaxUpgrade && wallet.Flesh == 0, "Asset max upgrade remains respected");
        Check(!Get<Button>(view, "upgradeButton").interactable, "Maximum-level upgrade button disables");
        owner.CloseWorkbench(); Finish(); inventory.items.Clear(); Call(owner, "InvalidateCreationAvailability"); Call(equipment, "RefreshUI");
    }
    static void PrepareCameras()
    {
        GraphicsSettings.defaultRenderPipeline = null; QualitySettings.renderPipeline = null;
        foreach (Camera camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) camera.enabled = false;
        foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 5;
        foreach (SpriteRenderer sprite in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) sprite.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        uiCamera = new GameObject("OctoberUiCamera").AddComponent<Camera>(); uiCamera.enabled = false;
        uiCamera.orthographic = true; uiCamera.transform.position = new Vector3(0,0,-10); uiCamera.nearClipPlane = 0.01f;
        uiCamera.clearFlags = CameraClearFlags.Depth; uiCamera.cullingMask = 1 << 5;
        worldCamera.cullingMask &= ~(1 << 5);
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = uiCamera; canvas.planeDistance = 1f;
        aspect = new GameObject("OctoberActualAspectProbe").AddComponent<FixedAspectRatioController>();
    }
    static void Configure(int width, int height)
    {
        if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
        target = new RenderTexture(width,height,24); target.Create(); uiCamera.targetTexture = target; worldCamera.targetTexture = target;
        uiCamera.rect = new Rect(0,0,1,1); uiCamera.aspect = (float)width/height;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; CallStatic(typeof(FixedAspectRatioController),"ApplyCanvasScaler",canvas); Call(scaler,"Handle"); Canvas.ForceUpdateCanvases();
        // ScreenSpaceCamera/RenderTexture roots settle on a native render; EditMode lacks the player Canvas phase.
        uiCamera.Render(); Call(scaler,"Handle"); ((RectTransform)canvas.transform).ForceUpdateRectTransforms(); Canvas.ForceUpdateCanvases();
        Log.AppendLine("NATIVE CANVAS rect="+((RectTransform)canvas.transform).rect+", pixelRect="+canvas.pixelRect+", renderingDisplaySize="+canvas.renderingDisplaySize+", sourceScaleFactor="+canvas.scaleFactor+", uiPixelRect="+uiCamera.pixelRect);
        Rect content = (Rect)CallStatic(typeof(FixedAspectRatioController),"CalculateContentPixelRect",width,height);
        Rect normalized = (Rect)CallStatic(typeof(FixedAspectRatioController),"PixelRectToNormalized",content,(float)width,(float)height);
        SetStatic(typeof(FixedAspectRatioController),"contentPixelRect",content); SetStatic(typeof(FixedAspectRatioController),"hasContentPixelRect",true);
        worldCamera.rect = normalized; worldCamera.aspect = content.width/content.height;
        Call(aspect,"ApplyCanvasRoots",(RectTransform)canvas.transform,normalized); Layout();
        uiCamera.Render(); Layout();
        Rect actualContent = Pixels((RectTransform)canvas.transform.Find("AspectSafeAreaRoot/AspectContentRoot"));
        Check(Mathf.Abs(actualContent.xMin-content.xMin)<.5f && Mathf.Abs(actualContent.yMin-content.yMin)<.5f && Mathf.Abs(actualContent.xMax-content.xMax)<.5f && Mathf.Abs(actualContent.yMax-content.yMax)<.5f, width+"x"+height+": actual logical 800x450 content corners equal target content pixel rect AFTER native render");
        Log.AppendLine("CONTENT CORNERS actual="+actualContent+", expected="+content+", canvas="+((RectTransform)canvas.transform).rect);
        Log.AppendLine("RESOLUTION " + width + "x" + height + " content=" + content + " authored logical HUD=" + hud.rect + " root scale=" + canvas.transform.Find("AspectSafeAreaRoot/AspectContentRoot").localScale);
    }
    static void CheckGeometry(Vector2Int size)
    {
        Rect bounds = Pixels(hud); Rect content = FixedAspectRatioController.ContentPixelRect; float scale=content.width/800f;
        Check(Mathf.Abs(hud.rect.height-78f)<0.001f && Mathf.Abs(hud.rect.width-150f)<0.001f,"Authored HUD is 150x78");
        Check(Contains(content,bounds),size+": HUD stays inside actual production content");
        foreach (Graphic graphic in hud.GetComponentsInChildren<Graphic>(true)) Check(Contains(bounds,Pixels(graphic.rectTransform)),size+": graphic contained, including shadow: "+graphic.name);
        for (int i=0;i<10;i++) Check(!Pixels(equipment.GetEquipmentSlot(i).GetComponent<RectTransform>()).Overlaps(bounds),size+": equipment slot "+i+" has zero HUD overlap");
        foreach (GameObject stage in stages)
        {
            float railMin=float.PositiveInfinity;
            foreach (SpriteRenderer sprite in stage.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!sprite.name.StartsWith("Lane") || sprite.sprite==null) continue;
                railMin=Mathf.Min(railMin,VisualRailBottom(sprite));
                var collider=sprite.GetComponent<BoxCollider2D>();
                if (collider!=null) Log.AppendLine("CONTACT "+stage.name+" "+sprite.name+" collider top screen="+worldCamera.WorldToScreenPoint(new Vector3(worldCamera.transform.position.x,collider.bounds.max.y,0)).y);
            }
            float gap=railMin-bounds.yMax;
            Log.AppendLine("RAIL "+size+" "+stage.name+" visible alpha lower screen="+railMin+", HUD top="+bounds.yMax+", pixel gap="+gap+", logical gap="+(gap/scale));
            Check(!float.IsInfinity(railMin) && gap/scale>=6.5f,size+": HUD clears actual visible rail alpha by >=6.5 logical pixels, "+stage.name);
        }
    }
    static float VisualRailBottom(SpriteRenderer renderer)
    {
        Sprite sprite=renderer.sprite; string path=AssetDatabase.GetAssetPath(sprite);
        var pixels=new Texture2D(2,2); if (!pixels.LoadImage(File.ReadAllBytes(path))) throw new Exception("Cannot read source sprite alpha "+path);
        Rect rect=sprite.rect; int bottom=-1; var colors=pixels.GetPixels32();
        for(int y=(int)rect.yMin;y<(int)rect.yMax && bottom<0;y++)
            for(int x=(int)rect.xMin;x<(int)rect.xMax;x++) if(colors[y*pixels.width+x].a>0) {bottom=y;break;}
        UnityEngine.Object.DestroyImmediate(pixels);
        if(bottom<0) return float.PositiveInfinity;
        float localY=(bottom-rect.yMin-sprite.pivot.y)/sprite.pixelsPerUnit;
        Vector3 world=renderer.transform.TransformPoint(new Vector3(0,localY,0));
        Log.AppendLine("ALPHA "+renderer.name+" source="+path+" bottom row="+bottom+" PPU="+sprite.pixelsPerUnit+" worldLower="+world.y);
        return worldCamera.WorldToScreenPoint(world).y;
    }
    static void CheckText(Vector2Int size,bool english,int amount)
    {
        foreach(TMP_Text text in hud.GetComponentsInChildren<TMP_Text>(true))
        {
            if(!text.gameObject.activeInHierarchy) continue;
            Check(!text.isTextOverflowing && text.textInfo.lineCount<=1,size+": text fits "+(english?"en":"ko")+" amount="+amount+" "+text.name);
            Log.AppendLine("TEXT "+text.name+" font="+text.fontSize+" rect="+text.rectTransform.rect.size+" overflow="+text.isTextOverflowing+" lines="+text.textInfo.lineCount+" value="+text.text);
        }
    }
    static void Seed(int amount,int souls=7) { var economy=new RunPartEconomy(souls); economy.AddReward(amount); Set(wallet,"economy",economy); }
    static void Refresh() { Call(flesh,"RefreshResources"); Call(flesh,"RefreshCreationHint",true); Layout(); }
    static void Layout() { Canvas.ForceUpdateCanvases(); foreach(TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true)) if(text.gameObject.activeInHierarchy) text.ForceMeshUpdate(true,true); Canvas.ForceUpdateCanvases(); }
    static Rect Pixels(RectTransform rect) { var corners=new Vector3[4];rect.GetWorldCorners(corners);Vector2 min=new Vector2(float.PositiveInfinity,float.PositiveInfinity),max=new Vector2(float.NegativeInfinity,float.NegativeInfinity);foreach(Vector3 corner in corners) {Vector2 p=uiCamera.WorldToScreenPoint(corner);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}return Rect.MinMaxRect(min.x,min.y,max.x,max.y); }
    static bool Contains(Rect outer,Rect inner) => inner.xMin>=outer.xMin-.25f && inner.xMax<=outer.xMax+.25f && inner.yMin>=outer.yMin-.25f && inner.yMax<=outer.yMax+.25f;
    static void Capture(string name)
    {
        for(int i=0;i<stages.Length;i++) stages[i].SetActive(i==0);
        foreach(SpriteRenderer sprite in stages[0].GetComponentsInChildren<SpriteRenderer>(true)) sprite.sharedMaterial=new Material(Shader.Find("Sprites/Default"));
        Layout(); worldCamera.Render(); uiCamera.Render(); RenderTexture old=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=old;
        foreach(GameObject stage in stages) stage.SetActive(true);
    }
    static Transform Named(Transform root,string name) { foreach(Transform tr in root.GetComponentsInChildren<Transform>(true)) if(tr.name==name)return tr; return null; }
    static T Find<T>() where T:Component { foreach(T value in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None)) if(value.gameObject.scene.IsValid())return value;throw new Exception("Missing "+typeof(T)); }
    static T Get<T>(object value,string field) => (T)value.GetType().GetField(field,F).GetValue(value);
    static void Set(object value,string field,object data) => value.GetType().GetField(field,F).SetValue(value,data);
    static void SetStatic(Type type,string field,object data) => type.GetField(field,F).SetValue(null,data);
    static object Call(object value,string method,params object[] args) => value.GetType().GetMethod(method,F).Invoke(value,args);
    static object CallStatic(Type type,string method,params object[] args) => type.GetMethod(method,F).Invoke(null,args);
    static void Finish() => Get<Sequence>(owner,"transition")?.Complete(true);
    static void Check(bool valid,string label) { checks++;if(!valid)failures++;Log.AppendLine((valid?"PASS ":"FAIL ")+label); }
}
