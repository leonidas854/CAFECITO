using NUnit.Framework;
using CafeSim.Core;
using CafeSim.Core.Metrics;
using CafeSim.Events;

namespace CafeSim.Tests.Integration
{
    /// <summary>
    /// Pruebas de integración que ejecutan la simulación completa por una
    /// ventana larga y contrastan los promedios empíricos contra los valores
    /// que predice la teoría de colas (M/M/1) o contra el escenario opuesto
    /// (físico vs híbrido).
    ///
    /// <para>Convención: las corridas duran lo suficiente como para que los
    /// promedios converjan razonablemente, pero las tolerancias se mantienen
    /// amplias (~15%) porque la naturaleza estocástica del sistema hace que
    /// una corrida única tenga ruido apreciable.</para>
    /// </summary>
    [TestFixture]
    public class SimulationIntegrationTests
    {
        [SetUp]
        public void SetUp() => GameEvents.ClearAllSubscriptions();

        // ─── Helpers ──────────────────────────────────────────────────────────

        private static void RunFor(SimulationManager sim, float totalSeconds, float dt = 0.25f)
        {
            int ticks = (int)(totalSeconds / dt);
            for (int i = 0; i < ticks; i++) sim.Tick(dt);
        }

        /// <summary>
        /// Construye parámetros para aislar el cajero como el único cuello de
        /// botella, permitiendo comparar el cajero contra fórmulas M/M/1.
        /// El barista tiene muchos servidores y los clientes consumen casi
        /// instantáneamente.
        /// </summary>
        private static SimulationParameters CashierAsMm1(float lambdaPerMin, float muPerMin, long seed)
        {
            return new SimulationParameters(
                arrivalRatePerSecond: lambdaPerMin / 60f,
                serviceRateCashierPerSecond: muPerMin / 60f,
                serviceRateBaristaPerSecond: 60f,        // barista efectivamente instantáneo
                averageConsumeTimeSeconds: 0.5f,         // consumo despreciable
                customerPatienceSeconds: 1_000_000f,     // sin abandonos
                cashierCount: 1,
                baristaCount: 30,                        // saturación imposible
                cashierAlsoBarista: false,
                tableCount: 0,
                seatsPerTable: 1,
                webOrderProbability: 0f,                 // todo pasa por la caja
                maxCashierQueueLength: 10_000,
                maxBaristaQueueLength: 10_000,
                maxConcurrentCustomers: 100_000,
                lcgSeed: seed);
        }

        // ─── Tests M/M/1 (cajero como cuello aislado) ─────────────────────────

        [Test]
        public void Cashier_UtilizationConverges_ToMm1Theoretical()
        {
            // λ = 4/min, μ = 12/min ⇒ ρ teórico = 1/3 ≈ 0.333
            var sim = SimulationManager.Instance;
            sim.Configure(CashierAsMm1(lambdaPerMin: 4f, muPerMin: 12f, seed: 4242L));
            RunFor(sim, totalSeconds: 21_600f); // 6 horas simuladas

            float rhoTheoretical = MetricCalculator.Utilization(4f, 12f, 1);
            var snap = sim.GetCurrentSnapshot();
            Assert.That(snap.CashierUtilization, Is.EqualTo(rhoTheoretical).Within(0.05),
                $"ρ empírica = {snap.CashierUtilization:F3}, teórica = {rhoTheoretical:F3}");
        }

        [Test]
        public void Cashier_AverageQueueLength_ConvergesToMm1Theoretical()
        {
            // λ = 6/min, μ = 10/min ⇒ ρ = 0.6, Lq = 0.36/0.4 = 0.9
            var sim = SimulationManager.Instance;
            sim.Configure(CashierAsMm1(lambdaPerMin: 6f, muPerMin: 10f, seed: 909090L));
            RunFor(sim, totalSeconds: 21_600f);

            float lqTheoretical = MetricCalculator.LqForMm1(6f, 10f);
            var snap = sim.GetCurrentSnapshot();
            // Tolerancia del 25% sobre Lq: el segundo momento de la cola tiene varianza alta.
            float absoluteTolerance = lqTheoretical * 0.25f;
            Assert.That(snap.CashierAverageQueueLength,
                Is.EqualTo(lqTheoretical).Within(absoluteTolerance),
                $"Lq empírico = {snap.CashierAverageQueueLength:F3}, teórico = {lqTheoretical:F3}");
        }

