using System;
using System.Collections.Generic;
using CafeSim.Core.Metrics;
using CafeSim.Core.Queue;
using CafeSim.Core.Tables;
using CafeSim.Events;

namespace CafeSim.Core
{
    /// <summary>
    /// Orquestador único de la simulación. Singleton no-MonoBehaviour para
    /// mantener el Core libre de dependencias de Unity.
    ///
    /// <para>Responsabilidades:
    /// <list type="bullet">
    ///   <item>Generación de llegadas (proceso de Poisson vía LCG + Exponencial).</item>
    ///   <item>Routing físico vs. web (vía <see cref="OrderSystem"/>).</item>
    ///   <item>Asignación de servidores con o sin modo multi-skill.</item>
    ///   <item>Asignación de mesas (vía <see cref="TableManager"/>) o consumo de pie.</item>
    ///   <item>Rechazo al llegar si la cola correspondiente está llena o se supera el límite concurrente.</item>
    ///   <item>Abandono por impaciencia en cola.</item>
    ///   <item>Publicación periódica del <see cref="MetricSnapshot"/> vía <see cref="GameEvents"/>.</item>
    /// </list></para>
    ///
    /// El driver de Unity (un <c>SimulationBootstrap : MonoBehaviour</c>) debe
    /// llamar a <see cref="Tick"/> cada frame con <c>Time.deltaTime</c>.
    /// </summary>
    public sealed class SimulationManager
    {
        // ─── Singleton ────────────────────────────────────────────────────────

        private static SimulationManager _instance;
        public static SimulationManager Instance => _instance ??= new SimulationManager();

        private SimulationManager() { }

        // ─── Estado interno ────────────────────────────────────────────────────

        private SimulationParameters _params;
        private LCGRandomGenerator _rng;
        private OrderSystem _orderSystem;
        private TableManager _tableManager;

        private QueueController<CustomerData> _cashierQueue;
        private QueueController<CustomerData> _baristaQueue;

        private Server[] _cashiers;
        private Server[] _baristas;

        private readonly List<CustomerData> _consumingCustomers = new List<CustomerData>();
        private readonly List<CustomerData> _finishedCustomers = new List<CustomerData>();

        private float _simulationTime;
        private float _nextArrivalTime;
        private float _lastMetricsEmitTime;
        private int _nextCustomerId;

        private int _arrivedCount;
        private int _servedCount;
        private int _abandonedCount;
        private int _rejectedCount;

        // ─── Propiedades públicas ─────────────────────────────────────────────

        public bool IsRunning { get; private set; }
        public float SimulationTime => _simulationTime;
        public SimulationParameters Parameters => _params;

        public int ArrivedCount => _arrivedCount;
        public int ServedCount => _servedCount;
        public int AbandonedCount => _abandonedCount;
        public int RejectedCount => _rejectedCount;
        public int ActiveCustomerCount => _arrivedCount - _servedCount - _abandonedCount - _rejectedCount;

        public TableManager TableManager => _tableManager;

        // ─── API pública ──────────────────────────────────────────────────────

        /// <summary>
        /// Configura los parámetros y reinicia toda la corrida desde t = 0.
        /// </summary>
        public void Configure(SimulationParameters parameters)
        {
            _params = parameters ?? throw new ArgumentNullException(nameof(parameters));
            Reset();
        }

        /// <summary>
        /// Actualiza los parámetros en caliente sin resetear el reloj. Útil
        /// para sliders en la UI.
        /// </summary>
        public void UpdateParameters(SimulationParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            _params = parameters;
            ResizeServers();
        }

