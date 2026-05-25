using System.Collections.Generic;

namespace CafeSim.Core.Queue
{
    /// <summary>
    /// Implementación FIFO (First-In-First-Out) de <see cref="IQueueDiscipline{T}"/>.
    /// Es la disciplina canónica de una caja registradora o de un mostrador de barista.
    /// </summary>
    public sealed class FifoQueue<T> : IQueueDiscipline<T>
    {
        private readonly LinkedList<T> _items = new LinkedList<T>();

        public int Count => _items.Count;

        public IEnumerable<T> Items => _items;

        public void Enqueue(T item) => _items.AddLast(item);

        public bool TryDequeue(out T item)
        {
            if (_items.Count == 0)
            {
                item = default;
                return false;
            }
            item = _items.First.Value;
            _items.RemoveFirst();
            return true;
        }

        public bool TryPeek(out T item)
        {
            if (_items.Count == 0)
            {
                item = default;
                return false;
            }
            item = _items.First.Value;
            return true;
        }

        public bool TryRemove(T item)
        {
            var node = _items.Find(item);
            if (node == null) return false;
            _items.Remove(node);
            return true;
        }

        public void Clear() => _items.Clear();
    }
}
