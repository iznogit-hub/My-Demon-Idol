using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using DemonIdol.UI;
using DemonIdol.Launch;

namespace DemonIdol.Tests.PlayMode
{
    [TestFixture]
    public class ModernMenuPlayTests
    {
        [UnityTest]
        public IEnumerator MainMenu_LoadAndNavigate_PillarsAndDrawer()
        {
            LogAssert.ignoreFailingMessages = true;

            var op = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            while (!op.isDone) yield return null;
            yield return null;
            yield return null;

            var ctrl = DemonIdolMainMenuController.Instance;
            Assert.IsNotNull(ctrl, "DemonIdolMainMenuController must exist in MainMenu.unity.");

            // Verify initial EventSystem selection
            Assert.IsNotNull(EventSystem.current, "EventSystem must exist.");
            yield return null;

            // Switch to DOJO pillar
            ctrl.SelectPillar(MainMenuPillar.Dojo);
            yield return null;
            Assert.AreEqual(MainMenuPillar.Dojo, ctrl.CurrentPillar);
            Assert.IsTrue(ctrl.panelDojo != null && ctrl.panelDojo.activeSelf, "DOJO panel must be active.");

            // Switch to ROSTER pillar
            ctrl.SelectPillar(MainMenuPillar.Roster);
            yield return null;
            Assert.AreEqual(MainMenuPillar.Roster, ctrl.CurrentPillar);
            Assert.IsTrue(ctrl.panelRoster != null && ctrl.panelRoster.activeSelf, "ROSTER panel must be active.");

            // Open Settings Drawer
            ctrl.OpenSettingsDrawer();
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.IsTrue(ctrl.IsDrawerOpen, "Settings drawer must be open.");
            Assert.IsTrue(ctrl.settingsDrawerRoot != null && ctrl.settingsDrawerRoot.gameObject.activeSelf);

            // Close Settings Drawer
            ctrl.CloseSettingsDrawer();
            yield return new WaitForSecondsRealtime(0.20f);
            Assert.IsFalse(ctrl.IsDrawerOpen, "Settings drawer must be closed.");
        }

        [UnityTest]
        public IEnumerator CharacterSelect_LoadAndMirrorMatch_25Fighters()
        {
            LogAssert.ignoreFailingMessages = true;

            PlayerPrefs.SetString("MDI_GameMode", "LocalVersus");
            PlayerPrefs.SetString("MDI_P1_CharacterId", "C01");
            PlayerPrefs.SetString("MDI_P2_CharacterId", "C01");
            PlayerPrefs.Save();

            var op = SceneManager.LoadSceneAsync("CharacterSelect", LoadSceneMode.Single);
            while (!op.isDone) yield return null;
            yield return null;
            yield return null;

            var mgr = DemonIdolCharacterSelectManager.Instance;
            Assert.IsNotNull(mgr, "DemonIdolCharacterSelectManager must exist in CharacterSelect.unity.");

            // Verify 25 fighters
            Assert.AreEqual(25, DemonIdolCharacterSelectManager.RosterIds.Length);
            Assert.IsNotNull(mgr.characterButtons, "characterButtons array must exist.");
            Assert.AreEqual(25, mgr.characterButtons.Length, "Must have 25 character buttons.");

            // Verify Dual Pedestals
            Assert.IsNotNull(mgr.p1Pedestal, "P1 pedestal must exist.");
            Assert.IsNotNull(mgr.p2Pedestal, "P2 pedestal must exist.");

            // Verify Mirror Match (C01 vs C01)
            mgr.p1Index = 0;
            mgr.p2Index = 0;
            mgr.isVersus = true;
            mgr.LockInFighter(isP1: true);
            mgr.LockInFighter(isP1: false);
            mgr.UpdateHighlights();
            mgr.UpdateWings();

            Assert.IsTrue(mgr.p1Locked, "P1 must be locked in.");
            Assert.IsTrue(mgr.p2Locked, "P2 must be locked in.");
            Assert.IsTrue(mgr.p1Borders[0].gameObject.activeSelf, "P1 border must be active on C01.");
            Assert.IsTrue(mgr.p2Borders[0].gameObject.activeSelf, "P2 border must be active on C01.");

            // Verify Tactical Info Wings
            Assert.IsNotNull(mgr.p1NameWing, "P1 Name wing text must exist.");
            Assert.IsTrue(mgr.p1NameWing.text.Contains("AKIO"), "P1 Wing should display Akio.");
            Assert.IsNotNull(mgr.p2NameWing, "P2 Name wing text must exist.");
            Assert.IsTrue(mgr.p2NameWing.text.Contains("AKIO"), "P2 Wing should display Akio in mirror match.");

            // Verify Inline Match Options Strip
            Assert.IsNotNull(mgr.matchOptionsStrip, "Inline match options strip must exist.");
            Assert.IsTrue(mgr.matchOptionsStrip.activeSelf, "Match options strip must be active when both locked.");
        }
    }
}
