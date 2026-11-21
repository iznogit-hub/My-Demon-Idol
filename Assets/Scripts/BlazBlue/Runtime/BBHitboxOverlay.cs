using System;
using UnityEngine;
using DemonIdol.UI;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Runtime 2D hitbox overlay for training mode and debug analysis.
    /// Visualizes Hurt (green), Hit (red), Push (yellow), and Throw (blue) boxes
    /// directly on the Z=0 plane using GL lines.
    /// Respects SettingsPanel.HighContrastHitboxes.
    /// </summary>
    public class BBHitboxOverlay : MonoBehaviour
    {
        public static bool IsOverlayActive = false;

        [Header("Visualization Settings")]
        public bool showHurtBoxes = true;
        public bool showHitBoxes = true;
        public bool showPushBoxes = true;
        public bool showThrowBoxes = true;

        private Material _lineMaterial;
        private readonly BBBoxBuffer _tempBuffer = new BBBoxBuffer();

        private void Awake()
        {
            CreateLineMaterial();
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.F1))
            {
                IsOverlayActive = !IsOverlayActive;
                BBFrameMeter.IsMeterVisible = IsOverlayActive;
                Debug.Log($"<color=#00e676>[BBHitboxOverlay] Debug Overlay (F1) toggled: {IsOverlayActive}</color>");
            }
        }

        private void CreateLineMaterial()
        {
            if (_lineMaterial == null)
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    _lineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                    _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    _lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                    _lineMaterial.SetInt("_ZWrite", 0);
                }
            }
        }

        private void OnRenderObject()
        {
            if (!IsOverlayActive) return;
            if (_lineMaterial == null) CreateLineMaterial();
            if (_lineMaterial == null) return;

            var brains = FindObjectsByType<BBFighterBrain>(FindObjectsSortMode.None);
            if (brains == null || brains.Length == 0) return;

            bool highContrast = SettingsPanel.HighContrastHitboxes;

            _lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.Begin(GL.LINES);

            for (int b = 0; b < brains.Length; b++)
            {
                var brain = brains[b];
                if (brain == null || !brain.isActiveAndEnabled) continue;

                _tempBuffer.Clear();
                brain.PopulateWorldBoxes(_tempBuffer);

                for (int i = 0; i < _tempBuffer.count; i++)
                {
                    ref BBWorldBox box = ref _tempBuffer.boxes[i];
                    Color col = GetBoxColor(box.kind, box.attribute, highContrast);
                    if (col.a <= 0f) continue;

                    GL.Color(col);
                    DrawBoxLines(box.minX, box.minY, box.maxX, box.maxY);
                }
            }

            GL.End();
            GL.PopMatrix();
        }

        private void OnDrawGizmos()
        {
            if (!IsOverlayActive) return;

            var brains = FindObjectsByType<BBFighterBrain>(FindObjectsSortMode.None);
            if (brains == null) return;

            bool highContrast = SettingsPanel.HighContrastHitboxes;

            for (int b = 0; b < brains.Length; b++)
            {
                var brain = brains[b];
                if (brain == null || !brain.isActiveAndEnabled) continue;

                _tempBuffer.Clear();
                brain.PopulateWorldBoxes(_tempBuffer);

                for (int i = 0; i < _tempBuffer.count; i++)
                {
                    ref BBWorldBox box = ref _tempBuffer.boxes[i];
                    Gizmos.color = GetBoxColor(box.kind, box.attribute, highContrast);
                    Vector3 center = new Vector3(box.CenterX, box.CenterY, 0f);
                    Vector3 size = new Vector3(box.maxX - box.minX, box.maxY - box.minY, 0.05f);
                    Gizmos.DrawWireCube(center, size);
                }
            }
        }

        private Color GetBoxColor(BBBoxKind kind, BBAttribute attribute, bool highContrast)
        {
            float alpha = highContrast ? 1.0f : 0.65f;
            switch (kind)
            {
                case BBBoxKind.Hit:
                    if (!showHitBoxes) return Color.clear;
                    return new Color(1f, 0.1f, 0.1f, alpha); // Red

                case BBBoxKind.Hurt:
                    if (!showHurtBoxes) return Color.clear;
                    return new Color(0.1f, 1f, 0.2f, alpha); // Green

                case BBBoxKind.Push:
                    if (!showPushBoxes) return Color.clear;
                    return new Color(1f, 0.9f, 0.1f, alpha); // Yellow

                case BBBoxKind.Throw:
                    if (!showThrowBoxes) return Color.clear;
                    return new Color(0.1f, 0.6f, 1f, alpha); // Blue

                case BBBoxKind.ThrowHurt:
                    return new Color(0.7f, 0.2f, 0.9f, alpha * 0.7f); // Purple

                case BBBoxKind.GuardProximity:
                    return new Color(1f, 0.5f, 0f, alpha * 0.5f); // Orange

                default:
                    return new Color(0.8f, 0.8f, 0.8f, alpha);
            }
        }

        private void DrawBoxLines(float minX, float minY, float maxX, float maxY)
        {
            // Bottom edge
            GL.Vertex3(minX, minY, 0f);
            GL.Vertex3(maxX, minY, 0f);

            // Right edge
            GL.Vertex3(maxX, minY, 0f);
            GL.Vertex3(maxX, maxY, 0f);

            // Top edge
            GL.Vertex3(maxX, maxY, 0f);
            GL.Vertex3(minX, maxY, 0f);

            // Left edge
            GL.Vertex3(minX, maxY, 0f);
            GL.Vertex3(minX, minY, 0f);
        }
    }
}
