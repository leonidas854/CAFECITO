namespace CafeSim.Core.Tables
{
    /// <summary>
    /// Mesa física con N sillas. Mantiene cuáles sillas están ocupadas y por
    /// qué cliente. No conoce a Unity ni a sprites.
    /// </summary>
    public sealed class Table
    {
        /// <summary>Identificador único de la mesa (asignado por el TableManager).</summary>
        public int Id { get; }

        /// <summary>Capacidad total de sillas.</summary>
        public int Capacity { get; }

        // _seats[i] guarda el id del cliente sentado, o 0 si está libre.
        private readonly int[] _seats;

        public Table(int id, int capacity)
        {
            Id = id;
            Capacity = capacity;
            _seats = new int[capacity];
        }

        /// <summary>Cantidad de sillas libres.</summary>
        public int FreeSeats
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _seats.Length; i++)
                    if (_seats[i] == 0) count++;
                return count;
            }
        }

        /// <summary>True si todas las sillas están libres.</summary>
        public bool IsEmpty => FreeSeats == Capacity;

        /// <summary>True si no quedan sillas libres.</summary>
        public bool IsFull => FreeSeats == 0;

        /// <summary>
        /// Asigna la primera silla libre a un cliente. Devuelve el índice de
        /// la silla (0..Capacity-1) o -1 si no hay sillas libres.
        /// </summary>
        public int TryAssignSeat(int customerId)
        {
            if (customerId <= 0) return -1;
            for (int i = 0; i < _seats.Length; i++)
            {
                if (_seats[i] == 0)
                {
                    _seats[i] = customerId;
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Libera la silla del cliente indicado. Devuelve true si se liberó.
        /// </summary>
        public bool ReleaseSeat(int customerId)
        {
            for (int i = 0; i < _seats.Length; i++)
            {
                if (_seats[i] == customerId)
                {
                    _seats[i] = 0;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Devuelve el id del cliente sentado en la silla indicada, o 0 si está libre.
        /// </summary>
        public int CustomerAtSeat(int seatIndex)
        {
            if (seatIndex < 0 || seatIndex >= _seats.Length) return 0;
            return _seats[seatIndex];
        }

        /// <summary>Libera todas las sillas (usado al reiniciar la simulación).</summary>
        public void Clear()
        {
            for (int i = 0; i < _seats.Length; i++) _seats[i] = 0;
        }
    }
}
