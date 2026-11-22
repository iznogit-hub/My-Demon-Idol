#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using DemonIdol.Combat;
using DemonIdol.Data;
using DemonIdol.BlazBlue;

namespace DemonIdol.BlazBlue.Editor
{
    public static class BBArchetypeFrameDataApplier
    {
        public struct AttackLevelData
        {
            public int hitstop;
            public int hitstun;
            public int blockstun;
            public float p2;
        }

        public static readonly Dictionary<int, AttackLevelData> LevelTable = new Dictionary<int, AttackLevelData>
        {
            { 1, new AttackLevelData { hitstop = 9,  hitstun = 12, blockstun = 11, p2 = 0.80f } },
            { 2, new AttackLevelData { hitstop = 10, hitstun = 14, blockstun = 13, p2 = 0.85f } },
            { 3, new AttackLevelData { hitstop = 11, hitstun = 17, blockstun = 16, p2 = 0.89f } },
            { 4, new AttackLevelData { hitstop = 12, hitstun = 19, blockstun = 18, p2 = 0.92f } },
            { 5, new AttackLevelData { hitstop = 13, hitstun = 21, blockstun = 20, p2 = 0.94f } },
        };

        public class MoveBaseSpec
        {
            public string moveId;
            public int startup;
            public int active;
            public int onBlock;
            public int damage;
            public int level;
            public BBGuard guard;
            public float baseReach;
            public string category; // "light", "medium", "heavy", "special", "super", "air", "throw"
            public bool causesKnockdown;
            public bool hardKnockdown;
            public bool isProjectile;
            public bool hasArmor;
            public int armorStart;
            public int armorEnd;
            public int landingRecovery;
            public float minDamagePct;
        }

        public class ArchetypeDefinition
        {
            public string name;
            public int hp;
            public Dictionary<string, MoveBaseSpec> moves = new Dictionary<string, MoveBaseSpec>();
        }

        public static readonly Dictionary<string, ArchetypeDefinition> Archetypes = BuildArchetypeDefinitions();

