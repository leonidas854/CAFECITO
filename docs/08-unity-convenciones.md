# 08 — Convenciones de Unity (Escenas, Prefabs, .meta)

Reglas específicas del entorno Unity para evitar romper el proyecto del compañero. Si solo lees un documento de esta carpeta antes de tu primer commit, que sea este.

---

## 1. La Regla del `.meta` (la más importante)

Cada archivo y cada carpeta dentro de `Assets/` tiene un archivo `.meta` invisible asociado (`CustomerEntity.cs` → `CustomerEntity.cs.meta`). Este archivo contiene el GUID que Unity usa para referenciar el asset desde otros assets (escenas, prefabs, componentes).

### Reglas

1. **Siempre commitea el `.meta`** junto al archivo o carpeta que describe.
2. **Si renombras** un archivo en VS Code o en la terminal, **renombra también el `.meta`** (Unity normalmente lo hace por ti si renombras dentro del editor).
3. **Si mueves** un archivo a otra carpeta, **mueve también el `.meta`**.
4. **Si eliminas** un archivo, **elimina también el `.meta`**.

### Verificación rápida

Antes de un commit, ejecuta:

```bash
# Lista archivos en staging sin su .meta correspondiente
git diff --cached --name-only | grep -v '\.meta$' | while read f; do
  if [ -e "Assets/" ] && [ ! -e "${f}.meta" ]; then
    echo "FALTA META: ${f}"
  fi
done
```

Si imprime algo, **detente** y agrega el `.meta` antes de commitear.

### Síntoma de un `.meta` faltante

El compañero abre el proyecto y ve mensajes en la consola tipo:

```text
Missing reference: The script of the component is missing on GameObject 'Customer'.
```

O peor: los prefabs aparecen como rectángulos rosados.

---

## 2. La Regla de la Escena Única

`Assets/_Project/Scenes/MainSimulation.unity` es la **única** escena del proyecto. Y la edita **solo el Integrante 3**.

### Por qué

Las escenas de Unity (`.unity`) son archivos YAML grandes con GUIDs y referencias entrelazadas. Si dos integrantes modifican la misma escena en ramas distintas, el merge es casi siempre destructivo.

### Cómo trabajar sin tocar la escena (I1, I2, I4)

1. **Trabaja en un Prefab**, no en la escena.
2. Crea el Prefab vacío: en Unity, **GameObject → Create Empty**, arrastra al panel de Project en `Assets/_Project/Prefabs/`.
3. Programa tus componentes y agrégalos al Prefab.
4. Prueba el Prefab abriéndolo desde el Project (doble clic → Prefab Mode).
5. Commitea tu Prefab + scripts + `.meta`.
6. En el siguiente Daily, avisa a I3 que tu Prefab está listo. I3 lo arrastra a la escena en un PR aparte.

### Cómo trabajar EN la escena (solo I3)

1. Antes de abrir Unity: `git pull origin develop`.
2. Abre `MainSimulation.unity`.
3. **Cierra Unity antes de cambiar de rama** (`git checkout otra-rama`). Unity puede dejar la escena en estado inconsistente si cambias el branch con el editor abierto.
4. Commitea con frecuencia para minimizar la ventana de conflicto.

---

## 3. Prefabs como contrato entre roles

Un Prefab es el "contrato" entre integrantes. I2 le entrega a I3 un `Dashboard.prefab`; I1 le entrega un `SimulationManagerBootstrap.prefab`. I3 los coloca en la escena.

### Estructura de carpetas

```text
Assets/_Project/Prefabs/
├── Entities/
│   ├── Customer.prefab
│   ├── Barista.prefab
│   └── Cashier.prefab
└── UI/
    ├── Dashboard.prefab
    ├── MetricPanel.prefab
    └── TimeControlPanel.prefab
```

### Nombre del Prefab = nombre del script raíz

`CustomerEntity.cs` → `Customer.prefab` (sin el sufijo `Entity` en el Prefab para que sea legible).
`DashboardUI.cs` → `Dashboard.prefab`.

### Prefab Variants

Si necesitas variar un Prefab (ej. `CustomerVIP.prefab` que hereda de `Customer.prefab`), usa **Prefab Variants** (clic derecho sobre el prefab base → Create → Prefab Variant). No dupliques el prefab base.

### Nested Prefabs

Está permitido (`Dashboard.prefab` puede contener `MetricPanel.prefab` adentro). Funciona bien en Unity 6.

---

## 4. ScriptableObjects para configuración

Cualquier parámetro que un humano pueda querer cambiar **sin recompilar** va en un `ScriptableObject` (no en un campo hardcoded).

### Estructura

```text
Assets/_Project/Settings/
├── DefaultConfig.asset       # Instancia base de SimulationConfig
├── ScenarioBusyHour.asset    # Escenario "hora pico"
├── ScenarioQuietHour.asset   # Escenario "hora muerta"
└── ScenarioWebHeavy.asset    # Escenario "alta proporción web"
```

### Por qué importa

- Permite a I4 crear escenarios para sus validaciones sin tocar código.
- Permite a los docentes probar valores extremos sin pedirnos un build nuevo.
- Mantiene el código libre de magic numbers.

### Cómo crear uno

```csharp
[CreateAssetMenu(fileName = "SimulationConfig", menuName = "CafeSim/Simulation Config")]
public class SimulationConfig : ScriptableObject
{
    public float arrivalRate = 5f;
    // ...
}
```

Luego en Unity: clic derecho en `Assets/_Project/Settings/` → **Create → CafeSim → Simulation Config**.

---

## 5. Tags y Layers

Reservamos algunos tags y layers para evitar conflictos.

### Tags

