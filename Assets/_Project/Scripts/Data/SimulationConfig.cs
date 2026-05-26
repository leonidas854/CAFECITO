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
        // ─── Tasas ─────────────────────────────────────────────────────────────

        [Header("Tasas (clientes por minuto)")]
        [Tooltip("Tasa de llegadas λ. Un cliente cada 15 s por defecto (4/min).")]
        [Range(0.1f, 60f)]
        [SerializeField] private float arrivalRatePerMinute = 4f;

        [Tooltip("Tasa de servicio por cajero μ_caja. ~5 s por orden (12/min).")]
        [Range(0.1f, 60f)]
        [SerializeField] private float serviceRateCashierPerMinute = 12f;

        [Tooltip("Tasa fallback del barista cuando el producto no fija tiempo. El tiempo real lo determina ProductCatalog.")]
        [Range(0.1f, 60f)]
        [SerializeField] private float serviceRateBaristaPerMinute = 3f;

        // ─── Tiempos ──────────────────────────────────────────────────────────

        [Header("Tiempos (segundos)")]
        [Tooltip("Tiempo promedio que un cliente permanece consumiendo en mesa o de pie. 5 min por defecto.")]
        [Range(30f, 1800f)]
        [SerializeField] private float averageConsumeTimeSeconds = 300f;

        [Tooltip("Paciencia máxima en cola antes de abandonar. 90 s por defecto.")]
        [Range(10f, 600f)]
        [SerializeField] private float customerPatienceSeconds = 90f;

        // ─── Servidores ───────────────────────────────────────────────────────

        [Header("Servidores")]
        [Tooltip("Cantidad de cajeros activos. 1 alcanza con λ moderada.")]
        [Range(1, 5)]
        [SerializeField] private int cashierCount = 1;

        [Tooltip("Cantidad de baristas activos. 2 mantiene ρ ≈ 0.7 con la mezcla de productos por defecto.")]
        [Range(1, 5)]
        [SerializeField] private int baristaCount = 2;

        [Tooltip("Si está activo, todos los servidores son multi-skill (cajeros y baristas a la vez).")]
        [SerializeField] private bool cashierAlsoBarista = false;

        // ─── Mesas ────────────────────────────────────────────────────────────

        [Header("Mesas")]
        [Tooltip("Cantidad de mesas en el local.")]
        [Range(0, 20)]
        [SerializeField] private int tableCount = 6;

        [Tooltip("Sillas por mesa.")]
        [Range(1, 6)]
        [SerializeField] private int seatsPerTable = 4;

        // ─── Pedidos web ──────────────────────────────────────────────────────

        [Header("Pedidos web")]
        [Tooltip("Probabilidad [0..1] de pedido web. El asset SimulationConfig_Fisico usa 0; SimulationConfig_Hibrido usa 0.4.")]
        [Range(0f, 1f)]
        [SerializeField] private float webOrderProbability = 0f;

        // ─── Límites de capacidad (protección de rendimiento) ─────────────────

        [Header("Límites de capacidad")]
        [Tooltip("Máximo de clientes en la cola del cajero. Los que llegan estando llena se rechazan.")]
        [Range(3, 30)]
        [SerializeField] private int maxCashierQueueLength = 10;

        [Tooltip("Máximo de clientes en la cola del barista.")]
        [Range(3, 30)]
        [SerializeField] private int maxBaristaQueueLength = 10;

        [Tooltip("Máximo absoluto de clientes simultáneos. Protege Unity de saturarse.")]
        [Range(10, 200)]
        [SerializeField] private int maxConcurrentCustomers = 40;

        // ─── Reproducibilidad ─────────────────────────────────────────────────

        [Header("Reproducibilidad")]
        [Tooltip("Semilla del LCG. Misma semilla ⇒ corrida idéntica.")]
        [SerializeField] private long lcgSeed = 12345L;

        [Header("Emisión de métricas")]
        [Tooltip("Cada cuántos segundos simulados se publica el snapshot de métricas.")]
        [Range(0.1f, 10f)]
        [SerializeField] private float metricsEmitIntervalSeconds = 1f;

        // ─── Lectura ──────────────────────────────────────────────────────────

        public float ArrivalRatePerMinute => arrivalRatePerMinute;
        public float ServiceRateCashierPerMinute => serviceRateCashierPerMinute;
        public float ServiceRateBaristaPerMinute => serviceRateBaristaPerMinute;
        public float AverageConsumeTimeSeconds => averageConsumeTimeSeconds;
        public float CustomerPatienceSeconds => customerPatienceSeconds;
        public int CashierCount => cashierCount;
        public int BaristaCount => baristaCount;
        public bool CashierAlsoBarista => cashierAlsoBarista;
        public int TableCount => tableCount;
        public int SeatsPerTable => seatsPerTable;
        public float WebOrderProbability => webOrderProbability;
        public int MaxCashierQueueLength => maxCashierQueueLength;
        public int MaxBaristaQueueLength => maxBaristaQueueLength;
        public int MaxConcurrentCustomers => maxConcurrentCustomers;
        public long LcgSeed => lcgSeed;
        public float MetricsEmitIntervalSeconds => metricsEmitIntervalSeconds;

        /// <summary>
        /// Convierte la configuración del Inspector a parámetros canónicos.
        /// </summary>
        public SimulationParameters ToSimulationParameters()
        {
            return SimulationParameters.FromPerMinuteRates(
                arrivalRatePerMinute: arrivalRatePerMinute,
                serviceRateCashierPerMinute: serviceRateCashierPerMinute,
                serviceRateBaristaPerMinute: serviceRateBaristaPerMinute,
                averageConsumeTimeSeconds: averageConsumeTimeSeconds,
                customerPatienceSeconds: customerPatienceSeconds,
                cashierCount: cashierCount,
                baristaCount: baristaCount,
                cashierAlsoBarista: cashierAlsoBarista,
                tableCount: tableCount,
                seatsPerTable: seatsPerTable,
                webOrderProbability: webOrderProbability,
                maxCashierQueueLength: maxCashierQueueLength,
                maxBaristaQueueLength: maxBaristaQueueLength,
                maxConcurrentCustomers: maxConcurrentCustomers,
                lcgSeed: lcgSeed,
                metricsEmitIntervalSeconds: metricsEmitIntervalSeconds);
        }

        // ─── Setters para sliders en runtime ──────────────────────────────────

        public void SetArrivalRatePerMinute(float value)   => arrivalRatePerMinute = Mathf.Max(0.1f, value);
        public void SetServiceRateCashier(float value)     => serviceRateCashierPerMinute = Mathf.Max(0.1f, value);
        public void SetServiceRateBarista(float value)     => serviceRateBaristaPerMinute = Mathf.Max(0.1f, value);
        public void SetAverageConsumeTime(float seconds)   => averageConsumeTimeSeconds = Mathf.Max(1f, seconds);
        public void SetCustomerPatience(float seconds)     => customerPatienceSeconds = Mathf.Max(1f, seconds);
        public void SetWebOrderProbability(float value)    => webOrderProbability = Mathf.Clamp01(value);
        public void SetCashierCount(int value)             => cashierCount = Mathf.Max(1, value);
        public void SetBaristaCount(int value)             => baristaCount = Mathf.Max(1, value);
        public void SetCashierAlsoBarista(bool value)      => cashierAlsoBarista = value;
        public void SetTableCount(int value)               => tableCount = Mathf.Max(0, value);
        public void SetSeatsPerTable(int value)            => seatsPerTable = Mathf.Max(1, value);
        public void SetMaxCashierQueueLength(int value)    => maxCashierQueueLength = Mathf.Max(1, value);
        public void SetMaxBaristaQueueLength(int value)    => maxBaristaQueueLength = Mathf.Max(1, value);
        public void SetMaxConcurrentCustomers(int value)   => maxConcurrentCustomers = Mathf.Max(1, value);

        private void OnValidate()
        {
            if (arrivalRatePerMinute <= 0f) arrivalRatePerMinute = 0.1f;
            if (serviceRateCashierPerMinute <= 0f) serviceRateCashierPerMinute = 0.1f;
            if (serviceRateBaristaPerMinute <= 0f) serviceRateBaristaPerMinute = 0.1f;
            if (averageConsumeTimeSeconds <= 0f) averageConsumeTimeSeconds = 1f;
            if (customerPatienceSeconds <= 0f) customerPatienceSeconds = 1f;
            if (cashierCount < 1) cashierCount = 1;
            if (baristaCount < 1) baristaCount = 1;
            if (tableCount < 0) tableCount = 0;
            if (seatsPerTable < 1) seatsPerTable = 1;
            if (maxCashierQueueLength < 1) maxCashierQueueLength = 1;
            if (maxBaristaQueueLength < 1) maxBaristaQueueLength = 1;
            if (maxConcurrentCustomers < 1) maxConcurrentCustomers = 1;
            if (lcgSeed <= 0) lcgSeed = 1;
            if (metricsEmitIntervalSeconds <= 0f) metricsEmitIntervalSeconds = 0.1f;
        }
    }
}
