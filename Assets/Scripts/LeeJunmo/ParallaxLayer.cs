using UnityEngine;

[System.Serializable]
public class ParallaxLayer
{
    [Tooltip("��� �׷��� �θ� ������Ʈ�Դϴ�. �� ������Ʈ �ȿ� ������ ��� 2���� ����־�� �մϴ�.")]
    public Transform layerTransform;

    [Tooltip("�з����� ȿ���� �����Դϴ�. 0�� �������� �ָ� �ִ� ��ó�� õõ�� �����Դϴ�.")]
    [Range(0f, 10f)]
    public float parallaxFactor;
}