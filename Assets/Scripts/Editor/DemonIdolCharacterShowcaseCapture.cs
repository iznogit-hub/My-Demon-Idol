using UnityEngine;
using UnityEditor;
using System.IO;

public static class DemonIdolCharacterShowcaseCapture
{
    private const string OUTPUT_DIR = "/Users/amber/.gemini/antigravity/brain/dd77d1a5-afbd-4c73-9398-88a8ea41254c";

    // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Verification/Capture Current Roster Showcase", priority = 10)]
    public static void CaptureRosterShowcase()
    {
        string[] testPrefabs = new string[]
        {
            "Assets/Prefabs/Characters/C02_Aika.prefab",
            "Assets/Prefabs/Characters/C01_Akio.prefab",
            "Assets/Prefabs/Characters/C12_Fina.prefab"
        };

        Vector3 studioPos = new Vector3(500f, 500f, 500f);

        var studio = new GameObject("__TempShowcaseRig");
        studio.transform.position = studioPos;

        var camGo = new GameObject("ShowcaseCamera");
        camGo.transform.SetParent(studio.transform);
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.12f, 0.15f, 1f);
        cam.fieldOfView = 30f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;

        var lightGo = new GameObject("StudioLight");
        lightGo.transform.SetParent(studio.transform);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1.0f, 0.98f, 0.95f);
        light.intensity = 1.1f;
        lightGo.transform.rotation = Quaternion.Euler(20f, -30f, 0f);

        var rt = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;

        foreach (var path in testPrefabs)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            string charName = Path.GetFileNameWithoutExtension(path);
            // Face camera directly (rotation 0, 0, 0)
            var inst = Object.Instantiate(prefab, studioPos, Quaternion.identity, studio.transform);

            // Full body front capture
            camGo.transform.position = studioPos + new Vector3(0f, 1.0f, 3.2f);
            camGo.transform.LookAt(studioPos + new Vector3(0f, 0.95f, 0f));

            cam.Render();

            RenderTexture.active = rt;
            var texFull = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
            texFull.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
            texFull.Apply();
            byte[] bytesFull = texFull.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(OUTPUT_DIR, $"showcase_{charName}_front_full.png"), bytesFull);
            Object.DestroyImmediate(texFull);

            // Torso / uniform front close-up capture
            camGo.transform.position = studioPos + new Vector3(0f, 1.15f, 1.5f);
            camGo.transform.LookAt(studioPos + new Vector3(0f, 1.05f, 0f));

            cam.Render();

            var texTorso = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
            texTorso.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
            texTorso.Apply();
            byte[] bytesTorso = texTorso.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(OUTPUT_DIR, $"showcase_{charName}_front_torso.png"), bytesTorso);
            Object.DestroyImmediate(texTorso);

            RenderTexture.active = null;
            Object.DestroyImmediate(inst);
            Debug.Log($"[ShowcaseCapture] Captured front showcase for {charName}");
        }

        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(studio);

        AssetDatabase.Refresh();
        Debug.Log("[ShowcaseCapture] All front verification captures saved to artifacts directory!");
    }
}
