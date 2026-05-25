# 06 — Plan de Sprints (14 días, 2 sprints de 7 días)

Plan operativo día a día. Cada integrante sabe **qué entregar cada día** y cuándo se integra el trabajo.

---

## Ceremonias Scrum (async-first)

| Ceremonia | Cuándo | Duración | Quién |
|---|---|---|---|
| **Sprint Planning** | Día 1, Día 8 | 30 min | Todos |
| **Daily Standup** | Cada mañana | 15 min o texto en Discord | Todos |
| **Sprint Review** | Día 7, Día 14 | 30 min demo + 15 min retro | Todos |

### Estructura del Daily

Cada uno responde 3 preguntas (puede ser texto en Discord, no requiere call):

1. ¿Qué hice ayer?
2. ¿Qué haré hoy?
3. ¿Tengo algún bloqueo? (taggear a quien pueda ayudar)

---

## Sprint 1 — Días 1 al 7 — "Fundaciones"

> **Objetivo**: al final del día 7, un cliente puede spawnearse, entrar a la cola FIFO, moverse visualmente y ser atendido por el barista. Las métricas básicas funcionan aunque no estén pulidas.

### Día 1 (LUN) — Setup total + Sprint Planning

**Todos**:
- [ ] Completar checklist de [01-setup.md](01-setup.md) en sus máquinas.
- [ ] Clonar repo, verificar que Unity abre el proyecto sin errores.
- [ ] Sprint Planning de 30 min: revisar historias de usuario, asignar tareas del Sprint 1.
- [ ] Cada uno crea su rama: `feature/I[N]-setup-inicial`.

**Hito del día**: los 4 integrantes pueden abrir el proyecto en sus máquinas (Linux + Windows) sin errores de compilación.

### Día 2 (MAR) — Arquitectura base

| Integrante | Entregable |
|---|---|
| **I1** | Esqueleto de `SimulationManager.cs` (Singleton). Esqueleto de `LCGRandomGenerator.cs` con la fórmula congruencial. Esqueleto de `GameEvents.cs` con 4 eventos vacíos. |
| **I2** | Layout del Canvas en Unity: panel de sliders (4 sliders con valores hardcoded por ahora) + panel de métricas (4 textos vacíos) + panel de control de tiempo (5 botones). |
| **I3** | Prototipo de movimiento: un `Circle.prefab` que se mueve de A a B usando `Vector3.Lerp` o `Vector3.MoveTowards`. Sin lógica de cola todavía. |
| **I4** | Levantar ProModel, crear modelo en blanco con las 4 locaciones (Entrada, Cola, Caja, Salida). Iniciar el documento de requerimientos. |

### Día 3 (MIE) — Primer merge de integración

**Todos**:
- [ ] Cada uno hace PR de su rama a `develop`.
- [ ] **Verificación crítica**: el proyecto debe abrir sin errores en ambos SO (Linux Y Windows). Si alguien tiene errores de line endings → revisar `.gitattributes` y `git config`.
- [ ] Resolver cualquier conflicto temprano.

**Hito del día**: la rama `develop` tiene una base estable que los 4 pueden compilar.

### Día 4 (JUE) — Motor de cola operativo

| Integrante | Entregable |
|---|---|
| **I1** | `QueueController.cs` con `Queue<CustomerData>` FIFO operativa. Métodos `Enqueue`, `Dequeue`, `Count`. Tick básico que mueve clientes por la cola. |
| **I2** | Sliders conectados al `SimulationConfig`. Mover el slider de "Cajeros" debe actualizar en runtime una variable que I1 pueda leer. |
| **I3** | `CustomerSpawner.cs` que instancia `Customer.prefab` cada N segundos basado en λ. |
| **I4** | Modelo ProModel con flujo Entrada → Cola → Caja → Salida funcionando. Tasas hardcoded por ahora. |

### Día 5 (VIE) — Flujo dual: físico vs. web

