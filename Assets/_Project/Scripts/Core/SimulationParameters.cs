using System;

namespace CafeSim.Core
{
    /// <summary>
    /// Parámetros canónicos de la simulación expresados en segundos. Es un POCO
    /// puro sin dependencias de Unity para permitir que el Core se ejecute
    /// desde un proceso .NET externo (validación contra ProModel).
    ///
    /// El ScriptableObject <c>SimulationConfig</c> de la capa Data lo construye
    /// convirtiendo las tasas de "clientes/min" (ergonómicas en el Inspector)
    /// a "clientes/segundo" (canónicas internamente).
    /// </summary>
    public sealed class SimulationParameters
    {
        // ─── Tasas ────────────────────────────────────────────────────────────

        /// <summary>Tasa de llegadas λ (clientes por segundo).</summary>
        public float ArrivalRatePerSecond { get; }

        /// <summary>Tasa de servicio del cajero μ_caja (clientes por segundo, por cajero).</summary>
        public float ServiceRateCashierPerSecond { get; }

        /// <summary>
        /// Tasa de servicio del barista cuando no hay producto asignado (fallback).
        /// El tiempo real se determina por el <see cref="ProductType"/> del cliente.
        /// </summary>
        public float ServiceRateBaristaPerSecond { get; }

        // ─── Tiempos ──────────────────────────────────────────────────────────

        /// <summary>Tiempo promedio que un cliente permanece consumiendo (segundos).</summary>
        public float AverageConsumeTimeSeconds { get; }

        /// <summary>Paciencia máxima en cola antes de abandonar (segundos).</summary>
        public float CustomerPatienceSeconds { get; }

        // ─── Servidores ───────────────────────────────────────────────────────

        /// <summary>Número de cajeros activos.</summary>
        public int CashierCount { get; }

        /// <summary>Número de baristas activos.</summary>
        public int BaristaCount { get; }

        /// <summary>
        /// Si es true, todos los cajeros y baristas operan como pool unificado:
        /// cualquiera puede atender la cola de la caja o la del barista (multi-skill).
        /// </summary>
        public bool CashierAlsoBarista { get; }

        // ─── Mesas ────────────────────────────────────────────────────────────

        /// <summary>Cantidad de mesas en el local.</summary>
        public int TableCount { get; }

        /// <summary>Sillas disponibles por mesa.</summary>
        public int SeatsPerTable { get; }

        // ─── Pedidos ──────────────────────────────────────────────────────────

        /// <summary>Probabilidad [0,1] de que un cliente nuevo realice pedido web.</summary>
        public float WebOrderProbability { get; }

        // ─── Límites (protegen rendimiento) ───────────────────────────────────

        /// <summary>Máximo de clientes esperando en la cola del cajero.</summary>
        public int MaxCashierQueueLength { get; }

        /// <summary>Máximo de clientes esperando en la cola del barista.</summary>
        public int MaxBaristaQueueLength { get; }

        /// <summary>
        /// Máximo absoluto de clientes simultáneos en el sistema (esperando,
        /// siendo atendidos o consumiendo). Protege a Unity de instanciar
        /// cientos de GameObjects con un λ muy alto.
        /// </summary>
        public int MaxConcurrentCustomers { get; }

        // ─── Reproducibilidad y emisión ───────────────────────────────────────

        /// <summary>Semilla para el generador LCG (entero positivo).</summary>
        public long LcgSeed { get; }

        /// <summary>Frecuencia con que se publica el snapshot de métricas (segundos).</summary>
        public float MetricsEmitIntervalSeconds { get; }

