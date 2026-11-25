#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using DemonIdol.BlazBlue;
using DemonIdol.BlazBlue.Visual;
using DemonIdol.Combat;

namespace DemonIdol.Editor.Combat
{
    /// <summary>
    /// Editor Hitbox Baker for My Demon Idol.
    /// Extracts exact strike timing (Hit() / Shoot() Animation Events), Warrior prefab collider bounds,
    /// and real animated limb/weapon swing envelopes from the 12 ExplosiveLLC Warrior Packs.
    /// Bakes deterministic 2D AABB hitboxes into BBFighterPrefabConfig and BBMatchSim / BBAttackData.
    /// Zero PhysX runtime collisions — 100% deterministic, rollback-safe.
    /// </summary>
    public static class WarriorHitboxBaker
    {
        public struct FighterConfig
        {
            public string id;
            public string name;
            public string warriorPack;
            public bool isWeaponFighter;
            public bool hasProjectile;
            public string[] projectileMoveIds;
            public string prefabPath;
            public string profilePath;
            public string moveSetPath;
            public string demoPrefabPath;
        }

        public static readonly FighterConfig[] LaunchFighters = new FighterConfig[]
        {
            new FighterConfig
            {
                id = "C01",
                name = "Akio",
                warriorPack = "Karate",
                isWeaponFighter = false,
                hasProjectile = false,
                projectileMoveIds = new string[0],
                prefabPath = "Assets/Prefabs/Characters/C01_Akio.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Akio/BBProfile_C01_Akio.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Akio/BBMoveSet_C01_Akio.asset",
                demoPrefabPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Karate Warrior Mecanim Animation Pack/Prefabs/Karate Warrior.prefab"
            },
            new FighterConfig
            {
                id = "C02",
                name = "Aika",
                warriorPack = "Ninja",
                isWeaponFighter = true,
                hasProjectile = false,
                projectileMoveIds = new string[0],
                prefabPath = "Assets/Prefabs/Characters/C02_Aika.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Aika/BBProfile_C02_Aika.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Aika/BBMoveSet_C02_Aika.asset",
                demoPrefabPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Ninja Warrior Mecanim Animation Pack/Prefabs/Ninja Warrior.prefab"
            },
            new FighterConfig
            {
                id = "C05",
                name = "Akane",
                warriorPack = "Sorceress",
                isWeaponFighter = true,
                hasProjectile = true,
                projectileMoveIds = new[] { "5D", "236236C" },
                prefabPath = "Assets/Prefabs/Characters/C05_Akane.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Archetypes/spear/BBProfile_C05_Akane.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Archetypes/spear/BBMoveSet_C05.asset",
                demoPrefabPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Sorceress Warrior Mecanim Animation Pack/Prefabs/Sorceress Warrior.prefab"
            },
            new FighterConfig
            {
                id = "EX04",
                name = "OfficerK",
                warriorPack = "Brute",
                isWeaponFighter = true,
                hasProjectile = false,
                projectileMoveIds = new string[0],
                prefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX04_OfficerK.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Archetypes/unarmed/BBProfile_EX04_OfficerK.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/OfficerK/BBMoveSet_EX04_OfficerK.asset",
                demoPrefabPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE/Brute Warrior Mecanim Animation Pack/Prefabs/Brute Warrior.prefab"
            },
            new FighterConfig
            {
                id = "C11",
                name = "Casual1",
                warriorPack = "Knight",
                isWeaponFighter = true,
                hasProjectile = false,
                projectileMoveIds = new string[0],
                prefabPath = "Assets/Prefabs/Characters/C11_Casual1.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Archetypes/crossbow/BBProfile_C11_Casual1.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Casual1/BBMoveSet_C11_Casual1.asset",
                demoPrefabPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Prefabs/Knight Warrior.prefab"
            },
            new FighterConfig
            {
                id = "C06",
                name = "Cultist",
                warriorPack = "Mage",
                isWeaponFighter = false,
                hasProjectile = true,
                projectileMoveIds = new[] { "5D", "236236C" },
                prefabPath = "Assets/Prefabs/Characters/C06_Cultist.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Cultist/BBProfile_C06_Cultist.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Cultist/BBMoveSet_C06_Cultist.asset",
                demoPrefabPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Mage Warrior Mecanim Animation Pack/Prefabs/Mage Warrior.prefab"
            },
            new FighterConfig
            {
                id = "C09",
                name = "PicoChan",
                warriorPack = "Archer",
                isWeaponFighter = true,
                hasProjectile = true,
                projectileMoveIds = new[] { "5C", "5D" },
                prefabPath = "Assets/Prefabs/Characters/C09_PicoChan.prefab",
                profilePath = "Assets/CombatData/BlazBlue/PicoChan/BBProfile_C09_PicoChan.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/PicoChan/BBMoveSet_C09_PicoChan.asset",
                demoPrefabPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Archer Warrior Mecanim Animation Pack/Prefabs/Archer Warrior.prefab"
            },
            new FighterConfig
            {
                id = "C14",
                name = "Muryotaisu",
                warriorPack = "TwoHanded",
                isWeaponFighter = true,
                hasProjectile = false,
                projectileMoveIds = new string[0],
                prefabPath = "Assets/Prefabs/Characters/C14_Muryotaisu.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Archetypes/twohand/BBProfile_C14_Muryotaisu.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Muryotaisu/BBMoveSet_C14_Muryotaisu.asset",
                demoPrefabPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/2 Handed Warrior Mecanim Animation Pack/Prefabs/2Handed Warrior.prefab"
            },
            new FighterConfig
            {
                id = "C08",
                name = "SanpakuChan",
                warriorPack = "Swordsman",
                isWeaponFighter = true,
                hasProjectile = false,
                projectileMoveIds = new string[0],
                prefabPath = "Assets/Prefabs/Characters/C08_SanpakuChan.prefab",
                profilePath = "Assets/CombatData/BlazBlue/SanpakuChan/BBProfile_C08_SanpakuChan.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/SanpakuChan/BBMoveSet_C08_SanpakuChan.asset",
                demoPrefabPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Swordsman Warrior Mecanim Animation Pack/Prefabs/Swordsman Warrior.prefab"
            },
            new FighterConfig
            {
                id = "C12",
                name = "Fina",
                warriorPack = "Crossbow",
                isWeaponFighter = true,
                hasProjectile = true,
                projectileMoveIds = new[] { "5B", "5C", "5D" },
                prefabPath = "Assets/Prefabs/Characters/C12_Fina.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Fina/BBProfile_C12_Fina.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Fina/BBMoveSet_C12_Fina.asset",
                demoPrefabPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Prefabs/Crossbow Warrior.prefab"
            },
            new FighterConfig
            {
                id = "EX01",
                name = "Hugo",
                warriorPack = "Spearman",
                isWeaponFighter = true,
                hasProjectile = false,
                projectileMoveIds = new string[0],
                prefabPath = "Assets/Prefabs/Characters/DotaHeroes/EX01_Hugo.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Archetypes/brute/BBProfile_EX01_Hugo.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Hugo/BBMoveSet_EX01_Hugo.asset",
                demoPrefabPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Spearman Warrior Mecanim Animation Pack/Prefabs/Spearman Warrior.prefab"
            },
            new FighterConfig
            {
                id = "SK08",
                name = "Torgrim",
                warriorPack = "Hammer",
                isWeaponFighter = true,
                hasProjectile = false,
                projectileMoveIds = new string[0],
                prefabPath = "Assets/Prefabs/Characters/Sidekicks/SK08_Torgrim.prefab",
                profilePath = "Assets/CombatData/BlazBlue/Torgrim/BBMoveSet_SK08_Torgrim.asset",
                moveSetPath = "Assets/CombatData/BlazBlue/Torgrim/BBMoveSet_SK08_Torgrim.asset",
                demoPrefabPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Hammer Warrior Mecanim Animation Pack/Prefabs/Hammer Warrior.prefab"
            }
        };