        /// <summary>Reinicia el estado a tiempo 0 conservando los parámetros vigentes.</summary>
        public void Reset()
        {
            if (_params == null)
                throw new InvalidOperationException("Llame a Configure antes de Reset.");

            _rng = new LCGRandomGenerator(_params.LcgSeed);

            _cashierQueue = new QueueController<CustomerData>(new FifoQueue<CustomerData>());
            _baristaQueue = new QueueController<CustomerData>(new FifoQueue<CustomerData>());
            _orderSystem = new OrderSystem(_cashierQueue, _baristaQueue);
            _tableManager = new TableManager(_params.TableCount, _params.SeatsPerTable);

            _cashiers = CreateServers(_params.CashierCount);
            _baristas = CreateServers(_params.BaristaCount);

            _consumingCustomers.Clear();
            _finishedCustomers.Clear();

            _simulationTime = 0f;
            _lastMetricsEmitTime = 0f;
            _nextCustomerId = 1;
            _arrivedCount = 0;
            _servedCount = 0;
            _abandonedCount = 0;
            _rejectedCount = 0;

            _nextArrivalTime = ExponentialDistribution
                .SampleWithRate(_rng, _params.ArrivalRatePerSecond);

            IsRunning = true;
            GameEvents.RaiseSimulationReset();
        }

        public void Pause() => IsRunning = false;
        public void Resume() { if (_params != null) IsRunning = true; }

        /// <summary>
        /// Avanza el reloj de la simulación y procesa todos los eventos que
        /// caen dentro del intervalo.
        /// </summary>
        public void Tick(float deltaTimeSeconds)
        {
            if (!IsRunning || _params == null || deltaTimeSeconds <= 0f) return;

            _simulationTime += deltaTimeSeconds;

            ProcessArrivals();
            ProcessServerCompletions(_cashiers);
            ProcessServerCompletions(_baristas);
            ProcessConsumeCompletions();
            ProcessAbandonments();
            TryAssignServers();

            _cashierQueue.AdvanceTime(_simulationTime);
            _baristaQueue.AdvanceTime(_simulationTime);

            EmitMetricsIfDue();
        }

        public MetricSnapshot GetCurrentSnapshot() => BuildSnapshot();

        // ─── Procesamiento: Llegadas ──────────────────────────────────────────

        private void ProcessArrivals()
        {
            while (_nextArrivalTime <= _simulationTime)
            {
                SpawnCustomerAt(_nextArrivalTime);
                _nextArrivalTime += ExponentialDistribution
                    .SampleWithRate(_rng, _params.ArrivalRatePerSecond);
            }
        }

        private void SpawnCustomerAt(float arrivalTime)
        {
            bool isWebOrder = _rng.NextBool(_params.WebOrderProbability);
            var product = ProductCatalog.SampleProduct(_rng);
            var customer = new CustomerData(_nextCustomerId++, isWebOrder, arrivalTime)
            {
                Product = product
            };
            _arrivedCount++;
            GameEvents.RaiseCustomerArrived(customer);

            if (ShouldReject(customer))
            {
                RejectCustomer(customer);
                return;
            }

            _orderSystem.RouteOnArrival(customer, arrivalTime);
            GameEvents.RaiseCustomerStateChanged(customer);
        }

        private bool ShouldReject(CustomerData customer)
        {
            if (ActiveCustomerCount > _params.MaxConcurrentCustomers) return true;

            if (customer.IsWebOrder)
                return _baristaQueue.Length >= _params.MaxBaristaQueueLength;
            return _cashierQueue.Length >= _params.MaxCashierQueueLength;
        }

        private void RejectCustomer(CustomerData customer)
        {
            customer.State = CustomerState.Rejected;
            customer.WasRejected = true;
            customer.DepartureTime = _simulationTime;
            _rejectedCount++;
            _finishedCustomers.Add(customer);
            GameEvents.RaiseCustomerRejected(customer);
        }

        // ─── Procesamiento: Servidores ────────────────────────────────────────

        private void ProcessServerCompletions(Server[] servers)
        {
            for (int i = 0; i < servers.Length; i++)
            {
                var server = servers[i];
                if (!server.IsBusy || server.ServiceEndTime > _simulationTime) continue;

                var customer = server.CurrentCustomer;
                var finishedTask = server.CurrentTask;
                server.TotalBusyTime += server.ServiceEndTime - server.ServiceStartTime;
                server.CurrentCustomer = null;
                server.CurrentTask = ServerTask.Idle;

                if (finishedTask == ServerTask.TakingOrder)
                {
                    customer.CashierServiceEndTime = server.ServiceEndTime;
                    _orderSystem.RouteAfterCashier(customer, server.ServiceEndTime);
                    GameEvents.RaiseCustomerStateChanged(customer);
                }
                else
                {
                    customer.BaristaServiceEndTime = server.ServiceEndTime;
                    StartConsuming(customer, server.ServiceEndTime);
                }
            }
        }

