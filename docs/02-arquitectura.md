# 02 — Arquitectura (Clean Architecture aplicada a Unity)

Este documento explica **por qué** y **cómo** organizamos el código bajo Clean Architecture, y los patrones de diseño que cada integrante debe respetar.

---

## 1. La idea central

> El núcleo matemático de la simulación **no debe depender** de Unity. Debe poder ejecutarse, probarse y validarse contra ProModel sin abrir el editor.

Esto se logra con dos disciplinas:

1. **Separación por capas**: cada carpeta tiene una responsabilidad clara y conoce solo a las capas "inferiores".
2. **Inversión de dependencias**: las capas externas (UI, Entities) hablan con el Core a través de **eventos** y **interfaces**, no de referencias directas.

---

## 2. Capas del proyecto

Todas viven bajo `Assets/_Project/Scripts/`.

### 2.1 `Core/` — Dominio puro

**Responsable**: Integrante 1.

- **Qué contiene**: lógica matemática del simulador. Generador LCG, cálculos de distribución exponencial, estructura de cola FIFO, fórmulas de teoría de colas (ρ, Lq, Wq, W), motor de avance de la simulación.
- **Qué NO contiene**: `using UnityEngine;` está **prohibido** aquí. Nada de `MonoBehaviour`, nada de `GameObject`, nada de `Transform`. Solo C# puro.
- **Por qué**: porque así I4 puede importar el Core en un proyecto .NET puro o en un script de Python (vía `dotnet`) para comparar los números con ProModel sin abrir Unity.

Archivos previstos:

```text
Core/
├── SimulationManager.cs       # Orquestador (Singleton). NO MonoBehaviour.
├── LCGRandomGenerator.cs      # Pseudoaleatorio congruencial lineal.
├── ExponentialDistribution.cs # Transformada inversa: -ln(U)/rate.
├── Queue/
│   ├── IQueueDiscipline.cs    # Interfaz: permite FIFO, LIFO, Priority en el futuro.
│   ├── FifoQueue.cs           # Implementación FIFO por defecto.
│   └── QueueController.cs     # Gestor: enqueue, dequeue, longitud.
└── Metrics/
    ├── MetricSnapshot.cs      # Struct con W, Wq, Lq, ρ.
    └── MetricCalculator.cs    # Funciones puras que computan métricas.
```

### 2.2 `Data/` — Configuración

**Responsable**: Integrante 1 (estructura) + Integrante 2 (assets).

- **Qué contiene**: `ScriptableObject` con los parámetros editables desde el inspector de Unity. Permite cambiar el escenario sin tocar código.
- **Ejemplo**: `SimulationConfig.cs` con campos `arrivalRate`, `serviceRateCashier`, `serviceRateBarista`, `customerPatience`, `lcgSeed`, `webOrderProbability`.

Archivos previstos:

```text
Data/
├── SimulationConfig.cs        # ScriptableObject con parámetros λ, μ, paciencia, semilla.
└── ScenarioPreset.cs          # Variantes: "Hora pico", "Hora muerta", "Solo web".
```

Los assets generados (`.asset`) viven en `Assets/_Project/Settings/`.

### 2.3 `Entities/` — Vista física (Unity)

**Responsable**: Integrante 3.

- **Qué contiene**: `MonoBehaviour` que representan visualmente al Cliente, Cajero y Barista. Manejan posición, sprite, animación (DOTween) y máquina de estados visual.
- **Regla**: consumen el Core (leen su estado y reaccionan), nunca al revés. El Core no sabe que existen entidades visuales.

Archivos previstos:

```text
Entities/
├── CustomerSpawner.cs         # Instancia clientes según λ.
├── CustomerEntity.cs          # MonoBehaviour de un cliente.
├── CustomerStateMachine.cs    # Estados: Entering → WaitingInLine → Ordering → ...
├── BaristaEntity.cs           # Servidor del barista.
├── CashierEntity.cs           # Servidor de la caja.
└── Movement/
    └── WaypointMover.cs       # Helper para mover entre puntos con DOTween.
```

### 2.4 `Events/` — Desacoplador (Observer)

