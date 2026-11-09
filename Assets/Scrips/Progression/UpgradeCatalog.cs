using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>ข้อมูลอัปเกรด 1 รายการ</summary>
[Serializable]
public class UpgradeDefinition
{
    public string id;
    public string displayName;
    public string description;
    public int maxLevel;
    public CurrencyType currency;

    [Tooltip("ราคาขั้นแรก")]
    public int baseCost;
    [Tooltip("ราคาเพิ่มขึ้นทีละเท่าไรต่อขั้น")]
    public int costStep;

    [Tooltip("ค่าที่เพิ่มต่อ 1 ขั้น")]
    public float valuePerLevel;
    public string valueFormat = "+{0}";

    public UpgradeDefinition(string id, string displayName, string description, int maxLevel,
        CurrencyType currency, int baseCost, int costStep, float valuePerLevel, string valueFormat)
    {
        this.id = id;
        this.displayName = displayName;
        this.description = description;
        this.maxLevel = maxLevel;
        this.currency = currency;
        this.baseCost = baseCost;
        this.costStep = costStep;
        this.valuePerLevel = valuePerLevel;
        this.valueFormat = valueFormat;
    }

    public int CostForNext(int currentLevel) => baseCost + costStep * currentLevel;
    public float TotalValue(int level) => valuePerLevel * level;
    public string FormatValue(int level) => string.Format(valueFormat, TotalValue(level));
}

/// <summary>
/// รายการอัปเกรดทั้งหมดในเกม (Upgrade)
/// อยากเพิ่ม/แก้ราคา แก้ที่ list นี้ที่เดียว
/// </summary>
public static class UpgradeCatalog
{
    public const string GlassHP = "glass_hp";
    public const string TapePower = "tape_power";
    public const string ComboTime = "combo_time";
    public const string BossDamage = "boss_damage";
    public const string SlotLuck = "slot_luck";

    public static readonly List<UpgradeDefinition> All = new List<UpgradeDefinition>
    {
        new UpgradeDefinition(GlassHP, "Tough Glass", "Glass max HP", 3, CurrencyType.Coin, 20, 20, 1f, "+{0} HP"),
        new UpgradeDefinition(TapePower, "Strong Tape", "HP restored per repair", 2, CurrencyType.Coin, 30, 30, 1f, "+{0} HP"),
        new UpgradeDefinition(ComboTime, "Combo Focus", "Combo time limit", 3, CurrencyType.Gem, 2, 2, 0.3f, "+{0:0.0}s"),
        new UpgradeDefinition(SlotLuck, "Lucky Lever", "Slot machine win rate", 3, CurrencyType.Gem, 3, 3, 5f, "+{0}%"),
        new UpgradeDefinition(BossDamage, "Heavy Swatter", "Damage per hit on bosses", 2, CurrencyType.BossToken, 1, 1, 1f, "+{0}")
    };

    private static Dictionary<string, UpgradeDefinition> byId;

    public static UpgradeDefinition Get(string id)
    {
        if (byId == null)
        {
            byId = new Dictionary<string, UpgradeDefinition>();
            foreach (UpgradeDefinition def in All) byId[def.id] = def;
        }
        return byId.TryGetValue(id, out UpgradeDefinition result) ? result : null;
    }
}

/// <summary>
/// ค่าหลังอัปเกรด สคริปต์เดิมเรียกใช้ตรงนี้
/// เช่น BreakableGlass: maxHP = UpgradeStats.GlassMaxHP(maxHP);
/// </summary>
public static class UpgradeStats
{
    private static float Bonus(string id)
    {
        UpgradeDefinition def = UpgradeCatalog.Get(id);
        return def == null ? 0f : def.TotalValue(PlayerProgress.GetUpgradeLevel(id));
    }

    public static int GlassMaxHP(int baseHP) => baseHP + Mathf.RoundToInt(Bonus(UpgradeCatalog.GlassHP));
    public static int TapeRepair(int baseAmount) => baseAmount + Mathf.RoundToInt(Bonus(UpgradeCatalog.TapePower));
    public static float ComboTime(float baseTime) => baseTime + Bonus(UpgradeCatalog.ComboTime);
    public static float SlotWinRate(float baseRate) => Mathf.Min(100f, baseRate + Bonus(UpgradeCatalog.SlotLuck));
    public static int BossDamage => 1 + Mathf.RoundToInt(Bonus(UpgradeCatalog.BossDamage));
}
