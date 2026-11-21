#if MDI_ARCHIVE
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Master Prop & Weapon Stripper.
    /// Completely removes all weapon props, staffs, guns, blasters, swords, katanas, and binders
    /// from all character prefabs and active scenes, ensuring characters are 100% pure unarmed anime fighters.
    /// </summary>
    public static class DemonIdolCharacterPropSanitizer
    {
        private const string CHAR_PREFABS_DIR = "Assets/Prefabs/Characters";

        public static void StripAllPropsAndWeaponsFromAllCharacters()
        {
            Debug.Log("<color=#00e676>[Prop Sanitizer] Starting complete removal of all props, weapons, staffs, and guns...</color>");

            string[] prefabFiles = Directory.Exists(CHAR_PREFABS_DIR)
                ? Directory.GetFiles(CHAR_PREFABS_DIR, "*.prefab", SearchOption.AllDirectories)
                : Array.Empty<string>();

            int cleanedPrefabs = 0;
            int removedPropsCount = 0;

            foreach (string path in prefabFiles)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                if (root == null) continue;

                bool modified = false;

                // 1. Remove FighterPropBinder components
                var binders = root.GetComponentsInChildren<FighterPropBinder>(true);
                foreach (var b in binders)
                {
                    UnityEngine.Object.DestroyImmediate(b, true);
                    modified = true;
                }

                // 2. Remove FighterWeaponProp components & GameObjects
                var weaponProps = root.GetComponentsInChildren<FighterWeaponProp>(true);
                foreach (var wp in weaponProps)
                {
                    if (wp != null && wp.gameObject != null)
                    {
                        UnityEngine.Object.DestroyImmediate(wp.gameObject, true);
                        removedPropsCount++;
                        modified = true;
                    }
                }

                // 3. Remove all child objects matching weapon/prop names
                var allTransforms = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in allTransforms)
                {
                    if (t == null || t == root.transform) continue;
                    string name = t.name;

                    if (name.StartsWith("Prop_") ||
                        name.Contains("Staff") ||
                        name.Contains("Blaster") ||
                        name.Contains("Katana") ||
                        name.Contains("Sword") ||
                        name.Contains("Dagger") ||
                        name.Contains("Weapon") ||
                        name.Contains("CrescentRing") ||
                        name.Contains("StaffOrb") ||
                        name.Contains("Crossguard") ||
                        name.Contains("BladeTip") ||
                        name.Contains("MuzzlePoint") ||
                        name.Contains("MagicWand") ||
                        name.Contains("Wand") ||
                        name.Contains("Gun") ||
                        name.Contains("Orb"))
                    {
                        UnityEngine.Object.DestroyImmediate(t.gameObject, true);
                        removedPropsCount++;
                        modified = true;
                    }
                }

                if (modified)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    cleanedPrefabs++;
                }

                PrefabUtility.UnloadPrefabContents(root);
            }

            // 4. Also clean up any active scene objects if scenes are open
            CleanSceneProps();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00e676><b>[Prop Sanitizer] SUCCESS:</b> Completely stripped all weapons! Cleaned {cleanedPrefabs} character prefabs and removed {removedPropsCount} props. Characters are now 100% pure unarmed anime fighters.</color>");
        }

        private static void CleanSceneProps()
        {
            var allSceneObjects = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in allSceneObjects)
            {
                if (t == null) continue;
                string name = t.name;
                if (name.StartsWith("Prop_") || name == "StaffOrb" || name == "CrescentRing" || name == "KatanaBlade" || name == "Staff")
                {
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
                }
            }

            var binders = UnityEngine.Object.FindObjectsByType<FighterPropBinder>(FindObjectsSortMode.None);
            foreach (var b in binders)
            {
                UnityEngine.Object.DestroyImmediate(b);
            }
        }
    }
}
#endif

#endif
