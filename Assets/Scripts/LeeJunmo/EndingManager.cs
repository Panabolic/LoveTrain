using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening; // ✨ DOTween 필수
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EndingManager : MonoBehaviour
{
    public static EndingManager Instance;

    [Header("연출 대상")]
    [SerializeField] private Transform trainTransform;
    [SerializeField] private Camera mainCamera;

    [Header("엔딩 위치 설정")]
    [SerializeField] private Vector3 trainEndingPos;
    [SerializeField] private Vector3 cameraEndingPos;
    [SerializeField] private float cameraEndingSize = 10f;

    [Header("결과창 UI")]
    [SerializeField] private RectTransform endingStatsPanel; // ✨ RectTransform으로 변경
    [SerializeField] private TextMeshProUGUI normalKillText;
    [SerializeField] private TextMeshProUGUI eliteKillText;
    [SerializeField] private TextMeshProUGUI bossKillText;
    [SerializeField] private TextMeshProUGUI totalKillText;

    [SerializeField] private UIAlphaFader fadePanel;

    [Header("패널 이동 설정")]
    [SerializeField] private Vector2 offScreenPosition = new Vector2(-2000f, 0f); // 화면 왼쪽 밖 좌표
    [SerializeField] private float panelMoveDuration = 0.8f;

    [Header("크레딧 설정")]
    [SerializeField] private GameObject creditPrefab;
    [SerializeField] private Transform[] creditSpawnPoints;
    [SerializeField] private float creditSpawnInterval = 1.5f;
    [SerializeField] private List<string> developerNames;

    private bool isCreditsPlaying = false;
    private bool isEndingFinished = false;
    private bool isSpawningFinished = false;

    private List<GameObject> activeCredits = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Update()
    {
        if (isEndingFinished) return;

        if (isCreditsPlaying)
        {
            // 스킵 (스페이스바) -> 패널 날리기 연출 후 종료
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Debug.Log("⏩ 엔딩 스킵 (패널 이동)");
                SkipEndingSequence();
            }

            // 자동 종료 (스폰 끝 + 남은 크레딧 없음) -> 그냥 종료
            if (isSpawningFinished)
            {
                activeCredits.RemoveAll(item => item == null);

                if (activeCredits.Count == 0)
                {
                    Debug.Log("🎬 모든 크레딧 종료");
                    FinishEndingSequence();
                }
            }
        }
    }

    public void StartEnding()
    {
        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(true);

        seq.AppendCallback(() => GameManager.Instance.ChangeState(GameState.Ending));

        if (fadePanel != null) seq.Append(fadePanel.FadeIn(1.0f)); // 암전 (Alpha 1)
        else seq.AppendInterval(1.0f);

        seq.AppendCallback(() =>
        {
            ClearAllEntities();

            if (trainTransform != null) trainTransform.position = trainEndingPos;
            if (mainCamera != null)
            {
                mainCamera.transform.position = cameraEndingPos;
                mainCamera.orthographicSize = cameraEndingSize;
                if (CameraShakeManager.Instance != null) CameraShakeManager.Instance.UpdateOriginalPosition();
            }

            SetupResultUI();
            if (endingStatsPanel != null) endingStatsPanel.gameObject.SetActive(true);
        });

        seq.AppendInterval(0.5f);

        if (fadePanel != null) seq.Append(fadePanel.FadeOut(1.0f)); // 밝아짐 (Alpha 0)

/*        seq.AppendInterval(1.0f);*/

        seq.OnComplete(() =>
        {
            isCreditsPlaying = true;
            isSpawningFinished = false;
            StartCoroutine(SpawnCreditsRoutine());
        });
    }

    private void SetupResultUI()
    {
        if (GameManager.Instance == null) return;
        if (normalKillText) normalKillText.text = $"{GameManager.Instance.NormalKillCount}";
        if (eliteKillText) eliteKillText.text = $"{GameManager.Instance.EliteKillCount}";
        if (bossKillText) bossKillText.text = $"{GameManager.Instance.BossKillCount}";
        if (totalKillText) totalKillText.text = $"{GameManager.Instance.TotalKillCount}";
    }

    private IEnumerator SpawnCreditsRoutine()
    {
        foreach (string creditText in developerNames)
        {
            if (isEndingFinished) yield break;

            SpawnCreditObject(creditText);
            yield return new WaitForSecondsRealtime(creditSpawnInterval);
        }
        isSpawningFinished = true;
    }

    private void SpawnCreditObject(string text)
    {
        if (creditPrefab == null || creditSpawnPoints.Length == 0) return;
        int randIdx = Random.Range(0, creditSpawnPoints.Length);
        GameObject obj = Instantiate(creditPrefab, creditSpawnPoints[randIdx].position, Quaternion.identity);
        activeCredits.Add(obj);
        CreditEnemy credit = obj.GetComponent<CreditEnemy>();
        if (credit != null) credit.Initialize(text);
    }

    // ✨ 스킵 시퀀스 (패널 이동 -> 종료)
    private void SkipEndingSequence()
    {
        if (isEndingFinished) return;
        isEndingFinished = true; // 중복 실행 방지

        // 패널 왼쪽으로 날리기 (DOTween)
        if (endingStatsPanel != null)
        {
            endingStatsPanel.DOAnchorPos(offScreenPosition, panelMoveDuration)
                .SetEase(Ease.InBack)
                .SetUpdate(true) // 시간 멈춤 무시
                .OnComplete(() =>
                {
                    // 이동 후 종료 시퀀스 실행
                    FinishEndingSequenceInternal();
                });
        }
        else
        {
            FinishEndingSequenceInternal();
        }
    }

    // 일반 종료 시퀀스 (그냥 페이드 아웃 -> 종료)
    private void FinishEndingSequence()
    {
        if (isEndingFinished) return;
        isEndingFinished = true;

        FinishEndingSequenceInternal();
    }

    // 실제 종료 처리 (페이드 아웃 -> 씬 로드)
    private void FinishEndingSequenceInternal()
    {
        Sequence seq = DOTween.Sequence().SetUpdate(true);

        if (fadePanel != null) seq.Append(fadePanel.FadeIn(1.5f)); // 어두워짐 (Alpha 1)
        else seq.AppendInterval(1.5f);

        seq.OnComplete(() =>
        {
            SceneManager.LoadScene("Start");
        });
    }

    private void ClearAllEntities()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Mob");
        foreach (var e in enemies) Destroy(e);
    }
}