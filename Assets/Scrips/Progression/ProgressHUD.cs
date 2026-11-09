using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// HUD ระหว่างเล่น (ฉาก InGame) สร้าง UI เองด้วยโค้ด
/// - เงิน 3 ประเภท + LV / XP (มุมขวาบน)
/// - แถบอุปกรณ์ปุ่ม 3-5 แสดงล็อก / คูลดาวน์ (ขวากลาง)
/// - ข้อความเด้ง (Toast)
/// - สรุปผลตอน Game Over + ปุ่มกลับเมนู (ไปร้านค้า)
/// </summary>
public class ProgressHUD : MonoBehaviour
{
    public static ProgressHUD Instance { get; private set; }

    public string menuSceneName = "MainMenu";

    private TMP_Text currencyText;
    private TMP_Text levelText;
    private Image xpFill;
    private TMP_Text toastText;
    private CanvasGroup toastGroup;
    private Coroutine toastRoutine;

    private GameObject resultPanel;
    private TMP_Text resultText;

    private class ToolSlot
    {
        public ToolDefinition def;
        public Image background;
        public Image cooldownFill;
        public TMP_Text label;
    }

    private readonly List<ToolSlot> toolSlots = new List<ToolSlot>();

    private static readonly Color SlotNormal = new Color(0f, 0f, 0f, 0.6f);
    private static readonly Color SlotSelected = new Color(0.25f, 0.55f, 0.35f, 0.9f);
    private static readonly Color SlotLocked = new Color(0.15f, 0.15f, 0.15f, 0.6f);

    /// <summary>แสดงข้อความสั้น ๆ กลางจอด้านบน เรียกได้จากทุกที่</summary>
    public static void Toast(string message)
    {
        if (Instance == null)
        {
            Debug.Log("[HUD] " + message);
            return;
        }
        Instance.ShowToast(message);
    }

    private void Awake()
    {
        Instance = this;
        Build();
    }

    private void OnEnable()
    {
        PlayerProgress.Changed += RefreshProgress;
        PlayerProgress.LeveledUp += HandleLevelUp;
        RunRewardTracker.RunCompleted += ShowResult;
    }

    private void OnDisable()
    {
        PlayerProgress.Changed -= RefreshProgress;
        PlayerProgress.LeveledUp -= HandleLevelUp;
        RunRewardTracker.RunCompleted -= ShowResult;
    }

    private void Start()
    {
        RefreshProgress();
    }

    private void Update()
    {
        RefreshToolSlots();
    }

    // ---------------- Build UI ----------------

    private void Build()
    {
        Canvas canvas = UIFactory.CreateCanvas("ProgressHUD Canvas", 450, transform);
        Transform root = canvas.transform;

        // มุมขวาบน: LV + เงิน
        Image box = UIFactory.Panel("ProgressBox", root, new Color(0f, 0f, 0f, 0.55f));
        box.raycastTarget = false;
        UIFactory.Place(box.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-360f, -150f), new Vector2(-20f, -20f));

