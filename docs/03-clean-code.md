# 03 — Clean Code, SOLID y Definition of Done

Este documento define el estándar de calidad de código del proyecto. Es el checklist con el que se revisarán los Pull Requests.

---

## 1. Principios SOLID aplicados

### S — Single Responsibility

Una clase, una razón para cambiar. Si describes la clase con la palabra "y", divídela.

✅ `LCGRandomGenerator` (genera números pseudoaleatorios).
❌ `LCGRandomGenerator` que **además** dibuja gráficos de uniformidad.

### O — Open/Closed

Abierto a extensión, cerrado a modificación. Por eso `IQueueDiscipline` existe: agregar LIFO no debe forzar a editar `QueueController`.

### L — Liskov Substitution

Cualquier `BaseServerEntity` (Cajero, Barista) debe ser intercambiable sin romper el `QueueController`. Si `BaristaEntity.Serve()` lanza una excepción que `CashierEntity.Serve()` no lanza, estás violando Liskov.

### I — Interface Segregation

Mejor varias interfaces pequeñas que una grande. `IQueueDiscipline` solo expone `Enqueue`, `Dequeue`, `Count`. Si necesitas `Peek()` para algún caso, crea `IPeekableQueue : IQueueDiscipline`.

### D — Dependency Inversion

Las capas externas dependen de **abstracciones**, no de implementaciones concretas. El `QueueController` recibe un `IQueueDiscipline` por constructor, no instancia `new FifoQueue()` directamente.

```csharp
// ✅ Correcto
public class QueueController
{
    private readonly IQueueDiscipline _queue;
    public QueueController(IQueueDiscipline queue) => _queue = queue;
}

// ❌ Incorrecto
public class QueueController
{
    private readonly FifoQueue _queue = new FifoQueue();
}
```

---

## 2. Naming (en inglés para código, español para comentarios)

| Elemento | Convención | Ejemplo |
|---|---|---|
| Clases | `PascalCase` | `CustomerEntity`, `SimulationManager` |
| Métodos públicos | `PascalCase` | `ProcessOrder()`, `GetQueueLength()` |
| Métodos privados | `PascalCase` | `CalculateNextArrival()` |
| Propiedades | `PascalCase` | `public float ArrivalRate { get; set; }` |
| Campos privados | `_camelCase` | `private float _arrivalRate;` |
| Parámetros / locales | `camelCase` | `void Spawn(float rate)` |
| Constantes | `PascalCase` | `private const float MaxPatience = 120f;` |
| Enums | `PascalCase` (tipo y valores) | `CustomerState.WaitingInLine` |
| Interfaces | `IPascalCase` | `IQueueDiscipline`, `IServerEntity` |
| ScriptableObjects (asset) | `PascalCase` | `DefaultConfig.asset` |
| Prefabs | nombre del script raíz, sin sufijo | `CustomerEntity.cs` → `CustomerEntity.prefab` |
| Eventos | `On + sustantivo + verboPasado` | `OnCustomerServed`, `OnMetricsUpdated` |

**Comentarios y nombres de variables semánticos**: en español está bien (`tiempoEspera`, `clientesAtendidos`) solo si el equipo decide unificarlo. **Decisión por defecto**: identificadores en **inglés**, summaries XML en **español**.

---

## 3. Reglas mecánicas

### 3.1 Tamaño

- **Métodos**: ≤ 30 líneas. Si crece, extrae métodos privados.
- **Clases**: ≤ 300 líneas. Si crece, probablemente viola SRP.
- **Parámetros por método**: ≤ 4. Si necesitas más, agrupa en un struct/record.

### 3.2 Complejidad ciclomática

**Límite duro: 12 por método** (requisito del rubric de Ing. de Software). Mide cuántos caminos puede tomar el código (cada `if`, `else`, `case`, `for`, `while`, `&&`, `||` suma 1).

Si superas 12: usa **early returns**, **polimorfismo**, o **diccionarios de despacho**.

```csharp
// ❌ Complejidad alta
public float CalculatePrice(string product, int qty, bool isMember)
{
    if (product == "coffee") {
        if (qty > 10) return isMember ? 1.5f * qty : 2f * qty;
        else return isMember ? 1.8f * qty : 2.5f * qty;
    } else if (product == "tea") {
        // ...más ramas
    }
}

// ✅ Diccionario de precios + función pura
private static readonly Dictionary<string, float> BasePrices = new() {
    ["coffee"] = 2.5f,
    ["tea"]    = 2.0f,
};
public float CalculatePrice(string product, int qty, bool isMember)
{
    float basePrice = BasePrices[product];
    float discount = isMember ? 0.2f : 0f;
    float bulk     = qty > 10 ? 0.1f : 0f;
    return basePrice * qty * (1f - discount - bulk);
}
```

### 3.3 Comentarios

