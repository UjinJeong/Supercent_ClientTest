using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI prisonerCountText;    // "3/20"
    public TextMeshProUGUI handcuffText;         // 수갑 개수

    [Header("Money Popup")]
    public GameObject moneyPopupPrefab;          // "+100" 팝업
    public Canvas worldCanvas;

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip coinSFX;
    public AudioClip armedSFX;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        GameManager.Instance.OnMoneyChanged       += UpdateMoney;
        GameManager.Instance.OnPrisonerCountChanged += UpdatePrisoners;
        GameManager.Instance.OnHandcuffChanged    += UpdateHandcuff;
    }

    void OnDisable()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnMoneyChanged       -= UpdateMoney;
        GameManager.Instance.OnPrisonerCountChanged -= UpdatePrisoners;
        GameManager.Instance.OnHandcuffChanged    -= UpdateHandcuff;
    }

    void Start()
    {
        // 초기값 반영
        UpdateMoney(GameManager.Instance.money);
        UpdatePrisoners(GameManager.Instance.prisonerCount, GameManager.Instance.maxPrisoners);
        UpdateHandcuff(GameManager.Instance.handcuffCount);
    }

    void UpdateMoney(int amount)
    {
        if (moneyText) moneyText.text = amount.ToString();
        audioSource?.PlayOneShot(coinSFX);
    }

    void UpdatePrisoners(int current, int max)
    {
        if (prisonerCountText) prisonerCountText.text = $"{current}/{max}";
    }

    void UpdateHandcuff(int count)
    {
        if (handcuffText) handcuffText.text = count.ToString();
    }

    // 월드 공간에 "+숫자" 팝업 생성
    public void ShowMoneyPopup(Vector3 worldPos, int amount)
    {
        if (moneyPopupPrefab == null || worldCanvas == null) return;

        GameObject popup = Instantiate(moneyPopupPrefab, worldCanvas.transform);
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        popup.GetComponent<RectTransform>().position = screenPos;

        TextMeshProUGUI txt = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (txt) txt.text = $"+{amount}";

        Destroy(popup, 1.5f);
    }
}
