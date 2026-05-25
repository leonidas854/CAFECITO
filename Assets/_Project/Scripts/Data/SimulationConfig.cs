using UnityEngine;
using CafeSim.Core;

namespace CafeSim.Data
{
    /// <summary>
    /// <see cref="ScriptableObject"/> que expone los parámetros de la simulación
    /// al Inspector de Unity en unidades ergonómicas (clientes/min, segundos)
    /// y los convierte a <see cref="SimulationParameters"/> canónicos para el Core.
    ///
    /// <para>Crear instancias desde el menú: <b>Assets → Create → CafeSim →
    /// Simulation Config</b>. Guardar varios assets para representar escenarios
    /// distintos (hora pico, hora muerta, alta proporción web, etc.).</para>
    /// </summary>
    [CreateAssetMenu(fileName = "SimulationConfig", menuName = "CafeSim/Simulation Config", order = 0)]
    public sealed class SimulationConfig : ScriptableObject
    {
        [Header("Tasas (clientes por minuto)")]
        [Tooltip("Tasa de llegadas λ. Clientes nuevos por minuto.")]
        [Range(0.1f, 60f)]
        [SerializeField] private float arrivalRatePerMinute = 5f;

        [Tooltip("Tasa de servicio por cajero μ_caja. Clientes atendidos por minuto.")]
        [Range(0.1f, 60f)]
        [SerializeField] private float serviceRateCashierPerMinute = 8f;

        [Tooltip("Tasa de servicio por barista μ_barista. Bebidas preparadas por minuto.")]
        [Range(0.1f, 60f)]
        [SerializeField] private float serviceRateBaristaPerMinute = 6f;

        [Header("Tiempos (segundos)")]
        [Tooltip("Tiempo promedio que un cliente permanece consumiendo en mesa.")]
        [Range(30f, 1800f)]
        [SerializeField] private float averageConsumeTimeSeconds = 600f;

        [Tooltip("Paciencia máxima en cola antes de abandonar.")]
        [Range(10f, 600f)]
        [SerializeField] private float customerPatienceSeconds = 120f;

        [Header("Servidores")]
        [Tooltip("Cantidad de cajeros activos (c_caja).")]
        [Range(1, 5)]
        [SerializeField] private int cashierCount = 1;

        [Tooltip("Cantidad de baristas activos (c_barista).")]
        [Range(1, 5)]
        [SerializeField] private int baristaCount = 1;

        [Header("Pedidos web")]
        [Tooltip("Probabilidad de que un cliente nuevo realice pedido web. 0 = todos físicos.")]
        [Range(0f, 1f)]
        [SerializeField] private float webOrderProbability = 0.3f;

        [Header("Reproducibilidad")]
        [Tooltip("Semilla del generador LCG. Con la misma semilla la corrida es idéntica.")]
        [SerializeField] private long lcgSeed = 12345L;

        [Header("Emisión de métricas")]
        [Tooltip("Cada cuántos segundos simulados se publica una foto de métricas.")]
        [Range(0.1f, 10f)]
        [SerializeField] private float metricsEmitIntervalSeconds = 1f;

        // ─── Acceso de solo lectura ──────────────────────────────────────────

        public float ArrivalRatePerMinute => arrivalRatePerMinute;
        public float ServiceRateCashierPerMinute => serviceRateCashierPerMinute;
        public float ServiceRateBaristaPerMinute => serviceRateBaristaPerMinute;
        public float AverageConsumeTimeSeconds => averageConsumeTimeSeconds;
        public float CustomerPatienceSeconds => customerPatienceSeconds;
        public int CashierCount => cashierCount;
        public int BaristaCount => baristaCount;
        public float WebOrderProbability => webOrderProbability;
        public long LcgSeed => lcgSeed;
        public float MetricsEmitIntervalSeconds => metricsEmitIntervalSeconds;

        /// <summary>
        /// Convierte la configuración del Inspector a parámetros canónicos del
        /// Core (tasas en clientes/segundo). El SimulationManager debe llamarse
        /// con este resultado.
        /// </summary>
        public SimulationParameters ToSimulationParameters()
        {
            return SimulationParameters.FromPerMinuteRates(
                arrivalRatePerMinute: arrivalRatePerMinute,
                serviceRateCashierPerMinute: serviceRateCashierPerMinute,
                serviceRateBaristaPerMinute: serviceRateBaristaPerMinute,
                averageConsumeTimeSeconds: averageConsumeTimeSeconds,
                customerPatienceSeconds: customerPatienceSeconds,
                webOrderProbability: webOrderProbability,
                cashierCount: cashierCount,
                baristaCount: baristaCount,
                lcgSeed: lcgSeed,
                metricsEmitIntervalSeconds: metricsEmitIntervalSeconds);
        }

        /// <summary>
        /// Aplica los valores del slider al asset y notifica a editor para que
        /// otras vistas (Inspector) se refresquen. Usado por la UI que escribe
        /// en runtime sobre la SimulationConfig vigente.
        /// </summary>
        public void SetArrivalRatePerMinute(float value)   => arrivalRatePerMinute = Mathf.Max(0.1f, value);
        public void SetServiceRateCashier(float value)     => serviceRateCashierPerMinute = Mathf.Max(0.1f, value);
        public void SetServiceRateBarista(float value)     => serviceRateBaristaPerMinute = Mathf.Max(0.1f, value);
        public void SetCustomerPatience(float seconds)     => customerPatienceSeconds = Mathf.Max(1f, seconds);
        public void SetWebOrderProbability(float value)    => webOrderProbability = Mathf.Clamp01(value);
        public void SetCashierCount(int value)             => cashierCount = Mathf.Max(1, value);
        public void SetBaristaCount(int value)             => baristaCount = Mathf.Max(1, value);

        private void OnValidate()
        {
            if (arrivalRatePerMinute <= 0f) arrivalRatePerMinute = 0.1f;
            if (serviceRateCashierPerMinute <= 0f) serviceRateCashierPerMinute = 0.1f;
            if (serviceRateBaristaPerMinute <= 0f) serviceRateBaristaPerMinute = 0.1f;
            if (averageConsumeTimeSeconds <= 0f) averageConsumeTimeSeconds = 1f;
            if (customerPatienceSeconds <= 0f) customerPatienceSeconds = 1f;
            if (cashierCount < 1) cashierCount = 1;
            if (baristaCount < 1) baristaCount = 1;
            if (lcgSeed <= 0) lcgSeed = 1;
            if (metricsEmitIntervalSeconds <= 0f) metricsEmitIntervalSeconds = 0.1f;
        }
    }
}
