#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DemonIdol.BlazBlue.Tests
{
    [TestFixture]
    public class ClassicRegressionTests
    {
        public static void RunAllTestsMenu()
        {
            var suite = new ClassicRegressionTests();
            suite.Test_GoldenMatch1_AkioNeutralMobility();
            suite.Test_GoldenMatch2_AkioNormalsChain();
            suite.Test_GoldenMatch3_AkioSpecialsSuper();
            suite.Test_GoldenMatch4_AkioVsAikaSkirmish();
            suite.Test_GoldenMatch5_AikaVsUnityChanCombat();
            suite.Test_GoldenMatch6_PicoChanThrowsAndBurst();
            BBMotionMatcherTests.RunAllTestsMenu();
            BBCollisionTests.RunAllTestsMenu();
            BBSubsystemTests.RunAllTestsMenu();
            BBAkioTests.RunAllTestsMenu();
            Debug.Log("<color=#00e676>[ClassicRegressionTests] ALL 6 GOLDEN REPLAY TESTS PASSED!</color>");
        }

        private void AssertGoldenReplay(string scenarioName)
        {
            string path = Path.Combine(ClassicMatchRecorder.GOLDEN_DIR, $"{scenarioName}.classicrec");
            if (!File.Exists(path))
            {
                // Generate if missing
                ClassicMatchRecorder.RecordAllGoldenMatches();
            }

            Assert.IsTrue(File.Exists(path), $"Golden match file missing at: {path}");
            string json = File.ReadAllText(path);
            var log = JsonUtility.FromJson<ClassicMatchLog>(json);
            Assert.IsNotNull(log, $"Failed to deserialize {path}");

            bool match = ClassicMatchRecorder.ReplayMatch(log, out string mismatchDetail);
            Assert.IsTrue(match, mismatchDetail);
        }

        [Test]
        public void Test_GoldenMatch1_AkioNeutralMobility()
        {
            AssertGoldenReplay("match1_akio_neutral_mobility");
        }

        [Test]
        public void Test_GoldenMatch2_AkioNormalsChain()
        {
            AssertGoldenReplay("match2_akio_normals_chain");
        }

        [Test]
        public void Test_GoldenMatch3_AkioSpecialsSuper()
        {
            AssertGoldenReplay("match3_akio_specials_super");
        }

        [Test]
        public void Test_GoldenMatch4_AkioVsAikaSkirmish()
        {
            AssertGoldenReplay("match4_akio_vs_aika_skirmish");
        }

        [Test]
        public void Test_GoldenMatch5_AikaVsUnityChanCombat()
        {
            AssertGoldenReplay("match5_aika_vs_unitychan_combat");
        }

        [Test]
        public void Test_GoldenMatch6_PicoChanThrowsAndBurst()
        {
            AssertGoldenReplay("match6_picochan_throws_and_burst");
        }
    }
}
#endif
