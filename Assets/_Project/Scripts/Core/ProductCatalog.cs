using System;

namespace CafeSim.Core
{
    /// <summary>
    /// Catálogo de productos: expone el tiempo medio de preparación de cada
    /// <see cref="ProductType"/> y permite sortear un producto aleatorio
    /// usando el <see cref="LCGRandomGenerator"/>.
    ///
    /// Los pesos por defecto representan una mezcla realista de pedidos
    /// (más café y capuchinos que frappés). Pueden sobrescribirse pasando
    /// un arreglo de pesos al método <see cref="SampleProduct"/>.
    /// </summary>
    public static class ProductCatalog
    {
        // Tiempo medio de preparación en segundos por producto.
        // Estos valores reflejan la realidad operativa de una cafetería pequeña.
        private const float WaterMeanSeconds = 5f;
        private const float CoffeeMeanSeconds = 15f;
        private const float TeaMeanSeconds = 20f;
        private const float CappuccinoMeanSeconds = 30f;
        private const float FrappeMeanSeconds = 45f;

        // Pesos relativos por defecto (no necesitan sumar 1; se normalizan).
        private static readonly float[] DefaultWeights =
        {
            0.10f, // Water
            0.35f, // Coffee
            0.20f, // Tea
            0.25f, // Cappuccino
            0.10f  // Frappe
        };

        /// <summary>
        /// Devuelve el tiempo medio de preparación (segundos) del producto.
        /// </summary>
        public static float GetMeanServiceTimeSeconds(ProductType product)
        {
            switch (product)
            {
                case ProductType.Water:      return WaterMeanSeconds;
                case ProductType.Coffee:     return CoffeeMeanSeconds;
                case ProductType.Tea:        return TeaMeanSeconds;
                case ProductType.Cappuccino: return CappuccinoMeanSeconds;
                case ProductType.Frappe:     return FrappeMeanSeconds;
                default:
                    throw new ArgumentOutOfRangeException(nameof(product), product, "Producto desconocido.");
            }
        }

        /// <summary>
        /// Sortea un producto aleatorio según pesos. Si <paramref name="weights"/>
        /// es null, usa la mezcla por defecto del catálogo.
        /// </summary>
        public static ProductType SampleProduct(LCGRandomGenerator rng, float[] weights = null)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            var w = weights ?? DefaultWeights;
            if (w.Length != 5)
                throw new ArgumentException("Se requieren exactamente 5 pesos (uno por ProductType).", nameof(weights));

            float total = 0f;
            for (int i = 0; i < w.Length; i++)
            {
                if (w[i] < 0f) throw new ArgumentException("Los pesos no pueden ser negativos.", nameof(weights));
                total += w[i];
            }
            if (total <= 0f) throw new ArgumentException("Al menos un peso debe ser positivo.", nameof(weights));

            float u = rng.NextFloat() * total;
            float acc = 0f;
            for (int i = 0; i < w.Length; i++)
            {
                acc += w[i];
                if (u <= acc) return (ProductType)i;
            }
            return ProductType.Coffee; // fallback ante errores de redondeo
        }
    }
}
