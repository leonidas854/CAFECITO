using UnityEngine;

namespace CafeSim.Entities.Placeholders
{
    /// <summary>
    /// Catálogo de formas placeholder generadas en runtime (cuadrado, círculo,
    /// rectángulo redondeado). Mientras no haya pixel-art real, los Entities
    /// usan estos sprites y los tintan vía <c>SpriteRenderer.color</c>.
    ///
    /// Sustituirlos por sprites reales es trivial: solo cambiar el asset del
    /// <see cref="SpriteRenderer"/> en el Prefab correspondiente sin tocar la
    /// lógica del Entity.
    /// </summary>
    public static class PlaceholderShapes
    {
        // Resolución de las texturas procedurales. 64×64 es suficiente para que
        // el círculo y el rectángulo redondeado se vean lisos a 1 unidad.
        private const int TextureSize = 64;

        private static Sprite _cachedSquare;
        private static Sprite _cachedCircle;
        private static Sprite _cachedRoundedRect;

        /// <summary>Sprite blanco 1×1, ideal para mesas, paredes y marcadores.</summary>
        public static Sprite Square
        {
            get
            {
                if (_cachedSquare == null) _cachedSquare = CreateSquare();
                return _cachedSquare;
            }
        }

        /// <summary>Sprite circular blanco. Usado para clientes (vista cenital de una cabeza) y sillas.</summary>
        public static Sprite Circle
        {
            get
            {
                if (_cachedCircle == null) _cachedCircle = CreateCircle();
                return _cachedCircle;
            }
        }

        /// <summary>Sprite rectangular con esquinas redondeadas. Usado para cajeros, baristas y mostradores.</summary>
        public static Sprite RoundedRect
        {
            get
            {
                if (_cachedRoundedRect == null) _cachedRoundedRect = CreateRoundedRect();
                return _cachedRoundedRect;
            }
        }

        /// <summary>
        /// Crea un GameObject 2D con un <see cref="SpriteRenderer"/>, sprite
        /// dado, tinte de color y escala dada. Útil para construir vistas
        /// placeholder sin pasar por prefabs.
        /// </summary>
        public static GameObject CreateColoredShape(
            string objectName,
            Sprite sprite,
            Color color,
            Vector2 size,
            Transform parent = null,
            int sortingOrder = 0)
        {
            var go = new GameObject(objectName);
            if (parent != null) go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return go;
        }

        /// <summary>
        /// Atajo histórico: equivale a <see cref="CreateColoredShape"/> con el
        /// sprite <see cref="Square"/>. Se mantiene para que el resto del código
        /// no tenga que cambiar todas sus llamadas en una sola pasada.
        /// </summary>
        public static GameObject CreateColoredSquare(
            string objectName,
            Color color,
            Vector2 size,
            Transform parent = null,
            int sortingOrder = 0)
        {
            return CreateColoredShape(objectName, Square, color, size, parent, sortingOrder);
        }

        // ─── Generación procedural de texturas ────────────────────────────────

        private static Sprite CreateSquare()
        {
            var tex = NewTexture("PlaceholderWhite_1x1", 1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return BuildSprite(tex, "PlaceholderSquare", pixelsPerUnit: 1f);
        }

        private static Sprite CreateCircle()
        {
            int size = TextureSize;
            var tex = NewTexture("PlaceholderCircle", size, size);
            float r = (size - 1) * 0.5f;
            float r2 = r * r;
            var transparent = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r;
                    float dy = y - r;
                    tex.SetPixel(x, y, (dx * dx + dy * dy) <= r2 ? Color.white : transparent);
                }
            }
            tex.Apply();
            return BuildSprite(tex, "PlaceholderCircle", pixelsPerUnit: size);
        }

        private static Sprite CreateRoundedRect()
        {
            int size = TextureSize;
            int radius = size / 6; // esquinas modestamente redondeadas
            var tex = NewTexture("PlaceholderRoundedRect", size, size);
            var transparent = new Color(1f, 1f, 1f, 0f);
            int r2 = radius * radius;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = true;
                    // Calcular esquina más cercana (si está dentro del cuadrante de la esquina).
                    int cx = x < radius ? radius : (x > size - 1 - radius ? size - 1 - radius : x);
                    int cy = y < radius ? radius : (y > size - 1 - radius ? size - 1 - radius : y);
                    int dx = x - cx;
                    int dy = y - cy;
                    if (dx * dx + dy * dy > r2) inside = false;
                    tex.SetPixel(x, y, inside ? Color.white : transparent);
                }
            }
            tex.Apply();
            return BuildSprite(tex, "PlaceholderRoundedRect", pixelsPerUnit: size);
        }

        private static Texture2D NewTexture(string textureName, int width, int height)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false)
            {
                name = textureName,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private static Sprite BuildSprite(Texture2D tex, string spriteName, float pixelsPerUnit)
        {
            var rect = new Rect(0f, 0f, tex.width, tex.height);
            var pivot = new Vector2(0.5f, 0.5f);
            var sprite = Sprite.Create(tex, rect, pivot, pixelsPerUnit);
            sprite.name = spriteName;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
