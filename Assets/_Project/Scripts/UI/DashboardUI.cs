using UnityEngine;
using UnityEngine.UI;
using CafeSim.Data;

namespace CafeSim.UI
{
    /// <summary>
    /// Orquestador del dashboard. Construye el Canvas raíz con tres regiones:
    /// <list type="bullet">
    ///   <item>Esquina superior izquierda — <see cref="MetricsPanel"/> con los
    ///   números en vivo del último <see cref="Core.Metrics.MetricSnapshot"/>.</item>
    ///   <item>Esquina superior derecha — <see cref="ParameterControls"/> con
    ///   los sliders que escriben directamente en el <see cref="SimulationConfig"/>.</item>
    ///   <item>Banda inferior — <see cref="SimulationControlBar"/> con
    ///   pausar/reanudar/reiniciar y selector de velocidad.</item>
    /// </list>
    ///
    /// El <c>SimulationBootstrap</c> lo crea automáticamente al iniciar si no
    /// hay otro DashboardUI en la escena. Para deshabilitarlo durante una
    /// demo "limpia" (sin overlay), basta con desactivar el GameObject.
    /// </summary>
    public sealed class DashboardUI : MonoBehaviour
    {
        private const float PanelWidth = 320f;
        private const float ControlBarHeight = 64f;

        private SimulationConfig _config;

        public MetricsPanel Metrics { get; private set; }
        public ParameterControls Parameters { get; private set; }
        public SimulationControlBar Controls { get; private set; }

        /// <summary>
        /// Construye toda la jerarquía UI bajo este GameObject. Llamar desde
        /// <c>SimulationBootstrap.Awake</c> después de tener un SimulationConfig
        /// resuelto (creado en runtime o asignado vía inspector).
        /// </summary>
        public void Build(SimulationConfig config)
        {
            _config = config;
            var canvas = UIFactory.CreateOverlayCanvas("CafeSim_Dashboard", transform);

            // Para que la UI reciba clics sin tener que arrastrar un EventSystem
            // a la escena, garantizamos que exista uno.
            EnsureEventSystem();

            BuildMetricsPanel(canvas.transform);
            BuildParameterPanel(canvas.transform);
            BuildControlBar(canvas.transform);
        }

        // ─── Subpaneles ───────────────────────────────────────────────────────

        private void BuildMetricsPanel(Transform parent)
        {
            var panel = UIFactory.CreatePanel(parent, "MetricsPanel", UIFactory.PanelBackground);
            UIFactory.AnchorTopLeft(panel,
                size: new Vector2(PanelWidth, 540f),
                offset: new Vector2(12f, -12f));

            Metrics = panel.AddComponent<MetricsPanel>();
            Metrics.Build();
        }

        private void BuildParameterPanel(Transform parent)
        {
            // Wrap el panel en un ScrollRect para que en pantallas chicas no se
            // recorte; el contenedor crece según el contenido.
            var scroll = new GameObject("ParametersScroll",
                typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scroll.transform.SetParent(parent, worldPositionStays: false);
            UIFactory.AnchorTopRight(scroll,
                size: new Vector2(PanelWidth, 720f),
                offset: new Vector2(-12f, -12f));

            var scrollImg = scroll.GetComponent<Image>();
            scrollImg.color = UIFactory.PanelBackground;

            // Viewport
            var viewport = new GameObject("Viewport",
                typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scroll.transform, worldPositionStays: false);
            UIFactory.StretchToParent(viewport);
            var maskImg = viewport.GetComponent<Image>();
            maskImg.color = new Color(1f, 1f, 1f, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            // Content
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, worldPositionStays: false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);
            UIFactory.AddContentSizeFitter(content,
                vertical: ContentSizeFitter.FitMode.PreferredSize,
                horizontal: ContentSizeFitter.FitMode.Unconstrained);

            var sr = scroll.GetComponent<ScrollRect>();
            sr.viewport = viewport.GetComponent<RectTransform>();
            sr.content = contentRt;
            sr.horizontal = false;
            sr.vertical = true;
            sr.scrollSensitivity = 24f;

            Parameters = content.AddComponent<ParameterControls>();
            Parameters.Build(_config);
        }

        private void BuildControlBar(Transform parent)
        {
            var bar = UIFactory.CreatePanel(parent, "ControlBar", UIFactory.PanelBackground);
            UIFactory.AnchorBottomStretch(bar,
                height: ControlBarHeight, horizontalMargin: 12f);

            Controls = bar.AddComponent<SimulationControlBar>();
            Controls.Build(_config, Parameters);
        }

        // ─── EventSystem ──────────────────────────────────────────────────────

        /// <summary>
        /// Garantiza que exista un EventSystem para que los clics lleguen a los
        /// botones y sliders. Selecciona el InputModule correcto según el Input
        /// Handler del proyecto: si está el paquete nuevo (Input System),
        /// se usa <c>InputSystemUIInputModule</c>; si no, el clásico
        /// <c>StandaloneInputModule</c>.
        /// </summary>
        private static void EnsureEventSystem()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null) return;
            var esGo = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));

            var newModuleType = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (newModuleType != null)
            {
                esGo.AddComponent(newModuleType);
            }
            else
            {
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
    }
}
