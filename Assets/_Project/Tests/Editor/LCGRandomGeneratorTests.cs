using System;
using NUnit.Framework;
using CafeSim.Core;

namespace CafeSim.Tests.Core
{
    /// <summary>
    /// Tests del generador pseudoaleatorio LCG. Estos tests aseguran las dos
    /// propiedades críticas para validar contra ProModel:
    /// <list type="bullet">
    ///   <item><b>Reproducibilidad</b>: misma semilla ⇒ misma secuencia.</item>
    ///   <item><b>No-cero</b>: NextFloat() nunca devuelve exactamente 0 (rompería log).</item>
    /// </list>
    /// </summary>
    [TestFixture]
    public class LCGRandomGeneratorTests
    {
        private const long Seed = 12345L;

        [Test]
        public void SameSeed_ProducesIdenticalSequence()
        {
            var rngA = new LCGRandomGenerator(Seed);
            var rngB = new LCGRandomGenerator(Seed);

            for (int i = 0; i < 1000; i++)
            {
                Assert.AreEqual(rngA.NextFloat(), rngB.NextFloat(),
                    $"Las secuencias divergen en el iterador {i}.");
            }
        }

        [Test]
        public void NextFloat_NeverReturnsZero()
        {
            var rng = new LCGRandomGenerator(1L); // semilla mínima
            for (int i = 0; i < 10_000; i++)
            {
                float u = rng.NextFloat();
                Assert.Greater(u, 0f, $"NextFloat devolvió 0 en iter {i} (rompería log).");
            }
        }

        [Test]
        public void NextFloat_StaysInUnitInterval()
        {
            var rng = new LCGRandomGenerator(Seed);
            for (int i = 0; i < 10_000; i++)
            {
                float u = rng.NextFloat();
                Assert.That(u, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f),
                    $"Valor fuera de (0, 1] en iter {i}: {u}");
            }
        }

        [Test]
        public void Reset_RestartsTheSequence()
        {
            var rng = new LCGRandomGenerator(Seed);
            float first = rng.NextFloat();
            for (int i = 0; i < 50; i++) rng.NextFloat(); // avanza el estado

            rng.Reset();
            Assert.AreEqual(first, rng.NextFloat(),
                "Reset() debería devolver el generador a su estado inicial.");
        }

        [Test]
        public void Constructor_ThrowsOnNonPositiveSeed()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LCGRandomGenerator(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LCGRandomGenerator(-1));
        }

        [Test]
        public void NextBool_RespectsProbabilityBounds()
        {
            var rng = new LCGRandomGenerator(Seed);

            // Probabilidad 0 nunca debería dar true.
            for (int i = 0; i < 1000; i++)
                Assert.IsFalse(rng.NextBool(0f), "NextBool(0) devolvió true.");

            // Probabilidad 1 siempre debería dar true.
            for (int i = 0; i < 1000; i++)
                Assert.IsTrue(rng.NextBool(1f), "NextBool(1) devolvió false.");
        }

        [Test]
        public void NextInt_StaysWithinBounds()
        {
            var rng = new LCGRandomGenerator(Seed);
            for (int i = 0; i < 10_000; i++)
            {
                int v = rng.NextInt(10);
                Assert.That(v, Is.InRange(0, 9), $"NextInt(10) devolvió {v}.");
            }
        }
    }
}