| Tag | Uso |
|---|---|
| `Customer` | GameObjects de clientes |
| `Barista` | GameObjects del barista |
| `Cashier` | GameObjects del cajero |
| `SpawnPoint` | Transform desde donde aparecen los clientes |
| `QueuePoint` | Transforms que definen las paradas de la cola |
| `ExitPoint` | Transform de salida del local |

### Layers

| Layer | Uso |
|---|---|
| `Default` | Todo lo no clasificado |
| `UI` | Canvas y elementos de UI |
| `Entities` | Sprites de personajes |
| `Background` | Pixel art del fondo |

No agregues tags ni layers nuevos sin discutirlo en el daily — afectan los Project Settings y por tanto a todos.

---

## 6. Sprites y arte

### Configuración por defecto

Al importar un sprite pixel-art:

| Propiedad | Valor |
|---|---|
| Texture Type | Sprite (2D and UI) |
| Pixels Per Unit | **32** (decisión del equipo, ajustable día 11) |
| Filter Mode | **Point (no filter)** ← crítico para pixel art |
| Compression | None |
| Wrap Mode | Clamp |

Aplica estas opciones desde el inspector del sprite y haz clic en **Apply**.

### Estructura de Art

```text
Assets/_Project/Art/
├── Sprites/
│   ├── Customers/      # Variantes de clientes
│   ├── Servers/        # Cajero, barista
│   └── Props/          # Mesas, sillas, máquina de café
├── Backgrounds/
│   └── Cafe_Layout.png
└── UI/
    ├── Icons/          # Iconos del dashboard
    └── Panels/         # Texturas 9-slice para paneles
```

### Paleta de colores

Mantener una paleta unificada (decisión Día 11). Definirla en `Assets/_Project/Settings/Palette.asset` (un `ScriptableObject` simple con campos `Color`).

---

## 7. URP — Universal Render Pipeline

El proyecto usa URP 2D (`com.unity.render-pipelines.universal: 17.3.0`).

- **No cambies el pipeline** a Built-in ni a HDRP sin discutirlo. Romperías los materiales y las luces.
- Los assets de URP viven en `Assets/Settings/` (carpeta autogenerada por Unity, no la nuestra).
- Si tu sprite no se ve, verifica que el material asignado sea `Sprite-Lit-Default` o `Sprite-Unlit-Default`.

---

## 8. Input System (nuevo)

El proyecto usa el **Input System** nuevo (`com.unity.inputsystem`), no el viejo `Input.GetKey()`.

- Acciones definidas en `Assets/InputSystem_Actions.inputactions`.
- Para leer input: usa `PlayerInput` component o `InputAction` directamente.
- Para el simulador (sin interacción del usuario salvo clicks UI), probablemente no necesitas tocar nada.

---

## 9. Test Runner

Tests con `com.unity.test-framework`. Se ejecutan desde **Window → General → Test Runner**.

### Estructura de tests recomendada

```text
Assets/_Project/Tests/
├── EditMode/                  # Tests del Core (sin Unity runtime)
│   ├── LCGRandomGeneratorTests.cs
│   └── MetricCalculatorTests.cs
└── PlayMode/                  # Tests con escena cargada
    └── CustomerSpawnerTests.cs
```

Los tests del `Core/` deben ser **EditMode** (no requieren Unity runtime → más rápidos, ejecutables sin abrir Unity).

### Ejemplo de test del LCG

```csharp
using NUnit.Framework;
using CafeSim.Core;

public class LCGRandomGeneratorTests
{
    [Test]
    public void NextFloat_ConSemillaFija_EsReproducible()
    {
        var rngA = new LCGRandomGenerator(12345);
        var rngB = new LCGRandomGenerator(12345);

        for (int i = 0; i < 100; i++)
            Assert.AreEqual(rngA.NextFloat(), rngB.NextFloat());
    }

    [Test]
    public void NextFloat_NuncaDevuelveCero()
    {
        var rng = new LCGRandomGenerator(1);
        for (int i = 0; i < 10000; i++)
            Assert.Greater(rng.NextFloat(), 0f);
    }
}
```

---

## 10. Builds

Configuración para los builds del Día 14.

### Build Settings (File → Build Settings)

| Plataforma | Backend | Architecture | Compresión |
|---|---|---|---|
| Linux | Mono | x86_64 | LZ4 |
| Windows | Mono | x86_64 | LZ4 |
| WebGL | — | — | Gzip |

### Carpeta de salida

```text
Builds/
├── Linux/CafeSim_Linux/
├── Windows/CafeSim_Windows/
└── WebGL/CafeSim_WebGL/        # Subir a itch.io o GitHub Pages para presentación
```

Esta carpeta está en `.gitignore`; no se commitea. Para distribuir, comprimir y subir como release de GitHub.

---

## 11. Errores frecuentes

| Síntoma | Causa | Solución |
|---|---|---|
| "Missing script" en escena al abrir | `.meta` perdido o renombrado | Recuperar del commit anterior; agregar el `.meta` |
| Material rosado en sprite | URP no encuentra el shader | Reasignar `Sprite-Lit-Default` |
| Cliente no se mueve con `Time.timeScale = 10` | Usa `Update()` y `transform.position += vector` | Cambiar a `vector * Time.deltaTime` |
| Conflicto de YAML en `.prefab` | Dos integrantes modificaron el prefab | Usar `UnityYAMLMerge` ([04-git-workflow.md](04-git-workflow.md)) |
| Proyecto no abre tras pull | Versión de Unity distinta | Verificar `ProjectVersion.txt`, reinstalar editor |
| Builds gigantes (>500 MB) | Sprites sin comprimir | Override de compresión por plataforma en cada sprite |
