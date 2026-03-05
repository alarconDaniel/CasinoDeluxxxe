using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CasinoStatsMenu : MonoBehaviour
{
    [Header("Root")]
    public CanvasGroup root;
    public float fadeTime = 0.12f;

    [Header("Texts")]
    public TextMeshProUGUI sessionText;
    public TextMeshProUGUI playsText;
    public TextMeshProUGUI winLossText;
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI symbolsText;
    public TextMeshProUGUI footerText;

    [Header("Buttons")]
    public Button exportButton;
    public Button closeButton;

    public bool IsOpen => isOpen;
    bool isOpen;
    public HUDController hud;

    void Awake()
    {
        if (exportButton != null) exportButton.onClick.AddListener(DoExport);
        if (closeButton != null) closeButton.onClick.AddListener(() => SetOpen(false));
        SetOpen(false, instant: true);
    }

    void Update()
    {
        if (!isOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
            SetOpen(false);

        Refresh();
    }

    public void SetOpen(bool open, bool instant = false)
    {
        isOpen = open;

        StopAllCoroutines();
        if (root != null)
        {
            if (instant)
            {
                root.alpha = open ? 1f : 0f;
                root.interactable = open;
                root.blocksRaycasts = open;
            }
            else
            {
                StartCoroutine(Fade(open));
            }
        }

        // Cursor + pausa (para que se sienta “menú real”)
        Cursor.visible = open;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Time.timeScale = open ? 0f : 1f;

        if (open) Refresh();

        if (hud != null)
            hud.SetHudVisible(!open);
    }

    IEnumerator Fade(bool open)
    {
        float from = root.alpha;
        float to = open ? 1f : 0f;
        float t = 0f;

        root.blocksRaycasts = true;

        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeTime);
            root.alpha = Mathf.Lerp(from, to, k);
            yield return null;
        }

        root.alpha = to;
        root.interactable = open;
        root.blocksRaycasts = open;
    }

    void Refresh()
    {
        var stats = CasinoStatsManager.Instance;
        if (stats == null) return;

        var d = stats.Data;
        float minutes = d.sessionSeconds / 60f;
        float rtp = (d.totalBetCoins > 0) ? (float)d.totalPayoutCoins / d.totalBetCoins : 0f;

        if (sessionText != null) sessionText.text = $"Tiempo de juego: {minutes:0.0} min";
        if (playsText != null) playsText.text = $"Tiros • Slots: {d.slotsSpins}  |  Ruleta: {d.rouletteSpins}  |  Yahtzee: {d.yahtzeeMatches}";
        if (winLossText != null) winLossText.text = $"Resultados • Ganadas: {d.totalWins}  |  Perdidas: {d.totalLosses}";
        if (moneyText != null) moneyText.text = $"Monedas • Apostado: {d.totalBetCoins}  |  Pagado: {d.totalPayoutCoins}  |  Neto: {stats.NetCoins}  |  RTP: {rtp:0.000}";

        if (symbolsText != null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Frecuencia (slots):");
            foreach (var sc in d.symbolCounts)
            {
                if (sc.symbol.StartsWith("roulette_")) continue;
                sb.AppendLine($"• {sc.symbol}: {sc.count}");
            }
            symbolsText.text = sb.ToString();
        }
    }

    void DoExport()
    {
        var stats = CasinoStatsManager.Instance;
        if (stats == null) return;

        string path = stats.ExportCsv();
        if (footerText != null) footerText.text = $"CSV guardado en:\n{path}";
    }
}