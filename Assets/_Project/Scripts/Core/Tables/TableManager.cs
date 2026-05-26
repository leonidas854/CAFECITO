using System;

namespace CafeSim.Core.Tables
{
    /// <summary>
    /// Coordinador de mesas del local. Mantiene un arreglo de <see cref="Table"/>
    /// y reparte sillas a clientes que terminaron en el barista.
    ///
    /// Política de asignación: la primera mesa con silla libre, en orden por id.
    /// Si todas están llenas, el llamador (SimulationManager) decide qué hacer
    /// (configuración por defecto: el cliente consume de pie y no ocupa mesa).
    /// </summary>
    public sealed class TableManager
    {
        private readonly Table[] _tables;

        /// <summary>Cantidad total de mesas en el local.</summary>
        public int TableCount => _tables.Length;

        /// <summary>Capacidad total agregada (mesas × sillas por mesa).</summary>
        public int TotalCapacity { get; }

        public TableManager(int tableCount, int seatsPerTable)
        {
            if (tableCount < 0) throw new ArgumentOutOfRangeException(nameof(tableCount));
            if (seatsPerTable < 1) throw new ArgumentOutOfRangeException(nameof(seatsPerTable));

            _tables = new Table[tableCount];
            for (int i = 0; i < tableCount; i++)
                _tables[i] = new Table(id: i + 1, capacity: seatsPerTable);
            TotalCapacity = tableCount * seatsPerTable;
        }

        /// <summary>Cantidad total de sillas libres en el local.</summary>
        public int FreeSeats
        {
            get
            {
                int total = 0;
                for (int i = 0; i < _tables.Length; i++) total += _tables[i].FreeSeats;
                return total;
            }
        }

        /// <summary>True si no queda ninguna silla libre.</summary>
        public bool IsFull => FreeSeats == 0;

        /// <summary>
        /// Intenta sentar a un cliente. Devuelve true y publica los identificadores
        /// asignados en los parámetros <c>out</c>. Si no hay sillas libres, devuelve
        /// false y los <c>out</c> quedan en valores inválidos.
        /// </summary>
        public bool TryAssignSeat(int customerId, out int tableId, out int seatIndex)
        {
            tableId = -1;
            seatIndex = -1;
            if (customerId <= 0) return false;

            for (int i = 0; i < _tables.Length; i++)
            {
                int seat = _tables[i].TryAssignSeat(customerId);
                if (seat >= 0)
                {
                    tableId = _tables[i].Id;
                    seatIndex = seat;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Libera la silla previamente asignada. Devuelve true si la mesa existe
        /// y el cliente estaba sentado en ella.
        /// </summary>
        public bool ReleaseSeat(int customerId, int tableId)
        {
            var table = FindTable(tableId);
            return table != null && table.ReleaseSeat(customerId);
        }

        /// <summary>Acceso de solo lectura a una mesa por id.</summary>
        public Table GetTable(int tableId) => FindTable(tableId);

        /// <summary>Libera todas las sillas (al reiniciar la simulación).</summary>
        public void Clear()
        {
            for (int i = 0; i < _tables.Length; i++) _tables[i].Clear();
        }

        private Table FindTable(int tableId)
        {
            for (int i = 0; i < _tables.Length; i++)
                if (_tables[i].Id == tableId) return _tables[i];
            return null;
        }
    }
}
