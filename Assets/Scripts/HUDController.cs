using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    public TMP_Text hudText;
    public TMP_Text promptText;
    public TMP_Text messageText;

    public CanvasGroup group;

    bool bound;

    void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        bound = false;
        TryBind();   // intenta una vez
        Refresh();   // por si ya está el GM
    }

    void Update()
    {
        if (!bound) TryBind(); // reintenta hasta que exista el GM
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

        // evita doble suscripción
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

    public void SetPrompt(string txt)
    {
        if (promptText == null) return;
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
}