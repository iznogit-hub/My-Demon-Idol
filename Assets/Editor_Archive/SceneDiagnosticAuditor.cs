#if MDI_ARCHIVE
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DemonIdol.Editor
{
    public static class SceneDiagnosticAuditor
    {
        public static readonly string[] TargetScenes = new string[]
        {
            "Assets/Scenes/SampleScene.unity",
            "Assets/Scenes/RosterShowcase.unity",
            "Assets/Scenes/UniversalFighter_Demo.unity"
        };

        public class AuditReport
        {
            public string scenePath;
            public int totalGameObjects = 0;
            public int totalComponents = 0;
            public List<string> missingScripts = new List<string>();
            public List<string> missingPrefabs = new List<string>();
            public List<string> missingSerializedReferences = new List<string>();
            public List<string> missingMaterials = new List<string>();
            public List<string> brokenButtonListeners = new List<string>();
            public List<string> specificIssues = new List<string>();
            public List<string> passedChecks = new List<string>();
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Audit All Three Scenes")]
        public static void RunAuditBatch()
        {
            Debug.Log("=================================================================");
            Debug.Log("   STARTING DEEP DIAGNOSTIC AUDIT OF THE THREE CORE SCENES       ");
            Debug.Log("=================================================================");

            var reports = new List<AuditReport>();

            foreach (var scenePath in TargetScenes)
            {
                if (!File.Exists(scenePath))
                {
                    Debug.LogError($"[SCENE MISSING] Scene file not found: {scenePath}");
                    continue;
                }

                AuditReport report = AuditScene(scenePath);
                reports.Add(report);
            }

            Debug.Log("\n=================================================================");
            Debug.Log("                   SCENE AUDIT SUMMARY REPORT                    ");
            Debug.Log("=================================================================");

            int totalIssues = 0;
            foreach (var r in reports)
            {
                int sceneIssues = r.missingScripts.Count + r.missingPrefabs.Count +
                                  r.missingSerializedReferences.Count + r.missingMaterials.Count +
                                  r.brokenButtonListeners.Count + r.specificIssues.Count;
                totalIssues += sceneIssues;

                Debug.Log($"\n>>> SCENE: {r.scenePath}");
                Debug.Log($"    GameObjects: {r.totalGameObjects} | Components: {r.totalComponents}");

                if (sceneIssues == 0)
                {
                    Debug.Log($"    STATUS: PERFECT CLEAN (0 issues)");
                }
                else
                {
                    Debug.LogWarning($"    STATUS: {sceneIssues} ISSUES FOUND");
                }

                foreach (var s in r.passedChecks) Debug.Log($"      [OK] {s}");
                foreach (var err in r.missingScripts) Debug.LogError($"      [MISSING SCRIPT] {err}");
                foreach (var err in r.missingPrefabs) Debug.LogError($"      [MISSING PREFAB] {err}");
                foreach (var err in r.missingSerializedReferences) Debug.LogError($"      [DANGLING REF] {err}");
                foreach (var err in r.missingMaterials) Debug.LogError($"      [MISSING MATERIAL] {err}");
                foreach (var err in r.brokenButtonListeners) Debug.LogError($"      [BROKEN BUTTON] {err}");
                foreach (var err in r.specificIssues) Debug.LogWarning($"      [LOGIC GAP] {err}");
            }

            Debug.Log("\n=================================================================");
            if (totalIssues == 0)
            {
                Debug.Log($"AUDIT COMPLETED: ALL 3 SCENES 100% CLEAN AND READY!");
            }
            else
            {
                Debug.LogWarning($"AUDIT COMPLETED: FOUND {totalIssues} TOTAL ISSUES ACROSS SCENES.");
            }
            Debug.Log("=================================================================");
        }

        private static AuditReport AuditScene(string scenePath)
        {
            var report = new AuditReport { scenePath = scenePath };

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                report.specificIssues.Add("Scene failed to open or is not valid.");
                return report;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            var allObjects = new List<GameObject>();
            foreach (var root in roots)
            {
                allObjects.Add(root);
                GetChildrenRecursive(root.transform, allObjects);
            }

            report.totalGameObjects = allObjects.Count;

            foreach (var go in allObjects)
            {
                string goPath = GetHierarchyPath(go.transform);

                // 1. Check Missing Prefab
                if (PrefabUtility.IsPartOfAnyPrefab(go))
                {
                    var status = PrefabUtility.GetPrefabInstanceStatus(go);
                    if (status == PrefabInstanceStatus.MissingAsset)
                    {
                        report.missingPrefabs.Add($"{goPath} (Missing Source Prefab Asset)");
                    }
                }

                // 2. Check Missing Scripts (null components)
                Component[] components = go.GetComponents<Component>();
                for (int cIndex = 0; cIndex < components.Length; cIndex++)
                {
                    Component comp = components[cIndex];
                    report.totalComponents++;

                    if (comp == null)
                    {
                        report.missingScripts.Add($"{goPath} [Component index {cIndex} is NULL (Missing Script)]");
                        continue;
                    }

                    // 3. Check Serialized Object References for dangling/missing assets
                    SerializedObject so = new SerializedObject(comp);
                    SerializedProperty prop = so.GetIterator();
                    while (prop.NextVisible(true))
                    {
                        if (prop.propertyType == SerializedPropertyType.ObjectReference)
                        {
#if UNITY_6000_0_OR_NEWER
                            if (prop.objectReferenceEntityIdValue != UnityEngine.EntityId.None && prop.objectReferenceValue == null)
                            {
                                report.missingSerializedReferences.Add($"{goPath} -> {comp.GetType().Name}.{prop.propertyPath} (Dangling Reference Entity: {prop.objectReferenceEntityIdValue})");
                            }
#else
#pragma warning disable CS0619
                            if (prop.objectReferenceInstanceIDValue != 0 && prop.objectReferenceValue == null)
                            {
                                report.missingSerializedReferences.Add($"{goPath} -> {comp.GetType().Name}.{prop.propertyPath} (Dangling Reference ID: {prop.objectReferenceInstanceIDValue})");
                            }
#pragma warning restore CS0619
#endif
                        }
                    }

                    // 4. Check Renderers for Missing Materials
                    if (comp is Renderer rend)
                    {
                        Material[] mats = rend.sharedMaterials;
                        for (int m = 0; m < mats.Length; m++)
                        {
                            if (mats[m] == null)
                            {
                                report.missingMaterials.Add($"{goPath} -> {rend.GetType().Name} Material[{m}] is null/missing");
                            }
                        }
                    }

                    // 5. Check Buttons for broken persistent listeners
                    if (comp is Button btn)
                    {
                        int eventCount = btn.onClick.GetPersistentEventCount();
                        for (int e = 0; e < eventCount; e++)
                        {
                            UnityEngine.Object target = btn.onClick.GetPersistentTarget(e);
                            string methodName = btn.onClick.GetPersistentMethodName(e);
                            if (target == null && !string.IsNullOrEmpty(methodName))
                            {
                                report.brokenButtonListeners.Add($"{goPath} Button.onClick[{e}] calls '{methodName}' on NULL target");
                            }
                        }
                    }
                }
            }

            // Scene-specific domain validation
            RunSceneSpecificChecks(scenePath, allObjects, report);

            return report;
        }

        private static void RunSceneSpecificChecks(string scenePath, List<GameObject> allObjects, AuditReport report)
        {
            if (scenePath.Contains("SampleScene"))
            {
                CheckSampleScene(allObjects, report);
            }
            else if (scenePath.Contains("RosterShowcase"))
            {
                CheckRosterShowcase(allObjects, report);
            }
            else if (scenePath.Contains("UniversalFighter_Demo"))
            {
                CheckUniversalFighterDemo(allObjects, report);
            }
        }

        private static void CheckSampleScene(List<GameObject> allObjects, AuditReport report)
        {
            // Check Camera
            var cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (cam != null) report.passedChecks.Add($"Main Camera found: {cam.name}");
            else report.specificIssues.Add("No Camera found in SampleScene");

            // Check Canvas & EventSystem
            var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas != null) report.passedChecks.Add($"Master UI Canvas found: {canvas.name}");
            else report.specificIssues.Add("No Canvas found in SampleScene");

            var es = UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es != null) report.passedChecks.Add($"EventSystem found: {es.name}");
            else report.specificIssues.Add("No EventSystem found in SampleScene");

            // Check UI Controller or Navigation
            var uiController = UnityEngine.Object.FindAnyObjectByType<DemonIdol.UI.BloodTechUIManager>();
            if (uiController != null)
            {
                report.passedChecks.Add("BloodTechUIManager present");
            }
            else
            {
                report.specificIssues.Add("BloodTechUIManager not found in SampleScene");
            }
        }

        private static void CheckRosterShowcase(List<GameObject> allObjects, AuditReport report)
        {
            var showcaseCtrl = UnityEngine.Object.FindAnyObjectByType<DemonIdol.Characters.MDI_RosterShowcaseController>();
            if (showcaseCtrl != null)
            {
                report.passedChecks.Add("MDI_RosterShowcaseController found");
                if (showcaseCtrl.fighterDefinitions == null || showcaseCtrl.fighterDefinitions.Count == 0)
                {
                    report.specificIssues.Add("MDI_RosterShowcaseController.fighterDefinitions is empty");
                }
                else
                {
                    report.passedChecks.Add($"MDI_RosterShowcaseController has {showcaseCtrl.fighterDefinitions.Count} fighter definitions");
                }

                if (showcaseCtrl.showcaseCamera == null)
                {
                    report.specificIssues.Add("MDI_RosterShowcaseController.showcaseCamera is null");
                }
                else
                {
                    report.passedChecks.Add("Showcase Camera wired");
                }
            }
            else
            {
                report.specificIssues.Add("MDI_RosterShowcaseController missing from RosterShowcase scene");
            }
        }

        private static void CheckUniversalFighterDemo(List<GameObject> allObjects, AuditReport report)
        {
            // Check CombatManager
            var combatGo = GameObject.Find("CombatManager");
            if (combatGo != null)
            {
                report.passedChecks.Add("CombatManager GameObject found");
                var tickEngine = combatGo.GetComponent<DemonIdol.Combat.CombatTickEngine>();
                if (tickEngine != null) report.passedChecks.Add("CombatTickEngine present");
                else report.specificIssues.Add("CombatTickEngine missing from CombatManager");

                var audioDir = combatGo.GetComponent<DemonIdol.Audio.CombatAudioDirector>();
                if (audioDir != null) report.passedChecks.Add("CombatAudioDirector present");
                else report.specificIssues.Add("CombatAudioDirector missing from CombatManager");

                var hitVfx = combatGo.GetComponent<DemonIdol.Combat.HitEffectCoordinator>();
                if (hitVfx != null) report.passedChecks.Add("HitEffectCoordinator present");
                else report.specificIssues.Add("HitEffectCoordinator missing from CombatManager");
            }
            else
            {
                report.specificIssues.Add("CombatManager GameObject missing");
            }

            // Check Fighters
            var fighters = UnityEngine.Object.FindObjectsByType<DemonIdol.Combat.FighterController>(FindObjectsSortMode.None);
            if (fighters != null && fighters.Length >= 2)
            {
                report.passedChecks.Add($"Found {fighters.Length} FighterControllers in arena (P1 & P2)");
                foreach (var f in fighters)
                {
                    if (f.lightAttackData == null) report.specificIssues.Add($"{f.name} missing lightAttackData");
                    if (f.mediumAttackData == null) report.specificIssues.Add($"{f.name} missing mediumAttackData");
                    if (f.heavyAttackData == null) report.specificIssues.Add($"{f.name} missing heavyAttackData");
                    if (f.GetComponent<CharacterController>() == null) report.specificIssues.Add($"{f.name} missing CharacterController");
                }
            }
            else
            {
                report.specificIssues.Add($"Expected at least 2 FighterControllers, found {fighters?.Length ?? 0}");
            }

            // Check Battle HUD
            var battleHud = UnityEngine.Object.FindAnyObjectByType<DemonIdol.UI.BattleHUDController>(FindObjectsInactive.Include);
            if (battleHud != null) report.passedChecks.Add("BattleHUDController present");
            else report.specificIssues.Add("BattleHUDController missing from arena");

            // Check Camera
            var cam = UnityEngine.Object.FindAnyObjectByType<DemonIdol.Combat.FighterCamera2D>(FindObjectsInactive.Include);
            if (cam != null) report.passedChecks.Add("FighterCamera2D present");
            else report.specificIssues.Add("FighterCamera2D missing from arena");

            // Check Ground
            var ground = GameObject.Find("Ground_StoneBridge");
            if (ground != null && ground.GetComponent<Collider>() != null)
            {
                report.passedChecks.Add("Ground_StoneBridge with Collider present");
            }
            else
            {
                report.specificIssues.Add("Ground_StoneBridge missing or missing Collider");
            }
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Fix All Dangling References Across Scenes")]
        public static void FixAllScenesMasterUI()
        {
            string originalScene = EditorSceneManager.GetActiveScene().path;
            foreach (var scenePath in TargetScenes)
            {
                if (!File.Exists(scenePath)) continue;
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (GameObject.Find("Master_BloodTech_Canvas") != null || scenePath.Contains("UniversalFighter") || scenePath.Contains("SampleScene"))
                {
                    DemonIdolUIBuilder.BuildMasterUI();
                    if (scenePath.Contains("UniversalFighter")) DemonIdolArenaBuilder.BuildArenaInScene();
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            if (!string.IsNullOrEmpty(originalScene) && File.Exists(originalScene))
            {
                EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#00e676>Fixed all scenes Master UI Canvas references!</color>");
        }

        private static void GetChildrenRecursive(Transform parent, List<GameObject> list)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                list.Add(child.gameObject);
                GetChildrenRecursive(child, list);
            }
        }

        private static string GetHierarchyPath(Transform t)
        {
            if (t.parent == null) return "/" + t.name;
            return GetHierarchyPath(t.parent) + "/" + t.name;
        }
    }
}
#endif

#endif
