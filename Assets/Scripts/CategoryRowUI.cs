using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategoryRowUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text playerScoreText;
    public TMP_Text npcScoreText;
    public Button pickButton;

    public void SetName(string n)
    {
        if (nameText != null) nameText.text = n;
    }

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