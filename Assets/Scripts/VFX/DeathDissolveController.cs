using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DemonIdol.Combat;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DemonIdol.VFX
{
    /// <summary>
    /// Handles procedural incandescent death dissolve sequence when a fighter reaches 0 HP.
    /// Fades/dissolves character renderers over 1.5s with noise dissolve and ash disintegration VFX.
    /// </summary>
    public class DeathDissolveController : MonoBehaviour
    {
        public static void TriggerDissolve(FighterController fighter)
        {
            if (fighter == null) return;
            var controller = fighter.gameObject.GetComponent<DeathDissolveController>();
            if (controller == null)
            {
                controller = fighter.gameObject.AddComponent<DeathDissolveController>();
            }
            controller.StartDissolveSequence();
        }

        private void StartDissolveSequence()
        {
            StartCoroutine(DissolveRoutine());
        }

        private IEnumerator DissolveRoutine()
        {
            // Spawn defeat ash / souls VFX
            SpawnDefeatVfx(transform.position);

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            List<Material> dynamicMats = new List<Material>();

            foreach (var r in renderers)
            {
                if (r is ParticleSystemRenderer) continue;
                foreach (var m in r.materials)
                {
                    if (m != null && !dynamicMats.Contains(m))
                    {
                        dynamicMats.Add(m);
                    }
                }
            }

            float duration = 1.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                // Update dissolve cutoff / alpha on all material instances
                foreach (var mat in dynamicMats)
                {
                    if (mat.HasProperty("_DissolveCutoff"))
                    {
                        mat.SetFloat("_DissolveCutoff", progress);
                    }
                    else if (mat.HasProperty("_Dissolve"))
                    {
                        mat.SetFloat("_Dissolve", progress);
                    }
                    else if (mat.HasProperty("_Cutoff"))
                    {
                        mat.SetFloat("_Cutoff", progress * 0.95f);
                    }
                    else if (mat.HasProperty("_BaseColor"))
                    {
                        Color c = mat.GetColor("_BaseColor");
                        c.a = 1f - progress;
                        mat.SetColor("_BaseColor", c);
                    }
                }

                yield return null;
            }

            // After dissolve complete, ensure character remains cleanly faded
            foreach (var mat in dynamicMats)
            {
                if (mat.HasProperty("_DissolveCutoff")) mat.SetFloat("_DissolveCutoff", 1.0f);
                else if (mat.HasProperty("_Dissolve")) mat.SetFloat("_Dissolve", 1.0f);
            }
        }

        private void SpawnDefeatVfx(Vector3 pos)
        {
#if UNITY_EDITOR
            string[] defeatVfxPaths = {
                "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Eerie/CFXR2 Souls Escape.prefab",
                "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Explosions/CFXR Explosion Smoke 2 Solo (HDR).prefab",
                "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR Magic Poof.prefab"
            };

            foreach (var path in defeatVfxPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    GameObject vfx = Instantiate(prefab, pos + Vector3.up * 1.0f, Quaternion.identity);
                    Destroy(vfx, 2.5f);
                    return;
                }
            }
#endif
            if (InkBloodSplatterManager.Instance != null)
            {
                InkBloodSplatterManager.Instance.SpawnAirborneBurst(pos + Vector3.up * 1.0f, Color.black, 2.0f);
            }
        }
    }
}
