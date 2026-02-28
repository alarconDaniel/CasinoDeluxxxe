using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    public TMP_Text hudText;
    public TMP_Text promptText;
    public TMP_Text messageText;

    void Start()
    {
        CasinoGameManager.Instance.OnStateChanged += Refresh;
        Refresh();
    }

    void Refresh()
    {
        var gm = CasinoGameManager.Instance;
        float minutes = gm.sessionSeconds / 60f;

        hudText.text =
            $"Monedas (C): {gm.coins}\n" +
            $"Nivel (L): {gm.level}\n" +
            $"XP: {gm.xp}\n" +
            $"Apuesta (B): {gm.bet}  ( - / + para cambiar )\n" +
            $"Tiempo (T): {minutes:0.0} min";

        messageText.text = gm.lastMessage;
    }

    public void SetPrompt(string txt)
    {
        promptText.text = txt;
        promptText.gameObject.SetActive(!string.IsNullOrEmpty(txt));
    }
}