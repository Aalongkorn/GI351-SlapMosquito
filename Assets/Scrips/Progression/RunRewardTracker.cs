using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>ผลสรุปของการเล่น 1 รอบ</summary>
public struct RunResult
{
    public int score;
    public int xpGained;
    public int levelBefore;
    public int levelAfter;
    public int coins;
    public int gems;
    public int bossTokens;
}

/// <summary>
/// ให้เงินระหว่างเล่น และสรุป XP ตอน Game Over (อยู่ในฉาก InGame)
/// ฟัง event ที่เกมมีอยู่แล้ว: Mosquito.OnMosquitoKilled, BossMosquito.AnyDefeated
/// </summary>
public class RunRewardTracker : MonoBehaviour
{
    public static RunRewardTracker Instance { get; private set; }

    [Header("Rewards")]
    public int coinPerMosquito = 1;
    public int coinPerSpecial = 3;
    public int gemPerSpecial = 1;
    public int tokenPerBoss = 1;

    [Header("XP")]
    [Tooltip("ทุก ๆ กี่คะแนน ได้ 1 XP")]
    public int scorePerXP = 100;

    public static event Action<RunResult> RunCompleted;

    private readonly Dictionary<CurrencyType, int> earned = new Dictionary<CurrencyType, int>();
    private bool completed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        RunCompleted = null;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        Mosquito.OnMosquitoKilled += HandleMosquitoKilled;
        BossMosquito.AnyDefeated += HandleBossDefeated;
    }

    private void OnDisable()
    {
        Mosquito.OnMosquitoKilled -= HandleMosquitoKilled;
        BossMosquito.AnyDefeated -= HandleBossDefeated;

        // ออกจากฉากกลางคัน ก็ยังเก็บเงินที่ได้ไว้
        if (!completed) PlayerProgress.Save();
    }

    private void HandleMosquitoKilled(Mosquito mosquito)
    {
        if (mosquito == null) return;

        if (mosquito.isSpecial)
        {
            Give(CurrencyType.Coin, coinPerSpecial);
            Give(CurrencyType.Gem, gemPerSpecial);
        }
        else
        {
            Give(CurrencyType.Coin, coinPerMosquito);
        }
    }

    private void HandleBossDefeated(BossMosquito boss)
    {
        Give(CurrencyType.BossToken, tokenPerBoss);
        ProgressHUD.Toast($"+{tokenPerBoss} Boss Token!");
    }

    private void Give(CurrencyType type, int amount)
    {
        if (amount <= 0) return;
        PlayerProgress.Add(type, amount);
        earned.TryGetValue(type, out int current);
        earned[type] = current + amount;
    }

    public int Earned(CurrencyType type)
    {
        return earned.TryGetValue(type, out int value) ? value : 0;
    }

    /// <summary>เรียกจาก GameOverManager ตอนจบรอบ: แปลงคะแนนเป็น XP แล้วบันทึก</summary>
    public static RunResult CompleteRun(int score)
    {
        int perXP = Instance != null ? Mathf.Max(1, Instance.scorePerXP) : 100;

        var result = new RunResult
        {
            score = score,
            levelBefore = PlayerProgress.Level,
            xpGained = Mathf.Max(0, score / perXP)
        };

        PlayerProgress.AddXP(result.xpGained);
        result.levelAfter = PlayerProgress.Level;

        if (Instance != null)
        {
            Instance.completed = true;
            result.coins = Instance.Earned(CurrencyType.Coin);
            result.gems = Instance.Earned(CurrencyType.Gem);
            result.bossTokens = Instance.Earned(CurrencyType.BossToken);
        }

        PlayerProgress.Save();
        RunCompleted?.Invoke(result);
        return result;
    }
}
