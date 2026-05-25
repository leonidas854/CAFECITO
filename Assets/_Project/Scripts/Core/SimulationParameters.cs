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
        /// <summary>Tasa de llegadas λ (clientes por segundo).</summary>
        public float ArrivalRatePerSecond { get; }

        /// <summary>Tasa de servicio del cajero μ_caja (clientes por segundo, por cajero).</summary>
        public float ServiceRateCashierPerSecond { get; }

        /// <summary>Tasa de servicio del barista μ_barista (clientes por segundo, por barista).</summary>
        public float ServiceRateBaristaPerSecond { get; }

        /// <summary>Tiempo promedio que un cliente permanece consumiendo en la mesa (segundos).</summary>
        public float AverageConsumeTimeSeconds { get; }

        /// <summary>Paciencia máxima en cola antes de abandonar (segundos).</summary>
        public float CustomerPatienceSeconds { get; }

        /// <summary>Probabilidad de que un cliente nuevo realice pedido web (rango [0, 1]).</summary>
        public float WebOrderProbability { get; }

        /// <summary>Número de cajeros activos (c_caja). Debe ser ≥ 1 si hay clientes físicos.</summary>
        public int CashierCount { get; }

        /// <summary>Número de baristas activos (c_barista). Debe ser ≥ 1.</summary>
        public int BaristaCount { get; }

        /// <summary>Semilla para el generador LCG. Debe ser entero positivo.</summary>
        public long LcgSeed { get; }

        /// <summary>Frecuencia con que el SimulationManager emite snapshot de métricas (segundos).</summary>
        public float MetricsEmitIntervalSeconds { get; }

        public SimulationParameters(
            float arrivalRatePerSecond,
            float serviceRateCashierPerSecond,
            float serviceRateBaristaPerSecond,
            float averageConsumeTimeSeconds,
            float customerPatienceSeconds,
            float webOrderProbability,
            int cashierCount,
            int baristaCount,
            long lcgSeed,
            float metricsEmitIntervalSeconds = 1f)
        {
            Validate(arrivalRatePerSecond, nameof(arrivalRatePerSecond), strictlyPositive: true);
            Validate(serviceRateCashierPerSecond, nameof(serviceRateCashierPerSecond), strictlyPositive: true);
            Validate(serviceRateBaristaPerSecond, nameof(serviceRateBaristaPerSecond), strictlyPositive: true);
            Validate(averageConsumeTimeSeconds, nameof(averageConsumeTimeSeconds), strictlyPositive: true);
            Validate(customerPatienceSeconds, nameof(customerPatienceSeconds), strictlyPositive: true);
            Validate(metricsEmitIntervalSeconds, nameof(metricsEmitIntervalSeconds), strictlyPositive: true);

            if (webOrderProbability < 0f || webOrderProbability > 1f)
                throw new ArgumentOutOfRangeException(nameof(webOrderProbability), "Debe estar en [0, 1].");
            if (cashierCount < 1)
                throw new ArgumentOutOfRangeException(nameof(cashierCount), "Debe ser ≥ 1.");
            if (baristaCount < 1)
                throw new ArgumentOutOfRangeException(nameof(baristaCount), "Debe ser ≥ 1.");
            if (lcgSeed <= 0)
                throw new ArgumentOutOfRangeException(nameof(lcgSeed), "Debe ser entero positivo.");

            ArrivalRatePerSecond = arrivalRatePerSecond;
            ServiceRateCashierPerSecond = serviceRateCashierPerSecond;
            ServiceRateBaristaPerSecond = serviceRateBaristaPerSecond;
            AverageConsumeTimeSeconds = averageConsumeTimeSeconds;
            CustomerPatienceSeconds = customerPatienceSeconds;
            WebOrderProbability = webOrderProbability;
            CashierCount = cashierCount;
            BaristaCount = baristaCount;
            LcgSeed = lcgSeed;
            MetricsEmitIntervalSeconds = metricsEmitIntervalSeconds;
        }

        /// <summary>
        /// Construye unos parámetros usando tasas en clientes/min (formato académico).
        /// </summary>
        public static SimulationParameters FromPerMinuteRates(
            float arrivalRatePerMinute,
            float serviceRateCashierPerMinute,
            float serviceRateBaristaPerMinute,
            float averageConsumeTimeSeconds,
            float customerPatienceSeconds,
            float webOrderProbability,
            int cashierCount,
            int baristaCount,
            long lcgSeed,
            float metricsEmitIntervalSeconds = 1f)
        {
            return new SimulationParameters(
                arrivalRatePerMinute / 60f,
                serviceRateCashierPerMinute / 60f,
                serviceRateBaristaPerMinute / 60f,
                averageConsumeTimeSeconds,
                customerPatienceSeconds,
                webOrderProbability,
                cashierCount,
                baristaCount,
                lcgSeed,
                metricsEmitIntervalSeconds);
        }

        private static void Validate(float value, string name, bool strictlyPositive)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentException("Valor no finito.", name);
            if (strictlyPositive && value <= 0f)
                throw new ArgumentOutOfRangeException(name, "Debe ser estrictamente positivo.");
        }
    }
}