        private static readonly string[] SevenMoves = new[] { "5A", "5B", "5C", "2C", "jB", "5D", "236236C" };

        public static void BakeAll12WarriorHitboxes()
        {
            Debug.Log("<color=#00e676><b>[Warrior Hitbox Baker] >>> STARTING 12-FIGHTER HITBOX & HURTBOX BAKING PASS <<<</b></color>");

            var sbReport = new StringBuilder();
            sbReport.AppendLine("# MDI 12-FIGHTER WARRIOR HITBOX BAKER AUDIT REPORT");
            sbReport.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sbReport.AppendLine("| Fighter | Pack | Demo Collider | Hit/Shoot Events | 5A Reach | 5C Reach | 5D Reach | Hurtbox H | Projectile Spawn | Status |");
            sbReport.AppendLine("|:---|:---|:---|:---|:---|:---|:---|:---|:---|:---|");

            int totalFightersBaked = 0;

            foreach (var fighter in LaunchFighters)
            {
                // =====================================================================
                // STEP 1: Inspect Warrior Demo Prefab for Colliders & Inspect Animation Events
                // =====================================================================
                bool hasDemoCollider = false;
                string colliderType = "None";
                if (File.Exists(fighter.demoPrefabPath))
                {
                    GameObject demoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fighter.demoPrefabPath);
                    if (demoPrefab != null)
                    {
                        var col = demoPrefab.GetComponentInChildren<Collider>();
                        if (col != null)
                        {
                            hasDemoCollider = true;
                            colliderType = col.GetType().Name;
                        }
                    }
                }

                BBMoveSet moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>(fighter.moveSetPath);
                int hitEventsFound = 0;
                int shootEventsFound = 0;

                // =====================================================================
                // STEP 2: Instantiate Preview, Calibrate Hurtboxes & Bake 7-Move Hitboxes
                // =====================================================================
                if (!File.Exists(fighter.prefabPath))
                {
                    Debug.LogWarning($"[Warrior Hitbox Baker] Missing prefab at {fighter.prefabPath}");
                    continue;
                }

                GameObject prefabContents = PrefabUtility.LoadPrefabContents(fighter.prefabPath);
                if (prefabContents == null) continue;

                float baked5AReach = 0.95f;
                float baked5CReach = 1.55f;
                float baked5DReach = 1.76f;
                float standingHurtH = 1.75f;
                Vector2 projectileSpawn = Vector2.zero;

                try
                {
                    var anim = prefabContents.GetComponentInChildren<Animator>();
                    var weaponRig = prefabContents.GetComponentInChildren<BBWeaponRig>();
                    var pConfig = prefabContents.GetComponent<BBFighterPrefabConfig>();
                    if (pConfig == null) pConfig = prefabContents.AddComponent<BBFighterPrefabConfig>();
                    pConfig.characterId = fighter.id;
                    pConfig.characterDisplayName = fighter.name;
                    pConfig.animator = anim;

                    if (weaponRig != null)
                    {
                        weaponRig.Initialize(anim);
                    }

                    // 1. Calibrate standing & crouching hurtbox and pushbox from character mesh / bones
                    float avatarHeight = 1.75f;
                    float hipWidth = 0.34f;
                    var renderers = prefabContents.GetComponentsInChildren<SkinnedMeshRenderer>();
                    if (renderers.Length > 0)
                    {
                        Bounds combined = renderers[0].bounds;
                        for (int r = 1; r < renderers.Length; r++) combined.Encapsulate(renderers[r].bounds);
                        if (combined.size.y >= 1.2f && combined.size.y <= 2.4f)
                        {
                            avatarHeight = combined.size.y;
                        }
                    }

                    if (anim != null && anim.isHuman)
                    {
                        var lLeg = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                        var rLeg = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                        if (lLeg != null && rLeg != null)
                        {
                            float d = Vector3.Distance(lLeg.position, rLeg.position);
                            if (d >= 0.15f && d <= 0.6f) hipWidth = d;
                        }
                    }

                    standingHurtH = avatarHeight;
                    pConfig.standingHurtbox = new Rect(0.02f, avatarHeight * 0.52f, hipWidth * 1.35f, avatarHeight * 0.94f);
                    pConfig.crouchingHurtbox = new Rect(0.04f, avatarHeight * 0.32f, hipWidth * 1.55f, avatarHeight * 0.60f);
                    pConfig.pushbox = new Rect(0f, avatarHeight * 0.48f, hipWidth * 1.15f, avatarHeight * 0.90f);

                    // 2. Bake 7 Moves
                    if (pConfig.moveHitboxes == null) pConfig.moveHitboxes = new List<MoveHitboxConfig>();

                    for (int m = 0; m < SevenMoves.Length; m++)
                    {
                        string mid = SevenMoves[m];
                        string searchMid = mid == "jB" ? "j.B" : mid;
                        BBAttackData atk = moveSet != null ? moveSet.Find(searchMid) : null;

                        // Check if developer already authored custom override
                        int existingIdx = pConfig.moveHitboxes.FindIndex(cfg => cfg.moveId == mid || cfg.moveId == searchMid);
                        if (existingIdx >= 0 && pConfig.moveHitboxes[existingIdx].useCustomOverride)
                        {
                            continue; // Preserve developer authored box!
                        }

                        // Inspect animation events
                        AnimationClip clip = atk != null ? (atk.clip ?? atk.animationClip) : null;
                        float detectedEventTime = -1f;
                        bool hasHitEvent = false;
                        bool hasShootEvent = false;

                        if (clip != null)
                        {
                            var events = AnimationUtility.GetAnimationEvents(clip);
                            foreach (var ev in events)
                            {
                                if (ev.functionName.IndexOf("Hit", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    hasHitEvent = true;
                                    detectedEventTime = ev.time;
                                    hitEventsFound++;
                                    break;
                                }
                                if (ev.functionName.IndexOf("Shoot", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    hasShootEvent = true;
                                    detectedEventTime = ev.time;
                                    shootEventsFound++;
                                    break;
                                }
                            }
                        }

                        // Derive startup / active / recovery ticks:
                        // Preserve existing BBAttackData frame data (which strictly satisfies Dustloop recovery math)
                        int startupTicks = atk != null ? atk.startupFrames : GetDefaultStartup(mid);
                        int activeTicks = atk != null ? atk.activeFrames : GetDefaultActive(mid);
                        int recoveryTicks = atk != null ? atk.recoveryFrames : Mathf.Max(4, GetDefaultTotal(mid) - startupTicks - activeTicks);

                        if (atk == null && detectedEventTime > 0.01f && clip != null && clip.length > 0.05f)
                        {
                            int totalTicks = GetDefaultTotal(mid);
                            int evtTick = Mathf.RoundToInt((detectedEventTime / clip.length) * totalTicks);
                            startupTicks = Mathf.Clamp(evtTick, GetMinStartup(mid), GetMaxStartup(mid));
                            recoveryTicks = Mathf.Max(4, totalTicks - startupTicks - activeTicks);
                        }

                        // Sample pose at active window apex
                        float sampleTime = 0.1f;
                        if (clip != null && clip.length > 0.05f)
                        {
                            int totalTicks = atk != null ? atk.TotalTicks : GetDefaultTotal(mid);
                            sampleTime = clip.length * ((float)startupTicks / Mathf.Max(1, totalTicks));
                            clip.SampleAnimation(prefabContents, sampleTime);
                        }

                        // Measure striking extremity or weapon envelope
                        float forwardReach = GetTargetReach(fighter.id, mid);
                        float minY = 0.5f;
                        float maxY = 1.35f;

                        if (fighter.isWeaponFighter && weaponRig != null && weaponRig.PrimaryInstance != null)
                        {
                            Bounds wb = BBWeaponRig.MeasureMeshBounds(weaponRig.PrimaryInstance);
                            Vector3 relCenter = prefabContents.transform.InverseTransformPoint(wb.center);
                            float weaponTip = relCenter.z > relCenter.x ? relCenter.z + wb.extents.z : relCenter.x + wb.extents.x;
                            if (weaponTip > 0.4f) forwardReach = Mathf.Max(forwardReach, weaponTip + 0.15f);
                            minY = Mathf.Max(0.1f, relCenter.y - wb.extents.y);
                            maxY = relCenter.y + wb.extents.y;
                        }
                        else if (anim != null && anim.isHuman)
                        {
                            HumanBodyBones bone = (mid == "2C" || mid == "jB") ? HumanBodyBones.RightFoot : HumanBodyBones.RightHand;
                            Transform strikeBone = anim.GetBoneTransform(bone) ?? anim.GetBoneTransform(HumanBodyBones.RightHand);
                            if (strikeBone != null)
                            {
                                Vector3 relBone = prefabContents.transform.InverseTransformPoint(strikeBone.position);
                                float boneForward = Mathf.Abs(relBone.z) > Mathf.Abs(relBone.x) ? Mathf.Abs(relBone.z) : Mathf.Abs(relBone.x);
                                forwardReach = Mathf.Max(forwardReach, boneForward + 0.18f); // 0.12m fist padding + 0.06m margin
                                minY = Mathf.Max(0.1f, relBone.y - 0.20f);
                                maxY = relBone.y + 0.25f;
                            }
                        }

                        // Projectile spawn point calculation
                        Vector2 projSpawn = Vector2.zero;
                        float projRadius = 0.22f;
                        if (fighter.hasProjectile && fighter.projectileMoveIds.Contains(mid))
                        {
                            Vector3 muzzlePos = Vector3.forward * 0.8f + Vector3.up * 1.1f;
                            if (weaponRig != null && weaponRig.PrimaryInstance != null)
                            {
                                muzzlePos = weaponRig.PrimaryInstance.transform.position;
                            }
                            else if (anim != null && anim.isHuman)
                            {
                                var hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
                                if (hand != null) muzzlePos = hand.position;
                            }
                            Vector3 relMuzzle = prefabContents.transform.InverseTransformPoint(muzzlePos);
                            projSpawn = new Vector2(Mathf.Max(0.6f, relMuzzle.x != 0f ? relMuzzle.x : relMuzzle.z), relMuzzle.y);
                            projectileSpawn = projSpawn;
                        }

                        // Compute clean 2D AABB
                        float halfWidth = forwardReach * 0.50f;
                        float centerX = forwardReach * 0.52f;
                        float centerY = (minY + maxY) * 0.5f;
                        float halfHeight = Mathf.Max(0.30f, (maxY - minY) * 0.5f);

                        // Ensure 2C low hits crouch plane
                        if (mid == "2C")
                        {
                            centerY = Mathf.Clamp(centerY, 0.25f, 0.55f);
                            halfHeight = Mathf.Max(halfHeight, 0.35f);
                        }

                        var moveConfig = new MoveHitboxConfig
                        {
                            moveId = mid,
                            startupTicks = startupTicks,
                            activeTicks = activeTicks,
                            recoveryTicks = recoveryTicks,
                            hitboxOffset = new Vector2(centerX, centerY),
                            hitboxSize = new Vector2(halfWidth * 2f, halfHeight * 2f),
                            projectileSpawnOffset = projSpawn,
                            projectileRadius = projRadius,
                            isDisjoint = fighter.isWeaponFighter || (fighter.hasProjectile && projSpawn != Vector2.zero),
                            useCustomOverride = false
                        };

                        if (existingIdx >= 0) pConfig.moveHitboxes[existingIdx] = moveConfig;
                        else pConfig.moveHitboxes.Add(moveConfig);

                        if (mid == "5A") baked5AReach = moveConfig.Reach;
                        else if (mid == "5C") baked5CReach = moveConfig.Reach;
                        else if (mid == "5D") baked5DReach = moveConfig.Reach;

                        // Also bake directly into BBAttackData.boxes if available
                        if (atk != null && atk.boxes != null)
                        {
                            BakeAttackDataBoxes(atk, moveConfig);
                            EditorUtility.SetDirty(atk.boxes);
                        }
                    }

                    EditorUtility.SetDirty(pConfig);
                    PrefabUtility.SaveAsPrefabAsset(prefabContents, fighter.prefabPath);
                    totalFightersBaked++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabContents);
                }

                string eventSummary = $"Hit: {hitEventsFound}, Shoot: {shootEventsFound}";
                string projSummary = fighter.hasProjectile ? $"({projectileSpawn.x:F2}m, {projectileSpawn.y:F2}m)" : "N/A";
                sbReport.AppendLine($"| {fighter.id} ({fighter.name}) | {fighter.warriorPack} | {colliderType} | {eventSummary} | {baked5AReach:F2}m | {baked5CReach:F2}m | {baked5DReach:F2}m | {standingHurtH:F2}m | {projSummary} | BAKED |");
            }

            string reportText = sbReport.ToString();
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string reportFile = Path.Combine(projectRoot, "Screenshots", "HITBOX_BAKER_REPORT.txt");
            File.WriteAllText(reportFile, reportText);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676><b>[Warrior Hitbox Baker] Successfully baked hitboxes & hurtboxes for all {totalFightersBaked} launch fighters!</b></color>");
            Debug.Log($"[Warrior Hitbox Baker] Report saved to: {reportFile}");
        }

