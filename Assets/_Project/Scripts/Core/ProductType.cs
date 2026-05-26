namespace CafeSim.Core
{
    /// <summary>
    /// Tipos de bebida que el barista puede preparar. Cada tipo tiene un tiempo
    /// promedio de preparación distinto; el tiempo real sigue una distribución
    /// exponencial alrededor de esa media.
    /// </summary>
    public enum ProductType
    {
        /// <summary>Vaso de agua — el más rápido (5 s).</summary>
        Water = 0,

        /// <summary>Café americano simple (15 s).</summary>
        Coffee = 1,

        /// <summary>Té (20 s).</summary>
        Tea = 2,

        /// <summary>Capuchino con espuma de leche (30 s).</summary>
        Cappuccino = 3,

        /// <summary>Frappé licuado, el más lento (45 s).</summary>
        Frappe = 4
    }
}
