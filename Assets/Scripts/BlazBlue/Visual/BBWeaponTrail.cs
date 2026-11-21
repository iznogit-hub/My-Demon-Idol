using System;
using UnityEngine;

namespace DemonIdol.BlazBlue.Visual
{
    /// <summary>
    /// Phase 4: Archetype-calibrated weapon trail renderer.
    /// Provides zero-allocation dynamic weapon trails synchronized with
    /// attack startup/active frames and archetype blade proportions.
    /// </summary>
    [DisallowMultipleComponent]
    public class BBWeaponTrail : MonoBehaviour
    {
        [Header("Trail Configuration")]
        public TrailRenderer trailRenderer;
        public float startWidth = 0.25f;
        public float endWidth = 0.0f;
        public float lifetime = 0.18f;
        public Color trailColor = new Color(1f, 1f, 1f, 0.85f);
        public Vector3 tipOffset = Vector3.forward;

        private Transform _tipAnchor;

        private void Awake()
        {
            EnsureTrailInitialized();
        }

        public void EnsureTrailInitialized()
        {
            if (trailRenderer == null)
            {
                trailRenderer = GetComponentInChildren<TrailRenderer>();
            }

            if (trailRenderer == null)
            {
                // Create a dedicated tip anchor child
                var anchorGO = new GameObject("TrailAnchor");
                anchorGO.transform.SetParent(transform, false);
                anchorGO.transform.localPosition = tipOffset;
                _tipAnchor = anchorGO.transform;

                trailRenderer = anchorGO.AddComponent<TrailRenderer>();
            }
            else
            {
                _tipAnchor = trailRenderer.transform;
            }

            // Configure trail parameters
            trailRenderer.time = lifetime;
            trailRenderer.startWidth = startWidth;
            trailRenderer.endWidth = endWidth;
            trailRenderer.emitting = false;
            trailRenderer.autodestruct = false;
            trailRenderer.Clear();

            // Soft alpha gradient
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(trailColor, 0.0f),
                    new GradientColorKey(trailColor, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(trailColor.a, 0.0f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            trailRenderer.colorGradient = gradient;
        }

        public void SetEmitting(bool emit)
        {
            if (trailRenderer == null) EnsureTrailInitialized();
            if (trailRenderer != null)
            {
                if (emit && !trailRenderer.emitting)
                {
                    trailRenderer.Clear();
                }
                trailRenderer.emitting = emit;
            }
        }
    }
}