**Responsable**: Integrante 1 (definición) + todos (uso).

- **Qué contiene**: eventos estáticos de C# (`public static event Action<T>`) que actúan como un canal de mensajería entre capas.
- **Por qué existe**: para que la UI no necesite una referencia directa a `CustomerEntity` o al `SimulationManager`. La UI se "suscribe" y reacciona cuando el evento dispara.

Archivos previstos:

```text
Events/
└── GameEvents.cs    # Clase estática con todos los eventos del proyecto.
```

Ejemplo de uso:

```csharp
// En Core/SimulationManager.cs
GameEvents.OnCustomerServed?.Invoke(customer);

// En UI/MetricTracker.cs
void OnEnable()  => GameEvents.OnCustomerServed += HandleCustomerServed;
void OnDisable() => GameEvents.OnCustomerServed -= HandleCustomerServed;
```

### 2.5 `UI/` — Interfaz de usuario

**Responsable**: Integrante 2.

- **Qué contiene**: scripts del dashboard, sliders, botones de control de tiempo, gráficos, textos dinámicos.
- **Regla**: lee del Core mediante eventos. Escribe al Core solo cambiando valores del `SimulationConfig` (no llamando métodos internos del simulador).

Archivos previstos:

```text
UI/
├── DashboardUI.cs        # Layout principal.
├── MetricTracker.cs      # Suscriptor a eventos; actualiza textos de W, Wq, Lq, ρ.
├── TimeController.cs     # Botones 0x, 1x, 2x, 5x, 10x → Time.timeScale.
├── ParameterSliders.cs   # Sliders λ y μ vinculados al SimulationConfig.
└── Charts/
    └── LiveLineChart.cs  # Gráfico simple en tiempo real.
```

---

## 3. Regla de dependencias (la regla maestra)

```text
                    ┌─────────────┐
                    │     UI      │
                    └──────┬──────┘
                           │ se suscribe a
                           ▼
                    ┌─────────────┐
                    │   Events    │ (canal estático)
                    └──────▲──────┘
                           │ dispara
        ┌──────────────────┼──────────────────┐
        │                  │                  │
┌───────┴────────┐  ┌──────┴───────┐          │
│   Entities     │  │     Core     │          │
│ (MonoBehaviour)│─►│  (C# puro)   │          │
└───────┬────────┘  └──────┬───────┘          │
        │                  │                  │
        │                  ▼                  │
        │           ┌─────────────┐           │
        └──────────►│    Data     │◄──────────┘
                    │ (Scriptable │
                    │  Object)    │
                    └─────────────┘
```

Reglas:

- **Core no conoce a nadie** (excepto Data, que es un contrato de configuración).
- **Entities conoce a Core y Data**.
- **UI conoce a Data**; para hablar con el resto, **usa Events**.
- **Events no conoce a nadie**: es un buzón estático.

Si te das cuenta de que tienes que hacer `using Assets._Project.Scripts.UI;` desde `Core/`, **estás violando la regla**. Detente y rediseña: probablemente lo que necesitas es disparar un evento.

---

## 4. Patrones de diseño aplicados

### 4.1 Singleton (Core/SimulationManager.cs)

Único punto de verdad para el estado de la simulación.

```csharp
public sealed class SimulationManager
{
    private static SimulationManager _instance;
    public static SimulationManager Instance => _instance ??= new SimulationManager();

    private SimulationManager() { }

    public SimulationConfig Config { get; set; }
    public float SimulationTime { get; private set; }
    // ...
}
```

> Variante elegida: Singleton **no-MonoBehaviour**. Si necesitas vincularlo a la vida de una escena, crea un `SimulationManagerBootstrap : MonoBehaviour` en `Entities/` que llame a `SimulationManager.Instance` en `Awake()`.

### 4.2 Observer (Events/GameEvents.cs)

Eventos C# estáticos. Cero acoplamiento entre emisor y receptor.

```csharp
public static class GameEvents
{
    public static event Action<CustomerData> OnCustomerArrived;
    public static event Action<CustomerData> OnCustomerServed;
    public static event Action<CustomerData> OnCustomerAbandoned;
    public static event Action<MetricSnapshot> OnMetricsUpdated;
}
```

