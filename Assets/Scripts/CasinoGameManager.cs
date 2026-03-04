using System;
using System.Collections;
using UnityEngine;

public class CasinoGameManager : MonoBehaviour
{
    public static CasinoGameManager Instance { get; private set; }

    [Header("Estado (variables)")]
    public int coins = 100;
    public int level = 1;
    public int xp = 0;
    public int bet = 10;
    public float sessionSeconds;

    [Header("Progresión determinística (XP acumulada)")]
    public int[] levelThresholds = { 0, 100, 250, 450, 700 };
    public int[] levelRewardsCoins = { 100, 150, 200, 250, 300 };

    public string lastMessage = "";

    public event Action OnStateChanged;

    Coroutine clearMsgRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        sessionSeconds += Time.deltaTime;

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
        while (level < levelThresholds.Length && xp >= levelThresholds[level])
        {
            level++;
            int rewardIndex = Mathf.Clamp(level - 1, 0, levelRewardsCoins.Length - 1);
            AddCoins(levelRewardsCoins[rewardIndex]);
            SetMessage($"¡Nivel {level}! +{levelRewardsCoins[rewardIndex]} monedas");
        }
    }

    public void SetMessage(string msg)
    {
        lastMessage = msg;
        Notify();
    }

    // ✅ NUEVO: mensaje por X segundos y luego se limpia
    public void SetMessageTimed(string msg, float seconds)
    {
        lastMessage = msg;
        Notify();

        if (clearMsgRoutine != null) StopCoroutine(clearMsgRoutine);
        clearMsgRoutine = StartCoroutine(ClearMessageAfter(seconds, msg));
    }

    IEnumerator ClearMessageAfter(float seconds, string sameMsg)
    {
        yield return new WaitForSeconds(seconds);
        if (lastMessage == sameMsg)
        {
            lastMessage = "";
            Notify();
        }
        clearMsgRoutine = null;
    }

    private void Notify() => OnStateChanged?.Invoke();
}