using UnityEngine;

public class RouletteMachine : CasinoMachineBase
{
    [Tooltip("Si está activo, paga mejor (premium).")]
    public bool premium = false;

    protected override void PlayOnce()
    {
        var gm = CasinoGameManager.Instance;
        int bet = gm.bet;

        if (!gm.TrySpendCoins(bet))
        {
            gm.SetMessage("No tienes monedas suficientes para apostar.");
            return;
        }

        // Uniforme discreta 0..36 :contentReference[oaicite:14]{index=14}
        int result = Random.Range(0, 37);

        // MVP: apuesta fija a PAR (excluyendo 0)
        bool win = (result != 0) && (result % 2 == 0);

        if (win)
        {
            int payout = premium ? (bet * 3) : (bet * 2); // premium paga más
            gm.AddCoins(payout);
            gm.SetMessage($"{machineName}: cayó {result}. ¡GANASTE +{payout}!");
        }
        else
        {
            gm.SetMessage($"{machineName}: cayó {result}. No ganaste.");
        }

        gm.AddXP(bet);
    }
}