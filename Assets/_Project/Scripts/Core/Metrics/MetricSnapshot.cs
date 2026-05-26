namespace CafeSim.Core.Metrics
{
    /// <summary>
    /// Foto instantánea de las métricas de la simulación. Es un valor inmutable
    /// que se publica vía <c>GameEvents.OnMetricsUpdated</c> para que la UI
    /// actualice el dashboard sin acoplarse al <c>SimulationManager</c>.
    ///
    /// Todas las cantidades temporales están en segundos.
    /// </summary>
    public readonly struct MetricSnapshot
    {
        /// <summary>Tiempo simulado al momento de tomar la foto.</summary>
        public float SimulationTimeSeconds { get; }

        /// <summary>Clientes que han llegado al sistema desde el inicio.</summary>
        public int ArrivedCount { get; }

        /// <summary>Clientes que completaron el flujo (salieron atendidos).</summary>
        public int ServedCount { get; }

        /// <summary>Clientes que abandonaron por superar su paciencia.</summary>
        public int AbandonedCount { get; }

        /// <summary>Clientes rechazados al llegar por límites de capacidad.</summary>
        public int RejectedCount { get; }

        /// <summary>Longitud actual de la cola del cajero.</summary>
        public int CashierQueueLength { get; }

        /// <summary>Longitud actual de la cola del barista.</summary>
        public int BaristaQueueLength { get; }

        /// <summary>Promedio empírico Lq sobre la cola del cajero.</summary>
        public float CashierAverageQueueLength { get; }

        /// <summary>Promedio empírico Lq sobre la cola del barista.</summary>
        public float BaristaAverageQueueLength { get; }

        /// <summary>Promedio empírico Wq: tiempo en colas por cliente atendido.</summary>
        public float AverageTimeInQueues { get; }

        /// <summary>Promedio empírico W: tiempo total en el sistema por cliente atendido.</summary>
        public float AverageTimeInSystem { get; }

        /// <summary>
        /// Utilización empírica ρ del cajero: fracción del tiempo simulado en que
        /// los cajeros estuvieron ocupados (0 a 1, agregado para todos los servidores).
        /// </summary>
        public float CashierUtilization { get; }

        /// <summary>Utilización empírica ρ del barista (0 a 1, agregado).</summary>
        public float BaristaUtilization { get; }

        /// <summary>
        /// Tasa de abandono observada: <c>AbandonedCount / ArrivedCount</c>.
        /// Devuelve 0 si todavía no han llegado clientes.
        /// </summary>
        public float AbandonmentRate
            => ArrivedCount > 0 ? (float)AbandonedCount / ArrivedCount : 0f;

        public MetricSnapshot(
            float simulationTimeSeconds,
            int arrivedCount,
            int servedCount,
            int abandonedCount,
            int rejectedCount,
            int cashierQueueLength,
            int baristaQueueLength,
            float cashierAverageQueueLength,
            float baristaAverageQueueLength,
            float averageTimeInQueues,
            float averageTimeInSystem,
            float cashierUtilization,
            float baristaUtilization)
        {
            SimulationTimeSeconds = simulationTimeSeconds;
            ArrivedCount = arrivedCount;
            ServedCount = servedCount;
            AbandonedCount = abandonedCount;
            RejectedCount = rejectedCount;
            CashierQueueLength = cashierQueueLength;
            BaristaQueueLength = baristaQueueLength;
            CashierAverageQueueLength = cashierAverageQueueLength;
            BaristaAverageQueueLength = baristaAverageQueueLength;
            AverageTimeInQueues = averageTimeInQueues;
            AverageTimeInSystem = averageTimeInSystem;
            CashierUtilization = cashierUtilization;
            BaristaUtilization = baristaUtilization;
        }
    }
}