        public SimulationParameters(
            float arrivalRatePerSecond,
            float serviceRateCashierPerSecond,
            float serviceRateBaristaPerSecond,
            float averageConsumeTimeSeconds,
            float customerPatienceSeconds,
            int cashierCount,
            int baristaCount,
            bool cashierAlsoBarista,
            int tableCount,
            int seatsPerTable,
            float webOrderProbability,
            int maxCashierQueueLength,
            int maxBaristaQueueLength,
            int maxConcurrentCustomers,
            long lcgSeed,
            float metricsEmitIntervalSeconds = 1f)
        {
            ValidatePositive(arrivalRatePerSecond, nameof(arrivalRatePerSecond));
            ValidatePositive(serviceRateCashierPerSecond, nameof(serviceRateCashierPerSecond));
            ValidatePositive(serviceRateBaristaPerSecond, nameof(serviceRateBaristaPerSecond));
            ValidatePositive(averageConsumeTimeSeconds, nameof(averageConsumeTimeSeconds));
            ValidatePositive(customerPatienceSeconds, nameof(customerPatienceSeconds));
            ValidatePositive(metricsEmitIntervalSeconds, nameof(metricsEmitIntervalSeconds));

            if (webOrderProbability < 0f || webOrderProbability > 1f)
                throw new ArgumentOutOfRangeException(nameof(webOrderProbability), "Debe estar en [0, 1].");
            if (cashierCount < 1) throw new ArgumentOutOfRangeException(nameof(cashierCount), "≥ 1.");
            if (baristaCount < 1) throw new ArgumentOutOfRangeException(nameof(baristaCount), "≥ 1.");
            if (tableCount < 0) throw new ArgumentOutOfRangeException(nameof(tableCount), "≥ 0.");
            if (seatsPerTable < 1) throw new ArgumentOutOfRangeException(nameof(seatsPerTable), "≥ 1.");
            if (maxCashierQueueLength < 1) throw new ArgumentOutOfRangeException(nameof(maxCashierQueueLength), "≥ 1.");
            if (maxBaristaQueueLength < 1) throw new ArgumentOutOfRangeException(nameof(maxBaristaQueueLength), "≥ 1.");
            if (maxConcurrentCustomers < 1) throw new ArgumentOutOfRangeException(nameof(maxConcurrentCustomers), "≥ 1.");
            if (lcgSeed <= 0) throw new ArgumentOutOfRangeException(nameof(lcgSeed), "Entero positivo.");

            ArrivalRatePerSecond = arrivalRatePerSecond;
            ServiceRateCashierPerSecond = serviceRateCashierPerSecond;
            ServiceRateBaristaPerSecond = serviceRateBaristaPerSecond;
            AverageConsumeTimeSeconds = averageConsumeTimeSeconds;
            CustomerPatienceSeconds = customerPatienceSeconds;
            CashierCount = cashierCount;
            BaristaCount = baristaCount;
            CashierAlsoBarista = cashierAlsoBarista;
            TableCount = tableCount;
            SeatsPerTable = seatsPerTable;
            WebOrderProbability = webOrderProbability;
            MaxCashierQueueLength = maxCashierQueueLength;
            MaxBaristaQueueLength = maxBaristaQueueLength;
            MaxConcurrentCustomers = maxConcurrentCustomers;
            LcgSeed = lcgSeed;
            MetricsEmitIntervalSeconds = metricsEmitIntervalSeconds;
        }

        /// <summary>
        /// Construye parámetros usando tasas en clientes/min (formato académico).
        /// </summary>
        public static SimulationParameters FromPerMinuteRates(
            float arrivalRatePerMinute,
            float serviceRateCashierPerMinute,
            float serviceRateBaristaPerMinute,
            float averageConsumeTimeSeconds,
            float customerPatienceSeconds,
            int cashierCount,
            int baristaCount,
            bool cashierAlsoBarista,
            int tableCount,
            int seatsPerTable,
            float webOrderProbability,
            int maxCashierQueueLength,
            int maxBaristaQueueLength,
            int maxConcurrentCustomers,
            long lcgSeed,
            float metricsEmitIntervalSeconds = 1f)
        {
            return new SimulationParameters(
                arrivalRatePerSecond: arrivalRatePerMinute / 60f,
                serviceRateCashierPerSecond: serviceRateCashierPerMinute / 60f,
                serviceRateBaristaPerSecond: serviceRateBaristaPerMinute / 60f,
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

        private static void ValidatePositive(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentException("Valor no finito.", name);
            if (value <= 0f)
                throw new ArgumentOutOfRangeException(name, "Debe ser estrictamente positivo.");
        }
    }
}
