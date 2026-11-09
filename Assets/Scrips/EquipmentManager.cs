using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public enum ToolType
    {
        FlySwatter,
        RepairTape,
        ElectricRacket, // ปลดล็อก LV 3
        BugSpray,       // ปลดล็อก LV 5
        MosquitoCoil    // ปลดล็อก LV 8
    }

    public static EquipmentManager Instance { get; private set; }

    [Header("Equipment Settings")]
    [SerializeField]
    private ToolType startingTool = ToolType.FlySwatter;

    [Header("Equipment UI")]
    [SerializeField]
    private EquipmentSlotUI flySwatterSlot;

    [SerializeField]
    private EquipmentSlotUI repairTapeSlot;

    [Header("Repair Settings")]
    [Min(1)]
    public int tapeRepairAmount = 1;

    public static ToolType SelectedTool { get; private set; }
        = ToolType.FlySwatter;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        SelectedTool = startingTool;
    }

    private void Start()
    {
        UpdateEquipmentUI();
    }

    private void Update()
    {
        if (BreakableGlass.InputBlocked)
        {
            return;
        }

        // ปุ่ม 1 = ไม้ตียุง
        if (Input.GetKeyDown(KeyCode.Alpha1) ||
            Input.GetKeyDown(KeyCode.Keypad1))
        {
            SelectTool(ToolType.FlySwatter);
        }

        // ปุ่ม 2 = เทปซ่อมกระจก
        if (Input.GetKeyDown(KeyCode.Alpha2) ||
            Input.GetKeyDown(KeyCode.Keypad2))
        {
            SelectTool(ToolType.RepairTape);
        }

        // ปุ่ม 3-5 = อุปกรณ์ที่ปลดล็อกด้วยเลเวล (ดู ToolCatalog)
        foreach (ToolDefinition def in ToolCatalog.All)
        {
            if (def.tool == ToolType.FlySwatter || def.tool == ToolType.RepairTape)
                continue;

            if (Input.GetKeyDown(def.key))
                SelectTool(def.tool);
        }
    }

    public void SelectTool(ToolType tool)
    {
        // Unlock item with LV: ยังไม่ถึงเลเวล เลือกไม่ได้
        if (!ToolCatalog.IsUnlocked(tool))
        {
            ToolDefinition locked = ToolCatalog.Get(tool);
            ProgressHUD.Toast(locked.displayName + " unlocks at LV " + locked.requiredLevel);
            return;
        }

        SelectedTool = tool;

        UpdateEquipmentUI();

        ToolDefinition def = ToolCatalog.Get(tool);
        Debug.Log("เลือกอุปกรณ์: " + (def != null ? def.displayName : tool.ToString()));
    }

    private void UpdateEquipmentUI()
    {
        // ช่องไม้ตียุง
        if (flySwatterSlot != null)
        {
            flySwatterSlot.SetSelected(
                SelectedTool == ToolType.FlySwatter
            );
        }

        // ช่องเทปซ่อมกระจก
        if (repairTapeSlot != null)
        {
            repairTapeSlot.SetSelected(
                SelectedTool == ToolType.RepairTape
            );
        }
    }

    public static int GetRepairAmount()
    {
        if (Instance == null)
        {
            return 1;
        }

        // Upgrade: Strong Tape
        return Mathf.Max(
            1,
            UpgradeStats.TapeRepair(Instance.tapeRepairAmount)
        );
    }
}