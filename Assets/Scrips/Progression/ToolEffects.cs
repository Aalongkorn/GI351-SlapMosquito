using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// การทำงานของอุปกรณ์ที่ปลดล็อกด้วยเลเวล (ปุ่ม 3, 4, 5)
/// ไม้ตียุงกับเทปยังใช้ระบบเดิมใน Mosquito / BreakableGlass
/// </summary>
public class ToolEffects : MonoBehaviour
{
    public static ToolEffects Instance { get; private set; }

    [Header("Electric Racket")]
    public float racketRadius = 1.3f;
    public Color racketColor = new Color(0.4f, 0.8f, 1f);

    [Header("Bug Spray")]
    public float sprayRadius = 2.5f;
    [Range(0.1f, 1f)] public float spraySpeedMultiplier = 0.4f;
    public float sprayDuration = 4f;
    public Color sprayColor = new Color(0.5f, 1f, 0.5f);

    [Header("Mosquito Coil")]
    public float coilDuration = 6f;

    // เก็บเวลาที่แต่ละอุปกรณ์จะพร้อมใช้อีกครั้ง
    private readonly Dictionary<EquipmentManager.ToolType, float> readyTime = new Dictionary<EquipmentManager.ToolType, float>();
    private bool coilActive;
    private Material lineMaterial;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        EquipmentManager.ToolType tool = EquipmentManager.SelectedTool;
        if (tool == EquipmentManager.ToolType.FlySwatter || tool == EquipmentManager.ToolType.RepairTape) return;

        if (BreakableGlass.InputBlocked) return;
        if (SlotEventManager.Instance != null && SlotEventManager.Instance.IsEventRunning) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (!ToolCatalog.IsUnlocked(tool)) return;

        float remaining = CooldownRemaining(tool);
        if (remaining > 0f)
        {
            ProgressHUD.Toast($"Cooling down... {remaining:0.0}s");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null) return;
        Vector2 point = cam.ScreenToWorldPoint(Input.mousePosition);

        bool used = false;
        switch (tool)
        {
            case EquipmentManager.ToolType.ElectricRacket: used = UseRacket(point); break;
            case EquipmentManager.ToolType.BugSpray: used = UseSpray(point); break;
            case EquipmentManager.ToolType.MosquitoCoil: used = UseCoil(); break;
        }

        if (used) StartCooldown(tool);
    }

    // ---------------- Cooldown ----------------

    public float CooldownRemaining(EquipmentManager.ToolType tool)
    {
        return readyTime.TryGetValue(tool, out float t) ? Mathf.Max(0f, t - Time.time) : 0f;
    }

    public float CooldownProgress01(EquipmentManager.ToolType tool)
    {
        ToolDefinition def = ToolCatalog.Get(tool);
        if (def == null || def.cooldown <= 0f) return 0f;
        return Mathf.Clamp01(CooldownRemaining(tool) / def.cooldown);
    }

    private void StartCooldown(EquipmentManager.ToolType tool)
    {
        ToolDefinition def = ToolCatalog.Get(tool);
        readyTime[tool] = Time.time + (def != null ? def.cooldown : 0f);
    }

    // ---------------- Tools ----------------

    private bool UseRacket(Vector2 point)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(point, racketRadius);
        var hitBosses = new HashSet<BossMosquito>(); // กันบอสโดนหลายครั้งจาก collider หลายอัน
        int kills = 0;

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;

            BossMosquito boss = hit.GetComponentInParent<BossMosquito>();
            if (boss != null)
            {
                if (!boss.IsDead && hitBosses.Add(boss)) boss.TakeHit();
                continue;
            }

            Mosquito mosquito = hit.GetComponentInParent<Mosquito>();
            if (mosquito != null && mosquito.KillByTool()) kills++;
        }

        SpawnRing(point, racketRadius, racketColor);
        if (kills >= 2) ProgressHUD.Toast($"ZAP x{kills}!");
        return true;
    }

    private bool UseSpray(Vector2 point)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(point, sprayRadius);
        var slowed = new HashSet<Mosquito>();

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            if (hit.GetComponentInParent<BossMosquito>() != null) continue;

            Mosquito mosquito = hit.GetComponentInParent<Mosquito>();
            if (mosquito != null && slowed.Add(mosquito))
                SlowEffect.Apply(mosquito, spraySpeedMultiplier, sprayDuration);
        }

        SpawnRing(point, sprayRadius, sprayColor);
        ProgressHUD.Toast($"Sprayed {slowed.Count} mosquitoes");
        return true;
    }

    private bool UseCoil()
    {
        if (coilActive) return false;
        StartCoroutine(CoilRoutine());

        // จุดยากันยุงแล้วกลับไปถือไม้ตียุง
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.SelectTool(EquipmentManager.ToolType.FlySwatter);
        return true;
    }

    private IEnumerator CoilRoutine()
    {
        coilActive = true;
        BreakableGlass.GlassProtected = true;
        ProgressHUD.Toast($"Glass protected for {coilDuration:0}s");

        yield return new WaitForSeconds(coilDuration);

        // ถ้ารางวัล slot ยังป้องกันกระจกอยู่ ไม่ต้องปิด
        bool slotStillProtecting = SlotEventManager.Instance != null && SlotEventManager.Instance.IsRewardActive;
        if (!slotStillProtecting) BreakableGlass.GlassProtected = false;

        coilActive = false;
        ProgressHUD.Toast("Coil burned out");
    }

    // ---------------- Visual ----------------

    private void SpawnRing(Vector2 center, float radius, Color color)
    {
        if (lineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) return;
            lineMaterial = new Material(shader);
        }

        var go = new GameObject("ToolRing");
        go.transform.position = center;
        var line = go.AddComponent<LineRenderer>();
        line.material = lineMaterial;
        line.useWorldSpace = false;
        line.loop = true;
        line.widthMultiplier = 0.08f;
        line.sortingOrder = 100;

        const int segments = 40;
        line.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius);
        }

        StartCoroutine(FadeRing(line, color));
    }

    private IEnumerator FadeRing(LineRenderer line, Color color)
    {
        const float duration = 0.35f;
        float t = 0f;
        while (t < duration && line != null)
        {
            t += Time.deltaTime;
            Color c = color;
            c.a = 1f - t / duration;
            line.startColor = c;
            line.endColor = c;
            line.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, t / duration);
            yield return null;
        }
        if (line != null) Destroy(line.gameObject);
    }
}
