using System.Collections;
using UnityEngine;

public class YahtzeeController : MonoBehaviour
{
    [Header("Refs")]
    public YahtzeeMenuUI ui;
    public HUDController hud;

    public Camera playerCam;
    public Transform camPoint;
    public Transform camLook;

    [Tooltip("Scripts que se desactivan mientras juegas (PlayerMovement, MouseLook, PlayerInteractor, etc.)")]
    public MonoBehaviour[] disableWhilePlaying;

    [Header("Dice")]
    public DiceRoller playerRoller;
    public DiceRoller npcRoller;

    [Header("Camera move")]
    public float camMoveTime = 0.25f;

    bool inMatch;
    bool rollRequested;
    bool exitRequested;
    YahtzeeCategory? chosenCat;

    int bet;
    YahtzeeScoreCard playerCard;
    YahtzeeScoreCard npcCard;

    int[] lastPlayerDice = new int[5];
    int[] lastNpcDice = new int[5];

    Vector3 camPosBefore;
    Quaternion camRotBefore;

    void Awake()
    {
        if (ui != null)
        {
            ui.OnRollPressed += () => rollRequested = true;
            ui.OnExitPressed += () => exitRequested = true;
            ui.OnCategoryPressed += (c) => chosenCat = c;
        }
    }

    public void TryStartMatch()
    {
        if (inMatch) return;

        var gm = CasinoGameManager.Instance;
        if (gm == null) return;

        bet = gm.bet;

        if (!gm.TrySpendCoins(bet))
        {
            gm.SetMessage("No tienes monedas suficientes para apostar.");
            return;
        }

        StartCoroutine(MatchRoutine());
    }

    IEnumerator MatchRoutine()
    {
        inMatch = true;
        exitRequested = false;

        // UI open (sin pausar)
        ui.BuildRows();
        ui.SetOpen(true);
        ui.ResetHolds();
        ui.SetHoldsInteractable(false);
        ui.SetStatus("Entrando a Yahtzee...");

        if (hud != null) hud.SetHudVisible(false);

        // Cursor + bloquear player
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        foreach (var mb in disableWhilePlaying)
            if (mb != null) mb.enabled = false;

        // Camera move
        if (playerCam != null)
        {
            camPosBefore = playerCam.transform.position;
            camRotBefore = playerCam.transform.rotation;
            yield return MoveCamera(playerCam.transform, camPoint.position, camLook.position, camMoveTime);
        }

        playerCard = new YahtzeeScoreCard();
        npcCard = new YahtzeeScoreCard();

        ui.SetTotals(0, 0);

        // 13 rondas (categorías)
        for (int round = 1; round <= 13; round++)
        {
            if (exitRequested) break;

            yield return PlayerTurn(round);
            if (exitRequested) break;

            yield return NpcTurn(round);
        }

        // Resolve
        var gm = CasinoGameManager.Instance;

        bool forfeited = exitRequested;
        int playerTotal = playerCard.Total();
        int npcTotal = npcCard.Total();

        bool win = !forfeited && (playerTotal >= npcTotal);

        int payout = 0;
        if (win)
        {
            payout = bet * 2;
            gm.AddCoins(payout);
            gm.SetMessage($"Yahtzee: GANASTE. +{payout} monedas (Total {playerTotal} vs {npcTotal})");
        }
        else
        {
            gm.SetMessage(forfeited
                ? $"Yahtzee: Saliste -> cuenta como derrota. (Perdiste {bet})"
                : $"Yahtzee: Perdiste. (Total {playerTotal} vs {npcTotal})");
        }

        gm.AddXP(bet);

        // stats (opcional)
        var stats = CasinoStatsManager.Instance;
        if (stats != null)
        {
            stats.RecordYahtzeeMatch(bet, win, payout);
        }

        ui.SetHoldsInteractable(false);
        ui.SetRollButtonInteractable(false);
        ui.SetStatus("Partida terminada. Dale SALIR.");

        // Espera a que el usuario le dé salir (si no lo hizo)
        exitRequested = false;
        while (!exitRequested)
            yield return null;

        yield return ExitMatch();
    }