        [Test]
        public void Cashier_AverageWaitTime_ConvergesToMm1Theoretical()
        {
            // λ = 6/min = 0.1/s, μ = 10/min = 0.1667/s
            // Wq = Lq/λ = 0.9 / 0.1 = 9 s
            var sim = SimulationManager.Instance;
            sim.Configure(CashierAsMm1(lambdaPerMin: 6f, muPerMin: 10f, seed: 51515L));
            RunFor(sim, totalSeconds: 21_600f);

            float lqTheoretical = MetricCalculator.LqForMm1(6f, 10f);
            float wqTheoretical = MetricCalculator.WqFromLq(lqTheoretical, 6f / 60f); // ≈ 9 s
            var snap = sim.GetCurrentSnapshot();
            // El tiempo medio agregado en colas es prácticamente todo cola de caja
            // en esta configuración (barista sin cuello). Tolerancia 25%.
            Assert.That(snap.AverageTimeInQueues,
                Is.EqualTo(wqTheoretical).Within(wqTheoretical * 0.25f),
                $"Wq empírico = {snap.AverageTimeInQueues:F2}s, teórico = {wqTheoretical:F2}s");
        }

        // ─── Tests Físico vs Híbrido (pregunta de investigación) ──────────────

        [Test]
        public void HybridScenario_ReducesCashierLoad_VsPhysicalOnly()
        {
            // Mismas tasas y semilla. En híbrido el 50% de los clientes bypassa
            // la caja, así que la cola del cajero crece menos.
            var sim = SimulationManager.Instance;

            var physical = BuildScenario(webProb: 0f, seed: 31415L);
            sim.Configure(physical);
            RunFor(sim, totalSeconds: 3_600f);
            float queuePhysical = sim.GetCurrentSnapshot().CashierAverageQueueLength;
            float utilPhysical  = sim.GetCurrentSnapshot().CashierUtilization;

            var hybrid = BuildScenario(webProb: 0.5f, seed: 31415L);
            sim.Configure(hybrid);
            RunFor(sim, totalSeconds: 3_600f);
            float queueHybrid = sim.GetCurrentSnapshot().CashierAverageQueueLength;
            float utilHybrid  = sim.GetCurrentSnapshot().CashierUtilization;

            Assert.Less(utilHybrid, utilPhysical,
                $"Utilización del cajero debería bajar al introducir pedidos web. " +
                $"Físico={utilPhysical:F3}, Híbrido={utilHybrid:F3}");
            Assert.LessOrEqual(queueHybrid, queuePhysical,
                $"Cola del cajero debería ser ≤ con pedidos web. " +
                $"Físico={queuePhysical:F3}, Híbrido={queueHybrid:F3}");
        }

        [Test]
        public void SameSeedSameScenario_ProducesIdenticalAggregateMetrics()
        {
            // Reproducibilidad a nivel agregado: dos corridas idénticas
            // deben dar exactamente los mismos contadores y la misma utilización.
            var sim = SimulationManager.Instance;
            var config = BuildScenario(webProb: 0.3f, seed: 88888L);

            sim.Configure(config);
            RunFor(sim, totalSeconds: 1_800f);
            var a = sim.GetCurrentSnapshot();
            int servedA = sim.ServedCount;

            sim.Configure(config);
            RunFor(sim, totalSeconds: 1_800f);
            var b = sim.GetCurrentSnapshot();
            int servedB = sim.ServedCount;

            Assert.AreEqual(a.ArrivedCount, b.ArrivedCount);
            Assert.AreEqual(servedA, servedB);
            Assert.AreEqual(a.CashierUtilization, b.CashierUtilization, 1e-4f);
            Assert.AreEqual(a.BaristaUtilization, b.BaristaUtilization, 1e-4f);
        }

        private static SimulationParameters BuildScenario(float webProb, long seed)
        {
            // λ = 5/min, μ_caja = 12/min, 1 cajero, 2 baristas — sistema estable.
            return new SimulationParameters(
                arrivalRatePerSecond: 5f / 60f,
                serviceRateCashierPerSecond: 12f / 60f,
                serviceRateBaristaPerSecond: 3f / 60f,
                averageConsumeTimeSeconds: 180f,
                customerPatienceSeconds: 120f,
                cashierCount: 1,
                baristaCount: 2,
                cashierAlsoBarista: false,
                tableCount: 6,
                seatsPerTable: 4,
                webOrderProbability: webProb,
                maxCashierQueueLength: 30,
                maxBaristaQueueLength: 30,
                maxConcurrentCustomers: 80,
                lcgSeed: seed);
        }
    }
}
