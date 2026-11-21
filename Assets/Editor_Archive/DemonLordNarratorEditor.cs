#if MDI_ARCHIVE
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using DemonIdol.Cutscenes;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Editor utilities and shortcuts to test all Demon Lord cutscenes, announcer portraits,
    /// and commentary scripts directly inside Unity.
    /// </summary>
    public static class DemonLordNarratorEditor
    {
        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Narrator & Cutscenes/Play Game Intro (Demon Lord)", priority = 20)]
        public static void PlayIntro()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DemonLordNarratorEditor] Please enter Play Mode to trigger in-engine cutscenes.");
                return;
            }
            DemonLordNarratorManager.Instance.PlayGameIntroduction(() =>
            {
                Debug.Log("<color=#FFD700>[Demon Lord]</color> Intro cutscene finished.");
            });
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Narrator & Cutscenes/Play Daily Routine (Demon Lord)", priority = 21)]
        public static void PlayRoutine()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DemonLordNarratorEditor] Please enter Play Mode to trigger in-engine cutscenes.");
                return;
            }
            DemonLordNarratorManager.Instance.PlayDailyRoutine(() =>
            {
                Debug.Log("<color=#FFD700>[Demon Lord]</color> Daily routine cutscene finished.");
            });
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Narrator & Cutscenes/Play Commentary to Camera", priority = 22)]
        public static void PlayCommentary()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DemonLordNarratorEditor] Please enter Play Mode to trigger in-engine cutscenes.");
                return;
            }
            DemonLordNarratorManager.Instance.PlayCommentary(() =>
            {
                Debug.Log("<color=#FFD700>[Demon Lord]</color> Commentary cutscene finished.");
            });
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Narrator & Cutscenes/Play Soul Drain (Domain Expansion)", priority = 23)]
        public static void PlaySoulDrain()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DemonLordNarratorEditor] Please enter Play Mode to trigger in-engine cutscenes.");
                return;
            }
            DemonLordNarratorManager.Instance.PlaySoulDrain(() =>
            {
                Debug.Log("<color=#FFD700>[Demon Lord]</color> Soul drain cutscene finished.");
            });
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Narrator & Cutscenes/Play Dark Energy Blast (Demon Beast)", priority = 24)]
        public static void PlayDarkBlast()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DemonLordNarratorEditor] Please enter Play Mode to trigger in-engine cutscenes.");
                return;
            }
            DemonLordNarratorManager.Instance.PlayDemonBeastBlast(() =>
            {
                Debug.Log("<color=#FFD700>[Demon Lord]</color> Dark blast cutscene finished.");
            });
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Narrator & Cutscenes/Play Game Over (Defeat)", priority = 25)]
        public static void PlayGameOver()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[DemonLordNarratorEditor] Please enter Play Mode to trigger in-engine cutscenes.");
                return;
            }
            DemonLordNarratorManager.Instance.PlayGameOver(() =>
            {
                Debug.Log("<color=#FFD700>[Demon Lord]</color> Game over cutscene finished.");
            });
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Narrator & Cutscenes/Verify All Media Files Exist", priority = 26)]
        public static void VerifyAllMediaFiles()
        {
            string[] relativePaths = new[]
            {
                DemonLordAssetCatalog.INTRO_HAILUO,
                DemonLordAssetCatalog.INTRO_KLING_A,
                DemonLordAssetCatalog.INTRO_KLING_SC,
                DemonLordAssetCatalog.SPEAKING_TO_CAMERA,
                DemonLordAssetCatalog.UMBRELLA_SPEAKING,
                DemonLordAssetCatalog.DAILY_ROUTINE,
                DemonLordAssetCatalog.SOUL_DRAIN_IDOL,
                DemonLordAssetCatalog.SOUL_DRAIN_ETHEREAL,
                DemonLordAssetCatalog.DARK_BLAST_BEAST,
                DemonLordAssetCatalog.DARK_ENERGY_BEAST,
                DemonLordAssetCatalog.GAME_OVER_GHOST,
                DemonLordAssetCatalog.GAME_OVER_GHOST_ALT,
                DemonLordAssetCatalog.GHOST_FLOATING_TITLE,
                DemonLordAssetCatalog.ANNOUNCER_PORTRAIT,
                DemonLordAssetCatalog.SHADOW_DEMON_ART,
                DemonLordAssetCatalog.GHOST_FLOATING_ART
            };

            int found = 0;
            int missing = 0;

            foreach (var rel in relativePaths)
            {
                if (DemonLordNarratorManager.MediaExists(rel))
                {
                    found++;
                }
                else
                {
                    missing++;
                    Debug.LogWarning($"[DemonLordNarratorEditor] Missing media: {rel}");
                }
            }

            Debug.Log($"<color=#00FF66>[DemonLordNarratorEditor] Media verification complete: {found} files found, {missing} missing.</color>");

            var portrait = DemonLordNarratorManager.Instance.GetAnnouncerPortraitTexture();
            if (portrait != null)
            {
                Debug.Log($"<color=#00FF66>[DemonLordNarratorEditor] Announcer portrait loaded cleanly: {portrait.width}x{portrait.height}</color>");
            }
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Narrator & Cutscenes/Log Announcer Roster Commentary", priority = 27)]
        public static void LogRosterCommentary()
        {
            string[] fighterIds = new[] { "C01", "C02", "C04", "C05", "C06" };
            foreach (var id in fighterIds)
            {
                string quote = DemonLordNarratorManager.Instance.GetFighterCommentary(id);
                Debug.Log($"<color=#FFD700>[Demon Lord on {id}]</color> {quote}");
            }
        }
    }
}
#endif

#endif
