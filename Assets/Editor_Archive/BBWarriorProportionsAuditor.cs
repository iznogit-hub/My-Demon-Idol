#if MDI_ARCHIVE
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using DemonIdol.BlazBlue.Visual;

namespace DemonIdol.BlazBlue.Editor
{
    public static class BBWarriorProportionsAuditor
    {
        [MenuItem("Demon Idol/BlazBlue/Audit Weapon and Character Proportions")]
        public static void AuditProportions()
        {
            Debug.Log("================================================================");
            Debug.Log("[BBWarriorProportionsAuditor] AUDITING WEAPON & CHARACTER PROPORTIONS");
            Debug.Log("================================================================");

            // 1. Audit Weapon Prefabs in Assets/Prefabs/Weapons/Warrior/
            string[] weaponPrefabs = Directory.GetFiles("Assets/Prefabs/Weapons/Warrior", "*.prefab");
            foreach (var wp in weaponPrefabs)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(wp);
                if (go == null) continue;

                var mf = go.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    Bounds b = mf.sharedMesh.bounds;
                    Debug.Log($"[WEAPON PREFAB] {go.name} | Mesh: {mf.sharedMesh.name} | Bounds Size: ({b.size.x:F3}, {b.size.y:F3}, {b.size.z:F3}) | Extents: ({b.extents.x:F3}, {b.extents.y:F3}, {b.extents.z:F3})");
                }
                else
                {
                    var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (smr != null && smr.sharedMesh != null)
                    {
                        Bounds b = smr.sharedMesh.bounds;
                        Debug.Log($"[WEAPON PREFAB (SMR)] {go.name} | Mesh: {smr.sharedMesh.name} | Bounds Size: ({b.size.x:F3}, {b.size.y:F3}, {b.size.z:F3})");
                    }
                }
            }

            // 2. Audit Character Prefabs
            string[] charPrefabs = {
                "Assets/Prefabs/Characters/C01_Akio.prefab",
                "Assets/Prefabs/Characters/C02_Aika.prefab",
                "Assets/Prefabs/Characters/C04_UnityChan.prefab",
                "Assets/Prefabs/Characters/C05_Akane.prefab",
                "Assets/Prefabs/Characters/C06_Cultist.prefab",
                "Assets/Prefabs/Characters/C07_StellarWitch.prefab",
                "Assets/Prefabs/Characters/C08_SanpakuChan.prefab",
                "Assets/Prefabs/Characters/C09_PicoChan.prefab",
                "Assets/Prefabs/Characters/C10_Reika.prefab",
                "Assets/Prefabs/Characters/C11_Casual1.prefab",
                "Assets/Prefabs/Characters/C12_Fina.prefab"
            };

            foreach (var cp in charPrefabs)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(cp);
                if (go == null) continue;

                var anim = go.GetComponentInChildren<Animator>();
                var rig = go.GetComponent<BBWeaponRig>();

                // Calculate total height from Renderers bounds
                Renderer[] rends = go.GetComponentsInChildren<Renderer>();
                Bounds totalBounds = new Bounds(go.transform.position, Vector3.zero);
                bool hasBounds = false;
                foreach (var r in rends)
                {
                    if (r is MeshRenderer || r is SkinnedMeshRenderer)
                    {
                        if (!hasBounds) { totalBounds = r.bounds; hasBounds = true; }
                        else totalBounds.Encapsulate(r.bounds);
                    }
                }

                string rigInfo = rig != null ? $"Warrior: {rig.warriorType}, Prim: {(rig.primaryWeaponPrefab != null ? rig.primaryWeaponPrefab.name : "none")}, PrimScale: {rig.primaryScale}, Sec: {(rig.secondaryWeaponPrefab != null ? rig.secondaryWeaponPrefab.name : "none")}, SecScale: {rig.secondaryScale}" : "No BBWeaponRig";
                Debug.Log($"[CHARACTER] {go.name} | Height ~{totalBounds.size.y:F2}m (Span {totalBounds.size.x:F2}m x {totalBounds.size.z:F2}m) | {rigInfo}");
            }
            Debug.Log("================================================================");
        }
    }
}

#endif
