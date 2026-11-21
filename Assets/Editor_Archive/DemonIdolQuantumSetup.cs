#if MDI_ARCHIVE
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using DemonIdol.Network;

namespace DemonIdol.Editor
{
    public static class DemonIdolQuantumSetup
    {
        public const string QUANTUM_APP_ID = "";

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Quantum/Configure Photon Quantum", priority = 20)]
        public static void ConfigureQuantum()
        {
            Debug.Log("[DemonIdolQuantumSetup] Configuring Photon Quantum with App ID: " + QUANTUM_APP_ID);

            // 1. Run Quantum installation of user files if SDK installer exists
            try
            {
                var hubType = Type.GetType("Quantum.Editor.QuantumEditorHubWindowSdk, Quantum.Unity.Editor")
                    ?? Type.GetType("Quantum.Editor.QuantumEditorHubWindowSdk");

                if (hubType != null)
                {
                    var installMethod = hubType.GetMethod("InstallAllUserFiles", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (installMethod != null)
                    {
                        installMethod.Invoke(null, null);
                        Debug.Log("[DemonIdolQuantumSetup] Quantum user files verified via InstallAllUserFiles().");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[DemonIdolQuantumSetup] Quantum user files install notice: " + ex.Message);
            }

            // 2. Configure PhotonServerSettings asset
            var settings = Resources.Load<Quantum.PhotonServerSettings>("PhotonServerSettings");
            if (settings == null)
            {
                string path = "Assets/QuantumUser/Resources/PhotonServerSettings.asset";
                settings = AssetDatabase.LoadAssetAtPath<Quantum.PhotonServerSettings>(path);
            }

            if (settings != null)
            {
                settings.AppSettings.AppIdQuantum = QUANTUM_APP_ID;
                settings.AppSettings.AppIdRealtime = string.Empty; // Only populate AppIdQuantum for Quantum automatic detection
                settings.AppSettings.AppIdFusion = string.Empty;
                settings.AppSettings.UseNameServer = true;
                settings.AppSettings.FixedRegion = "";
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Debug.Log("<color=#00FF66>[DemonIdolQuantumSetup] Successfully updated PhotonServerSettings with AppIdQuantum: " + QUANTUM_APP_ID + "</color>");
            }
            else
            {
                Debug.LogWarning("[DemonIdolQuantumSetup] PhotonServerSettings asset not found in Resources. Will ensure yaml is written.");
            }

            // 3. Force-refresh and populate Quantum Unity DB
            try
            {
                Quantum.Editor.QuantumUnityDBUtilities.RefreshGlobalDB(true);
                Debug.Log("<color=#00FF66>[DemonIdolQuantumSetup] Quantum Unity Asset Database successfully populated & refreshed.</color>");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[DemonIdolQuantumSetup] Quantum Unity DB refresh notice: " + ex.Message);
            }

            // 4. Update active QuantumPvPManager instances in open scene
            WireQuantumSceneObjects();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#00FF66>[DemonIdolQuantumSetup] Quantum Setup & Scene Wiring Complete!</color>");
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Quantum/Wire Quantum In All Scenes", priority = 21)]
        public static void WireQuantumSceneObjects()
        {
            string[] combatScenes = new string[]
            {
                "Assets/Scenes/SampleScene.unity",
                "Assets/Scenes/UniversalFighter_Demo.unity"
            };

            string originalScenePath = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;

            foreach (var scenePath in combatScenes)
            {
                if (!File.Exists(scenePath)) continue;

                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
                WireActiveSceneInternal();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                Debug.Log($"<color=#00FF66>[DemonIdolQuantumSetup] Successfully wired Quantum in {scenePath}!</color>");
            }

            if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(originalScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            }
        }

        public static void WireActiveSceneInternal()
        {
            // 1. Ensure [NetworkGatekeeper]
            var gatekeeper = UnityEngine.Object.FindAnyObjectByType<NetworkGatekeeper>();
            if (gatekeeper == null)
            {
                var go = new GameObject("[NetworkGatekeeper]");
                gatekeeper = go.AddComponent<NetworkGatekeeper>();
                Undo.RegisterCreatedObjectUndo(go, "Create NetworkGatekeeper");
                Debug.Log("[DemonIdolQuantumSetup] Created [NetworkGatekeeper] GameObject.");
            }

            // 2. Ensure [QuantumPvPManager]
            var pvpManager = UnityEngine.Object.FindAnyObjectByType<QuantumPvPManager>();
            if (pvpManager == null)
            {
                var go = new GameObject("[QuantumPvPManager]");
                pvpManager = go.AddComponent<QuantumPvPManager>();
                Undo.RegisterCreatedObjectUndo(go, "Create QuantumPvPManager");
                Debug.Log("[DemonIdolQuantumSetup] Created [QuantumPvPManager] GameObject.");
            }
            pvpManager.quantumAppId = QUANTUM_APP_ID;
            EditorUtility.SetDirty(pvpManager);

            // 3. Ensure [QuantumCombatBridge]
            var bridge = UnityEngine.Object.FindAnyObjectByType<QuantumCombatBridge>();
            if (bridge == null)
            {
                var go = new GameObject("[QuantumCombatBridge]");
                bridge = go.AddComponent<QuantumCombatBridge>();
                Undo.RegisterCreatedObjectUndo(go, "Create QuantumCombatBridge");
                Debug.Log("[DemonIdolQuantumSetup] Created [QuantumCombatBridge] GameObject.");
            }

            // 4. Bind Fighters to QuantumCombatBridge
            var fighters = UnityEngine.Object.FindObjectsByType<DemonIdol.Combat.FighterController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            DemonIdol.Combat.FighterController p1 = null;
            DemonIdol.Combat.FighterController p2 = null;

            foreach (var f in fighters)
            {
                string lower = f.gameObject.name.ToLower();
                if (lower.Contains("kai") || lower.Contains("p1") || lower.Contains("player1"))
                {
                    if (p1 == null) p1 = f;
                }
                else if (lower.Contains("dummy") || lower.Contains("p2") || lower.Contains("player2") || lower.Contains("arin"))
                {
                    if (p2 == null) p2 = f;
                }
            }

            if (p1 == null && fighters.Length > 0) p1 = fighters[0];
            if (p2 == null && fighters.Length > 1) p2 = fighters[1];

            SerializedObject soBridge = new SerializedObject(bridge);
            var p1Prop = soBridge.FindProperty("_fighterViewP1");
            var p2Prop = soBridge.FindProperty("_fighterViewP2");
            if (p1Prop != null) p1Prop.objectReferenceValue = p1;
            if (p2Prop != null) p2Prop.objectReferenceValue = p2;
            soBridge.ApplyModifiedProperties();
            bridge.BindFighters(p1, p2);
            EditorUtility.SetDirty(bridge);

            // 5. Ensure BattleHUDController Netcode HUD
            var hud = UnityEngine.Object.FindAnyObjectByType<DemonIdol.UI.BattleHUDController>(FindObjectsInactive.Include);
            if (hud != null)
            {
                hud.EnsureNetcodeHUD();
                EditorUtility.SetDirty(hud);
            }

            // 6. Ensure Master_BloodTech_Canvas PvPLobbyUI
            var uiMgr = UnityEngine.Object.FindAnyObjectByType<DemonIdol.UI.BloodTechUIManager>(FindObjectsInactive.Include);
            if (uiMgr != null)
            {
                uiMgr.EnsurePvPLobbyModal();
                EditorUtility.SetDirty(uiMgr);
            }
        }
    }
}

#endif
