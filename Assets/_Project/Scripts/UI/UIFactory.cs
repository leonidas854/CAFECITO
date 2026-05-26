using System;
using UnityEngine;
using UnityEngine.UI;

namespace CafeSim.UI
{
    /// <summary>
    /// Helpers para construir widgets de uGUI desde código sin depender de
    /// prefabs ni de TextMeshPro. Todos los controles usan <c>Image</c> con
    /// <c>sprite = null</c> (rectángulo sólido tintado), lo que mantiene la UI
    /// liviana y consistente sin necesidad de sprites embarcados.
    ///
    /// <para>Tipografía: <see cref="DefaultFont"/> resuelve a
    /// <c>LegacyRuntime.ttf</c> (la fuente built-in de uGUI en Unity 6).</para>
    /// </summary>
    public static class UIFactory
    {
        // ─── Paleta ───────────────────────────────────────────────────────────

        public static readonly Color PanelBackground = new Color(0.08f, 0.08f, 0.10f, 0.85f);
        public static readonly Color PanelHeader     = new Color(0.15f, 0.40f, 0.65f, 1f);
        public static readonly Color TextPrimary     = new Color(0.95f, 0.95f, 0.95f, 1f);
        public static readonly Color TextMuted       = new Color(0.70f, 0.70f, 0.70f, 1f);
        public static readonly Color TextAccent      = new Color(0.55f, 0.85f, 1f, 1f);
        public static readonly Color ButtonNormal    = new Color(0.25f, 0.30f, 0.40f, 1f);
        public static readonly Color ButtonAccent    = new Color(0.20f, 0.55f, 0.80f, 1f);
        public static readonly Color ButtonDanger    = new Color(0.70f, 0.30f, 0.25f, 1f);
        public static readonly Color SliderTrack     = new Color(0.20f, 0.20f, 0.22f, 1f);
        public static readonly Color SliderFill      = new Color(0.30f, 0.65f, 0.95f, 1f);
        public static readonly Color SliderHandle    = new Color(0.95f, 0.95f, 0.95f, 1f);

        // ─── Tipografía ───────────────────────────────────────────────────────

        private static Font _cachedFont;
        public static Font DefaultFont
        {
            get
            {
                if (_cachedFont == null)
                    _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _cachedFont;
            }
        }

        // ─── Canvas ───────────────────────────────────────────────────────────

