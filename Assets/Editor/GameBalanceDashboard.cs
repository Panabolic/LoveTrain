using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

// 설정 데이터
public class BalanceToolSettings : ScriptableObject
{
    [System.Serializable]
    public class WatchItem
    {
        public Object targetObject;   // (표시용) 오브젝트
        public string globalObjectId; // ✨ (저장용) 절대 주소 ID
        public string propertyPath;
        public string displayName;
    }

    [System.Serializable]
    public class WatchGroup
    {
        public string groupName = "새 그룹";
        public bool isExpanded = true;
        public List<WatchItem> items = new List<WatchItem>();
    }

    public List<WatchGroup> groups = new List<WatchGroup>();
}

public class CustomBalanceTool : EditorWindow
{
    private static BalanceToolSettings settings;
    private Vector2 scrollPos;
    private const string SETTING_PATH = "Assets/Editor/BalanceToolSettings.asset";

    private int columnCount = 4;

    [MenuItem("Tools/Custom Balance Tool")]
    public static void ShowWindow()
    {
        GetWindow<CustomBalanceTool>("커스텀 밸런스 툴");
    }

    private void OnEnable()
    {
        LoadSettings();
        // 툴이 켜질 때 저장된 ID를 이용해 오브젝트를 복구 시도
        RestoreSceneReferences();
    }

    // ✨ [핵심] 끊어진 씬 오브젝트 연결을 복구하는 함수
    private void RestoreSceneReferences()
    {
        if (settings == null) return;

        bool changed = false;
        foreach (var group in settings.groups)
        {
            foreach (var item in group.items)
            {
                // 타겟이 없는데 ID는 저장되어 있다면? -> 복구 시도!
                if (item.targetObject == null && !string.IsNullOrEmpty(item.globalObjectId))
                {
                    if (GlobalObjectId.TryParse(item.globalObjectId, out GlobalObjectId id))
                    {
                        Object obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
                        if (obj != null)
                        {
                            item.targetObject = obj;
                            changed = true;
                        }
                    }
                }
            }
        }
        if (changed) EditorUtility.SetDirty(settings); // 변경사항 반영
    }

    // ✨ [핵심] 오브젝트가 할당될 때 ID를 저장하는 함수
    private void SaveTargetReference(BalanceToolSettings.WatchItem item, Object newTarget)
    {
        item.targetObject = newTarget;
        if (newTarget != null)
        {
            // 오브젝트의 고유 주민등록번호(GlobalObjectId)를 문자열로 저장
            item.globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(newTarget).ToString();
        }
        else
        {
            item.globalObjectId = "";
        }
        SaveSettings();
    }