- **Por defecto: no escribas comentarios.** El nombre del método y de las variables ya explican el "qué".
- Escribe un comentario **solo si** el "por qué" no es obvio: una restricción matemática (`// + 0.0001 evita log(0) en exponencial`), una fórmula con referencia (`// Wq = Lq / λ — Little's Law`), un workaround documentado.
- **Comentarios XML obligatorios** en clases y métodos `public` del `Core/`:

```csharp
/// <summary>
/// Genera el siguiente valor pseudoaleatorio uniforme en (0, 1] usando
/// el método congruencial lineal con los parámetros de Numerical Recipes.
/// </summary>
/// <returns>Un float en el intervalo (0, 1].</returns>
public float NextFloat() { ... }
```

### 3.4 Magic numbers

❌ `if (queue.Count > 10) Reject();`
✅ `if (queue.Count > _config.MaxQueueLength) Reject();`

Toda constante que un humano podría querer cambiar va en `SimulationConfig` o como `private const` con nombre semántico.

### 3.5 Manejo de errores

- En el **Core**: lanza excepciones específicas (`ArgumentOutOfRangeException`, `InvalidOperationException`) cuando el contrato se viola.
- En **Entities** y **UI**: valida en los bordes, nunca dejes que un nulo silencioso se propague.
- No uses `try/catch` para flujo normal de control.

```csharp
// ✅ Validación en el borde
public LCGRandomGenerator(long seed)
{
    if (seed <= 0)
        throw new ArgumentOutOfRangeException(nameof(seed), "La semilla debe ser positiva.");
    _x = seed;
}
```

---

## 4. Reglas específicas de Unity

1. **Nunca `GameObject.Find()` ni `FindObjectOfType()` en `Update()`** — son O(n) sobre todos los objetos de la escena. Cachea referencias en `Awake()` o `[SerializeField]`.
2. **Prefiere `[SerializeField] private`** sobre `public`. Expones al inspector sin romper encapsulación.
3. **No instancies en `Update()`** sin un pool. Para clientes, usa un object pool si llegas a >50 simultáneos.
4. **Usa `TryGetComponent` en lugar de `GetComponent` + null check** cuando el componente puede no existir.
5. **No uses `string` como identificador de objetos** (`GameObject.Find("Customer1")`). Usa referencias tipadas.
6. **Coroutines vs `Task`**: para esperar tiempo de juego, usa `IEnumerator` + `WaitForSeconds`. No mezcles `async/await` con la vida del `MonoBehaviour` sin un `CancellationToken`.

---

## 5. Definition of Done (DoD)

Un PR **no se aprueba** si no cumple los 9 ítems:

- [ ] **Compila** sin errores ni warnings nuevos en la consola de Unity.
- [ ] **Play Mode** ejecuta sin null references ni stack traces.
- [ ] **Al menos 1 compañero** aprobó el PR en GitHub.
- [ ] Todo asset nuevo incluye su **`.meta`** en el commit.
- [ ] Toda clase pública del `Core/` tiene **comentario XML `<summary>`** en español.
- [ ] **Sin paths absolutos** (`grep "C:\\\\"` y `grep "/home/"` deben no retornar nada).
- [ ] **Sin magic numbers**: las constantes están en `SimulationConfig` o nombradas.
- [ ] **Complejidad ciclomática** ≤ 12 por método (puedes medirla con la extensión de C# en VS Code).
- [ ] **Nombres de commits** siguen Conventional Commits en español ([04-git-workflow.md](04-git-workflow.md)).

---

## 6. Code smells a vigilar

| Smell | Cómo se ve | Cómo arreglarlo |
|---|---|---|
| God Object | Una clase con +500 líneas y +20 métodos | Aplica SRP, extrae helpers |
| Feature Envy | Método que usa más atributos de OTRA clase que de la suya | Mueve el método a la clase correcta |
| Primitive Obsession | `void Spawn(float x, float y, float z, float speed, int state)` | Crea un struct `SpawnData` |
| Long Parameter List | >4 parámetros | Agrupa en un objeto de configuración |
| Duplicated Code | Mismas 3 líneas en 3 lugares | Extrae a método privado o helper |
| Comments explaining "what" | `// Incrementa el contador` antes de `count++` | Borra el comentario |
| Dead Code | Métodos sin referencias, variables sin uso | Bórralos. Git recuerda. |
| Shotgun Surgery | Un cambio pequeño toca 8 archivos | Centraliza la responsabilidad |

---

## 7. Cuándo refactorizar

**Regla de Three Strikes** (Martin Fowler):

1. La primera vez que escribes algo, hazlo simple.
2. La segunda vez (duplicas), aguanta el dolor pero anótalo.
3. La tercera vez, **refactoriza** y extrae la abstracción.

No persigas perfección prematura. Pero tampoco normalices el código sucio.
