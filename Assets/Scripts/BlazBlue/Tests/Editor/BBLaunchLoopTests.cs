#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue.Tests
{
    public class BBLaunchLoopTests
    {
        [Test]
        public void EveryFighterHasDistinctDriveSpecialAndSuper()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < BBMatchFlow.Roster.Length; i++)
            {
                string id = BBMatchFlow.Roster[i];
                Assert.IsTrue(BBSignatureTable.TryBase(id, out var signature), id);
                Assert.IsFalse(string.IsNullOrEmpty(signature.driveName), id);
                Assert.IsFalse(string.IsNullOrEmpty(signature.specialId), id);
                Assert.IsFalse(string.IsNullOrEmpty(signature.superId), id);
                Assert.IsFalse(string.IsNullOrEmpty(signature.clipToken), id);
                string key = signature.driveName + "|" + signature.specialId + "|" + signature.superId + "|" + signature.damageScale;
                Assert.IsTrue(seen.Add(key), id + " signature collided " + key);
                Assert.IsTrue(BBSignatureTable.TryGet(id, signature.specialId, out _));
            }
            Assert.IsTrue(BBProjectileCaster.ShouldSpawn("C06", "236A"));
            Assert.IsTrue(BBProjectileCaster.ShouldSpawn("C04", "236A"));
            Assert.IsTrue(BBProjectileCaster.ShouldSpawn("EX05", "236C"));
            Assert.IsTrue(BBProjectileCaster.ShouldSpawn("EX07", "236A"));
            Assert.IsFalse(BBProjectileCaster.ShouldSpawn("C08", "623B"));
            Assert.AreEqual(25, BBMatchFlow.Roster.Length);
        }

        [Test]
        public void HitAndGuardPresentationCounters()
        {
            BBContactPresentation.ResetCounters();
            BBContactPresentation.OnCleanHit(Vector3.zero, null, false, false);
            BBContactPresentation.OnGuard(Vector3.zero, true, false);
            Assert.AreEqual(1, BBContactPresentation.LastHitEvents);
            Assert.AreEqual(1, BBContactPresentation.LastGuardEvents);
            Assert.AreEqual(0, BBContactPresentation.ComboCount);
        }

        [Test]
        public void ProjectileOverlapAndCasterList()
        {
            Assert.IsTrue(BBProjectileCaster.ShouldSpawn("C06", "236A"));
            Assert.IsTrue(BBProjectileCaster.ShouldSpawn("EX05", "236C"));
            Assert.IsFalse(BBProjectileCaster.ShouldSpawn("C06", "5A"));
            Assert.IsFalse(MDIProjectile.Overlaps(null, null));
        }

        [Test]
        public void RoundAndArcadeFlow()
        {
            var flow = new BBMatchFlow();
            flow.SelectMode("Arcade");
            Assert.AreEqual(BBFlowScreen.CharacterSelect, flow.Screen);
            flow.ConfirmCharacters("C01", "C02");
            Assert.AreEqual(BBFlowScreen.Versus, flow.Screen);
            flow.BeginFight();
            flow.ReportRound(1, true, false);
            Assert.AreEqual(BBFlowScreen.RoundEnd, flow.Screen);
            Assert.IsTrue(flow.LastRoundPerfect);
            flow.ContinueAfterRound();
            flow.BeginFight();
            flow.ReportRound(1, false, true);
            Assert.AreEqual(BBFlowScreen.MatchEnd, flow.Screen);
            Assert.AreEqual("C01", flow.WinnerQuoteHook);
            flow.ContinueAfterRound();
            Assert.AreEqual(BBFlowScreen.ArcadeNext, flow.Screen);
            flow.Pause();
            Assert.AreEqual(BBFlowScreen.Pause, flow.Screen);
            flow.Resume();
            Assert.AreEqual(BBFlowScreen.ArcadeNext, flow.Screen);
        }

        [Test]
        public void OnlineModeAndQuantumCodec()
        {
            var flow = new BBMatchFlow();
            flow.SelectMode("Online");
            Assert.AreEqual(BBFlowScreen.Online, flow.Screen);
            var packed = BBQuantumInputCodec.Pack(new BBQuantumInputCodec.Buttons
            {
                Left = true,
                A = true,
                D = true
            });
            var back = BBQuantumInputCodec.Unpack(packed);
            Assert.IsTrue(back.Left && back.A && back.D);
            Assert.IsFalse(back.Right || back.B || back.C || back.Up || back.Down);
        }
    }
}
#endif
