using System;
using NUnit.Framework;
using CafeSim.Core;

namespace CafeSim.Tests.Core
{
    /// <summary>
    /// Tests del catálogo de productos: tiempos correctos, muestreo válido,
    /// y validación de pesos.
    /// </summary>
    [TestFixture]
    public class ProductCatalogTests
    {
        [Test]
        public void GetMeanServiceTime_HasExpectedValues()
        {
            Assert.AreEqual(5f,  ProductCatalog.GetMeanServiceTimeSeconds(ProductType.Water));
            Assert.AreEqual(15f, ProductCatalog.GetMeanServiceTimeSeconds(ProductType.Coffee));
            Assert.AreEqual(20f, ProductCatalog.GetMeanServiceTimeSeconds(ProductType.Tea));
            Assert.AreEqual(30f, ProductCatalog.GetMeanServiceTimeSeconds(ProductType.Cappuccino));
            Assert.AreEqual(45f, ProductCatalog.GetMeanServiceTimeSeconds(ProductType.Frappe));
        }

        [Test]
        public void SampleProduct_ReturnsAllProductTypes_OverManyDraws()
        {
            // Con n=5000 muestras y pesos default >0 para los 5, esperamos ver cada uno al menos una vez.
            var rng = new LCGRandomGenerator(42L);
            var seen = new bool[5];

            for (int i = 0; i < 5000; i++)
            {
                var p = ProductCatalog.SampleProduct(rng);
                seen[(int)p] = true;
            }

            for (int i = 0; i < seen.Length; i++)
                Assert.IsTrue(seen[i], $"Producto {(ProductType)i} nunca apareció en 5000 muestras.");
        }

        [Test]
        public void SampleProduct_RespectsCustomWeights()
        {
            // Forzamos que solo el café (índice 1) tenga peso.
            var weights = new float[] { 0f, 1f, 0f, 0f, 0f };
            var rng = new LCGRandomGenerator(42L);

            for (int i = 0; i < 100; i++)
                Assert.AreEqual(ProductType.Coffee, ProductCatalog.SampleProduct(rng, weights));
        }

        [Test]
        public void SampleProduct_ThrowsOnInvalidWeights()
        {
            var rng = new LCGRandomGenerator(1L);
            Assert.Throws<ArgumentException>(
                () => ProductCatalog.SampleProduct(rng, new float[] { 1f, 2f }));       // longitud incorrecta
            Assert.Throws<ArgumentException>(
                () => ProductCatalog.SampleProduct(rng, new float[] { 0f, 0f, 0f, 0f, 0f })); // todos en cero
            Assert.Throws<ArgumentException>(
                () => ProductCatalog.SampleProduct(rng, new float[] { -1f, 1f, 1f, 1f, 1f })); // peso negativo
        }
    }
}
