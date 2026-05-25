namespace CafeSim.Core
{
    /// <summary>
    /// Datos puros de un cliente dentro de la simulación. No contiene
    /// referencias a Unity ni a sprites; solo identidad, estado y timestamps
    /// que permiten calcular métricas (W, Wq, tiempo de servicio, etc.).
    /// </summary>
    public sealed class CustomerData
    {
        /// <summary>Identificador único, asignado por el SimulationManager.</summary>
        public int Id { get; }

        /// <summary>True si el cliente realizó un pedido web (bypassa la caja).</summary>
        public bool IsWebOrder { get; }

        /// <summary>Estado actual dentro del flujo de atención.</summary>
        public CustomerState State { get; set; }

        // ─── Timestamps en segundos de simulación ──────────────────────────────

        /// <summary>Tiempo en que el cliente apareció en el sistema.</summary>
        public float ArrivalTime { get; }

        /// <summary>Tiempo en que entró a la cola de la caja (null si fue pedido web).</summary>
        public float? CashierQueueEnterTime { get; set; }

        /// <summary>Tiempo en que el cajero empezó a atenderlo.</summary>
        public float? CashierServiceStartTime { get; set; }

        /// <summary>Tiempo en que el cajero terminó de atenderlo.</summary>
        public float? CashierServiceEndTime { get; set; }

        /// <summary>Tiempo en que entró a la cola del barista.</summary>
        public float? BaristaQueueEnterTime { get; set; }

        /// <summary>Tiempo en que el barista empezó a prepararle la bebida.</summary>
        public float? BaristaServiceStartTime { get; set; }

        /// <summary>Tiempo en que el barista terminó la bebida.</summary>
        public float? BaristaServiceEndTime { get; set; }

        /// <summary>Tiempo en que se sentó a consumir.</summary>
        public float? ConsumeStartTime { get; set; }

        /// <summary>Tiempo programado en que terminará de consumir.</summary>
        public float? ConsumeEndTime { get; set; }

        /// <summary>Tiempo en que abandonó el local (atendido).</summary>
        public float? DepartureTime { get; set; }

        /// <summary>Tiempo en que abandonó la cola por impaciencia.</summary>
        public float? AbandonmentTime { get; set; }

        public CustomerData(int id, bool isWebOrder, float arrivalTime)
        {
            Id = id;
            IsWebOrder = isWebOrder;
            ArrivalTime = arrivalTime;
            State = CustomerState.Entering;
        }

        /// <summary>
        /// Tiempo total que el cliente pasó dentro del sistema (W).
        /// Devuelve null si todavía está dentro.
        /// </summary>
        public float? TimeInSystem
        {
            get
            {
                float? exit = DepartureTime ?? AbandonmentTime;
                return exit.HasValue ? exit.Value - ArrivalTime : (float?)null;
            }
        }

        /// <summary>
        /// Tiempo total que el cliente pasó esperando en colas (Wq).
        /// Suma tiempo en cola de caja + tiempo en cola de barista.
        /// </summary>
        public float TimeInQueues
        {
            get
            {
                float total = 0f;
                if (CashierQueueEnterTime.HasValue && CashierServiceStartTime.HasValue)
                    total += CashierServiceStartTime.Value - CashierQueueEnterTime.Value;
                if (BaristaQueueEnterTime.HasValue && BaristaServiceStartTime.HasValue)
                    total += BaristaServiceStartTime.Value - BaristaQueueEnterTime.Value;
                return total;
            }
        }

        /// <summary>True si el cliente completó el flujo (fue atendido o abandonó).</summary>
        public bool IsFinished => State == CustomerState.Leaving
                                  || State == CustomerState.Abandoned;
    }
}
