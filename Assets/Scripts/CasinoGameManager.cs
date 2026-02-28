using System;
using UnityEngine;

public class CasinoGameManager : MonoBehaviour
{
    public static CasinoGameManager Instance { get; private set; }

    [Header("Estado (variables)")]
    public int coins = 100;        // C (bienvenida) :contentReference[oaicite:6]{index=6}
    public int level = 1;          // L
    public int xp = 0;             // XP
    public int bet = 10;           // B
    public float sessionSeconds;   // T (en segundos, mostramos en minutos)

    [Header("Progresión determinística (XP acumulada)")]
    // L1: 0, L2: 100, L3: 250, L4: 450, L5: 700 (del ejemplo) :contentReference[oaicite:7]{index=7}
    public int[] levelThresholds = { 0, 100, 250, 450, 700 };
    public int[] levelRewardsCoins = { 100, 150, 200, 250, 300 }; // ejemplo del doc :contentReference[oaicite:8]{index=8}

    public string lastMessage = "";

    public event Action OnStateChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        sessionSeconds += Time.deltaTime;

        // Ajuste de apuesta rápido (B)
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus)) SetBet(bet - 5);
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus)) SetBet(bet + 5);
    }

    public void SetBet(int newBet)
    {
        bet = Mathf.Clamp(newBet, 1, 999);
        Notify();
    }

    public bool TrySpendCoins(int amount)
    {
        if (coins < amount) return false;
        coins -= amount;
        Notify();
        return true;
    }

    public void AddCoins(int amount)
    {
        coins += Mathf.Max(0, amount);
        Notify();
    }

    public void AddXP(int amount)
    {
        xp += Mathf.Max(0, amount);
        CheckLevelUp();
        Notify();
    }

    private void CheckLevelUp()
    {
        // Determinístico: si supera umbral, sube y aplica recompensa fija :contentReference[oaicite:9]{index=9}
        while (level < levelThresholds.Length && xp >= levelThresholds[level])
        {
            level++;
            int rewardIndex = Mathf.Clamp(level - 1, 0, levelRewardsCoins.Length - 1);
            AddCoins(levelRewardsCoins[rewardIndex]);
            SetMessage($"¡Subiste a nivel {level}! Recompensa: +{levelRewardsCoins[rewardIndex]} monedas.");
        }
    }

    public void SetMessage(string msg)
    {
        lastMessage = msg;
        Notify();
    }

    private void Notify() => OnStateChanged?.Invoke();
}