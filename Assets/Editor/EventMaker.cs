#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

// ---------------------------------------------------------
// [1] 이펙트 타입 정의
// ---------------------------------------------------------
public enum EditorEffectType
{
    None,

    [InspectorName("아이템 획득 (스마트)")]
    AcquireItem,

    [InspectorName("특정 아이템 획득")]
    AcquireSpecificItem,

    [InspectorName("랜덤 아이템 획득")]
    AcquireRandomItem,

    [InspectorName("랜덤 아이템 MAX 강화")]
    UpgradeRandomItemToMax,

    [InspectorName("랜덤 아이템 N회 강화")]
    UpgradeRandomItemNTimes,

    [InspectorName("속도 변경")]
    ModifySpeed,

    [InspectorName("적 체력 강화 (영구)")]
    IncreaseEnemyHealthBuff, // ✨ 추가됨

    [InspectorName("몬스터 무리 소환 (1회)")]
    SpawnMobBatch,

    [InspectorName("주기적 몬스터 추가 (영구)")]
    SpawnMobPeriodically,
}

// ---------------------------------------------------------
// [2] 에디터 임시 데이터
// ---------------------------------------------------------
[System.Serializable]
public class TempOutcomeData
{
    public float weight = 10f;
    public EditorEffectType effectType;

    // 파라미터
    public int param_Int1;
    public int param_Int2;
    public float param_Float1;
    public float param_Float2;
    public bool param_Bool;

    public Item_SO param_Item;
    public GameObject param_Prefab;

    [TextArea] public string resultDescription = "결과 텍스트...";
    public bool includeDefaultText = true;
    public EventResultOutput.OutputOrder outputOrder = EventResultOutput.OutputOrder.DefaultFirst;
}

[System.Serializable]
public class TempRollGroupData
{
    public string description = "그룹 설명";
    public List<TempOutcomeData> outcomes = new List<TempOutcomeData>();
}

[System.Serializable]
public class TempSelectionData
{
    public string selectionText = "선택지 내용";
    public string selectionUnderText = "선택지 하단 설명";
    public List<TempRollGroupData> rollGroups = new List<TempRollGroupData>();
}

// ---------------------------------------------------------
// [3] 메인 에디터 윈도우
// ---------------------------------------------------------
public class EventMakerWindow : EditorWindow
{
    private string eventTitle = "New Event";
    private string eventText = "이벤트 본문 텍스트";

    private List<TempSelectionData> selections = new List<TempSelectionData>();
    private Vector2 scrollPos;

    [MenuItem("Tools/LoveTrain/Event Maker")]
    public static void ShowWindow()
    {
        GetWindow<EventMakerWindow>("Event Maker");
    }

    private void OnGUI()
    {
        GUILayout.Label("🚂 이벤트 생성기 (통합 버전)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("1. 이벤트 기본 정보", EditorStyles.helpBox);
        eventTitle = EditorGUILayout.TextField("이벤트 제목 (파일명)", eventTitle);
        GUILayout.Label("이벤트 본문:");
        eventText = EditorGUILayout.TextArea(eventText, GUILayout.Height(60));

        EditorGUILayout.Space(10);

        GUILayout.Label($"2. 선택지 설정 (현재: {selections.Count}개)", EditorStyles.helpBox);
        for (int i = 0; i < selections.Count; i++)
        {
            DrawSelectionUI(selections[i], i);
        }

        if (GUILayout.Button("+ 선택지 추가", GUILayout.Height(30)))
        {
            AddDefaultSelection();
        }

        EditorGUILayout.Space(20);
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("✨ 에셋 생성 및 저장 ✨", GUILayout.Height(40)))
        {
            CreateAllAssets();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();
    }

    private void AddDefaultSelection()
    {
        var newSel = new TempSelectionData();
        var newGroup = new TempRollGroupData();
        newGroup.outcomes.Add(new TempOutcomeData());
        newSel.rollGroups.Add(newGroup);
        selections.Add(newSel);
    }

    private void DrawSelectionUI(TempSelectionData selection, int index)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"Selection #{index + 1}", EditorStyles.boldLabel);
        if (GUILayout.Button("삭제", GUILayout.Width(50))) { selections.RemoveAt(index); return; }
        EditorGUILayout.EndHorizontal();

        selection.selectionText = EditorGUILayout.TextField("버튼 텍스트", selection.selectionText);
        selection.selectionUnderText = EditorGUILayout.TextField("하단 설명", selection.selectionUnderText);
        EditorGUILayout.Space();

        for (int g = 0; g < selection.rollGroups.Count; g++)
        {
            DrawRollGroupUI(selection.rollGroups[g], g, selection.rollGroups);
        }
        if (GUILayout.Button("+ 그룹 추가")) selection.rollGroups.Add(new TempRollGroupData());
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