        private static void BakeAttackDataBoxes(BBAttackData atk, MoveHitboxConfig cfg)
        {
            var track = atk.boxes;
            if (track == null) return;

            float hx = cfg.hitboxSize.x * 0.5f;
            float hy = cfg.hitboxSize.y * 0.5f;
            byte flags = cfg.isDisjoint ? BBBox.FLAG_DISJOINT : (byte)0;

            // Update existing hitboxes in place to preserve hurtbox layout and frame count
            if (track.boxes != null && track.boxes.Length > 0)
            {
                bool foundHit = false;
                for (int i = 0; i < track.boxes.Length; i++)
                {
                    if (track.boxes[i].kind == BBBoxKind.Hit)
                    {
                        track.boxes[i].cx = cfg.hitboxOffset.x;
                        track.boxes[i].cy = cfg.hitboxOffset.y;
                        track.boxes[i].hx = hx;
                        track.boxes[i].hy = hy;
                        if (cfg.isDisjoint)
                        {
                            track.boxes[i].flags |= flags;
                        }
                        foundHit = true;
                    }
                }
                if (foundHit) return;
            }

            // Fallback if no existing hitboxes were authored:
            int totalTicks = atk.TotalTicks;
            track.tickCount = totalTicks;
            track.frameStart = new int[totalTicks + 1];

            var boxList = new List<BBBox>();
            int currentOffset = 0;
            var hitBox = new BBBox(cfg.hitboxOffset.x, cfg.hitboxOffset.y, hx, hy, BBBoxKind.Hit, 0, BBAttribute.Body, flags);

            int startActive = atk.startupFrames;
            int endActive = atk.startupFrames + atk.activeFrames;

            for (int t = 0; t < totalTicks; t++)
            {
                track.frameStart[t] = currentOffset;
                if (t >= startActive && t < endActive)
                {
                    boxList.Add(hitBox);
                    currentOffset++;
                }
            }
            track.frameStart[totalTicks] = currentOffset;
            track.boxes = boxList.ToArray();
            track.keyTicks = new int[] { atk.startupFrames };
        }

