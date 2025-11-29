using UnityEngine;

public class LonginusPathData : MonoBehaviour
{
    [System.Serializable]
    public struct PathPoints
    {
        public Transform startPoint;
    }

    [Header("경로 목록")]
    [Tooltip("순서대로 실행될 경로들을 등록하세요.")]
    public PathPoints[] paths;
}