using System;
using NUnit.Framework;
using CafeSim.Core;

namespace CafeSim.Tests.Core
{
    /// <summary>
    /// Tests de la distribución exponencial. Verifica la propiedad esencial:
    /// la media de N muestras converge a 1/rate.
    /// </summary>
    [TestFixture]
    public class ExponentialDistributionTests
    {
        [Test]
        public void SampleWithRate_AlwaysProducesPositiveValues()
        {
            var rng = new LCGRandomGenerator(12345L);
            for (int i = 0; i < 10_000; i++)
            {
                float t = ExponentialDistribution.SampleWithRate(rng, 1f);
                Assert.Greater(t, 0f, $"Muestra no positiva en iter {i}: {t}");
            }
        }

        [Test]
        public void SampleWithRate_MeanConvergesToTheoretical()
        {
            // Con rate = 1, la media teórica es 1.
            // Con n = 50000, el error estándar es ~1/sqrt(n) ≈ 0.0045.
            // Permitimos hasta 5% de desviación (margen amplio).
            var rng = new LCGRandomGenerator(98765L);
            const int n = 50_000;
            const float rate = 1f;
            double sum = 0d;
            for (int i = 0; i < n; i++)
                sum += ExponentialDistribution.SampleWithRate(rng, rate);

            double mean = sum / n;
            Assert.That(mean, Is.EqualTo(1.0 / rate).Within(0.05),
                $"Media empírica = {mean}, esperada ≈ {1.0 / rate}");
        }

        [Test]
        public void SampleWithMean_EquivalentToSampleWithRate()
        {
            // SampleWithMean(rng, μ) y SampleWithRate(rng, 1/μ) deben coincidir
            // numéricamente porque consumen un valor U cada uno.
            var rngA = new LCGRandomGenerator(11111L);
            var rngB = new LCGRandomGenerator(11111L);
            const float mean = 30f;
            const float rate = 1f / 30f;

            for (int i = 0; i < 100; i++)
            {
                float a = ExponentialDistribution.SampleWithMean(rngA, mean);
                float b = ExponentialDistribution.SampleWithRate(rngB, rate);
                Assert.AreEqual(a, b, 1e-4f, $"Discrepancia en iter {i}: {a} vs {b}");
            }
        }

        [Test]
        public void SampleWithRate_ThrowsOnInvalidRate()
        {
            var rng = new LCGRandomGenerator(1L);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ExponentialDistribution.SampleWithRate(rng, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ExponentialDistribution.SampleWithRate(rng, -1f));
            Assert.Throws<ArgumentNullException>(
                () => ExponentialDistribution.SampleWithRate(null, 1f));
        }
    }
}
