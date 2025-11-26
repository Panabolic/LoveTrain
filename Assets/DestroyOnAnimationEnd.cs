// DestroyOnAnimationEnd.cs
using UnityEngine;

// 이 스크립트는 MonoBehaviour가 아니라 StateMachineBehaviour를 상속받습니다.
public class DestroyOnAnimationEnd : StateMachineBehaviour
{
    // 이 상태(State)가 업데이트될 때마다 매 프레임 호출됩니다.
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // normalizedTime은 애니메이션의 진행도를 0.0 ~ 1.0으로 나타냅니다.
        // 1.0 이상이면 애니메이션이 끝까지 재생되었다는 뜻입니다.
        // (단, 애니메이션 클립의 'Loop Time'이 꺼져 있어야 정확히 작동합니다)
        if (stateInfo.normalizedTime >= 1.0f)
        {
            // 이 애니메이터가 붙어있는 게임 오브젝트를 파괴합니다.
            animator.gameObject.GetComponent<RevolverBullet>().Despawn();
        }
    }
}