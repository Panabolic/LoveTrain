using UnityEngine;
using System;

public static class SoundEventBus
{
    // 소리를 재생하라는 '액션' (이벤트)
    // 매개변수: 사운드ID, 재생 위치(Vector3)
    public static event Action<SoundID, Vector3> OnPlaySound;

    /// <summary>
    /// 사운드 재생 요청 (2D / UI / BGM)
    /// </summary>
    public static void Publish(SoundID id)
    {
        // 위치값 없으면 (0,0,0) -> 2D 사운드로 처리
        OnPlaySound?.Invoke(id, Vector3.zero);
    }
}