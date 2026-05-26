using UnityEngine;

namespace CafeSim.Entities.Placeholders
{
    /// <summary>
    /// Factoría estática de sprites placeholder. Mientras no haya pixel-art real,
    /// todos los GameObjects (cliente, cajero, barista, mesa, silla) usan
    /// un sprite blanco 1×1 que se tinta vía <c>SpriteRenderer.color</c>.
    ///
    /// Reemplazar luego es trivial: solo cambiar la asignación del sprite real
    /// en los Prefabs sin tocar la lógica de los entities.
    /// </summary>
    public static class PlaceholderShapes
    {
        private static Sprite _cachedSquare;

        /// <summary>
        /// Devuelve un sprite cuadrado blanco 1×1. El mismo sprite se reusa en
        /// toda la escena para que no haya overhead de memoria.
        /// </summary>
        public static Sprite Square
        {
            get
            {
                if (_cachedSquare == null) _cachedSquare = CreateSquare();
                return _cachedSquare;
            }
        }

        /// <summary>
        /// Crea un GameObject 2D con un <see cref="SpriteRenderer"/>,
        /// tinte de color y escala dada. Útil para construir vistas placeholder
        /// sin pasar por prefabs.
        /// </summary>
        public static GameObject CreateColoredSquare(
            string objectName,
            Color color,
            Vector2 size,
            Transform parent = null,
            int sortingOrder = 0)
        {
            var go = new GameObject(objectName);
            if (parent != null) go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Square;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return go;
        }

        private static Sprite CreateSquare()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false)
            {
                name = "PlaceholderWhite_1x1",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();

            var rect = new Rect(0f, 0f, 1f, 1f);
            var pivot = new Vector2(0.5f, 0.5f);
            var sprite = Sprite.Create(tex, rect, pivot, pixelsPerUnit: 1f);
            sprite.name = "PlaceholderSquare";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
