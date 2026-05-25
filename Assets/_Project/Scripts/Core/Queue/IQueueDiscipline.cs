using System.Collections.Generic;

namespace CafeSim.Core.Queue
{
    /// <summary>
    /// Contrato de una disciplina de cola. Permite intercambiar la política de
    /// despacho (FIFO, LIFO, prioridad) sin tocar el código que consume colas.
    ///
    /// El parámetro genérico es el tipo del elemento; en este proyecto será
    /// <see cref="CustomerData"/>.
    /// </summary>
    public interface IQueueDiscipline<T>
    {
        /// <summary>Cantidad actual de elementos en la cola.</summary>
        int Count { get; }

        /// <summary>Agrega un elemento al final (o donde corresponda según la disciplina).</summary>
        void Enqueue(T item);

        /// <summary>
        /// Retira el siguiente elemento según la disciplina. Devuelve false si la cola está vacía.
        /// </summary>
        bool TryDequeue(out T item);

        /// <summary>
        /// Inspecciona el siguiente elemento sin retirarlo. Devuelve false si la cola está vacía.
        /// </summary>
        bool TryPeek(out T item);

        /// <summary>
        /// Remueve un elemento específico (útil para abandonos por impaciencia).
        /// Devuelve false si el elemento no estaba en la cola.
        /// </summary>
        bool TryRemove(T item);

        /// <summary>Vacía la cola.</summary>
        void Clear();

        /// <summary>
        /// Iterador de solo lectura sobre los elementos actuales, en orden de despacho.
        /// </summary>
        IEnumerable<T> Items { get; }
    }
}
