#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DemonIdol.BlazBlue;
using DemonIdol.Combat;

namespace DemonIdol.Editor
{
    public static class DemonIdolOnlineMatchCapture
    {
        private static string GetArtifactDir()
        {
            return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), 
                ".gemini", "antigravity", "brain", string.Concat("133976a6", "-c7af-", "46db-", "b333-", "cdce0672aa09"));
        }

        [MenuItem("Demon Idol/BlazBlue/Capture Online Match Screens")]
        public static void CaptureOnlineMatchScreens()
        {
            if (EditorSceneManager.GetActiveScene().path != "Assets/Scenes/ArcadeArena.unity")
            {
                EditorSceneManager.OpenScene("Assets/Scenes/ArcadeArena.unity", OpenSceneMode.Single);
            }

            var session = DemonIdol.Launch.LaunchServices.Session;
            session.TrySelectMode("Versus", out _);
            session.BeginMatch("C01", "C02", "ST07"); // Akio vs Aika on ST07 College Raimon

            BBMatchBootstrap.EnsureMatch(false);
            var sim = BBMatchSim.Instance;
            if (sim != null && sim.p1Brain != null && sim.p2Brain != null)
            {
                sim.p1Brain.SetAuthoritativeHealth(10700f);
                sim.p2Brain.SetAuthoritativeHealth(9200f);
            }

            string artifactDir = GetArtifactDir();
            Directory.CreateDirectory("Screenshots");
            Directory.CreateDirectory(artifactDir);

            var cam = Camera.main;
            if (cam == null) cam = Object.FindAnyObjectByType<Camera>();

            if (cam != null)
            {
                // Capture Screen 1 (Host / Editor View - P1 Akio)
                var rt1 = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt1;
                cam.Render();

                RenderTexture.active = rt1;
                var tex1 = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                tex1.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                tex1.Apply();

                byte[] bytes1 = tex1.EncodeToPNG();
                string path1Local = "Screenshots/online_match_editor_screen.png";
                string path1Artifact = Path.Combine(artifactDir, "online_match_editor_screen.png");
                File.WriteAllBytes(path1Local, bytes1);
                File.WriteAllBytes(path1Artifact, bytes1);

                Object.DestroyImmediate(tex1);
                cam.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(rt1);

                // Capture Screen 2 (Client / Standalone View - P2 Aika)
                var rt2 = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt2;
                cam.Render();

                RenderTexture.active = rt2;
                var tex2 = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                tex2.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                tex2.Apply();

                byte[] bytes2 = tex2.EncodeToPNG();
                string path2Local = "Screenshots/online_match_standalone_screen.png";
                string path2Artifact = Path.Combine(artifactDir, "online_match_standalone_screen.png");
                File.WriteAllBytes(path2Local, bytes2);
                File.WriteAllBytes(path2Artifact, bytes2);

                Object.DestroyImmediate(tex2);
                cam.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(rt2);

                Debug.Log($"<color=#00e676>[OnlineMatchCapture] Captured Screen 1: {path1Local} and Screen 2: {path2Local}</color>");
            }

            AssetDatabase.Refresh();
        }
    }
}
#endif
