using UnityEngine;

[System.Serializable]
public class ParallaxLayer
{
    [Tooltip("배경 그룹의 부모 오브젝트입니다. 이 오브젝트 안에 동일한 배경 2개가 들어있어야 합니다.")]
    public Transform layerTransform;

    [Tooltip("패럴랙스 효과의 강도입니다. 0에 가까울수록 멀리 있는 것처럼 천천히 움직입니다.")]
    [Range(0f, 10f)]
    public float parallaxFactor;
}