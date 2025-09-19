using UnityEngine;

public class BackGround : MonoBehaviour
{
    // ��� ��ҵ��� ��� �迭
    public ScrollingLayer[] scrollingLayers;

    [System.Serializable]
    public class ScrollingLayer
    {
        public Transform background;  // ��� ������Ʈ
        public float scrollSpeed;     // ��� ��ũ�� �ӵ�
    }

    private float[] backgroundWidths;  // �� ����� �ʺ�

    void Start()
    {
        // �� ��� ������Ʈ�� �ʺ� ���
        backgroundWidths = new float[scrollingLayers.Length];
        for (int i = 0; i < scrollingLayers.Length; i++)
        {
            backgroundWidths[i] = scrollingLayers[i].background.GetComponent<SpriteRenderer>().bounds.size.x;
        }
    }

    void Update()
    {
        // �� ��� ��Ҹ��� ��ũ�� �̵�
        for (int i = 0; i < scrollingLayers.Length; i++)
        {
            var layer = scrollingLayers[i];
            layer.background.Translate(Vector2.left * layer.scrollSpeed * Time.deltaTime);

            // ����� ȭ���� ����� ����ġ�� �ǵ�����
            if (layer.background.position.x <= -backgroundWidths[i])
            {
                layer.background.position = new Vector2(layer.background.position.x + backgroundWidths[i] * scrollingLayers.Length, layer.background.position.y);
            }
        }
    }
}