> **Importante**: siempre desuscribirse en `OnDisable()` para evitar memory leaks.

### 4.3 State Machine (Entities/CustomerStateMachine.cs)

Estados explícitos en lugar de booleanos sueltos.

```csharp
public enum CustomerState
{
    Entering,        // Caminando hacia la cola
    WaitingInLine,   // En la cola física
    Ordering,        // Frente al cajero
    WaitingDrink,    // Cajero ya tomó la orden, espera al barista
    Consuming,       // Sentado tomando el café
    Leaving          // Saliendo del establecimiento
}
```

Cada estado tiene su `Enter()`, `Update()`, `Exit()`. Las transiciones son explícitas y log-eables.

### 4.4 Factory (Entities/CustomerSpawner.cs)

Centraliza la creación de clientes. Recibe el prefab y los inicializa con los parámetros del Core.

```csharp
public class CustomerSpawner : MonoBehaviour
{
    [SerializeField] private CustomerEntity _customerPrefab;
    [SerializeField] private Transform _spawnPoint;

    public CustomerEntity Spawn(bool isWebOrder)
    {
        var customer = Instantiate(_customerPrefab, _spawnPoint.position, Quaternion.identity);
        customer.Initialize(isWebOrder);
        return customer;
    }
}
```

### 4.5 ScriptableObject (Data/SimulationConfig.cs)

Parámetros configurables sin tocar código.

```csharp
[CreateAssetMenu(fileName = "SimulationConfig", menuName = "CafeSim/Simulation Config")]
public class SimulationConfig : ScriptableObject
{
    [Header("Tasas de llegada (clientes/min)")]
    [Range(0.1f, 30f)] public float arrivalRate = 5f;

    [Header("Tasas de servicio (clientes/min)")]
    [Range(0.1f, 30f)] public float serviceRateCashier = 8f;
    [Range(0.1f, 30f)] public float serviceRateBarista = 6f;

    [Header("Paciencia del cliente (segundos)")]
    [Range(10f, 600f)] public float customerPatience = 120f;

    [Header("Pedidos web")]
    [Range(0f, 1f)] public float webOrderProbability = 0.3f;

    [Header("Semilla LCG")]
    public long lcgSeed = 12345;
}
```

### 4.6 Strategy (futuro)

`IQueueDiscipline` permite extender con LIFO o Priority Queue sin tocar `QueueController`. No se implementa en el MVP de 14 días, pero la interfaz se deja preparada.

---

## 5. Puntos de extensibilidad

Si un docente o el propio equipo quiere agregar:

| Extensión | Cómo hacerlo |
|---|---|
| Nueva disciplina de cola | Implementar `IQueueDiscipline`. Sin tocar el `QueueController`. |
| Nuevo tipo de servidor | Heredar de `BaseServerEntity`. `BaristaEntity` y `CashierEntity` ya lo hacen. |
| Nueva métrica | Crear un suscriptor de `GameEvents.OnCustomerServed` en `UI/`. Cero modificaciones al Core. |
| Export a CSV | Usar `StreamingAssets/` + `System.IO`. `MetricTracker` ya acumula los datos. |
| Otro escenario | Crear un nuevo `ScenarioPreset.asset` desde el menú "Create → CafeSim → Scenario Preset". |

---

## 6. Errores comunes a evitar

1. ❌ **Singleton con `MonoBehaviour.FindObjectOfType`** — lento y frágil. Usa el patrón estático puro.
2. ❌ **Acceder al Core desde UI con `SimulationManager.Instance.Customers[0]`** — viola la regla. Suscríbete a `GameEvents.OnCustomerArrived`.
3. ❌ **Lógica en `Update()` de un `MonoBehaviour`** — el avance de la simulación lo hace el Core en su propio tick.
4. ❌ **Hardcodear valores como `if (queue.Count > 10)`** — esos deben venir de `SimulationConfig`.
5. ❌ **No desuscribirse de eventos en `OnDisable()`** — fuga de memoria + null references al recargar la escena.

Más reglas concretas en [03-clean-code.md](03-clean-code.md).
