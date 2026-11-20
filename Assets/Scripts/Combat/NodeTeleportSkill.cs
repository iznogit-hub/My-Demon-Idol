using System;
using System.Collections.Generic;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.Audio;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DemonIdol.Combat
{
    /// <summary>
    /// Node-based spatial teleport skill for agile / occult fighters
    /// (Cultist, Fina, Akane, Reika, Muryotaisu).
    /// Instantly translates fighter through spatial blink nodes with boundary collision safety,
    /// departure/arrival VFX, and tactical behind-the-opponent positioning.
    /// </summary>
    public static class NodeTeleportSkill
    {
        private static readonly HashSet<string> _teleportFighters = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Cultist", "Fina", "Akane", "Reika", "Muryotaisu", "C06_Cultist", "C12_Fina", "C05_Akane", "C12B_Reika", "C14_Muryotaisu"
        };

        public static bool IsTeleportFighter(string fighterName)
        {
            if (string.IsNullOrEmpty(fighterName)) return false;
            foreach (var name in _teleportFighters)
            {
                if (fighterName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        public static void ExecuteTeleport(FighterController fighter)
        {
            if (fighter == null) return;

            Vector3 startPos = fighter.transform.position;

            // Target destination: behind opponent if within 4.5m, otherwise 3m forward
            float targetX;
            if (fighter.opponent != null && Mathf.Abs(fighter.opponent.transform.position.x - startPos.x) <= 4.5f)
            {
                // Blink behind opponent
                int oppFacing = fighter.opponent.FacingDirection;
                targetX = fighter.opponent.transform.position.x - (oppFacing * 1.15f);
            }
            else
            {
                targetX = startPos.x + (fighter.FacingDirection * 3.2f);
            }

            // Strict Arena Boundary Safety
            targetX = PushbackSolver.ClampToArena(targetX);
            Vector3 targetPos = new Vector3(targetX, startPos.y, 0f);

            // Spawn Departure VFX Node
            SpawnTeleportNodeVfx(startPos);

            // Spatial Translation
            fighter.transform.position = targetPos;

            // Spawn Arrival VFX Node
            SpawnTeleportNodeVfx(targetPos);

            // Re-latch facing towards opponent
            if (fighter.opponent != null)
            {
                fighter.SetFacing(fighter.opponent.transform.position.x >= targetPos.x ? 1 : -1);
            }

            // Audio & Haptics
            if (CombatAudioDirector.Instance != null)
            {
                CombatAudioDirector.Instance.PlayParry();
            }
            MobileHaptics.Trigger(HapticImpact.Medium);

            // Brief recovery state (8 ticks)
            fighter.SetState(FighterState.AttackRecovery, 8);
            fighter.TriggerAnimation("DriveSkill");
        }

        private static void SpawnTeleportNodeVfx(Vector3 position)
        {
#if UNITY_EDITOR
            string[] nodeVfxPaths = {
                "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR Magic Poof.prefab",
                "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Eerie/CFXR2 Souls Escape.prefab",
                "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR Flash.prefab"
            };

            foreach (var path in nodeVfxPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    GameObject vfx = UnityEngine.Object.Instantiate(prefab, position + Vector3.up * 0.9f, Quaternion.identity);
                    UnityEngine.Object.Destroy(vfx, 1.2f);
                    return;
                }
            }
#endif
            // Fallback: spawn ink burst from InkBloodSplatterManager
            if (DemonIdol.VFX.InkBloodSplatterManager.Instance != null)
            {
                DemonIdol.VFX.InkBloodSplatterManager.Instance.SpawnAirborneBurst(position + Vector3.up * 0.9f, Color.black, 1.5f);
            }
        }
    }
}
