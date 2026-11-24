#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using DemonIdol.UI;
using DemonIdol.Combat;
using DemonIdol.Launch;

namespace DemonIdol.BlazBlue.Tests
{
    [TestFixture]
    public class ModernMenuNavigationTests
    {
        [Test]
        public void Test_SumiStyleTokens_Accessibility_And_Palettes()
        {
            // Verify core palette tokens
            Assert.AreEqual(new Color(0.957f, 0.937f, 0.902f, 0.95f), SumiStyleTokens.PaperWhite);
            Assert.AreEqual(new Color(0.102f, 0.102f, 0.102f, 1.0f), SumiStyleTokens.CarbonInk);
            Assert.AreEqual(new Color(0.761f, 0.231f, 0.133f, 1.0f), SumiStyleTokens.Vermilion);
            Assert.AreEqual(new Color(0.831f, 0.686f, 0.216f, 1.0f), SumiStyleTokens.ParryGold);
            Assert.AreEqual(new Color(0.106f, 0.122f, 0.231f, 0.95f), SumiStyleTokens.IndigoVoid);

            // Verify timings
            Assert.AreEqual(0.20f, SumiStyleTokens.PanelInDurationNormal);
            Assert.AreEqual(0.14f, SumiStyleTokens.PanelOutDurationNormal);
            Assert.AreEqual(0.08f, SumiStyleTokens.ButtonPulseDurationNormal);
            Assert.AreEqual(0.15f, SumiStyleTokens.InkWipeDurationNormal);
            Assert.AreEqual(0.05f, SumiStyleTokens.ReducedMotionMaxDuration);

            // Verify easing curves
            Assert.AreEqual(0f, SumiStyleTokens.EaseOutCubic(0f));
            Assert.AreEqual(1f, SumiStyleTokens.EaseOutCubic(1f));
            Assert.Greater(SumiStyleTokens.EaseOutCubic(0.5f), 0.5f, "EaseOutCubic should decelerate (greater than linear midpoint).");

            Assert.AreEqual(0f, SumiStyleTokens.EaseInCubic(0f));
            Assert.AreEqual(1f, SumiStyleTokens.EaseInCubic(1f));
            Assert.Less(SumiStyleTokens.EaseInCubic(0.5f), 0.5f, "EaseInCubic should accelerate (less than linear midpoint).");
        }

