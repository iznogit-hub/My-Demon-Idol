#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEngine;
using DemonIdol.Audio;
using DemonIdol.BlazBlue;

namespace DemonIdol.BlazBlue.Tests
{
    [TestFixture]
    public class CombatAudioDirectorTests
    {
        public static void RunAllTestsMenu()
        {
            var suite = new CombatAudioDirectorTests();
            suite.Test_EnsureSources_CreatesDistinctMusicAndSfxSources();
            suite.Test_FulfillMissingClips_GuaranteesNoNullClips();
            suite.Test_AudioTriggers_ExecuteWithoutExceptions();
            suite.Test_MusicSwitching_StartsPlaybackAndUpdatesClip();
            suite.Test_BlazBlueCombatPresentation_AudioRouting();
            Debug.Log("<color=#00e676>[CombatAudioDirectorTests] ALL COMBAT AUDIO DIRECTOR TESTS PASSED!</color>");
        }

        [Test]
        public void Test_EnsureSources_CreatesDistinctMusicAndSfxSources()
        {
            var go = new GameObject("Test_AudioDirectorHost");
            try
            {
                var director = go.AddComponent<CombatAudioDirector>();
                director.EnsureSources();

                Assert.IsNotNull(director.MusicSource, "MusicSource must not be null.");
                Assert.IsNotNull(director.SfxSource, "SfxSource must not be null.");
                Assert.AreNotSame(director.MusicSource, director.SfxSource, "MusicSource and SfxSource must be distinct AudioSource instances.");
                Assert.IsTrue(director.MusicSource.loop, "MusicSource should be set to loop.");
                Assert.IsFalse(director.SfxSource.loop, "SfxSource should not be set to loop.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Test_FulfillMissingClips_GuaranteesNoNullClips()
        {
            var go = new GameObject("Test_AudioDirectorFulfill");
            try
            {
                var director = go.AddComponent<CombatAudioDirector>();
                director.EnsureSources();
                director.FulfillMissingClips();

                Assert.IsNotNull(director.HitLightClip, "HitLightClip must be fulfilled.");
                Assert.IsNotNull(director.HitMediumClip, "HitMediumClip must be fulfilled.");
                Assert.IsNotNull(director.HitHeavyClip, "HitHeavyClip must be fulfilled.");
                Assert.IsNotNull(director.BlockClip, "BlockClip must be fulfilled.");
                Assert.IsNotNull(director.ParryClip, "ParryClip must be fulfilled.");
                Assert.IsNotNull(director.RapidCancelClip, "RapidCancelClip must be fulfilled.");
                Assert.IsNotNull(director.DomainActivationClip, "DomainActivationClip must be fulfilled.");
                Assert.IsNotNull(director.SwingLightClip, "SwingLightClip must be fulfilled.");
                Assert.IsNotNull(director.SwingHeavyClip, "SwingHeavyClip must be fulfilled.");
                Assert.IsNotNull(director.Round1Clip, "Round1Clip must be fulfilled.");
                Assert.IsNotNull(director.Round2Clip, "Round2Clip must be fulfilled.");
                Assert.IsNotNull(director.FightClip, "FightClip must be fulfilled.");
                Assert.IsNotNull(director.VictoryClip, "VictoryClip must be fulfilled.");
                Assert.IsNotNull(director.BgmFightTrack, "BgmFightTrack must be fulfilled.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Test_AudioTriggers_ExecuteWithoutExceptions()
        {
            var go = new GameObject("Test_AudioDirectorTriggers");
            try
            {
                var director = go.AddComponent<CombatAudioDirector>();
                director.EnsureSources();
                director.FulfillMissingClips();

                Assert.DoesNotThrow(() => director.PlayHitLight());
                Assert.DoesNotThrow(() => director.PlayHitMedium());
                Assert.DoesNotThrow(() => director.PlayHitHeavy());
                Assert.DoesNotThrow(() => director.PlayBlock());
                Assert.DoesNotThrow(() => director.PlayParry());
                Assert.DoesNotThrow(() => director.PlayRapidCancel());
                Assert.DoesNotThrow(() => director.PlayDomainActivation());
                Assert.DoesNotThrow(() => director.PlayFootstep());
                Assert.DoesNotThrow(() => director.PlaySwingLight());
                Assert.DoesNotThrow(() => director.PlaySwingHeavy());
                Assert.DoesNotThrow(() => director.PlayLand());
                Assert.DoesNotThrow(() => director.PlayButtonClick());
                Assert.DoesNotThrow(() => director.PlayButtonHover());
                Assert.DoesNotThrow(() => director.PlayProjectileFire());
                Assert.DoesNotThrow(() => director.PlayProjectileImpact());
                Assert.DoesNotThrow(() => director.PlayRound1());
                Assert.DoesNotThrow(() => director.PlayRound2());
                Assert.DoesNotThrow(() => director.PlayFinalRound());
                Assert.DoesNotThrow(() => director.PlayFight());
                Assert.DoesNotThrow(() => director.PlayVictorySound());
                Assert.DoesNotThrow(() => director.PlayVoiceGruntLight());
                Assert.DoesNotThrow(() => director.PlayVoiceGruntHeavy());
                Assert.DoesNotThrow(() => director.PlayVoiceDomainSuper());
                Assert.DoesNotThrow(() => director.PlayVoiceKODefeat());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Test_MusicSwitching_StartsPlaybackAndUpdatesClip()
        {
            var go = new GameObject("Test_AudioDirectorMusic");
            try
            {
                var director = go.AddComponent<CombatAudioDirector>();
                director.EnsureSources();
                director.FulfillMissingClips();

                Assert.DoesNotThrow(() => director.PlayCombatMusic());
                Assert.IsNotNull(director.MusicSource.clip, "MusicSource must have a valid clip assigned after PlayCombatMusic.");
                Assert.IsTrue(director.MusicSource.loop, "Combat music must loop.");

                Assert.DoesNotThrow(() => director.PlayMenuMusic());
                Assert.IsNotNull(director.MusicSource.clip, "MusicSource must have a valid clip assigned after PlayMenuMusic.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Test_BlazBlueCombatPresentation_AudioRouting()
        {
            // Verify BBContactPresentation hit, guard, swing and whiff can be invoked safely
            BBContactPresentation.ResetCounters();
            Assert.DoesNotThrow(() => typeof(BBContactPresentation).GetMethod("OnCleanHit")?.Invoke(null, new object[] { Vector3.zero, null, false, false }));
            Assert.DoesNotThrow(() => typeof(BBContactPresentation).GetMethod("OnCleanHit")?.Invoke(null, new object[] { Vector3.zero, null, true, true }));
            Assert.DoesNotThrow(() => BBContactPresentation.OnGuard(Vector3.zero, false, false));
            Assert.DoesNotThrow(() => BBContactPresentation.OnGuard(Vector3.zero, true, true));
            Assert.DoesNotThrow(() => BBContactPresentation.OnWhiff());
            Assert.DoesNotThrow(() => BBContactPresentation.OnAttackSwing(null));
        }
    }
}
#endif
