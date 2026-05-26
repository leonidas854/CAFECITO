using UnityEngine;
using CafeSim.Entities.Placeholders;

namespace CafeSim.Entities
{
    /// <summary>
    /// Vista placeholder de una mesa con sus N sillas. La mesa se renderiza
    /// como un rectángulo grande y cada silla como un cuadrado pequeño a su
    /// alrededor. No mantiene estado lógico (eso vive en <c>Core.Tables.Table</c>);
    /// solo provee la geometría visual y los puntos a los que un cliente
    /// puede caminar para sentarse.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TableEntity : MonoBehaviour
    {
        [SerializeField] private int seatCount = 4;
        [SerializeField] private float seatRadius = 0.55f;

        private static readonly Color TableColor = new Color(0.45f, 0.30f, 0.20f); // madera
        private static readonly Color FreeSeatColor = new Color(0.85f, 0.85f, 0.85f);

        private Transform[] _seatAnchors;
        private SpriteRenderer[] _seatRenderers;

        /// <summary>Id lógico de la mesa (debe coincidir con <c>Core.Tables.Table.Id</c>).</summary>
        public int TableId { get; private set; }

        public int SeatCount => seatCount;

        /// <summary>
        /// Construye la mesa y sus sillas. Llamar inmediatamente después de
        /// instanciar el GameObject (lo hace <c>SimulationBootstrap</c>).
        /// </summary>
        public void Build(int tableId, int seats, Vector2 size)
        {
            TableId = tableId;
            seatCount = seats;
            transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = PlaceholderShapes.Square;
            renderer.color = TableColor;
            renderer.sortingOrder = 0;
            CreateSeats();
        }

        /// <summary>
        /// Devuelve la posición mundial de la silla indicada (0..seatCount-1).
        /// </summary>
        public Vector3 GetSeatWorldPosition(int seatIndex)
        {
            if (_seatAnchors == null || seatIndex < 0 || seatIndex >= _seatAnchors.Length)
                return transform.position;
            return _seatAnchors[seatIndex].position;
        }

        /// <summary>
        /// Tinta la silla indicada para mostrar visualmente si está ocupada.
        /// </summary>
        public void SetSeatOccupied(int seatIndex, bool occupied, Color customerColor)
        {
            if (_seatRenderers == null || seatIndex < 0 || seatIndex >= _seatRenderers.Length) return;
            _seatRenderers[seatIndex].color = occupied ? customerColor : FreeSeatColor;
        }

        private void CreateSeats()
        {
            DestroyExistingSeats();
            _seatAnchors = new Transform[seatCount];
            _seatRenderers = new SpriteRenderer[seatCount];

            for (int i = 0; i < seatCount; i++)
            {
                Vector2 offset = ComputeSeatOffset(i, seatCount);
                var seat = PlaceholderShapes.CreateColoredSquare(
                    objectName: $"Seat_{i + 1}",
                    color: FreeSeatColor,
                    size: new Vector2(0.4f, 0.4f),
                    parent: transform,
                    sortingOrder: 1);
                seat.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
                _seatAnchors[i] = seat.transform;
                _seatRenderers[i] = seat.GetComponent<SpriteRenderer>();
            }
        }

        private Vector2 ComputeSeatOffset(int index, int total)
        {
            // Sillas distribuidas en círculo alrededor de la mesa.
            float angle = (Mathf.PI * 2f * index) / total;
            float scaleX = transform.localScale.x;
            float scaleY = transform.localScale.y;
            // Localización en local-space: dividir por scale para compensar.
            float x = (Mathf.Cos(angle) * seatRadius) / Mathf.Max(0.01f, scaleX);
            float y = (Mathf.Sin(angle) * seatRadius) / Mathf.Max(0.01f, scaleY);
            return new Vector2(x, y);
        }

        private void DestroyExistingSeats()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }
    }
}
