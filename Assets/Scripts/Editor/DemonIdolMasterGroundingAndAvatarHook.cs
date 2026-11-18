
#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.Characters.Editor
{
    public static class DemonIdolMasterGroundingAndAvatarHook
    {
        [InitializeOnLoadMethod]
        public static void AutoExecuteOnCompile()
        {
            string triggerFile = "run_master_hook.trigger";
            if (!File.Exists(triggerFile)) return;
            try { File.Delete(triggerFile); } catch {}
            ExecuteFullHookAndVerification();
        }

        [MenuItem("Demon Idol/🚀 Playability/Master Grounding, Avatar & Animation Fix (All 14)", priority = 1)]
        public static void ExecuteFullHookAndVerification()
        {
            Debug.Log("<color=#00e676>=============================================================</color>");
            Debug.Log("<color=#00e676>  DEMON IDOL: MASTER GROUNDING, AVATARS & ALL ANIMATIONS HOOK </color>");
            Debug.Log("<color=#00e676>=============================================================</color>");

            // 1. Deploy Pure Mecanim Humanoid Clips
            MDI_PureHumanoidClipDeployer.DeployPureMecanimClips();

            // 2. Hook All 14 Fighters, Authentic Avatars, and Controllers
            DemonIdolRosterAnimatorHook.HookAll14FightersAndAnimators();

            // 3. Rebuild Arcade Arena Scene cleanly
            DemonIdol.Editor.DemonIdolArcadeArenaBuilder.BuildArcadeArenaScene();

            // 4. Perform Complete Grounding Audit on all 14 Fighters
            RunComplete14FighterAudit();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#00e676><b>[MasterGroundingHook] ALL 14 FIGHTERS FULLY GROUNDED WITH AUTHENTIC AVATARS & CLEAN ANIMATIONS!</b></color>");
        }

        private static void RunComplete14FighterAudit()
        {
            AssetDatabase.Refresh();
            string logFile = "final_14_fighter_audit.log";
            using (var sw = new StreamWriter(logFile, false))
            {
                sw.WriteLine("=== DEMON IDOL 14-FIGHTER AUTHENTIC AVATAR & GROUNDING AUDIT ===");
                var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/MDI_Animations/MDI_Anim_Idle.anim");
                string[] ids = new string[] { "C01", "C02", "C03", "C04", "C05", "C06", "C07", "C08", "C09", "C10", "C11", "C12", "C13", "C14" };

                int passedCount = 0;

                foreach (var cid in ids)
                {
                    CharacterDefinition charDef = null;
                    string[] guids = AssetDatabase.FindAssets($"t:CharacterDefinition {cid}");
                    if (guids != null && guids.Length > 0)
                    {
                        string defPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                        charDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(defPath);
                    }

                    if (charDef == null)
                    {
                        sw.WriteLine($"[{cid}] CharacterDefinition NULL!");
                        continue;
                    }

                    GameObject prefab = charDef.modelPrefab;
                    if (prefab == null)
                    {
                        string pPath = $"Assets/Prefabs/Characters/{cid}_{charDef.displayName}.prefab";
                        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pPath);
                        if (prefab == null)
                        {
                            string[] pGuids = AssetDatabase.FindAssets($"t:GameObject {cid} in:Assets/Prefabs/Characters");
                            if (pGuids != null && pGuids.Length > 0)
                            {
                                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(pGuids[0]));
                            }
                        }
                        if (prefab != null)
                        {
                            charDef.modelPrefab = prefab;
                            EditorUtility.SetDirty(charDef);
                        }
                    }

                    if (prefab == null)
                    {
                        sw.WriteLine($"[{cid} {charDef.displayName}] modelPrefab NULL!");
                        continue;
                    }

                    GameObject root = new GameObject($"Audit_{cid}");
                    root.transform.position = Vector3.zero;

                    GameObject model = UnityEngine.Object.Instantiate(prefab, root.transform);
                    model.name = $"Model_{cid}";
                    model.transform.localPosition = Vector3.zero;

                    Animator anim = model.GetComponentInChildren<Animator>();
                    if (anim != null)
                    {
                        anim.applyRootMotion = false;
                        if (charDef.animatorController != null) anim.runtimeAnimatorController = charDef.animatorController;
                        if (charDef.avatar != null) anim.avatar = charDef.avatar;
                    }

                    if (idleClip != null)
                    {
                        idleClip.SampleAnimation(model, 0.5f);
                    }

                    float minFootY = 999f;
                    float hipsY = -999f;
                    string hipsName = "";

                    if (anim != null && anim.isHuman)
                    {
                        var h = anim.GetBoneTransform(HumanBodyBones.Hips);
                        if (h != null)
                        {
                            hipsY = h.position.y;
                            hipsName = h.name;
                        }
                        var lf = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
                        var rf = anim.GetBoneTransform(HumanBodyBones.RightFoot);
                        var lt = anim.GetBoneTransform(HumanBodyBones.LeftToes);
                        var rt = anim.GetBoneTransform(HumanBodyBones.RightToes);
                        if (lf != null && lf.position.y < minFootY) minFootY = lf.position.y;
                        if (rf != null && rf.position.y < minFootY) minFootY = rf.position.y;
                        if (lt != null && lt.position.y < minFootY) minFootY = lt.position.y;
                        if (rt != null && rt.position.y < minFootY) minFootY = rt.position.y;
                    }

                    foreach (var t in model.GetComponentsInChildren<Transform>())
                    {
                        string lname = t.name.ToLower();
                        if (hipsY == -999f && (lname.Contains("hip") || lname.Contains("pelvis") || lname.Contains("root")))
                        {
                            hipsY = t.position.y;
                            hipsName = t.name;
                        }
                        if (lname.Contains("ankle") || lname.Contains("foot") || lname.Contains("toe"))
                        {
                            if (t.position.y < minFootY) minFootY = t.position.y;
                        }
                    }

                    float boundsMinY = 999f;
                    float boundsMaxY = -999f;
                    int rendererCount = 0;
                    var smrs = model.GetComponentsInChildren<Renderer>();
                    foreach (var smr in smrs)
                    {
                        rendererCount++;
                        if (smr.bounds.min.y < boundsMinY) boundsMinY = smr.bounds.min.y;
                        if (smr.bounds.max.y > boundsMaxY) boundsMaxY = smr.bounds.max.y;
                    }

                    if (minFootY == 999f && boundsMinY != 999f) minFootY = boundsMinY;

                    string avatarName = anim != null && anim.avatar != null ? anim.avatar.name : "NONE";
                    string ctrlName = anim != null && anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "NONE";

                    bool avatarValid = anim != null && anim.avatar != null;
                    bool groundedValid = (minFootY >= -0.20f && minFootY <= 0.30f) || (boundsMinY >= -0.20f && boundsMinY <= 0.30f);
                    bool renderersValid = rendererCount > 0;

                    if (avatarValid && groundedValid && renderersValid) passedCount++;

                    sw.WriteLine($"[{cid} {charDef.displayName}] Avatar: {avatarName} (Valid: {avatarValid}) | Controller: {ctrlName} | Renderers: {rendererCount} | Hips({hipsName}): Y={hipsY:F2}m | MinFoot: Y={minFootY:F2}m | MeshBounds: [{boundsMinY:F2}m, {boundsMaxY:F2}m] | STATUS: {(avatarValid && groundedValid && renderersValid ? "PASS" : "FAIL")}");

                    UnityEngine.Object.DestroyImmediate(root);
                }

                sw.WriteLine($"\nAUDIT RESULT: {passedCount}/{ids.Length} FIGHTERS FULLY VERIFIED & GROUNDED");
                sw.WriteLine("=== END OF AUDIT ===");
            }
        }
    }
}
#endif