    IEnumerator PlayerTurn(int round)
    {
        rollRequested = false;
        chosenCat = null;

        ui.ResetHolds();
        ui.SetHoldsInteractable(false);
        ui.SetStatus($"Ronda {round}/13 - TU TURNO (hasta 3 rolls).");

        // Solo habilitamos elegir categoría después de 1er roll
        SetAllPickables(false);

        int rolls = 0;
        bool hasRolledOnce = false;

        while (chosenCat == null)
        {
            if (exitRequested) yield break;

            ui.SetRollButtonInteractable(rolls < 3);

            // espera acción
            yield return new WaitUntil(() => exitRequested || rollRequested || chosenCat != null);

            if (exitRequested) yield break;

            if (rollRequested && rolls < 3)
            {
                rollRequested = false;
                rolls++;

                ui.SetStatus($"Ronda {round}/13 - Roll {rolls}/3 (marca HOLD si quieres).");
                ui.SetHoldsInteractable(false);

                int[] values = null;
                bool[] hold = hasRolledOnce ? ui.GetHoldMask() : new bool[5]; // primer roll: ignora holds

                yield return StartCoroutine(playerRoller.Roll(hold, v => values = v));

                if (values != null && values.Length == lastPlayerDice.Length)
                    lastPlayerDice = values;

                hasRolledOnce = true;
                ui.SetHoldsInteractable(true);

                // ya se puede escoger categoría
                RefreshRowsPickable(playerCard, canPick: true);
                ui.SetDiceTexts(lastPlayerDice, lastNpcDice);

                // si ya hizo 3 rolls, fuerza a escoger
                if (rolls >= 3)
                {
                    ui.SetStatus("Elige una categoría (ya no hay más rolls).");
                    ui.SetRollButtonInteractable(false);
                }
            }

            // si el usuario cliqueó una categoría
            if (chosenCat != null)
            {
                if (playerCard.IsUsed(chosenCat.Value))
                {
                    ui.SetStatus("Esa categoría ya está usada. Elige otra.");
                    chosenCat = null;
                    continue;
                }

                int score = YahtzeeScoring.Score(chosenCat.Value, lastPlayerDice);
                playerCard.Set(chosenCat.Value, score);
                ui.SetTotals(playerCard.Total(), npcCard.Total());

                ui.SetStatus($"Guardaste {score} en {YahtzeeScoring.Pretty(chosenCat.Value)}.");
                RefreshRowsPickable(playerCard, canPick: false); // se deshabilitan usadas, pero dejamos el estado correcto
                ui.SetDiceTexts(lastPlayerDice, lastNpcDice);

                yield return new WaitForSeconds(0.35f);
                yield break;
            }
        }
    }

    IEnumerator NpcTurn(int round)
    {
        ui.SetStatus($"Ronda {round}/13 - TURNO NPC...");
        ui.SetHoldsInteractable(false);
        ui.SetRollButtonInteractable(false);

        bool[] hold = new bool[5];
        int[] values = null;

        // 3 rolls “tontos pero efectivos”
        for (int r = 1; r <= 3; r++)
        {
            if (exitRequested) yield break;

            yield return StartCoroutine(npcRoller.Roll(hold, v => values = v));
            if (values != null && values.Length == lastNpcDice.Length)
                lastNpcDice = values;

            ui.SetDiceTexts(lastPlayerDice, lastNpcDice);
            ui.SetStatus($"NPC roll {r}/3...");

            // decide holds para el siguiente (simple heuristic)
            if (r < 3)
                hold = DecideNpcHolds(lastNpcDice);

            yield return new WaitForSeconds(0.4f);
        }

        // elige mejor categoría disponible
        YahtzeeCategory bestCat = FindBestCategory(npcCard, lastNpcDice);
        int bestScore = YahtzeeScoring.Score(bestCat, lastNpcDice);

        npcCard.Set(bestCat, bestScore);
        ui.SetTotals(playerCard.Total(), npcCard.Total());

        ui.SetStatus($"NPC eligió {YahtzeeScoring.Pretty(bestCat)} = {bestScore}");
        RefreshRowsPickable(playerCard, canPick: true); // player pickables se mantienen bien
        RefreshRowsNpc(npcCard);
        yield return new WaitForSeconds(0.6f);
    }

