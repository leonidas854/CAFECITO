using System;

namespace CafeSim.Core
{
    /// <summary>
    /// Generador pseudoaleatorio congruencial lineal (LCG) reproducible.
    ///
    /// Implementa <c>X_{n+1} = (a · X_n + c) mod m</c> con los parámetros
    /// de Numerical Recipes (a = 1664525, c = 1013904223, m = 2^32).
    /// Con la misma semilla produce exactamente la misma secuencia, lo que
    /// permite reproducir corridas y comparar contra ProModel.
    /// </summary>
    public sealed class LCGRandomGenerator
    {
        private const long A = 1664525L;
        private const long C = 1013904223L;
        private const long M = 4294967296L; // 2^32

        // Cota inferior para evitar que NextFloat() devuelva exactamente 0.
        // La transformada inversa de la exponencial usa -ln(U)/rate: si U = 0
        // el resultado es +∞ y la simulación se congela.
        private const float Epsilon = 1e-7f;

        private readonly long _seed;
        private long _x;

        /// <summary>Semilla original con la que se inicializó el generador.</summary>
        public long Seed => _seed;

        public LCGRandomGenerator(long seed)
        {
            if (seed <= 0)
                throw new ArgumentOutOfRangeException(nameof(seed), "La semilla debe ser entero positivo.");
            _seed = seed;
            _x = seed;
        }

        /// <summary>
        /// Devuelve el siguiente valor uniforme en el intervalo (0, 1].
        /// Nunca devuelve 0 exacto (sustituido por <c>Epsilon = 1e-7</c>) para
        /// proteger cálculos logarítmicos como <c>-ln(U)/rate</c>.
        /// </summary>
        public float NextFloat()
        {
            _x = (A * _x + C) % M;
            float u = (float)_x / M;
            return u < Epsilon ? Epsilon : u;
        }

        /// <summary>
        /// Devuelve un entero pseudoaleatorio en el rango [0, exclusiveMax).
        /// </summary>
        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 0)
                throw new ArgumentOutOfRangeException(nameof(exclusiveMax), "Debe ser estrictamente positivo.");
            return (int)(NextFloat() * exclusiveMax) % exclusiveMax;
        }

        /// <summary>
        /// Devuelve true con probabilidad <paramref name="probability"/>.
        /// </summary>
        public bool NextBool(float probability)
        {
            if (probability < 0f || probability > 1f)
                throw new ArgumentOutOfRangeException(nameof(probability), "Debe estar en [0, 1].");
            return NextFloat() < probability;
        }

        /// <summary>
        /// Reinicia el generador a su semilla original (la secuencia volverá a ser idéntica).
        /// </summary>
        public void Reset() => _x = _seed;
    }
}