        private void StartConsuming(CustomerData customer, float startTime)
        {
            bool seated = _tableManager.TryAssignSeat(customer.Id, out int tableId, out int seatIndex);
            if (seated)
            {
                customer.TableId = tableId;
                customer.SeatIndex = seatIndex;
                customer.State = CustomerState.Consuming;
                GameEvents.RaiseCustomerSeated(customer);
            }
            else
            {
                customer.ConsumedStanding = true;
                customer.State = CustomerState.ConsumingStanding;
            }

            customer.ConsumeStartTime = startTime;
            customer.ConsumeEndTime = startTime
                + ExponentialDistribution.SampleWithMean(_rng, _params.AverageConsumeTimeSeconds);
            _consumingCustomers.Add(customer);
            GameEvents.RaiseCustomerStateChanged(customer);
        }

        // ─── Procesamiento: Consumo ───────────────────────────────────────────

        private void ProcessConsumeCompletions()
        {
            for (int i = _consumingCustomers.Count - 1; i >= 0; i--)
            {
                var customer = _consumingCustomers[i];
                if (!customer.ConsumeEndTime.HasValue
                    || customer.ConsumeEndTime.Value > _simulationTime) continue;

                if (customer.TableId > 0)
                {
                    _tableManager.ReleaseSeat(customer.Id, customer.TableId);
                    GameEvents.RaiseCustomerLeftTable(customer);
                }

                customer.State = CustomerState.Leaving;
                customer.DepartureTime = customer.ConsumeEndTime.Value;
                _consumingCustomers.RemoveAt(i);
                _finishedCustomers.Add(customer);
                _servedCount++;
                GameEvents.RaiseCustomerServed(customer);
            }
        }

        // ─── Procesamiento: Abandono ──────────────────────────────────────────

        private void ProcessAbandonments()
        {
            CheckAbandonmentsInQueue(_cashierQueue, c => c.CashierQueueEnterTime);
            CheckAbandonmentsInQueue(_baristaQueue, c => c.BaristaQueueEnterTime);
        }

        private void CheckAbandonmentsInQueue(
            QueueController<CustomerData> queue,
            Func<CustomerData, float?> enqueueTimeOf)
        {
            var snapshot = new List<CustomerData>(queue.Snapshot());
            foreach (var customer in snapshot)
            {
                float? enteredAt = enqueueTimeOf(customer);
                if (!enteredAt.HasValue) continue;
                if (_simulationTime - enteredAt.Value < _params.CustomerPatienceSeconds) continue;

                if (queue.TryRemove(customer, _simulationTime))
                {
                    customer.State = CustomerState.Abandoned;
                    customer.AbandonmentTime = _simulationTime;
                    _finishedCustomers.Add(customer);
                    _abandonedCount++;
                    GameEvents.RaiseCustomerAbandoned(customer);
                }
            }
        }

        // ─── Procesamiento: Asignación de servidores ──────────────────────────

        private void TryAssignServers()
        {
            // Fase 1: cada pool atiende su cola "nativa".
            AssignFreeServers(_cashiers, _cashierQueue, ServerTask.TakingOrder);
            AssignFreeServers(_baristas, _baristaQueue, ServerTask.PreparingDrink);

            // Fase 2 (solo multi-skill): los workers libres pueden cubrir la otra cola.
            if (_params.CashierAlsoBarista)
            {
                AssignFreeServers(_cashiers, _baristaQueue, ServerTask.PreparingDrink);
                AssignFreeServers(_baristas, _cashierQueue, ServerTask.TakingOrder);
            }
        }

