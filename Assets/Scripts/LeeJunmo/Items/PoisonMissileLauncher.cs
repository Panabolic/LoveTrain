using UnityEngine;

public class PoisonMissileLauncher : MonoBehaviour, IInstantiatedItem
{
    private PoisonMissileLauncher_SO itemData;
    // private Gun playerGun; // <-- [삭제] 총기 참조 필요 없음 (독립 스탯 사용)

    [Header("연결")]
    [Tooltip("미사일이 발사될 위치들")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("비주얼")]
    [SerializeField] private Animator animatorFront;
    [SerializeField] private Animator animatorBack;

    // --- 스탯 ---
    private float missileSpeed;
    private float verticalDistance;
    private float cooldown;
    private float gasMoveSpeed;
    private float gasDamage;
    private float gasTickRate;

    private GameObject missilePrefab;
    private GameObject gasPrefab;

    // 내부 변수
    private float cooldownTimer = 0f;
    private int currentSpawnIndex = 0;

    private void Awake()
    {
        if (animatorFront == null) animatorFront = transform.Find("FrontVisual")?.GetComponent<Animator>();
        if (animatorBack == null) animatorBack = transform.Find("BackVisual")?.GetComponent<Animator>();
    }

    public void Initialize(PoisonMissileLauncher_SO data, GameObject user)
    {
        this.itemData = data;
        // this.playerGun = ...; // <-- [삭제] 총기 정보 안 가져옴
    }

    public void UpgradeInstItem(ItemInstance instance)
    {
        int lv = Mathf.Clamp(instance.currentUpgrade - 1, 0, itemData.gasDamageByLevel.Length - 1);

        this.gasDamage = itemData.gasDamageByLevel[lv];
        this.gasTickRate = itemData.gasTickRateByLevel[lv];
        this.cooldown = itemData.cooldownByLevel[lv]; // SO의 쿨타임 그대로 사용
        this.gasMoveSpeed = itemData.gasMoveSpeed;

        this.missileSpeed = itemData.missileSpeed;
        this.verticalDistance = itemData.verticalDistance;
        this.missilePrefab = itemData.MissilePrefab;
        this.gasPrefab = itemData.GasPrefab;

        if (!gameObject.activeSelf) gameObject.SetActive(true);

        Debug.Log($"독 미사일 런처 업그레이드 완료 (Lv.{instance.currentUpgrade})");
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing || GameManager.Instance.CurrentState != GameState.Boss) return;

        if (Time.timeScale == 0) return;

        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }
        else
        {
            if (PoolManager.instance != null && PoolManager.instance.activeEnemies.Count > 0)
            {
                FireSequence();
            }
        }
    }

    private void FireSequence()
    {
        // ✨ [수정] 공속 배율 계산 삭제 -> 항상 1배속으로 동작
        float speedMult = 1f;

        if (animatorFront != null)
        {
            animatorFront.speed = speedMult; // 1.0f
            animatorFront.SetTrigger("Fire");
        }

        if (animatorBack != null)
        {
            animatorBack.speed = speedMult; // 1.0f
            animatorBack.SetTrigger("Fire");
        }

        if (animatorFront == null && animatorBack == null)
        {
            SpawnMissileFromAnim();
        }

        // ✨ [수정] 쿨타임도 SO 설정값 그대로 사용
        cooldownTimer = cooldown;
    }

    public void SpawnMissileFromAnim()
    {
        if (missilePrefab == null || spawnPoints.Length == 0) return;

        Transform currentPoint = spawnPoints[currentSpawnIndex];

        GameObject missileObj = Instantiate(missilePrefab, currentPoint.position, currentPoint.rotation);
        PoisonMissile missileScript = missileObj.GetComponent<PoisonMissile>();

        if (missileScript != null)
        {
            // 가스 데미지도 배율 없이 SO 값 그대로(gasDamage) 전달
            missileScript.Initialize(missileSpeed, verticalDistance, gasPrefab, gasDamage, gasTickRate, gasMoveSpeed);
        }

        currentSpawnIndex = (currentSpawnIndex + 1) % spawnPoints.Length;
    }
}