using UnityEngine;

public class DestroyOnAnimationEndzz : StateMachineBehaviour
{
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // normalizedTime은 애니메이션의 진행도를 0.0 ~ 1.0으로 나타냅니다.
        // 1.0 이상이면 애니메이션이 끝까지 재생되었다는 뜻입니다.
        // (단, 애니메이션 클립의 'Loop Time'이 꺼져 있어야 정확히 작동합니다)
        if (stateInfo.normalizedTime >= 1.0f)
        {
            Destroy(animator.gameObject);
        }
    }
}
