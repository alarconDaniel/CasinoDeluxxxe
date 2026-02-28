using UnityEngine;

public class SlotMachine : CasinoMachineBase
{
    [System.Serializable]
    public class SymbolWeight
    {
        public string symbolName;
        public int weight = 1;
        public int payoutMultiplier = 2; // si salen 3 iguales
    }

    public SymbolWeight[] symbols;

    protected override void PlayOnce()
    {
        var gm = CasinoGameManager.Instance;
        int bet = gm.bet;

        if (!gm.TrySpendCoins(bet))
        {
            gm.SetMessage("No tienes monedas suficientes para apostar.");
            return;
        }

        // Categórica (pesos) :contentReference[oaicite:12]{index=12}
        var a = PickSymbol();
        var b = PickSymbol();
        var c = PickSymbol();

        int win = 0;

        if (a.symbolName == b.symbolName && b.symbolName == c.symbolName)
        {
            win = bet * a.payoutMultiplier;
            gm.AddCoins(win);
            gm.SetMessage($"{machineName}: [{a.symbolName}] [{b.symbolName}] [{c.symbolName}]  ¡GANASTE +{win}!");
        }
        else
        {
            gm.SetMessage($"{machineName}: [{a.symbolName}] [{b.symbolName}] [{c.symbolName}]  No ganaste.");
        }

        // XP determinístico por jugar (puede ser = apuesta)
        gm.AddXP(bet);
    }

    private SymbolWeight PickSymbol()
    {
        int total = 0;
        foreach (var s in symbols) total += Mathf.Max(0, s.weight);

        int r = Random.Range(0, total);
        int acc = 0;

        foreach (var s in symbols)
        {
            acc += Mathf.Max(0, s.weight);
            if (r < acc) return s;
        }
        return symbols[0];
    }
}