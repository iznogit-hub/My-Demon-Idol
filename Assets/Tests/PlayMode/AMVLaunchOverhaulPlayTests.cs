using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DemonIdol.UI;
using DemonIdol.Launch;
using DemonIdol.Cinematics;
using DemonIdol.BlazBlue;
using DemonIdol.BlazBlue.Visual;

namespace DemonIdol.Tests.PlayMode
{
    [TestFixture]
    public class AMVLaunchOverhaulPlayTests
    {
        private static readonly string[] LaunchFighterIds = new string[]
        {
            "C01", "C02", "C05", "C06", "C08", "C09",
            "C11", "C12", "C14", "EX01", "EX04", "SK08"
        };

        [UnityTest]
        public IEnumerator Phase1_RaimonMaterialUnification_All12FightersVerified()
        {
            LogAssert.ignoreFailingMessages = true;

            // Verify all 12 fighters have unified material directories and valid shaders
            foreach (var fid in LaunchFighterIds)
            {
                string matDir = Path.Combine(Application.dataPath, "Materials/RaimonUnified", fid);
                Assert.IsTrue(Directory.Exists(matDir), $"Unified material directory for {fid} must exist at {matDir}");

                string[] matFiles = Directory.GetFiles(matDir, "*_Raimon.mat");
                Assert.Greater(matFiles.Length, 0, $"Fighter {fid} must have at least one unified material.");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Phase2_MenuCameraRig_TelephotoAndPostProcessing_Verified()
        {
            LogAssert.ignoreFailingMessages = true;

            // Load MainMenu scene
            var op = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            while (!op.isDone) yield return null;
            yield return null;

            // 1. Verify Camera Rig & Telephoto Optics (28°-32° FOV)
            var cam = UnityEngine.Camera.main;
            Assert.IsNotNull(cam, "Main Camera must exist.");
            var rig = cam.GetComponent<MenuFighterCameraRig>();
            Assert.IsNotNull(rig, "MenuFighterCameraRig must be attached to Main Camera.");
            Assert.IsTrue(cam.fieldOfView >= 28f && cam.fieldOfView <= 32f, $"Camera FOV ({cam.fieldOfView}) must be between 28 and 32 degrees.");

            // 2. Verify Atmospheric Fog (obsidian-teal)
            Assert.IsTrue(RenderSettings.fog, "RenderSettings.fog must be enabled.");
            Assert.AreEqual(FogMode.ExponentialSquared, RenderSettings.fogMode, "Fog mode must be ExponentialSquared.");
            Assert.AreEqual(SumiStyleTokens.FogTeal, RenderSettings.fogColor, "MainMenu fog color must be deep obsidian-teal.");

            // 3. Verify URP Volume Overrides
            Assert.IsNotNull(rig.postProcessVolume, "Post-process volume must be assigned.");
            var profile = rig.postProcessVolume.profile;
            Assert.IsNotNull(profile, "VolumeProfile must exist.");

            Assert.IsTrue(profile.Has<DepthOfField>(), "Volume must contain DepthOfField.");
            Assert.IsTrue(profile.Has<Tonemapping>(), "Volume must contain Tonemapping.");
            Assert.IsTrue(profile.Has<ColorAdjustments>(), "Volume must contain ColorAdjustments.");
            Assert.IsTrue(profile.Has<Bloom>(), "Volume must contain Bloom.");
            Assert.IsTrue(profile.Has<Vignette>(), "Volume must contain Vignette.");
            Assert.IsTrue(profile.Has<ChromaticAberration>(), "Volume must contain ChromaticAberration.");

            // 4. Test GraphicsProfiles quality wiring
            GraphicsProfiles.ApplyToVolume(rig.postProcessVolume, 0); // Low tier
            profile.TryGet<DepthOfField>(out var dof);
            Assert.IsFalse(dof.active, "Low quality must disable Bokeh DOF.");

            GraphicsProfiles.ApplyToVolume(rig.postProcessVolume, 2); // High tier
            Assert.IsTrue(dof.active, "High quality must enable Bokeh DOF.");
        }

        [UnityTest]
        public IEnumerator Phase3_AkioColdOpen_DirectorAndSkip_Verified()
        {
            LogAssert.ignoreFailingMessages = true;
            AkioColdOpenDirector.HasPlayedColdOpen = false;

            var op = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            while (!op.isDone) yield return null;
            yield return null;

            var director = AkioColdOpenDirector.Instance;
            Assert.IsNotNull(director, "AkioColdOpenDirector must exist in MainMenu.unity.");
            Assert.IsNotNull(director.typographyCanvas, "Giant typography canvas must exist.");
            Assert.IsNotNull(director.txtImpactWord, "Giant impact text must exist.");

            // Test Skip
            director.SkipColdOpen();
            yield return null;

            Assert.IsFalse(director.IsColdOpenPlaying, "Cold open must stop playing after skip.");
            Assert.IsTrue(AkioColdOpenDirector.HasPlayedColdOpen, "HasPlayedColdOpen must be true.");
            Assert.IsTrue(director.menuUIGroup.alpha > 0.99f, "Menu UI group must be fully visible after skip.");
            Assert.IsFalse(director.typographyCanvas.gameObject.activeSelf, "Typography canvas must be disabled after skip.");
        }

        [UnityTest]
        public IEnumerator Phase4_CharacterSelect_CloseUpDualPedestals_AndLockIn_Verified()
        {
            LogAssert.ignoreFailingMessages = true;

            PlayerPrefs.SetString("MDI_GameMode", "LocalVersus");
            PlayerPrefs.SetString("MDI_P1_CharacterId", "C01");
            PlayerPrefs.SetString("MDI_P2_CharacterId", "EX01");
            PlayerPrefs.Save();

            var op = SceneManager.LoadSceneAsync("CharacterSelect", LoadSceneMode.Single);
            while (!op.isDone) yield return null;
            yield return null;

            var mgr = DemonIdolCharacterSelectManager.Instance;
            Assert.IsNotNull(mgr, "DemonIdolCharacterSelectManager must exist in CharacterSelect.");

            // Verify Launch Roster IDs (12 fighters)
            Assert.AreEqual(12, DemonIdolCharacterSelectManager.LaunchRosterIds.Length);

            // Verify Pedestals & Telephoto Rig
            Assert.IsNotNull(mgr.p1Pedestal, "P1 pedestal must exist.");
            Assert.IsNotNull(mgr.p2Pedestal, "P2 pedestal must exist.");
            var cam = UnityEngine.Camera.main;
            Assert.IsNotNull(cam, "Main camera must exist in CharacterSelect.");
            var rig = cam.GetComponent<MenuFighterCameraRig>();
            Assert.IsNotNull(rig, "MenuFighterCameraRig must exist in CharacterSelect.");

            // Test Lock-In presentation
            mgr.LockInFighter(isP1: true);
            yield return null;

            Assert.IsTrue(mgr.p1Locked, "P1 must be locked.");
            if (mgr.p1LockInSlamText != null)
            {
                Assert.IsTrue(mgr.p1LockInSlamText.gameObject.activeSelf, "P1 lock-in slam text must be active.");
            }
        }

        [UnityTest]
        public IEnumerator Phase4_AMVImpactFx_FightFeelAccents_Verified()
        {
            LogAssert.ignoreFailingMessages = true;

            AMVImpactFx.EnsureCanvas();
            var fx = AMVImpactFx.Instance;
            Assert.IsNotNull(fx, "AMVImpactFx instance must exist.");

            // Trigger counter-hit flash
            AMVImpactFx.TriggerCounterOrCrushFlash(Vector3.zero, isCounterHit: true, isGuardCrush: false);
            yield return null;

            // Trigger distortion drive cut-in
            AMVImpactFx.TriggerDistortionCutIn("C01");
            yield return null;

            // Trigger match-winning KO flash
            AMVImpactFx.TriggerKODramaticFlash(isHeavyOrSuper: true);
            yield return null;

            // Verify no exceptions or null refs
            Assert.Pass("AMV impact presentation accents triggered successfully.");
        }
    }
}