        levelText = UIFactory.Text("Level", box.transform, "LV 1", 30);
        UIFactory.Place(levelText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -48f), new Vector2(-16f, -6f));
        levelText.color = UIFactory.Gold;

        Image xpBack = UIFactory.Panel("XPBar", box.transform, new Color(1f, 1f, 1f, 0.15f));
        xpBack.raycastTarget = false;
        UIFactory.Place(xpBack.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -64f), new Vector2(-16f, -52f));
        xpFill = UIFactory.Panel("Fill", xpBack.transform, UIFactory.Gold);
        xpFill.raycastTarget = false;
        UIFactory.Fill(xpFill.rectTransform);
        xpFill.rectTransform.pivot = new Vector2(0f, 0.5f);

        currencyText = UIFactory.Text("Currency", box.transform, "", 24, TextAlignmentOptions.TopLeft);
        UIFactory.Place(currencyText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(16f, 6f), new Vector2(-16f, -70f));

        // ขวากลาง: อุปกรณ์ปุ่ม 3-5
        RectTransform bar = UIFactory.Rect("ToolBar", root);
        UIFactory.Place(bar, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-280f, -170f), new Vector2(-20f, 170f));
        UIFactory.VerticalList(bar, 10f);

        foreach (ToolDefinition def in ToolCatalog.All)
        {
            if (def.tool == EquipmentManager.ToolType.FlySwatter || def.tool == EquipmentManager.ToolType.RepairTape)
                continue; // สองอันนี้มีช่องใน UI เดิมแล้ว

            var slot = new ToolSlot { def = def };
            slot.background = UIFactory.Panel(def.displayName, bar, SlotNormal);
            slot.background.raycastTarget = false;
            UIFactory.SetHeight(slot.background, 100f);

            slot.cooldownFill = UIFactory.Panel("Cooldown", slot.background.transform, new Color(0f, 0f, 0f, 0.55f));
            slot.cooldownFill.raycastTarget = false;
            UIFactory.Fill(slot.cooldownFill.rectTransform);
            slot.cooldownFill.rectTransform.pivot = new Vector2(0f, 0.5f);

            slot.label = UIFactory.Text("Label", slot.background.transform, "", 22, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(slot.label.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 6f), new Vector2(-10f, -6f));
            toolSlots.Add(slot);
        }

        // Toast
        toastText = UIFactory.Text("Toast", root, "", 36, TextAlignmentOptions.Center);
        UIFactory.Place(toastText.rectTransform, new Vector2(0.2f, 0.78f), new Vector2(0.8f, 0.88f), Vector2.zero, Vector2.zero);
        toastText.fontStyle = FontStyles.Bold;
        toastGroup = toastText.gameObject.AddComponent<CanvasGroup>();
        toastGroup.alpha = 0f;
        toastGroup.blocksRaycasts = false;

        // สรุปผลตอน Game Over (วางด้านล่าง ไม่ทับหน้า Game Over เดิม)
        Image result = UIFactory.Panel("RunResult", root, UIFactory.PanelColor);
        UIFactory.Place(result.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-420f, 30f), new Vector2(420f, 250f));
        resultPanel = result.gameObject;

        resultText = UIFactory.Text("Text", result.transform, "", 28, TextAlignmentOptions.TopLeft);
        UIFactory.Place(resultText.rectTransform, Vector2.zero, Vector2.one, new Vector2(24f, 16f), new Vector2(-300f, -16f));

        Button menuButton = UIFactory.MakeButton("MenuButton", result.transform, "Shop / Menu", 30, UIFactory.ButtonColor);
        UIFactory.Place((RectTransform)menuButton.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-270f, -45f), new Vector2(-24f, 45f));
        menuButton.onClick.AddListener(GoToMenu);

        resultPanel.SetActive(false);
    }

    // ---------------- Refresh ----------------

    private void RefreshProgress()
    {
        levelText.text = $"LV {PlayerProgress.Level}   <size=70%>{PlayerProgress.XPText}</size>";
        xpFill.rectTransform.localScale = new Vector3(PlayerProgress.LevelProgress01, 1f, 1f);

        currencyText.text =
            UIFactory.CurrencyText(CurrencyType.Coin, PlayerProgress.Get(CurrencyType.Coin)) + "\n" +
            UIFactory.CurrencyText(CurrencyType.Gem, PlayerProgress.Get(CurrencyType.Gem)) + "   " +
            UIFactory.CurrencyText(CurrencyType.BossToken, PlayerProgress.Get(CurrencyType.BossToken));
    }

    private void RefreshToolSlots()
    {
        foreach (ToolSlot slot in toolSlots)
        {
            ToolDefinition def = slot.def;
            string keyName = ((int)def.key - (int)KeyCode.Alpha0).ToString();
            bool unlocked = ToolCatalog.IsUnlocked(def.tool);

            if (!unlocked)
            {
                slot.background.color = SlotLocked;
                slot.cooldownFill.rectTransform.localScale = new Vector3(0f, 1f, 1f);
                slot.label.text = $"<color=#888888>[{keyName}] {def.displayName}\n<size=80%>Unlock at LV {def.requiredLevel}</size></color>";
                continue;
            }

            bool selected = EquipmentManager.SelectedTool == def.tool;
            slot.background.color = selected ? SlotSelected : SlotNormal;

            float remaining = ToolEffects.Instance != null ? ToolEffects.Instance.CooldownRemaining(def.tool) : 0f;
            float progress = ToolEffects.Instance != null ? ToolEffects.Instance.CooldownProgress01(def.tool) : 0f;
            slot.cooldownFill.rectTransform.localScale = new Vector3(progress, 1f, 1f);

            string status = remaining > 0f ? $"{remaining:0.0}s" : "Ready";
            slot.label.text = $"[{keyName}] {def.displayName}\n<size=80%>{status}</size>";
        }
    }

    // ---------------- Events ----------------

    private void HandleLevelUp(int newLevel)
    {
        ShowToast($"LEVEL UP!  LV {newLevel}");
    }

    private void ShowResult(RunResult r)
    {
        string text =
            $"<color=#{ColorUtility.ToHtmlStringRGB(UIFactory.Gold)}>+{r.xpGained} XP</color>   " +
            $"LV {r.levelBefore}" + (r.levelAfter > r.levelBefore ? $" -> <b>LV {r.levelAfter}</b>" : "") + "\n" +
            "Earned: " +
            UIFactory.CurrencyText(CurrencyType.Coin, r.coins) + ", " +
            UIFactory.CurrencyText(CurrencyType.Gem, r.gems) + ", " +
            UIFactory.CurrencyText(CurrencyType.BossToken, r.bossTokens);

        List<ToolDefinition> unlocked = ToolCatalog.UnlockedBetween(r.levelBefore, r.levelAfter);
        foreach (ToolDefinition def in unlocked)
            text += $"\n<color=#7CFC9A>New tool unlocked: {def.displayName}!</color>";

        resultText.text = text;
        resultPanel.SetActive(true);
    }

    private void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    // ---------------- Toast ----------------

    private void ShowToast(string message)
    {
        toastText.text = message;
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine());
    }

    private IEnumerator ToastRoutine()
    {
        toastGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(1.2f);

        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            toastGroup.alpha = 1f - t / 0.4f;
            yield return null;
        }
        toastGroup.alpha = 0f;
        toastRoutine = null;
    }
}
