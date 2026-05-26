using NUnit.Framework;
using CafeSim.Core;
using CafeSim.Core.Queue;

namespace CafeSim.Tests.Core
{
    /// <summary>
    /// Tests del routing dual: pedidos físicos van a la cola de la caja,
    /// pedidos web van directo a la cola del barista.
    /// </summary>
    [TestFixture]
    public class OrderSystemTests
    {
        private QueueController<CustomerData> _cashier;
        private QueueController<CustomerData> _barista;
        private OrderSystem _system;

        [SetUp]
        public void SetUp()
        {
            _cashier = new QueueController<CustomerData>(new FifoQueue<CustomerData>());
            _barista = new QueueController<CustomerData>(new FifoQueue<CustomerData>());
            _system = new OrderSystem(_cashier, _barista);
        }

        [Test]
        public void RouteOnArrival_PhysicalOrder_EntersCashierQueue()
        {
            var customer = new CustomerData(id: 1, isWebOrder: false, arrivalTime: 0f);
            _system.RouteOnArrival(customer, currentTime: 0f);

            Assert.AreEqual(1, _cashier.Length);
            Assert.AreEqual(0, _barista.Length);
            Assert.AreEqual(CustomerState.WaitingInLine, customer.State);
            Assert.AreEqual(0f, customer.CashierQueueEnterTime);
        }

        [Test]
        public void RouteOnArrival_WebOrder_BypassesCashierAndEntersBaristaQueue()
        {
            var customer = new CustomerData(id: 2, isWebOrder: true, arrivalTime: 5f);
            _system.RouteOnArrival(customer, currentTime: 5f);

            Assert.AreEqual(0, _cashier.Length);
            Assert.AreEqual(1, _barista.Length);
            Assert.AreEqual(CustomerState.WaitingDrink, customer.State);
            Assert.AreEqual(5f, customer.BaristaQueueEnterTime);
            Assert.IsNull(customer.CashierQueueEnterTime, "Pedido web no debe pasar por caja.");
        }

        [Test]
        public void RouteAfterCashier_MovesCustomerToBaristaQueue()
        {
            var customer = new CustomerData(id: 3, isWebOrder: false, arrivalTime: 0f);
            _system.RouteOnArrival(customer, 0f);
            _cashier.TryDequeue(currentTime: 12f, out var _); // simulamos que ya lo atendieron
            _system.RouteAfterCashier(customer, currentTime: 12f);

            Assert.AreEqual(0, _cashier.Length);
            Assert.AreEqual(1, _barista.Length);
            Assert.AreEqual(CustomerState.WaitingDrink, customer.State);
            Assert.AreEqual(12f, customer.BaristaQueueEnterTime);
        }
    }
}
