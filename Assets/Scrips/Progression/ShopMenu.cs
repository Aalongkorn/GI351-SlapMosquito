using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ร้านค้าในเมนูหลัก (ฉาก MainMenu) สร้าง UI เองด้วยโค้ด
/// ซ้าย: อัปเกรด (ใช้เงิน 3 ประเภท)   ขวา: อุปกรณ์ที่ปลดล็อกด้วยเลเวล
/// </summary>
public class ShopMenu : MonoBehaviour
{
    [Tooltip("แสดงปุ่มทดสอบ (+XP, +เงิน, ล้างเซฟ) ปิดก่อนส่งงานจริง")]
    public bool showDebugButtons = true;

    private GameObject openButton;
    private GameObject panel;
    private TMP_Text headerText;
    private RectTransform upgradeList;
    private RectTransform toolList;

    private ComicCameraController comic;
    private bool isOpen;

    private class UpgradeRow
    {
        public UpgradeDefinition def;
        public TMP_Text info;
        public Button buy;
    }

    private readonly List<UpgradeRow> upgradeRows = new List<UpgradeRow>();
    private readonly List<TMP_Text> toolRows = new List<TMP_Text>();

    private void Awake()
    {
        Build();
    }

    private void OnEnable()
    {
        PlayerProgress.Changed += Refresh;
    }

    private void OnDisable()
    {
        PlayerProgress.Changed -= Refresh;
    }

    private void Start()
    {
        comic = FindAnyObjectByType<ComicCameraController>();
        Refresh();
    }

    private void Update()
    {
        // ซ่อนปุ่มร้านค้าตอนเล่นคัตซีน
        bool cutscenePlaying = comic != null && comic.IsPlaying;
        if (openButton.activeSelf == cutscenePlaying) openButton.SetActive(!cutscenePlaying);
        if (cutscenePlaying && isOpen) Close();

        if (isOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    public void Open()
    {
        isOpen = true;
        panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        isOpen = false;
        panel.SetActive(false);
    }

    // ---------------- Build UI ----------------

    private void Build()
    {
        Canvas canvas = UIFactory.CreateCanvas("Shop Canvas", 400, transform);
        Transform root = canvas.transform;

        Button shopButton = UIFactory.MakeButton("ShopButton", root, "SHOP", 36, new Color(0.75f, 0.5f, 0.15f));
        UIFactory.Place((RectTransform)shopButton.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-260f, -110f), new Vector2(-30f, -30f));
        shopButton.onClick.AddListener(Open);
        openButton = shopButton.gameObject;

        // ฉากหลังมืดเต็มจอ
        Image dim = UIFactory.Panel("Shop", root, new Color(0f, 0f, 0f, 0.7f));
        UIFactory.Fill(dim.rectTransform);
        panel = dim.gameObject;

        Image window = UIFactory.Panel("Window", dim.transform, UIFactory.PanelColor);
        UIFactory.Place(window.rectTransform, new Vector2(0.08f, 0.07f), new Vector2(0.92f, 0.93f), Vector2.zero, Vector2.zero);

        TMP_Text title = UIFactory.Text("Title", window.transform, "SHOP", 52, TextAlignmentOptions.TopLeft);
        UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0.3f, 1f), new Vector2(30f, -90f), new Vector2(0f, -20f));
        title.color = UIFactory.Gold;
        title.fontStyle = FontStyles.Bold;

        headerText = UIFactory.Text("Header", window.transform, "", 28, TextAlignmentOptions.TopRight);
        UIFactory.Place(headerText.rectTransform, new Vector2(0.3f, 1f), new Vector2(1f, 1f), new Vector2(0f, -110f), new Vector2(-110f, -24f));