        private static Dictionary<string, ArchetypeDefinition> BuildArchetypeDefinitions()
        {
            var archs = new Dictionary<string, ArchetypeDefinition>();

            // 1. Rushdown (HP 10500)
            var rush = new ArchetypeDefinition { name = "Rushdown", hp = 10500 };
            AddMove(rush, "5A", 5, 3, 1, 280, 1, BBGuard.Mid, 0.80f, "light");
            AddMove(rush, "2A", 6, 3, 0, 260, 1, BBGuard.Mid, 0.75f, "light");
            AddMove(rush, "5B", 7, 4, -2, 520, 2, BBGuard.Mid, 1.05f, "medium");
            AddMove(rush, "2B", 8, 3, -3, 480, 2, BBGuard.Low, 1.10f, "medium");
            AddMove(rush, "5C", 11, 4, -4, 820, 3, BBGuard.Mid, 1.20f, "heavy");
            AddMove(rush, "2C", 11, 3, -10, 760, 3, BBGuard.Low, 1.30f, "heavy", causesKd: true);
            AddMove(rush, "5D", 13, 4, -4, 700, 3, BBGuard.Mid, 1.20f, "heavy");
            AddMove(rush, "236A", 12, 3, -6, 950, 3, BBGuard.Mid, 1.25f, "special");
            AddMove(rush, "214B", 15, 3, -3, 900, 4, BBGuard.Overhead, 1.20f, "special");
            AddMove(rush, "236236C", 5, 4, -20, 2400, 5, BBGuard.Mid, 1.30f, "super");
            AddAirMoves(rush, 5, 7, 11, 3, 4, 4, 280, 520, 820);
            AddThrow(rush, 0.95f);
            archs["Rushdown"] = rush;

            // 2. Zoner (HP 10000)
            var zone = new ArchetypeDefinition { name = "Zoner", hp = 10000 };
            AddMove(zone, "5A", 6, 3, 0, 260, 1, BBGuard.Mid, 0.80f, "light");
            AddMove(zone, "2A", 7, 2, -1, 250, 1, BBGuard.Mid, 0.75f, "light");
            AddMove(zone, "5B", 9, 4, -4, 560, 2, BBGuard.Mid, 1.05f, "medium");
            AddMove(zone, "2B", 10, 3, -5, 520, 2, BBGuard.Low, 1.10f, "medium");
            AddMove(zone, "5C", 13, 5, -6, 880, 3, BBGuard.Mid, 1.20f, "heavy");
            AddMove(zone, "2C", 13, 3, -12, 800, 3, BBGuard.Low, 1.30f, "heavy", causesKd: true);
            AddMove(zone, "5D", 14, 4, -6, 680, 3, BBGuard.Mid, 1.20f, "heavy");
            AddMove(zone, "236A", 14, 4, -2, 700, 3, BBGuard.Mid, 1.25f, "special", isProj: true);
            AddMove(zone, "214B", 18, 4, -8, 900, 4, BBGuard.Mid, 1.20f, "special");
            AddMove(zone, "236236C", 8, 5, -24, 2300, 5, BBGuard.Mid, 1.30f, "super");
            AddAirMoves(zone, 6, 9, 13, 3, 4, 5, 260, 560, 880);
            AddThrow(zone, 0.95f);
            archs["Zoner"] = zone;

            // 3. Grappler (HP 12000)
            var grap = new ArchetypeDefinition { name = "Grappler", hp = 12000 };
            AddMove(grap, "5A", 6, 3, -1, 320, 1, BBGuard.Mid, 0.80f, "light");
            AddMove(grap, "2A", 7, 3, -2, 300, 1, BBGuard.Mid, 0.75f, "light");
            AddMove(grap, "5B", 10, 4, -5, 680, 2, BBGuard.Mid, 1.05f, "medium");
            AddMove(grap, "2B", 11, 3, -6, 600, 2, BBGuard.Low, 1.10f, "medium");
            AddMove(grap, "5C", 15, 5, -8, 1100, 3, BBGuard.Mid, 1.20f, "heavy");
            AddMove(grap, "2C", 15, 4, -14, 950, 3, BBGuard.Low, 1.30f, "heavy", causesKd: true);
            AddMove(grap, "5D", 16, 4, -6, 850, 3, BBGuard.Mid, 1.20f, "heavy", hasArmor: true, armStart: 4, armEnd: 12);
            AddMove(grap, "236A", 14, 4, -8, 1150, 3, BBGuard.Mid, 1.25f, "special");
            AddMove(grap, "214B", 18, 3, -10, 1200, 4, BBGuard.Mid, 1.20f, "special");
            AddMove(grap, "236236C", 3, 3, -30, 2800, 5, BBGuard.Mid, 1.10f, "super", isThrowSuper: true);
            AddAirMoves(grap, 6, 10, 15, 3, 4, 5, 320, 680, 1100);
            AddThrow(grap, 1.10f); // connects <= 1.10m
            archs["Grappler"] = grap;

            // 4. Swordsman (HP 11000)
            var sword = new ArchetypeDefinition { name = "Swordsman", hp = 11000 };
            AddMove(sword, "5A", 6, 3, 0, 300, 1, BBGuard.Mid, 0.80f, "light");
            AddMove(sword, "2A", 7, 3, -1, 280, 1, BBGuard.Mid, 0.75f, "light");
            AddMove(sword, "5B", 9, 4, -3, 600, 2, BBGuard.Mid, 1.05f, "medium");
            AddMove(sword, "2B", 10, 3, -4, 560, 2, BBGuard.Low, 1.10f, "medium");
            AddMove(sword, "5C", 13, 5, -5, 950, 3, BBGuard.Mid, 1.20f, "heavy");
            AddMove(sword, "2C", 13, 3, -11, 860, 3, BBGuard.Low, 1.30f, "heavy", causesKd: true);
            AddMove(sword, "5D", 14, 4, -5, 780, 3, BBGuard.Mid, 1.20f, "heavy");
            AddMove(sword, "236A", 13, 4, -5, 1000, 3, BBGuard.Mid, 1.25f, "special");
            AddMove(sword, "214B", 16, 4, -4, 1050, 4, BBGuard.Overhead, 1.20f, "special");
            AddMove(sword, "236236C", 6, 5, -22, 2500, 5, BBGuard.Mid, 1.30f, "super");
            AddAirMoves(sword, 6, 9, 13, 3, 4, 5, 300, 600, 950);
            AddThrow(sword, 0.95f);
            archs["Swordsman"] = sword;

            // 5. All-rounder (HP 11000)
            var all = new ArchetypeDefinition { name = "All-rounder", hp = 11000 };
            AddMove(all, "5A", 5, 3, 0, 290, 1, BBGuard.Mid, 0.80f, "light");
            AddMove(all, "2A", 6, 3, 0, 270, 1, BBGuard.Mid, 0.75f, "light");
            AddMove(all, "5B", 8, 4, -3, 560, 2, BBGuard.Mid, 1.05f, "medium");
            AddMove(all, "2B", 9, 3, -4, 520, 2, BBGuard.Low, 1.10f, "medium");
            AddMove(all, "5C", 12, 4, -5, 880, 3, BBGuard.Mid, 1.20f, "heavy");
            AddMove(all, "2C", 12, 3, -10, 820, 3, BBGuard.Low, 1.30f, "heavy", causesKd: true);
            AddMove(all, "5D", 13, 4, -4, 740, 3, BBGuard.Mid, 1.20f, "heavy");
            AddMove(all, "236A", 13, 4, -4, 1000, 3, BBGuard.Mid, 1.25f, "special");
            AddMove(all, "214B", 15, 4, -3, 1050, 4, BBGuard.Mid, 1.20f, "special");
            AddMove(all, "236236C", 5, 4, -21, 2450, 5, BBGuard.Mid, 1.30f, "super");
            AddAirMoves(all, 5, 8, 12, 3, 4, 4, 290, 560, 880);
            AddThrow(all, 0.95f);
            archs["All-rounder"] = all;

            return archs;
        }

