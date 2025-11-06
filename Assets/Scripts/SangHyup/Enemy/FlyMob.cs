using UnityEngine;

public class FlyMob : Mob
{
    [Header("Fly Mob Specification")]
    [Tooltip("기차에게 꼬라박는 거리 조건")]
    [SerializeField] private float diveDistance = 15.0f;


    private void FixedUpdate()
    {
        if (trainController == null) return; // TrainController가 없으면 실행 중지

        // Move Direction Setting
        SetMoveDirection(targetRigid.position);

        // 기차의 'X위치 퍼센티지'를 기준으로 0~1 사이의 비율을 계산합니다.
        float speedRate = Mathf.InverseLerp(
            trainController.MinXPosition,      // 기차의 최소 X위치
            trainController.MaxXPosition,      // 기차의 최대 X위치
            trainController.transform.position.x // 기차의 현재 X좌표
        );

        float currentMultiplier;

        // 자신의 X좌표와 기차의 X좌표를 비교 (이 로직은 그대로 유지)
        if (transform.position.x > targetRigid.position.x)
        {
            // [상황] 내가 기차보다 앞에 있을 때
            // 기차가 오른쪽으로 갈수록(speedRate → 1.0), 나도 최대 배율(maxMultiplier)로 빨라집니다.
            currentMultiplier = Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, speedRate);
        }
        else
        {
            // [상황] 내가 기차보다 뒤에 있을 때
            // 기차가 오른쪽으로 갈수록(speedRate → 1.0), 나는 최소 배율(minMultiplier)로 느려집니다.
            currentMultiplier = Mathf.Lerp(maxSpeedMultiplier, minSpeedMultiplier, speedRate);
        }

        // 죽으면 왼쪽으로 확 날아가게
        if (!isAlive)
        {
            moveDirection = new Vector2(-1, rigid2D.linearVelocityY);
            float deathMoveSpeed = 20.0f * currentMultiplier;

            rigid2D.linearVelocity = new Vector2(deathMoveSpeed * moveDirection.x, rigid2D.linearVelocity.y);

            return;
        }

        // 최종 속력 = 적의 기본 속력 * 현재 계산된 배율
        float finalMoveSpeed = moveSpeed * currentMultiplier;

        // Move
        if (finalMoveSpeed > 0 && isAlive == true)
        {
            rigid2D.linearVelocity = finalMoveSpeed * moveDirection;
        }
    }

    protected override void SetMoveDirection(Vector2 targetPos)
    {
        float deltaX = targetPos.x - transform.position.x;

        // 1) 수평 15 초과 → 좌/우로만 이동
        if (Mathf.Abs(deltaX) > diveDistance)
        {
            moveDirection = (deltaX > 0f) ? Vector2.right : Vector2.left;

            sprite.flipX = (moveDirection.x > 0f);

            return;
        }
        else
        {
            moveDirection = (targetPos - (Vector2)transform.position).normalized;
        }

        // Set sprite to move direction
        sprite.flipX = (moveDirection.x > 0f);

        return;
    }
}
