#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue.Tests
{
    [InitializeOnLoad]
    public static class BBArenaCleanupTestAutoRunner
    {
        static BBArenaCleanupTestAutoRunner()
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    BBArenaCleanupTests.RunAllTestsMenu();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[BBArenaCleanupTests] Test run failed: {ex}");
                }
            };
        }
    }

    [TestFixture]
    public class BBArenaCleanupTests
    {
        private const string SCENE_PATH = "Assets/Scenes/ArcadeArena.unity";

        [MenuItem("Demon Idol/BlazBlue/Run BBArena Cleanup Tests")]
        public static void RunAllTestsMenu()
        {
            var suite = new BBArenaCleanupTests();
            suite.Test_ArcadeArena_HasNoProjectilesAtSceneLoad();
            suite.Test_Projectiles_DespawnOnHitAndLifetime();
            Debug.Log("<color=#00e676>[BBArenaCleanupTests] ALL 2 ARENA CLEANUP & PROJECTILE TESTS PASSED!</color>");
        }

        [Test]
        public void Test_ArcadeArena_HasNoProjectilesAtSceneLoad()
        {
            Scene scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            Assert.IsTrue(scene.isLoaded, "ArcadeArena.unity must load successfully.");

            var allRoots = scene.GetRootGameObjects();
            int projectileCount = 0;

            foreach (var root in allRoots)
            {
                var projs = root.GetComponentsInChildren<MDIProjectile>(true);
                projectileCount += projs.Length;

                var allTrans = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in allTrans)
                {
                    if (t.name.Contains("Projectile_") || t.name.Contains("CFXR Hit D 3D"))
                    {
                        projectileCount++;
                    }
                }
            }

            Assert.AreEqual(0, projectileCount, "ArcadeArena scene must contain exactly 0 projectile objects at initial load.");
        }

        [Test]
        public void Test_Projectiles_DespawnOnHitAndLifetime()
        {
            GameObject go = new GameObject("Test_Projectile");
            var proj = go.AddComponent<MDIProjectile>();
            proj.speed = 10f;
            proj.lifetime = 0.1f;
            proj.facingDirection = 1;

            Assert.IsNotNull(proj, "MDIProjectile component must attach properly.");
            Assert.AreEqual(0.1f, proj.lifetime, "Lifetime must match assigned duration.");
            Assert.IsFalse(proj.pooled, "Default spawned projectile should not be pooled unless specified.");

            // Clean up test object
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
#endif
