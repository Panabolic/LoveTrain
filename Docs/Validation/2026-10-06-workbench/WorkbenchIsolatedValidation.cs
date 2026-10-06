using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class WorkbenchIsolatedValidation
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public static void Run()
    {
        int count = 0;
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TrainItemWorkbench.prefab");
            Require(prefab != null, "Prefab imports", ref count);
            var view = prefab.GetComponent<TrainItemWorkbenchView>();
            Require(view != null, "View script resolves", ref count);
            var serialized = new SerializedObject(view);
            string[] refs =
            {
                "rootGroup", "trainHost", "title", "resources", "closeButton", "choicesRoot", "choicesGroup",
                "creationHint", "rerollButton", "rerollLabel", "upgradeRoot", "upgradeSlot", "upgradeIcon",
                "beforeDescription", "afterDescription", "beforeHeading", "afterHeading", "upgradeStatus",
                "upgradeActions", "upgradeButton", "removeButton", "cancelButton", "largeCloseButton",
                "floatingIcon", "tooltipRoot", "tooltipTitle", "tooltipDescription"
            };
            foreach (var field in refs)
            {
                var prop = serialized.FindProperty(field);
                Require(prop != null && prop.objectReferenceValue != null, "View reference " + field, ref count);
            }
            var choices = serialized.FindProperty("choices");
            Require(choices != null && choices.arraySize == 3, "Three choices", ref count);
            for (int i = 0; i < 3; i++) Require(choices.GetArrayElementAtIndex(i).objectReferenceValue != null, "Choice " + i, ref count);
            Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab) == 0, "Prefab root missing scripts", ref count);
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Junmo.unity", OpenSceneMode.Single);
            int missing = 0; LevelUpUIManager owner = null; EventObjectSpawner spawner = null; GameManager game = null; EventSystem events = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                {
                    missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject);
                    if (owner == null) owner = tr.GetComponent<LevelUpUIManager>();
                    if (spawner == null) spawner = tr.GetComponent<EventObjectSpawner>();
                    if (game == null) game = tr.GetComponent<GameManager>();
                    if (events == null) events = tr.GetComponent<EventSystem>();
                }
            Require(owner != null, "Scene workshop owner script resolves", ref count);
            Require(spawner != null, "Scene event spawner script resolves", ref count);
            Require(game != null && events != null, "Scene game manager and event system resolve", ref count);
            Require(missing == 0, "Scene has no missing scripts", ref count);
            var ownerData = new SerializedObject(owner);
            foreach (var field in new[] { "inventoryUI", "workbenchPrefab", "playerInventory", "itemDatabase" })
                Require(ownerData.FindProperty(field).objectReferenceValue != null, "Scene owner reference " + field, ref count);
            Require(ownerData.FindProperty("upgradeCostPerCell").intValue == -1, "Upgrade cost remains deferred", ref count);
            var spawnerData = new SerializedObject(spawner);
            Require(spawnerData.FindProperty("firstSpawnTime").floatValue == 60f, "First event 60", ref count);
            Require(spawnerData.FindProperty("spawnInterval").floatValue == 60f, "Event interval 60", ref count);
            Require(spawnerData.FindProperty("eventsPerStage").intValue == 3, "Three ordinary events", ref count);

            ProbeEditorInteractions(owner, game, events, ref count);

            string result = "PASS Unity asset/scene and Editor-method interaction validation: " + count +
                " checks. No Play Mode or actual pointer-raycast claim.";
            Debug.Log(result);
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../../asset-validation-result.txt")), result);
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../../asset-validation-result.txt")), "FAIL after " + count + " checks: " + ex);
            EditorApplication.Exit(1);
        }
    }

    private static void ProbeEditorInteractions(LevelUpUIManager owner, GameManager game, EventSystem events, ref int count)
    {
        string oldCompany = PlayerSettings.companyName, oldProduct = PlayerSettings.productName;
        float oldTime = Time.timeScale;
        SimulationMode2D oldPhysics = Physics2D.simulationMode;
        object oldGameInstance = GetStatic(typeof(GameManager), "<Instance>k__BackingField");
        LevelUpUIManager oldOwnerInstance = LevelUpUIManager.Instance;
        var inventory = Get<Inventory>(owner, "playerInventory");
        var hud = Get<InventoryUI>(owner, "inventoryUI");
        var wallet = inventory.GetComponent<TrainLevelManager>();
        var originalDatabase = Get<ItemDatabase>(owner, "itemDatabase");
        var database = ScriptableObject.CreateInstance<ItemDatabase>();
        var dummyItems = new List<Item_SO>();
        var originalItems = new List<ItemInstance>(inventory.items);
        var spriteTexture = new Texture2D(2, 2);
        var sprite = Sprite.Create(spriteTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
        const string probeCompany = "CodexWorkbenchIsolatedValidation";
        string probeProduct = "WorkbenchProbe_" + Guid.NewGuid().ToString("N");
        bool namespaceVerified = false;
        bool ownerStarted = false, hudStarted = false;
        try
        {
            PlayerSettings.companyName = probeCompany;
            PlayerSettings.productName = probeProduct;
            // Refuse resource methods if Application has not adopted the isolated clone identity.
            Require(Application.companyName == probeCompany && Application.productName == probeProduct,
                "Unique PlayerPrefs namespace verified before wallet access", ref count);
            namespaceVerified = true;
            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            SetStatic(typeof(GameManager), "<Instance>k__BackingField", game);
            Set(game, "<CurrentState>k__BackingField", GameState.Playing);
            Set(game, "gameTime", 0f);
            Set(game, "isUIProcessing", false);
            Get<Queue<Action>>(game, "uiRequestQueue").Clear();
            LevelUpUIManager.Instance = null;

            database.allItems = new List<Item_SO>();
            for (int i = 0; i < 6; i++)
            {
                var item = ScriptableObject.CreateInstance<Item_SO>();
                item.itemName = "Validation item " + i;
                item.itemScript = "Validation description " + i;
                item.iconSprite = sprite;
                item.MaxUpgrade = 3;
                item.allowedEquipmentSlots = EquipmentSlotMask.Head | EquipmentSlotMask.Middle;
                database.allItems.Add(item);
                dummyItems.Add(item);
            }
            Set(owner, "itemDatabase", database);
            inventory.items.Clear();
            Call(inventory, "Awake");
            for (int i = 0; i < 10; i++)
            {
                var slot = hud.GetEquipmentSlot(i);
                Require(slot != null, "Equipment slot " + i + " resolves", ref count);
                Call(slot, "Awake");
            }
            Call(wallet, "Awake");
            Call(hud, "Start"); hudStarted = true;
            Call(owner, "Awake");
            Call(owner, "Start"); ownerStarted = true;
            wallet.AddFlesh(500);
            Require(wallet.Flesh == 500 && wallet.CreationCost == 30, "Creation wallet starts at planned cost", ref count);
            var train = hud.EquipmentRect;
            var originalParent = train.parent;
            var originalAnchorMin = train.anchorMin;
            var originalAnchorMax = train.anchorMax;
            var originalPivot = train.pivot;
            var originalSize = train.sizeDelta;
            var originalPosition = train.anchoredPosition;
            var originalScale = train.localScale;
            int originalSibling = train.GetSiblingIndex();
            var pointer = new PointerEventData(events) { position = new Vector2(400, 200), button = PointerEventData.InputButton.Left };

            game.RegisterUIQueue(() => owner.ShowCreation(wallet));
            var view = Get<TrainItemWorkbenchView>(owner, "view");
            Require(owner.IsOpen && game.CurrentState == GameState.Event && Time.timeScale == 0f,
                "Creation opens through the real UI pause queue", ref count);
            Require(train.parent == view.TrainHost && !Get<RectTransform>(view, "choicesRoot").gameObject.activeSelf,
                "Choices hidden while train begins moving", ref count);
            CompleteTransition(owner);
            Require(Get<RectTransform>(view, "choicesRoot").gameObject.activeSelf && view.ChoicesGroup.alpha == 1f &&
                view.RootGroup.interactable, "Choices revealed after train transition", ref count);
            Require(train.anchoredPosition.sqrMagnitude < 0.001f &&
                Vector3.Distance(train.localScale, originalScale * Get<float>(owner, "trainPanelScale")) < 0.001f,
                "Train reaches the elevated host with its configured larger scale", ref count);
            Require(view.Choices.Length == 3 && Get<RectTransform>(view, "choicesRoot").GetComponent<HorizontalLayoutGroup>() != null,
                "Three choices use a horizontal layout", ref count);
            for (int i = 0; i < 3; i++)
                Require(view.Choices[i].Item != null && view.Choices[i].GetComponentsInChildren<TMP_Text>(true).Length == 0,
                    "Choice " + i + " contains only an icon", ref count);

            view.Choices[0].OnPointerEnter(pointer);
            Require(Get<RectTransform>(view, "tooltipRoot").gameObject.activeSelf &&
                !string.IsNullOrEmpty(Get<TMP_Text>(view, "tooltipDescription").text),
                "Pointer enter displays item description", ref count);
            view.Choices[0].OnPointerExit(pointer);
            Require(!Get<RectTransform>(view, "tooltipRoot").gameObject.activeSelf, "Pointer exit hides description", ref count);

            int beforeFlesh = wallet.Flesh, beforeCreated = wallet.CreatedParts;
            view.Choices[0].OnBeginDrag(pointer);
            Require(hud.GetEquipmentSlot(9).GetComponent<Image>().color.g < 0.3f &&
                hud.GetEquipmentSlot(2).GetComponent<Image>().color.g > 0.3f,
                "Invalid slots red and valid slots retain their frame color", ref count);
            hud.GetEquipmentSlot(9).OnDrop(pointer);
            Require(inventory.items.Count == 0 && wallet.Flesh == beforeFlesh && wallet.CreatedParts == beforeCreated,
                "Invalid drop does not equip or charge", ref count);
            view.Choices[0].OnEndDrag(pointer);
            Require(!view.FloatingIcon.gameObject.activeSelf && hud.GetEquipmentSlot(9).GetComponent<Image>().color.g > 0.3f,
                "Unsuccessful drag hides the ghost and restores borders", ref count);

            Item_SO picked = view.Choices[0].Item;
            view.Choices[0].OnBeginDrag(pointer);
            hud.GetEquipmentSlot(2).OnDrop(pointer);
            view.Choices[0].OnEndDrag(pointer); // Installed InputSystem releases drop before end-drag.
            Require(inventory.GetItemAtSlot(2)?.itemData == picked && wallet.Flesh == beforeFlesh - 30 &&
                wallet.CreatedParts == beforeCreated + 1 && wallet.CreationCost == 40,
                "Valid drop equips exactly the selected slot and charges once", ref count);
            Require(view.FloatingIcon.gameObject.activeSelf, "End-drag does not interrupt the successful drop animation", ref count);
            CompleteTransition(owner);
            Require(Get<bool>(owner, "closing"), "Choice collapse begins the return transition", ref count);
            for (int i = 0; i < 3; i++)
                Require(view.Choices[i].transform.localScale.sqrMagnitude < 0.00001f, "Choice " + i + " shrinks after equip", ref count);
            CompleteTransition(owner);
            CheckRestored(owner, game, train, originalParent, originalAnchorMin, originalAnchorMax, originalPivot,
                originalSize, originalPosition, originalScale, originalSibling, ref count);

            Require(!wallet.TryPurchaseCreation(() => false) && wallet.Flesh == beforeFlesh - 30,
                "Failed equipment transaction does not charge", ref count);
            ItemInstance first = inventory.GetItemAtSlot(2);
            Item_SO secondData = database.allItems.Find(item => item != picked);
            Require(inventory.TryEquipAt(secondData, 4, out ItemInstance second), "Second fixture item equips", ref count);

            game.RegisterUIQueue(owner.ShowUpgradeEvent);
            CompleteTransition(owner);
            Require(!Get<Image>(view, "upgradeIcon").enabled && Get<ItemInstance>(owner, "selectedItem") == null,
                "Upgrade event opens with an empty selection slot", ref count);
            hud.GetEquipmentSlot(2).OnPointerClick(pointer);
            Require(!Get<Image>(hud.GetEquipmentSlot(2), "itemIcon").enabled && view.FloatingIcon.gameObject.activeSelf,
                "Selection hides source icon while moving to the upgrade slot", ref count);
            hud.GetEquipmentSlot(4).OnPointerClick(pointer);
            Require(Get<Image>(hud.GetEquipmentSlot(2), "itemIcon").enabled &&
                !Get<Image>(hud.GetEquipmentSlot(4), "itemIcon").enabled &&
                Get<ItemInstance>(owner, "selectedItem") == second, "Quick selection swap restores previous source", ref count);
            Get<Sequence>(owner, "transition").Goto(0.1f, false);
            Vector3 movingPosition = view.FloatingIcon.rectTransform.position;
            owner.CancelUpgradeSelection();
            Require(Vector3.Distance(movingPosition, view.FloatingIcon.rectTransform.position) < 0.001f,
                "Early cancel starts from the current moving icon position", ref count);
            CompleteTransition(owner);
            Require(Get<Image>(hud.GetEquipmentSlot(4), "itemIcon").enabled && !view.FloatingIcon.gameObject.activeSelf,
                "Cancel smoothly restores source icon", ref count);

            hud.GetEquipmentSlot(2).OnPointerClick(pointer);
            CompleteTransition(owner);
            int levelBefore = first.currentUpgrade;
            beforeFlesh = wallet.Flesh;
            owner.UpgradeSelectedItem();
            Require(first.currentUpgrade == levelBefore && wallet.Flesh == beforeFlesh &&
                !Get<Button>(view, "upgradeButton").interactable, "Deferred cost leaves upgrade action disabled", ref count);
            owner.RemoveSelectedItem();
            Require(inventory.FindItem(picked) == null && inventory.GetItemAtSlot(2) == null && Get<bool>(owner, "removalUsed"),
                "Removal releases the item and consumes the event allowance", ref count);
            hud.GetEquipmentSlot(4).OnPointerClick(pointer);
            CompleteTransition(owner);
            owner.RemoveSelectedItem();
            Require(inventory.GetItemAtSlot(4) == second && Get<Button>(view, "largeCloseButton").gameObject.activeSelf,
                "Second removal is blocked and exhausted event shows the large close button", ref count);
            string descriptionBeforeClose = Get<TMP_Text>(view, "beforeDescription").text;
            owner.CloseWorkbench();
            Require(!string.IsNullOrEmpty(descriptionBeforeClose) &&
                Get<TMP_Text>(view, "beforeDescription").text == descriptionBeforeClose &&
                Get<Image>(hud.GetEquipmentSlot(4), "itemIcon").enabled,
                "Closing preserves description for fade and restores source icon", ref count);
            CompleteTransition(owner);
            CheckRestored(owner, game, train, originalParent, originalAnchorMin, originalAnchorMax, originalPivot,
                originalSize, originalPosition, originalScale, originalSibling, ref count);

            // Cost is injected only into this in-memory Editor fixture, never an authored scene.
            Set(owner, "upgradeCostPerCell", 10);
            game.RegisterUIQueue(owner.ShowUpgradeEvent);
            CompleteTransition(owner);
            hud.GetEquipmentSlot(4).OnPointerClick(pointer);
            CompleteTransition(owner);
            beforeFlesh = wallet.Flesh;
            owner.UpgradeSelectedItem();
            owner.UpgradeSelectedItem();
            Require(second.currentUpgrade == 3 && wallet.Flesh == beforeFlesh - 20,
                "Configured cost permits repeated upgrades in one event", ref count);
            owner.UpgradeSelectedItem();
            Require(wallet.Flesh == beforeFlesh - 20, "Maximum level does not charge again", ref count);
            owner.CancelUpgradeSelection();
            owner.CloseWorkbench(); // Kill a return tween after selectedSlot was cleared.
            CompleteTransition(owner);
            Require(Get<Image>(hud.GetEquipmentSlot(4), "itemIcon").enabled,
                "Close during cancel restores all selected source icons", ref count);
            Set(owner, "upgradeCostPerCell", -1);

            bool extraQueuedExecuted = false;
            game.RegisterUIQueue(() => owner.ShowCreation(wallet));
            game.RegisterUIQueue(() => extraQueuedExecuted = true);
            Call(owner, "OnDisable");
            Require(!owner.IsOpen && !extraQueuedExecuted && !Get<bool>(game, "isUIProcessing") &&
                Get<Queue<Action>>(game, "uiRequestQueue").Count == 0,
                "Disable abort clears the current and queued modal", ref count);
            CheckRestored(owner, game, train, originalParent, originalAnchorMin, originalAnchorMax, originalPivot,
                originalSize, originalPosition, originalScale, originalSibling, ref count);

            game.RegisterUIQueue(owner.ShowUpgradeEvent);
            game.RegisterUIQueue(() => extraQueuedExecuted = true);
            game.ChangeState(GameState.Die);
            Require(!owner.IsOpen && !extraQueuedExecuted && game.CurrentState == GameState.Die &&
                Time.timeScale == 1f && Physics2D.simulationMode == SimulationMode2D.FixedUpdate &&
                !Get<bool>(game, "isUIProcessing"), "Terminal state abort restores simulation without running another modal", ref count);
        }
        finally
        {
            if (owner != null && owner.IsOpen) Call(owner, "OnDisable");
            if (ownerStarted) Call(owner, "OnDestroy");
            if (hudStarted) Call(hud, "OnDestroy");
            var generatedView = Get<TrainItemWorkbenchView>(owner, "view");
            if (generatedView != null) UnityEngine.Object.DestroyImmediate(generatedView.gameObject);
            Set(owner, "itemDatabase", originalDatabase);
            Set(owner, "upgradeCostPerCell", -1);
            inventory.items.Clear();
            inventory.items.AddRange(originalItems);
            SetStatic(typeof(GameManager), "<Instance>k__BackingField", oldGameInstance);
            LevelUpUIManager.Instance = oldOwnerInstance;
            Time.timeScale = oldTime;
            Physics2D.simulationMode = oldPhysics;
            foreach (var item in dummyItems) UnityEngine.Object.DestroyImmediate(item);
            UnityEngine.Object.DestroyImmediate(database);
            UnityEngine.Object.DestroyImmediate(sprite);
            UnityEngine.Object.DestroyImmediate(spriteTexture);
            // Clean only the new validation namespace; never read or print the user's saved values.
            if (namespaceVerified)
            {
                PlayerPrefs.DeleteKey("LoveTrain.Progression.Souls.v1");
                PlayerPrefs.Save();
            }
            PlayerSettings.companyName = oldCompany;
            PlayerSettings.productName = oldProduct;
        }
    }

    private static void CheckRestored(LevelUpUIManager owner, GameManager game, RectTransform train, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position, Vector3 scale, int sibling, ref int count)
    {
        Require(!owner.IsOpen && train.parent == parent && train.GetSiblingIndex() == sibling,
            "Return restores equipment parent and sibling", ref count);
        Require(Vector2.Distance(train.anchorMin, anchorMin) < 0.001f && Vector2.Distance(train.anchorMax, anchorMax) < 0.001f &&
            Vector2.Distance(train.pivot, pivot) < 0.001f && Vector2.Distance(train.sizeDelta, size) < 0.001f,
            "Return restores original anchors, pivot and dimensions", ref count);
        Require(Vector2.Distance(train.anchoredPosition, position) < 0.001f && Vector3.Distance(train.localScale, scale) < 0.001f,
            "Return restores original position and scale", ref count);
        Require(game.CurrentState == GameState.Playing && Time.timeScale == 1f &&
            Physics2D.simulationMode == SimulationMode2D.FixedUpdate, "Return restores gameplay and simulation", ref count);
    }

    private static void CompleteTransition(LevelUpUIManager owner)
    {
        Sequence tween = Get<Sequence>(owner, "transition");
        if (tween == null || !tween.IsActive()) throw new Exception("Expected an active workbench transition");
        tween.Complete(true);
    }

    private static T Get<T>(object owner, string field) => (T)owner.GetType().GetField(field, Members).GetValue(owner);
    private static void Set(object owner, string field, object value) => owner.GetType().GetField(field, Members).SetValue(owner, value);
    private static object GetStatic(Type type, string field) => type.GetField(field, Members).GetValue(null);
    private static void SetStatic(Type type, string field, object value) => type.GetField(field, Members).SetValue(null, value);
    private static void Call(object owner, string method) => owner.GetType().GetMethod(method, Members).Invoke(owner, null);
    private static void Require(bool value, string label, ref int count)
    {
        if (!value) throw new Exception(label);
        count++;
    }
}
