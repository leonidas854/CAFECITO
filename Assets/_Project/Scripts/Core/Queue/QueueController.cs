using System;
using System.Collections.Generic;

namespace CafeSim.Core.Queue
{
    /// <summary>
    /// Fachada sobre una <see cref="IQueueDiscipline{T}"/> que además registra
    /// estadísticas de la cola: cantidad de arribos, despachos, abandonos, y la
    /// integral del tamaño de la cola en el tiempo (necesaria para Lq empírico).
    ///
    /// El <c>SimulationManager</c> debe llamar a <see cref="AdvanceTime"/> en
    /// cada tick para acumular correctamente el área bajo la curva.
    /// </summary>
    public sealed class QueueController<T>
    {
        private readonly IQueueDiscipline<T> _discipline;

        private float _lastUpdateTime;
        private double _areaUnderLength;
        private long _enqueuedCount;
        private long _dequeuedCount;
        private long _removedCount;
        private int _maxLength;

        public QueueController(IQueueDiscipline<T> discipline)
        {
            _discipline = discipline ?? throw new ArgumentNullException(nameof(discipline));
        }

        /// <summary>Cantidad actual de elementos en la cola.</summary>
        public int Length => _discipline.Count;

        /// <summary>Acumulado histórico de elementos encolados.</summary>
        public long EnqueuedCount => _enqueuedCount;

        /// <summary>Acumulado histórico de elementos despachados (dequeue).</summary>
        public long DequeuedCount => _dequeuedCount;

        /// <summary>Acumulado histórico de elementos removidos por abandono.</summary>
        public long RemovedCount => _removedCount;

        /// <summary>Mayor longitud observada hasta el momento.</summary>
        public int MaxLength => _maxLength;

        /// <summary>
        /// Promedio temporal de la longitud de cola desde el último <see cref="Reset"/>.
        /// Es el Lq empírico (medido).
        /// </summary>
        public float AverageLength(float currentTime)
        {
            if (currentTime <= 0f) return 0f;
            return (float)(_areaUnderLength / currentTime);
        }

        /// <summary>Iterador de solo lectura sobre los elementos.</summary>
        public IEnumerable<T> Snapshot() => _discipline.Items;

        // ─── Mutaciones ────────────────────────────────────────────────────────

        public void Enqueue(T item, float currentTime)
        {
            AdvanceTime(currentTime);
            _discipline.Enqueue(item);
            _enqueuedCount++;
            if (_discipline.Count > _maxLength) _maxLength = _discipline.Count;
        }

        public bool TryDequeue(float currentTime, out T item)
        {
            AdvanceTime(currentTime);
            bool ok = _discipline.TryDequeue(out item);
            if (ok) _dequeuedCount++;
            return ok;
        }

        public bool TryPeek(out T item) => _discipline.TryPeek(out item);

        public bool TryRemove(T item, float currentTime)
        {
            AdvanceTime(currentTime);
            bool ok = _discipline.TryRemove(item);
            if (ok) _removedCount++;
            return ok;
        }

        /// <summary>
        /// Acumula el área bajo la longitud de cola entre el último tiempo
        /// registrado y <paramref name="currentTime"/>. Idempotente si se llama
        /// varias veces con el mismo instante.
        /// </summary>
        public void AdvanceTime(float currentTime)
        {
            if (currentTime <= _lastUpdateTime) return;
            float dt = currentTime - _lastUpdateTime;
            _areaUnderLength += _discipline.Count * (double)dt;
            _lastUpdateTime = currentTime;
        }

        /// <summary>
        /// Reinicia la cola y sus contadores históricos. Llamar al iniciar una
        /// corrida nueva desde el SimulationManager.
        /// </summary>
        public void Reset()
        {
            _discipline.Clear();
            _lastUpdateTime = 0f;
            _areaUnderLength = 0d;
            _enqueuedCount = 0;
            _dequeuedCount = 0;
            _removedCount = 0;
            _maxLength = 0;
        }
    }
}