        private static int GetDefaultStartup(string mid)
        {
            switch (mid)
            {
                case "5A": return 5;
                case "5B": return 8;
                case "5C": return 12;
                case "2C": return 11;
                case "jB": return 7;
                case "5D": return 14;
                case "236236C": return 10;
                default: return 6;
            }
        }

        private static int GetMinStartup(string mid)
        {
            switch (mid)
            {
                case "5A": return 4;
                case "5B": return 7;
                case "5C": return 10;
                case "2C": return 9;
                case "jB": return 5;
                case "5D": return 12;
                case "236236C": return 8;
                default: return 4;
            }
        }

        private static int GetMaxStartup(string mid)
        {
            switch (mid)
            {
                case "5A": return 6;
                case "5B": return 10;
                case "5C": return 16;
                case "2C": return 15;
                case "jB": return 10;
                case "5D": return 18;
                case "236236C": return 14;
                default: return 12;
            }
        }

        private static int GetDefaultActive(string mid)
        {
            if (mid == "236236C") return 6;
            if (mid == "5C" || mid == "2C" || mid == "5D") return 4;
            return 3;
        }

        private static int GetDefaultTotal(string mid)
        {
            switch (mid)
            {
                case "5A": return 18;
                case "5B": return 24;
                case "5C": return 32;
                case "2C": return 30;
                case "jB": return 22;
                case "5D": return 36;
                case "236236C": return 45;
                default: return 25;
            }
        }

        private static float GetTargetReach(string fighterId, string mid)
        {
            // Guaranteed playable fighting game reach
            float baseReach = 0.95f;
            if (mid == "5B") baseReach = 1.15f;
            else if (mid == "5C") baseReach = 1.55f;
            else if (mid == "2C") baseReach = 1.35f;
            else if (mid == "jB") baseReach = 1.10f;
            else if (mid == "5D") baseReach = 1.76f;
            else if (mid == "236236C") baseReach = 2.21f;

            if (BBSignatureTable.TryGet(fighterId, mid, out var sig))
            {
                baseReach *= sig.reachScale;
            }
            return baseReach;
        }
    }
}
#endif
