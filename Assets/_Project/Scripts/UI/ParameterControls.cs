using UnityEngine;
using UnityEngine.UI;
using CafeSim.Core;
using CafeSim.Data;

namespace CafeSim.UI
{
    /// <summary>
    /// Panel derecho con sliders para retocar los parámetros mientras la
    /// simulación corre. Cada control escribe en el <see cref="SimulationConfig"/>
    /// inyectado y luego decide si aplica el cambio "en caliente" (vía
    /// <see cref="SimulationManager.UpdateParameters"/>) o si solo lo deja
    /// pendiente para el próximo Reset (cambios estructurales como número
    /// de mesas o semilla).
    /// </summary>
    public sealed class ParameterControls : MonoBehaviour
    {
        private SimulationConfig _config;

        private UIFactory.LabeledSlider _lambda;
        private UIFactory.LabeledSlider _muCashier;
        private UIFactory.LabeledSlider _muBarista;
        private UIFactory.LabeledSlider _patience;
        private UIFactory.LabeledSlider _consume;
        private UIFactory.LabeledSlider _pWeb;
        private UIFactory.LabeledSlider _cashierCount;
        private UIFactory.LabeledSlider _baristaCount;
        private UIFactory.LabeledSlider _tableCount;
        private UIFactory.LabeledSlider _seatsPerTable;
        private Toggle _multiSkill;
        private Text _structuralHint;

        // Si hay cambios estructurales (mesas, sillas) pendientes de Reset,
        // se muestra el aviso debajo del panel.
        private bool _hasPendingStructural;

        // ─── Construcción ─────────────────────────────────────────────────────

        public void Build(SimulationConfig config)
        {
            _config = config;

            UIFactory.AddVerticalLayout(gameObject, spacing: 6f,
                padding: new RectOffset(12, 12, 10, 12));

            UIFactory.CreateHeader(transform, "PARÁMETROS");
            AddHint("Tasas y probabilidades se aplican en caliente. " +
                    "Cambios estructurales (mesas, sillas, semilla) requieren Reiniciar.");

            AddSectionTitle("Tasas (clientes/min)");
            _lambda = UIFactory.CreateLabeledSlider(transform, "λ Llegadas",
                0.5f, 30f, _config.ArrivalRatePerMinute, wholeNumbers: false,
                v => $"{v:F1}/min",
                v => { _config.SetArrivalRatePerMinute(v); ApplyHot(); });

            _muCashier = UIFactory.CreateLabeledSlider(transform, "μ Caja",
                1f, 30f, _config.ServiceRateCashierPerMinute, wholeNumbers: false,
                v => $"{v:F1}/min",
                v => { _config.SetServiceRateCashier(v); ApplyHot(); });

            _muBarista = UIFactory.CreateLabeledSlider(transform, "μ Barista (fallback)",
                0.5f, 15f, _config.ServiceRateBaristaPerMinute, wholeNumbers: false,
                v => $"{v:F1}/min",
                v => { _config.SetServiceRateBarista(v); ApplyHot(); });

            AddSectionTitle("Comportamiento");
            _patience = UIFactory.CreateLabeledSlider(transform, "Paciencia",
                10f, 600f, _config.CustomerPatienceSeconds, wholeNumbers: false,
                v => $"{v:F0} s",
                v => { _config.SetCustomerPatience(v); ApplyHot(); });

            _consume = UIFactory.CreateLabeledSlider(transform, "Tiempo en mesa",
                30f, 1800f, _config.AverageConsumeTimeSeconds, wholeNumbers: false,
                v => v < 60f ? $"{v:F0} s" : $"{v / 60f:F1} min",
                v => { _config.SetAverageConsumeTime(v); ApplyHot(); });

            _pWeb = UIFactory.CreateLabeledSlider(transform, "% pedidos web",
                0f, 1f, _config.WebOrderProbability, wholeNumbers: false,
                v => $"{v * 100f:F0}%",
                v => { _config.SetWebOrderProbability(v); ApplyHot(); });

            AddSectionTitle("Recursos (aplica al reiniciar)");
            _cashierCount = UIFactory.CreateLabeledSlider(transform, "Cajeros",
                1f, 5f, _config.CashierCount, wholeNumbers: true,
                v => $"{(int)v}",
                v => { _config.SetCashierCount((int)v); ApplyHot(); });

            _baristaCount = UIFactory.CreateLabeledSlider(transform, "Baristas",
                1f, 5f, _config.BaristaCount, wholeNumbers: true,
                v => $"{(int)v}",
                v => { _config.SetBaristaCount((int)v); ApplyHot(); });

            _tableCount = UIFactory.CreateLabeledSlider(transform, "Mesas",
                0f, 20f, _config.TableCount, wholeNumbers: true,
                v => $"{(int)v}",
                v => { _config.SetTableCount((int)v); MarkStructuralDirty(); });

            _seatsPerTable = UIFactory.CreateLabeledSlider(transform, "Sillas/mesa",
                1f, 6f, _config.SeatsPerTable, wholeNumbers: true,
                v => $"{(int)v}",
                v => { _config.SetSeatsPerTable((int)v); MarkStructuralDirty(); });

            _multiSkill = UIFactory.CreateToggle(transform, "Cajero también prepara (multi-skill)",
                _config.CashierAlsoBarista,
                value => { _config.SetCashierAlsoBarista(value); ApplyHot(); });

            _structuralHint = UIFactory.CreateText(transform, "", 12,
                new Color(0.95f, 0.80f, 0.30f), TextAnchor.MiddleLeft, FontStyle.Italic);
            UIFactory.AddLayoutElement(_structuralHint.gameObject, minHeight: 18f, preferredHeight: 18f);
        }

