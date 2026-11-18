using NUnit.Framework;

namespace DemonIdol.BlazBlue.Tests
{
    public class BBEditModeTests
    {
        [Test]
        public void MotionsChordsCancelsDamageHitstunCollisionAndDeterminism()
        {
            var fails = BBCoreChecks.Run();
            Assert.IsEmpty(fails, string.Join("\n", fails));
        }

        [Test]
        public void ClassicSchemeStaysTheDefault()
        {
            Assert.AreEqual(0, (int)ControlScheme.Classic3Button);
            Assert.AreEqual(1, (int)ControlScheme.BlazBlue4Button);
            Assert.IsFalse(BBTuning.Default().enableAstral);
        }
    }
}
