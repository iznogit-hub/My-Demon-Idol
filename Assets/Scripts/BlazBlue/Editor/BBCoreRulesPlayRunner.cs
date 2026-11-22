#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DemonIdol.BlazBlue.Editor
{
    public static class BBCoreRulesPlayRunner
    {
        [MenuItem("Demon Idol/Run Core Rules Play Tests")]
        public static void RunCoreRules()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[BBCoreRulesPlayRunner] Already in Play Mode. Exit Play Mode before launching tests.");
                return;
            }

            Debug.Log("[BBCoreRulesPlayRunner] Starting Core Rules Play Mode Tests...");
            EditorPrefs.SetBool("MDI_CoreRules_Run", true);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/MainMenu.unity");
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
