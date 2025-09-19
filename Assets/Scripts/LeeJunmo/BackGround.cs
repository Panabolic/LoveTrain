using UnityEngine;

public class BackGround : MonoBehaviour
{
    // 배경 요소들을 담는 배열
    public ScrollingLayer[] scrollingLayers;

    [System.Serializable]
    public class ScrollingLayer
    {
        public Transform background;  // 배경 오브젝트
        public float scrollSpeed;     // 배경 스크롤 속도
    }

    private float[] backgroundWidths;  // 각 배경의 너비

    void Start()
    {
        // 각 배경 오브젝트의 너비 계산
        backgroundWidths = new float[scrollingLayers.Length];
        for (int i = 0; i < scrollingLayers.Length; i++)
        {
            backgroundWidths[i] = scrollingLayers[i].background.GetComponent<SpriteRenderer>().bounds.size.x;
        }
    }

    void Update()
    {
        // 각 배경 요소마다 스크롤 이동
        for (int i = 0; i < scrollingLayers.Length; i++)
        {
            var layer = scrollingLayers[i];
            layer.background.Translate(Vector2.left * layer.scrollSpeed * Time.deltaTime);

            // 배경이 화면을 벗어나면 원위치로 되돌리기
            if (layer.background.position.x <= -backgroundWidths[i])
            {
                layer.background.position = new Vector2(layer.background.position.x + backgroundWidths[i] * scrollingLayers.Length, layer.background.position.y);
            }
        }
    }
}
