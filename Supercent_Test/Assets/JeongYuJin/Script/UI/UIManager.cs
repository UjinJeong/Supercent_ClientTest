using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// UI 전반을 관리하는 싱글톤 매니저
/// - 돈 HUD 표시 (GameManager 이벤트 구독)
/// - 조이스틱 입력값을 PlayerController에 전달하는 창구
/// [부착 위치] Manager > UIManager 오브젝트
/// </summary>
public class UIManager : MonoBehaviour
{
    // ── 싱글톤 ───────────────────────────────────────
    public static UIManager Instance { get; private set; }

    // ── 인스펙터 ─────────────────────────────────────
    [Header("HUD")]
    public TextMeshProUGUI moneyText;   // 화면 상단 돈 표시 텍스트

    [Header("조이스틱")]
    public Joystick joystick;           // UI Canvas > Joystick 오브젝트 연결

    // ── 공개 프로퍼티 (조이스틱 입력 중계) ───────────
    /// <summary>현재 수평 입력값 — Joystick에서 읽어 PlayerController에 전달</summary>
    public float Horizontal => joystick != null ? joystick.Horizontal : 0f;
    /// <summary>현재 수직 입력값 — Joystick에서 읽어 PlayerController에 전달</summary>
    public float Vertical   => joystick != null ? joystick.Vertical   : 0f;

    // ── 내부 변수 ────────────────────────────────────
    private bool isSubscribed = false;  // GameManager 이벤트 중복 구독 방지

    // ── 유니티 생명주기 ──────────────────────────────
    void Awake()
    {
        // 씬에 인스턴스가 이미 존재하면 자신을 제거 (싱글톤 보장)
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // GameManager가 준비될 때까지 대기 후 이벤트 구독
        StartCoroutine(WaitAndSubscribe());
    }

    // ── 이벤트 구독 ──────────────────────────────────
    IEnumerator WaitAndSubscribe()
    {
        // GameManager 싱글톤이 초기화될 때까지 대기
        while (GameManager.Instance == null)
            yield return null;

        if (!isSubscribed)
        {
            GameManager.Instance.OnMoneyChanged += UpdateMoney;
            isSubscribed = true;
        }

        // 현재 금액으로 HUD 초기값 설정
        UpdateMoney(GameManager.Instance.money);
    }

    void OnDisable() => Unsubscribe();
    void OnDestroy() => Unsubscribe();

    void Unsubscribe()
    {
        if (!isSubscribed) return;
        if (GameManager.Instance != null)
            GameManager.Instance.OnMoneyChanged -= UpdateMoney;
        isSubscribed = false;
    }

    // ── HUD 갱신 ─────────────────────────────────────
    /// <summary>돈이 변경될 때 HUD 텍스트를 갱신한다</summary>
    void UpdateMoney(int amount)
    {
        if (moneyText != null)
            moneyText.text = amount.ToString();
    }
}
