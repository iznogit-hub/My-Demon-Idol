#if UNITY_EDITOR
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Verification tools may touch a scene in memory. They must not write ArcadeArena
    /// or any other shipped scene. This reloads the on-disk copy and clears the dirty flag.
    /// </summary>
    public static class ShippedSceneGuard
    {
        public static void DiscardEdits(string scenePath)
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isDirty)
                return;

            // Only reload the scene this tool was working on. Clearing the flag
            // without a reload would leave the mutated objects in memory.
            if (!string.IsNullOrEmpty(scenePath) && !string.IsNullOrEmpty(scene.path) && scene.path != scenePath)
            {
                UnityEngine.Debug.LogError("[ShippedSceneGuard] " + scene.path + " is dirty. Not saving it.");
                return;
            }

            string path = string.IsNullOrEmpty(scene.path) ? scenePath : scene.path;
            if (!ClearDirtiness(scene))
            {
                UnityEngine.Debug.LogError("[ShippedSceneGuard] Could not clear the dirty flag on " + path + ". Not saving. Decline any save prompt and reload the scene.");
                return;
            }

            if (!string.IsNullOrEmpty(path))
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }

        public static bool ClearDirtiness(Scene scene)
        {
            MethodInfo method = typeof(EditorSceneManager).GetMethod(
                "ClearSceneDirtiness",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
                return false;
            method.Invoke(null, new object[] { scene });
            return !scene.isDirty;
        }
    }
}
#endif