        private static void AddMove(ArchetypeDefinition arch, string id, int s, int a, int ob, int dmg, int lvl, BBGuard g, float reach, string cat, bool causesKd = false, bool isProj = false, bool hasArmor = false, int armStart = 0, int armEnd = 0, bool isThrowSuper = false)
        {
            arch.moves[id] = new MoveBaseSpec
            {
                moveId = id,
                startup = s,
                active = a,
                onBlock = ob,
                damage = dmg,
                level = lvl,
                guard = g,
                baseReach = reach,
                category = cat,
                causesKnockdown = causesKd,
                hardKnockdown = causesKd,
                isProjectile = isProj,
                hasArmor = hasArmor,
                armorStart = armStart,
                armorEnd = armEnd,
                landingRecovery = 0,
                minDamagePct = isThrowSuper ? 1.0f : 0.08f
            };
        }

        private static void AddAirMoves(ArchetypeDefinition arch, int sA, int sB, int sC, int aA, int aB, int aC, int dmgA, int dmgB, int dmgC)
        {
            arch.moves["j.A"] = new MoveBaseSpec
            {
                moveId = "j.A",
                startup = sA,
                active = aA,
                onBlock = 0,
                damage = dmgA,
                level = 1,
                guard = BBGuard.Overhead,
                baseReach = 0.80f,
                category = "air",
                landingRecovery = 3,
                minDamagePct = 0.08f
            };
            arch.moves["j.B"] = new MoveBaseSpec
            {
                moveId = "j.B",
                startup = sB,
                active = aB,
                onBlock = 0,
                damage = dmgB,
                level = 2,
                guard = BBGuard.Overhead,
                baseReach = 1.05f,
                category = "air",
                landingRecovery = 3,
                minDamagePct = 0.08f
            };
            arch.moves["j.C"] = new MoveBaseSpec
            {
                moveId = "j.C",
                startup = sC,
                active = aC,
                onBlock = 0,
                damage = dmgC,
                level = 3,
                guard = BBGuard.Overhead,
                baseReach = 1.20f,
                category = "air",
                landingRecovery = 3,
                minDamagePct = 0.08f
            };
        }

        private static void AddThrow(ArchetypeDefinition arch, float reach)
        {
            arch.moves["BC"] = new MoveBaseSpec
            {
                moveId = "BC",
                startup = 7,
                active = 3,
                onBlock = 0,
                damage = 1300,
                level = 1,
                guard = BBGuard.Mid,
                baseReach = reach,
                category = "throw",
                causesKnockdown = true,
                hardKnockdown = true,
                minDamagePct = 1.0f
            };
        }

        public struct CharacterRosterEntry
        {
            public string charId;
            public string archetype;
            public int variant;
            public bool isWeapon;
        }

