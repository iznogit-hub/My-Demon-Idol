#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Editor validator for 14-fighter data-driven MoveSets.
    /// Checks every fighter and move for missing AnimationClips or clips shared with Akio (C01),
    /// producing an actionable checklist for combat animators.
    /// </summary>
    public static class FighterMoveSetValidator
    {
        public struct MoveValidationItem
        {
            public string CharacterId;
            public string CharacterName;
            public string MoveSlot;
            public string Status; // "MISSING_CLIP" | "AKIO_SHARED" | "AUTHORED"
            public string ClipName;
        }

        public class ValidationReport
        {
            public int TotalFightersChecked;
            public int TotalMovesChecked;
            public int AuthoredClipsCount;
            public int MissingClipsCount;
            public int KaiSharedClipsCount;
            public List<MoveValidationItem> Items = new List<MoveValidationItem>();
            public string ChecklistText;
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Combat/Validate 14-Fighter MoveSets", priority = 100)]
        public static void ValidateAndLog()
        {
            ValidationReport report = RunValidation();
            Debug.Log($"<color=#00e676>[FighterMoveSetValidator] Validation complete.</color>\n" +
                      $"Fighters: {report.TotalFightersChecked} | Total Moves: {report.TotalMovesChecked} | " +
                      $"Authored: {report.AuthoredClipsCount} | Missing: {report.MissingClipsCount} | Shared: {report.KaiSharedClipsCount}\n\n" +
                      report.ChecklistText);
        }

        public static ValidationReport RunValidation()
        {
            var report = new ValidationReport();
            var sb = new StringBuilder();
            sb.AppendLine("=== 14-FIGHTER ANIMATION CHECKLIST ===");

            string[] fighterIds = new string[]
            {
                "C01", "C02", "C04", "C05", "C06", "C07",
                "C08", "C09", "C10", "C11", "C12", "C13", "C14"
            };

            CharacterRegistry.ReloadRegistry();

            // Cache Akio's baseline moves
            MoveSet baselineMoveSet = CharacterRegistry.GetCharacterById("C01")?.moveSet;

            report.TotalFightersChecked = fighterIds.Length;

            foreach (string fid in fighterIds)
            {
                string shortName = CharacterRegistry.GetCharacterNameShort(fid);
                var charDef = CharacterRegistry.GetCharacterById(fid);
                MoveSet ms = charDef?.moveSet ?? Resources.Load<MoveSet>($"Content/MoveSets/{fid}_{shortName}_MoveSet");

                sb.AppendLine($"\n[{fid}] {shortName} (Archetype: {ms?.archetype.ToString() ?? "Unknown"}):");

                if (ms == null)
                {
                    sb.AppendLine($"  [!] MoveSet asset completely missing for {fid}_{shortName}!");
                    continue;
                }

                // Moves to inspect:
                var moves = new (string slotName, AttackData attack, AttackData baseAttack)[]
                {
                    ("Light (Standing L)", ms.standingL, baselineMoveSet?.standingL),
                    ("Light Low (Crouching L)", ms.crouchingL, baselineMoveSet?.crouchingL),
                    ("Light Air (Air L)", ms.airL, baselineMoveSet?.airL),
                    ("Heavy (Standing H)", ms.standingH, baselineMoveSet?.standingH),
                    ("Heavy Overhead (Forward H)", ms.forwardH, baselineMoveSet?.forwardH),
                    ("Heavy Low (Crouching H)", ms.crouchingH, baselineMoveSet?.crouchingH),
                    ("Heavy Air (Air H)", ms.airH, baselineMoveSet?.airH),
                    ("Skill Neutral (S)", ms.neutralS, baselineMoveSet?.neutralS),
                    ("Skill Advancing (Forward S)", ms.forwardS, baselineMoveSet?.forwardS),
                    ("Skill AntiAir (Down S)", ms.downS, baselineMoveSet?.downS),
                    ("Skill Air (Air S)", ms.airS, baselineMoveSet?.airS),
                    ("Throw (L+H)", ms.tacticalThrow, baselineMoveSet?.tacticalThrow),
                    ("Super (H+S)", ms.domainSuper, baselineMoveSet?.domainSuper),
                    ("Escape / Burst (L+S)", ms.defensiveBurst, baselineMoveSet?.defensiveBurst)
                };

                foreach (var (slotName, atk, baseAtk) in moves)
                {
                    report.TotalMovesChecked++;
                    var item = new MoveValidationItem
                    {
                        CharacterId = fid,
                        CharacterName = shortName,
                        MoveSlot = slotName
                    };

                    if (atk == null || atk.animationClip == null)
                    {
                        item.Status = "MISSING_CLIP";
                        item.ClipName = "(None)";
                        report.MissingClipsCount++;
                        sb.AppendLine($"  [ ] {slotName}: [MISSING CLIP]");
                    }
                    else if (fid != "C01" && baseAtk != null && atk.animationClip == baseAtk.animationClip)
                    {
                        item.Status = "SHARED";
                        item.ClipName = atk.animationClip.name;
                        report.KaiSharedClipsCount++;
                        sb.AppendLine($"  [ ] {slotName}: [KAI-SHARED CLIP: {atk.animationClip.name}]");
                    }
                    else
                    {
                        item.Status = "AUTHORED";
                        item.ClipName = atk.animationClip != null ? atk.animationClip.name : atk.animationTrigger;
                        report.AuthoredClipsCount++;
                        sb.AppendLine($"  [x] {slotName}: [AUTHORED: {item.ClipName}]");
                    }

                    report.Items.Add(item);
                }
            }

            report.ChecklistText = sb.ToString();
            return report;
        }
    }
}
#endif