        // ─── API pública ──────────────────────────────────────────────────────

        /// <summary>
        /// Refresca todos los sliders desde el <see cref="SimulationConfig"/>
        /// actual. Útil después de un Reset que aplicó cambios estructurales.
        /// </summary>
        public void SyncFromConfig()
        {
            if (_config == null) return;
            _lambda.SetValueWithoutNotify(_config.ArrivalRatePerMinute);
            _muCashier.SetValueWithoutNotify(_config.ServiceRateCashierPerMinute);
            _muBarista.SetValueWithoutNotify(_config.ServiceRateBaristaPerMinute);
            _patience.SetValueWithoutNotify(_config.CustomerPatienceSeconds);
            _consume.SetValueWithoutNotify(_config.AverageConsumeTimeSeconds);
            _pWeb.SetValueWithoutNotify(_config.WebOrderProbability);
            _cashierCount.SetValueWithoutNotify(_config.CashierCount);
            _baristaCount.SetValueWithoutNotify(_config.BaristaCount);
            _tableCount.SetValueWithoutNotify(_config.TableCount);
            _seatsPerTable.SetValueWithoutNotify(_config.SeatsPerTable);
            if (_multiSkill != null) _multiSkill.SetIsOnWithoutNotify(_config.CashierAlsoBarista);
            ClearStructuralHint();
        }

        public void ClearStructuralHint()
        {
            _hasPendingStructural = false;
            if (_structuralHint != null) _structuralHint.text = "";
        }

        // ─── Aplicación de cambios ────────────────────────────────────────────

        private void ApplyHot()
        {
            if (_config == null) return;
            // Los cambios de tasas/probabilidades + redimensión de servidores se
            // aplican sin reiniciar el reloj. Mesas y sillas también se escriben
            // al config pero solo toman efecto en el próximo Configure().
            try
            {
                SimulationManager.Instance.UpdateParameters(_config.ToSimulationParameters());
            }
            catch (System.Exception)
            {
                // Si los parámetros aún no son válidos (e.g. lambda mínima durante drag),
                // ignoramos silenciosamente; el siguiente movimiento los normaliza.
            }
        }

        private void MarkStructuralDirty()
        {
            _hasPendingStructural = true;
            if (_structuralHint != null)
                _structuralHint.text = "Cambios pendientes — pulsa REINICIAR para aplicarlos.";
        }

        // ─── Helpers de UI internos ───────────────────────────────────────────

        private void AddSectionTitle(string title)
        {
            var t = UIFactory.CreateText(transform, title, 13,
                UIFactory.TextAccent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.AddLayoutElement(t.gameObject, minHeight: 22f, preferredHeight: 22f);
        }

        private void AddHint(string text)
        {
            var t = UIFactory.CreateText(transform, text, 11,
                UIFactory.TextMuted, TextAnchor.UpperLeft);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            UIFactory.AddLayoutElement(t.gameObject, minHeight: 38f, preferredHeight: 44f);
        }
    }
}
