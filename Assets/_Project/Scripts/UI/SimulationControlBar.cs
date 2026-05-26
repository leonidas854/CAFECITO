using System;
using UnityEngine;
using UnityEngine.UI;
using CafeSim.Core;
using CafeSim.Data;

namespace CafeSim.UI
{
    /// <summary>
    /// Barra inferior con los controles globales del simulador: pausar/reanudar,
    /// reiniciar, y elegir velocidad de simulación (afecta <c>Time.timeScale</c>).
    ///
    /// <para>El botón "Reiniciar" llama a <c>Configure</c> con el config actual,
    /// lo que regenera el TableManager y permite que cambios estructurales
    /// (mesas, sillas, semilla) tomen efecto.</para>
    /// </summary>
    public sealed class SimulationControlBar : MonoBehaviour
    {
        private static readonly float[] SpeedPresets = { 0.5f, 1f, 2f, 5f, 10f };

        private SimulationConfig _config;
        private ParameterControls _parameterControls; // para refrescar sliders al reset

        private Button _playPauseButton;
        private Text _playPauseLabel;
        private Text _statusText;
        private Button[] _speedButtons;

        private bool _isPaused;
        private float _currentSpeed = 1f;

        // ─── Construcción ─────────────────────────────────────────────────────

        public void Build(SimulationConfig config, ParameterControls parameterControls)
        {
            _config = config;
            _parameterControls = parameterControls;

            UIFactory.AddHorizontalLayout(gameObject, spacing: 8f,
                padding: new RectOffset(12, 12, 8, 8),
                childForceExpandWidth: false, childForceExpandHeight: false);

            // Play/Pause
            _playPauseButton = UIFactory.CreateButton(transform, "PAUSAR",
                UIFactory.ButtonAccent, OnTogglePlayPause);
            UIFactory.AddLayoutElement(_playPauseButton.gameObject,
                minWidth: 110f, preferredWidth: 110f, minHeight: 36f, preferredHeight: 36f);
            _playPauseLabel = _playPauseButton.GetComponentInChildren<Text>();

            // Reiniciar
            var resetBtn = UIFactory.CreateButton(transform, "REINICIAR",
                UIFactory.ButtonDanger, OnReset);
            UIFactory.AddLayoutElement(resetBtn.gameObject,
                minWidth: 110f, preferredWidth: 110f, minHeight: 36f, preferredHeight: 36f);

            // Separador con etiqueta Velocidad
            var speedLabel = UIFactory.CreateText(transform, "Velocidad:", 14,
                UIFactory.TextMuted, TextAnchor.MiddleLeft);
            UIFactory.AddLayoutElement(speedLabel.gameObject,
                minWidth: 80f, preferredWidth: 80f);

            // Botones de velocidad
            _speedButtons = new Button[SpeedPresets.Length];
            for (int i = 0; i < SpeedPresets.Length; i++)
            {
                float speed = SpeedPresets[i];
                var btn = UIFactory.CreateButton(transform, FormatSpeed(speed),
                    UIFactory.ButtonNormal, () => OnSpeedSelected(speed));
                UIFactory.AddLayoutElement(btn.gameObject,
                    minWidth: 56f, preferredWidth: 56f, minHeight: 36f, preferredHeight: 36f);
                _speedButtons[i] = btn;
            }

            // Status (a la derecha, flexible)
            _statusText = UIFactory.CreateText(transform, "Listo.", 13,
                UIFactory.TextAccent, TextAnchor.MiddleRight);
            UIFactory.AddLayoutElement(_statusText.gameObject,
                flexibleWidth: 1f, minHeight: 36f);

            // Aplicar valor inicial
            ApplySpeed(1f);
        }

        // ─── Acciones ─────────────────────────────────────────────────────────

        private void OnTogglePlayPause()
        {
            _isPaused = !_isPaused;
            if (_isPaused)
            {
                Time.timeScale = 0f;
                SimulationManager.Instance.Pause();
                _playPauseLabel.text = "REANUDAR";
                _statusText.text = "Pausado.";
            }
            else
            {
                Time.timeScale = _currentSpeed;
                SimulationManager.Instance.Resume();
                _playPauseLabel.text = "PAUSAR";
                _statusText.text = "Corriendo a " + FormatSpeed(_currentSpeed) + ".";
            }
        }

        private void OnReset()
        {
            try
            {
                var parameters = _config.ToSimulationParameters();
                SimulationManager.Instance.Configure(parameters);
                _isPaused = false;
                _playPauseLabel.text = "PAUSAR";
                Time.timeScale = _currentSpeed;
                _statusText.text = "Reiniciado.";
                if (_parameterControls != null) _parameterControls.ClearStructuralHint();
            }
            catch (Exception e)
            {
                _statusText.text = "Error: " + e.Message;
            }
        }

        private void OnSpeedSelected(float speed)
        {
            _currentSpeed = speed;
            ApplySpeed(speed);
            if (!_isPaused) Time.timeScale = speed;
            _statusText.text = _isPaused
                ? "Velocidad lista (pausado)."
                : "Corriendo a " + FormatSpeed(speed) + ".";
        }

        private void ApplySpeed(float selected)
        {
            for (int i = 0; i < SpeedPresets.Length; i++)
            {
                var colors = _speedButtons[i].colors;
                bool isSelected = Mathf.Approximately(SpeedPresets[i], selected);
                _speedButtons[i].image.color = isSelected
                    ? UIFactory.ButtonAccent
                    : UIFactory.ButtonNormal;
                _speedButtons[i].colors = colors;
            }
        }

        private static string FormatSpeed(float speed)
        {
            if (Mathf.Approximately(speed, Mathf.Round(speed))) return $"{(int)speed}x";
            return $"{speed:F1}x";
        }

        private void OnDestroy()
        {
            // Restaurar timescale al salir del play mode/escena por si quedó en 0.
            Time.timeScale = 1f;
        }
    }
}