    bool[] DecideNpcHolds(int[] dice)
    {
        // idea: quedarse con el número que más se repite (ayuda a 3-kind, 4-kind, yahtzee, full house)
        int[] counts = new int[7];
        foreach (var d in dice) counts[d]++;

        int bestVal = 1;
        int bestCount = counts[1];
        for (int v = 2; v <= 6; v++)
        {
            if (counts[v] > bestCount)
            {
                bestCount = counts[v];
                bestVal = v;
            }
        }

        bool[] hold = new bool[5];
        for (int i = 0; i < dice.Length; i++)
            hold[i] = (dice[i] == bestVal);

        // si todo es muy parejo, no holds
        if (bestCount <= 1)
            return new bool[5];

        return hold;
    }

    YahtzeeCategory FindBestCategory(YahtzeeScoreCard card, int[] dice)
    {
        YahtzeeCategory best = YahtzeeCategory.Chance;
        int bestScore = -1;

        foreach (YahtzeeCategory c in System.Enum.GetValues(typeof(YahtzeeCategory)))
        {
            if (card.IsUsed(c)) continue;
            int s = YahtzeeScoring.Score(c, dice);
            if (s > bestScore)
            {
                bestScore = s;
                best = c;
            }
        }

        return best;
    }

    void SetAllPickables(bool on)
    {
        foreach (YahtzeeCategory c in System.Enum.GetValues(typeof(YahtzeeCategory)))
            ui.SetRowState(c, playerCard?.scores[(int)c], npcCard?.scores[(int)c], on);
    }

    void RefreshRowsPickable(YahtzeeScoreCard card, bool canPick)
    {
        foreach (YahtzeeCategory c in System.Enum.GetValues(typeof(YahtzeeCategory)))
        {
            bool pickable = canPick && !card.IsUsed(c);
            ui.SetRowState(c, card.scores[(int)c], npcCard?.scores[(int)c], pickable);
        }
    }

    void RefreshRowsNpc(YahtzeeScoreCard card)
    {
        foreach (YahtzeeCategory c in System.Enum.GetValues(typeof(YahtzeeCategory)))
        {
            ui.SetRowState(c, playerCard.scores[(int)c], card.scores[(int)c], !playerCard.IsUsed(c));
        }
    }

    IEnumerator ExitMatch()
    {
        // cierra UI
        ui.SetOpen(false);

        // regresar cámara
        if (playerCam != null)
        {
            yield return MoveCamera(playerCam.transform, camPosBefore, (camPosBefore + playerCam.transform.forward), camMoveTime, restoreRot: camRotBefore);
        }

        // restore scripts
        foreach (var mb in disableWhilePlaying)
            if (mb != null) mb.enabled = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (hud != null) hud.SetHudVisible(true);

        inMatch = false;
    }

    IEnumerator MoveCamera(Transform cam, Vector3 targetPos, Vector3 lookAt, float time, Quaternion? restoreRot = null)
    {
        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;

        Quaternion targetRot = restoreRot ?? Quaternion.LookRotation((lookAt - targetPos).normalized, Vector3.up);

        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / time);
            cam.position = Vector3.Lerp(startPos, targetPos, k);
            cam.rotation = Quaternion.Slerp(startRot, targetRot, k);
            yield return null;
        }

        cam.position = targetPos;
        cam.rotation = targetRot;
    }
}