        [Test]
        public void Test_MainMenu_Controller_Pillar_Switching_And_Panels()
        {
            var go = new GameObject("TestMainMenu");
            var ctrl = go.AddComponent<DemonIdolMainMenuController>();

            // Setup mock UI elements
            ctrl.panelFight = new GameObject("Panel_FIGHT");
            ctrl.panelDojo = new GameObject("Panel_DOJO");
            ctrl.panelRoster = new GameObject("Panel_ROSTER");

            // Test Pillar: FIGHT
            ctrl.SelectPillar(MainMenuPillar.Fight, immediate: true);
            Assert.AreEqual(MainMenuPillar.Fight, ctrl.CurrentPillar);
            Assert.IsTrue(ctrl.panelFight.activeSelf, "FIGHT panel must be active.");
            Assert.IsFalse(ctrl.panelDojo.activeSelf, "DOJO panel must be inactive.");
            Assert.IsFalse(ctrl.panelRoster.activeSelf, "ROSTER panel must be inactive.");

            // Test Pillar: DOJO
            ctrl.SelectPillar(MainMenuPillar.Dojo, immediate: true);
            Assert.AreEqual(MainMenuPillar.Dojo, ctrl.CurrentPillar);
            Assert.IsFalse(ctrl.panelFight.activeSelf);
            Assert.IsTrue(ctrl.panelDojo.activeSelf);
            Assert.IsFalse(ctrl.panelRoster.activeSelf);

            // Test Pillar: ROSTER
            ctrl.SelectPillar(MainMenuPillar.Roster, immediate: true);
            Assert.AreEqual(MainMenuPillar.Roster, ctrl.CurrentPillar);
            Assert.IsFalse(ctrl.panelFight.activeSelf);
            Assert.IsFalse(ctrl.panelDojo.activeSelf);
            Assert.IsTrue(ctrl.panelRoster.activeSelf);

            UnityEngine.Object.DestroyImmediate(ctrl.panelFight);
            UnityEngine.Object.DestroyImmediate(ctrl.panelDojo);
            UnityEngine.Object.DestroyImmediate(ctrl.panelRoster);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_MainMenu_SlideOver_Settings_Drawer_Tabs()
        {
            var go = new GameObject("TestMainMenuDrawer");
            var ctrl = go.AddComponent<DemonIdolMainMenuController>();

            ctrl.settingsDrawerRoot = new GameObject("DrawerRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            ctrl.drawerCanvasGroup = ctrl.settingsDrawerRoot.gameObject.AddComponent<CanvasGroup>();
            ctrl.drawerScrim = new GameObject("Scrim");

            ctrl.drawerPanelAudio = new GameObject("Panel_Audio");
            ctrl.drawerPanelGraphics = new GameObject("Panel_Graphics");
            ctrl.drawerPanelAccessibility = new GameObject("Panel_Access");
            ctrl.drawerPanelControls = new GameObject("Panel_Controls");

            // Open Drawer
            ctrl.OpenSettingsDrawer();
            Assert.IsTrue(ctrl.IsDrawerOpen, "Settings drawer must report open.");

            // Test Drawer Tabs
            ctrl.SelectDrawerTab(0);
            Assert.IsTrue(ctrl.drawerPanelAudio.activeSelf, "Audio tab must be active.");
            Assert.IsFalse(ctrl.drawerPanelGraphics.activeSelf);

            ctrl.SelectDrawerTab(1);
            Assert.IsFalse(ctrl.drawerPanelAudio.activeSelf);
            Assert.IsTrue(ctrl.drawerPanelGraphics.activeSelf, "Graphics tab must be active.");

            ctrl.SelectDrawerTab(2);
            Assert.IsFalse(ctrl.drawerPanelGraphics.activeSelf);
            Assert.IsTrue(ctrl.drawerPanelAccessibility.activeSelf, "Accessibility tab must be active.");

            ctrl.SelectDrawerTab(3);
            Assert.IsFalse(ctrl.drawerPanelAccessibility.activeSelf);
            Assert.IsTrue(ctrl.drawerPanelControls.activeSelf, "Controls tab must be active.");

            UnityEngine.Object.DestroyImmediate(ctrl.drawerPanelAudio);
            UnityEngine.Object.DestroyImmediate(ctrl.drawerPanelGraphics);
            UnityEngine.Object.DestroyImmediate(ctrl.drawerPanelAccessibility);
            UnityEngine.Object.DestroyImmediate(ctrl.drawerPanelControls);
            UnityEngine.Object.DestroyImmediate(ctrl.drawerScrim);
            UnityEngine.Object.DestroyImmediate(ctrl.settingsDrawerRoot.gameObject);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_CharacterSelect_Roster_25_Fighters_And_Categories()
        {
            string[] roster = DemonIdolCharacterSelectManager.RosterIds;
            Assert.AreEqual(25, roster.Length, "Roster must contain exactly 25 fighters.");

            // Verify categories
            int mainCast = 0;
            int shadowKnights = 0;
            int expansion = 0;

            for (int i = 0; i < roster.Length; i++)
            {
                string id = roster[i];
                if (id.StartsWith("C")) mainCast++;
                else if (id.StartsWith("SK")) shadowKnights++;
                else if (id.StartsWith("EX")) expansion++;
            }

            Assert.AreEqual(13, mainCast, "Main Cast must have 13 fighters (C01-C14 excl C03).");
            Assert.AreEqual(5, shadowKnights, "Shadow Knights must have 5 fighters (SK02, SK03, SK05, SK06, SK08).");
            Assert.AreEqual(7, expansion, "Expansion must have 7 fighters (EX01-EX07).");
        }

        [Test]
        public void Test_CharacterSelect_Stats_Computation_From_BBRosterArchetype()
        {
            string[] roster = DemonIdolCharacterSelectManager.RosterIds;

            for (int i = 0; i < roster.Length; i++)
            {
                string id = roster[i];
                var info = BBRosterArchetype.Get(id);
                Assert.IsFalse(string.IsNullOrEmpty(info.driveName), $"Fighter {id} must have a driveName.");

                DemonIdolCharacterSelectManager.GetFighterStats(id, out float reach, out float speed, out float power);

                Assert.GreaterOrEqual(reach, 0f, $"{id} reach must be >= 0");
                Assert.LessOrEqual(reach, 1f, $"{id} reach must be <= 1");

                Assert.GreaterOrEqual(speed, 0f, $"{id} speed must be >= 0");
                Assert.LessOrEqual(speed, 1f, $"{id} speed must be <= 1");

                Assert.GreaterOrEqual(power, 0f, $"{id} power must be >= 0");
                Assert.LessOrEqual(power, 1f, $"{id} power must be <= 1");
            }
        }

        [Test]
        public void Test_CharacterSelect_Mirror_Match_Cursor_Support()
        {
            var go = new GameObject("TestCharSelect");
            var mgr = go.AddComponent<DemonIdolCharacterSelectManager>();

            mgr.p1Borders = new Image[25];
            mgr.p2Borders = new Image[25];

            for (int i = 0; i < 25; i++)
            {
                var b1 = new GameObject($"P1_Border_{i}").AddComponent<Image>();
                var b2 = new GameObject($"P2_Border_{i}").AddComponent<Image>();
                mgr.p1Borders[i] = b1;
                mgr.p2Borders[i] = b2;
            }

            // Mirror Match: Both on C01 (index 0)
            mgr.p1Index = 0;
            mgr.p2Index = 0;
            mgr.p1Locked = true;
            mgr.p2Locked = true;
            mgr.isVersus = true;

            mgr.UpdateHighlights();

            Assert.IsTrue(mgr.p1Borders[0].gameObject.activeSelf, "P1 border must be active on mirror slot 0.");
            Assert.IsTrue(mgr.p2Borders[0].gameObject.activeSelf, "P2 border must be active on mirror slot 0.");
            Assert.AreEqual(SumiStyleTokens.Vermilion, mgr.p1Borders[0].color, "P1 locked border must be Vermilion.");
            Assert.AreEqual(SumiStyleTokens.ParryGold, mgr.p2Borders[0].color, "P2 mirror border must be ParryGold.");

            for (int i = 0; i < 25; i++)
            {
                UnityEngine.Object.DestroyImmediate(mgr.p1Borders[i].gameObject);
                UnityEngine.Object.DestroyImmediate(mgr.p2Borders[i].gameObject);
            }
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_CharacterSelect_Inline_Options_And_Stage_Defaults()
        {
            Assert.AreEqual("ST07", DemonIdolCharacterSelectManager.StageIds[0], "Default stage must be ST07 (College Raimon).");
            Assert.IsTrue(DemonIdolCharacterSelectManager.StageNames[0].Contains("COLLEGE RAIMON"), "ST07 display name must mention College Raimon.");
            Assert.AreEqual(5, DemonIdolCharacterSelectManager.StageIds.Length);
            Assert.AreEqual(5, DemonIdolCharacterSelectManager.DifficultyNames.Length);
        }
    }
}
#endif
