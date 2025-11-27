using UnityEngine;

public class WeaponVisual : MonoBehaviour
{
    [Header("필수 설정")]
    [Tooltip("이 무기의 총알이 나갈 위치 (프리팹 내부의 Muzzle 오브젝트 연결)")]
    public Transform muzzlePoint;

    [Header("선택 설정")]
    [Tooltip("이 무기를 꼈을 때 바뀔 거치대(TrainHead) 이미지 (없으면 안 바뀜)")]
    public Sprite customHolderSprite;

    [Tooltip("발사 애니메이션이 있다면 여기에 연결 (선택)")]
    public Animator weaponAnimator;
}