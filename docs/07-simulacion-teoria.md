# 07 — Teoría de Simulación, LCG y Validación con ProModel

Este documento sostiene el componente académico de la materia **Simulación**: cómo se generan los números pseudoaleatorios, cómo se modela el sistema con teoría de colas, y cómo se valida contra ProModel.

---

## 1. Generador Congruencial Lineal (LCG)

### Fórmula

Cada nuevo número se calcula a partir del anterior:

```text
X_{n+1} = (a · X_n + c) mod m
U_n     = X_n / m       → uniforme en (0, 1)
```

### Parámetros elegidos (Numerical Recipes)

| Parámetro | Valor | Justificación |
|---|---|---|
| `a` (multiplicador) | `1664525` | Validado en literatura, periodo máximo |
| `c` (incremento) | `1013904223` | Coprimo con `m` |
| `m` (módulo) | `2^32 = 4294967296` | Permite aritmética en `long` sin overflow |
| `X_0` (semilla) | `12345` (configurable) | Cualquier entero positivo |

### Implementación C# (corregida para evitar `log(0)`)

```csharp
using System;

namespace CafeSim.Core
{
    /// <summary>
    /// Generador pseudoaleatorio congruencial lineal (LCG).
    /// Implementa la fórmula X_{n+1} = (a · X_n + c) mod m con los parámetros
    /// de Numerical Recipes. Reproducible con la misma semilla.
    /// </summary>
    public sealed class LCGRandomGenerator
    {
        private const long A = 1664525L;
        private const long C = 1013904223L;
        private const long M = 4294967296L; // 2^32

        // Cota mínima para evitar que NextFloat() devuelva exactamente 0
        // (lo cual rompería el cálculo de la exponencial: -log(0) = +∞).
        private const float Epsilon = 1e-7f;

        private long _x;

        public LCGRandomGenerator(long seed)
        {
            if (seed <= 0)
                throw new ArgumentOutOfRangeException(nameof(seed), "La semilla debe ser un entero positivo.");
            _x = seed;
        }

        /// <summary>
        /// Devuelve el siguiente número pseudoaleatorio uniforme en (0, 1].
        /// </summary>
        public float NextFloat()
        {
            _x = (A * _x + C) % M;
            float u = (float)_x / M;
            return u < Epsilon ? Epsilon : u;
        }
    }
}
```

> **Por qué `Epsilon` en lugar de `0`**: la transformada inversa de la exponencial usa `-ln(U)/rate`. Si `U = 0`, el resultado es `+∞`, lo que congela la simulación. La corrección sustituye `0` por `1e-7`, que está muy por debajo de la precisión de cualquier tiempo simulado.

### Pruebas de validación del LCG (responsabilidad de I4)

Con semilla `12345` y `n = 1000`:

1. **Chi-cuadrada de uniformidad** (10 bins, α = 0.05).
   - Hipótesis nula: los datos siguen U(0,1).
   - Aceptar si `χ²_calculado < χ²_crítico(9 gl, 0.05) = 16.92`.

2. **Test de Rachas (Runs Test)**.
   - Detecta dependencia entre observaciones consecutivas.
   - Aceptar si `|Z| < 1.96` para α = 0.05.

3. **Kolmogorov-Smirnov**.
   - Compara la CDF empírica con la CDF teórica U(0,1).
   - Aceptar si `D_n < D_crítico(n=1000, α=0.05) = 0.043`.

Resultados se documentan en `docs/anexos/pruebas-estadisticas.{ipynb|xlsx}`.

---

## 2. Distribución Exponencial por Transformada Inversa

Para generar tiempos de llegada (entre clientes) y de servicio:

```text
T = -ln(U) / λ        donde U ~ U(0,1) y λ es la tasa (clientes/min)
```

Justificación: si `T` sigue Exp(λ), su CDF es `F(t) = 1 - e^(-λt)`. Invirtiendo:
`F^{-1}(u) = -ln(1-u)/λ`. Como `1-U` también es uniforme, simplificamos a `-ln(U)/λ`.

### Implementación C#

