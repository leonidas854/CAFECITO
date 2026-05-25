# 05 — Roles y Responsabilidades del Equipo (4 Integrantes)

Cada integrante tiene un dominio claro. Si te encuentras tocando código fuera de tu dominio, **avisa primero** — probablemente esa modificación debería hacerla otro y entregarte el resultado vía Prefab o evento.

---

## Tabla resumen

| # | Rol | Carpetas que posees | Materia principal |
|---|---|---|---|
| **I1** | Core / Lógica / LCG | `Scripts/Core/`, `Scripts/Data/`, `Scripts/Events/` | Simulación + Ing. Software |
| **I2** | UI / Dashboard / Control de Tiempos | `Scripts/UI/`, `Prefabs/UI/`, `Art/UI/` | Ing. Software (UX) |
| **I3** | Escena / Entidades / Arte | `Scripts/Entities/`, `Prefabs/Entities/`, `Scenes/`, `Art/Sprites/`, `Art/Backgrounds/` | Ing. Software (integración) |
| **I4** | ProModel / Estadística / UML | `docs/`, modelos ProModel, scripts de análisis externos | Simulación (validación) |

---

## I1 — Core, Lógica y Generador LCG

### Misión

Construir el **motor matemático** del simulador. Tu código vive en `Scripts/Core/` y **no debe importar `UnityEngine`**. Esto permite que I4 lo ejecute desde un script externo para validar contra ProModel.

### Entregables (en orden de prioridad)

1. **`SimulationManager.cs`** (Singleton no-MonoBehaviour) — orquestador del estado global de la simulación.
2. **`LCGRandomGenerator.cs`** — generador congruencial lineal con semilla configurable (especificación matemática en [07-simulacion-teoria.md](07-simulacion-teoria.md)).
3. **`ExponentialDistribution.cs`** — transformada inversa para tiempos de llegada y servicio.
4. **`Queue/IQueueDiscipline.cs` + `Queue/FifoQueue.cs` + `Queue/QueueController.cs`** — cola FIFO con interfaz para futura extensión.
5. **`OrderSystem.cs`** — lógica del flujo dual: pedidos físicos (caja → barista) vs. pedidos web (bypass de caja, directo al barista).
6. **`Metrics/MetricCalculator.cs`** — fórmulas de teoría de colas (ρ, Lq, Wq, W).
7. **`Events/GameEvents.cs`** — definición de todos los eventos del proyecto.
8. **`Data/SimulationConfig.cs`** — `ScriptableObject` con λ, μ_caja, μ_barista, paciencia, semilla, probabilidad de pedido web.

### Contratos con otros roles

- **Con I2**: la UI lee del `SimulationConfig` y se suscribe a `GameEvents.OnMetricsUpdated`. **Nunca le des referencia directa a `SimulationManager`.**
- **Con I3**: las entidades visuales se suscriben a `GameEvents.OnCustomerArrived/Served/Abandoned`. El Core no sabe que existen `GameObject`s.
- **Con I4**: tu generador LCG y el motor deben poder ejecutarse en una consola .NET. Provee un método `RunSimulation(SimulationConfig, int seconds)` que retorne un `List<EventLog>` exportable a CSV.

### No te metas con

- ❌ La escena (`MainSimulation.unity`).
- ❌ Sprites ni canvases.
- ❌ Animaciones DOTween.

---

## I2 — UI, Dashboard y Control de Tiempos

### Misión

Construir el **panel administrativo** del simulador. Sliders para configurar λ y μ en tiempo real, botones de velocidad (`0x` / `1x` / `2x` / `5x` / `10x`), gráficos en vivo y textos de métricas.

### Entregables (en orden de prioridad)

