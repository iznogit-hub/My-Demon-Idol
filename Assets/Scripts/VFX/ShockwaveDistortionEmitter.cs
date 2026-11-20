using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DemonIdol.VFX
{
    /// <summary>
    /// Spawns expanding screen-space shockwave distortion rings on combat impacts
    /// using Cartoon FX Remaster distortion assets.
    /// </summary>
    public class ShockwaveDistortionEmitter : MonoBehaviour
    {
        private static Material _distortionMaterial;
        private Material _slotMaterial;

        static readonly ShockwaveDistortionEmitter[] Pool = new ShockwaveDistortionEmitter[6];
        static int _poolCursor;

        public static void EmitShockwave(Vector3 position, float maxRadius = 2.4f, float duration = 0.28f)
        {
            EmitPooled(position, maxRadius, duration);
        }

        public static void EmitPooled(Vector3 position, float maxRadius, float duration)
        {
            int slot = _poolCursor;
            _poolCursor = (_poolCursor + 1) % Pool.Length;
            var emitter = Pool[slot];
            if (emitter == null)
            {
                GameObject waveGo = new GameObject("ShockwaveDistortionRing");
                emitter = waveGo.AddComponent<ShockwaveDistortionEmitter>();
                Pool[slot] = emitter;
            }
            emitter.gameObject.SetActive(true);
            emitter.transform.position = new Vector3(position.x, position.y, 0f);
            emitter.StopAllCoroutines();
            emitter.StartExpansion(maxRadius, duration);
        }

        private void StartExpansion(float maxRadius, float duration)
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
            if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();

            if (mf.sharedMesh == null) mf.sharedMesh = CreateRingMesh();

            if (_distortionMaterial == null)
            {
#if UNITY_EDITOR
                _distortionMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Assets/Graphics/cfxr screen distortion ring.mat");
#endif
                if (_distortionMaterial == null)
                {
                    Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
                    _distortionMaterial = new Material(s);
                    _distortionMaterial.color = new Color(1f, 1f, 1f, 0.5f);
                }
            }

            if (_slotMaterial == null)
            {
                _slotMaterial = new Material(_distortionMaterial);
                mr.sharedMaterial = _slotMaterial;
            }

            StartCoroutine(ExpandRoutine(_slotMaterial, maxRadius, duration));
        }

        private IEnumerator ExpandRoutine(Material mat, float maxRadius, float duration)
        {
            float elapsed = 0f;
            transform.localScale = Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Ease out quad expansion
                float currentRadius = Mathf.Lerp(0.3f, maxRadius, Mathf.Sin(t * Mathf.PI * 0.5f));
                transform.localScale = new Vector3(currentRadius, currentRadius, 1f);

                // Fade alpha
                float alpha = 1f - t;
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha * 0.7f;
                    mat.color = c;
                }
                else if (mat.HasProperty("_BaseColor"))
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = alpha * 0.7f;
                    mat.SetColor("_BaseColor", c);
                }

                yield return null;
            }

            gameObject.SetActive(false);
        }

        private Mesh CreateRingMesh()
        {
            Mesh mesh = new Mesh();
            int segments = 24;
            Vector3[] vertices = new Vector3[segments * 2];
            Vector2[] uvs = new Vector2[segments * 2];
            int[] triangles = new int[segments * 6];

            float innerR = 0.7f;
            float outerR = 1.0f;

            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                vertices[i * 2] = new Vector3(cos * innerR, sin * innerR, 0f);
                vertices[i * 2 + 1] = new Vector3(cos * outerR, sin * outerR, 0f);

                uvs[i * 2] = new Vector2(i / (float)segments, 0f);
                uvs[i * 2 + 1] = new Vector2(i / (float)segments, 1f);

                int next = (i + 1) % segments;
                int triIndex = i * 6;

                triangles[triIndex + 0] = i * 2;
                triangles[triIndex + 1] = i * 2 + 1;
                triangles[triIndex + 2] = next * 2;

                triangles[triIndex + 3] = next * 2;
                triangles[triIndex + 4] = i * 2 + 1;
                triangles[triIndex + 5] = next * 2 + 1;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
