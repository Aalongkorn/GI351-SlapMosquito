using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ตัวช่วยสร้าง UI ด้วยโค้ด (ShopMenu / ProgressHUD ใช้)
/// ทำให้ไม่ต้องลาก UI เองใน Scene ตอนนี้ ภายหลังจะย้ายไปทำเป็น prefab ก็ได้
/// </summary>
public static class UIFactory
{
    public static readonly Color PanelColor = new Color(0.07f, 0.07f, 0.1f, 0.95f);
    public static readonly Color RowColor = new Color(1f, 1f, 1f, 0.06f);
    public static readonly Color ButtonColor = new Color(0.22f, 0.45f, 0.3f, 1f);
    public static readonly Color DisabledColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    public static readonly Color Gold = new Color(1f, 0.85f, 0.35f);

    public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent = null)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        if (parent != null) go.transform.SetParent(parent, false);

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    /// <summary>ตั้ง anchor (0..1) และระยะขอบ (pixel)</summary>
    public static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    public static void Fill(RectTransform rt) => Place(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

    public static Image Panel(string name, Transform parent, Color color)
    {
        RectTransform rt = Rect(name, parent);
        var image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    public static TextMeshProUGUI Text(string name, Transform parent, string text, float size,
        TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        RectTransform rt = Rect(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = alignment;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    public static Button MakeButton(string name, Transform parent, string label, float fontSize, Color color)
    {
        Image image = Panel(name, parent, color);
        var button = image.gameObject.AddComponent<Button>();

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        button.colors = colors;

        TextMeshProUGUI text = Text("Label", image.transform, label, fontSize, TextAlignmentOptions.Center);
        Fill(text.rectTransform);
        return button;
    }

    public static TMP_Text Label(this Button button) => button.GetComponentInChildren<TMP_Text>();

    public static VerticalLayoutGroup VerticalList(RectTransform rt, float spacing, int padding = 0)
    {
        var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(padding, padding, padding, padding);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return layout;
    }

    public static void SetHeight(Component c, float height)
    {
        LayoutElement le = c.GetComponent<LayoutElement>();
        if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
    }

    public static string CurrencyName(CurrencyType type)
    {
        switch (type)
        {
            case CurrencyType.Coin: return "Coin";
            case CurrencyType.Gem: return "Gem";
            case CurrencyType.BossToken: return "Boss Token";
            default: return type.ToString();
        }
    }

    public static Color CurrencyColor(CurrencyType type)
    {
        switch (type)
        {
            case CurrencyType.Coin: return new Color(1f, 0.82f, 0.3f);
            case CurrencyType.Gem: return new Color(0.45f, 0.85f, 1f);
            case CurrencyType.BossToken: return new Color(1f, 0.45f, 0.45f);
            default: return Color.white;
        }
    }

    /// <summary>ข้อความเงินพร้อมสี เช่น "12 Coin"</summary>
    public static string CurrencyText(CurrencyType type, int amount)
    {
        string hex = ColorUtility.ToHtmlStringRGB(CurrencyColor(type));
        return $"<color=#{hex}>{amount} {CurrencyName(type)}</color>";
    }
}