1. **`DashboardUI.cs`** — layout principal del Canvas, organización de paneles.
2. **`ParameterSliders.cs`** — sliders λ, μ_caja, μ_barista, paciencia, probabilidad de pedido web. Escriben sobre el `SimulationConfig`.
3. **`TimeController.cs`** — botones de velocidad que modifican `Time.timeScale` y muestran el tiempo simulado vs. tiempo real.
4. **`MetricTracker.cs`** — suscriptor de `GameEvents.OnMetricsUpdated` que actualiza los textos de W, Wq, Lq, ρ, clientes atendidos, clientes perdidos.
5. **`Charts/LiveLineChart.cs`** — gráfico simple en tiempo real (longitud de cola vs. tiempo). Puede usarse el `UnityEngine.UI` nativo + LineRenderer, o un asset gratuito si el tiempo lo permite.
6. **Prefab `Dashboard.prefab`** que I3 podrá arrastrar a la escena.

### Contratos con otros roles

- **Con I1**: escribes en `SimulationConfig` (que I1 define). Te suscribes a `GameEvents`. **No llames métodos del `SimulationManager` directamente.**
- **Con I3**: entregas tus paneles como **Prefabs** (`Dashboard.prefab`, `MetricPanel.prefab`). I3 los coloca en la escena.
- **Con I4**: tus textos de métricas deben coincidir exactamente con las fórmulas documentadas. Si tu UI muestra "Wq: 5.32s", I4 debe poder reproducir ese 5.32 con su modelo de ProModel usando la misma semilla.

### No te metas con

- ❌ Lógica de cola, generación de números aleatorios, máquina de estados del cliente.
- ❌ La escena (`MainSimulation.unity`) — entregas Prefabs a I3.
- ❌ Sprites de personajes.

---

## I3 — Escena, Entidades y Arte

### Misión

Darle **vida visual** al simulador. Spawneo de clientes, movimiento entre puntos, máquina de estados visual, integración del arte (sprites, backgrounds). Eres el único integrante que toca `MainSimulation.unity` directamente.

### Entregables (en orden de prioridad)

1. **`CustomerSpawner.cs`** (Factory) — instancia clientes a la tasa que indique el `SimulationManager`.
2. **`CustomerEntity.cs`** — MonoBehaviour del cliente; mantiene su `CustomerData`, su estado actual, su sprite.
3. **`CustomerStateMachine.cs`** — 6 estados: `Entering`, `WaitingInLine`, `Ordering`, `WaitingDrink`, `Consuming`, `Leaving`.
4. **`BaristaEntity.cs` / `CashierEntity.cs`** — servidores. Heredan de `BaseServerEntity`.
5. **`Movement/WaypointMover.cs`** — helper para mover entidades suavemente entre puntos usando **DOTween**.
6. **Prefabs**: `Customer.prefab`, `Barista.prefab`, `Cashier.prefab` (en `Prefabs/Entities/`).
7. **Escena**: `Assets/_Project/Scenes/MainSimulation.unity`. Aquí colocas:
   - El `Dashboard.prefab` (de I2) en el Canvas.
   - Los puntos de spawn, cola, caja, barista, mesas (todos como `Transform` con tag claro).
   - El `SimulationManagerBootstrap.cs` (MonoBehaviour) que arranca al `SimulationManager.Instance` en `Awake()`.
8. **Integración del arte**: importar sprites pixel-art, configurar `Pixels Per Unit`, paletas de colores unificadas.

### Contratos con otros roles

- **Con I1**: te suscribes a `GameEvents.OnCustomerArrived` para spawnear, a `OnCustomerServed` para reproducir animación de salida. Llamas al Core solo para preguntar "¿cuál es la posición lógica del cliente X en la cola?".
- **Con I2**: recibes el `Dashboard.prefab` y lo colocas en el Canvas de la escena. **No modifiques sus scripts.**
- **Con I4**: tu escena debe poder pausarse para que I4 tome capturas de pantalla en momentos específicos para la documentación UML.

### No te metas con

- ❌ El generador LCG ni las fórmulas matemáticas.
- ❌ Los sliders ni el dashboard (su lógica es de I2).
- ❌ La exportación a CSV ni los cálculos de ProModel.

---

## I4 — ProModel, Estadística y Documentación UML

### Misión

Validar que el simulador de Unity es **estadísticamente correcto** y producir la documentación formal de Ingeniería de Software (UML).

