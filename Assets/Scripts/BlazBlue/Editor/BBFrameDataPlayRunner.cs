#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DemonIdol.BlazBlue.Editor
{
    public static class BBFrameDataPlayRunner
    {
        [MenuItem("Tools/MDI/Run Frame Data Play Mode Verification")]
        public static void RunVerification()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[BBFrameDataPlayRunner] Already in Play Mode. Exit Play Mode before launching tests.");
                return;
            }

            Debug.Log("[BBFrameDataPlayRunner] Starting Frame Data Play Mode Verification...");
            EditorPrefs.SetBool("MDI_FrameData_Run", true);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/MainMenu.unity");
            EditorApplication.isPlaying = true;
        }
    }
}
#endif