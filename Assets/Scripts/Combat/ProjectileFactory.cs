using System;
using System.Collections.Generic;
using UnityEngine;
using DemonIdol.Combat;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DemonIdol.Combat
{
    /// <summary>
    /// Factory for spawning character-specific unique projectiles across all 14 fighters.
    /// Maps each fighter to their bespoke projectile data, travel parameters, and visual FX.
    /// </summary>
    public static class ProjectileFactory
    {
        public struct ProjectilePreset
        {
            public string name;
            public float speed;
            public float damage;
            public Color coreColor;
            public Color trailColor;
            public string cfxrPrefabPath;
        }

        private static readonly Dictionary<string, ProjectilePreset> _presets = new Dictionary<string, ProjectilePreset>(StringComparer.OrdinalIgnoreCase)
        {
            // C01 Akio: Shadow Ink Kunai
            { "Akio", new ProjectilePreset { name = "Shadow Ink Kunai", speed = 16f, damage = 42f, coreColor = new Color(0.12f, 0.12f, 0.15f), trailColor = new Color(0.2f, 0.2f, 0.25f), cfxrPrefabPath = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Hit A (Red).prefab" } },
            // C02 Aika: Resonance Occult Nail
            { "Aika", new ProjectilePreset { name = "Resonance Occult Nail", speed = 14f, damage = 45f, coreColor = new Color(0.85f, 0.2f, 0.6f), trailColor = new Color(0.6f, 0.1f, 0.8f), cfxrPrefabPath = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Impact Glowing HDR (Blue).prefab" } },
            // C03 Ren: Phantom Flame Shuriken
            { "Ren", new ProjectilePreset { name = "Phantom Flame Shuriken", speed = 15f, damage = 46f, coreColor = new Color(1.0f, 0.4f, 0.1f), trailColor = new Color(0.9f, 0.2f, 0.1f), cfxrPrefabPath = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Fire/CFXR3 Hit Fire B (Air).prefab" } },
            // C04 UnityChan: Aero Plasma Disc
            { "UnityChan", new ProjectilePreset { name = "Aero Plasma Disc", speed = 15f, damage = 44f, coreColor = new Color(0.1f, 0.85f, 1.0f), trailColor = new Color(0.2f, 0.9f, 0.7f), cfxrPrefabPath = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Electric/CFXR3 Hit Electric C (Air).prefab" } },
            // C05 Akane: Cursed Sonic Dagger
            { "Akane", new ProjectilePreset { name = "Cursed Sonic Dagger", speed = 18f, damage = 40f, coreColor = new Color(0.95f, 0.2f, 0.3f), trailColor = new Color(0.7f, 0.05f, 0.2f), cfxrPrefabPath = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Sword Trails/Plain/CFXR4 Sword Hit PLAIN (Cross).prefab" } },
            // C06 Cultist: Void Skull Orb
            { "Cultist", new ProjectilePreset { name = "Void Skull Orb", speed = 12f, damage = 50f, coreColor = new Color(0.4f, 0.1f, 0.7f), trailColor = new Color(0.2f, 0.05f, 0.4f), cfxrPrefabPath = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Eerie/CFXR2 Skull Head Alt.prefab" } },
            // C07 StellarWitch: Starlight Comet
            { "StellarWitch", new ProjectilePreset { name = "Starlight Comet", speed = 13f, damage = 48f, coreColor = new Color(1.0f, 0.85f, 0.3f), trailColor = new Color(0.9f, 0.6f, 1.0f), cfxrPrefabPath = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Magic Misc/CFXR4 Falling Stars.prefab" } },
            // C08 SanpakuChan: Crow Feather Needle
            { "SanpakuChan", new ProjectilePreset { name = "Crow Feather Needle", speed = 16f, damage = 42f, coreColor = new Color(0.25f, 0.25f, 0.3f), trailColor = new Color(0.5f, 0.5f, 0.55f), cfxrPrefabPath = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR3 Hit Misc A.prefab" } },
            // C09 PicoChan: Cyber Laser Cannon
            { "PicoChan", new ProjectilePreset { name = "Cyber Laser Cannon", speed = 20f, damage = 44f, coreColor = new Color(0.0f, 0.95f, 0.95f), trailColor = new Color(0.1f, 0.6f, 1.0f), cfxrPrefabPath = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Impact Glowing HDR (Blue).prefab" } },
            // C10 Reika: Crimson Crescent Wave
            { "Reika", new ProjectilePreset { name = "Crimson Crescent Wave", speed = 14f, damage = 48f, coreColor = new Color(1.0f, 0.15f, 0.35f), trailColor = new Color(0.8f, 0.05f, 0.2f), cfxrPrefabPath = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Hit A (Red).prefab" } },
            // C11 Casual1: Shockwave Air Palm
            { "Casual1", new ProjectilePreset { name = "Shockwave Air Palm", speed = 15f, damage = 45f, coreColor = new Color(0.9f, 0.9f, 0.95f), trailColor = new Color(0.7f, 0.8f, 0.9f), cfxrPrefabPath = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR Flash.prefab" } },
            // C12 Fina: Neo-Pink Plasma Burst (Lead Protagonist Signature)
            { "Fina", new ProjectilePreset { name = "Neo-Pink Plasma Burst", speed = 16f, damage = 52f, coreColor = new Color(1.0f, 0.16f, 0.43f), trailColor = new Color(1.0f, 0.4f, 0.7f), cfxrPrefabPath = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Hit A (Red).prefab" } },
            // C13 SapphiArtchan: Harmonic Pop Melody
            { "SapphiArtchan", new ProjectilePreset { name = "Harmonic Pop Melody", speed = 13f, damage = 43f, coreColor = new Color(1.0f, 0.7f, 0.85f), trailColor = new Color(0.95f, 0.85f, 0.4f), cfxrPrefabPath = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Magic Misc/CFXR4 Bouncing Glows Bubble (Blue Purple).prefab" } },
            // C14 Muryotaisu: Cursed Thread Needles
            { "Muryotaisu", new ProjectilePreset { name = "Cursed Thread Needles", speed = 17f, damage = 44f, coreColor = new Color(0.3f, 0.1f, 0.35f), trailColor = new Color(0.5f, 0.2f, 0.6f), cfxrPrefabPath = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Impact Glowing HDR (Blue).prefab" } },
            // Fighter Kai (Training / Shinobi Default)
            { "Kai", new ProjectilePreset { name = "Shinobi Star Shuriken", speed = 16f, damage = 45f, coreColor = new Color(1.0f, 0.8f, 0.2f), trailColor = new Color(0.8f, 0.5f, 0.1f), cfxrPrefabPath = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Hit D 3D (Yellow).prefab" } }
        };

        public static ProjectilePreset GetPresetForFighter(string fighterName)
        {
            if (string.IsNullOrEmpty(fighterName)) return _presets["Kai"];
            foreach (var kvp in _presets)
            {
                if (fighterName.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return kvp.Value;
                }
            }
            return _presets["Fina"];
        }

        public static MDIProjectile SpawnProjectileForCharacter(string fighterName, FighterController owner, Vector3 spawnPos, int facing)
        {
            ProjectilePreset preset = GetPresetForFighter(fighterName);

            GameObject projGo = new GameObject($"Projectile_{preset.name}");
            projGo.transform.position = spawnPos;

            MDIProjectile proj = projGo.AddComponent<MDIProjectile>();
            proj.projectileName = preset.name;
            proj.speed = preset.speed;
            proj.facingDirection = facing;
            proj.owner = owner;
            proj.trailColor = preset.trailColor;

            // AttackData definition
            AttackData atk = ScriptableObject.CreateInstance<AttackData>();
            atk.attackName = preset.name;
            atk.damage = preset.damage;
            atk.startupFrames = 10;
            atk.activeFrames = 6;
            atk.recoveryFrames = 18;
            atk.hitLevel = HitLevel.Mid;
            atk.guardRequirement = GuardRequirement.Normal;
            atk.hitstopAttacker = 4;
            atk.hitstopDefender = 8;
            atk.pushbackOnHit = 1.2f;
            atk.pushbackOnBlock = 0.9f;
            proj.attackData = atk;

            // Visual Glowing Orb / Mesh Child
            GameObject coreMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coreMesh.name = "CoreGlow";
            coreMesh.transform.SetParent(projGo.transform, false);
            coreMesh.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);

            // Strip default collider from core visual
            var col = coreMesh.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col);

            // Styled Material
            var renderer = coreMesh.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                Material mat = new Material(unlitShader);
                mat.color = preset.coreColor;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", preset.coreColor);
                renderer.material = mat;
            }

            // Trail Renderer
            TrailRenderer trail = projGo.AddComponent<TrailRenderer>();
            trail.time = 0.22f;
            trail.startWidth = 0.3f;
            trail.endWidth = 0.02f;
            trail.startColor = preset.trailColor;
            trail.endColor = new Color(preset.trailColor.r, preset.trailColor.g, preset.trailColor.b, 0f);
            Shader trailShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            trail.material = new Material(trailShader);

            // Optional CFXR child particle
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(preset.cfxrPrefabPath))
            {
                GameObject cfxrPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(preset.cfxrPrefabPath);
                if (cfxrPrefab != null)
                {
                    proj.impactVfxPrefab = cfxrPrefab;
                    GameObject effectChild = UnityEngine.Object.Instantiate(cfxrPrefab, projGo.transform);
                    effectChild.transform.localPosition = Vector3.zero;
                    effectChild.transform.localScale = Vector3.one * 0.6f;
                }
            }
#endif

            return proj;
        }
    }
}
