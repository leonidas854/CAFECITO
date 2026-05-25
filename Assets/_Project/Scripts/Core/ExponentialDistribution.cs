using System;

namespace CafeSim.Core
{
    /// <summary>
    /// Generación de tiempos aleatorios con distribución exponencial mediante
    /// el método de la transformada inversa.
    ///
    /// Para U ~ U(0, 1] y tasa λ &gt; 0:  T = -ln(U) / λ.
    /// Equivalentemente, dado el promedio μ = 1/λ:  T = -μ · ln(U).
    /// </summary>
    public static class ExponentialDistribution
    {
        /// <summary>
        /// Devuelve un tiempo según Exp(rate), donde <paramref name="rate"/>
        /// está expresado en eventos por unidad de tiempo.
        /// </summary>
        /// <param name="rng">Generador uniforme reproducible.</param>
        /// <param name="rate">Tasa λ; debe ser estrictamente positiva.</param>
        public static float SampleWithRate(LCGRandomGenerator rng, float rate)
        {
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));
            if (rate <= 0f)
                throw new ArgumentOutOfRangeException(nameof(rate), "La tasa debe ser estrictamente positiva.");

            float u = rng.NextFloat();
            return -(float)Math.Log(u) / rate;
        }

        /// <summary>
        /// Devuelve un tiempo según Exp con media <paramref name="meanTime"/>.
        /// Equivalente a <see cref="SampleWithRate"/> con rate = 1/meanTime.
        /// </summary>
        /// <param name="rng">Generador uniforme reproducible.</param>
        /// <param name="meanTime">Tiempo promedio entre eventos; debe ser positivo.</param>
        public static float SampleWithMean(LCGRandomGenerator rng, float meanTime)
        {
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));
            if (meanTime <= 0f)
                throw new ArgumentOutOfRangeException(nameof(meanTime), "El tiempo medio debe ser positivo.");

            float u = rng.NextFloat();
            return -(float)Math.Log(u) * meanTime;
        }
    }
}
