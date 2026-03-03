using UnityEngine;
using UnityEngine.UI;

public class CategoryRowUI : MonoBehaviour
{
    public TMPro.TMP_Text nameText;
    public TMPro.TMP_Text playerScoreText;
    public TMPro.TMP_Text npcScoreText;
    public Button pickButton;

    CanvasGroup pickCG;

    void Awake()
    {
        if (pickButton != null)
        {
            pickCG = pickButton.GetComponent<CanvasGroup>();
            if (pickCG == null) pickCG = pickButton.gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void SetPickButtonGhost(bool visible)
    {
        if (pickButton == null) return;

        // visible = true => normal
        // visible = false => "fantasma": ocupa espacio pero no se ve ni clickea
        if (pickCG == null) pickCG = pickButton.GetComponent<CanvasGroup>();

        pickCG.alpha = visible ? 1f : 0f;
        pickCG.interactable = visible;
        pickCG.blocksRaycasts = visible;
    }

    public void SetName(string n) { if (nameText != null) nameText.text = n; }

    public void SetScores(int? player, int? npc)
    {
        if (playerScoreText != null) playerScoreText.text = player.HasValue ? player.Value.ToString() : "-";
        if (npcScoreText != null) npcScoreText.text = npc.HasValue ? npc.Value.ToString() : "-";
    }

    public void SetPickable(bool pickable)
    {
        if (pickButton != null) pickButton.interactable = pickable;
    }
}