        Button close = UIFactory.MakeButton("Close", window.transform, "X", 34, new Color(0.5f, 0.2f, 0.2f));
        UIFactory.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-90f, -90f), new Vector2(-24f, -24f));
        close.onClick.AddListener(Close);

        // คอลัมน์ซ้าย: Upgrade
        TMP_Text upTitle = UIFactory.Text("UpgradesTitle", window.transform, "UPGRADES", 32);
        UIFactory.Place(upTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0.6f, 1f), new Vector2(30f, -170f), new Vector2(-10f, -120f));
        upTitle.color = UIFactory.Gold;
        upgradeList = UIFactory.Rect("Upgrades", window.transform);
        UIFactory.Place(upgradeList, new Vector2(0f, 0f), new Vector2(0.6f, 1f), new Vector2(30f, 100f), new Vector2(-10f, -180f));
        UIFactory.VerticalList(upgradeList, 10f);

        foreach (UpgradeDefinition def in UpgradeCatalog.All)
            upgradeRows.Add(CreateUpgradeRow(def));

        // คอลัมน์ขวา: Tools
        TMP_Text toolTitle = UIFactory.Text("ToolsTitle", window.transform, "TOOLS (unlock by level)", 32);
        UIFactory.Place(toolTitle.rectTransform, new Vector2(0.6f, 1f), new Vector2(1f, 1f), new Vector2(20f, -170f), new Vector2(-30f, -120f));
        toolTitle.color = UIFactory.Gold;
        toolList = UIFactory.Rect("Tools", window.transform);
        UIFactory.Place(toolList, new Vector2(0.6f, 0f), new Vector2(1f, 1f), new Vector2(20f, 100f), new Vector2(-30f, -180f));
        UIFactory.VerticalList(toolList, 10f);

        foreach (ToolDefinition def in ToolCatalog.All)
        {
            Image row = UIFactory.Panel(def.displayName, toolList, UIFactory.RowColor);
            UIFactory.SetHeight(row, 92f);
            TMP_Text text = UIFactory.Text("Text", row.transform, "", 24, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 4f), new Vector2(-16f, -4f));
            toolRows.Add(text);
        }

        if (showDebugButtons) BuildDebugButtons(window.transform);

        panel.SetActive(false);
    }

    private UpgradeRow CreateUpgradeRow(UpgradeDefinition def)
    {
        Image row = UIFactory.Panel(def.displayName, upgradeList, UIFactory.RowColor);
        UIFactory.SetHeight(row, 92f);

        TMP_Text info = UIFactory.Text("Info", row.transform, "", 24, TextAlignmentOptions.MidlineLeft);
        UIFactory.Place(info.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 4f), new Vector2(-250f, -4f));

        Button buy = UIFactory.MakeButton("Buy", row.transform, "Buy", 24, UIFactory.ButtonColor);
        UIFactory.Place((RectTransform)buy.transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-236f, 12f), new Vector2(-12f, -12f));
        buy.onClick.AddListener(() => PlayerProgress.TryBuyUpgrade(def));

        return new UpgradeRow { def = def, info = info, buy = buy };
    }

    private void BuildDebugButtons(Transform parent)
    {
        RectTransform bar = UIFactory.Rect("Debug", parent);
        UIFactory.Place(bar, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(30f, 20f), new Vector2(-30f, 80f));
        var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        void Add(string label, UnityEngine.Events.UnityAction action)
        {
            Button b = UIFactory.MakeButton(label, bar, label, 22, new Color(0.25f, 0.25f, 0.35f));
            var le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 220f;
            b.onClick.AddListener(action);
        }

        Add("DEBUG +20 XP", () => { PlayerProgress.AddXP(20); PlayerProgress.Save(); });
        Add("DEBUG +50 money", () =>
        {
            PlayerProgress.Add(CurrencyType.Coin, 50);
            PlayerProgress.Add(CurrencyType.Gem, 10);
            PlayerProgress.Add(CurrencyType.BossToken, 2);
            PlayerProgress.Save();
        });
        Add("DEBUG Reset save", PlayerProgress.ResetAll);
    }

    // ---------------- Refresh ----------------

    private void Refresh()
    {
        if (headerText == null) return;

        headerText.text =
            $"<color=#{ColorUtility.ToHtmlStringRGB(UIFactory.Gold)}>LV {PlayerProgress.Level}</color>  ({PlayerProgress.XPText})\n" +
            UIFactory.CurrencyText(CurrencyType.Coin, PlayerProgress.Get(CurrencyType.Coin)) + "   " +
            UIFactory.CurrencyText(CurrencyType.Gem, PlayerProgress.Get(CurrencyType.Gem)) + "   " +
            UIFactory.CurrencyText(CurrencyType.BossToken, PlayerProgress.Get(CurrencyType.BossToken));

        foreach (UpgradeRow row in upgradeRows)
        {
            UpgradeDefinition def = row.def;
            int level = PlayerProgress.GetUpgradeLevel(def.id);
            bool maxed = level >= def.maxLevel;

            row.info.text =
                $"<b>{def.displayName}</b>  <size=80%>LV {level}/{def.maxLevel}</size>\n" +
                $"<size=80%><color=#BBBBBB>{def.description}: {def.FormatValue(level)}</color></size>";

            if (maxed)
            {
                row.buy.interactable = false;
                row.buy.GetComponentInChildren<TMP_Text>().text = "MAX";
            }
            else
            {
                int cost = def.CostForNext(level);
                row.buy.interactable = PlayerProgress.CanAfford(def.currency, cost);
                row.buy.GetComponentInChildren<TMP_Text>().text = UIFactory.CurrencyText(def.currency, cost);
            }
        }

        for (int i = 0; i < toolRows.Count; i++)
        {
            ToolDefinition def = ToolCatalog.All[i];
            bool unlocked = ToolCatalog.IsUnlocked(def.tool);
            string keyName = ((int)def.key - (int)KeyCode.Alpha0).ToString();

            toolRows[i].text = unlocked
                ? $"<b>[{keyName}] {def.displayName}</b>  <color=#7CFC9A>UNLOCKED</color>\n<size=80%><color=#BBBBBB>{def.description}</color></size>"
                : $"<color=#888888><b>[{keyName}] {def.displayName}</b>  LOCKED - LV {def.requiredLevel}\n<size=80%>{def.description}</size></color>";
        }
    }
}
