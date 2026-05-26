using NUnit.Framework;
using CafeSim.Core;
using CafeSim.Events;

namespace CafeSim.Tests.Core
{
    /// <summary>
    /// Tests end-to-end del orquestador. Validan que el flujo completo
    /// (llegada → cola → servicio → consumo → salida) funciona, que la
    /// reproducibilidad con semilla fija se mantiene, y que los límites
    /// duros protegen el sistema.
    /// </summary>
    [TestFixture]
    public class SimulationManagerTests
    {
        [SetUp]
        public void SetUp()
        {
            // Limpiar suscripciones para que no haya interferencia entre tests.
            GameEvents.ClearAllSubscriptions();
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static SimulationParameters BuildDefaultParams(
            float arrivalRatePerSec = 1f,
            int cashierCount = 1,
            int baristaCount = 1,
            int tableCount = 4,
            int seatsPerTable = 4,
            int maxConcurrent = 50,
            float patience = 120f,
            bool multiSkill = false,
            long seed = 12345L)
        {
            return new SimulationParameters(
                arrivalRatePerSecond: arrivalRatePerSec,
                serviceRateCashierPerSecond: 1f,
                serviceRateBaristaPerSecond: 1f / 30f,
                averageConsumeTimeSeconds: 60f,
                customerPatienceSeconds: patience,
                cashierCount: cashierCount,
                baristaCount: baristaCount,
                cashierAlsoBarista: multiSkill,
                tableCount: tableCount,
                seatsPerTable: seatsPerTable,
                webOrderProbability: 0f,
                maxCashierQueueLength: 10,
                maxBaristaQueueLength: 10,
                maxConcurrentCustomers: maxConcurrent,
                lcgSeed: seed,
                metricsEmitIntervalSeconds: 1f);
        }

        private static void RunFor(SimulationManager sim, float totalSeconds, float dt = 0.1f)
        {
            int ticks = (int)(totalSeconds / dt);
            for (int i = 0; i < ticks; i++) sim.Tick(dt);
        }

        // ─── Tests ───────────────────────────────────────────────────────────

        [Test]
        public void SameSeed_ProducesSameFinalCounts()
        {
            var sim = SimulationManager.Instance;

            sim.Configure(BuildDefaultParams(seed: 7777L));
            RunFor(sim, totalSeconds: 600f);
            int arrivedA = sim.ArrivedCount;
            int servedA = sim.ServedCount;
            int abandonedA = sim.AbandonedCount;
            int rejectedA = sim.RejectedCount;

            sim.Configure(BuildDefaultParams(seed: 7777L));
            RunFor(sim, totalSeconds: 600f);
            int arrivedB = sim.ArrivedCount;
            int servedB = sim.ServedCount;
            int abandonedB = sim.AbandonedCount;
            int rejectedB = sim.RejectedCount;

            Assert.AreEqual(arrivedA, arrivedB, "ArrivedCount difiere entre corridas con la misma semilla.");
            Assert.AreEqual(servedA, servedB, "ServedCount difiere entre corridas con la misma semilla.");
            Assert.AreEqual(abandonedA, abandonedB, "AbandonedCount difiere entre corridas con la misma semilla.");
            Assert.AreEqual(rejectedA, rejectedB, "RejectedCount difiere entre corridas con la misma semilla.");
        }

        [Test]
        public void ActiveCustomers_NeverExceedHardLimit()
        {
            // λ muy alto (5/s) y maxConcurrent bajo (8). El sistema debe rechazar
            // a partir de cierto punto y nunca tener más de 8 clientes activos.
            var sim = SimulationManager.Instance;
            sim.Configure(BuildDefaultParams(
                arrivalRatePerSec: 5f,
                cashierCount: 1,
                baristaCount: 1,
                maxConcurrent: 8,
                seed: 1234L));

            for (int i = 0; i < 600; i++)
            {
                sim.Tick(0.1f);
                Assert.LessOrEqual(sim.ActiveCustomerCount, 8,
                    $"Tick {i}: ActiveCustomerCount = {sim.ActiveCustomerCount} (límite=8)");
            }

            Assert.Greater(sim.RejectedCount, 0,
                "Con λ alto y límite bajo deberían haber rechazos.");
        }

        [Test]
        public void TickAdvancesSimulationTime()
        {
            var sim = SimulationManager.Instance;
            sim.Configure(BuildDefaultParams(seed: 9999L));
            sim.Tick(0.5f);
            sim.Tick(1.5f);
            Assert.AreEqual(2.0f, sim.SimulationTime, 1e-4f);
        }

        [Test]
        public void Reset_BringsCountersBackToZero()
        {
            var sim = SimulationManager.Instance;
            sim.Configure(BuildDefaultParams(seed: 5555L));
            RunFor(sim, totalSeconds: 60f);

            Assert.Greater(sim.ArrivedCount, 0, "Debería haber llegado al menos un cliente.");

            sim.Reset();
            Assert.AreEqual(0, sim.ArrivedCount);
            Assert.AreEqual(0, sim.ServedCount);
            Assert.AreEqual(0, sim.AbandonedCount);
            Assert.AreEqual(0, sim.RejectedCount);
            Assert.AreEqual(0f, sim.SimulationTime);
        }

        [Test]
        public void NoExceptions_OnLongRun()
        {
            // Ejecutar 1 hora simulada (3600 s) con tasa moderada no debe lanzar.
            var sim = SimulationManager.Instance;
            sim.Configure(BuildDefaultParams(
                arrivalRatePerSec: 0.5f,
                cashierCount: 2,
                baristaCount: 2,
                tableCount: 8,
                seatsPerTable: 4,
                seed: 31415L));

            Assert.DoesNotThrow(() => RunFor(sim, totalSeconds: 3600f));
            Assert.Greater(sim.ArrivedCount, 0);
        }

        [Test]
        public void Abandonment_HappensWhenPatienceIsTinyAndQueueIsSlow()
        {
            // Paciencia minúscula (1 s) y servicio muy lento ⇒ todos abandonan.
            var sim = SimulationManager.Instance;
            sim.Configure(new SimulationParameters(
                arrivalRatePerSecond: 2f,
                serviceRateCashierPerSecond: 0.01f,
                serviceRateBaristaPerSecond: 0.01f,
                averageConsumeTimeSeconds: 60f,
                customerPatienceSeconds: 1f,
                cashierCount: 1,
                baristaCount: 1,
                cashierAlsoBarista: false,
                tableCount: 4,
                seatsPerTable: 4,
                webOrderProbability: 0f,
                maxCashierQueueLength: 30,
                maxBaristaQueueLength: 30,
                maxConcurrentCustomers: 100,
                lcgSeed: 999L));

            RunFor(sim, totalSeconds: 30f);
            Assert.Greater(sim.AbandonedCount, 0,
                "Con paciencia=1s y servicio muy lento debería haber abandonos.");
        }

        [Test]
        public void MultiSkillMode_AllowsCashiersToHelpBaristas()
        {
            // Configuración favorable: caja rápida, barista lento, modo multi-skill.
            // En modo NO multi-skill se acumula la cola del barista.
            // En modo multi-skill, los cajeros ayudan y se atiende a más clientes.
            // Comparamos servedCount entre los dos escenarios con la misma semilla.
            var paramsNoMulti = BuildDefaultParams(
                arrivalRatePerSec: 0.5f,
                cashierCount: 2,
                baristaCount: 1,
                multiSkill: false,
                seed: 4242L);
            var paramsMulti = BuildDefaultParams(
                arrivalRatePerSec: 0.5f,
                cashierCount: 2,
                baristaCount: 1,
                multiSkill: true,
                seed: 4242L);

            var sim = SimulationManager.Instance;
            sim.Configure(paramsNoMulti);
            RunFor(sim, totalSeconds: 600f);
            int servedNoMulti = sim.ServedCount;

            sim.Configure(paramsMulti);
            RunFor(sim, totalSeconds: 600f);
            int servedMulti = sim.ServedCount;

            Assert.GreaterOrEqual(servedMulti, servedNoMulti,
                $"Multi-skill debería atender ≥ que modo separado. Multi={servedMulti}, Sep={servedNoMulti}");
        }

        [Test]
        public void ConsumedStanding_HappensWhenNoTablesAvailable()
        {
            // Sin mesas pero con servidores: todos los atendidos consumen de pie.
            var sim = SimulationManager.Instance;
            sim.Configure(BuildDefaultParams(
                arrivalRatePerSec: 0.5f,
                tableCount: 0,
                seatsPerTable: 1,
                seed: 9090L));

            bool sawStanding = false;
            GameEvents.OnCustomerServed += c =>
            {
                if (c.ConsumedStanding) sawStanding = true;
            };

            RunFor(sim, totalSeconds: 300f);
            Assert.IsTrue(sawStanding,
                "Con 0 mesas y clientes atendidos, alguno debe haber consumido de pie.");
        }

        [Test]
        public void Configure_RaisesSimulationResetEvent()
        {
            int resets = 0;
            GameEvents.OnSimulationReset += () => resets++;

            var sim = SimulationManager.Instance;
            sim.Configure(BuildDefaultParams(seed: 1L));
            sim.Configure(BuildDefaultParams(seed: 2L));

            Assert.AreEqual(2, resets, "OnSimulationReset debió dispararse dos veces.");
        }
    }
}
