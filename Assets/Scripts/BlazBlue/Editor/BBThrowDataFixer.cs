using System;
using UnityEngine;
using UnityEditor;
using DemonIdol.BlazBlue;

namespace DemonIdol.BlazBlue.Editor
{
    public static class BBThrowDataFixer
    {
        [MenuItem("Tools/MDI/Fix All Throw Move Assets")]
        public static void FixAllThrows()
        {
            string[] guids = AssetDatabase.FindAssets("t:BBAttackData", new[] { "Assets/CombatData/BlazBlue" });
            int fixedCount = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.EndsWith("_BC.asset", StringComparison.OrdinalIgnoreCase) ||
                    path.Contains("_BC.asset") ||
                    path.Contains("BC.asset"))
                {
                    var attack = AssetDatabase.LoadAssetAtPath<BBAttackData>(path);
                    if (attack != null && (attack.moveId == "BC" || attack.name.EndsWith("_BC") || attack.attackName.Contains("Throw")))
                    {
                        attack.startupFrames = 7;
                        attack.activeFrames = 3;
                        attack.recoveryFrames = 23;
                        attack.damage = 1300f;
                        attack.minDamagePct = 1.0f;

                        if (attack.boxes != null)
                        {
                            if (attack.boxes.boxes != null)
                            {
                                for (int b = 0; b < attack.boxes.boxes.Length; b++)
                                {
                                    attack.boxes.boxes[b].kind = BBBoxKind.Throw;
                                }
                            }
                            int targetTicks = attack.TotalTicks; // 33
                            if (attack.boxes.tickCount != targetTicks)
                            {
                                int lastOffset = attack.boxes.frameStart != null && attack.boxes.frameStart.Length > 0 
                                    ? attack.boxes.frameStart[attack.boxes.frameStart.Length - 1] 
                                    : 0;
                                var newFrameStart = new int[targetTicks + 1];
                                for (int t = 0; t <= targetTicks; t++)
                                {
                                    if (attack.boxes.frameStart != null && t < attack.boxes.frameStart.Length)
                                    {
                                        newFrameStart[t] = attack.boxes.frameStart[t];
                                    }
                                    else
                                    {
                                        newFrameStart[t] = lastOffset;
                                    }
                                }
                                attack.boxes.frameStart = newFrameStart;
                                attack.boxes.tickCount = targetTicks;
                                EditorUtility.SetDirty(attack.boxes);
                            }
                        }

                        EditorUtility.SetDirty(attack);
                        fixedCount++;
                    }
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=#00e676>[BBThrowDataFixer] Successfully updated {fixedCount} throw assets to 7f/3f/23f/1300dmg/100% min dmg.</color>");
        }
    }
}
