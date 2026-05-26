using System;
using CafeSim.Core;
using CafeSim.Core.Metrics;

namespace CafeSim.Events
{
    /// <summary>
    /// Bus estático de eventos del simulador. Implementa el patrón Observer:
    /// el <c>SimulationManager</c> dispara estos eventos y las capas externas
    /// (Entities visuales, UI dashboard) se suscriben sin necesidad de una
    /// referencia directa al Core.
    ///
    /// <para><b>Regla obligatoria</b>: cada <c>OnEnable</c> que se suscriba
    /// debe tener su <c>OnDisable</c> que se desuscriba, o habrá fugas de
    /// memoria y null references al recargar la escena.</para>
    /// </summary>
    public static class GameEvents
    {
        /// <summary>Se dispara cuando un cliente nuevo aparece en el sistema.</summary>
        public static event Action<CustomerData> OnCustomerArrived;

        /// <summary>Se dispara cada vez que un cliente cambia de estado.</summary>
        public static event Action<CustomerData> OnCustomerStateChanged;

        /// <summary>Se dispara cuando un cliente sale del local atendido.</summary>
        public static event Action<CustomerData> OnCustomerServed;

        /// <summary>Se dispara cuando un cliente abandona la cola por impaciencia.</summary>
        public static event Action<CustomerData> OnCustomerAbandoned;

        /// <summary>
        /// Se dispara cuando un cliente es rechazado al llegar porque la cola
        /// está llena o se alcanzó el máximo de clientes simultáneos.
        /// </summary>
        public static event Action<CustomerData> OnCustomerRejected;

        /// <summary>Se dispara cuando un cliente toma asiento en una mesa.</summary>
        public static event Action<CustomerData> OnCustomerSeated;

        /// <summary>Se dispara cuando un cliente libera la mesa que ocupaba.</summary>
        public static event Action<CustomerData> OnCustomerLeftTable;

        /// <summary>
        /// Se dispara cuando un cliente desaparece del sistema, sea por haber
        /// sido atendido o por abandono. Útil para que la capa visual remueva
        /// el GameObject con una sola suscripción.
        /// </summary>
        public static event Action<CustomerData> OnCustomerLeft;

        /// <summary>Se dispara periódicamente con la foto actual de métricas.</summary>
        public static event Action<MetricSnapshot> OnMetricsUpdated;

        /// <summary>Se dispara cuando la simulación es reiniciada desde cero.</summary>
        public static event Action OnSimulationReset;

        // ─── Métodos invocadores (usados solo por el SimulationManager) ───────

        public static void RaiseCustomerArrived(CustomerData customer)
            => OnCustomerArrived?.Invoke(customer);

        public static void RaiseCustomerStateChanged(CustomerData customer)
            => OnCustomerStateChanged?.Invoke(customer);

        public static void RaiseCustomerServed(CustomerData customer)
        {
            OnCustomerServed?.Invoke(customer);
            OnCustomerLeft?.Invoke(customer);
        }

        public static void RaiseCustomerAbandoned(CustomerData customer)
        {
            OnCustomerAbandoned?.Invoke(customer);
            OnCustomerLeft?.Invoke(customer);
        }

        public static void RaiseCustomerRejected(CustomerData customer)
        {
            OnCustomerRejected?.Invoke(customer);
            OnCustomerLeft?.Invoke(customer);
        }

        public static void RaiseCustomerSeated(CustomerData customer)
            => OnCustomerSeated?.Invoke(customer);

        public static void RaiseCustomerLeftTable(CustomerData customer)
            => OnCustomerLeftTable?.Invoke(customer);

        public static void RaiseMetricsUpdated(MetricSnapshot snapshot)
            => OnMetricsUpdated?.Invoke(snapshot);

        public static void RaiseSimulationReset()
            => OnSimulationReset?.Invoke();

        /// <summary>
        /// Borra TODAS las suscripciones. Llamar solo al desmontar el bootstrap
        /// del simulador para evitar referencias colgantes entre cargas de escena.
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            OnCustomerArrived = null;
            OnCustomerStateChanged = null;
            OnCustomerServed = null;
            OnCustomerAbandoned = null;
            OnCustomerRejected = null;
            OnCustomerSeated = null;
            OnCustomerLeftTable = null;
            OnCustomerLeft = null;
            OnMetricsUpdated = null;
            OnSimulationReset = null;
        }
    }
}
