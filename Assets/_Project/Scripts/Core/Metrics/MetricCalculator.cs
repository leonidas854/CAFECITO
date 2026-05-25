using System;
using System.Collections.Generic;

namespace CafeSim.Core.Metrics
{
    /// <summary>
    /// Funciones puras de teoría de colas. Calcula tanto métricas teóricas
    /// (M/M/1 y M/M/c por fórmula de Erlang-C) como métricas empíricas
    /// (a partir de la lista de clientes que ya completaron el flujo).
    ///
    /// Todos los métodos son determinísticos: misma entrada ⇒ misma salida.
    /// </summary>
    public static class MetricCalculator
    {
        // ─── Métricas teóricas (analíticas) ────────────────────────────────────

        /// <summary>
        /// Utilización ρ = λ / (c · μ). Para que el sistema sea estable debe
        /// ser estrictamente menor a 1; si no, la cola crece sin cota.
        /// </summary>
        public static float Utilization(float arrivalRate, float serviceRate, int servers)
        {
            if (arrivalRate < 0f) throw new ArgumentOutOfRangeException(nameof(arrivalRate));
            if (serviceRate <= 0f) throw new ArgumentOutOfRangeException(nameof(serviceRate));
            if (servers < 1) throw new ArgumentOutOfRangeException(nameof(servers));
            return arrivalRate / (servers * serviceRate);
        }

        /// <summary>
        /// Longitud promedio en cola Lq para un sistema M/M/1.
        /// Fórmula: ρ² / (1 - ρ). Requiere ρ &lt; 1.
        /// </summary>
        public static float LqForMm1(float arrivalRate, float serviceRate)
        {
            float rho = Utilization(arrivalRate, serviceRate, 1);
            if (rho >= 1f)
                throw new InvalidOperationException("Sistema inestable: ρ ≥ 1. No existe Lq estacionario.");
            return rho * rho / (1f - rho);
        }

        /// <summary>
        /// Tiempo promedio en cola Wq por la Ley de Little: Wq = Lq / λ.
        /// </summary>
        public static float WqFromLq(float lq, float arrivalRate)
        {
            if (arrivalRate <= 0f)
                throw new ArgumentOutOfRangeException(nameof(arrivalRate), "Debe ser positiva.");
            return lq / arrivalRate;
        }

        /// <summary>
        /// Tiempo promedio en el sistema W = Wq + 1/μ.
        /// </summary>
        public static float WFromWq(float wq, float serviceRate)
        {
            if (serviceRate <= 0f)
                throw new ArgumentOutOfRangeException(nameof(serviceRate), "Debe ser positiva.");
            return wq + 1f / serviceRate;
        }

        /// <summary>
        /// Longitud promedio en cola Lq para un sistema M/M/c usando Erlang-C.
        /// Requiere ρ = λ/(cμ) &lt; 1.
        /// </summary>
        public static float LqForMmc(float arrivalRate, float serviceRate, int servers)
        {
            float rho = Utilization(arrivalRate, serviceRate, servers);
            if (rho >= 1f)
                throw new InvalidOperationException("Sistema inestable: ρ ≥ 1.");

            float cRho = servers * rho;
            double p0 = ProbabilityOfEmpty(servers, cRho);
            double numerator = Math.Pow(cRho, servers) * rho;
            double denominator = Factorial(servers) * Math.Pow(1.0 - rho, 2.0);
            return (float)(numerator / denominator * p0);
        }

        /// <summary>
        /// Probabilidad de sistema vacío P₀ para M/M/c. Componente intermedia
        /// de la fórmula de Erlang-C; expuesta por si se quiere graficar.
        /// </summary>
        public static double ProbabilityOfEmpty(int servers, float cRho)
        {
            double sum = 0.0;
            for (int n = 0; n < servers; n++)
                sum += Math.Pow(cRho, n) / Factorial(n);

            double tail = Math.Pow(cRho, servers)
                          / (Factorial(servers) * (1.0 - cRho / servers));
            return 1.0 / (sum + tail);
        }

        // ─── Métricas empíricas (medidas sobre la simulación corrida) ──────────

        /// <summary>
        /// Promedio del tiempo total en el sistema (W) sobre los clientes ya
        /// finalizados. Solo considera clientes con DepartureTime (atendidos).
        /// </summary>
        public static float AverageTimeInSystem(IEnumerable<CustomerData> finishedCustomers)
        {
            if (finishedCustomers == null) return 0f;
            float sum = 0f;
            int n = 0;
            foreach (var c in finishedCustomers)
            {
                if (!c.DepartureTime.HasValue) continue;
                sum += c.DepartureTime.Value - c.ArrivalTime;
                n++;
            }
            return n == 0 ? 0f : sum / n;
        }

        /// <summary>
        /// Promedio del tiempo total en colas (Wq) sobre los clientes ya
        /// finalizados (atendidos o abandonados).
        /// </summary>
        public static float AverageTimeInQueues(IEnumerable<CustomerData> finishedCustomers)
        {
            if (finishedCustomers == null) return 0f;
            float sum = 0f;
            int n = 0;
            foreach (var c in finishedCustomers)
            {
                sum += c.TimeInQueues;
                n++;
            }
            return n == 0 ? 0f : sum / n;
        }

        // ─── Utilitarios ───────────────────────────────────────────────────────

        private static double Factorial(int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            double result = 1.0;
            for (int i = 2; i <= n; i++) result *= i;
            return result;
        }
    }
}
