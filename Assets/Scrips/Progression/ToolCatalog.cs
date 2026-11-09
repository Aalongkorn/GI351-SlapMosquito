using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>ข้อมูลอุปกรณ์ 1 ชิ้น พร้อมเลเวลที่ต้องมีเพื่อปลดล็อก</summary>
[Serializable]
public class ToolDefinition
{
    public EquipmentManager.ToolType tool;
    public string displayName;
    public string description;
    public int requiredLevel;
    public float cooldown;
    public KeyCode key;

    public ToolDefinition(EquipmentManager.ToolType tool, string displayName, string description,
        int requiredLevel, float cooldown, KeyCode key)
    {
        this.tool = tool;
        this.displayName = displayName;
        this.description = description;
        this.requiredLevel = requiredLevel;
        this.cooldown = cooldown;
        this.key = key;
    }
}

/// <summary>
/// รายการอุปกรณ์ทั้งหมด (Unlock item with LV)
/// เรียงตาม requiredLevel จากน้อยไปมาก
/// </summary>
public static class ToolCatalog
{
    public static readonly List<ToolDefinition> All = new List<ToolDefinition>
    {
        new ToolDefinition(EquipmentManager.ToolType.FlySwatter, "Fly Swatter", "Slap one mosquito.", 1, 0f, KeyCode.Alpha1),
        new ToolDefinition(EquipmentManager.ToolType.RepairTape, "Repair Tape", "Fix cracked glass.", 1, 0f, KeyCode.Alpha2),
        new ToolDefinition(EquipmentManager.ToolType.ElectricRacket, "Electric Racket", "Zap every mosquito in a small area.", 3, 1.5f, KeyCode.Alpha3),
        new ToolDefinition(EquipmentManager.ToolType.BugSpray, "Bug Spray", "Slow mosquitoes in a wide area.", 5, 8f, KeyCode.Alpha4),
        new ToolDefinition(EquipmentManager.ToolType.MosquitoCoil, "Mosquito Coil", "Protect the glass for a few seconds.", 8, 30f, KeyCode.Alpha5)
    };

    public static ToolDefinition Get(EquipmentManager.ToolType tool)
    {
        foreach (ToolDefinition def in All)
        {
            if (def.tool == tool) return def;
        }
        return null;
    }

    public static bool IsUnlocked(EquipmentManager.ToolType tool)
    {
        ToolDefinition def = Get(tool);
        return def == null || PlayerProgress.Level >= def.requiredLevel;
    }

    /// <summary>อุปกรณ์ที่เพิ่งปลดล็อกเมื่อขึ้นจาก oldLevel เป็น newLevel</summary>
    public static List<ToolDefinition> UnlockedBetween(int oldLevel, int newLevel)
    {
        var result = new List<ToolDefinition>();
        foreach (ToolDefinition def in All)
        {
            if (def.requiredLevel > oldLevel && def.requiredLevel <= newLevel) result.Add(def);
        }
        return result;
    }
}
