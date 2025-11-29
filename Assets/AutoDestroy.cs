using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    [Tooltip("파괴될 때까지 걸리는 시간 (애니메이션 길이와 맞추세요)")]
    public float lifetime = 1.0f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}