        public static readonly CharacterRosterEntry[] Roster = new CharacterRosterEntry[]
        {
            // Rushdown: C01v0 C11v1 C13v2 EX03v3 C04v4 C10v5
            new CharacterRosterEntry { charId = "C01",  archetype = "Rushdown", variant = 0, isWeapon = false },
            new CharacterRosterEntry { charId = "C11",  archetype = "Rushdown", variant = 1, isWeapon = false },
            new CharacterRosterEntry { charId = "C13",  archetype = "Rushdown", variant = 2, isWeapon = false },
            new CharacterRosterEntry { charId = "EX03", archetype = "Rushdown", variant = 3, isWeapon = false },
            new CharacterRosterEntry { charId = "C04",  archetype = "Rushdown", variant = 4, isWeapon = false },
            new CharacterRosterEntry { charId = "C10",  archetype = "Rushdown", variant = 5, isWeapon = false },

            // Zoner: C02v0 C07v1 C09v2 SK05v3 EX05v4 EX07v5 C06v6
            new CharacterRosterEntry { charId = "C02",  archetype = "Zoner", variant = 0, isWeapon = false },
            new CharacterRosterEntry { charId = "C07",  archetype = "Zoner", variant = 1, isWeapon = true  },
            new CharacterRosterEntry { charId = "C09",  archetype = "Zoner", variant = 2, isWeapon = false },
            new CharacterRosterEntry { charId = "SK05", archetype = "Zoner", variant = 3, isWeapon = true  },
            new CharacterRosterEntry { charId = "EX05", archetype = "Zoner", variant = 4, isWeapon = false },
            new CharacterRosterEntry { charId = "EX07", archetype = "Zoner", variant = 5, isWeapon = false },
            new CharacterRosterEntry { charId = "C06",  archetype = "Zoner", variant = 6, isWeapon = true  },

            // Grappler: SK08v0 EX02v1 SK03v2 EX04v3
            new CharacterRosterEntry { charId = "SK08", archetype = "Grappler", variant = 0, isWeapon = false },
            new CharacterRosterEntry { charId = "EX02", archetype = "Grappler", variant = 1, isWeapon = false },
            new CharacterRosterEntry { charId = "SK03", archetype = "Grappler", variant = 2, isWeapon = false },
            new CharacterRosterEntry { charId = "EX04", archetype = "Grappler", variant = 3, isWeapon = false },

            // Swordsman: SK02v0 SK06v1 EX01v2 EX06v3 C05v4 C14v5
            new CharacterRosterEntry { charId = "SK02", archetype = "Swordsman", variant = 0, isWeapon = true },
            new CharacterRosterEntry { charId = "SK06", archetype = "Swordsman", variant = 1, isWeapon = true },
            new CharacterRosterEntry { charId = "EX01", archetype = "Swordsman", variant = 2, isWeapon = true },
            new CharacterRosterEntry { charId = "EX06", archetype = "Swordsman", variant = 3, isWeapon = true },
            new CharacterRosterEntry { charId = "C05",  archetype = "Swordsman", variant = 4, isWeapon = true },
            new CharacterRosterEntry { charId = "C14",  archetype = "Swordsman", variant = 5, isWeapon = true },

            // All-rounder: C12v0 C08v1
            new CharacterRosterEntry { charId = "C12",  archetype = "All-rounder", variant = 0, isWeapon = false },
            new CharacterRosterEntry { charId = "C08",  archetype = "All-rounder", variant = 1, isWeapon = false }
        };