| Integrante | Entregable |
|---|---|
| **I1** | `OrderSystem.cs`: pedidos web bypassean la caja y entran directo a la cola del barista. Probabilidad configurable por `webOrderProbability`. |
| **I3** | `CustomerStateMachine.cs`: 6 estados implementados con sus `Enter()`, `Update()`, `Exit()`. Cliente cambia de sprite/color según estado. |
| **I2** | Texto en el dashboard mostrando "Clientes en cola" en vivo (lectura simple sin métricas calculadas). |
| **I4** | Implementar el flujo web en ProModel (atributo `tipo_pedido`, ruteo condicional). |

### Día 6 (SAB) — Integración

**Todos**:
- [ ] I3 arrastra los prefabs de I1 (Bootstrap del SimulationManager) e I2 (Dashboard) a `MainSimulation.unity`.
- [ ] **Demo interna**: cliente spawneado → entra a cola → camina al cajero → camina al barista → sale.
- [ ] Bug fixes de la integración.

### Día 7 (DOM) — Sprint Review + Retro + Merge a `main`

**Todos**:
- [ ] **Sprint Review** (30 min): demo funcional grabada.
- [ ] **Retrospectiva** (15 min): ¿qué salió bien?, ¿qué mejorar?, ¿qué bloquea para el Sprint 2?
- [ ] PR de `develop` → `main`. Tag `v0.1-sprint1`.

**Hito del Sprint 1**: simulador funcional con cola FIFO, bypass web, movimiento visual. Sin métricas pulidas, sin abandono, sin polish.

---

## Sprint 2 — Días 8 al 14 — "Features + Polish + Entrega"

> **Objetivo**: al final del día 14, el simulador tiene métricas reales, abandono por paciencia, control de velocidad, polish visual, y builds de Linux + Windows + WebGL.

### Día 8 (LUN) — Sprint Planning + Control de Tiempos + Fórmulas

**Todos**: Sprint Planning de 30 min.

| Integrante | Entregable |
|---|---|
| **I2** | `TimeController.cs`: botones 0x / 1x / 2x / 5x / 10x funcionando con `Time.timeScale`. Texto "Tiempo simulado: HH:MM:SS". |
| **I1** | `MetricCalculator.cs` con fórmulas M/M/c (ver [07-simulacion-teoria.md](07-simulacion-teoria.md)): ρ, Lq, Wq, W en tiempo real. Disparar `GameEvents.OnMetricsUpdated` cada N ticks. |
| **I3** | Refactor del `CustomerEntity` para que sus animaciones respeten `Time.timeScale` (usar `Time.deltaTime` correctamente). |
| **I4** | Empezar pruebas estadísticas del LCG: extraer 1000 valores con semilla 12345, calcular Chi-cuadrada en Excel. |

### Día 9 (MAR) — Lógica de Abandono

| Integrante | Entregable |
|---|---|
| **I3** | Timer de paciencia en `CustomerEntity`. Si en `WaitingInLine` supera `customerPatience` segundos → estado `Leaving` y disparar `GameEvents.OnCustomerAbandoned`. |
| **I1** | Suscriptor del `OnCustomerAbandoned` en el `SimulationManager` que incrementa el contador de clientes perdidos. |
| **I2** | Métrica "Clientes perdidos" agregada al dashboard. Color rojo si supera el 20% de los atendidos. |
| **I4** | Diagrama de estados del cliente en formato UML (PlantUML o draw.io). Primer borrador. |

### Día 10 (MIE) — Integración total de métricas

**Todos**:
- [ ] I2 conecta los textos del dashboard a `GameEvents.OnMetricsUpdated`.
- [ ] **Validación cruzada**: I1 corre la simulación 5 min con semilla fija, I4 corre ProModel con misma semilla, comparan W, Wq, Lq, ρ.
- [ ] Si hay discrepancia >5% → debugging conjunto.

### Día 11 (JUE) — Pulido visual

