using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class YahtzeeMenuUI : MonoBehaviour
{
    [Header("Root")]
    public CanvasGroup root;
    public float fadeTime = 0.12f;

    [Header("Texts")]
    public TMP_Text titleText;
    public TMP_Text statusText;
    public TMP_Text playerDiceText;
    public TMP_Text npcDiceText;

    [Header("Buttons")]
    public Button rollButton;
    public Button exitButton;

    [Header("Holds (5)")]
    public Toggle[] holdToggles = new Toggle[5];

    [Header("Score rows")]
    public Transform rowsParent;
    public CategoryRowUI rowPrefab;
    CategoryRowUI totalRow;

    public bool IsOpen => isOpen;
    bool isOpen;

    public event Action OnRollPressed;
    public event Action OnExitPressed;
    public event Action<YahtzeeCategory> OnCategoryPressed;

    readonly Dictionary<YahtzeeCategory, CategoryRowUI> rows = new();

    void Awake()
    {
        if (rollButton != null) rollButton.onClick.AddListener(() => OnRollPressed?.Invoke());
        if (exitButton != null) exitButton.onClick.AddListener(() => OnExitPressed?.Invoke());

        SetOpen(false, true);
    }

    public void BuildRows()
    {
        // Limpia
        foreach (Transform ch in rowsParent) Destroy(ch.gameObject);
        rows.Clear();

        foreach (YahtzeeCategory c in Enum.GetValues(typeof(YahtzeeCategory)))
        {
            var row = Instantiate(rowPrefab, rowsParent);
            row.SetName(YahtzeeScoring.Pretty(c));
            row.SetScores(null, null);

            if (row.pickButton != null)
            {
                var captured = c;
                row.pickButton.onClick.AddListener(() => OnCategoryPressed?.Invoke(captured));
            }

            rows[c] = row;
        }

        // --- TOTAL ROW ---
        totalRow = Instantiate(rowPrefab, rowsParent);
        totalRow.SetName("TOTAL");
        totalRow.SetScores(0, 0);
totalRow.SetPickButtonGhost(false); // sin botón
    }

    public void SetRowState(YahtzeeCategory c, int? p, int? n, bool pickable)
    {
        if (!rows.TryGetValue(c, out var row)) return;
        row.SetScores(p, n);
        row.SetPickable(pickable);
    }

    public bool[] GetHoldMask()
    {
        bool[] m = new bool[holdToggles.Length];
        for (int i = 0; i < holdToggles.Length; i++)
            m[i] = (holdToggles[i] != null && holdToggles[i].isOn);
        return m;
    }

    public void SetHoldsInteractable(bool on)
    {
        for (int i = 0; i < holdToggles.Length; i++)
        {
            if (holdToggles[i] == null) continue;
            holdToggles[i].interactable = on;
        }
    }

    public void ResetHolds()
    {
        for (int i = 0; i < holdToggles.Length; i++)
            if (holdToggles[i] != null) holdToggles[i].isOn = false;
    }

    public void SetDiceTexts(int[] playerDice, int[] npcDice)
    {
        if (playerDiceText != null) playerDiceText.text = "Tú: " + string.Join(" ", playerDice);
        if (npcDiceText != null) npcDiceText.text = "NPC: " + string.Join(" ", npcDice);
    }

    public void SetStatus(string s)
    {
        if (statusText != null) statusText.text = s;
    }

    public void SetRollButtonInteractable(bool on)
    {
        if (rollButton != null) rollButton.interactable = on;
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
    }

    System.Collections.IEnumerator Fade(bool open)
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

    public void SetTotals(int playerTotal, int npcTotal)
    {
        if (totalRow == null) return;
        totalRow.SetScores(playerTotal, npcTotal);
    }
}