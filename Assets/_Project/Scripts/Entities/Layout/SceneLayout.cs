using UnityEngine;

namespace CafeSim.Entities.Layout
{
    /// <summary>
    /// Configuración geométrica del layout 2D de la cafetería. Es un POCO
    /// serializable que el <c>SimulationBootstrap</c> usa para colocar
    /// programáticamente los placeholders cuando la escena está vacía.
    ///
    /// Toda la disposición es paramétrica (no hay Transforms hardcoded) para
    /// que el equipo pueda reorganizar el local cambiando un solo asset.
    /// </summary>
    [CreateAssetMenu(fileName = "SceneLayout", menuName = "CafeSim/Scene Layout", order = 1)]
    public sealed class SceneLayout : ScriptableObject
    {
        [Header("Entrada y salida")]
        [Tooltip("Posición donde aparecen los clientes nuevos.")]
        public Vector2 entryPoint = new Vector2(-9f, 0f);

        [Tooltip("Punto al que caminan al irse del local.")]
        public Vector2 exitPoint = new Vector2(11f, 0f);

        [Header("Cola del cajero")]
        [Tooltip("Cabeza de la cola (junto al cajero).")]
        public Vector2 cashierQueueHead = new Vector2(-2f, 0f);

        [Tooltip("Vector entre dos posiciones consecutivas de la cola.")]
        public Vector2 cashierQueueStride = new Vector2(-1f, 0f);

        [Header("Estación del cajero")]
        public Vector2 cashierStation = new Vector2(0f, 0f);

        [Tooltip("Offset entre cajeros si hay varios.")]
        public Vector2 cashierStride = new Vector2(0f, 1.5f);

        [Header("Cola del barista")]
        public Vector2 baristaQueueHead = new Vector2(2.5f, 0f);
        public Vector2 baristaQueueStride = new Vector2(0f, -0.8f);

        [Header("Estación del barista")]
        public Vector2 baristaStation = new Vector2(4.5f, 0f);
        public Vector2 baristaStride = new Vector2(0f, 1.5f);

        [Header("Mesas")]
        public Vector2 firstTablePosition = new Vector2(7f, 1.5f);

        [Tooltip("Separación horizontal entre mesas (las mesas se acomodan en filas).")]
        public float tableHorizontalSpacing = 1.8f;

        [Tooltip("Separación vertical entre filas de mesas.")]
        public float tableVerticalSpacing = -1.8f;

        [Tooltip("Cuántas mesas caben por fila antes de bajar a la siguiente.")]
        public int tablesPerRow = 3;

        [Header("Zona de consumo de pie")]
        [Tooltip("Punto donde se paran los clientes que no consiguieron mesa.")]
        public Vector2 standingArea = new Vector2(6.5f, -2.5f);

        // ─── Helpers de cálculo ──────────────────────────────────────────────

        public Vector2 GetCashierQueueSlot(int slotIndex)
            => cashierQueueHead + cashierQueueStride * slotIndex;

        public Vector2 GetBaristaQueueSlot(int slotIndex)
            => baristaQueueHead + baristaQueueStride * slotIndex;

        public Vector2 GetCashierStation(int cashierIndex)
            => cashierStation + cashierStride * cashierIndex;

        public Vector2 GetBaristaStation(int baristaIndex)
            => baristaStation + baristaStride * baristaIndex;

        public Vector2 GetTablePosition(int tableIndex)
        {
            int row = tablesPerRow > 0 ? tableIndex / tablesPerRow : 0;
            int col = tablesPerRow > 0 ? tableIndex % tablesPerRow : tableIndex;
            return firstTablePosition
                   + new Vector2(col * tableHorizontalSpacing, row * tableVerticalSpacing);
        }
    }
}
