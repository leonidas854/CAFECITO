# Índice de Documentación — CaféSim

Bienvenido. Esta carpeta contiene **toda** la información que necesitas para trabajar en el proyecto sin depender de explicaciones verbales. Lee los documentos en este orden la primera vez; después usa el índice como referencia rápida.

---

## Lectura obligatoria antes del primer commit

| # | Documento | Tiempo | Para quién |
|---|---|---|---|
| 1 | [01-setup.md](01-setup.md) | 20 min | **Todos** (Linux y Windows) |
| 2 | [04-git-workflow.md](04-git-workflow.md) | 15 min | **Todos** |
| 3 | [05-roles.md](05-roles.md) | 10 min | **Todos** (lee tu rol y entiende los demás) |
| 4 | [08-unity-convenciones.md](08-unity-convenciones.md) | 10 min | **Todos** (escenas, prefabs, `.meta`) |

---

## Documentos por tema

### Setup y entorno

- **[01-setup.md](01-setup.md)** — Instalación de Unity 6.3 LTS (`6000.3.16f1`), Git, Git LFS opcional, VS Code, configuración cross-platform (Arch Linux + Windows), verificación del entorno.

### Arquitectura y código

- **[02-arquitectura.md](02-arquitectura.md)** — Clean Architecture aplicada a Unity. Capas (Core / Data / Entities / Events / UI), regla de dependencias, patrones aplicados (Singleton, Observer, State Machine, Factory, ScriptableObject), puntos de extensibilidad.
- **[03-clean-code.md](03-clean-code.md)** — Principios SOLID, naming, comentarios XML, complejidad ciclomática, Definition of Done.

### Flujo de trabajo

- **[04-git-workflow.md](04-git-workflow.md)** — Estrategia de ramas (`main` / `develop` / `feature` / `bugfix`), Conventional Commits en español, plantilla de Pull Request, resolución de conflictos en escenas Unity.

### Equipo y planificación

- **[05-roles.md](05-roles.md)** — Responsabilidades detalladas de los 4 integrantes (I1, I2, I3, I4), entregables, fronteras de propiedad, contratos entre roles.
- **[06-sprint-plan.md](06-sprint-plan.md)** — Calendario día a día de los 14 días (Sprint 1: días 1-7, Sprint 2: días 8-14), ceremonias Scrum, hitos.

### Contenido académico

- **[07-simulacion-teoria.md](07-simulacion-teoria.md)** — Generador LCG (con corrección anti-`log(0)`), distribución exponencial por transformada inversa, fórmulas M/M/c (ρ, Lq, Wq, W), protocolo de validación Unity ↔ ProModel, pruebas estadísticas (Chi², Rachas, K-S).
- **[08-unity-convenciones.md](08-unity-convenciones.md)** — Reglas específicas de Unity: archivos `.meta`, `MainSimulation.unity` como zona exclusiva de I3, Prefabs como interfaz entre roles, `ScriptableObject` para configuración, política de carpetas.

---

## Atajos por rol

- **Eres I1 (Core/Lógica)** → lee `01`, `02`, `03`, `04`, `05`, `07`.
- **Eres I2 (UI/Tiempos)** → lee `01`, `02`, `03`, `04`, `05`, `08`.
- **Eres I3 (Escena/Entidades)** → lee `01`, `02`, `04`, `05`, `08` (a fondo).
- **Eres I4 (ProModel/Estadística/UML)** → lee `01`, `04`, `05`, `07` (a fondo).

---

## Convenciones de la documentación

- Todo en español.
- Bloques de código siempre con lenguaje declarado (` ```csharp `, ` ```bash `, ` ```text `).
- Comandos de terminal con prefijo del SO cuando aplica (`# Linux` / `# Windows`).
- Referencias internas con links relativos: `[texto](otro-doc.md)`.
- Diagramas en ASCII para que se rendericen en GitHub sin dependencias externas.
- Si actualizas un documento, **actualiza también este índice** si cambia su propósito o el orden de lectura.