| Integrante | Entregable |
|---|---|
| **I3** | Integrar pixel art final (Nano Banana o similar). Background del local. Posicionar las zonas (espera, caja, barista, mesas) alineadas al pixel art. |
| **I2** | Paleta de colores unificada en el dashboard. Iconos limpios para cada métrica (lupa, reloj, taza, X roja). |
| **I1** | Optimización del Core: profiling rápido para asegurar que con 100+ clientes simultáneos no haya frame drops. |
| **I4** | Diagrama de clases del paquete `Core/`. Diagrama de casos de uso. |

### Día 12 (VIE) — DOTween + suavizado de animaciones

| Integrante | Entregable |
|---|---|
| **I3** | Integrar DOTween. Movimientos de clientes con `transform.DOMove(...).SetEase(Ease.OutQuad)`. Aparición de paneles UI con tween de fade. |
| **I2** | Animar el cambio de valores numéricos en el dashboard (interpolación visual del texto, no salto). |
| **I1** | Ajustes finos en el LCG si las pruebas estadísticas de I4 detectaron problemas. |
| **I4** | Tests estadísticos completos (Chi², Rachas, K-S). Reporte preliminar de comparación Unity vs. ProModel. |

### Día 13 (SAB) — Pruebas de estrés + Bug fixes

**Todos**:
- [ ] **Escenarios de estrés**:
  - λ = 30 clientes/min, 1 cajero, 1 barista → debe haber colas largas y abandonos.
  - λ = 2 clientes/min, 3 cajeros, 3 baristas → debe haber utilización baja (ρ < 0.3).
  - Time.timeScale = 10x durante 5 min → métricas no deben corromperse.
- [ ] Documentar cualquier bug encontrado, asignar y arreglar.
- [ ] PR con bugfixes a `develop`.

### Día 14 (DOM) — Build final + Sprint Review

**Todos**:
- [ ] **Builds**:
  - [ ] WebGL → carpeta `Builds/WebGL/` (para presentación en navegador).
  - [ ] Linux x64 → `Builds/Linux/`.
  - [ ] Windows x64 → `Builds/Windows/`.
- [ ] PR final de `develop` → `main`. Tag `v1.0-final`.
- [ ] **I4**: documento de validación Unity vs. ProModel terminado. Diagramas UML finalizados.
- [ ] **Sprint Review con docentes**: demo en vivo, presentación de métricas, comparación con ProModel.
- [ ] **Retrospectiva final**: lecciones aprendidas, escribir cierre en el README.

**Hito final**: proyecto entregable con simulador funcional, validación estadística y documentación completa.

---

## Definition of Ready (DoR) por tarea

Antes de tomar una tarea, debe tener:

- [ ] Asignado a un integrante específico (I1/I2/I3/I4).
- [ ] Criterio de aceptación claro (¿cómo sé que está terminada?).
- [ ] Estimación en horas (1h, 2h, medio día, día entero).
- [ ] Dependencias identificadas (¿necesito que otro termine algo primero?).

---

## Riesgos identificados y plan de mitigación

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| Conflictos de merge en `MainSimulation.unity` | Alta | Alto | Solo I3 toca la escena. Otros entregan Prefabs. |
| Discrepancia Unity vs. ProModel | Media | Alto | Día 10 y 12 dedicados a validación. Si falla, ajustar parámetros. |
| Diferencia de versión de Unity entre integrantes | Baja | Crítico | Verificar `ProjectVersion.txt` el día 1. Reinstalar si difiere. |
| DOTween no se instala correctamente | Baja | Medio | Plan B: usar `Vector3.Lerp` nativo. |
| Time.timeScale = 10x rompe la física del cliente | Media | Medio | Día 8 dedicado a `Time.deltaTime` consistente. Pruebas el día 13. |
| Un integrante no entrega su parte a tiempo | Media | Alto | Daily standup detecta bloqueos. Pair programming si urge. |
