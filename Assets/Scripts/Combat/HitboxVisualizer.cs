using System;
using System.Collections.Generic;
using UnityEngine;
using DemonIdol.UI;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Runtime Hitbox / Hurtbox Visualizer for Lab and Practice mode.
    /// Draws wireframe / translucent bounding cubes around active Hitboxes (Red) and Hurtboxes (Green).
    /// Respects SettingsPanel.HighContrastHitboxes.
    /// </summary>
    public class HitboxVisualizer : MonoBehaviour
    {
        public static HitboxVisualizer Instance { get; private set; }

        public bool isVisible = false;

        private Material _hitboxMat;
        private Material _hurtboxMat;

        private readonly List<Collider> _activeHitboxes = new List<Collider>();
        private readonly List<Collider> _activeHurtboxes = new List<Collider>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            CreateMaterials();
        }

        private void CreateMaterials()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Hidden/Internal-Colored");
            if (shader != null)
            {
                _hitboxMat = new Material(shader) { color = new Color(1f, 0.1f, 0.1f, 0.5f) };
                _hurtboxMat = new Material(shader) { color = new Color(0.1f, 0.9f, 0.2f, 0.35f) };
            }
        }

        public void SetVisible(bool visible)
        {
            isVisible = visible;
        }

        public void ToggleVisibility()
        {
            SetVisible(!isVisible);
        }

        private void Update()
        {
            if (SafeGetKeyDown(KeyCode.F1) || SafeGetKeyDown(KeyCode.F4) || SafeGetKeyDown(KeyCode.H))
            {
                ToggleVisibility();
            }
        }

        private static bool SafeGetKeyDown(KeyCode key)
        {
            try { return UnityEngine.Input.GetKeyDown(key); }
            catch { return false; }
        }

        private void LateUpdate()
        {
            if (!isVisible && !SettingsPanel.HighContrastHitboxes)
            {
                return;
            }

            FindColliders();
        }

        private void FindColliders()
        {
            _activeHitboxes.Clear();
            _activeHurtboxes.Clear();

            var hitboxes = FindObjectsByType<Hitbox>();
            foreach (var hb in hitboxes)
            {
                var col = hb.GetComponent<Collider>();
                if (col != null && col.enabled) _activeHitboxes.Add(col);
            }

            var hurtboxes = FindObjectsByType<Hurtbox>();
            foreach (var hurt in hurtboxes)
            {
                var col = hurt.GetComponent<Collider>();
                if (col != null && col.enabled) _activeHurtboxes.Add(col);
            }
        }

        private void OnGUI()
        {
            if (!isVisible) return;
            GUI.color = Color.white;
            GUI.Box(new Rect(10, 10, 280, 50), "<b>[F1] HITBOX VISUALIZER: ON</b>");
            GUI.Label(new Rect(20, 30, 260, 25), $"Active Hitboxes: {_activeHitboxes.Count} | Hurtboxes: {_activeHurtboxes.Count}");
        }

        private void OnDrawGizmos()
        {
            if (!isVisible) return;

            // Render Hurtboxes in Green
            Gizmos.color = new Color(0f, 1f, 0.2f, 0.4f);
            foreach (var col in _activeHurtboxes)
            {
                if (col == null) continue;
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }

            // Render Hitboxes in Bright Red
            Gizmos.color = new Color(1f, 0f, 0f, 0.7f);
            foreach (var col in _activeHitboxes)
            {
                if (col == null) continue;
                Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            }
        }
    }
}
