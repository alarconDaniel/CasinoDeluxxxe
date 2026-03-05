using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class CasinoStatsManager : MonoBehaviour
{
    public static CasinoStatsManager Instance { get; private set; }

    [Serializable]
    public struct SymbolCount
    {
        public string symbol;
        public int count;
    }

    [Serializable]
    public class StatsData
    {
        public int slotsSpins;
        public int rouletteSpins;

        public int totalWins;
        public int totalLosses;

        // Dinero (monedas)
        public int totalBetCoins;     // “gastado en apuestas” (bruto)
        public int totalPayoutCoins;  // “pagado por premios” (bruto)

        // Tiempo de sesión (T)
        public float sessionSeconds;

        // Conteo por símbolo (slots) y si quieres, por número en ruleta (roulette_17)
        public List<SymbolCount> symbolCounts = new List<SymbolCount>();


        public int yahtzeeMatches;
    }

    [Header("Persistencia")]
    [SerializeField] bool persistBetweenScenes = true;
    [SerializeField] string autosaveFileName = "casino_stats.json";

    private StatsData data = new StatsData();
    private Dictionary<string, int> symbolIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public StatsData Data => data;

    public int NetCoins => data.totalPayoutCoins - data.totalBetCoins; // positivo=ganó neto, negativo=perdió neto

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (persistBetweenScenes) DontDestroyOnLoad(gameObject);

        Load(); // intenta cargar stats previos
    }

    void Update()
    {
        // Tiempo de sesión (si abres menú y pausas con Time.timeScale=0, esto se congela)
        data.sessionSeconds += Time.deltaTime;
    }

    // ---------- RECORDS ----------
    public void RecordSlotsSpin(int bet, string s1, string s2, string s3, bool won, int payout)
    {
        data.slotsSpins++;
        data.totalBetCoins += Mathf.Max(0, bet);
        if (won) data.totalPayoutCoins += Mathf.Max(0, payout);

        RecordSymbol(s1);
        RecordSymbol(s2);
        RecordSymbol(s3);

        if (won) data.totalWins++;
        else data.totalLosses++;
    }

    public void RecordRouletteSpin(int bet, int landedNumber, bool won, int payout)
    {
        data.rouletteSpins++;
        data.totalBetCoins += Mathf.Max(0, bet);
        if (won) data.totalPayoutCoins += Mathf.Max(0, payout);

        // Si quieres contar números:
        if (landedNumber >= 0)
            RecordSymbol("roulette_" + landedNumber);

        if (won) data.totalWins++;
        else data.totalLosses++;
    }

    private void RecordSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol)) return;

        if (!symbolIndex.TryGetValue(symbol, out int idx))
        {
            idx = data.symbolCounts.Count;
            symbolIndex[symbol] = idx;
            data.symbolCounts.Add(new SymbolCount { symbol = symbol, count = 0 });
        }

        var sc = data.symbolCounts[idx];
        sc.count++;
        data.symbolCounts[idx] = sc;
    }

    // ---------- CSV EXPORT ----------
    public string ExportCsv()
    {
        string dir = Application.persistentDataPath;
        string fileName = $"casino_stats_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        string path = Path.Combine(dir, fileName);

        float rtp = (data.totalBetCoins > 0) ? (float)data.totalPayoutCoins / data.totalBetCoins : 0f;

        char sep = ';';

        var sb = new StringBuilder();
        sb.AppendLine($"metric{sep}value");
        sb.AppendLine($"session_seconds{sep}{data.sessionSeconds:0.00}");
        sb.AppendLine($"slots_spins{sep}{data.slotsSpins}");
        sb.AppendLine($"roulette_spins{sep}{data.rouletteSpins}");
        sb.AppendLine($"yahtzee_matches{sep}{data.yahtzeeMatches}");
        sb.AppendLine($"wins{sep}{data.totalWins}");
        sb.AppendLine($"losses{sep}{data.totalLosses}");
        sb.AppendLine($"total_bets{sep}{data.totalBetCoins}");
        sb.AppendLine($"total_payouts{sep}{data.totalPayoutCoins}");
        sb.AppendLine($"net{sep}{NetCoins}");
        sb.AppendLine($"rtp{sep}{rtp:0.000}");
        sb.AppendLine();
        sb.AppendLine($"symbol{sep}count");
        foreach (var sc in data.symbolCounts)
            sb.AppendLine($"{Escape(sc.symbol, sep)}{sep}{sc.count}");

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        Debug.Log("CSV exportado en: " + path);
        return path;
    }

    private static string Escape(string s, char sep)
    {
        if (s.Contains(sep) || s.Contains("\"") || s.Contains("\n"))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    // ---------- SAVE/LOAD ----------
    public void Save()
    {
        string path = Path.Combine(Application.persistentDataPath, autosaveFileName);
        File.WriteAllText(path, JsonUtility.ToJson(data, true), Encoding.UTF8);
    }

    public void Load()
    {
        symbolIndex.Clear();

        string path = Path.Combine(Application.persistentDataPath, autosaveFileName);
        if (File.Exists(path))
        {
            try
            {
                var loaded = JsonUtility.FromJson<StatsData>(File.ReadAllText(path, Encoding.UTF8));
                data = loaded ?? new StatsData();
            }
            catch
            {
                data = new StatsData();
            }
        }
        else data = new StatsData();

        // reconstruye índice
        for (int i = 0; i < data.symbolCounts.Count; i++)
        {
            var sym = data.symbolCounts[i].symbol;
            if (!string.IsNullOrWhiteSpace(sym) && !symbolIndex.ContainsKey(sym))
                symbolIndex[sym] = i;
        }
    }

public void RecordYahtzeeMatch(int bet, bool won, int payout)
{
    data.yahtzeeMatches++;
    data.totalBetCoins += Mathf.Max(0, bet);
    if (won) data.totalPayoutCoins += Mathf.Max(0, payout);

    if (won) data.totalWins++;
    else data.totalLosses++;
}

    void OnApplicationQuit() => Save();



}