using System.Collections;
using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    public TMP_Text hudText;
    public TMP_Text promptText;
    public TMP_Text messageText; // lo dejamos para otros mensajes si quieres

    public CanvasGroup group;

    [Header("Prompt")]
    public float promptFontSizeMultiplier = 1f; // 1 = igual a hudText

    [Header("Overlays (auto-creados)")]
    public float prizeFontSize = 22f;
    public float resultFontSize = 36f;

    // offsets desde el borde (píxeles)
    public Vector2 prizeOffset = new Vector2(-20f, -20f);   // arriba-derecha
    public Vector2 resultOffset = new Vector2(0f, -20f);    // arriba-centro

    public Vector2 prizeSize = new Vector2(520f, 220f);
    public Vector2 resultSize = new Vector2(1000f, 120f);

    TMP_Text prizeText;
    TMP_Text resultText;

    bool bound;
    Coroutine resultRoutine;

    void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        EnsureOverlays();
    }

    void OnEnable()
    {
        bound = false;
        TryBind();
        Refresh();
    }

    void Update()
    {
        if (!bound) TryBind();
    }

    void OnDisable()
    {
        var gm = CasinoGameManager.Instance;
        if (gm != null) gm.OnStateChanged -= Refresh;
        bound = false;
    }

    void TryBind()
    {
        var gm = CasinoGameManager.Instance;
        if (gm == null) return;

        gm.OnStateChanged -= Refresh;
        gm.OnStateChanged += Refresh;

        bound = true;
        Refresh();
    }

    void Refresh()
    {
        var gm = CasinoGameManager.Instance;
        if (gm == null) return;

        float minutes = gm.sessionSeconds / 60f;

        if (hudText != null)
        {
            hudText.text =
                $"Monedas (C): {gm.coins}\n" +
                $"Nivel (L): {gm.level}\n" +
                $"XP: {gm.xp}\n" +
                $"Apuesta (B): {gm.bet}  ( - / + para cambiar )\n" +
                $"Tiempo (T): {minutes:0.0} min";
        }

        if (messageText != null) messageText.text = gm.lastMessage;
    }

    // ------------------ PROMPT ------------------

    public void SetPrompt(string txt)
    {
        if (promptText == null) return;

        // ✅ Asegura tamaño del prompt igual al hudText (o multiplicador)
        if (hudText != null)
            promptText.fontSize = hudText.fontSize * promptFontSizeMultiplier;

        promptText.text = txt;
        promptText.gameObject.SetActive(!string.IsNullOrEmpty(txt));
    }

    public void SetHudVisible(bool visible)
    {
        if (group != null)
        {
            group.alpha = visible ? 1f : 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        if (!visible) SetPrompt("");
    }

    // ------------------ OVERLAYS (NUEVO) ------------------

    public void ShowPrizeTable(string text)
    {
        EnsureOverlays();
        prizeText.text = text;
        prizeText.gameObject.SetActive(true);
    }

    public void HidePrizeTable()
    {
        EnsureOverlays();
        prizeText.text = "";
        prizeText.gameObject.SetActive(false);
    }

    public void ShowResultTimed(string text, float seconds)
    {
        EnsureOverlays();

        if (resultRoutine != null) StopCoroutine(resultRoutine);
        resultRoutine = StartCoroutine(ResultRoutine(text, seconds));
    }

    IEnumerator ResultRoutine(string text, float seconds)
    {
        resultText.text = text;
        resultText.gameObject.SetActive(true);

        yield return new WaitForSeconds(seconds);

        resultText.text = "";
        resultText.gameObject.SetActive(false);
        resultRoutine = null;
    }

    void EnsureOverlays()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        TMP_Text template = messageText != null ? messageText : (hudText != null ? hudText : promptText);

        if (prizeText == null)
        {
            prizeText = CreateOverlayText(canvas.transform, "PrizeOverlay", template);
            ConfigureTopRight(prizeText.rectTransform);
            prizeText.fontSize = prizeFontSize;
            prizeText.alignment = TextAlignmentOptions.TopRight;
            prizeText.enableWordWrapping = false;   // ✅ evita que se “descuadre”
            prizeText.gameObject.SetActive(false);
        }

        if (resultText == null)
        {
            resultText = CreateOverlayText(canvas.transform, "ResultOverlay", template);
            ConfigureTopCenter(resultText.rectTransform);
            resultText.fontSize = resultFontSize;
            resultText.alignment = TextAlignmentOptions.Top;
            resultText.enableWordWrapping = false;
            resultText.gameObject.SetActive(false);
        }
    }

    TMP_Text CreateOverlayText(Transform parent, string name, TMP_Text template)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var t = go.AddComponent<TextMeshProUGUI>();
        t.raycastTarget = false;

        if (template != null)
        {
            t.font = template.font;
            t.fontSharedMaterial = template.fontSharedMaterial;
            t.color = template.color;
            t.outlineWidth = template.outlineWidth;
            t.outlineColor = template.outlineColor;
        }
        else
        {
            t.color = Color.white;
        }

        return t;
    }

    void ConfigureTopRight(RectTransform rt)
    {
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = prizeOffset;
        rt.sizeDelta = prizeSize;
    }

    void ConfigureTopCenter(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = resultOffset;
        rt.sizeDelta = resultSize;
    }
}