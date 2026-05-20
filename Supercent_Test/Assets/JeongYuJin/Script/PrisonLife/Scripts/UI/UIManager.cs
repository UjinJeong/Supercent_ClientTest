using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // 싱글톤 인스턴스: 전역에서 UIManager에 접근할 때 사용
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    // 화면 HUD에 표시될 텍스트들
    public TextMeshProUGUI moneyText;
  //  public TextMeshProUGUI prisonerCountText;    // "3/20" 형태로 표시
  //  public TextMeshProUGUI handcuffText;         // 수갑 개수 표시

    [Header("Money Popup")]
    public GameObject moneyPopupPrefab;          // 월드 스페이스에서 뜨는 "+100" 팝업 프리팹
    public Canvas worldCanvas;                   // 팝업 렌더링용 캔버스

    [Header("Sound")]
    public AudioSource audioSource;              // 효과음 재생용 오디오소스
    public AudioClip coinSFX;                    // 동전 획득 효과음
    public AudioClip armedSFX;                   // 무장 관련 효과음(미사용 시 무시)

    // GameManager 구독 상태 추적
    private bool isSubscribedToGameManager = false;

    // 마지막으로 표시된 금액(초기에는 unset)
    private int lastMoney = 0;
    private bool hasLastMoney = false;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        // GameManager 인스턴스가 준비될 때까지 대기한 뒤 구독 및 초기화 수행
        StartCoroutine(EnsureGameManagerAndSubscribe());
    }

    System.Collections.IEnumerator EnsureGameManagerAndSubscribe()
    {
        // GameManager.Instance가 null인 경우 프레임마다 대기
        while (GameManager.Instance == null)
        {
            // 씬에 GameManager가 존재하지만 Awake가 아직 실행되지 않았을 수 있으므로
            // FindObjectOfType로 확인하고, 그래도 Instance가 세팅될 때까지 한 프레임씩 대기
            var gm = FindObjectOfType<GameManager>();
            yield return null;
        }

        // 안전하게 구독 수행
        SubscribeToGameManager();

        // 초기값 반영: 초기 음성 재생을 방지하기 위해 lastMoney를 먼저 세팅
        lastMoney = GameManager.Instance.money;
        hasLastMoney = true;

        UpdateMoney(GameManager.Instance.money);
        UpdatePrisoners(GameManager.Instance.prisonerCount, GameManager.Instance.maxPrisoners);
        UpdateHandcuff(GameManager.Instance.handcuffCount);
    }

    // GameManager 이벤트에 안전하게 구독
    void SubscribeToGameManager()
    {
        if (isSubscribedToGameManager) return;
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnMoneyChanged       += UpdateMoney;
        GameManager.Instance.OnPrisonerCountChanged += UpdatePrisoners;
        GameManager.Instance.OnHandcuffChanged    += UpdateHandcuff;

        isSubscribedToGameManager = true;
    }

    // 구독 해제 (비활성화/파괴 시 안전하게 호출)
    void UnsubscribeFromGameManager()
    {
        if (!isSubscribedToGameManager) return;
        if (GameManager.Instance == null) { isSubscribedToGameManager = false; return; }

        GameManager.Instance.OnMoneyChanged       -= UpdateMoney;
        GameManager.Instance.OnPrisonerCountChanged -= UpdatePrisoners;
        GameManager.Instance.OnHandcuffChanged    -= UpdateHandcuff;

        isSubscribedToGameManager = false;
    }

    void OnDisable()
    {
        // OnDisable에서 바로 접근하면 GameManager.Instance가 null일 수 있으므로 안전하게 처리
        UnsubscribeFromGameManager();
    }

    void OnDestroy()
    {
        // 확실한 정리
        UnsubscribeFromGameManager();
    }

    // 금액 변경 시 호출되어 HUD 갱신 및 효과음 재생
    void UpdateMoney(int amount)
    {
        if (moneyText) moneyText.text = amount.ToString();

        // 첫 초기화 시 불필요한 SFX 재생을 막기 위해 lastMoney를 이용해 비교
        // coinSFX는 금액이 증가했을 때만 재생하도록 함
        if (audioSource != null && coinSFX != null)
        {
            if (hasLastMoney)
            {
                if (amount > lastMoney)
                    audioSource.PlayOneShot(coinSFX);
            }
            // else: hasLastMoney가 false인 경우(정상적으론 Ensure에서 세팅하므로 보통 발생하지 않음) 재생하지 않음
        }

        lastMoney = amount;
        hasLastMoney = true;
    }

    // 수감자 수 변경 시 호출되어 "현재/최대" 형태로 표시
    void UpdatePrisoners(int current, int max)
    {
       // if (prisonerCountText) prisonerCountText.text = $"{current}/{max}";
    }

    // 수갑 수량 변경 시 호출되어 표시 업데이트
    void UpdateHandcuff(int count)
    {
      //  if (handcuffText) handcuffText.text = count.ToString();
    }

    // 월드 좌표에 "+숫자" 형태의 팝업을 생성하여 표시
    // 팝업은 카메라를 항상 바라보도록 Billboard 컴포넌트를 붙여 처리한다.
    public void ShowMoneyPopup(Vector3 worldPos, int amount)
    {
        if (moneyPopupPrefab == null || worldCanvas == null) return;

        // 캔버스 하위에 팝업 인스턴스 생성
        GameObject popup = Instantiate(moneyPopupPrefab, worldCanvas.transform);

        // 월드 좌표를 스크린 위치로 변환하여 팝업 위치에 적용 (초기 위치)
        if (Camera.main != null)
        {
            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            popup.GetComponent<RectTransform>().position = screenPos;
        }

        // 팝업 내부 텍스트에 금액 표시
        TextMeshProUGUI txt = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (txt) txt.text = $"+{amount}";

        // Billboard 컴포넌트를 붙여 팝업이 카메라를 계속 바라보게 하고, 수명 설정
        var billboard = popup.AddComponent<WorldToScreenBillboard>();
        billboard.worldPosition = worldPos;
        billboard.lifetime = 1.5f;
    }
}
