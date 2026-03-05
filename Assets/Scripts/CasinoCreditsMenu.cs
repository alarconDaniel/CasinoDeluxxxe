using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CasinoCreditsMenu : MonoBehaviour
{
    [Header("Root")]
    public CanvasGroup root;
    public float fadeTime = 0.12f;

    [Header("UI")]
    public ScrollRect scroll;
    public TextMeshProUGUI creditsText;
    public Button closeButton;

    [Header("Opcional")]
    public HUDController hud;

    [TextArea(12, 40)]
    public string credits =
@"CRÉDITOS

Daniel Alarcón — programación y diseño
Juan Silva — programación de la ruleta
Felipe Gordillo — recocha y chanza

ASSETS / MODELOS
Table Round Small by Quaternius (https://poly.pizza/m/oEArSZykyi)
Spot Light by iPoly3D (https://poly.pizza/m/eaQqgLn0oJ)
Monkey by Aya Kawa [CC-BY] (https://creativecommons.org/licenses/by/3.0/) via Poly Pizza (https://poly.pizza/m/2qKt4ZeTtXb)
Bar v2 by Adrian Hon [CC-BY] (https://creativecommons.org/licenses/by/3.0/) via Poly Pizza (https://poly.pizza/m/9f9xBGPIrtp)
Dice by Poly by Google [CC-BY] (https://creativecommons.org/licenses/by/3.0/) via Poly Pizza (https://poly.pizza/m/dLS2cGjYw6C)
Horse by Aya Kawa [CC-BY] (https://creativecommons.org/licenses/by/3.0/) via Poly Pizza (https://poly.pizza/m/cIDCkP-PZVs)
""Mage"" (https://skfb.ly/69VpA) by undeadfae is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).
Stool by Poly by Google [CC-BY] (https://creativecommons.org/licenses/by/3.0/) via Poly Pizza (https://poly.pizza/m/cLydFlVg-wI)
Wall by Quaternius (https://poly.pizza/m/CkF171SeTV)
metal scaffold by Simon Hischier [CC-BY] (https://creativecommons.org/licenses/by/3.0/) via Poly Pizza (https://poly.pizza/m/fETtk2OoFZe)
Cable Long by Quaternius (https://poly.pizza/m/WNfAG8VSD5)
Cable by Quaternius (https://poly.pizza/m/aoNcGMnNiG)

AUDIO
Slots winning sound by https://www.youtube.com/watch?v=ZIcPW_uGPhU
Slots spinning sound by https://www.youtube.com/watch?v=hRDGI3kcjhg
Music jingles by https://kenney.nl/assets/music-jingles
Interface sounds by https://kenney.nl/assets/interface-sounds
Casino audios (dice throws) by https://kenney.nl/assets/casino-audio
Yahtzee winning jingle by https://freesound.org/people/LittleRobotSoundFactory/sounds/270545/

MATERIALS
Yughues Free Fabric Materials by Nobiax / Yughees
Hand Painted Seamless Wood Texture Vol - 6 by Innovana Games
PBR Materials - Wood & Metal by Adam Bielecki
PBR Materials - Wood & Metal by Nobiax / Yughees

";

    public bool IsOpen => isOpen;
    bool isOpen;

    void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(() => SetOpen(false));

        SetOpen(false, instant: true);
    }

    void Update()
    {
        if (!isOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
            SetOpen(false);
    }

    public void SetOpen(bool open, bool instant = false)
    {
        isOpen = open;

        // Set text + reset scroll when opening
        if (open)
        {
            if (creditsText != null) creditsText.text = credits;

            if (scroll != null)
            {
                Canvas.ForceUpdateCanvases();
                scroll.verticalNormalizedPosition = 1f; // arriba
            }
        }

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

        // Igual que StatsMenu: cursor + pausa
        Cursor.visible = open;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Time.timeScale = open ? 0f : 1f;

        if (hud != null)
            hud.SetHudVisible(!open);

        // sfx opcional (si tienes CasinoAudioSfx)
        var audio = CasinoAudioSfx.Instance;
        if (audio != null) audio.PlayPickCategory();
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
}