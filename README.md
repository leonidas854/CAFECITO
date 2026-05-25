# CaféSim — Simulador de Cafetería 2D

Proyecto académico integrador de **7mo Semestre** para las materias de **Simulación** e **Ingeniería de Software**. Modela una cafetería con teoría de colas y compara dos escenarios: ordenamiento puramente físico vs. híbrido con inyección de pedidos web. El mismo modelo se valida en **ProModel** y mediante pruebas estadísticas (Chi-cuadrada, Rachas, K-S).

---

## Datos del proyecto

| Campo | Valor |
|---|---|
| Motor | **Unity 6.3 LTS** — versión exacta `6000.3.16f1` (revision `a56f230f6470`) |
| Plantilla | 2D (URP) |
| Lenguaje | C# (.NET estándar de Unity) |
| Plataformas objetivo | Linux, Windows, WebGL |
| Validación cruzada | ProModel + Excel/Python |
| Equipo | 4 integrantes |
| Duración | 14 días (2 sprints de 7 días) |
| Repo | <https://github.com/leonidas854/CAFECITO> |

> **Crítico:** los 4 integrantes deben instalar **exactamente** la build `6000.3.16f1`. Cualquier diferencia de revisión corrompe el proyecto al hacer `checkout`.

---

## Cómo empezar (lectura obligatoria por integrante)

Toda la documentación está en [`docs/`](docs/). Empieza por el índice [docs/00-INDEX.md](docs/00-INDEX.md) y luego lee **al menos** estos tres documentos antes de tu primer commit:

1. [docs/01-setup.md](docs/01-setup.md) — Instalación de Unity, Git y configuración del entorno en Linux y Windows.
2. [docs/04-git-workflow.md](docs/04-git-workflow.md) — Ramas, commits convencionales, Pull Requests.
3. [docs/05-roles.md](docs/05-roles.md) — Tu rol específico (I1, I2, I3 o I4) y tus entregables.

---

## Clonar el repositorio (resumen)

**Antes de clonar**, configura Git en tu máquina (solo una vez en la vida):

```bash
# En Linux (Arch / Ubuntu / Fedora)
git config --global core.autocrlf false
git config --global core.eol lf

# En Windows (Git Bash o PowerShell)
git config --global core.autocrlf input
git config --global core.safecrlf false
```

Luego clona y abre el proyecto:

```bash
git clone https://github.com/leonidas854/CAFECITO.git
cd CAFECITO
```

Abre **Unity Hub** → **Add project from disk** → selecciona la carpeta `CAFECITO`. Unity reconstruirá la carpeta `Library/` la primera vez (puede tardar 3-5 minutos). Detalles completos en [docs/01-setup.md](docs/01-setup.md).

---

## Arquitectura (Clean Architecture aplicada a Unity)

```text
Assets/_Project/
├── Scripts/
│   ├── Core/        # Dominio puro C#. LCG, colas, fórmulas. SIN UnityEngine.
│   ├── Data/        # ScriptableObjects: parámetros λ, μ, paciencia, semilla.
│   ├── Entities/    # MonoBehaviours: Cliente, Cajero, Barista, Spawner.
│   ├── Events/      # Eventos estáticos (Observer pattern). Desacoplador.
│   └── UI/          # Dashboard, sliders, controles de tiempo, métricas.
├── Prefabs/         # Plantillas reutilizables (Customer, Barista, UI).
├── Scenes/          # MainSimulation.unity (solo modifica I3).
└── Art/             # Sprites, backgrounds, iconos.
```

**Regla de dependencias** (no puede invertirse):

```text
UI ──► Events ◄── Entities ──► Core
                   │
                   ▼
                  Data
```

Justificación y patrones (Singleton, Observer, State Machine, Factory, ScriptableObject) en [docs/02-arquitectura.md](docs/02-arquitectura.md).

---

## Reglas de oro del equipo

1. **Nunca** commit directo a `main` ni a `develop`. Siempre por rama `feature/I[N]-...` y PR aprobado por otro integrante.
2. **`MainSimulation.unity` es territorio del Integrante 3.** I1, I2 e I4 entregan **Prefabs**, no editan la escena.
3. **Siempre commit del `.meta`** junto al archivo o carpeta que describe. Un `.meta` faltante rompe referencias en la máquina del compañero.
4. **Cero rutas absolutas** en código. Usa `Application.dataPath`, `Application.streamingAssetsPath` o `Resources.Load()`.
5. **El Core no conoce a Unity.** No importes `UnityEngine` en `Scripts/Core/`. Esto permite probar la lógica matemática con NUnit puro.
6. **Conflicto de escena → reunión de 10 min.** No intentes resolver merge conflicts de `.unity` solo: descarta y re-aplica los cambios.

---

## Roles del equipo (resumen)

| # | Rol | Responsable de |
|---|---|---|
| **I1** | Core / Lógica / LCG | `SimulationManager`, `LCGRandomGenerator`, `QueueController`, `OrderSystem`, fórmulas M/M/c |
| **I2** | UI / Dashboard / Tiempos | `DashboardUI`, `MetricTracker`, `TimeController`, sliders λ/μ, gráficos en tiempo real |
| **I3** | Escena / Entidades / Arte | `CustomerSpawner`, `CustomerEntity`, `CustomerStateMachine`, `BaristaEntity`, integración de sprites, DOTween, escena principal |
| **I4** | ProModel / Estadística / UML | Modelo equivalente en ProModel, exportación CSV desde Unity, pruebas Chi² / Rachas / K-S, diagramas UML, documentación formal |

Detalle por sprint y por día en [docs/05-roles.md](docs/05-roles.md) y [docs/06-sprint-plan.md](docs/06-sprint-plan.md).

---

## Convenciones de commits (Conventional Commits, en español)

```text
tipo(alcance): descripción en minúsculas
```

Tipos: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`.

Ejemplos correctos:

```text
feat(core): implementar lcg con semilla configurable
fix(ui): slider de lambda no actualizaba al manager
test(lcg): agregar prueba de chi-cuadrada con semilla 12345
docs(readme): agregar guia de setup para windows
```

Más ejemplos y guía de PRs en [docs/04-git-workflow.md](docs/04-git-workflow.md).

---

## Soporte y dudas

- Dudas técnicas → canal de Discord del equipo.
- Conflictos de merge en escenas → reunión inmediata.
- Documentación faltante o desactualizada → abrir issue en GitHub con etiqueta `docs`.
