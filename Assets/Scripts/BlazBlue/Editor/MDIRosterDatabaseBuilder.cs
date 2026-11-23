using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DemonIdol.Combat;
using DemonIdol.Data;
using DemonIdol.BlazBlue;

namespace DemonIdol.BlazBlue.Editor
{
    public static class MDIRosterDatabaseBuilder
    {
        public static readonly string[] Full25Roster = new string[]
        {
            "C01", "C02", "C04", "C05", "C06", "C07", "C08", "C09", "C10", "C11", "C12", "C13", "C14",
            "SK02", "SK03", "SK05", "SK06", "SK08",
            "EX01", "EX02", "EX03", "EX04", "EX05", "EX06", "EX07"
        };

        [MenuItem("Tools/MDI/Build Roster Database")]
        public static MDIRosterDatabase BuildDatabase()
        {
            string resourcesDir = "Assets/Resources";
            if (!Directory.Exists(resourcesDir))
            {
                Directory.CreateDirectory(resourcesDir);
            }

            string assetPath = "Assets/Resources/MDI_RosterDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<MDIRosterDatabase>(assetPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<MDIRosterDatabase>();
                AssetDatabase.CreateAsset(db, assetPath);
            }

            var charList = new List<CharacterDefinition>();
            for (int i = 0; i < Full25Roster.Length; i++)
            {
                string id = Full25Roster[i];
                string[] found = AssetDatabase.FindAssets($"CharacterDefinition_{id}_ t:CharacterDefinition", new[] { "Assets/CombatData/Characters" });
                if (found.Length == 0)
                {
                    found = AssetDatabase.FindAssets($"CharacterDefinition_{id} t:CharacterDefinition", new[] { "Assets/CombatData/Characters" });
                }

                if (found.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(found[0]);
                    var def = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
                    if (def != null)
                    {
                        // Ensure avatar is set from prefab if missing
                        if (def.avatar == null && def.modelPrefab != null)
                        {
                            var anim = def.modelPrefab.GetComponentInChildren<Animator>();
                            if (anim != null && anim.avatar != null)
                            {
                                def.avatar = anim.avatar;
                                EditorUtility.SetDirty(def);
                            }
                        }

                        // Ensure controller is set
                        if (def.animatorController == null)
                        {
                            def.animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/MDI_Characters/Runtime/MDI_FighterTemplate.controller");
                            EditorUtility.SetDirty(def);
                        }
                        charList.Add(def);
                        string prefabName = def.modelPrefab != null ? def.modelPrefab.name : "null";
                        string profileName = def.blazBlueProfile != null ? def.blazBlueProfile.name : "null";
                        string avatarName = def.avatar != null ? def.avatar.name : "null";
                        Debug.Log($"[MDIRosterDatabaseBuilder] Bound {id} -> {def.name} (Prefab: {prefabName}, Profile: {profileName}, Avatar: {avatarName})");
                    }
                }
                else
                {
                    Debug.LogError($"[MDIRosterDatabaseBuilder] Could not find CharacterDefinition for {id}!");
                }
            }

            db.characters = charList.ToArray();

            // ST07 Stage
            var st07 = AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/CombatData/Stages/ST07_ST07_CollegeRaimon.asset");
            db.st07Stage = st07;

            // Additional authored stages
            var addStages = new List<StageDefinition>();
            string[] stageGuids = AssetDatabase.FindAssets("t:StageDefinition", new[] { "Assets/CombatData/Stages" });
            for (int i = 0; i < stageGuids.Length; i++)
            {
                var s = AssetDatabase.LoadAssetAtPath<StageDefinition>(AssetDatabase.GUIDToAssetPath(stageGuids[i]));
                if (s != null && s != st07)
                {
                    addStages.Add(s);
                }
            }
            db.additionalStages = addStages.ToArray();

            // Global default controller
            db.defaultAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/MDI_Characters/Runtime/MDI_FighterTemplate.controller");

            // Sparks
            db.hitSparkLightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Hit A (Red).prefab");
            db.hitSparkHeavyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Hit D 3D (Yellow).prefab");
            db.hitSparkSuperPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Sword Trails/Fire/CFXR4 Sword Hit FIRE (Cross).prefab");
            db.guardSparkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR Impact Glowing HDR (Blue).prefab");

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676>[MDIRosterDatabaseBuilder] Successfully built MDI_RosterDatabase with {db.characters.Length} characters and ST07 stage at {assetPath}</color>");
            return db;
        }
    }
}
