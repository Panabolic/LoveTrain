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
    IncreaseEnemyHealthBuff,

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

    [TextArea] public string resultDescription = null;
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
    // ✨ [추가] 불러올 타겟 이벤트
    private SO_Event sourceEventAsset;

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
        GUILayout.Label("🚂 이벤트 생성 & 수정기", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // ----------------------------------------------------------------
        // [A] 로드 영역 (수정 기능)
        // ----------------------------------------------------------------
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("📂 이벤트 불러오기 (수정 모드)", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        sourceEventAsset = (SO_Event)EditorGUILayout.ObjectField("수정할 SO_Event", sourceEventAsset, typeof(SO_Event), false);

        if (GUILayout.Button("데이터 불러오기", GUILayout.Width(120)))
        {
            if (sourceEventAsset != null)
            {
                LoadEventData(sourceEventAsset);
            }
            else
            {
                EditorUtility.DisplayDialog("알림", "SO_Event 파일을 슬롯에 넣어주세요.", "확인");
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // ----------------------------------------------------------------
        // [B] 편집 영역
        // ----------------------------------------------------------------
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

        // 버튼 색상 변경 (저장/수정)
        GUI.backgroundColor = sourceEventAsset != null ? new Color(0.7f, 1f, 0.7f) : Color.green;
        string btnText = sourceEventAsset != null ? "💾 수정사항 저장 (덮어쓰기)" : "✨ 새 이벤트 생성";

        if (GUILayout.Button(btnText, GUILayout.Height(40)))
        {
            CreateOrUpdateAssets();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();
    }

    // ----------------------------------------------------------------
    // 로드 로직 (Load Logic)
    // ----------------------------------------------------------------
    private void LoadEventData(SO_Event source)
    {
        // 1. 기본 정보 로드
        eventTitle = source.EventTitle;
        eventText = source.EventText;

        // 2. 선택지 초기화
        selections.Clear();

        // 3. 선택지 순회하며 데이터 복원
        if (source.Selections != null)
        {
            foreach (var sel in source.Selections)
            {
                TempSelectionData tempSel = new TempSelectionData();
                tempSel.selectionText = sel.selectionText;
                tempSel.selectionUnderText = sel.selectionUnderText;

                // 연결된 GameEventSO 로직 데이터 가져오기
                GameEventSO logicSO = sel.eventToTrigger;
                if (logicSO != null)
                {
                    foreach (var group in logicSO.rollGroups)
                    {
                        TempRollGroupData tempGroup = new TempRollGroupData();
                        tempGroup.description = group.description;

                        foreach (var outcome in group.outcomes)
                        {
                            TempOutcomeData tempOut = new TempOutcomeData();
                            tempOut.weight = outcome.weight;

                            // EffectSO -> EditorEffectType 역추적
                            if (outcome.effectLogic != null)
                            {
                                tempOut.effectType = GetEffectTypeFromSO(outcome.effectLogic);
                            }
                            else
                            {
                                tempOut.effectType = EditorEffectType.None;
                            }

                            // 파라미터 복원
                            if (outcome.parameters != null)
                            {
                                tempOut.param_Int1 = outcome.parameters.intValue;
                                tempOut.param_Int2 = outcome.parameters.intValue2;
                                tempOut.param_Float1 = outcome.parameters.floatValue;
                                tempOut.param_Float2 = outcome.parameters.floatValue2;
                                tempOut.param_Bool = outcome.parameters.boolValue;
                                tempOut.param_Item = outcome.parameters.soReference as Item_SO;
                                tempOut.param_Prefab = outcome.parameters.prefabReference;
                            }

                            // 텍스트 설정 복원
                            if (outcome.outputSettings != null)
                            {
                                tempOut.resultDescription = outcome.outputSettings.specialText;
                                tempOut.includeDefaultText = outcome.outputSettings.includeDefaultText;
                                tempOut.outputOrder = outcome.outputSettings.order;
                            }

                            tempGroup.outcomes.Add(tempOut);
                        }
                        tempSel.rollGroups.Add(tempGroup);
                    }
                }
                selections.Add(tempSel);
            }
        }

        Debug.Log($"[EventMaker] '{eventTitle}' 로드 완료!");
    }

    private EditorEffectType GetEffectTypeFromSO(GameEffectSO effectSO)
    {
        string name = effectSO.name;
        if (name.Contains("AcquireSpecificItem")) return EditorEffectType.AcquireSpecificItem;
        if (name.Contains("AcquireRandomItem")) return EditorEffectType.AcquireRandomItem;
        if (name.Contains("AcquireItem")) return EditorEffectType.AcquireItem;
        if (name.Contains("UpgradeRandomItemToMax")) return EditorEffectType.UpgradeRandomItemToMax;
        if (name.Contains("UpgradeRandomItemNTimes")) return EditorEffectType.UpgradeRandomItemNTimes;
        if (name.Contains("ModifySpeed")) return EditorEffectType.ModifySpeed;
        if (name.Contains("IncreaseEnemyHealthBuff")) return EditorEffectType.IncreaseEnemyHealthBuff;
        if (name.Contains("SpawnMobBatch")) return EditorEffectType.SpawnMobBatch;
        if (name.Contains("SpawnMobPeriodically")) return EditorEffectType.SpawnMobPeriodically;

        return EditorEffectType.None;
    }

    // ----------------------------------------------------------------
    // 저장/생성 로직 (Update Logic)
    // ----------------------------------------------------------------
    private void CreateOrUpdateAssets()
    {
        // 1. 경로 설정 (제목 기반)
        string folderPath = "Assets/Datas/Events/Generated/" + eventTitle;
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        // 2. 메인 이벤트 SO 생성 또는 로드
        string mainEventPath = $"{folderPath}/{eventTitle}.asset";
        SO_Event mainEvent = AssetDatabase.LoadAssetAtPath<SO_Event>(mainEventPath);

        if (mainEvent == null)
        {
            mainEvent = CreateInstance<SO_Event>();
            AssetDatabase.CreateAsset(mainEvent, mainEventPath);
        }

        // 데이터 갱신
        mainEvent.EventTitle = eventTitle;
        mainEvent.EventText = eventText;

        // 기존 선택지 리스트 초기화 (새로 채워넣음)
        mainEvent.Selections = new List<SO_Event.Selection>();

        int selIndex = 1;
        foreach (var selData in selections)
        {
            // 3. GameEventSO (로직) 생성 또는 로드
            string logicName = $"{eventTitle}_Sel{selIndex}_Logic";
            string logicPath = $"{folderPath}/{logicName}.asset";

            GameEventSO gameEvent = AssetDatabase.LoadAssetAtPath<GameEventSO>(logicPath);
            if (gameEvent == null)
            {
                gameEvent = CreateInstance<GameEventSO>();
                AssetDatabase.CreateAsset(gameEvent, logicPath);
            }

            // 로직 데이터 갱신
            gameEvent.name = logicName;
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

            // 변경사항 저장 (Dirty)
            EditorUtility.SetDirty(gameEvent);

            // 4. 메인 이벤트에 연결
            SO_Event.Selection newSelection = new SO_Event.Selection();
            newSelection.selectionText = selData.selectionText;
            newSelection.selectionUnderText = selData.selectionUnderText;
            newSelection.eventToTrigger = gameEvent; // 연결

            mainEvent.Selections.Add(newSelection);
            selIndex++;
        }

        // 저장 및 리프레시
        EditorUtility.SetDirty(mainEvent);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 선택 해제 후 재선택 (인스펙터 갱신용)
        Selection.activeObject = null;
        EditorApplication.delayCall += () => Selection.activeObject = mainEvent;

        Debug.Log($"🎉 이벤트 '{eventTitle}' 저장/수정 완료!");
    }

    // ----------------------------------------------------------------
    // 기타 유틸리티
    // ----------------------------------------------------------------
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
            case EditorEffectType.IncreaseEnemyHealthBuff:
                outcome.param_Int1 = EditorGUILayout.IntField("체력 증가량(%)", outcome.param_Int1);
                break;
            case EditorEffectType.SpawnMobBatch:
                EditorGUILayout.LabelField("설정 (프리팹 사용):", EditorStyles.boldLabel);
                outcome.param_Prefab = (GameObject)EditorGUILayout.ObjectField("몬스터 프리팹", outcome.param_Prefab, typeof(GameObject), false);
                EditorGUILayout.BeginHorizontal();
                outcome.param_Int1 = EditorGUILayout.IntField("수량(Count)", outcome.param_Int1);
                outcome.param_Float1 = EditorGUILayout.FloatField("딜레이(초)", outcome.param_Float1);
                EditorGUILayout.EndHorizontal();
                outcome.param_Bool = EditorGUILayout.Toggle("공중(Fly)?", outcome.param_Bool);
                break;
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