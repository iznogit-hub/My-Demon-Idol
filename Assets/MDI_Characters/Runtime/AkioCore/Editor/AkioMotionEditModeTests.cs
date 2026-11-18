using NUnit.Framework;

namespace MDI.Akio.Tests
{
    public class AkioMotionEditModeTests
    {
        static MotionEdge E(int h, int v, int tick) => new MotionEdge(h, v, tick);

        [Test]
        public void QuarterCircleForward_AcceptsDownThenForward_WithoutDiagonal()
        {
            var edges = new[] { E(0, -1, 10), E(1, 0, 18) };
            Assert.IsTrue(MotionMatcher.Is236(edges, edges.Length, 20, 1));
        }

        [Test]
        public void QuarterCircleForward_AcceptsFull236()
        {
            var edges = new[] { E(0, -1, 10), E(1, -1, 14), E(1, 0, 18) };
            Assert.IsTrue(MotionMatcher.Is236(edges, edges.Length, 20, 1));
        }

        [Test]
        public void QuarterCircleForward_AllowsNeutralAfterTheForward()
        {
            var edges = new[] { E(0, -1, 10), E(1, 0, 14), E(0, 0, 18) };
            Assert.IsTrue(MotionMatcher.Is236(edges, edges.Length, 20, 1));
            var reversed = new[] { E(0, -1, 10), E(1, 0, 14), E(-1, 0, 18) };
            Assert.IsFalse(MotionMatcher.Is236(reversed, reversed.Length, 20, 1));
        }

        [Test]
        public void QuarterCircleForward_RejectsStaleWindow()
        {
            var edges = new[] { E(0, -1, 1), E(1, 0, 8) };
            Assert.IsFalse(MotionMatcher.Is236(edges, edges.Length, 40, 1));
        }

        [Test]
        public void QuarterCircleForward_FacingLeft_UsesWorldLeftAsForward()
        {
            var edges = new[] { E(0, -1, 10), E(-1, 0, 16) };
            Assert.IsTrue(MotionMatcher.Is236(edges, edges.Length, 18, -1));
            Assert.IsFalse(MotionMatcher.Is236(edges, edges.Length, 18, 1));
        }

        [Test]
        public void DragonPunch_RequiresForwardThenDownThenDownForward()
        {
            var edges = new[] { E(1, 0, 8), E(0, -1, 12), E(1, -1, 16) };
            Assert.IsTrue(MotionMatcher.Is623(edges, edges.Length, 18, 1));
            Assert.IsFalse(MotionMatcher.Is623(new[] { E(0, -1, 8), E(1, -1, 12), E(1, 0, 16) }, 3, 18, 1));
        }

        [Test]
        public void QuarterCircleBack_Accepts214()
        {
            var edges = new[] { E(0, -1, 5), E(-1, -1, 9), E(-1, 0, 13) };
            Assert.IsTrue(MotionMatcher.Is214(edges, edges.Length, 15, 1));
        }

        [Test]
        public void Dash_RequiresTwoForwardsInsideLenientWindow()
        {
            var edges = new[] { E(1, 0, 4), E(0, 0, 8), E(1, 0, 14) };
            Assert.IsTrue(MotionMatcher.IsDoubleTap(edges, edges.Length, 16, 1, true));
            Assert.IsTrue(MotionMatcher.IsDoubleTap(new[] { E(-1, 0, 3), E(0, 0, 7), E(-1, 0, 12) }, 3, 14, 1, false));
            Assert.IsFalse(MotionMatcher.IsDoubleTap(new[] { E(1, 0, 1) }, 1, 4, 1, true));
        }

        [Test]
        public void Charge_IsOptionalAndNeedsAFortyTickBackBeforeForward()
        {
            var edges = new[] { E(-1, 0, 10), E(1, 0, 55) };
            Assert.IsTrue(MotionMatcher.IsChargeBackForward(edges, edges.Length, 60, 1));
            var shortHold = new[] { E(-1, 0, 40), E(1, 0, 55) };
            Assert.IsFalse(MotionMatcher.IsChargeBackForward(shortHold, shortHold.Length, 60, 1));
        }

        [Test]
        public void Cancel_GatlingOnHitAndBlock_SpecialsDoNotReverse()
        {
            var onHit = new[] { "5M", "6S", "super" };
            var onBlock = new[] { "5M", "6S" };
            Assert.IsTrue(CancelQuery.Allows(onHit, onBlock, "5M", true, false));
            Assert.IsTrue(CancelQuery.Allows(onHit, onBlock, "6S", false, true));
            Assert.IsFalse(CancelQuery.Allows(onHit, onBlock, "super", false, true));
            Assert.IsFalse(CancelQuery.Allows(onHit, onBlock, "5P", true, true));
            Assert.IsTrue(CancelQuery.InWindow(10, 6, 15));
            Assert.IsFalse(CancelQuery.InWindow(4, 6, 15));
        }

        [Test]
        public void ChainIds_DefaultToAkiosMergedRoute_UntilAMovesetOverridesThem()
        {
            Assert.AreEqual("5M", CancelQuery.FirstChainId(null));
            Assert.AreEqual("5M", CancelQuery.FirstChainId(""));
            Assert.AreEqual("rekka1", CancelQuery.SecondChainId(null));
            Assert.AreEqual("rekka2", CancelQuery.ThirdChainId(""));
            Assert.AreEqual("5K", CancelQuery.FirstChainId("5K"));
        }
    }
}
