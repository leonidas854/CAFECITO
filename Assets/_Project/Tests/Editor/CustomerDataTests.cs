using NUnit.Framework;
using CafeSim.Core;

namespace CafeSim.Tests.Core
{
    /// <summary>
    /// Tests de la entidad de datos del cliente. Solo lógica derivada: las
    /// propiedades calculadas <see cref="CustomerData.TimeInSystem"/> y
    /// <see cref="CustomerData.TimeInQueues"/> son las que importa proteger
    /// porque las usan tanto el dashboard como las pruebas estadísticas.
    /// </summary>
    [TestFixture]
    public class CustomerDataTests
    {
        [Test]
        public void NewCustomer_StartsInEnteringState()
        {
            var c = new CustomerData(id: 1, isWebOrder: false, arrivalTime: 12.5f);
            Assert.AreEqual(1, c.Id);
            Assert.IsFalse(c.IsWebOrder);
            Assert.AreEqual(12.5f, c.ArrivalTime);
            Assert.AreEqual(CustomerState.Entering, c.State);
            Assert.AreEqual(-1, c.TableId);
            Assert.AreEqual(-1, c.SeatIndex);
        }

        [Test]
        public void TimeInSystem_IsNullWhileCustomerActive()
        {
            var c = new CustomerData(1, false, 0f);
            Assert.IsNull(c.TimeInSystem, "Sin DepartureTime ni AbandonmentTime debe ser null.");
        }

        [Test]
        public void TimeInSystem_UsesDepartureWhenServed()
        {
            var c = new CustomerData(1, false, 10f) { DepartureTime = 70f };
            Assert.AreEqual(60f, c.TimeInSystem);
        }

        [Test]
        public void TimeInSystem_UsesAbandonmentWhenLeft()
        {
            var c = new CustomerData(1, false, 10f) { AbandonmentTime = 25f };
            Assert.AreEqual(15f, c.TimeInSystem);
        }

        [Test]
        public void TimeInQueues_SumsCashierAndBaristaWaits()
        {
            // Espera 4 s en la caja + 6 s en el barista = 10 s
            var c = new CustomerData(1, false, 0f)
            {
                CashierQueueEnterTime = 0f,
                CashierServiceStartTime = 4f,
                BaristaQueueEnterTime = 10f,
                BaristaServiceStartTime = 16f
            };
            Assert.AreEqual(10f, c.TimeInQueues, 1e-4f);
        }

        [Test]
        public void TimeInQueues_IgnoresQueueWithoutServiceStart()
        {
            // Cliente que abandonó antes de ser atendido: enter sin start ⇒ no cuenta.
            var c = new CustomerData(1, false, 0f)
            {
                CashierQueueEnterTime = 0f,
                CashierServiceStartTime = null
            };
            Assert.AreEqual(0f, c.TimeInQueues);
        }

        [Test]
        public void IsFinished_TrueOnlyForTerminalStates()
        {
            var c = new CustomerData(1, false, 0f);
            Assert.IsFalse(c.IsFinished, "Entering no es terminal.");

            c.State = CustomerState.WaitingInLine;
            Assert.IsFalse(c.IsFinished);

            c.State = CustomerState.Leaving;
            Assert.IsTrue(c.IsFinished);

            c.State = CustomerState.Abandoned;
            Assert.IsTrue(c.IsFinished);

            c.State = CustomerState.Rejected;
            Assert.IsTrue(c.IsFinished);
        }
    }
}