    private void OnGUI()
    {
        if (settings == null) LoadSettings();

        // 스타일
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter, margin = new RectOffset(0, 0, 10, 10) };
        GUIStyle groupHeaderStyle = new GUIStyle(EditorStyles.toolbar);
        GUIStyle groupTitleStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };

        EditorGUILayout.LabelField("🎛️ 밸런스 대시보드 V5 (참조 보호)", headerStyle);

        // 상단 설정 바
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"레이아웃: {columnCount}열", GUILayout.Width(80));
        columnCount = (int)GUILayout.HorizontalSlider(columnCount, 1, 5);

        if (GUILayout.Button("모두 펼치기", EditorStyles.miniButtonLeft)) ToggleAllGroups(true);
        if (GUILayout.Button("모두 접기", EditorStyles.miniButtonRight)) ToggleAllGroups(false);
        EditorGUILayout.EndHorizontal();

        // 아이템 SO 자동 로드
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📦 모든 아이템 SO 불러오기", GUILayout.Height(30)))
        {
            LoadAllItemSOs();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // 그룹 리스트 그리기
        for (int g = 0; g < settings.groups.Count; g++)
        {
            var group = settings.groups[g];

            // 그룹 헤더
            EditorGUILayout.BeginHorizontal(groupHeaderStyle);
            group.isExpanded = EditorGUILayout.Foldout(group.isExpanded, GUIContent.none, true);
            group.groupName = EditorGUILayout.TextField(group.groupName, groupTitleStyle, GUILayout.Width(200));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("+ 아이템 추가", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                group.items.Add(new BalanceToolSettings.WatchItem());
                SaveSettings();
            }

            if (GUILayout.Button("그룹 삭제", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("그룹 삭제", $"'{group.groupName}' 삭제?", "삭제", "취소"))
                {
                    settings.groups.RemoveAt(g);
                    SaveSettings();
                    return;
                }
            }
            EditorGUILayout.EndHorizontal();

            // 그룹 내용
            if (group.isExpanded)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                int itemCount = group.items.Count;
                if (itemCount == 0)
                {
                    EditorGUILayout.LabelField("비어있음. '+ 아이템 추가'를 누르세요.", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    for (int i = 0; i < itemCount; i++)
                    {
                        if (i % columnCount == 0) EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.BeginVertical(GUILayout.Width(position.width / columnCount - 15));

                        if (DrawWatchItem(group.items, i))
                        {
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.EndScrollView();
                            return;
                        }

                        EditorGUILayout.EndVertical();

                        if (i % columnCount == columnCount - 1 || i == itemCount - 1) EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("+ 새 그룹 만들기", GUILayout.Height(35)))
        {
            settings.groups.Add(new BalanceToolSettings.WatchGroup());
            SaveSettings();
        }

        EditorGUILayout.Space(20);
        EditorGUILayout.EndScrollView();

        if (GUI.changed) EditorUtility.SetDirty(settings);
    }

    private bool DrawWatchItem(List<BalanceToolSettings.WatchItem> list, int index)
    {
        var item = list[index];
        bool listChanged = false;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // 1. 오브젝트 슬롯 & 버튼
        EditorGUILayout.BeginHorizontal();
        Object newTarget = EditorGUILayout.ObjectField(item.targetObject, typeof(Object), true);
        if (newTarget != item.targetObject)
        {
            // ✨ 오브젝트 변경 시 ID도 같이 저장
            SaveTargetReference(item, newTarget);
            item.propertyPath = "";
            item.displayName = "";
        }

        // 복제
        if (GUILayout.Button(new GUIContent("+", "복제"), GUILayout.Width(20)))
        {
            var newItem = new BalanceToolSettings.WatchItem();
            // 복제 시에도 ID와 타겟 모두 복사
            newItem.targetObject = item.targetObject;
            newItem.globalObjectId = item.globalObjectId;
            newItem.propertyPath = "";
            list.Insert(index + 1, newItem);
            SaveSettings();
            listChanged = true;
        }

        // 삭제
        if (GUILayout.Button("X", GUILayout.Width(20)))
        {
            list.RemoveAt(index);
            SaveSettings();
            listChanged = true;
        }
        EditorGUILayout.EndHorizontal();

        if (listChanged)
        {
            EditorGUILayout.EndVertical();
            return true;
        }

        // 2. 컴포넌트/변수 선택
        if (item.targetObject is GameObject go)
        {
            DrawComponentSelector(go, item);
        }
        else if (item.targetObject is Component comp)
        {
            if (comp.gameObject != null)
            {
                DrawComponentSelector(comp.gameObject, item, true);
            }
            DrawPropertySelector(item);
        }
        else if (item.targetObject != null)
        {
            DrawPropertySelector(item);
        }
        else
        {
            EditorGUILayout.LabelField("드래그 & 드롭", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();
        return false;
    }

    private void DrawComponentSelector(GameObject go, BalanceToolSettings.WatchItem item, bool isMini = false)
    {
        Component[] comps = go.GetComponents<Component>();
        string[] compNames = comps.Select(c => c.GetType().Name).ToArray();

        int currentIndex = -1;
        if (item.targetObject is Component currentComp)
        {
            for (int i = 0; i < comps.Length; i++)
            {
                if (comps[i] == currentComp) { currentIndex = i; break; }
            }
        }

        if (!isMini)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("↳ 스크립트:", GUILayout.Width(70));
            int newIndex = EditorGUILayout.Popup(currentIndex, compNames);
            EditorGUILayout.EndHorizontal();

            if (newIndex >= 0 && newIndex < comps.Length && comps[newIndex] != item.targetObject)
            {
                SaveTargetReference(item, comps[newIndex]); // ID 저장 포함
                item.propertyPath = "";
            }

            if (currentIndex == -1)
                EditorGUILayout.HelpBox("스크립트를 선택하세요.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("스크립트:", EditorStyles.miniLabel, GUILayout.Width(50));

            int newIndex = EditorGUILayout.Popup(currentIndex, compNames, EditorStyles.miniPullDown);
            EditorGUILayout.EndHorizontal();

            if (newIndex >= 0 && newIndex < comps.Length && comps[newIndex] != item.targetObject)
            {
                SaveTargetReference(item, comps[newIndex]); // ID 저장 포함
                item.propertyPath = "";
            }
        }
    }

    private void DrawPropertySelector(BalanceToolSettings.WatchItem item)
    {
        // 씬 오브젝트가 끊어졌을 경우를 대비해 널 체크
        if (item.targetObject == null)
        {
            EditorGUILayout.LabelField("오브젝트 로딩 중...", EditorStyles.centeredGreyMiniLabel);
            // 복구 시도
            if (!string.IsNullOrEmpty(item.globalObjectId)) RestoreSceneReferences();
            return;
        }

        SerializedObject so = new SerializedObject(item.targetObject);
        so.Update();

        List<string> paths = new List<string>();
        List<string> names = new List<string>();
        SerializedProperty prop = so.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            if (prop.name == "m_Script") { enterChildren = false; continue; }
            paths.Add(prop.propertyPath);
            names.Add(prop.displayName);
            enterChildren = false;
        }

        int current = paths.IndexOf(item.propertyPath);

        EditorGUILayout.BeginHorizontal();
        if (item.targetObject is Component)
            EditorGUILayout.LabelField("↳ 변수:", GUILayout.Width(50));
        else
            EditorGUILayout.LabelField("변수:", GUILayout.Width(40));

        int newIdx = EditorGUILayout.Popup(current, names.ToArray());
        EditorGUILayout.EndHorizontal();

        if (newIdx >= 0 && newIdx < paths.Count)
        {
            item.propertyPath = paths[newIdx];
            item.displayName = names[newIdx];
        }

        if (!string.IsNullOrEmpty(item.propertyPath))
        {
            SerializedProperty p = so.FindProperty(item.propertyPath);
            if (p != null)
            {
                GUI.backgroundColor = new Color(0.8f, 1f, 0.8f);
                EditorGUILayout.PropertyField(p, GUIContent.none, true);
                GUI.backgroundColor = Color.white;
            }
            else EditorGUILayout.LabelField("변수 없음", EditorStyles.miniLabel);
        }
        else if (item.targetObject is ScriptableObject)
        {
            EditorGUILayout.LabelField("변수 선택 ▼", EditorStyles.centeredGreyMiniLabel);
        }

        if (so.ApplyModifiedProperties()) EditorUtility.SetDirty(item.targetObject);
    }

    private void LoadAllItemSOs()
    {
        var itemGroup = new BalanceToolSettings.WatchGroup();
        itemGroup.groupName = "자동 로드된 아이템들";

        string[] guids = AssetDatabase.FindAssets("t:Item_SO");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Item_SO itemSO = AssetDatabase.LoadAssetAtPath<Item_SO>(path);
            if (itemSO != null)
            {
                var newItem = new BalanceToolSettings.WatchItem();
                SaveTargetReference(newItem, itemSO); // ID 저장
                newItem.propertyPath = "";
                itemGroup.items.Add(newItem);
            }
        }

        if (itemGroup.items.Count > 0)
        {
            settings.groups.Add(itemGroup);
            SaveSettings();
            Debug.Log($"[밸런스 툴] {itemGroup.items.Count}개의 아이템 SO 로드 완료.");
        }
    }

    private void ToggleAllGroups(bool expand)
    {
        foreach (var g in settings.groups) g.isExpanded = expand;
    }

    private void LoadSettings()
    {
        settings = AssetDatabase.LoadAssetAtPath<BalanceToolSettings>(SETTING_PATH);
        if (settings == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Editor")) AssetDatabase.CreateFolder("Assets", "Editor");
            settings = ScriptableObject.CreateInstance<BalanceToolSettings>();
            AssetDatabase.CreateAsset(settings, SETTING_PATH);
            AssetDatabase.SaveAssets();
        }
    }

    private void SaveSettings()
    {
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }
}