        [MenuItem("Tools/MDI/Apply Archetype Frame Data")]
        public static bool ApplyAll()
        {
            Debug.Log("<color=#00e676>[BBArchetypeFrameDataApplier] ========================================================</color>");
            Debug.Log("<color=#00e676>[BBArchetypeFrameDataApplier] STARTING APPLY ARCHETYPE FRAME DATA ACROSS 25 FIGHTERS</color>");
            Debug.Log("<color=#00e676>[BBArchetypeFrameDataApplier] ========================================================</color>");

            // Load ClipImpact.asset if available for peak limb extension frames
            var impactAsset = AssetDatabase.LoadAssetAtPath<BBClipImpactAsset>("Assets/CombatData/BlazBlue/ClipImpact.asset");
            if (impactAsset == null)
            {
                BBImpactFrameMeasurer.MeasureAllAttackClips();
                impactAsset = AssetDatabase.LoadAssetAtPath<BBClipImpactAsset>("Assets/CombatData/BlazBlue/ClipImpact.asset");
            }

            int fightersUpdated = 0;
            int movesUpdated = 0;
            int boxTracksUpdated = 0;

            for (int i = 0; i < Roster.Length; i++)
            {
                var entry = Roster[i];
                if (!Archetypes.TryGetValue(entry.archetype, out var archDef))
                {
                    Debug.LogError($"[BBArchetypeFrameDataApplier] Unknown archetype '{entry.archetype}' for {entry.charId}");
                    continue;
                }

                // 1. Locate CharacterDefinition and Profile
                string[] defGuids = AssetDatabase.FindAssets($"CharacterDefinition_{entry.charId}_ t:CharacterDefinition", new[] { "Assets/CombatData/Characters" });
                if (defGuids.Length == 0)
                {
                    defGuids = AssetDatabase.FindAssets($"CharacterDefinition_{entry.charId} t:CharacterDefinition", new[] { "Assets/CombatData/Characters" });
                }

                CharacterDefinition charDef = null;
                if (defGuids.Length > 0)
                {
                    charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(AssetDatabase.GUIDToAssetPath(defGuids[0]));
                }

                string[] profGuids = AssetDatabase.FindAssets($"BBProfile_{entry.charId} t:BBCharacterProfile", new[] { "Assets/CombatData/BlazBlue" });
                BBCharacterProfile profile = null;
                if (profGuids.Length > 0)
                {
                    profile = AssetDatabase.LoadAssetAtPath<BBCharacterProfile>(AssetDatabase.GUIDToAssetPath(profGuids[0]));
                }
                else if (charDef != null && charDef.blazBlueProfile is BBCharacterProfile cp)
                {
                    profile = cp;
                }

                if (profile == null)
                {
                    Debug.LogError($"[BBArchetypeFrameDataApplier] Could not find BBCharacterProfile for {entry.charId}");
                    continue;
                }

                // 2. Set HP on Profile
                profile.maxHealth = archDef.hp;
                EditorUtility.SetDirty(profile);

                // 3. Measure Model Height
                float modelHeight = 1.70f;
                if (charDef != null && charDef.modelPrefab != null)
                {
                    modelHeight = MeasureModelHeight(charDef.modelPrefab);
                }
                float heightScale = modelHeight / 1.70f;

                // 4. Locate BBMoveSet
                BBMoveSet moveSet = profile.moveSet as BBMoveSet;
                if (moveSet == null)
                {
                    string[] msGuids = AssetDatabase.FindAssets($"BBMoveSet_{entry.charId} t:BBMoveSet", new[] { "Assets/CombatData/BlazBlue" });
                    if (msGuids.Length > 0)
                    {
                        moveSet = AssetDatabase.LoadAssetAtPath<BBMoveSet>(AssetDatabase.GUIDToAssetPath(msGuids[0]));
                        profile.moveSet = moveSet;
                        EditorUtility.SetDirty(profile);
                    }
                }

                if (moveSet == null || moveSet.moves == null || moveSet.moves.Length == 0)
                {
                    Debug.LogError($"[BBArchetypeFrameDataApplier] No BBMoveSet or empty moves for {entry.charId}");
                    continue;
                }

                fightersUpdated++;

                // 5. Update each of the 14 moves in the moveSet
                // Core 14 moveIds
                string[] targetMoveIds = new string[]
                {
                    "5A", "2A", "5B", "2B", "5C", "2C", "5D", "236A", "214B", "236236C", "j.A", "j.B", "j.C", "BC"
                };

                for (int m = 0; m < targetMoveIds.Length; m++)
                {
                    string mid = targetMoveIds[m];
                    if (!archDef.moves.TryGetValue(mid, out var baseSpec))
                    {
                        Debug.LogError($"[BBArchetypeFrameDataApplier] Archetype {entry.archetype} missing spec for {mid}");
                        continue;
                    }

                    // Find attack data
                    BBAttackData attack = moveSet.Find(mid);
                    if (attack == null && mid == "BC")
                    {
                        attack = moveSet.Find("4BC") ?? moveSet.Find("6BC") ?? moveSet.throwForward ?? moveSet.throwBack;
                    }
                    if (attack == null)
                    {
                        // Search in moves array with partial matches
                        for (int k = 0; k < moveSet.moves.Length; k++)
                        {
                            var cand = moveSet.moves[k];
                            if (cand != null && (cand.moveId == mid || cand.name.EndsWith("_" + mid)))
                            {
                                attack = cand;
                                break;
                            }
                        }
                    }

                    if (attack == null)
                    {
                        Debug.LogWarning($"[BBArchetypeFrameDataApplier] Fighter {entry.charId} missing move {mid}");
                        continue;
                    }

                    // Apply variant modifications
                    int s = baseSpec.startup;
                    int a = baseSpec.active;
                    int ob = baseSpec.onBlock;
                    float dmg = baseSpec.damage;
                    float reachMod = 0f;

                    // Variants:
                    // v0 base
                    // v1 5A+1 dmg x1.04 reach+0.05
                    // v2 5B-1 2C+1 x0.96
                    // v3 5C-1 2A+1 x1.02
                    // v4 2B-1 236A+1 x0.98 reach+0.10
                    // v5 5B+1 5C+1 x1.06
                    // v6 2A-1 236A-2 x0.97
                    switch (entry.variant)
                    {
                        case 1:
                            if (mid == "5A") s += 1;
                            dmg *= 1.04f;
                            reachMod += 0.05f;
                            break;
                        case 2:
                            if (mid == "5B") s -= 1;
                            if (mid == "2C") s += 1;
                            dmg *= 0.96f;
                            break;
                        case 3:
                            if (mid == "5C") s -= 1;
                            if (mid == "2A") s += 1;
                            dmg *= 1.02f;
                            break;
                        case 4:
                            if (mid == "2B") s -= 1;
                            if (mid == "236A") s += 1;
                            dmg *= 0.98f;
                            reachMod += 0.10f;
                            break;
                        case 5:
                            if (mid == "5B") s += 1;
                            if (mid == "5C") s += 1;
                            dmg *= 1.06f;
                            break;
                        case 6:
                            if (mid == "2A") s -= 1;
                            if (mid == "236A") s -= 2;
                            dmg *= 0.97f;
                            break;
                    }

                    // Clamp startup ranges:
                    // lights: 5-7, mediums: 7-11, heavies: 11-16, specials: 12-18, super: 1-8
                    switch (baseSpec.category)
                    {
                        case "light":
                            s = Mathf.Clamp(s, 5, 7);
                            break;
                        case "medium":
                            s = Mathf.Clamp(s, 7, 11);
                            break;
                        case "heavy":
                            s = Mathf.Clamp(s, 11, 16);
                            break;
                        case "special":
                            s = Mathf.Clamp(s, 12, 18);
                            break;
                        case "super":
                            s = Mathf.Clamp(s, 1, 8);
                            break;
                        case "air":
                            // Air normals use the archetype's 5A/5B/5C startup
                            if (mid == "j.A") s = Mathf.Clamp(s, 5, 7);
                            else if (mid == "j.B") s = Mathf.Clamp(s, 7, 11);
                            else if (mid == "j.C") s = Mathf.Clamp(s, 11, 16);
                            break;
                        case "throw":
                            s = 7;
                            a = 3;
                            break;
                    }

                    // Attack level source numbers
                    int lvl = baseSpec.level;
                    var lvlData = LevelTable[lvl];

                    // Computed recovery: recovery = blockstun - (active - 1) - onBlock
                    int rec;
                    if (baseSpec.category == "air")
                    {
                        if (mid == "j.A") rec = 10;
                        else if (mid == "j.B") rec = 14;
                        else rec = 18;
                    }
                    else if (baseSpec.category == "throw")
                    {
                        rec = 23;
                    }
                    else
                    {
                        rec = lvlData.blockstun - (a - 1) - ob;
                    }

                    // Apply to attack asset
                    attack.startupFrames = s;
                    attack.activeFrames = a;
                    attack.recoveryFrames = rec;
                    attack.damage = Mathf.RoundToInt(dmg);
                    attack.attackLevel = lvl;
                    attack.hitstopAttacker = lvlData.hitstop;
                    attack.hitstopDefender = lvlData.hitstop;
                    attack.hitstopOnBlock = lvlData.hitstop;
                    attack.hitstunTicks = lvlData.hitstun;
                    attack.blockstunTicks = lvlData.blockstun;
                    attack.initialProration = lvlData.p2;
                    attack.comboDamageDecay = lvlData.p2;
                    attack.advantageOnBlock = ob;
                    attack.advantageOnHit = lvlData.hitstun - ((a - 1) + rec);
                    attack.guard = baseSpec.guard;
                    attack.landingRecovery = baseSpec.landingRecovery;
                    attack.minDamagePct = baseSpec.minDamagePct;
                    attack.causesKnockdown = baseSpec.causesKnockdown;
                    attack.hardKnockdown = baseSpec.hardKnockdown;
                    attack.hasArmor = baseSpec.hasArmor;
                    attack.crushStartTick = baseSpec.armorStart;
                    attack.crushEndTick = baseSpec.armorEnd;

                    if (baseSpec.isProjectile)
                    {
                        attack.attribute = BBAttribute.Projectile;
                    }

                    if (baseSpec.category == "super")
                    {
                        attack.heatCost = 50;
                        attack.rapidCancelable = false;
                        attack.ddCancel = false;
                        attack.invuln = new BBInvulnWindow[]
                        {
                            new BBInvulnWindow(0, s, BBInvuln.Strike | BBInvuln.Throw | BBInvuln.Projectile)
                        };
                    }

                    // Impact frame for animation sync: measure peak extension frame from clip first
                    AnimationClip clip = attack.clip != null ? attack.clip : attack.animationClip;
                    int measuredImp = -1;
                    if (clip != null && charDef != null && charDef.modelPrefab != null)
                    {
                        measuredImp = MeasureClipPeakFrame(charDef.modelPrefab, clip, attack.clipFirstFrame, attack.clipLastFrame);
                    }
                    if (measuredImp <= 0 && clip != null && impactAsset != null && impactAsset.TryGetImpact(clip.name, out var imp))
                    {
                        measuredImp = imp.impactFrame60;
                    }
                    if (measuredImp <= 0 && clip != null)
                    {
                        int totalFrames = Mathf.Max(1, Mathf.RoundToInt(clip.length * 60.0f));
                        measuredImp = Mathf.Clamp(Mathf.RoundToInt(totalFrames * 0.40f), 1, totalFrames - 1);
                    }
                    attack.measuredImpactFrame = measuredImp;

                    // 6. Autor / Rebake BBBoxTrack
                    float weaponReachBonus = entry.isWeapon ? 0.30f : 0f;
                    float farEdge = ((baseSpec.baseReach + weaponReachBonus) * heightScale) + reachMod;

                    // Height & Y bounds
                    float cy, hy;
                    if (mid == "2A" || mid == "2B" || mid == "2C")
                    {
                        // Crouch .20-.80
                        cy = 0.50f;
                        hy = 0.30f;
                    }
                    else if (mid == "214B")
                    {
                        // Overhead .90-1.80
                        cy = 1.35f;
                        hy = 0.45f;
                    }
                    else if (mid.StartsWith("j."))
                    {
                        // Air .40-1.10
                        cy = 0.75f;
                        hy = 0.35f;
                    }
                    else if (baseSpec.category == "throw")
                    {
                        cy = 0.95f;
                        hy = 0.40f;
                    }
                    else
                    {
                        // Stand .70-1.40
                        cy = 1.05f;
                        hy = 0.35f;
                    }

                    // Depth & X bounds (no whiff inside reach)
                    float depth = 0.40f;
                    float minX = Mathf.Min(farEdge - depth, 0.25f * heightScale);
                    float hx = (farEdge - minX) * 0.5f;
                    float cx = minX + hx;

                    BBBoxKind boxKind = (baseSpec.category == "throw") ? BBBoxKind.Throw : BBBoxKind.Hit;
                    BBBox activeBox = new BBBox(cx, cy, hx, hy, boxKind, 0, attack.attribute);

                    int totalTicks = s + a + rec;
                    if (attack.boxes == null)
                    {
                        attack.boxes = ScriptableObject.CreateInstance<BBBoxTrack>();
                        string boxPath = $"Assets/CombatData/BlazBlue/{entry.charId}/BoxTracks/BoxTrack_{entry.charId}_{mid}.asset";
                        string boxDir = Path.GetDirectoryName(boxPath);
                        if (!Directory.Exists(boxDir)) Directory.CreateDirectory(boxDir);
                        AssetDatabase.CreateAsset(attack.boxes, boxPath);
                    }

                    BBBoxTrack track = attack.boxes;
                    track.tickCount = totalTicks;
                    var boxList = new List<BBBox>();
                    track.frameStart = new int[totalTicks + 1];
                    int currentBoxOffset = 0;

                    for (int t = 0; t < totalTicks; t++)
                    {
                        track.frameStart[t] = currentBoxOffset;
                        if (t >= s && t < s + a)
                        {
                            boxList.Add(activeBox);
                            currentBoxOffset++;
                        }
                    }
                    track.frameStart[totalTicks] = currentBoxOffset;
                    track.boxes = boxList.ToArray();
                    track.keyTicks = new int[] { s };
                    track.bakeHash = $"Bake_{entry.charId}_{mid}_{s}_{a}_{rec}";
                    EditorUtility.SetDirty(track);
                    boxTracksUpdated++;

                    EditorUtility.SetDirty(attack);
                    movesUpdated++;
                }

                // Also update any throw variants (4BC, 6BC, j.BC) if present
                UpdateThrowVariants(moveSet, entry, heightScale);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"<color=#00e676>[BBArchetypeFrameDataApplier] SUCCESS: Updated {fightersUpdated} fighters, {movesUpdated} moves, {boxTracksUpdated} box tracks.</color>");
            return true;
        }

