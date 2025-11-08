using System.Collections.Generic;
using UnityEngine;
using TMPro; // TextMeshPro 네임스페이스 추가

#if UNITY_EDITOR
using UnityEditor;
#endif

/*
https://bonnate.tistory.com/

Insert the script into the game object
insert the TMP font in the inspector
and press the button to find and replace all components.

It may work abnormally, so make sure to back up your scene before using it!!
*/

public class FontChanger : MonoBehaviour
{
    [SerializeField] public TMP_FontAsset FontAsset;
}

#if UNITY_EDITOR
[CustomEditor(typeof(FontChanger))]
public class TMP_FontChangerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Change Font!"))
        {
            TMP_FontAsset fontAsset = ((FontChanger)target).FontAsset;

            // --- [수정] ---
            // 'FindObjectsOfType(true)' 대신 'FindObjectsByType'을 사용합니다.
            // 1. FindObjectsInactive.Include: 'true'와 동일 (비활성화된 오브젝트 포함)
            // 2. FindObjectsSortMode.None: 경고에서 언급한 대로, 정렬이 필요 없으므로
            //                              'None'을 사용해 속도를 향상시킵니다.
            foreach (TextMeshPro textMeshPro3D in Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                textMeshPro3D.font = fontAsset;
            }

            // TextMeshProUGUI에도 동일하게 적용
            foreach (TextMeshProUGUI textMeshProUi in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                textMeshProUi.font = fontAsset;
            }

            Debug.Log("Scene-wide TMP font change complete!");
        }
    }
}
#endif