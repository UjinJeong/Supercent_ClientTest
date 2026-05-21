using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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

    [Header("오디오 토글")]
    public Toggle              audioToggle;     // UI Canvas > Audio Toggle
    public TextMeshProUGUI     audioToggleText; // Audio Toggle > Text (TMP)
    #endregion

    #region 프로퍼티
    // PlayerController가 UIManager.Instance 경유로 읽는 조이스틱 입력값
    public float Horizontal => joystick != null ? joystick.Horizontal : 0f;
    public float Vertical   => joystick != null ? joystick.Vertical   : 0f;
    #endregion

    #region 내부 변수
    private bool  isSubscribed = false;

    // 토글 ON/OFF 색상 (Toggle ColorBlock에서 초기화 시 캐시)
    private Color audioOnColor;  // ON  → Normal Color
    private Color audioOffColor; // OFF → Selected Color
    #endregion

    #region 생명주기
    void Start()
    {
        StartCoroutine(WaitAndSubscribe());
        InitAudioToggle();
    }

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

    #region 오디오 토글
    void InitAudioToggle()
    {
        if (audioToggle == null) return;

        // Toggle ColorBlock에서 ON/OFF 색상 캐시
        audioOnColor  = audioToggle.colors.normalColor;
        audioOffColor = audioToggle.colors.selectedColor;

        // 초기 상태 : 음성 ON
        audioToggle.isOn = true;
        AudioListener.volume = 1f;
        ApplyAudioToggleColor(true);
        RefreshAudioToggleText(true);

        audioToggle.onValueChanged.AddListener(OnAudioToggleChanged);
    }

    /// <summary>Toggle의 On Value Changed 이벤트에 연결하거나, AddListener로 자동 등록됨</summary>
    public void OnAudioToggleChanged(bool isOn)
    {
        AudioListener.volume = isOn ? 1f : 0f;

        // normalColor를 원하는 색으로 바꾼 뒤 디셀렉트
        // → Unity가 Normal 상태로 전환할 때 바뀐 normalColor가 적용됨
        ApplyAudioToggleColor(isOn);
        RefreshAudioToggleText(isOn);
        EventSystem.current.SetSelectedGameObject(null);
    }

    void RefreshAudioToggleText(bool isOn)
    {
        if (audioToggleText != null)
            audioToggleText.text = isOn ? "Audio On" : "Audio Off";
    }

    void ApplyAudioToggleColor(bool isOn)
    {
        if (audioToggle == null) return;

        Color target   = isOn ? audioOnColor : audioOffColor;
        ColorBlock cb  = audioToggle.colors;
        cb.normalColor      = target; // Normal 상태 색 교체
        cb.highlightedColor = target; // 마우스 오버 시에도 동일 색 유지
        audioToggle.colors  = cb;
    }
    #endregion
}
