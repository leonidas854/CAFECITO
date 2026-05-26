using System;
using NUnit.Framework;
using CafeSim.Core;

namespace CafeSim.Tests.Core
{
    /// <summary>
    /// Tests del POCO de parámetros. La invariante crítica es la validación
    /// estricta de cada campo: cualquier valor inválido debe lanzar antes de
    /// que llegue al SimulationManager y produzca NaN o ciclos infinitos.
    /// </summary>
    [TestFixture]
    public class SimulationParametersTests
    {
        // Plantilla con valores válidos a partir de la cual mutamos un campo en cada test.
        private static SimulationParameters BuildValid(
            float arrivalRatePerSecond = 0.1f,
            float serviceRateCashierPerSecond = 0.2f,
            float serviceRateBaristaPerSecond = 0.05f,
            float averageConsumeTimeSeconds = 60f,
            float customerPatienceSeconds = 90f,
            int cashierCount = 1,
            int baristaCount = 1,
            bool cashierAlsoBarista = false,
            int tableCount = 4,
            int seatsPerTable = 4,
            float webOrderProbability = 0f,
            int maxCashierQueueLength = 10,
            int maxBaristaQueueLength = 10,
            int maxConcurrentCustomers = 50,
            long lcgSeed = 12345L)
        {
            return new SimulationParameters(
                arrivalRatePerSecond,
                serviceRateCashierPerSecond,
                serviceRateBaristaPerSecond,
                averageConsumeTimeSeconds,
                customerPatienceSeconds,
                cashierCount,
                baristaCount,
                cashierAlsoBarista,
                tableCount,
                seatsPerTable,
                webOrderProbability,
                maxCashierQueueLength,
                maxBaristaQueueLength,
                maxConcurrentCustomers,
                lcgSeed);
        }

        [Test]
        public void Constructor_StoresAllValues()
        {
            var p = BuildValid(
                arrivalRatePerSecond: 0.5f,
                serviceRateCashierPerSecond: 0.3f,
                cashierCount: 2,
                webOrderProbability: 0.25f,
                lcgSeed: 7777L);

            Assert.AreEqual(0.5f, p.ArrivalRatePerSecond);
            Assert.AreEqual(0.3f, p.ServiceRateCashierPerSecond);
            Assert.AreEqual(2, p.CashierCount);
            Assert.AreEqual(0.25f, p.WebOrderProbability);
            Assert.AreEqual(7777L, p.LcgSeed);
        }

        [Test]
        public void Constructor_RejectsNonPositiveRates()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BuildValid(arrivalRatePerSecond: 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => BuildValid(arrivalRatePerSecond: -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => BuildValid(serviceRateCashierPerSecond: 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => BuildValid(serviceRateBaristaPerSecond: 0f));
        }

        [Test]
        public void Constructor_RejectsNaNOrInfinity()
        {
            Assert.Throws<ArgumentException>(
                () => BuildValid(arrivalRatePerSecond: float.NaN));
            Assert.Throws<ArgumentException>(
                () => BuildValid(serviceRateCashierPerSecond: float.PositiveInfinity));
        }

        [Test]
        public void Constructor_RejectsWebProbabilityOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BuildValid(webOrderProbability: -0.01f));
            Assert.Throws<ArgumentOutOfRangeException>(() => BuildValid(webOrderProbability: 1.01f));
            Assert.DoesNotThrow(() => BuildValid(webOrderProbability: 0f));
            Assert.DoesNotThrow(() => BuildValid(webOrderProbability: 1f));
        }

        [Test]
        public void Constructor_RejectsZeroOrNegativeServerCounts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BuildValid(cashierCount: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => BuildValid(baristaCount: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => BuildValid(seatsPerTable: 0));
        }

        [Test]
        public void Constructor_AllowsZeroTables_ForStandingOnlyScenarios()
        {
            Assert.DoesNotThrow(() => BuildValid(tableCount: 0));
        }

        [Test]
        public void Constructor_RejectsNegativeTableCount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BuildValid(tableCount: -1));
        }

        [Test]
        public void Constructor_RejectsNonPositiveSeed()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BuildValid(lcgSeed: 0L));
            Assert.Throws<ArgumentOutOfRangeException>(() => BuildValid(lcgSeed: -1L));
        }

        [Test]
        public void FromPerMinuteRates_ConvertsToPerSecondCorrectly()
        {
            var p = SimulationParameters.FromPerMinuteRates(
                arrivalRatePerMinute: 60f,
                serviceRateCashierPerMinute: 30f,
                serviceRateBaristaPerMinute: 12f,
                averageConsumeTimeSeconds: 60f,
                customerPatienceSeconds: 90f,
                cashierCount: 1,
                baristaCount: 1,
                cashierAlsoBarista: false,
                tableCount: 4,
                seatsPerTable: 4,
                webOrderProbability: 0f,
                maxCashierQueueLength: 10,
                maxBaristaQueueLength: 10,
                maxConcurrentCustomers: 50,
                lcgSeed: 1L);

            Assert.AreEqual(1f, p.ArrivalRatePerSecond, 1e-5f);
            Assert.AreEqual(0.5f, p.ServiceRateCashierPerSecond, 1e-5f);
            Assert.AreEqual(0.2f, p.ServiceRateBaristaPerSecond, 1e-5f);
        }
    }
}
