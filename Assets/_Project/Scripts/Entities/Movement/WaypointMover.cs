using UnityEngine;

namespace CafeSim.Entities.Movement
{
    /// <summary>
    /// Mueve suavemente un Transform hacia un waypoint usando interpolación
    /// proporcional al tiempo. Sin DOTween (todavía no está en el proyecto):
    /// usa <c>Vector3.MoveTowards</c> respetando <c>Time.deltaTime</c> para que
    /// la velocidad sea consistente bajo cualquier <c>Time.timeScale</c>.
    ///
    /// Cuando se entrega DOTween en el Sprint 2, solo hace falta reemplazar
    /// el contenido de <see cref="Update"/> sin cambiar la interfaz pública.
    /// </summary>
    public sealed class WaypointMover : MonoBehaviour
    {
        [Tooltip("Velocidad en unidades de Unity por segundo (en tiempo simulado).")]
        [SerializeField] private float speedUnitsPerSecond = 3f;

        [Tooltip("Distancia bajo la cual se considera que llegó al destino.")]
        [SerializeField] private float arrivalThreshold = 0.05f;

        private Vector3 _target;
        private bool _hasTarget;

        /// <summary>True cuando hay un destino activo y aún no se ha alcanzado.</summary>
        public bool IsMoving => _hasTarget;

        /// <summary>Posición destino actual (válida solo si <see cref="IsMoving"/>).</summary>
        public Vector3 Target => _target;

        /// <summary>
        /// Ajusta la velocidad. La UI puede llamar esto si tiene un slider de
        /// velocidad de caminata.
        /// </summary>
        public void SetSpeed(float unitsPerSecond)
        {
            if (unitsPerSecond > 0f) speedUnitsPerSecond = unitsPerSecond;
        }

        /// <summary>
        /// Programa un nuevo destino. La altura Z del target se descarta
        /// (el proyecto es 2D), preservando la Z actual del transform.
        /// </summary>
        public void MoveTo(Vector3 worldPosition)
        {
            _target = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
            _hasTarget = true;
        }

        /// <summary>Cancela el movimiento sin alterar la posición actual.</summary>
        public void Stop() => _hasTarget = false;

        /// <summary>Teletransporta a la posición sin animación.</summary>
        public void TeleportTo(Vector3 worldPosition)
        {
            transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
            _hasTarget = false;
        }

        private void Update()
        {
            if (!_hasTarget) return;
            Vector3 current = transform.position;
            float step = speedUnitsPerSecond * Time.deltaTime;
            transform.position = Vector3.MoveTowards(current, _target, step);

            if (Vector3.SqrMagnitude(transform.position - _target) <= arrivalThreshold * arrivalThreshold)
            {
                transform.position = _target;
                _hasTarget = false;
            }
        }
    }
}
