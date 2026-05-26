using UnityEngine;
using CafeSim.Core;

namespace CafeSim.Entities
{
    /// <summary>
    /// Helper estático que mapea un <see cref="CustomerState"/> al color que
    /// debe pintar el placeholder del cliente. Centralizar la paleta evita
    /// que cada Entity tenga su propio switch repetido.
    /// </summary>
    public static class CustomerVisualController
    {
        // Paleta deliberadamente alta de contraste para distinguir estados
        // de un vistazo durante presentaciones. Cambiar aquí afecta a todos
        // los clientes en pantalla.
        public static readonly Color Entering         = new Color(0.65f, 0.85f, 1f);   // azul claro
        public static readonly Color WaitingInLine    = new Color(0.95f, 0.80f, 0.20f); // amarillo
        public static readonly Color Ordering         = new Color(1f,    0.55f, 0.10f); // naranja
        public static readonly Color WaitingDrink     = new Color(0.55f, 0.45f, 0.95f); // morado claro
        public static readonly Color BeingServed      = new Color(0.40f, 0.30f, 0.90f); // morado fuerte
        public static readonly Color Consuming        = new Color(0.30f, 0.80f, 0.45f); // verde
        public static readonly Color ConsumingStanding= new Color(0.20f, 0.60f, 0.35f); // verde oscuro
        public static readonly Color Leaving          = new Color(0.55f, 0.55f, 0.55f); // gris
        public static readonly Color Abandoned        = new Color(0.85f, 0.20f, 0.20f); // rojo
        public static readonly Color Rejected         = new Color(0.40f, 0f,    0f);    // rojo oscuro

        public static Color ColorFor(CustomerState state)
        {
            switch (state)
            {
                case CustomerState.Entering:          return Entering;
                case CustomerState.WaitingInLine:     return WaitingInLine;
                case CustomerState.Ordering:          return Ordering;
                case CustomerState.WaitingDrink:      return WaitingDrink;
                case CustomerState.BeingServed:       return BeingServed;
                case CustomerState.Consuming:         return Consuming;
                case CustomerState.ConsumingStanding: return ConsumingStanding;
                case CustomerState.Leaving:           return Leaving;
                case CustomerState.Abandoned:         return Abandoned;
                case CustomerState.Rejected:          return Rejected;
                default:                              return Color.white;
            }
        }
    }
}
