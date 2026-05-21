using System.Collections;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    public TextMeshProUGUI moneyText;

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip coinSFX;

    private bool isSubscribed = false;
    private int lastMoney = 0;
    private bool hasLastMoney = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(WaitAndSubscribe());
    }

    IEnumerator WaitAndSubscribe()
    {
        while (GameManager.Instance == null)
            yield return null;

        if (!isSubscribed)
        {
            GameManager.Instance.OnMoneyChanged += UpdateMoney;
            isSubscribed = true;
        }

        // 초기값 반영 (SFX 방지용으로 lastMoney 먼저 세팅)
        lastMoney = GameManager.Instance.money;
        hasLastMoney = true;
        UpdateMoney(GameManager.Instance.money);
    }

    void OnDisable()  { Unsubscribe(); }
    void OnDestroy()  { Unsubscribe(); }

    void Unsubscribe()
    {
        if (!isSubscribed) return;
        if (GameManager.Instance != null)
            GameManager.Instance.OnMoneyChanged -= UpdateMoney;
        isSubscribed = false;
    }

    void UpdateMoney(int amount)
    {
        if (moneyText) moneyText.text = amount.ToString();

        // 금액이 증가했을 때만 SFX 재생
        if (hasLastMoney && amount > lastMoney && audioSource != null && coinSFX != null)
            audioSource.PlayOneShot(coinSFX);

        lastMoney = amount;
        hasLastMoney = true;
    }
}
