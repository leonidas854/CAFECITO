using NUnit.Framework;
using CafeSim.Core.Queue;

namespace CafeSim.Tests.Core.Queue
{
    /// <summary>
    /// Tests del wrapper <see cref="QueueController{T}"/>. Foco en la
    /// integral temporal usada para calcular Lq empírico.
    /// </summary>
    [TestFixture]
    public class QueueControllerTests
    {
        [Test]
        public void Counters_TrackEnqueueDequeueAndRemove()
        {
            var q = new QueueController<int>(new FifoQueue<int>());
            q.Enqueue(1, currentTime: 0f);
            q.Enqueue(2, currentTime: 0f);
            q.Enqueue(3, currentTime: 0f);
            q.TryDequeue(0f, out _);
            q.TryRemove(2, currentTime: 0f);

            Assert.AreEqual(3, q.EnqueuedCount);
            Assert.AreEqual(1, q.DequeuedCount);
            Assert.AreEqual(1, q.RemovedCount);
            Assert.AreEqual(1, q.Length);
        }

        [Test]
        public void MaxLength_ReflectsHighestObservedSize()
        {
            var q = new QueueController<int>(new FifoQueue<int>());
            q.Enqueue(1, 0f);
            q.Enqueue(2, 0f);
            q.Enqueue(3, 0f); // max alcanza 3
            q.TryDequeue(0f, out _);
            q.TryDequeue(0f, out _); // ahora longitud 1, max sigue siendo 3
            Assert.AreEqual(3, q.MaxLength);
        }

        [Test]
        public void AverageLength_ComputesTimeWeightedMean()
        {
            // Escenario: 1 ítem entre t=0 y t=10, 3 ítems entre t=10 y t=30.
            // Área = 1·10 + 3·20 = 70. Promedio = 70 / 30 = 2.333...
            var q = new QueueController<int>(new FifoQueue<int>());
            q.Enqueue(1, currentTime: 0f);
            q.AdvanceTime(10f);
            q.Enqueue(2, currentTime: 10f);
            q.Enqueue(3, currentTime: 10f);
            q.AdvanceTime(30f);

            float avg = q.AverageLength(30f);
            Assert.AreEqual(70f / 30f, avg, 1e-3f);
        }

        [Test]
        public void Reset_ClearsStateAndCounters()
        {
            var q = new QueueController<int>(new FifoQueue<int>());
            q.Enqueue(1, 0f);
            q.Enqueue(2, 0f);
            q.AdvanceTime(5f);
            q.Reset();

            Assert.AreEqual(0, q.Length);
            Assert.AreEqual(0, q.EnqueuedCount);
            Assert.AreEqual(0, q.MaxLength);
            Assert.AreEqual(0f, q.AverageLength(10f));
        }
    }
}