```csharp
using System;

namespace CafeSim.Core
{
    /// <summary>
    /// Genera tiempos exponenciales por transformada inversa.
    /// </summary>
    public static class ExponentialDistribution
    {
        /// <summary>
        /// Devuelve un tiempo aleatorio según Exp(rate).
        /// </summary>
        /// <param name="rng">Generador uniforme (LCG).</param>
        /// <param name="rate">Tasa (clientes por unidad de tiempo). Debe ser > 0.</param>
        public static float Sample(LCGRandomGenerator rng, float rate)
        {
            if (rate <= 0f)
                throw new ArgumentOutOfRangeException(nameof(rate), "La tasa debe ser positiva.");

            float u = rng.NextFloat();
            return -(float)Math.Log(u) / rate;
        }
    }
}
```

---

## 3. Modelo del sistema: M/M/c con bypass

### Notación de Kendall: M/M/c/∞

- **M** primera (Markovian arrivals): llegadas con tiempos entre arribos exponenciales (proceso Poisson).
- **M** segunda (Markovian service): tiempos de servicio exponenciales.
- **c**: número de servidores (cajeros, baristas; configurable desde la UI).
- **∞**: capacidad de cola infinita (en la práctica, paciencia limitada → abandono).

### Variables del modelo

| Símbolo | Significado | Unidad |
|---|---|---|
| `λ` | Tasa de llegadas | clientes/min |
| `μ` | Tasa de servicio (por servidor) | clientes/min |
| `c` | Número de servidores | entero ≥ 1 |
| `ρ` | Utilización del sistema = `λ / (c · μ)` | adimensional (0 a 1) |
| `Lq` | Número esperado de clientes en cola | clientes |
| `Wq` | Tiempo esperado en cola | min |
| `W` | Tiempo esperado en el sistema (cola + servicio) | min |
| `p_w` | Probabilidad de que un pedido sea web | 0 a 1 |

### Fórmulas (Ley de Little y M/M/c)

```text
ρ  = λ / (c · μ)                      ← debe ser < 1 para estabilidad
Wq = Lq / λ                           ← Ley de Little
W  = Wq + 1/μ                         ← tiempo total en sistema
```

Para `Lq` en M/M/c se usa la fórmula de Erlang-C:

```text
       (cρ)^c · ρ
Lq = ─────────────── · P_0
       c! · (1-ρ)²

donde P_0 es la probabilidad de sistema vacío:
        ┌  c-1                                    ┐ ^(-1)
        │  ∑ (cρ)^n / n!   +   (cρ)^c / (c!(1-ρ)) │
P_0 =   │ n=0                                     │
        └                                         ┘
```

Para `c = 1` (cola simple M/M/1) las fórmulas se simplifican a:

```text
ρ  = λ / μ
Lq = ρ² / (1 - ρ)
Wq = ρ / (μ - λ)
W  = 1 / (μ - λ)
```

### Implementación recomendada (`Core/Metrics/MetricCalculator.cs`)

```csharp
namespace CafeSim.Core.Metrics
{
    /// <summary>
    /// Cálculo de métricas de teoría de colas (Erlang-C para M/M/c).
    /// Funciones puras: ningún side-effect, fácilmente unit-testables.
    /// </summary>
    public static class MetricCalculator
    {
        /// <summary>
        /// Utilización del sistema. Debe ser estrictamente menor a 1 para
        /// que el sistema sea estable (de lo contrario, la cola crece sin límite).
        /// </summary>
        public static float Utilization(float lambda, float mu, int servers)
            => lambda / (servers * mu);

        /// <summary>
        /// Tiempo promedio en cola, por Ley de Little.
        /// </summary>
        public static float WaitingTimeInQueue(float lq, float lambda)
            => lq / lambda;

        /// <summary>
        /// Tiempo promedio en el sistema (cola + servicio).
        /// </summary>
        public static float TimeInSystem(float wq, float mu)
            => wq + 1f / mu;

        // Lq por Erlang-C — pendiente de implementación en Sprint 2.
    }
}
```

---

## 4. Flujo dual: pedido físico vs. pedido web

### Pedido físico (probabilidad `1 - p_w`)