    private void DrawRollGroupUI(TempRollGroupData group, int index, List<TempRollGroupData> list)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"Group #{index + 1}", EditorStyles.boldLabel);
        if (GUILayout.Button("x", GUILayout.Width(20))) { list.RemoveAt(index); return; }
        EditorGUILayout.EndHorizontal();

        group.description = EditorGUILayout.TextField("그룹 설명", group.description);
        for (int k = 0; k < group.outcomes.Count; k++)
        {
            DrawOutcomeUI(group.outcomes[k], k, group.outcomes);
        }
        if (GUILayout.Button("+ 결과(Outcome) 추가")) group.outcomes.Add(new TempOutcomeData());
        EditorGUILayout.EndVertical();
    }

    private void DrawOutcomeUI(TempOutcomeData outcome, int index, List<TempOutcomeData> list)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"Outcome #{index + 1}", EditorStyles.miniBoldLabel);
        if (GUILayout.Button("x", GUILayout.Width(20))) { list.RemoveAt(index); return; }
        EditorGUILayout.EndHorizontal();

        outcome.weight = EditorGUILayout.FloatField("가중치 (Weight)", outcome.weight);

        EditorGUILayout.BeginVertical("box");
        outcome.effectType = (EditorEffectType)EditorGUILayout.EnumPopup("효과 타입", outcome.effectType);

        switch (outcome.effectType)
        {
            case EditorEffectType.AcquireItem:
                outcome.param_Item = (Item_SO)EditorGUILayout.ObjectField("획득 아이템", outcome.param_Item, typeof(Item_SO), false);
                outcome.param_Int1 = EditorGUILayout.IntField("개수", outcome.param_Int1);
                break;
            case EditorEffectType.AcquireSpecificItem:
                outcome.param_Item = (Item_SO)EditorGUILayout.ObjectField("획득 아이템 (필수)", outcome.param_Item, typeof(Item_SO), false);
                outcome.param_Int1 = EditorGUILayout.IntField("개수", outcome.param_Int1);
                break;
            case EditorEffectType.AcquireRandomItem:
            case EditorEffectType.UpgradeRandomItemToMax:
                outcome.param_Int1 = EditorGUILayout.IntField("대상 개수", outcome.param_Int1);
                break;
            case EditorEffectType.UpgradeRandomItemNTimes:
                EditorGUILayout.BeginHorizontal();
                outcome.param_Int1 = EditorGUILayout.IntField("대상 수", outcome.param_Int1);
                outcome.param_Int2 = EditorGUILayout.IntField("강화 레벨", outcome.param_Int2);
                EditorGUILayout.EndHorizontal();
                break;
            case EditorEffectType.ModifySpeed:
                outcome.param_Float1 = EditorGUILayout.FloatField("속도 변화량", outcome.param_Float1);
                break;

            // ✨ [추가됨] 체력 강화
            case EditorEffectType.IncreaseEnemyHealthBuff:
                outcome.param_Int1 = EditorGUILayout.IntField("체력 증가량(%)", outcome.param_Int1);
                break;

            // [배치 스폰]
            case EditorEffectType.SpawnMobBatch:
                EditorGUILayout.LabelField("설정 (프리팹 사용):", EditorStyles.boldLabel);
                outcome.param_Prefab = (GameObject)EditorGUILayout.ObjectField("몬스터 프리팹", outcome.param_Prefab, typeof(GameObject), false);
                EditorGUILayout.BeginHorizontal();
                outcome.param_Int1 = EditorGUILayout.IntField("수량(Count)", outcome.param_Int1);
                outcome.param_Float1 = EditorGUILayout.FloatField("딜레이(초)", outcome.param_Float1);
                EditorGUILayout.EndHorizontal();
                outcome.param_Bool = EditorGUILayout.Toggle("공중(Fly)?", outcome.param_Bool);
                break;

            // [주기적 스폰]
            case EditorEffectType.SpawnMobPeriodically:
                EditorGUILayout.LabelField("설정 (프리팹 영구 스폰):", EditorStyles.boldLabel);
                outcome.param_Prefab = (GameObject)EditorGUILayout.ObjectField("몬스터 프리팹", outcome.param_Prefab, typeof(GameObject), false);
                outcome.param_Float1 = EditorGUILayout.FloatField("생성 주기(초)", outcome.param_Float1);
                outcome.param_Bool = EditorGUILayout.Toggle("공중(Fly)?", outcome.param_Bool);
                break;
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.LabelField("결과 텍스트:");
        outcome.resultDescription = EditorGUILayout.TextArea(outcome.resultDescription, GUILayout.Height(40));

        EditorGUILayout.BeginHorizontal();
        outcome.includeDefaultText = EditorGUILayout.ToggleLeft("기본 로그 포함", outcome.includeDefaultText, GUILayout.Width(120));
        GUILayout.Label("순서:", GUILayout.Width(40));
        outcome.outputOrder = (EventResultOutput.OutputOrder)EditorGUILayout.EnumPopup(outcome.outputOrder);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void CreateAllAssets()
    {
        string folderPath = "Assets/Datas/Events/Generated/" + eventTitle;
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        SO_Event mainEvent = CreateInstance<SO_Event>();
        mainEvent.EventTitle = eventTitle;
        mainEvent.EventText = eventText;
        mainEvent.Selections = new List<SO_Event.Selection>();

        int selIndex = 1;
        foreach (var selData in selections)
        {
            GameEventSO gameEvent = CreateInstance<GameEventSO>();
            gameEvent.name = $"{eventTitle}_Sel{selIndex}_Logic";
            gameEvent.description = $"[Selection {selIndex}] {selData.selectionText}";
            gameEvent.rollGroups = new List<EventRollGroup>();

            foreach (var groupData in selData.rollGroups)
            {
                EventRollGroup newRollGroup = new EventRollGroup();
                newRollGroup.description = groupData.description;
                newRollGroup.outcomes = new List<WeightedEventOutcome>();

                foreach (var outData in groupData.outcomes)
                {
                    WeightedEventOutcome outcome = new WeightedEventOutcome();
                    outcome.weight = outData.weight;

                    if (outData.effectType != EditorEffectType.None)
                        outcome.effectLogic = FindEffectSO(outData.effectType);

                    outcome.parameters = new EffectParameters();
                    outcome.parameters.intValue = outData.param_Int1;
                    outcome.parameters.intValue2 = outData.param_Int2;
                    outcome.parameters.floatValue = outData.param_Float1;
                    outcome.parameters.floatValue2 = outData.param_Float2;
                    outcome.parameters.boolValue = outData.param_Bool;
                    outcome.parameters.soReference = outData.param_Item;
                    outcome.parameters.prefabReference = outData.param_Prefab;

                    outcome.outputSettings = new EventResultOutput();
                    outcome.outputSettings.specialText = outData.resultDescription;
                    outcome.outputSettings.order = outData.outputOrder;
                    outcome.outputSettings.includeDefaultText = outData.includeDefaultText;

                    newRollGroup.outcomes.Add(outcome);
                }
                gameEvent.rollGroups.Add(newRollGroup);
            }

            string gameEventPath = $"{folderPath}/{eventTitle}_Sel{selIndex}.asset";
            AssetDatabase.CreateAsset(gameEvent, gameEventPath);

            SO_Event.Selection newSelection = new SO_Event.Selection();
            newSelection.selectionText = selData.selectionText;
            newSelection.selectionUnderText = selData.selectionUnderText;
            newSelection.eventToTrigger = gameEvent;

            mainEvent.Selections.Add(newSelection);
            selIndex++;
        }

        string mainEventPath = $"{folderPath}/{eventTitle}.asset";
        AssetDatabase.CreateAsset(mainEvent, mainEventPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = mainEvent;
        Debug.Log($"🎉 이벤트 생성 성공: {mainEventPath}");
    }

    private GameEffectSO FindEffectSO(EditorEffectType type)
    {
        string targetFileName = "";
        switch (type)
        {
            case EditorEffectType.AcquireItem: targetFileName = "Effect_AcquireItem"; break;
            case EditorEffectType.AcquireSpecificItem: targetFileName = "Effect_AcquireSpecificItem"; break;
            case EditorEffectType.AcquireRandomItem: targetFileName = "Effect_AcquireRandomItem"; break;
            case EditorEffectType.UpgradeRandomItemToMax: targetFileName = "Effect_UpgradeRandomItemToMax"; break;
            case EditorEffectType.UpgradeRandomItemNTimes: targetFileName = "Effect_UpgradeRandomItemNTimes"; break;
            case EditorEffectType.ModifySpeed: targetFileName = "Effect_ModifySpeed"; break;
            // ✨ 파일명 매핑 추가
            case EditorEffectType.IncreaseEnemyHealthBuff: targetFileName = "Effect_IncreaseEnemyHealthBuff"; break;
            case EditorEffectType.SpawnMobBatch: targetFileName = "Effect_SpawnMobBatch"; break;
            case EditorEffectType.SpawnMobPeriodically: targetFileName = "Effect_SpawnMobPeriodically"; break;
        }

        if (string.IsNullOrEmpty(targetFileName)) return null;

        string[] guids = AssetDatabase.FindAssets($"{targetFileName} t:GameEffectSO");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<GameEffectSO>(path);
        }
        return null;
    }
}
#endif