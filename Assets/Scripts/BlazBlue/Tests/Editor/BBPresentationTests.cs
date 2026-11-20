#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using DemonIdol.UI;

namespace DemonIdol.BlazBlue.Tests
{
    public class BBPresentationTests
    {
        [Test]
        public void PresentationCardsCoverAllTwentyFive()
        {
            Assert.AreEqual(25, BBMatchFlow.Roster.Length);
            for (int i = 0; i < BBMatchFlow.Roster.Length; i++)
            {
                string id = BBMatchFlow.Roster[i];
                var card = LoadCard(id);
                Assert.IsNotNull(card, id);
                Assert.AreEqual(id, card.characterId);
                Assert.IsFalse(string.IsNullOrEmpty(card.displayName));
                Assert.IsFalse(string.IsNullOrEmpty(card.driveName));
                Assert.IsNull(card.portrait);
                Assert.IsNull(card.versusVideo);
                Assert.IsTrue(string.IsNullOrEmpty(card.quote));
            }
        }

        [Test]
        public void SignatureClipNamesMatchTokens()
        {
            Assert.IsTrue(BBSignatureClipFallback.ClipNameMatches("HumanF@Attack1H01_R", "KI 1H Attack"));
            Assert.IsTrue(BBSignatureClipFallback.ClipNameMatches("Spearman@Attack1", "Spearman Attack1"));
            Assert.IsTrue(BBSignatureClipFallback.ClipNameMatches("MDI_BB_Attack_1", "CMU 79 disc"));
            Assert.IsFalse(BBSignatureClipFallback.ClipNameMatches("Karate@Idle", "Hammer Attack1"));
            for (int i = 0; i < BBMatchFlow.Roster.Length; i++)
            {
                Assert.IsTrue(BBSignatureTable.TryBase(BBMatchFlow.Roster[i], out var signature));
                Assert.IsFalse(string.IsNullOrEmpty(signature.clipToken));
                Assert.IsFalse(string.IsNullOrEmpty(signature.specialId));
                Assert.IsFalse(string.IsNullOrEmpty(signature.superId));
            }
        }

        [Test]
        public void ReducedGorePersistsInSettingsJson()
        {
            var data = new SettingsData();
            data.reducedGore = true;
            string json = JsonUtility.ToJson(data);
            var loaded = JsonUtility.FromJson<SettingsData>(json);
            Assert.IsTrue(loaded.reducedGore);
            data.reducedGore = false;
            loaded = JsonUtility.FromJson<SettingsData>(JsonUtility.ToJson(data));
            Assert.IsFalse(loaded.reducedGore);
        }

        [Test]
        public void VersusAndResultsFlow()
        {
            var flow = new BBMatchFlow();
            flow.SelectMode("Versus");
            flow.ConfirmCharacters("C01", "C02");
            Assert.AreEqual(BBFlowScreen.Versus, flow.Screen);
            Assert.AreEqual("vs", flow.PortraitHook);
            flow.BeginFight();
            Assert.AreEqual(BBFlowScreen.Fight, flow.Screen);
            flow.ReportRound(1, true, false);
            Assert.IsTrue(flow.LastRoundPerfect);
            flow.ContinueAfterRound();
            flow.BeginFight();
            flow.ReportRound(1, false, true);
            Assert.AreEqual(BBFlowScreen.MatchEnd, flow.Screen);
            Assert.AreEqual("winner", flow.PortraitHook);
            flow.Rematch();
            Assert.AreEqual(BBFlowScreen.Versus, flow.Screen);
            flow.ReturnToCharacters();
            Assert.AreEqual(BBFlowScreen.CharacterSelect, flow.Screen);
            flow.ReturnToMenu();
            Assert.AreEqual(BBFlowScreen.Title, flow.Screen);
        }

        static BBCharacterPresentation LoadCard(string id)
        {
            string[] guids = AssetDatabase.FindAssets("BBPresentation_" + id + " t:BBCharacterPresentation");
            for (int i = 0; i < guids.Length; i++)
            {
                var card = AssetDatabase.LoadAssetAtPath<BBCharacterPresentation>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (card != null && card.characterId == id) return card;
            }
            return null;
        }
    }
}
#endif
