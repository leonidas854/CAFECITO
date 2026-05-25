using System;
using System.Collections.Generic;
using CafeSim.Core.Metrics;
using CafeSim.Core.Queue;
using CafeSim.Events;

namespace CafeSim.Core
{
    /// <summary>
    /// Orquestador único de la simulación. Singleton no-MonoBehaviour para
    /// mantener el Core libre de dependencias de Unity.
    ///
    /// <para>El loop de Unity (a través de un <c>SimulationBootstrap : MonoBehaviour</c>
    /// que escribirá I3) debe llamar a <see cref="Tick"/> en cada frame con
    /// <c>Time.deltaTime</c>. Todas las cantidades temporales se manejan en
    /// segundos. Las tasas de llegada/servicio se reciben en clientes por
    /// segundo (ver <see cref="SimulationParameters"/>).</para>
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

        // ─── Propiedades públicas ─────────────────────────────────────────────

        /// <summary>True cuando el simulador está configurado y corriendo.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Tiempo simulado transcurrido (segundos).</summary>
        public float SimulationTime => _simulationTime;

        /// <summary>Parámetros vigentes; null si nunca se configuró.</summary>
        public SimulationParameters Parameters => _params;

        public int ArrivedCount => _arrivedCount;
        public int ServedCount => _servedCount;
        public int AbandonedCount => _abandonedCount;

        // ─── API pública ──────────────────────────────────────────────────────

        /// <summary>
        /// Configura los parámetros y reinicia toda la corrida desde t = 0.
        /// Debe llamarse al menos una vez antes del primer <see cref="Tick"/>.
        /// </summary>
        public void Configure(SimulationParameters parameters)
        {
            _params = parameters ?? throw new ArgumentNullException(nameof(parameters));
            Reset();
        }

        /// <summary>
        /// Actualiza los parámetros en caliente (típicamente desde un slider de
        /// UI) sin resetear el reloj. Solo afecta a los próximos eventos: las
        /// llegadas ya programadas y los servicios en curso no se reagendan.
        /// </summary>
        public void UpdateParameters(SimulationParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            _params = parameters;
            ResizeServers();
        }

        /// <summary>
        /// Reinicia todo el estado de la simulación a tiempo 0 conservando los
        /// parámetros vigentes. Limpia clientes, colas y servidores.
        /// </summary>
        public void Reset()
        {
            if (_params == null)
                throw new InvalidOperationException("Llame a Configure antes de Reset.");

            _rng = new LCGRandomGenerator(_params.LcgSeed);

            _cashierQueue = new QueueController<CustomerData>(new FifoQueue<CustomerData>());
            _baristaQueue = new QueueController<CustomerData>(new FifoQueue<CustomerData>());
            _orderSystem = new OrderSystem(_cashierQueue, _baristaQueue);

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

            _nextArrivalTime = ExponentialDistribution
                .SampleWithRate(_rng, _params.ArrivalRatePerSecond);

            IsRunning = true;
            GameEvents.RaiseSimulationReset();
        }

        /// <summary>
        /// Pausa la simulación. <see cref="Tick"/> no avanza el tiempo mientras esté pausado.
        /// </summary>
        public void Pause() => IsRunning = false;

        /// <summary>Reanuda la simulación pausada.</summary>
        public void Resume()
        {
            if (_params != null) IsRunning = true;
        }

        /// <summary>
        /// Avanza el reloj de la simulación <paramref name="deltaTimeSeconds"/>
        /// segundos y procesa todos los eventos que caen dentro de ese intervalo.
        /// </summary>
        public void Tick(float deltaTimeSeconds)
        {
            if (!IsRunning || _params == null || deltaTimeSeconds <= 0f) return;

            _simulationTime += deltaTimeSeconds;

            ProcessArrivals();
            ProcessServerCompletions(_cashiers, isCashier: true);
            ProcessServerCompletions(_baristas, isCashier: false);
            ProcessConsumeCompletions();
            ProcessAbandonments();
            TryAssignServers();

            _cashierQueue.AdvanceTime(_simulationTime);
            _baristaQueue.AdvanceTime(_simulationTime);

            EmitMetricsIfDue();
        }

        /// <summary>
        /// Devuelve la foto actual de métricas sin esperar al próximo emit programado.
        /// </summary>
        public MetricSnapshot GetCurrentSnapshot() => BuildSnapshot();

        // ─── Procesamiento interno por fase ───────────────────────────────────

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
            var customer = new CustomerData(_nextCustomerId++, isWebOrder, arrivalTime);
            _arrivedCount++;

            GameEvents.RaiseCustomerArrived(customer);
            _orderSystem.RouteOnArrival(customer, arrivalTime);
            GameEvents.RaiseCustomerStateChanged(customer);
        }

        private void ProcessServerCompletions(Server[] servers, bool isCashier)
        {
            for (int i = 0; i < servers.Length; i++)
            {
                var server = servers[i];
                if (!server.IsBusy || server.ServiceEndTime > _simulationTime) continue;

                var customer = server.CurrentCustomer;
                server.TotalBusyTime += server.ServiceEndTime - server.ServiceStartTime;
                server.CurrentCustomer = null;

                if (isCashier)
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
            customer.State = CustomerState.Consuming;
            customer.ConsumeStartTime = startTime;
            customer.ConsumeEndTime = startTime
                + ExponentialDistribution.SampleWithMean(_rng, _params.AverageConsumeTimeSeconds);
            _consumingCustomers.Add(customer);
            GameEvents.RaiseCustomerStateChanged(customer);
        }

        private void ProcessConsumeCompletions()
        {
            for (int i = _consumingCustomers.Count - 1; i >= 0; i--)
            {
                var customer = _consumingCustomers[i];
                if (!customer.ConsumeEndTime.HasValue
                    || customer.ConsumeEndTime.Value > _simulationTime) continue;

                customer.State = CustomerState.Leaving;
                customer.DepartureTime = customer.ConsumeEndTime.Value;
                _consumingCustomers.RemoveAt(i);
                _finishedCustomers.Add(customer);
                _servedCount++;
                GameEvents.RaiseCustomerServed(customer);
            }
        }

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

        private void TryAssignServers()
        {
            AssignFromQueue(_cashierQueue, _cashiers,
                _params.ServiceRateCashierPerSecond, isCashier: true);
            AssignFromQueue(_baristaQueue, _baristas,
                _params.ServiceRateBaristaPerSecond, isCashier: false);
        }

        private void AssignFromQueue(
            QueueController<CustomerData> queue,
            Server[] servers,
            float serviceRate,
            bool isCashier)
        {
            for (int i = 0; i < servers.Length; i++)
            {
                var server = servers[i];
                if (server.IsBusy) continue;
                if (!queue.TryDequeue(_simulationTime, out var customer)) return;

                server.CurrentCustomer = customer;
                server.ServiceStartTime = _simulationTime;
                server.ServiceEndTime = _simulationTime
                    + ExponentialDistribution.SampleWithRate(_rng, serviceRate);

                if (isCashier)
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

        // ─── Tipo auxiliar ────────────────────────────────────────────────────

        private sealed class Server
        {
            public int Id;
            public CustomerData CurrentCustomer;
            public float ServiceStartTime;
            public float ServiceEndTime;
            public float TotalBusyTime;
            public bool IsBusy => CurrentCustomer != null;
        }
    }
}
