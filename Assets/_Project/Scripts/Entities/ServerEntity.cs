using UnityEngine;

namespace CafeSim.Entities
{
    /// <summary>
    /// Vista placeholder de un servidor (cajero o barista). No se conecta al
    /// Core directamente: el <c>SimulationBootstrap</c> lo coloca en su posición
    /// y le asigna un color base. Cuando esté atendiendo (lógica futura de I3
    /// con animaciones) puede oscurecer su color o agregar un indicador encima.
    /// </summary>
    public enum ServerRole { Cashier, Barista, Both }

    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ServerEntity : MonoBehaviour
    {
        [SerializeField] private ServerRole role = ServerRole.Cashier;

        // Paleta para diferenciar visualmente cada tipo de servidor.
        private static readonly Color CashierColor = new Color(0.20f, 0.45f, 0.85f); // azul fuerte
        private static readonly Color BaristaColor = new Color(0.70f, 0.30f, 0.20f); // café oscuro
        private static readonly Color BothColor    = new Color(0.55f, 0.35f, 0.65f); // morado

        private SpriteRenderer _renderer;

        public ServerRole Role => role;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            ApplyRoleColor();
        }

        public void SetRole(ServerRole newRole)
        {
            role = newRole;
            ApplyRoleColor();
        }

        private void ApplyRoleColor()
        {
            if (_renderer == null) return;
            switch (role)
            {
                case ServerRole.Cashier: _renderer.color = CashierColor; break;
                case ServerRole.Barista: _renderer.color = BaristaColor; break;
                case ServerRole.Both:    _renderer.color = BothColor;    break;
            }
        }
    }
}
