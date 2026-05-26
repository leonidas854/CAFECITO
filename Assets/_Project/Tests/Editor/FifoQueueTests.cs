using NUnit.Framework;
using CafeSim.Core.Queue;

namespace CafeSim.Tests.Core.Queue
{
    /// <summary>
    /// Tests de la implementación FIFO de la cola.
    /// </summary>
    [TestFixture]
    public class FifoQueueTests
    {
        [Test]
        public void Enqueue_Dequeue_RespectsFifoOrder()
        {
            var q = new FifoQueue<int>();
            q.Enqueue(1);
            q.Enqueue(2);
            q.Enqueue(3);

            Assert.IsTrue(q.TryDequeue(out int a) && a == 1);
            Assert.IsTrue(q.TryDequeue(out int b) && b == 2);
            Assert.IsTrue(q.TryDequeue(out int c) && c == 3);
            Assert.IsFalse(q.TryDequeue(out _));
        }

        [Test]
        public void TryPeek_DoesNotRemoveItem()
        {
            var q = new FifoQueue<string>();
            q.Enqueue("a");
            Assert.IsTrue(q.TryPeek(out string peeked) && peeked == "a");
            Assert.AreEqual(1, q.Count);
            Assert.IsTrue(q.TryDequeue(out string dequeued) && dequeued == "a");
        }

        [Test]
        public void TryRemove_FromMiddleSucceeds()
        {
            var q = new FifoQueue<int>();
            q.Enqueue(10);
            q.Enqueue(20);
            q.Enqueue(30);

            Assert.IsTrue(q.TryRemove(20));
            Assert.AreEqual(2, q.Count);

            Assert.IsTrue(q.TryDequeue(out int a) && a == 10);
            Assert.IsTrue(q.TryDequeue(out int b) && b == 30);
        }

        [Test]
        public void TryRemove_ReturnsFalseWhenItemNotPresent()
        {
            var q = new FifoQueue<int>();
            q.Enqueue(1);
            Assert.IsFalse(q.TryRemove(99));
            Assert.AreEqual(1, q.Count);
        }

        [Test]
        public void Clear_EmptiesTheQueue()
        {
            var q = new FifoQueue<int>();
            q.Enqueue(1);
            q.Enqueue(2);
            q.Clear();
            Assert.AreEqual(0, q.Count);
            Assert.IsFalse(q.TryDequeue(out _));
        }
    }
}