        private static void UpdateThrowVariants(BBMoveSet moveSet, CharacterRosterEntry entry, float heightScale)
        {
            string[] throwIds = new string[] { "4BC", "6BC", "j.BC" };
            float baseReach = (entry.archetype == "Grappler") ? 1.10f : 0.95f;
            float farEdge = baseReach * heightScale;
            float depth = 0.40f;
            float minX = Mathf.Min(farEdge - depth, 0.25f * heightScale);
            float hx = (farEdge - minX) * 0.5f;
            float cx = minX + hx;

            for (int i = 0; i < throwIds.Length; i++)
            {
                var atk = moveSet.Find(throwIds[i]);
                if (atk == null) continue;

                atk.startupFrames = 7;
                atk.activeFrames = 3;
                atk.recoveryFrames = 23;
                atk.damage = 1300;
                atk.minDamagePct = 1.0f;
                atk.attackLevel = 1;
                atk.hitstopAttacker = 9;
                atk.hitstopDefender = 9;
                atk.hitstopOnBlock = 9;
                atk.hitstunTicks = 12;
                atk.blockstunTicks = 11;
                atk.causesKnockdown = true;
                atk.hardKnockdown = true;
                atk.guard = BBGuard.Mid;

                if (atk.boxes != null)
                {
                    var track = atk.boxes;
                    int totalTicks = 7 + 3 + 23;
                    track.tickCount = totalTicks;
                    var throwBox = new BBBox(cx, 0.95f, hx, 0.40f, BBBoxKind.Throw, 0, atk.attribute);
                    var boxList = new List<BBBox>();
                    track.frameStart = new int[totalTicks + 1];
                    int currentBoxOffset = 0;

                    for (int t = 0; t < totalTicks; t++)
                    {
                        track.frameStart[t] = currentBoxOffset;
                        if (t >= 7 && t < 10)
                        {
                            boxList.Add(throwBox);
                            currentBoxOffset++;
                        }
                    }
                    track.frameStart[totalTicks] = currentBoxOffset;
                    track.boxes = boxList.ToArray();
                    track.keyTicks = new int[] { 7 };
                    EditorUtility.SetDirty(track);
                }
                EditorUtility.SetDirty(atk);
            }
        }

