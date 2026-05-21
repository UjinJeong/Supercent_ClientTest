using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// UI 전반을 관리하는 싱글톤 매니저
/// [부착 위치] Manager > UIManager
/// </summary>
public class UIManager : MonoBehaviour
{
    #region 싱글톤
    public static UIManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }
    #endregion

    #region 인스펙터
    [Header("HUD")]
    public TextMeshProUGUI moneyText;

    [Header("조이스틱")]
    public Joystick joystick;
    #endregion

    #region 프로퍼티
    // PlayerController가 UIManager.Instance 경유로 읽는 조이스틱 입력값
    public float Horizontal => joystick != null ? joystick.Horizontal : 0f;
    public float Vertical   => joystick != null ? joystick.Vertical   : 0f;
    #endregion

    #region 내부 변수
    private bool isSubscribed = false;
    #endregion

    #region 생명주기
    void Start() => StartCoroutine(WaitAndSubscribe());

    void OnDisable() => Unsubscribe();
    void OnDestroy() => Unsubscribe();
    #endregion

    #region 이벤트 구독
    IEnumerator WaitAndSubscribe()
    {
        while (GameManager.Instance == null) yield return null;

        if (!isSubscribed)
        {
            GameManager.Instance.OnMoneyChanged += UpdateMoney;
            isSubscribed = true;
        }
        UpdateMoney(GameManager.Instance.money);
    }

    void Unsubscribe()
    {
        if (!isSubscribed) return;
        if (GameManager.Instance != null)
            GameManager.Instance.OnMoneyChanged -= UpdateMoney;
        isSubscribed = false;
    }
    #endregion

    #region HUD
    void UpdateMoney(int amount)
    {
        if (moneyText != null) moneyText.text = amount.ToString();
    }
    #endregion
}
