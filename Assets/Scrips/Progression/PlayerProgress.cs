using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>เงิน 3 ประเภท (Currency 3 type)</summary>
public enum CurrencyType
{
    Coin,       // ตียุงทั่วไป
    Gem,        // ตียุงทอง (ยุงพิเศษ)
    BossToken   // ฆ่าบอส
}

/// <summary>
/// ข้อมูลผู้เล่นที่เก็บข้ามรอบเล่นและข้ามฉาก (บันทึกด้วย PlayerPrefs)
/// - เงิน 3 ประเภท      : Dictionary&lt;CurrencyType, int&gt;
/// - XP / เลเวล         : หาเลเวลจาก XP ด้วย Binary Search
/// - เลเวลของอัปเกรด     : Dictionary&lt;string, int&gt;
/// เป็น static class จึงเรียกใช้ได้ทุกฉากโดยไม่ต้องวางใน Scene
/// </summary>
public static class PlayerProgress
{
    private const string Prefix = "SM_";

    private static readonly Dictionary<CurrencyType, int> currencies = new Dictionary<CurrencyType, int>();
    private static readonly Dictionary<string, int> upgradeLevels = new Dictionary<string, int>();
    private static int totalXP;
    private static bool loaded;

    /// <summary>XP สะสมที่ต้องมีเพื่อถึงแต่ละเลเวล (index 0 = LV1) ต้องเรียงจากน้อยไปมาก</summary>
    public static readonly int[] LevelXP = { 0, 10, 25, 45, 70, 100, 140, 190, 250, 320 };

    public static int MaxLevel => LevelXP.Length;

    /// <summary>ยิงทุกครั้งที่เงิน / XP / อัปเกรดเปลี่ยน (ให้ UI อัปเดต)</summary>
    public static event Action Changed;

    /// <summary>ยิงเมื่อเลเวลขึ้น ส่งเลเวลใหม่มาด้วย</summary>
    public static event Action<int> LeveledUp;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        loaded = false;
        currencies.Clear();
        upgradeLevels.Clear();
        totalXP = 0;
        Changed = null;
        LeveledUp = null;
    }

    // ---------------- Save / Load ----------------

    private static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;

        foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
            currencies[type] = PlayerPrefs.GetInt(Prefix + "cur_" + type, 0);

        totalXP = PlayerPrefs.GetInt(Prefix + "xp", 0);

        foreach (UpgradeDefinition def in UpgradeCatalog.All)
            upgradeLevels[def.id] = PlayerPrefs.GetInt(Prefix + "upg_" + def.id, 0);
    }

    public static void Save()
    {
        EnsureLoaded();

        foreach (KeyValuePair<CurrencyType, int> pair in currencies)
            PlayerPrefs.SetInt(Prefix + "cur_" + pair.Key, pair.Value);

        PlayerPrefs.SetInt(Prefix + "xp", totalXP);

        foreach (KeyValuePair<string, int> pair in upgradeLevels)
            PlayerPrefs.SetInt(Prefix + "upg_" + pair.Key, pair.Value);

        PlayerPrefs.Save();
    }

    /// <summary>ล้างข้อมูลทั้งหมด (ใช้ตอนทดสอบ)</summary>
    public static void ResetAll()
    {
        EnsureLoaded();

        foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
        {
            currencies[type] = 0;
            PlayerPrefs.DeleteKey(Prefix + "cur_" + type);
        }

        totalXP = 0;
        PlayerPrefs.DeleteKey(Prefix + "xp");

        foreach (UpgradeDefinition def in UpgradeCatalog.All)
        {
            upgradeLevels[def.id] = 0;
            PlayerPrefs.DeleteKey(Prefix + "upg_" + def.id);
        }

        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    // ---------------- Currency ----------------

    public static int Get(CurrencyType type)
    {
        EnsureLoaded();
        return currencies.TryGetValue(type, out int value) ? value : 0;
    }

    public static void Add(CurrencyType type, int amount)
    {
        if (amount <= 0) return;
        EnsureLoaded();
        currencies[type] = Get(type) + amount;
        Changed?.Invoke();
    }

    public static bool CanAfford(CurrencyType type, int cost)
    {
        return cost >= 0 && Get(type) >= cost;
    }

    public static bool Spend(CurrencyType type, int cost)
    {
        if (!CanAfford(type, cost)) return false;
        currencies[type] -= cost;
        Changed?.Invoke();
        return true;
    }

    // ---------------- XP / Level ----------------

    public static int TotalXP
    {
        get
        {
            EnsureLoaded();
            return totalXP;
        }
    }

    public static int Level => LevelFromXP(TotalXP);
    public static bool IsMaxLevel => Level >= MaxLevel;

    /// <summary>เพิ่ม XP คืนค่าจำนวนเลเวลที่ขึ้น</summary>
    public static int AddXP(int amount)
    {
        if (amount <= 0) return 0;
        EnsureLoaded();

        int before = Level;
        totalXP += amount;
        int after = Level;

        if (after > before) LeveledUp?.Invoke(after);
        Changed?.Invoke();
        return after - before;
    }

    /// <summary>
    /// Binary Search: หา index สุดท้ายที่ LevelXP[index] &lt;= xp
    /// ทำงาน O(log n) แทนการไล่ทีละช่อง
    /// </summary>
    public static int LevelFromXP(int xp)
    {
        int low = 0;
        int high = LevelXP.Length - 1;
        int answer = 0;

        while (low <= high)
        {
            int mid = (low + high) / 2;

            if (LevelXP[mid] <= xp)
            {
                answer = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return answer + 1; // index 0 = LV1
    }

    public static int XPForLevel(int level)
    {
        return LevelXP[Mathf.Clamp(level - 1, 0, LevelXP.Length - 1)];
    }

    /// <summary>ความคืบหน้าไปเลเวลถัดไป 0..1</summary>
    public static float LevelProgress01
    {
        get
        {
            if (IsMaxLevel) return 1f;
            int current = XPForLevel(Level);
            int next = XPForLevel(Level + 1);
            return Mathf.Clamp01((TotalXP - current) / (float)(next - current));
        }
    }

    public static string XPText
    {
        get
        {
            if (IsMaxLevel) return "MAX";
            return $"{TotalXP - XPForLevel(Level)}/{XPForLevel(Level + 1) - XPForLevel(Level)} XP";
        }
    }

    // ---------------- Upgrades ----------------

    public static int GetUpgradeLevel(string id)
    {
        EnsureLoaded();
        return upgradeLevels.TryGetValue(id, out int level) ? level : 0;
    }

    public static bool TryBuyUpgrade(UpgradeDefinition def)
    {
        if (def == null) return false;

        int level = GetUpgradeLevel(def.id);
        if (level >= def.maxLevel) return false;

        int cost = def.CostForNext(level);
        if (!Spend(def.currency, cost)) return false;

        upgradeLevels[def.id] = level + 1;
        Save();
        Changed?.Invoke();

        Debug.Log($"[Upgrade] {def.displayName} -> LV {level + 1}");
        return true;
    }
}
