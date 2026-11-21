#if MDI_ARCHIVE
#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DemonIdol.Combat;
using DemonIdol.Input;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Master Director for Armory Distribution, Ability Icons, Fantasy Shield Display Frames,
    /// Super Virtual Joysticks, Character Mugshots, URP Indoor Template Lighting, and Ace Death Replays.
    /// </summary>
    [InitializeOnLoad]
    public static class DemonIdolArmoryBuilder
    {
        private const string ARMORY_PREFAB_DIR = "Assets/Prefabs/Armory";
        private const string MUGSHOTS_DIR = "Assets/Resources/UI/Portraits/Mugshots";
        private const string ICONS_DIR = "Assets/CombatData/Icons";
        private const string FRAMES_DIR = "Assets/Resources/UI/Frames";
        private const string SETTINGS_DIR = "Assets/Settings";
        private const string INDOOR_PROFILE_PATH = "Assets/Settings/URP_IndoorTemplateVolumeProfile.asset";

        static DemonIdolArmoryBuilder()
        {
            EditorApplication.update += CheckAutoRun;
        }

        private static void CheckAutoRun()
        {
            string trigger = "Temp/run_armory_director.txt";
            if (File.Exists(trigger))
            {
                try { File.Delete(trigger); } catch { }
                DeployAll();
            }
        }

        private static bool s_isDeploying = false;

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Deploy Master Armory And Stage System", priority = 1)]
        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Armory/Deploy All Systems", priority = 1)]
        public static void DeployAll()
        {
            if (s_isDeploying) return;
            try
            {
                s_isDeploying = true;
                Debug.Log("<color=#00e676>=============================================================</color>");
                Debug.Log("<color=#00e676>  DEMON IDOL: MASTER ARMORY, URP INDOOR, MUGSHOTS & REPLAY    </color>");
                Debug.Log("<color=#00e676>=============================================================</color>");

                EnsureDirectories();

                // 2. Configure texture import settings for sprites (Mugshots, Icons, Frames)
                ConfigureAllSpriteImporters();

                // 3. Strip all weapon props and guns from characters (Pure unarmed combat)
                DemonIdolCharacterPropSanitizer.StripAllPropsAndWeaponsFromAllCharacters();

                // 4. Bind Mugshot Sprites into Character Definitions
                BindMugshotsToCharacterDefinitions();

                // 5. Build and Apply URP Indoor Template Volume Profile & Stage Lighting
                SetupURPIndoorTemplateAndStages();

                // 6. Rebuild Master UI Canvas (Mugshots, Shield Frames, Ability Icons, Super Virtual Joysticks)
                DemonIdolUIBuilder.BuildMasterUI();

                // 7. Configure Ace Death Replay Director & Home Scene Combat Directors
                SetupHomeCombatDirectors();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("<color=#00e676><b>[ArmoryBuilder] SUCCESS:</b> Armory, Icons, Frames, Mugshots, URP Indoor & Replays Fully Deployed!</color>");
            }
            finally
            {
                s_isDeploying = false;
            }
        }

        private static void SetupHomeCombatDirectors()
        {
            if (!File.Exists("Assets/Scenes/Home.unity")) return;
            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Home.unity")
            {
                scene = EditorSceneManager.OpenScene("Assets/Scenes/Home.unity", OpenSceneMode.Single);
            }

            // Ensure AceDeathReplayDirector
            var replay = UnityEngine.Object.FindAnyObjectByType<AceDeathReplayDirector>();
            if (replay == null)
            {
                GameObject replayGo = new GameObject("AceDeathReplayDirector");
                replay = replayGo.AddComponent<AceDeathReplayDirector>();
            }

            GameObject p1Go = GameObject.Find("Player1_Akio");
            GameObject p2Go = GameObject.Find("Player2_Aika");
            FighterController p1Ctrl = p1Go != null ? p1Go.GetComponent<FighterController>() : null;
            FighterController p2Ctrl = p2Go != null ? p2Go.GetComponent<FighterController>() : null;

            if (p1Ctrl != null && p2Ctrl != null)
            {
                replay.BindFighters(p1Ctrl, p2Ctrl);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(ARMORY_PREFAB_DIR)) AssetDatabase.CreateFolder("Assets/Prefabs", "Armory");
            if (!AssetDatabase.IsValidFolder("Assets/Armory/Materials"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Armory")) AssetDatabase.CreateFolder("Assets", "Armory");
                AssetDatabase.CreateFolder("Assets/Armory", "Materials");
            }
        }

        #region 1. Sprite Importers

        public static void ConfigureAllSpriteImporters()
        {
            ConfigureFolderSprites(MUGSHOTS_DIR);
            ConfigureFolderSprites(ICONS_DIR);
            ConfigureFolderSprites(FRAMES_DIR);
        }

        private static void ConfigureFolderSprites(string folder)
        {
            if (!Directory.Exists(folder)) return;
            string[] files = Directory.GetFiles(folder, "*.png");
            foreach (string file in files)
            {
                var importer = AssetImporter.GetAtPath(file) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
            }
        }

        #endregion

        #region 2. Build Weapon Prop Prefabs

        public static void BuildAllWeaponPropPrefabs()
        {
            Shader toonShader = Shader.Find("DemonIdol/MDI_AnimeToon") ?? Shader.Find("Universal Render Pipeline/Lit");

            // Cosmic Retro Blaster
            Material blasterMat = GetOrCreateMaterial("Assets/Armory/Materials/MAT_CosmicRetroBlaster.mat", toonShader,
                new Color(0.12f, 0.14f, 0.18f, 1f), new Color(0f, 0.95f, 1f, 1f), 3.5f);
            BuildBlasterPrefab("Prop_CosmicRetroBlaster", blasterMat, WeaponArchetype.Gun);

            // Tactical Weapon Delta
            Material tacticalMat = GetOrCreateMaterial("Assets/Armory/Materials/MAT_TacticalWeaponDelta.mat", toonShader,
                new Color(0.18f, 0.20f, 0.22f, 1f), new Color(1f, 0.35f, 0.1f, 1f), 2.5f);
            BuildBlasterPrefab("Prop_TacticalWeaponDelta", tacticalMat, WeaponArchetype.Gun);

            // Low Poly Fantasy Sword
            Material swordMat = GetOrCreateMaterial("Assets/Armory/Materials/MAT_LowPolyFantasySword.mat", toonShader,
                new Color(0.85f, 0.88f, 0.95f, 1f), new Color(1f, 0.85f, 0.3f, 1f), 2.0f);
            BuildSwordPrefab("Prop_LowPolyFantasySword", swordMat);

            // Low Poly Katana
            Material katanaMat = GetOrCreateMaterial("Assets/Armory/Materials/MAT_LowPolyKatana.mat", toonShader,
                new Color(0.92f, 0.92f, 0.96f, 1f), new Color(1f, 0.3f, 0.58f, 1f), 2.5f);
            BuildKatanaPrefab("Prop_LowPolyKatana", katanaMat);

            // Stylized Magic Staff
            Material staffMat = GetOrCreateMaterial("Assets/Armory/Materials/MAT_StylizedMagicStaff.mat", toonShader,
                new Color(0.28f, 0.22f, 0.32f, 1f), new Color(0.75f, 0.35f, 1f, 1f), 4.0f);
            BuildMagicStaffPrefab("Prop_StylizedMagicStaff", staffMat);
        }

        private static Material GetOrCreateMaterial(string path, Shader shader, Color baseColor, Color neonColor, float neonIntensity)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_NeonColor")) mat.SetColor("_NeonColor", neonColor);
            if (mat.HasProperty("_NeonIntensity")) mat.SetFloat("_NeonIntensity", neonIntensity);
            if (mat.HasProperty("_RampThreshold1")) mat.SetFloat("_RampThreshold1", 0.45f);
            if (mat.HasProperty("_RampThreshold2")) mat.SetFloat("_RampThreshold2", 0.15f);
            if (mat.HasProperty("_RampSmoothness")) mat.SetFloat("_RampSmoothness", 0.02f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void BuildBlasterPrefab(string prefabName, Material mat, WeaponArchetype archetype)
        {
            string path = $"{ARMORY_PREFAB_DIR}/{prefabName}.prefab";
            GameObject root = new GameObject(prefabName);

            // 1. Receiver
            var receiver = GameObject.CreatePrimitive(PrimitiveType.Cube);
            receiver.name = "Receiver";
            receiver.transform.SetParent(root.transform, false);
            receiver.transform.localScale = new Vector3(0.06f, 0.08f, 0.22f);
            receiver.GetComponent<MeshRenderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(receiver.GetComponent<BoxCollider>());

            // 2. Barrel
            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Barrel";
            barrel.transform.SetParent(root.transform, false);
            barrel.transform.localPosition = new Vector3(0f, 0.02f, 0.18f);
            barrel.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.04f, 0.12f, 0.04f);
            barrel.GetComponent<MeshRenderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(barrel.GetComponent<CapsuleCollider>());

            // 3. Grip
            var grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Grip";
            grip.transform.SetParent(root.transform, false);
            grip.transform.localPosition = new Vector3(0f, -0.06f, -0.04f);
            grip.transform.localEulerAngles = new Vector3(18f, 0f, 0f);
            grip.transform.localScale = new Vector3(0.045f, 0.10f, 0.05f);
            grip.GetComponent<MeshRenderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(grip.GetComponent<BoxCollider>());

            // 4. Energy Cell (Glowing Core)
            var cell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cell.name = "EnergyCell";
            cell.transform.SetParent(root.transform, false);
            cell.transform.localPosition = new Vector3(0f, 0.01f, 0.02f);
            cell.transform.localScale = new Vector3(0.065f, 0.065f, 0.065f);
            cell.GetComponent<MeshRenderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(cell.GetComponent<SphereCollider>());

            // 5. Muzzle Point
            var muzzle = new GameObject("MuzzlePoint");
            muzzle.transform.SetParent(root.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 0.02f, 0.32f);

            // FighterWeaponProp descriptor
            var prop = root.AddComponent<FighterWeaponProp>();
            prop.archetype = archetype;
            prop.weaponName = prefabName.Replace("Prop_", "");
            prop.targetBone = HumanBodyBones.RightHand;
            prop.muzzlePoint = muzzle.transform;
            prop.localPositionOffset = new Vector3(0.02f, 0.05f, 0.04f);
            prop.localRotationOffset = new Vector3(0f, 90f, -90f);
            prop.localScale = Vector3.one;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void BuildSwordPrefab(string prefabName, Material mat)
        {
            string path = $"{ARMORY_PREFAB_DIR}/{prefabName}.prefab";
            GameObject root = new GameObject(prefabName);

            // 1. Blade
            var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Blade";
            blade.transform.SetParent(root.transform, false);
            blade.transform.localPosition = new Vector3(0f, 0.50f, 0f);
            blade.transform.localScale = new Vector3(0.025f, 0.85f, 0.08f);
            blade.GetComponent<MeshRenderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(blade.GetComponent<BoxCollider>());

            // 2. Crossguard
            var guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guard.name = "Crossguard";
            guard.transform.SetParent(root.transform, false);
            guard.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            guard.transform.localScale = new Vector3(0.04f, 0.04f, 0.28f);
            guard.GetComponent<MeshRenderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(guard.GetComponent<BoxCollider>());

            // 3. Grip & Pommel
            var grip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            grip.name = "Grip";
            grip.transform.SetParent(root.transform, false);
            grip.transform.localPosition = new Vector3(0f, -0.10f, 0f);
            grip.transform.localScale = new Vector3(0.035f, 0.12f, 0.035f);
            grip.GetComponent<MeshRenderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(grip.GetComponent<CapsuleCollider>());

            var pommel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pommel.name = "Pommel";
            pommel.transform.SetParent(root.transform, false);
            pommel.transform.localPosition = new Vector3(0f, -0.23f, 0f);
            pommel.transform.localScale = new Vector3(0.065f, 0.065f, 0.065f);
            pommel.GetComponent<MeshRenderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(pommel.GetComponent<SphereCollider>());

            // Blade Tip & Base
            var tip = new GameObject("BladeTip");
            tip.transform.SetParent(root.transform, false);
            tip.transform.localPosition = new Vector3(0f, 0.95f, 0f);

            var bBase = new GameObject("BladeBase");
            bBase.transform.SetParent(root.transform, false);
            bBase.transform.localPosition = new Vector3(0f, 0.08f, 0f);

            // Slash Trail
            var trailGo = new GameObject("SlashTrail");
            trailGo.transform.SetParent(tip.transform, false);
            var trail = trailGo.AddComponent<TrailRenderer>();
            trail.time = 0.20f;
            trail.startWidth = 0.35f;
            trail.endWidth = 0.0f;
            trail.emitting = false;
            trail.material = mat;

            var prop = root.AddComponent<FighterWeaponProp>();
            prop.archetype = WeaponArchetype.Sword;
            prop.weaponName = "Low Poly Fantasy Sword";
            prop.targetBone = HumanBodyBones.RightHand;
            prop.bladeTip = tip.transform;
            prop.bladeBase = bBase.transform;
            prop.slashTrail = trail;
            prop.localPositionOffset = new Vector3(0f, 0.06f, 0.02f);
            prop.localRotationOffset = new Vector3(-85f, 0f, 0f);
            prop.localScale = Vector3.one;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void BuildKatanaPrefab(string prefabName, Material mat)
        {
            string path = $"{ARMORY_PREFAB_DIR}/{prefabName}.prefab";
            GameObject root = new GameObject(prefabName);

            // Curved Katana Blade
            var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "KatanaBlade";
            blade.transform.SetParent(root.transform, false);
            blade.transform.localPosition = new Vector3(0f, 0.48f, 0.02f);
            blade.transform.localEulerAngles = new Vector3(2.5f, 0f, 0f);
            blade.transform.localScale = new Vector3(0.015f, 0.88f, 0.055f);
            blade.GetComponent<MeshRenderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(blade.GetComponent<BoxCollider>());

            // Circular Tsuba Guard
            var tsuba = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tsuba.name = "Tsuba";
            tsuba.transform.SetParent(root.transform, false);
            tsuba.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            tsuba.transform.localScale = new Vector3(0.12f, 0.008f, 0.12f);
            tsuba.GetComponent<MeshRenderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(tsuba.GetComponent<CapsuleCollider>());

            // Tsuka Handle
            var tsuka = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tsuka.name = "Tsuka";
            tsuka.transform.SetParent(root.transform, false);
            tsuka.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            tsuka.transform.localScale = new Vector3(0.03f, 0.14f, 0.03f);
            tsuka.GetComponent<MeshRenderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(tsuka.GetComponent<CapsuleCollider>());

            var tip = new GameObject("BladeTip");
            tip.transform.SetParent(root.transform, false);
            tip.transform.localPosition = new Vector3(0f, 0.94f, 0.04f);

            var trailGo = new GameObject("SlashTrail");
            trailGo.transform.SetParent(tip.transform, false);
            var trail = trailGo.AddComponent<TrailRenderer>();
            trail.time = 0.20f;
            trail.startWidth = 0.30f;
            trail.endWidth = 0.0f;
            trail.emitting = false;
            trail.material = mat;

            var prop = root.AddComponent<FighterWeaponProp>();
            prop.archetype = WeaponArchetype.Sword;
            prop.weaponName = "Low Poly Katana";
            prop.targetBone = HumanBodyBones.RightHand;
            prop.bladeTip = tip.transform;
            prop.slashTrail = trail;
            prop.localPositionOffset = new Vector3(0f, 0.05f, 0.02f);
            prop.localRotationOffset = new Vector3(-85f, 0f, 0f);
            prop.localScale = Vector3.one;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void BuildMagicStaffPrefab(string prefabName, Material mat)
        {
            string path = $"{ARMORY_PREFAB_DIR}/{prefabName}.prefab";
            GameObject root = new GameObject(prefabName);

            // 1. Staff Shaft
            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Shaft";
            shaft.transform.SetParent(root.transform, false);
            shaft.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            shaft.transform.localScale = new Vector3(0.04f, 0.75f, 0.04f);
            shaft.GetComponent<MeshRenderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(shaft.GetComponent<CapsuleCollider>());

            // 2. Crescent Head Filigree
            var headRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            headRing.name = "CrescentRing";
            headRing.transform.SetParent(root.transform, false);
            headRing.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            headRing.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
            headRing.transform.localScale = new Vector3(0.24f, 0.025f, 0.24f);
            headRing.GetComponent<MeshRenderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(headRing.GetComponent<CapsuleCollider>());

            // 3. Floating Orb Focus (Emissive Crystal)
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "StaffOrb";
            orb.transform.SetParent(root.transform, false);
            orb.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            orb.transform.localScale = new Vector3(0.11f, 0.11f, 0.11f);
            orb.GetComponent<MeshRenderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(orb.GetComponent<SphereCollider>());

            var prop = root.AddComponent<FighterWeaponProp>();
            prop.archetype = WeaponArchetype.MageStaff;
            prop.weaponName = "Stylized Magic Staff";
            prop.targetBone = HumanBodyBones.RightHand;
            prop.staffOrbPoint = orb.transform;
            prop.localPositionOffset = new Vector3(0f, 0.05f, 0.02f);
            prop.localRotationOffset = new Vector3(-85f, 0f, 0f);
            prop.localScale = Vector3.one;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        #endregion

        #region 3. Distribute Weapons to All Characters

        public static void DistributeWeaponsToAllCharacters()
        {
            var blasterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ARMORY_PREFAB_DIR}/Prop_CosmicRetroBlaster.prefab");
            var tacticalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ARMORY_PREFAB_DIR}/Prop_TacticalWeaponDelta.prefab");
            var swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ARMORY_PREFAB_DIR}/Prop_LowPolyFantasySword.prefab");
            var katanaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ARMORY_PREFAB_DIR}/Prop_LowPolyKatana.prefab");
            var staffPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ARMORY_PREFAB_DIR}/Prop_StylizedMagicStaff.prefab");

            // Character archetype mapping:
            // Guns: C01_Akio, C04_UnityChan, C09_PicoChan, Free Test Character Asuna
            // Swords: C02_Aika, C03_AnimeGirl, Fighter_Arin, C10_Kiki, C12_Underwear1 (Hero), C13_SapphiArtchan, Fighter_Kai
            // Magic Staffs: C05_Akane, C06_Cultist, C07_StellarWitch, C08_SanpakuChan, C11_Casual1, C14_Muryotaisu

            string[] characterPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Characters" });
            foreach (string guid in characterPrefabs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);

                GameObject propToAttach = swordPrefab;
                if (fileName.Contains("Akio") || fileName.Contains("UnityChan") || fileName.Contains("PicoChan") || fileName.Contains("Asuna"))
                {
                    propToAttach = (fileName.Contains("UnityChan") || fileName.Contains("Asuna")) ? tacticalPrefab : blasterPrefab;
                }
                else if (fileName.Contains("Akane") || fileName.Contains("Cultist") || fileName.Contains("StellarWitch") ||
                         fileName.Contains("Sanpaku") || fileName.Contains("Casual") || fileName.Contains("Muryotaisu"))
                {
                    propToAttach = staffPrefab;
                }
                else if (fileName.Contains("Aika") || fileName.Contains("Kiki"))
                {
                    propToAttach = katanaPrefab;
                }

                AttachPropToCharacterPrefab(path, propToAttach);
            }
        }

        private static void AttachPropToCharacterPrefab(string prefabPath, GameObject weaponPrefab)
        {
            if (weaponPrefab == null) return;

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null) return;

            var binder = root.GetComponent<FighterPropBinder>();
            if (binder == null) binder = root.AddComponent<FighterPropBinder>();

            // Remove previous attached weapon instances
            var existingProps = root.GetComponentsInChildren<FighterWeaponProp>(true);
            foreach (var ep in existingProps)
            {
                UnityEngine.Object.DestroyImmediate(ep.gameObject);
            }

            // Find right hand bone
            Transform socket = FindBone(root.transform, new[] {
                "Right wrist", "RightHand", "mixamorig:RightHand", "Hand.R", "Hand_R",
                "Right_Hand", "Bip01 R Hand", "R_Hand", "HandR"
            });

            Transform parent = (socket != null) ? socket : root.transform;
            GameObject propInstance = UnityEngine.Object.Instantiate(weaponPrefab, parent);
            propInstance.name = weaponPrefab.name;

            var propComponent = propInstance.GetComponent<FighterWeaponProp>();
            if (propComponent != null)
            {
                propInstance.transform.localPosition = propComponent.localPositionOffset;
                propInstance.transform.localEulerAngles = propComponent.localRotationOffset;
                propInstance.transform.localScale = propComponent.localScale;

                binder.SetWeapons(propComponent);
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log($"<color=#81c784>[ArmoryBuilder] Equipped {weaponPrefab.name} on {Path.GetFileNameWithoutExtension(prefabPath)}</color>");
        }

        private static Transform FindBone(Transform root, string[] names)
        {
            if (root == null) return null;
            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(root.name, names[i], StringComparison.OrdinalIgnoreCase)) return root;
            }
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindBone(root.GetChild(i), names);
                if (found != null) return found;
            }
            return null;
        }

        #endregion

        #region 4. Bind Mugshots to Character Definitions

        public static void BindMugshotsToCharacterDefinitions()
        {
            string[] defGuids = AssetDatabase.FindAssets("t:CharacterDefinition", new[] { "Assets/CombatData/Characters" });
            foreach (string guid in defGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
                if (def == null) continue;

                // Match mugshot by ID or name
                string mugshotName = $"Mugshot_{def.id}_";
                string[] mugshotFiles = Directory.GetFiles(MUGSHOTS_DIR, $"*{def.id}*.png");
                if (mugshotFiles.Length == 0)
                {
                    mugshotFiles = Directory.GetFiles(MUGSHOTS_DIR, $"*{def.displayName}*.png");
                }

                if (mugshotFiles.Length > 0)
                {
                    string spritePath = mugshotFiles[0].Replace("\\", "/");
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if (sprite != null)
                    {
                        def.portraitSprite = sprite;
                        EditorUtility.SetDirty(def);
                        Debug.Log($"<color=#81c784>[ArmoryBuilder] Bound {sprite.name} to {def.displayName}</color>");
                    }
                }
            }
        }

        #endregion

        #region 5. URP Indoor Template & Stage Lighting

        public static void SetupURPIndoorTemplateAndStages()
        {
            // 1. Create or load URP Indoor Template Volume Profile
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(INDOOR_PROFILE_PATH);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, INDOOR_PROFILE_PATH);
            }

            // Ensure ACES Tonemapping
            if (!profile.TryGet<Tonemapping>(out var tonemap))
            {
                tonemap = profile.Add<Tonemapping>(true);
            }
            tonemap.mode.Override(TonemappingMode.ACES);

            // Ensure Indoor Bloom
            if (!profile.TryGet<Bloom>(out var bloom))
            {
                bloom = profile.Add<Bloom>(true);
            }
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(0.70f);
            bloom.scatter.Override(0.70f);
            bloom.tint.Override(new Color(1f, 0.92f, 0.94f, 1f));

            // Ensure Color Adjustments
            if (!profile.TryGet<ColorAdjustments>(out var colorAdj))
            {
                colorAdj = profile.Add<ColorAdjustments>(true);
            }
            colorAdj.postExposure.Override(0.18f);
            colorAdj.contrast.Override(12f);
            colorAdj.saturation.Override(8f);

            // Ensure White Balance (warm indoor incandescent glow)
            if (!profile.TryGet<WhiteBalance>(out var wb))
            {
                wb = profile.Add<WhiteBalance>(true);
            }
            wb.temperature.Override(3.5f);
            wb.tint.Override(-1.5f);

            // Ensure Vignette
            if (!profile.TryGet<Vignette>(out var vig))
            {
                vig = profile.Add<Vignette>(true);
            }
            vig.intensity.Override(0.24f);
            vig.smoothness.Override(0.35f);

            EditorUtility.SetDirty(profile);

            // 2. Calibrate all 6 stage prefabs with Indoor Reflection Probes & Lighting
            string[] stagePrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Stages" });
            foreach (string guid in stagePrefabs)
            {
                string sPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(sPath);
                if (root != null)
                {
                    // Reflection Probe with Box Projection
                    var probe = root.GetComponentInChildren<ReflectionProbe>();
                    if (probe == null)
                    {
                        var probeGo = new GameObject("Indoor_ReflectionProbe");
                        probeGo.transform.SetParent(root.transform, false);
                        probeGo.transform.localPosition = new Vector3(0f, 3.5f, 0f);
                        probe = probeGo.AddComponent<ReflectionProbe>();
                    }
                    probe.mode = ReflectionProbeMode.Realtime;
                    probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                    probe.boxProjection = true;
                    probe.size = new Vector3(32f, 16f, 24f);
                    probe.center = new Vector3(0f, 4f, 0f);
                    probe.intensity = 1.0f;

                    // Indoor Volume Profile override
                    var vol = root.GetComponentInChildren<Volume>();
                    if (vol == null)
                    {
                        var vGo = new GameObject("Stage_IndoorVolume");
                        vGo.transform.SetParent(root.transform, false);
                        vol = vGo.AddComponent<Volume>();
                    }
                    vol.isGlobal = true;
                    vol.priority = 2f;
                    vol.sharedProfile = profile;

                    PrefabUtility.SaveAsPrefabAsset(root, sPath);
                    PrefabUtility.UnloadPrefabContents(root);
                    Debug.Log($"<color=#81c784>[ArmoryBuilder] Applied URP Indoor Template to {Path.GetFileNameWithoutExtension(sPath)}</color>");
                }
            }

            // 3. Update Home Scene Volume
            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.path == "Assets/Scenes/Home.unity")
            {
                var globalVol = UnityEngine.Object.FindAnyObjectByType<Volume>();
                if (globalVol != null)
                {
                    globalVol.sharedProfile = profile;
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }
        }

        #endregion
    }
}
#endif

#endif