        public static Canvas CreateOverlayCanvas(string name, Transform parent = null)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) go.transform.SetParent(parent, worldPositionStays: false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        // ─── Contenedores ─────────────────────────────────────────────────────

        public static GameObject CreatePanel(Transform parent, string name, Color background)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, worldPositionStays: false);
            var img = go.GetComponent<Image>();
            img.color = background;
            img.raycastTarget = true;
            return go;
        }

        public static VerticalLayoutGroup AddVerticalLayout(GameObject host,
            float spacing = 6f, RectOffset padding = null,
            bool childForceExpandWidth = true, bool childForceExpandHeight = false)
        {
            var v = host.GetComponent<VerticalLayoutGroup>() ?? host.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = padding ?? new RectOffset(10, 10, 10, 10);
            v.childAlignment = TextAnchor.UpperLeft;
            v.childForceExpandWidth = childForceExpandWidth;
            v.childForceExpandHeight = childForceExpandHeight;
            v.childControlWidth = true;
            v.childControlHeight = true;
            return v;
        }

        public static HorizontalLayoutGroup AddHorizontalLayout(GameObject host,
            float spacing = 6f, RectOffset padding = null,
            bool childForceExpandWidth = false, bool childForceExpandHeight = false)
        {
            var h = host.GetComponent<HorizontalLayoutGroup>() ?? host.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = padding ?? new RectOffset(6, 6, 4, 4);
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childForceExpandWidth = childForceExpandWidth;
            h.childForceExpandHeight = childForceExpandHeight;
            h.childControlWidth = true;
            h.childControlHeight = true;
            return h;
        }

        public static ContentSizeFitter AddContentSizeFitter(GameObject host,
            ContentSizeFitter.FitMode vertical = ContentSizeFitter.FitMode.PreferredSize,
            ContentSizeFitter.FitMode horizontal = ContentSizeFitter.FitMode.Unconstrained)
        {
            var f = host.GetComponent<ContentSizeFitter>() ?? host.AddComponent<ContentSizeFitter>();
            f.verticalFit = vertical;
            f.horizontalFit = horizontal;
            return f;
        }

        public static LayoutElement AddLayoutElement(GameObject host,
            float minHeight = -1f, float preferredHeight = -1f, float flexibleHeight = -1f,
            float minWidth = -1f, float preferredWidth = -1f, float flexibleWidth = -1f)
        {
            var e = host.GetComponent<LayoutElement>() ?? host.AddComponent<LayoutElement>();
            if (minHeight >= 0f) e.minHeight = minHeight;
            if (preferredHeight >= 0f) e.preferredHeight = preferredHeight;
            if (flexibleHeight >= 0f) e.flexibleHeight = flexibleHeight;
            if (minWidth >= 0f) e.minWidth = minWidth;
            if (preferredWidth >= 0f) e.preferredWidth = preferredWidth;
            if (flexibleWidth >= 0f) e.flexibleWidth = flexibleWidth;
            return e;
        }

        // ─── Texto ────────────────────────────────────────────────────────────

        public static Text CreateText(Transform parent, string content, int fontSize, Color color,
            TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, worldPositionStays: false);
            var t = go.AddComponent<Text>();
            t.text = content;
            t.font = DefaultFont;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return t;
        }

        public static Text CreateHeader(Transform parent, string content)
        {
            var t = CreateText(parent, content, 20, TextPrimary,
                align: TextAnchor.MiddleLeft, style: FontStyle.Bold);
            AddLayoutElement(t.gameObject, minHeight: 28f, preferredHeight: 28f);
            return t;
        }

        // ─── Botón ────────────────────────────────────────────────────────────

        public static Button CreateButton(Transform parent, string label, Color background, Action onClick)
        {
            var go = new GameObject("Button_" + Sanitize(label),
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, worldPositionStays: false);

            var img = go.GetComponent<Image>();
            img.color = background;

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            btn.colors = colors;

            if (onClick != null) btn.onClick.AddListener(() => onClick());

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, worldPositionStays: false);
            var rt = labelGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(6f, 4f); rt.offsetMax = new Vector2(-6f, -4f);

            var t = labelGo.AddComponent<Text>();
            t.text = label; t.font = DefaultFont; t.fontSize = 16;
            t.color = TextPrimary; t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;

            AddLayoutElement(go, minHeight: 32f, preferredHeight: 32f);
            return btn;
        }

        // ─── Toggle ───────────────────────────────────────────────────────────

        public static Toggle CreateToggle(Transform parent, string label, bool initial, Action<bool> onChange)
        {
            var row = new GameObject("Toggle_" + Sanitize(label), typeof(RectTransform), typeof(Toggle));
            row.transform.SetParent(parent, worldPositionStays: false);
            AddHorizontalLayout(row, spacing: 8f, padding: new RectOffset(0, 0, 0, 0));
            AddLayoutElement(row, minHeight: 26f, preferredHeight: 26f);

            // Check box
            var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(row.transform, worldPositionStays: false);
            var boxImg = box.GetComponent<Image>();
            boxImg.color = SliderTrack;
            AddLayoutElement(box, minWidth: 20f, preferredWidth: 20f, minHeight: 20f, preferredHeight: 20f);

            // Checkmark dentro
            var mark = new GameObject("Mark", typeof(RectTransform), typeof(Image));
            mark.transform.SetParent(box.transform, worldPositionStays: false);
            var markImg = mark.GetComponent<Image>();
            markImg.color = TextAccent;
            var markRt = mark.GetComponent<RectTransform>();
            markRt.anchorMin = new Vector2(0.2f, 0.2f);
            markRt.anchorMax = new Vector2(0.8f, 0.8f);
            markRt.offsetMin = Vector2.zero; markRt.offsetMax = Vector2.zero;

            // Texto
            var text = CreateText(row.transform, label, 14, TextPrimary, TextAnchor.MiddleLeft);
            AddLayoutElement(text.gameObject, flexibleWidth: 1f, minHeight: 20f);

            // Toggle wiring
            var toggle = row.GetComponent<Toggle>();
            toggle.targetGraphic = boxImg;
            toggle.graphic = markImg;
            toggle.isOn = initial;
            if (onChange != null) toggle.onValueChanged.AddListener(v => onChange(v));
            return toggle;
        }

        // ─── Slider ───────────────────────────────────────────────────────────

        /// <summary>
        /// Construye un slider con etiqueta + valor formateado + handle deslizable.
        /// Devuelve el <see cref="Slider"/> y el <see cref="Text"/> que muestra
        /// el valor numérico para que el caller pueda actualizarlo si cambia
        /// el formato.
        /// </summary>
        public static LabeledSlider CreateLabeledSlider(
            Transform parent, string label, float min, float max, float initial,
            bool wholeNumbers, Func<float, string> format, Action<float> onChange)
        {
            var row = new GameObject("SliderRow_" + Sanitize(label), typeof(RectTransform));
            row.transform.SetParent(parent, worldPositionStays: false);
            AddVerticalLayout(row, spacing: 2f, padding: new RectOffset(0, 0, 2, 2));
            AddLayoutElement(row, minHeight: 44f, preferredHeight: 44f);

            // Header con etiqueta + valor
            var headerGo = new GameObject("Header", typeof(RectTransform));
            headerGo.transform.SetParent(row.transform, worldPositionStays: false);
            AddHorizontalLayout(headerGo, spacing: 4f, padding: new RectOffset(0, 0, 0, 0));
            AddLayoutElement(headerGo, minHeight: 18f, preferredHeight: 18f);

            var labelText = CreateText(headerGo.transform, label, 13, TextMuted, TextAnchor.MiddleLeft);
            AddLayoutElement(labelText.gameObject, flexibleWidth: 1f);

            var valueText = CreateText(headerGo.transform, format(initial), 13, TextAccent, TextAnchor.MiddleRight);
            AddLayoutElement(valueText.gameObject, minWidth: 90f, preferredWidth: 90f);

            // Slider track
            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(row.transform, worldPositionStays: false);
            AddLayoutElement(sliderGo, minHeight: 18f, preferredHeight: 18f);

            // Background
            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(sliderGo.transform, worldPositionStays: false);
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = SliderTrack;
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.35f);
            bgRt.anchorMax = new Vector2(1f, 0.65f);
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

            // Fill area
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, worldPositionStays: false);
            var faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0f, 0.35f);
            faRt.anchorMax = new Vector2(1f, 0.65f);
            faRt.offsetMin = new Vector2(5f, 0f); faRt.offsetMax = new Vector2(-15f, 0f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, worldPositionStays: false);
            var fillImg = fill.GetComponent<Image>();
            fillImg.color = SliderFill;
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;

            // Handle area
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGo.transform, worldPositionStays: false);
            var haRt = handleArea.GetComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(5f, 0f); haRt.offsetMax = new Vector2(-5f, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, worldPositionStays: false);
            var handleImg = handle.GetComponent<Image>();
            handleImg.color = SliderHandle;
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.anchorMin = Vector2.zero; handleRt.anchorMax = Vector2.zero;
            handleRt.sizeDelta = new Vector2(12f, 24f);

            // Slider component
            var slider = sliderGo.GetComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;
            slider.value = Mathf.Clamp(initial, min, max);

            slider.onValueChanged.AddListener(v =>
            {
                valueText.text = format(v);
                onChange?.Invoke(v);
            });

            return new LabeledSlider
            {
                Slider = slider,
                ValueText = valueText,
                Format = format
            };
        }

        // ─── Misc ─────────────────────────────────────────────────────────────

        public static void StretchToParent(GameObject host)
        {
            var rt = host.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void AnchorTopLeft(GameObject host, Vector2 size, Vector2 offset)
        {
            var rt = host.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
        }

        public static void AnchorTopRight(GameObject host, Vector2 size, Vector2 offset)
        {
            var rt = host.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
        }

        public static void AnchorBottomStretch(GameObject host, float height, float horizontalMargin)
        {
            var rt = host.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(-horizontalMargin * 2f, height);
            rt.anchoredPosition = new Vector2(0f, 10f);
        }

        private static string Sanitize(string label)
        {
            if (string.IsNullOrEmpty(label)) return "Anon";
            var chars = label.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i])) chars[i] = '_';
            }
            return new string(chars);
        }

        /// <summary>Wrapper devuelto por <see cref="CreateLabeledSlider"/>.</summary>
        public struct LabeledSlider
        {
            public Slider Slider;
            public Text ValueText;
            public Func<float, string> Format;

            public void SetValueWithoutNotify(float value)
            {
                if (Slider == null) return;
                Slider.SetValueWithoutNotify(value);
                if (ValueText != null && Format != null)
                    ValueText.text = Format(value);
            }
        }
    }
}