        private void AssignFreeServers(
            Server[] servers,
            QueueController<CustomerData> queue,
            ServerTask task)
        {
            for (int i = 0; i < servers.Length; i++)
            {
                var server = servers[i];
                if (server.IsBusy) continue;
                if (!queue.TryDequeue(_simulationTime, out var customer)) return;

                server.CurrentCustomer = customer;
                server.CurrentTask = task;
                server.ServiceStartTime = _simulationTime;
                server.ServiceEndTime = _simulationTime + ComputeServiceTime(task, customer);

                if (task == ServerTask.TakingOrder)
                {
                    customer.State = CustomerState.Ordering;
                    customer.CashierServiceStartTime = _simulationTime;
                }
                else
                {
                    customer.State = CustomerState.BeingServed;
                    customer.BaristaServiceStartTime = _simulationTime;
                }
                GameEvents.RaiseCustomerStateChanged(customer);
            }
        }

        private float ComputeServiceTime(ServerTask task, CustomerData customer)
        {
            if (task == ServerTask.TakingOrder)
                return ExponentialDistribution.SampleWithRate(_rng, _params.ServiceRateCashierPerSecond);

            float mean = ProductCatalog.GetMeanServiceTimeSeconds(customer.Product);
            return ExponentialDistribution.SampleWithMean(_rng, mean);
        }

        // ─── Métricas ─────────────────────────────────────────────────────────

        private void EmitMetricsIfDue()
        {
            if (_simulationTime - _lastMetricsEmitTime < _params.MetricsEmitIntervalSeconds)
                return;
            _lastMetricsEmitTime = _simulationTime;
            GameEvents.RaiseMetricsUpdated(BuildSnapshot());
        }

        private MetricSnapshot BuildSnapshot()
        {
            return new MetricSnapshot(
                simulationTimeSeconds: _simulationTime,
                arrivedCount: _arrivedCount,
                servedCount: _servedCount,
                abandonedCount: _abandonedCount,
                cashierQueueLength: _cashierQueue.Length,
                baristaQueueLength: _baristaQueue.Length,
                cashierAverageQueueLength: _cashierQueue.AverageLength(_simulationTime),
                baristaAverageQueueLength: _baristaQueue.AverageLength(_simulationTime),
                averageTimeInQueues: MetricCalculator.AverageTimeInQueues(_finishedCustomers),
                averageTimeInSystem: MetricCalculator.AverageTimeInSystem(_finishedCustomers),
                cashierUtilization: ComputeUtilization(_cashiers),
                baristaUtilization: ComputeUtilization(_baristas));
        }

        private float ComputeUtilization(Server[] servers)
        {
            if (_simulationTime <= 0f || servers.Length == 0) return 0f;
            double totalBusy = 0d;
            for (int i = 0; i < servers.Length; i++)
            {
                totalBusy += servers[i].TotalBusyTime;
                if (servers[i].IsBusy)
                    totalBusy += _simulationTime - servers[i].ServiceStartTime;
            }
            return (float)(totalBusy / (servers.Length * (double)_simulationTime));
        }

        // ─── Gestión de servidores ────────────────────────────────────────────

        private static Server[] CreateServers(int count)
        {
            var arr = new Server[count];
            for (int i = 0; i < count; i++) arr[i] = new Server { Id = i };
            return arr;
        }

        private void ResizeServers()
        {
            _cashiers = ResizeKeepingState(_cashiers, _params.CashierCount);
            _baristas = ResizeKeepingState(_baristas, _params.BaristaCount);
        }

        private static Server[] ResizeKeepingState(Server[] current, int desiredCount)
        {
            if (current == null) return CreateServers(desiredCount);
            if (current.Length == desiredCount) return current;

            var resized = new Server[desiredCount];
            int keep = Math.Min(current.Length, desiredCount);
            for (int i = 0; i < keep; i++) resized[i] = current[i];
            for (int i = keep; i < desiredCount; i++) resized[i] = new Server { Id = i };
            return resized;
        }

        // ─── Tipos auxiliares ─────────────────────────────────────────────────

        private enum ServerTask
        {
            Idle,
            TakingOrder,    // está atendiendo en la caja
            PreparingDrink  // está preparando una bebida
        }

        private sealed class Server
        {
            public int Id;
            public CustomerData CurrentCustomer;
            public ServerTask CurrentTask;
            public float ServiceStartTime;
            public float ServiceEndTime;
            public float TotalBusyTime;
            public bool IsBusy => CurrentCustomer != null;
        }
    }
}
