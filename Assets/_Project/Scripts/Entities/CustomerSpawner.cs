using System.Collections.Generic;
using UnityEngine;
using CafeSim.Core;
using CafeSim.Entities.Layout;
using CafeSim.Entities.Movement;
using CafeSim.Entities.Placeholders;
using CafeSim.Events;

namespace CafeSim.Entities
{
    /// <summary>
    /// Suscriptor de los eventos de cliente del Core. Crea, mueve y destruye
    /// los GameObjects placeholder en respuesta a las transiciones de estado.
    ///
    /// El <c>SimulationBootstrap</c> arma una instancia de este componente y
    /// le inyecta el <see cref="SceneLayout"/> con las posiciones del local.
    /// </summary>
    public sealed class CustomerSpawner : MonoBehaviour
    {
        [Tooltip("Tamaño del placeholder del cliente. Se usa un círculo (vista cenital).")]
        [SerializeField] private Vector2 customerSize = new Vector2(0.6f, 0.6f);

        [Tooltip("Velocidad del cliente caminando, en unidades por segundo.")]
        [SerializeField] private float walkSpeed = 3f;

        private SceneLayout _layout;
        private Transform _customerParent;

        // Diccionario id de cliente → GameObject vivo.
        private readonly Dictionary<int, CustomerEntity> _alive
            = new Dictionary<int, CustomerEntity>();

        // Conteo de slots ocupados en cada cola, para asignar posiciones de espera.
        private int _cashierQueueCount;
        private int _baristaQueueCount;

        /// <summary>Total de GameObjects de cliente actualmente en escena.</summary>
        public int AliveCount => _alive.Count;

        /// <summary>
        /// Inicializa el spawner con un layout y un transform padre para
        /// agrupar todos los clientes en el Hierarchy.
        /// </summary>
        public void Initialize(SceneLayout layout, Transform parent)
        {
            _layout = layout;
            _customerParent = parent;
        }

        private void OnEnable()
        {
            GameEvents.OnCustomerArrived += HandleCustomerArrived;
            GameEvents.OnCustomerStateChanged += HandleCustomerStateChanged;
            GameEvents.OnCustomerLeft += HandleCustomerLeft;
            GameEvents.OnSimulationReset += HandleSimulationReset;
        }

        private void OnDisable()
        {
            GameEvents.OnCustomerArrived -= HandleCustomerArrived;
            GameEvents.OnCustomerStateChanged -= HandleCustomerStateChanged;
            GameEvents.OnCustomerLeft -= HandleCustomerLeft;
            GameEvents.OnSimulationReset -= HandleSimulationReset;
        }

        // ─── Handlers ────────────────────────────────────────────────────────

        private void HandleCustomerArrived(CustomerData data)
        {
            if (_layout == null) return;
            var entity = CreateCustomerVisual(data);
            _alive[data.Id] = entity;
        }

        private void HandleCustomerStateChanged(CustomerData data)
        {
            if (!_alive.TryGetValue(data.Id, out var entity)) return;
            entity.ApplyStateColor(data.State);
            Vector3 destination = ComputeDestination(data);
            entity.WalkTo(destination);
        }

        private void HandleCustomerLeft(CustomerData data)
        {
            if (!_alive.TryGetValue(data.Id, out var entity)) return;
            _alive.Remove(data.Id);
            if (entity != null) Destroy(entity.gameObject);
            // Recontar slots; el orden visual no es crítico.
            RecountQueueSlots();
        }

        private void HandleSimulationReset()
        {
            foreach (var entity in _alive.Values)
            {
                if (entity != null) Destroy(entity.gameObject);
            }
            _alive.Clear();
            _cashierQueueCount = 0;
            _baristaQueueCount = 0;
        }

        // ─── Construcción visual ─────────────────────────────────────────────

        private CustomerEntity CreateCustomerVisual(CustomerData data)
        {
            string visualName = data.IsWebOrder
                ? $"Customer_{data.Id}_Web"
                : $"Customer_{data.Id}_Walk-in";

            var go = PlaceholderShapes.CreateColoredShape(
                objectName: visualName,
                sprite: PlaceholderShapes.Circle,
                color: CustomerVisualController.ColorFor(data.State),
                size: customerSize,
                parent: _customerParent,
                sortingOrder: 5);

            var mover = go.AddComponent<WaypointMover>();
            mover.SetSpeed(walkSpeed);

            var entity = go.AddComponent<CustomerEntity>();
            var spawnPos = new Vector3(_layout.entryPoint.x, _layout.entryPoint.y, 0f);
            entity.Bind(data, spawnPos);
            return entity;
        }

        // ─── Cálculo de posición destino según estado ────────────────────────

        private Vector3 ComputeDestination(CustomerData data)
        {
            switch (data.State)
            {
                case CustomerState.WaitingInLine:
                    return ToVec3(_layout.GetCashierQueueSlot(NextCashierSlot()));
                case CustomerState.Ordering:
                    return ToVec3(_layout.cashierStation);
                case CustomerState.WaitingDrink:
                    return ToVec3(_layout.GetBaristaQueueSlot(NextBaristaSlot()));
                case CustomerState.BeingServed:
                    return ToVec3(_layout.baristaStation);
                case CustomerState.Consuming:
                    return ResolveTableSeat(data);
                case CustomerState.ConsumingStanding:
                    return ToVec3(_layout.standingArea);
                case CustomerState.Leaving:
                case CustomerState.Abandoned:
                case CustomerState.Rejected:
                    return ToVec3(_layout.exitPoint);
                default:
                    return ToVec3(_layout.entryPoint);
            }
        }

        private Vector3 ResolveTableSeat(CustomerData data)
        {
            // Buscamos el TableEntity en escena cuyo TableId coincida.
            // Como la cantidad de mesas es chica (≤20), un FindObjectsOfType
            // por evento es aceptable. Si se vuelve un cuello de botella,
            // SimulationBootstrap puede cachear las TableEntities y exponerlas.
            var tables = FindObjectsByType<TableEntity>(FindObjectsSortMode.None);
            for (int i = 0; i < tables.Length; i++)
            {
                if (tables[i].TableId == data.TableId)
                    return tables[i].GetSeatWorldPosition(data.SeatIndex);
            }
            return ToVec3(_layout.standingArea);
        }

        private int NextCashierSlot()
        {
            int slot = _cashierQueueCount;
            _cashierQueueCount++;
            return slot;
        }

        private int NextBaristaSlot()
        {
            int slot = _baristaQueueCount;
            _baristaQueueCount++;
            return slot;
        }

        private void RecountQueueSlots()
        {
            // Recuento simple: cuántos clientes vivos están en cada cola.
            // Reasigna slots posibles errores acumulados visualmente.
            int cashier = 0, barista = 0;
            foreach (var e in _alive.Values)
            {
                if (e == null) continue;
                if (e.Data.State == CustomerState.WaitingInLine) cashier++;
                else if (e.Data.State == CustomerState.WaitingDrink) barista++;
            }
            _cashierQueueCount = cashier;
            _baristaQueueCount = barista;
        }

        private static Vector3 ToVec3(Vector2 v) => new Vector3(v.x, v.y, 0f);
    }
}