### Entregables (en orden de prioridad)

1. **Modelo en ProModel** equivalente al de Unity:
   - Locaciones: `Entrada`, `Cola_Caja`, `Caja`, `Cola_Barista`, `Barista`, `Mesa_Consumo`, `Salida`.
   - Entidad: `Cliente`.
   - Arribos con distribución exponencial(λ).
   - Procesos con distribución exponencial(μ_caja, μ_barista).
   - Mismas tasas que las del `SimulationConfig` de Unity para comparación directa.

2. **Protocolo de exportación CSV desde Unity**:
   - Colaborar con I1 para que el `SimulationManager` exporte a `StreamingAssets/run_<seed>_<timestamp>.csv` con los eventos (arribo, atención, salida, abandono) y sus timestamps.
   - Importar el CSV en ProModel como tabla de arribos (User Distribution) para comparación pareada.

3. **Pruebas estadísticas del LCG** (con la semilla por defecto `12345` y `n=1000`):
   - **Chi-cuadrada** de uniformidad: 10 bins, α = 0.05.
   - **Test de Rachas** (Runs Test): detecta dependencias.
   - **Kolmogorov-Smirnov**: bondad de ajuste a U(0,1).
   - Entregar en un cuaderno Jupyter o Excel en `docs/anexos/pruebas-estadisticas.{ipynb,xlsx}`.

4. **Diagramas UML** (entregar como PNG en `docs/uml/`):
   - **Diagrama de clases** del paquete `Core/` y `Entities/`.
   - **Diagrama de estados** del `CustomerStateMachine`.
   - **Diagrama de secuencia** del flujo "llegada → atención → salida" (caso pedido físico y caso pedido web).
   - **Diagrama de casos de uso** del simulador (Usuario configura, Usuario observa métricas, Usuario exporta CSV).

5. **Documento de requerimientos** (`docs/anexos/requerimientos.md`):
   - Historias de usuario (`Como X, quiero Y, para Z`).
   - Requisitos funcionales y no funcionales.
   - Criterios de aceptación por historia.

6. **Reporte de comparación Unity vs. ProModel** (`docs/anexos/validacion-promodel.md`):
   - Tabla con métricas (W, Wq, Lq, ρ, clientes perdidos) lado a lado, mismo escenario, misma semilla.
   - Conclusión: ¿están dentro del margen del 5%? Si no, ¿por qué?

### Contratos con otros roles

- **Con I1**: solicítale el método `RunSimulation()` desde una consola .NET y la exportación CSV. Le entregas los resultados de las pruebas estadísticas para que ajuste el LCG si falla alguna.
- **Con I2**: validas que los números mostrados en el dashboard coincidan con tus cálculos.
- **Con I3**: necesitas que pause la escena en momentos específicos para tomar capturas para los diagramas.

### No te metas con

- ❌ Código de Unity (excepto el script de exportación CSV, coordinado con I1).
- ❌ Diseño de UI ni de escena.

---

## Matriz RACI resumida

| Tarea | I1 | I2 | I3 | I4 |
|---|---|---|---|---|
| Generador LCG | **R** | C | I | C |
| Cola FIFO y bypass web | **R** | I | C | C |
| Fórmulas M/M/c | **R** | C | I | A |
| Sliders λ/μ | C | **R** | I | I |
| Botones de velocidad | I | **R** | C | I |
| Dashboard de métricas | C | **R** | C | A |
| Spawner de clientes | I | I | **R** | I |
| Máquina de estados visual | C | I | **R** | C |
| Escena `MainSimulation.unity` | I | I | **R** | I |
| Integración de sprites/arte | I | I | **R** | I |
| Modelo ProModel | I | I | I | **R** |
| Pruebas estadísticas | C | I | I | **R** |
| Diagramas UML | C | C | C | **R** |
| README y /docs (general) | C | C | C | **R** |
| Setup de Git y proyecto | **R** | I | I | C |

> **R**=Responsible (lo hace), **A**=Accountable (rinde cuentas), **C**=Consulted (le preguntan), **I**=Informed (le avisan).
