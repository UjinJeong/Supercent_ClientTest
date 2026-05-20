using UnityEngine;
using TMPro;
using System.Collections;

public class MoneyPopup : MonoBehaviour
{
    public float riseSpeed = 100f;
    public float fadeDuration = 1f;

    private TextMeshProUGUI label;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        label = GetComponentInChildren<TextMeshProUGUI>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        StartCoroutine(AnimatePopup());
    }

    IEnumerator AnimatePopup()
    {
        float elapsed = 0f;
        RectTransform rt = GetComponent<RectTransform>();
        Vector2 startPos = rt.anchoredPosition;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            rt.anchoredPosition = startPos + Vector2.up * riseSpeed * t;
            canvasGroup.alpha = 1f - t;

            yield return null;
        }

        Destroy(gameObject);
    }
}