        private static float MeasureModelHeight(GameObject prefab)
        {
            if (prefab == null) return 1.70f;
            GameObject dummy = UnityEngine.Object.Instantiate(prefab);
            dummy.hideFlags = HideFlags.HideAndDontSave;
            float h = 1.70f;
            try
            {
                var anim = dummy.GetComponentInChildren<Animator>();
                if (anim != null && anim.isHuman)
                {
                    var head = anim.GetBoneTransform(HumanBodyBones.Head);
                    if (head != null)
                    {
                        h = head.position.y - dummy.transform.position.y + 0.15f;
                    }
                }
                if (h < 1.0f || h > 2.5f)
                {
                    var renderers = dummy.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        Bounds b = renderers[0].bounds;
                        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                        h = b.size.y;
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(dummy);
            }
            return Mathf.Clamp(h, 1.40f, 2.20f);
        }

        private static int MeasureClipPeakFrame(GameObject prefab, AnimationClip clip, int firstFrame, int lastFrame)
        {
            if (prefab == null || clip == null) return -1;
            const float EngineFps = 60.0f;
            float t0 = (firstFrame > 0) ? (firstFrame / EngineFps) : 0f;
            float tLast = (lastFrame > firstFrame && lastFrame > 0) ? (lastFrame / EngineFps) : clip.length;
            tLast = Mathf.Min(tLast, clip.length);

            int startTick = Mathf.RoundToInt(t0 * EngineFps);
            int endTick = Mathf.RoundToInt(tLast * EngineFps);
            if (endTick <= startTick) return startTick;

            GameObject dummy = UnityEngine.Object.Instantiate(prefab);
            dummy.hideFlags = HideFlags.HideAndDontSave;
            int bestTick = -1;

            try
            {
                var anim = dummy.GetComponentInChildren<Animator>();
                if (anim == null || !anim.isHuman) return -1;

                Transform rHand = anim.GetBoneTransform(HumanBodyBones.RightHand);
                Transform lHand = anim.GetBoneTransform(HumanBodyBones.LeftHand);
                Transform rFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);
                Transform lFoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
                Transform[] bones = new Transform[] { rHand, lHand, rFoot, lFoot };

                // 1. Measure baseline at t0
                clip.SampleAnimation(dummy, t0);
                float[] baseline = new float[bones.Length];
                for (int b = 0; b < bones.Length; b++)
                {
                    baseline[b] = (bones[b] != null) ? dummy.transform.InverseTransformPoint(bones[b].position).z : 0f;
                }

                // 2. Track maximum forward displacement from baseline across all 60fps ticks
                float maxTravel = 0.04f; // Must travel forward at least 4cm

                for (int tick = startTick; tick <= endTick; tick++)
                {
                    float time = Mathf.Clamp(tick / EngineFps, t0, tLast);
                    clip.SampleAnimation(dummy, time);

                    for (int b = 0; b < bones.Length; b++)
                    {
                        if (bones[b] == null) continue;
                        float z = dummy.transform.InverseTransformPoint(bones[b].position).z;
                        float travel = z - baseline[b];
                        if (travel > maxTravel)
                        {
                            maxTravel = travel;
                            bestTick = tick;
                        }
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(dummy);
            }

            return bestTick;
        }
    }
}
#endif