using System.Collections.Generic;
using UnityEngine;
using CafeSim.Core;
using CafeSim.Data;
using CafeSim.Entities.Layout;
using CafeSim.Entities.Placeholders;
using CafeSim.Events;
using CafeSim.UI;

namespace CafeSim.Entities
{
    /// <summary>
    /// Driver del simulador del lado de Unity. Es el único <see cref="MonoBehaviour"/>
    /// que arranca todo:
    /// <list type="number">
    ///   <item>Convierte la <see cref="SimulationConfig"/> en <see cref="SimulationParameters"/>.</item>
    ///   <item>Llama a <c>SimulationManager.Instance.Configure</c> en <c>Start</c>.</item>
    ///   <item>Construye programáticamente los placeholders (cajeros, baristas, mesas)
    ///   usando <see cref="SceneLayout"/>. No hace falta arrastrar nada en la escena.</item>
    ///   <item>Llama a <c>SimulationManager.Instance.Tick(Time.deltaTime)</c> en cada <c>Update</c>.</item>
    ///   <item>Limpia las suscripciones del bus de eventos al destruirse.</item>
    /// </list>
    ///
    /// Para correr la simulación: coloca este componente en cualquier GameObject
    /// de la escena (por ejemplo "GameManager") y arrástrale los assets de
    /// SimulationConfig y SceneLayout en el inspector. Si no se asigna ningún
    /// asset, se crean instancias por defecto en runtime.
    /// </summary>
    public sealed class SimulationBootstrap : MonoBehaviour
    {
        [Header("Configuración")]
        [Tooltip("Parámetros de la simulación. Si está vacío, se crea uno por defecto.")]
        [SerializeField] private SimulationConfig config;

        [Tooltip("Layout geométrico del local. Si está vacío, se crea uno por defecto.")]
        [SerializeField] private SceneLayout layout;

        [Header("Spawner de clientes")]
        [SerializeField] private CustomerSpawner customerSpawner;

        [Header("Cámara")]
        [Tooltip("Si está activo, ajusta la cámara principal para encuadrar todo el local.")]
        [SerializeField] private bool autoFitCamera = true;

        [Tooltip("Tamaño orthographic de la cámara si autoFitCamera está activo.")]
        [Range(3f, 30f)]
        [SerializeField] private float cameraOrthographicSize = 8f;

        [Header("Dashboard UI")]
        [Tooltip("Crea el dashboard de métricas, sliders y controles en runtime.")]
        [SerializeField] private bool buildDashboard = true;

        private Transform _entitiesRoot;
        private DashboardUI _dashboard;
        private readonly List<TableEntity> _tableEntities = new List<TableEntity>();

        // ─── Ciclo de vida ───────────────────────────────────────────────────

        private void Awake()
        {
            EnsureConfig();
            EnsureLayout();
            EnsureSpawner();
            BuildSceneRoot();
            BuildLocalFurniture();
            if (autoFitCamera) FitCamera();
            if (buildDashboard) BuildDashboard();
        }

        private void Start()
        {
            var parameters = config.ToSimulationParameters();
            SimulationManager.Instance.Configure(parameters);
        }

        private void Update()
        {
            SimulationManager.Instance.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            GameEvents.ClearAllSubscriptions();
            SimulationManager.Instance.Pause();
        }

        // ─── Setup automático cuando no se inyectan assets ───────────────────

        private void EnsureConfig()
        {
            if (config != null) return;
            config = ScriptableObject.CreateInstance<SimulationConfig>();
            config.name = "DefaultSimulationConfig (runtime)";
            Debug.Log("[CafeSim] Sin SimulationConfig asignada — usando valores por defecto.");
        }

        private void EnsureLayout()
        {
            if (layout != null) return;
            layout = ScriptableObject.CreateInstance<SceneLayout>();
            layout.name = "DefaultSceneLayout (runtime)";
            Debug.Log("[CafeSim] Sin SceneLayout asignado — usando layout por defecto.");
        }

        private void EnsureSpawner()
        {
            if (customerSpawner != null) return;
            var go = new GameObject("CustomerSpawner");
            go.transform.SetParent(transform, worldPositionStays: false);
            customerSpawner = go.AddComponent<CustomerSpawner>();
        }

