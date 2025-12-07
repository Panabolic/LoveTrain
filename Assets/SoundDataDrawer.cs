using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SoundData))]
public class SoundDataDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 1. SoundData 내부의 'id' 변수 찾기
        SerializedProperty idProperty = property.FindPropertyRelative("id");

        // 2. 라벨 이름 결정
        // idProperty가 잘 찾아졌다면, 현재 선택된 Enum의 이름을 가져옴
        string newLabel = label.text;
        if (idProperty != null)
        {
            // 배열 인덱스 범위 체크 (혹시 모를 에러 방지)
            if (idProperty.enumValueIndex >= 0 && idProperty.enumValueIndex < idProperty.enumDisplayNames.Length)
            {
                newLabel = idProperty.enumDisplayNames[idProperty.enumValueIndex];
            }
        }

        // 3. 속성 그리기 (기존 라벨 대신 새로운 이름으로 교체)
        EditorGUI.PropertyField(position, property, new GUIContent(newLabel), true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // 기본 높이 계산 (접혔을 때/펼쳤을 때 높이 자동 계산)
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
#endif