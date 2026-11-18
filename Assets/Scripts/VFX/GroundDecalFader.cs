using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DemonIdol.VFX
{
    /// <summary>
    /// Individual ink/blood decal on the ground pavers.
    /// Handles random orientation, scale jitter, and smooth alpha fade-out into the object pool.
    /// </summary>
    public class GroundDecalFader : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private float _lifetime = 5.0f;
        [SerializeField] private float _fadeDuration = 1.5f;

        private Color _baseColor = new Color(0.1f, 0.1f, 0.12f, 0.85f); // Sumi-e Carbon Ink
        private Coroutine _fadeRoutine;

        private void Awake()
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void Initialize(Color color, float scaleMultiplier = 1.0f)
        {
            _baseColor = color;
            transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f); // Flat on ground
            transform.localScale = Vector3.one * Random.Range(0.7f, 1.3f) * scaleMultiplier;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _baseColor;
            }

            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(FadeRoutine());
        }

        private IEnumerator FadeRoutine()
        {
            yield return new WaitForSeconds(_lifetime - _fadeDuration);

            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(_baseColor.a, 0f, elapsed / _fadeDuration);
                if (_spriteRenderer != null)
                {
                    Color c = _baseColor;
                    c.a = alpha;
                    _spriteRenderer.color = c;
                }
                yield return null;
            }

            gameObject.SetActive(false);
        }
    }
}