        private void BuildSceneRoot()
        {
            var rootGo = new GameObject("CafeSim_Entities");
            _entitiesRoot = rootGo.transform;
            customerSpawner.Initialize(layout, _entitiesRoot);
        }

        // ─── Construcción del mobiliario placeholder ─────────────────────────

        private void BuildLocalFurniture()
        {
            BuildEntryAndExitMarkers();
            BuildCashierStations();
            BuildBaristaStations();
            BuildTables();
            BuildStandingArea();
        }

        private void BuildEntryAndExitMarkers()
        {
            CreateMarker("EntryMarker", layout.entryPoint, new Vector2(0.3f, 1.4f),
                new Color(0.30f, 0.85f, 0.50f, 0.5f));
            CreateMarker("ExitMarker", layout.exitPoint, new Vector2(0.3f, 1.4f),
                new Color(0.85f, 0.50f, 0.30f, 0.5f));
        }

        private void BuildCashierStations()
        {
            int count = config.CashierCount;
            ServerRole role = config.CashierAlsoBarista ? ServerRole.Both : ServerRole.Cashier;
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = layout.GetCashierStation(i);
                CreateServer($"Cashier_{i + 1}", pos, role);
            }
        }

        private void BuildBaristaStations()
        {
            int count = config.BaristaCount;
            ServerRole role = config.CashierAlsoBarista ? ServerRole.Both : ServerRole.Barista;
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = layout.GetBaristaStation(i);
                CreateServer($"Barista_{i + 1}", pos, role);
            }
        }

        private void BuildTables()
        {
            _tableEntities.Clear();
            int tableCount = config.TableCount;
            int seatsPerTable = config.SeatsPerTable;
            for (int i = 0; i < tableCount; i++)
            {
                Vector2 pos = layout.GetTablePosition(i);
                var go = new GameObject($"Table_{i + 1}");
                go.transform.SetParent(_entitiesRoot, worldPositionStays: false);
                go.transform.position = new Vector3(pos.x, pos.y, 0f);
                go.AddComponent<SpriteRenderer>();
                var table = go.AddComponent<TableEntity>();
                table.Build(tableId: i + 1, seats: seatsPerTable, size: new Vector2(1.4f, 0.9f));
                _tableEntities.Add(table);
            }
        }

        private void BuildStandingArea()
        {
            CreateMarker("StandingArea", layout.standingArea, new Vector2(2f, 1.5f),
                new Color(0.85f, 0.85f, 0.50f, 0.3f));
        }

        private void CreateServer(string objectName, Vector2 position, ServerRole role)
        {
            var go = PlaceholderShapes.CreateColoredShape(
                objectName: objectName,
                sprite: PlaceholderShapes.RoundedRect,
                color: Color.white,
                size: new Vector2(0.8f, 0.8f),
                parent: _entitiesRoot,
                sortingOrder: 2);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            var server = go.AddComponent<ServerEntity>();
            server.SetRole(role);
        }

        private void CreateMarker(string objectName, Vector2 position, Vector2 size, Color color)
        {
            var go = PlaceholderShapes.CreateColoredShape(
                objectName: objectName,
                sprite: PlaceholderShapes.RoundedRect,
                color: color,
                size: size,
                parent: _entitiesRoot,
                sortingOrder: -1);
            go.transform.position = new Vector3(position.x, position.y, 0f);
        }

        // ─── Dashboard ───────────────────────────────────────────────────────

        private void BuildDashboard()
        {
            var existing = FindFirstObjectByType<DashboardUI>();
            if (existing != null)
            {
                _dashboard = existing;
                return;
            }
            var go = new GameObject("Dashboard");
            go.transform.SetParent(transform, worldPositionStays: false);
            _dashboard = go.AddComponent<DashboardUI>();
            _dashboard.Build(config);
        }

        // ─── Cámara ──────────────────────────────────────────────────────────

        private void FitCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = true;
            cam.orthographicSize = cameraOrthographicSize;
            cam.transform.position = new Vector3(
                (layout.entryPoint.x + layout.exitPoint.x) * 0.5f,
                0f,
                -10f);
        }
    }
}
