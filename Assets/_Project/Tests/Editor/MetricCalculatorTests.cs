using System;
using System.Collections.Generic;
using NUnit.Framework;
using CafeSim.Core;
using CafeSim.Core.Metrics;

namespace CafeSim.Tests.Core
{
    /// <summary>
    /// Tests de las fórmulas teóricas de M/M/1 y M/M/c, contrastadas contra
    /// valores calculados a mano. Estos tests valen la pena para asegurar
    /// que las métricas mostradas en el dashboard coincidan con ProModel.
    /// </summary>
    [TestFixture]
    public class MetricCalculatorTests
    {
        [Test]
        public void Utilization_ComputesCorrectly_ForSingleServer()
        {
            // ρ = λ / (c·μ) = 5 / (1 · 10) = 0.5
            float rho = MetricCalculator.Utilization(5f, 10f, 1);
            Assert.AreEqual(0.5f, rho, 1e-5f);
        }

        [Test]
        public void Utilization_ComputesCorrectly_ForMultipleServers()
        {
            // ρ = 12 / (3 · 5) = 0.8
            float rho = MetricCalculator.Utilization(12f, 5f, 3);
            Assert.AreEqual(0.8f, rho, 1e-5f);
        }

        [Test]
        public void LqForMm1_AgreesWithFormulaByHand()
        {
            // M/M/1: λ=8, μ=10  ⇒ ρ=0.8  ⇒ Lq = ρ²/(1-ρ) = 0.64/0.2 = 3.2
            float lq = MetricCalculator.LqForMm1(8f, 10f);
            Assert.AreEqual(3.2f, lq, 1e-3f);
        }

        [Test]
        public void LqForMm1_ThrowsWhenSystemIsUnstable()
        {
            // ρ ≥ 1 ⇒ sistema inestable
            Assert.Throws<InvalidOperationException>(
                () => MetricCalculator.LqForMm1(10f, 10f));
            Assert.Throws<InvalidOperationException>(
                () => MetricCalculator.LqForMm1(15f, 10f));
        }

        [Test]
        public void LqForMmc_DecreasesWithMoreServers()
        {
            // Mismo λ y μ, más servidores ⇒ menor Lq.
            float lqOne = MetricCalculator.LqForMmc(8f, 10f, 1);
            float lqTwo = MetricCalculator.LqForMmc(8f, 10f, 2);
            float lqThree = MetricCalculator.LqForMmc(8f, 10f, 3);

            Assert.Greater(lqOne, lqTwo, "Lq debería bajar al agregar el 2do servidor.");
            Assert.Greater(lqTwo, lqThree, "Lq debería bajar al agregar el 3er servidor.");
        }

        [Test]
        public void WqFromLq_AppliesLittlesLaw()
        {
            // Wq = Lq / λ = 4 / 8 = 0.5
            float wq = MetricCalculator.WqFromLq(lq: 4f, arrivalRate: 8f);
            Assert.AreEqual(0.5f, wq, 1e-5f);
        }

        [Test]
        public void WFromWq_AddsServiceTime()
        {
            // W = Wq + 1/μ = 0.5 + 0.1 = 0.6
            float w = MetricCalculator.WFromWq(wq: 0.5f, serviceRate: 10f);
            Assert.AreEqual(0.6f, w, 1e-5f);
        }

        [Test]
        public void EmpiricalAverages_HandleEmptyAndNullInputs()
        {
            Assert.AreEqual(0f, MetricCalculator.AverageTimeInQueues(null));
            Assert.AreEqual(0f, MetricCalculator.AverageTimeInSystem(null));
            Assert.AreEqual(0f, MetricCalculator.AverageTimeInQueues(new List<CustomerData>()));
            Assert.AreEqual(0f, MetricCalculator.AverageTimeInSystem(new List<CustomerData>()));
        }

        [Test]
        public void AverageTimeInSystem_OnlyConsidersDepartedCustomers()
        {
            // Tres clientes: dos atendidos, uno abandonó (sin DepartureTime).
            // El promedio debe ignorar al que abandonó.
            var c1 = new CustomerData(1, false, 0f) { DepartureTime = 10f };
            var c2 = new CustomerData(2, false, 5f) { DepartureTime = 25f };
            var c3 = new CustomerData(3, false, 8f) { AbandonmentTime = 20f };
            var avg = MetricCalculator.AverageTimeInSystem(new List<CustomerData> { c1, c2, c3 });
            // (10 + 20) / 2 = 15
            Assert.AreEqual(15f, avg, 1e-3f);
        }
    }
}