```text
Entrada → Cola_Caja → Caja (μ_caja) → Cola_Barista → Barista (μ_barista) → Mesa → Salida
```

### Pedido web (probabilidad `p_w`)

```text
Entrada → [skip caja] → Cola_Barista → Barista (μ_barista) → Mesa → Salida
```

**Implicación**: subir `p_w` reduce la presión sobre la caja pero aumenta la presión sobre el barista. El simulador permite explorar este trade-off variando `p_w` desde el dashboard en tiempo real.

---

## 5. Lógica de abandono (paciencia)

Cada cliente lleva un `patienceTimer`. Al entrar al estado `WaitingInLine` se inicia. Si supera `customerPatience` segundos:

1. Cambia al estado `Leaving`.
2. Se remueve de la cola.
3. Dispara `GameEvents.OnCustomerAbandoned`.
4. El `MetricTracker` incrementa el contador "clientes perdidos".

**Métrica derivada**:

```text
tasa_abandono = clientes_perdidos / clientes_llegados
```

Se reporta en el dashboard como porcentaje. Si supera 20%, el panel cambia de color (verde → amarillo → rojo).

---

## 6. Protocolo de validación Unity ↔ ProModel

El objetivo: demostrar que las dos simulaciones producen métricas equivalentes con los mismos parámetros.

### Procedimiento (responsabilidad conjunta de I1 e I4)

1. **Configurar Unity** con un escenario canónico:
   - `λ = 5 clientes/min`
   - `μ_caja = 8 clientes/min`
   - `μ_barista = 6 clientes/min`
   - `c_caja = 1`, `c_barista = 1`
   - `p_w = 0` (sin pedidos web, primer test)
   - `seed = 12345`
   - Duración: 60 min simulados.

2. **Exportar CSV desde Unity** a `StreamingAssets/run_12345_<timestamp>.csv`:

   ```csv
   evento,tiempo_simulado_s,cliente_id,estado_anterior,estado_nuevo
   arribo,2.34,1,,Entering
   en_cola,5.10,1,Entering,WaitingInLine
   atencion_caja,12.55,1,WaitingInLine,Ordering
   ...
   ```

3. **Configurar ProModel** con los mismos parámetros:
   - Locaciones: Entrada (cap 1), Cola_Caja (cap ∞), Caja (cap 1), Cola_Barista (cap ∞), Barista (cap 1), Mesa (cap 20), Salida.
   - Llegadas: `Cliente` con frecuencia `Exp(12)` segundos (= 5/min).
   - Procesos:
     - `Caja`: `Wait Exp(7.5)` segundos.
     - `Barista`: `Wait Exp(10)` segundos.
   - Tiempo de simulación: 60 min.

4. **Comparar métricas** en una tabla:

| Métrica | Unity | ProModel | % diferencia |
|---|---|---|---|
| W (tiempo en sistema) | 2.31 min | 2.35 min | 1.7% |
| Wq (tiempo en cola) | 1.84 min | 1.86 min | 1.1% |
| Lq (clientes en cola) | 9.2 | 9.3 | 1.1% |
| ρ (utilización barista) | 0.83 | 0.84 | 1.2% |

**Criterio de aceptación**: diferencia ≤ 5% en todas las métricas. Si excede, debugging conjunto.

### Repetir con escenarios:

- **Escenario A**: parámetros base (arriba).
- **Escenario B**: `p_w = 0.5` (alta proporción de pedidos web).
- **Escenario C**: `λ = 15`, `c_caja = 1` (saturación intencional).
- **Escenario D**: `λ = 2`, `c_caja = 3` (sub-utilización).

Cada escenario se reporta en `docs/anexos/validacion-promodel.md`.

---

## 7. Referencias

- Banks, Carson, Nelson, Nicol. *Discrete-Event System Simulation*, 5ta edición.
- Hillier & Lieberman. *Introduction to Operations Research*, capítulo de teoría de colas.
- Numerical Recipes in C, 2nd Edition — parámetros `a, c, m` del LCG.
- ProModel User Guide — modelado de locaciones y procesos.
- Documentación Unity: <https://docs.unity3d.com/Manual/index.html>
