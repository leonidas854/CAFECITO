using System;
using UnityEngine;
using UnityEngine.UI;
using CafeSim.Core.Metrics;
using CafeSim.Events;

namespace CafeSim.UI
{
    /// <summary>
    /// Panel de métricas en vivo. Se suscribe a
    /// <see cref="GameEvents.OnMetricsUpdated"/> y refresca las filas con los
    /// valores del último <see cref="MetricSnapshot"/>.
    ///
    /// <para>Cada fila tiene una etiqueta + valor; la utilización se colorea
    /// para que se vea de un vistazo si el sistema está saturado (verde &lt; 0.7,
    /// amarillo &lt; 0.9, rojo &gt;= 0.9).</para>
    /// </summary>
    public sealed class MetricsPanel : MonoBehaviour
    {
        private static readonly Color RhoGood = new Color(0.40f, 0.85f, 0.45f);
        private static readonly Color RhoWarn = new Color(0.95f, 0.80f, 0.30f);
        private static readonly Color RhoBad  = new Color(0.95f, 0.30f, 0.30f);

        // Referencias a los textos que se actualizan en cada tick.
        private Text _simTime;
        private Text _arrived, _served, _abandoned, _rejected;
        private Text _cashierQueueLive, _baristaQueueLive;
        private Text _cashierLq, _baristaLq;
        private Text _wq, _w;
        private Text _cashierRho, _baristaRho;
        private Text _abandonRate;

        public void Build()
        {
            UIFactory.AddVerticalLayout(gameObject, spacing: 4f,
                padding: new RectOffset(12, 12, 10, 12));

            UIFactory.CreateHeader(transform, "MÉTRICAS EN VIVO");

            _simTime = AddRow("Tiempo simulado", "0.0 s");

            AddDivider();
            AddSectionTitle("Población");
            _arrived   = AddRow("Llegados",      "0");
            _served    = AddRow("Atendidos",     "0");
            _abandoned = AddRow("Abandonos",     "0");
            _rejected  = AddRow("Rechazos",      "0");

            AddDivider();
            AddSectionTitle("Colas");
            _cashierQueueLive = AddRow("Caja (actual)",   "0");
            _baristaQueueLive = AddRow("Barista (actual)", "0");
            _cashierLq        = AddRow("Lq caja (prom)",   "0.00");
            _baristaLq        = AddRow("Lq barista (prom)","0.00");

            AddDivider();
            AddSectionTitle("Tiempos");
            _wq = AddRow("Wq (en colas)",  "0.0 s");
            _w  = AddRow("W (en sistema)", "0.0 s");

            AddDivider();
            AddSectionTitle("Utilización");
            _cashierRho  = AddRow("ρ caja",    "0%");
            _baristaRho  = AddRow("ρ barista", "0%");
            _abandonRate = AddRow("Tasa abandono", "0%");
        }

        private void OnEnable()
        {
            GameEvents.OnMetricsUpdated += HandleSnapshot;
            GameEvents.OnSimulationReset += HandleReset;
        }

        private void OnDisable()
        {
            GameEvents.OnMetricsUpdated -= HandleSnapshot;
            GameEvents.OnSimulationReset -= HandleReset;
        }

        // ─── Handlers ─────────────────────────────────────────────────────────

        private void HandleSnapshot(MetricSnapshot snap)
        {
            if (_simTime == null) return; // aún no construido

            _simTime.text = FormatTime(snap.SimulationTimeSeconds);

            _arrived.text   = snap.ArrivedCount.ToString();
            _served.text    = snap.ServedCount.ToString();
            _abandoned.text = snap.AbandonedCount.ToString();
            _rejected.text  = snap.RejectedCount.ToString();

            _cashierQueueLive.text = snap.CashierQueueLength.ToString();
            _baristaQueueLive.text = snap.BaristaQueueLength.ToString();
            _cashierLq.text = snap.CashierAverageQueueLength.ToString("F2");
            _baristaLq.text = snap.BaristaAverageQueueLength.ToString("F2");

            _wq.text = $"{snap.AverageTimeInQueues:F1} s";
            _w.text  = $"{snap.AverageTimeInSystem:F1} s";

            SetUtilization(_cashierRho, snap.CashierUtilization);
            SetUtilization(_baristaRho, snap.BaristaUtilization);

            float abandonPct = snap.AbandonmentRate;
            _abandonRate.text = $"{abandonPct * 100f:F1}%";
            _abandonRate.color = abandonPct >= 0.15f ? RhoBad
                                : abandonPct >= 0.05f ? RhoWarn
                                : UIFactory.TextPrimary;
        }

        private void HandleReset()
        {
            if (_simTime == null) return;
            _simTime.text = "0.0 s";
            _arrived.text = _served.text = _abandoned.text = _rejected.text = "0";
            _cashierQueueLive.text = _baristaQueueLive.text = "0";
            _cashierLq.text = _baristaLq.text = "0.00";
            _wq.text = _w.text = "0.0 s";
            _cashierRho.text = _baristaRho.text = "0%";
            _cashierRho.color = _baristaRho.color = UIFactory.TextPrimary;
            _abandonRate.text = "0%";
            _abandonRate.color = UIFactory.TextPrimary;
        }

        // ─── Helpers de construcción ──────────────────────────────────────────

        private Text AddRow(string label, string initialValue)
        {
            var row = new GameObject("Row_" + label, typeof(RectTransform));
            row.transform.SetParent(transform, worldPositionStays: false);
            UIFactory.AddHorizontalLayout(row, spacing: 8f, padding: new RectOffset(0, 0, 1, 1));
            UIFactory.AddLayoutElement(row, minHeight: 20f, preferredHeight: 20f);

            var lbl = UIFactory.CreateText(row.transform, label, 13,
                UIFactory.TextMuted, TextAnchor.MiddleLeft);
            UIFactory.AddLayoutElement(lbl.gameObject, flexibleWidth: 1f);

            var val = UIFactory.CreateText(row.transform, initialValue, 13,
                UIFactory.TextPrimary, TextAnchor.MiddleRight);
            UIFactory.AddLayoutElement(val.gameObject, minWidth: 80f, preferredWidth: 80f);
            return val;
        }

        private void AddSectionTitle(string title)
        {
            var t = UIFactory.CreateText(transform, title, 12,
                UIFactory.TextAccent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.AddLayoutElement(t.gameObject, minHeight: 18f, preferredHeight: 18f);
        }

        private void AddDivider()
        {
            var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            divider.transform.SetParent(transform, worldPositionStays: false);
            var img = divider.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.06f);
            UIFactory.AddLayoutElement(divider, minHeight: 1f, preferredHeight: 1f);
        }

        private static void SetUtilization(Text target, float rho)
        {
            target.text = $"{rho * 100f:F1}%";
            target.color = rho >= 0.9f ? RhoBad : rho >= 0.7f ? RhoWarn : RhoGood;
        }

        private static string FormatTime(float seconds)
        {
            if (seconds < 60f) return $"{seconds:F1} s";
            int totalSec = Mathf.RoundToInt(seconds);
            int m = totalSec / 60;
            int s = totalSec % 60;
            return $"{m}m {s:00}s";
        }

    }
}
