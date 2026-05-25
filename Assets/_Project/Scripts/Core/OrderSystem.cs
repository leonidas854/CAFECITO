using System;
using CafeSim.Core.Queue;

namespace CafeSim.Core
{
    /// <summary>
    /// Ruteador del flujo dual de pedidos:
    /// <list type="bullet">
    ///   <item>Pedido físico (probabilidad <c>1 - p_w</c>):
    ///     Entrada → Cola_Caja → Caja → Cola_Barista → Barista → Mesa.</item>
    ///   <item>Pedido web (probabilidad <c>p_w</c>):
    ///     Entrada → Cola_Barista → Barista → Mesa (bypassa la caja).</item>
    /// </list>
    ///
    /// Esta clase es delgada a propósito: solo decide a qué cola entra cada
    /// cliente al llegar y a cuál pasa después de la caja. El avance temporal
    /// y los servidores los maneja <c>SimulationManager</c>.
    /// </summary>
    public sealed class OrderSystem
    {
        private readonly QueueController<CustomerData> _cashierQueue;
        private readonly QueueController<CustomerData> _baristaQueue;

        public OrderSystem(
            QueueController<CustomerData> cashierQueue,
            QueueController<CustomerData> baristaQueue)
        {
            _cashierQueue = cashierQueue ?? throw new ArgumentNullException(nameof(cashierQueue));
            _baristaQueue = baristaQueue ?? throw new ArgumentNullException(nameof(baristaQueue));
        }

        /// <summary>
        /// Coloca un cliente recién llegado en la cola correspondiente según el
        /// tipo de pedido. Actualiza su estado y timestamp de entrada a la cola.
        /// </summary>
        public void RouteOnArrival(CustomerData customer, float currentTime)
        {
            if (customer == null) throw new ArgumentNullException(nameof(customer));

            if (customer.IsWebOrder)
            {
                customer.State = CustomerState.WaitingDrink;
                customer.BaristaQueueEnterTime = currentTime;
                _baristaQueue.Enqueue(customer, currentTime);
            }
            else
            {
                customer.State = CustomerState.WaitingInLine;
                customer.CashierQueueEnterTime = currentTime;
                _cashierQueue.Enqueue(customer, currentTime);
            }
        }

        /// <summary>
        /// Mueve un cliente que terminó en la caja a la cola del barista.
        /// </summary>
        public void RouteAfterCashier(CustomerData customer, float currentTime)
        {
            if (customer == null) throw new ArgumentNullException(nameof(customer));
            customer.State = CustomerState.WaitingDrink;
            customer.BaristaQueueEnterTime = currentTime;
            _baristaQueue.Enqueue(customer, currentTime);
        }
    }
}
