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
        public Object targetObject;
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
    }

    private void OnGUI()
    {
        if (settings == null) LoadSettings();

        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter, margin = new RectOffset(0, 0, 10, 10) };
        GUIStyle groupHeaderStyle = new GUIStyle(EditorStyles.toolbar);
        GUIStyle groupTitleStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };

        EditorGUILayout.LabelField("🎛️ 밸런스 대시보드", headerStyle);

        // --- 상단 설정 바 ---
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"레이아웃: {columnCount}열", GUILayout.Width(80));
        columnCount = (int)GUILayout.HorizontalSlider(columnCount, 1, 5);

        if (GUILayout.Button("모두 펼치기", EditorStyles.miniButtonLeft)) ToggleAllGroups(true);
        if (GUILayout.Button("모두 접기", EditorStyles.miniButtonRight)) ToggleAllGroups(false);
        EditorGUILayout.EndHorizontal();

        // ✨ [추가됨] 아이템 SO 자동 로드 버튼 영역
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📦 모든 아이템 SO 불러오기 (자동 그룹 생성)", GUILayout.Height(30)))
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

            // [1] 그룹 헤더
            EditorGUILayout.BeginHorizontal(groupHeaderStyle);

            group.isExpanded = EditorGUILayout.Foldout(group.isExpanded, GUIContent.none, true);
            group.groupName = EditorGUILayout.TextField(group.groupName, groupTitleStyle, GUILayout.Width(200));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("+ 아이템 추가", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                group.items.Add(new BalanceToolSettings.WatchItem());
                SaveSettings();
            }

            if (GUILayout.Button("삭제", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                if (EditorUtility.DisplayDialog("그룹 삭제", $"'{group.groupName}' 그룹을 삭제하시겠습니까?", "삭제", "취소"))
                {
                    settings.groups.RemoveAt(g);
                    SaveSettings();
                    return;
                }
            }
            EditorGUILayout.EndHorizontal();

            // [2] 그룹 내용
            if (group.isExpanded)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                int itemCount = group.items.Count;
                if (itemCount == 0)
                {
                    EditorGUILayout.LabelField("비어있음.", EditorStyles.centeredGreyMiniLabel);
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

        if (GUILayout.Button("+ 새 그룹 만들기", GUILayout.Height(30)))
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

        // 상단: 오브젝트 & 버튼
        EditorGUILayout.BeginHorizontal();
        Object newTarget = EditorGUILayout.ObjectField(item.targetObject, typeof(Object), true);
        if (newTarget != item.targetObject)
        {
            item.targetObject = newTarget;
            item.propertyPath = "";
            item.displayName = "";
            SaveSettings();
        }

        // ➕ 복제
        if (GUILayout.Button(new GUIContent("+", "복제"), GUILayout.Width(20)))
        {
            var newItem = new BalanceToolSettings.WatchItem();
            newItem.targetObject = item.targetObject;
            newItem.propertyPath = "";
            list.Insert(index + 1, newItem);
            SaveSettings();
            listChanged = true;
        }

        // X 삭제
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

        // 변수 선택 및 표시 로직
        if (item.targetObject != null)
        {
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

            // 변수 선택 드롭다운 (변수가 아직 선택 안됐으면 '변수 선택' 표시)
            int current = paths.IndexOf(item.propertyPath);
            int newIdx = EditorGUILayout.Popup(current, names.ToArray());

            if (newIdx >= 0 && newIdx < paths.Count)
            {
                item.propertyPath = paths[newIdx];
                item.displayName = names[newIdx];
            }

            // 실제 변수 그리기
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
            // 변수가 선택되지 않았을 때만 안내 문구 표시
            else
            {
                EditorGUILayout.LabelField("변수 선택 ▼", EditorStyles.centeredGreyMiniLabel);
            }

            if (so.ApplyModifiedProperties()) EditorUtility.SetDirty(item.targetObject);
        }
        else
        {
            EditorGUILayout.LabelField("Empty", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();
        return false;
    }

    // ✨ [추가됨] 아이템 SO 자동 로드 함수
    private void LoadAllItemSOs()
    {
        // 1. 새 그룹 생성
        var itemGroup = new BalanceToolSettings.WatchGroup();
        itemGroup.groupName = "자동 로드된 아이템들";

        // 2. 프로젝트 전체에서 Item_SO 타입 검색
        string[] guids = AssetDatabase.FindAssets("t:Item_SO");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Item_SO itemSO = AssetDatabase.LoadAssetAtPath<Item_SO>(path);

            if (itemSO != null)
            {
                // WatchItem 생성 및 오브젝트 등록 (변수는 비워둠)
                var newItem = new BalanceToolSettings.WatchItem();
                newItem.targetObject = itemSO;
                newItem.propertyPath = ""; // 변수 선택 안 함

                itemGroup.items.Add(newItem);
            }
        }

        if (itemGroup.items.Count > 0)
        {
            settings.groups.Add(itemGroup);
            SaveSettings();
            Debug.Log($"[밸런스 툴] {itemGroup.items.Count}개의 아이템 SO를 불러왔습니다.");
        }
        else
        {
            Debug.LogWarning("[밸런스 툴] Item_SO 타입의 에셋을 찾을 수 없습니다.");
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