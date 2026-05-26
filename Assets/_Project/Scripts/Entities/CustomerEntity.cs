using UnityEngine;
using CafeSim.Core;
using CafeSim.Entities.Movement;

namespace CafeSim.Entities
{
    /// <summary>
    /// MonoBehaviour que representa visualmente un cliente. No tiene lógica
    /// de simulación: solo recibe el <see cref="CustomerData"/> del Core y
    /// ajusta su color y posición de destino según cambios de estado.
    ///
    /// El <c>CustomerSpawner</c> es el que crea instancias de este componente
    /// (vía <c>GameObject</c> generado en runtime con placeholders) y le
    /// asigna su <see cref="CustomerData"/>.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(WaypointMover))]
    public sealed class CustomerEntity : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private WaypointMover _mover;

        /// <summary>Datos del cliente en el dominio. Solo lectura para el resto del sistema.</summary>
        public CustomerData Data { get; private set; }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _mover = GetComponent<WaypointMover>();
        }

        /// <summary>
        /// Inicializa la vista con los datos del cliente. Llamar inmediatamente
        /// después de instanciar el GameObject.
        /// </summary>
        public void Bind(CustomerData data, Vector3 spawnPosition)
        {
            Data = data;
            _mover.TeleportTo(spawnPosition);
            ApplyStateColor(data.State);
        }

        /// <summary>Cambia el color del placeholder al correspondiente al nuevo estado.</summary>
        public void ApplyStateColor(CustomerState state)
        {
            if (_renderer != null) _renderer.color = CustomerVisualController.ColorFor(state);
        }

        /// <summary>Ordena al cliente caminar hacia el destino indicado.</summary>
        public void WalkTo(Vector3 target) => _mover.MoveTo(target);

        /// <summary>Teletransporta sin animación.</summary>
        public void TeleportTo(Vector3 target) => _mover.TeleportTo(target);
    }
}
