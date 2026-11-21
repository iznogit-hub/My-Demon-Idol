#if MDI_ARCHIVE
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using DemonIdol.Combat;
using DemonIdol.Input;

public static class DemonIdolUniformCustomizer
{
    private const string SHADER_NAME = "DemonIdol/MDI/AnimeToon";
    private const string CHAR_PREFABS_DIR = "Assets/Prefabs/Characters";
    private const string ARCHIVE_DIR = "Assets/Prefabs/Archived";
    private const string MDI_CONTROLLER_PATH = "Assets/MDI_Characters/Runtime/MDI_FighterTemplate.controller";

    // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Costumes/Focus Characters Folder", priority = 1)]
    public static void FocusCharactersFolder()
    {
        var ren = AssetDatabase.LoadAssetAtPath<Object>("Assets/Prefabs/Characters/C01_Akio.prefab");
        if (ren != null)
        {
            Selection.activeObject = ren;
            EditorGUIUtility.PingObject(ren);
        }
        EditorWindow.FocusWindowIfItsOpen<SceneView>();
    }

    // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Costumes/Rebuild All Character Costumes (Trending Uniform Style)", priority = 0)]
    public static void RebuildAllCostumes()
    {
        Debug.Log("<color=#00e676>[MDI Costumes] Starting Master Costume, Roster & Controller Standardization...</color>");

        var toonShader = Shader.Find(SHADER_NAME);
        if (toonShader == null)
        {
            Debug.LogError($"[MDI Costumes] Could not find shader {SHADER_NAME}!");
            return;
        }

        // 1. Ensure Folder Structure & Archive Kiki / Clean C03
        EnsureDirectoryStructure();

        // Training dummy prefabs were removed. Practice uses a CPU fighter.

        // 3. Setup Hero Characters (C12 Fina, C12M Ren, C12B Reika)
        SetupHeroCharacters(toonShader);

        // 4. Configure Materials for Roster Characters (90s Cel Anime + 0 Cloth Specular)
        FixAllRosterMaterials(toonShader);

        // 5. Standardize Combat Controller Stack across ALL Character Prefabs
        StandardizeAllFighterControllers();

        // 6. Global Sanitize Material Glints
        SanitizeAllMaterialGlints();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=#00e676><b>[MDI Costumes] MASTER STANDARDIZATION COMPLETE!</b> All characters outfitted with C12 trending uniform palette, 90s anime cel shading, and full combat controller stack.</color>");
    }

    private static void EnsureDirectoryStructure()
    {
        if (!Directory.Exists(CHAR_PREFABS_DIR)) Directory.CreateDirectory(CHAR_PREFABS_DIR);
        if (!Directory.Exists(ARCHIVE_DIR)) Directory.CreateDirectory(ARCHIVE_DIR);

        // Archive Kiki if present in main characters folder
        string kikiSrc = Path.Combine(CHAR_PREFABS_DIR, "C10_Kiki.prefab");
        string kikiDst = Path.Combine(ARCHIVE_DIR, "C10_Kiki.prefab");
        if (File.Exists(kikiSrc))
        {
            if (!File.Exists(kikiDst))
            {
                AssetDatabase.MoveAsset(kikiSrc, kikiDst);
            }
            else
            {
                AssetDatabase.DeleteAsset(kikiSrc);
            }
            Debug.Log("[MDI Costumes] Safely preserved Kiki in Assets/Prefabs/Archived/");
        }

        // Completely remove Arin and C03 AnimeGirl per user instruction
        string arinPath = Path.Combine(CHAR_PREFABS_DIR, "Fighter_Arin.prefab");
        if (File.Exists(arinPath))
        {
            AssetDatabase.DeleteAsset(arinPath);
            Debug.Log("[MDI Costumes] Completely removed Fighter_Arin.prefab");
        }

        string c03Path = Path.Combine(CHAR_PREFABS_DIR, "C03_AnimeGirl.prefab");
        if (File.Exists(c03Path))
        {
            AssetDatabase.DeleteAsset(c03Path);
            Debug.Log("[MDI Costumes] Removed deprecated C03_AnimeGirl.prefab");
        }

        // Remove duplicate alias prefabs so only canonical C12M_Ren and C12B_Reika exist
        string renDup = Path.Combine(CHAR_PREFABS_DIR, "C03_REN.prefab");
        if (File.Exists(renDup))
        {
            AssetDatabase.DeleteAsset(renDup);
        }
        string reikaDup = Path.Combine(CHAR_PREFABS_DIR, "C10_REIKA.prefab");
        if (File.Exists(reikaDup))
        {
            AssetDatabase.DeleteAsset(reikaDup);
        }
    }

    private static void SetupTrainingDummies(Shader toonShader)
    {
        // Retired. TrainingDummy_Yellow/Red/Purple are not recreated.
    }

    private static void CreateDummyPrefab(string srcPath, string dstPath, string dummyName, Material mat, DummyMode mode)
    {
        var srcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(srcPath);
        if (srcPrefab == null)
        {
            Debug.LogError($"[MDI Costumes] Kai source prefab not found at {srcPath}");
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(srcPrefab);
        instance.name = dummyName;

        // Apply material to all SkinnedMeshRenderers
        if (mat != null)
        {
            foreach (var smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mats = new Material[smr.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                smr.sharedMaterials = mats;
            }
        }

        // Setup AI Controller
        var ai = instance.GetComponent<TrainingDummyAI>();
        if (ai == null) ai = instance.AddComponent<TrainingDummyAI>();
        ai.mode = mode;

        PrefabUtility.SaveAsPrefabAsset(instance, dstPath);
        Object.DestroyImmediate(instance);
        Debug.Log($"[MDI Costumes] Configured Training Dummy variant: {dstPath} (Mode: {mode})");
    }

    private static void SetupHeroCharacters(Shader toonShader)
    {
        // 1. C12_Fina (Lead Protagonist)
        string finaSrc = "Assets/smoky_fox/FinaAnimeGirl/Prefabs/Fina.prefab";
        string finaDst = $"{CHAR_PREFABS_DIR}/C12_Fina.prefab";
        string finaMatDir = "Assets/smoky_fox/FinaAnimeGirl/Materials/";

        var finaObj = AssetDatabase.LoadAssetAtPath<GameObject>(finaSrc);
        if (finaObj != null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(finaObj);
            instance.name = "C12_Fina";

            var smr = instance.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null)
            {
                var mats = new Material[]
                {
                    AssetDatabase.LoadAssetAtPath<Material>(finaMatDir + "Body.mat"),
                    AssetDatabase.LoadAssetAtPath<Material>(finaMatDir + "Cloth.mat"),
                    AssetDatabase.LoadAssetAtPath<Material>(finaMatDir + "Eyes.mat"),
                    AssetDatabase.LoadAssetAtPath<Material>(finaMatDir + "Face.mat"),
                    AssetDatabase.LoadAssetAtPath<Material>(finaMatDir + "Hair.mat"),
                    AssetDatabase.LoadAssetAtPath<Material>(finaMatDir + "Mouth.mat")
                };
                smr.sharedMaterials = mats;
            }

            PrefabUtility.SaveAsPrefabAsset(instance, finaDst);
            Object.DestroyImmediate(instance);
            Debug.Log("[MDI Costumes] Configured Lead Hero Protagonist: C12_Fina.prefab");
        }

        // C03 Ren and C12M_Ren are retired and are not recreated.

        // 3. C12B_Reika (Hero Alt Counterpart)
        string reikaSrc = "Assets/MDI_Characters/Clans/Occult/Reika/Reika.prefab";
        string reikaDst = $"{CHAR_PREFABS_DIR}/C12B_Reika.prefab";
        string reikaMatPath = "Assets/MDI_Characters/Clans/Occult/Reika/Material_OCC_REIKA.mat";

        var reikaObj = AssetDatabase.LoadAssetAtPath<GameObject>(reikaSrc);
        if (reikaObj != null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(reikaObj);
            instance.name = "C12B_Reika";

            var reikaMat = AssetDatabase.LoadAssetAtPath<Material>(reikaMatPath);
            if (reikaMat != null)
            {
                foreach (var smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mats = new Material[smr.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = reikaMat;
                    smr.sharedMaterials = mats;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(instance, reikaDst);
            Object.DestroyImmediate(instance);
            Debug.Log("[MDI Costumes] Configured Hero Alt Counterpart: C12B_Reika.prefab");
        }
    }

    private static void FixAllRosterMaterials(Shader toonShader)
    {
        // Aika Materials
        FixAikaMaterials(toonShader);

        // Akio Materials
        FixAkioMaterials(toonShader);

        // Casual1 Materials
        FixCasual1Materials(toonShader);

        // PicoChan Materials
        FixPicoChanMaterials(toonShader);

        // SanpakuChan Materials
        FixSanpakuMaterials(toonShader);
    }

    private static void FixAikaMaterials(Shader toonShader)
    {
        string matDir = "Assets/Novacorp Studios/Characters/Hikari Series/Aika/Highschool Uniform/Materials/";
        string bodyTexPath = matDir + "1036813356 Body.png";
        string faceTexPath = matDir + "1036813356 Face.png";

        var bodyTex = AssetDatabase.LoadAssetAtPath<Texture2D>(bodyTexPath);
        var faceTex = AssetDatabase.LoadAssetAtPath<Texture2D>(faceTexPath);

        ConfigureMaterial(matDir + "Bod SKIN.mat", toonShader, bodyTex, isCloth: true, specIntensity: 0f);
        ConfigureMaterial(matDir + "Tops CLOTH.mat", toonShader, bodyTex, isCloth: true, specIntensity: 0f);
        ConfigureMaterial(matDir + "HairBack HAIR.mat", toonShader, bodyTex, isCloth: false, specIntensity: 0.8f);
        ConfigureMaterial(matDir + "Hair HAIR.mat", toonShader, bodyTex, isCloth: false, specIntensity: 0.8f);
        ConfigureMaterial(matDir + "Face SKIN.mat", toonShader, faceTex, isCloth: false, specIntensity: 0f);
        ConfigureMaterial(matDir + "FaceBrow FACE.mat", toonShader, faceTex, isCloth: false, specIntensity: 0f, alphaClip: true);
        ConfigureMaterial(matDir + "FaceMouth FACE.mat", toonShader, faceTex, isCloth: false, specIntensity: 0f, alphaClip: true);
        ConfigureMaterial(matDir + "EyeIris EYE.mat", toonShader, faceTex, isCloth: false, specIntensity: 0f);
        ConfigureMaterial(matDir + "EyeHighlight EYE.mat", toonShader, faceTex, isCloth: false, specIntensity: 0f, alphaClip: true);
    }

    private static void FixAkioMaterials(Shader toonShader)
    {
        string matDir = "Assets/Novacorp Studios/Characters/Hikari Series/Akio/Highschool Uniform/Materials/";
        string bodyTexPath = matDir + "6569948208 Body.png";
        string faceTexPath = matDir + "6569948208 Face.png";

        var bodyTex = AssetDatabase.LoadAssetAtPath<Texture2D>(bodyTexPath);
        var faceTex = AssetDatabase.LoadAssetAtPath<Texture2D>(faceTexPath);

        ConfigureMaterial(matDir + "Body SKIN.mat", toonShader, bodyTex, isCloth: true, specIntensity: 0f);
        ConfigureMaterial(matDir + "Bottoms CLOTH.mat", toonShader, bodyTex, isCloth: true, specIntensity: 0f);
        
        // Akio Hair: Styled modern dark espresso noir
        var hairMat1 = ConfigureMaterial(matDir + "HairBack HAIR.mat", toonShader, bodyTex, isCloth: false, specIntensity: 0.8f);
        if (hairMat1 != null) hairMat1.SetColor("_BaseColor", new Color(0.18f, 0.15f, 0.20f, 1.0f));
        
        var hairMat2 = ConfigureMaterial(matDir + "Hair HAIR.mat", toonShader, bodyTex, isCloth: false, specIntensity: 0.8f);
        if (hairMat2 != null) hairMat2.SetColor("_BaseColor", new Color(0.18f, 0.15f, 0.20f, 1.0f));

        ConfigureMaterial(matDir + "Face SKIN.mat", toonShader, faceTex, isCloth: false, specIntensity: 0f);
        ConfigureMaterial(matDir + "FaceBrow FACE.mat", toonShader, faceTex, isCloth: false, specIntensity: 0f, alphaClip: true);
        ConfigureMaterial(matDir + "FaceMouth FACE.mat", toonShader, faceTex, isCloth: false, specIntensity: 0f, alphaClip: true);
        ConfigureMaterial(matDir + "EyeIris EYE.mat", toonShader, faceTex, isCloth: false, specIntensity: 0f);
        ConfigureMaterial(matDir + "EyeHighlight EYE.mat", toonShader, faceTex, isCloth: false, specIntensity: 0f, alphaClip: true);
    }

    private static void FixCasual1Materials(Shader toonShader)
    {
        string texDir = "Assets/AnimeGirls/BaseCharacter/BaseCharacter/Sourcefiles/Tex/";
        string matDir = "Assets/AnimeGirls/BaseCharacter/BaseCharacter/Sourcefiles/Mat/";
        string casualTex = "Assets/AnimeGirls/Casual1/SourceFiles/Texture/Casual1.png";
        string casualMat = "Assets/AnimeGirls/Casual1/SourceFiles/Material/Mat.mat";

        var cTex = AssetDatabase.LoadAssetAtPath<Texture2D>(casualTex);
        ConfigureMaterial(casualMat, toonShader, cTex, isCloth: true, specIntensity: 0f);

        var eyeTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "Tex_Eyes.png");
        var eyeWhiteTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "Tex_EyeWhite.png");
        var eyeReflTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "Tex_EyeReflection.png");
        var faceTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "Tex_Face.png");
        var bodyTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "Tex_Body.png");
        var hairTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "Tex_Hair1_hair.png");
        var headTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "Tex_Hair1_head.png");
        var mouthTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "Tex_Mouth.png");
        var browTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "Tex_Eyebrows.png");
        var lashTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "Tex_Eyelashes.png");
        var lineTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "Tex_Eyeliner.png");

        ConfigureMaterial(matDir + "Eye.mat", toonShader, eyeTex, isCloth: false, specIntensity: 0f);
        ConfigureMaterial(matDir + "EyeBack.mat", toonShader, eyeWhiteTex, isCloth: false, specIntensity: 0f);
        ConfigureMaterial(matDir + "Eye_reflection.mat", toonShader, eyeReflTex, isCloth: false, specIntensity: 0f, alphaClip: true);
        ConfigureMaterial(matDir + "Face.mat", toonShader, faceTex, isCloth: false, specIntensity: 0f);
        ConfigureMaterial(matDir + "Body.mat", toonShader, bodyTex, isCloth: false, specIntensity: 0f);
        ConfigureMaterial(matDir + "Hair_head.mat", toonShader, headTex, isCloth: false, specIntensity: 0.6f);
        ConfigureMaterial(matDir + "Mat_Hair_1_hair.mat", toonShader, hairTex, isCloth: false, specIntensity: 0.8f);
        ConfigureMaterial(matDir + "mouth_inside.mat", toonShader, mouthTex, isCloth: false, specIntensity: 0f, alphaClip: true);
        ConfigureMaterial(matDir + "Eyebrows.mat", toonShader, browTex, isCloth: false, specIntensity: 0f, alphaClip: true);
        ConfigureMaterial(matDir + "Eyelashes.mat", toonShader, lashTex, isCloth: false, specIntensity: 0f, alphaClip: true);
        ConfigureMaterial(matDir + "Eyeliner.mat", toonShader, lineTex, isCloth: false, specIntensity: 0f, alphaClip: true);
    }

    private static void FixPicoChanMaterials(Shader toonShader)
    {
        string texDir = "Assets/Picola/PicoChan/Textures/";
        string matDir = "Assets/Picola/PicoChan/Materials/";

        var eyeTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "pico_chan_chr_pico_00_eye_c.png");
        var eyehTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "pico_chan_chr_pico_00_eyeh_c.png");
        var faceTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "pico_chan_chr_pico_00_face_c.png");
        var bodyTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "pico_chan_chr_pico_00_body_c.png");
        var clothTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "pico_chan_chr_pico_00_clothes_c.png");
        var hairTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "pico_chan_chr_pico_00_hair_c.png");
        var bagTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "pico_chan_chr_pico_00_bag_c.png");

        ConfigureMaterial(matDir + "pico_eye_m.mat", toonShader, eyeTex, isCloth: false, specIntensity: 0f);
        ConfigureMaterial(matDir + "pico_eye_h_m.mat", toonShader, eyehTex, isCloth: false, specIntensity: 0f, alphaClip: true);
        ConfigureMaterial(matDir + "pico_face_m.mat", toonShader, faceTex, isCloth: false, specIntensity: 0f);
        ConfigureMaterial(matDir + "pico_body_m.mat", toonShader, bodyTex, isCloth: false, specIntensity: 0f);
        ConfigureMaterial(matDir + "pico_clothes_m.mat", toonShader, clothTex, isCloth: true, specIntensity: 0f);
        ConfigureMaterial(matDir + "pico_clothes_lens_m.mat", toonShader, clothTex, isCloth: true, specIntensity: 0f);
        ConfigureMaterial(matDir + "pico_clothes_metal_m.mat", toonShader, clothTex, isCloth: false, specIntensity: 1.0f);
        ConfigureMaterial(matDir + "pico_body_metal_m.mat", toonShader, bodyTex, isCloth: false, specIntensity: 1.0f);
        ConfigureMaterial(matDir + "pico_hair_m.mat", toonShader, hairTex, isCloth: false, specIntensity: 0.8f);
        ConfigureMaterial(matDir + "pico_bag_m.mat", toonShader, bagTex, isCloth: true, specIntensity: 0f);
    }

    private static void FixSanpakuMaterials(Shader toonShader)
    {
        string matDir = "Assets/SanpakuChan/Materials/";
        string texDir = "Assets/SanpakuChan/Textures/";

        var faceTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "T_Face.tga");
        var bodyTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "T_Body.tga");
        var hairTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "T_Hair.tga");

        ConfigureMaterial(matDir + "M_Face.mat", toonShader, faceTex, isCloth: false, specIntensity: 0f);
        ConfigureMaterial(matDir + "M_Body.mat", toonShader, bodyTex, isCloth: true, specIntensity: 0f);
        ConfigureMaterial(matDir + "M_Hair.mat", toonShader, hairTex, isCloth: false, specIntensity: 0.8f);
    }

    public static void StandardizeAllFighterControllers()
    {
        string[] allPrefabs = Directory.GetFiles(CHAR_PREFABS_DIR, "*.prefab");
        var templateAnim = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MDI_CONTROLLER_PATH);

        var lightAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_LightAttack.asset");
        var lowAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_LowAttack.asset");
        var medAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_MediumAttack.asset");
        var heavyAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_HeavyAttack.asset");
        var driveAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_DriveSkill.asset");
        var jumpAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_JumpAttack.asset");
        var superAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_DomainSuper.asset");
        var throwAtk = AssetDatabase.LoadAssetAtPath<AttackData>("Assets/CombatData/Kai_Throw.asset");

        foreach (string pPath in allPrefabs)
        {
            string fileName = Path.GetFileNameWithoutExtension(pPath);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(pPath);

            // 1. CharacterController
            CharacterController cc = prefabRoot.GetComponent<CharacterController>();
            if (cc == null) cc = prefabRoot.AddComponent<CharacterController>();
            cc.height = 1.85f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.95f, 0f);

            // 2. FighterController
            FighterController fc = prefabRoot.GetComponent<FighterController>();
            if (fc == null) fc = prefabRoot.AddComponent<FighterController>();

            bool isDummy = fileName.StartsWith("TrainingDummy");
            fc.fighterName = isDummy ? "Training Dummy" : fileName.Replace("C", "").Replace("_", " ");
            fc.isPlayerControlled = !isDummy;
            fc.maxHealth = isDummy ? 9999 : 1000;
            fc.maxSuperMeter = 100;

            fc.forwardWalkSpeed = 3.6f;
            fc.backwardWalkSpeed = 2.8f;
            fc.jumpVerticalLaunch = 8.0f;
            fc.jumpHorizontalSpeed = 3.2f;
            fc.gravity = 20f;
            fc.creepSpeed = 1.2f;
            fc.forwardDashSpeed = 6.5f;
            fc.forwardDashDurationTicks = 12;
            fc.backdashSpeed = 7f;
            fc.backdashDurationTicks = 14;
            fc.backdashInvincibleTicks = 5;
            fc.jumpAttackVelocityX = 3.2f;
            fc.jumpAttackVelocityY = 6.0f;
            fc.parryActiveTicks = 5;
            fc.parryRecoveryTicks = 14;
            fc.parryStunTicksOnAttacker = 18;
            fc.sidestepDistanceZ = 0.85f;
            fc.sidestepDurationTicks = 14;

            // Attack Data Assets
            fc.lightAttackData = lightAtk;
            fc.lowAttackData = lowAtk;
            fc.mediumAttackData = medAtk;
            fc.heavyAttackData = heavyAtk;
            fc.driveSkillData = driveAtk;
            fc.jumpAttackData = jumpAtk;
            fc.domainSuperData = superAtk;
            fc.throwAttackData = throwAtk;

            // 3. Child Hitbox
            Transform hbTransform = prefabRoot.transform.Find("Hitbox") ?? prefabRoot.transform.Find("Hitbox_Weapon");
            if (hbTransform == null)
            {
                GameObject hbGo = new GameObject("Hitbox");
                hbGo.transform.SetParent(prefabRoot.transform, false);
                hbGo.transform.localPosition = new Vector3(0f, 1.0f, 0.8f);
                var col = hbGo.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(1.2f, 1.2f, 1.4f);
                var hb = hbGo.AddComponent<Hitbox>();
                fc.activeHitbox = hb;
            }
            else
            {
                fc.activeHitbox = hbTransform.GetComponent<Hitbox>();
            }

            // 4. Child Hurtbox
            Transform hurtTransform = prefabRoot.transform.Find("Hurtbox_Torso");
            if (hurtTransform == null)
            {
                GameObject hurtGo = new GameObject("Hurtbox_Torso");
                hurtGo.transform.SetParent(prefabRoot.transform, false);
                hurtGo.transform.localPosition = new Vector3(0f, 1.0f, 0f);
                var col = hurtGo.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(0.8f, 1.6f, 0.8f);
                hurtGo.AddComponent<Hurtbox>();
            }

            // 5. MobileFighterInput
            if (!isDummy)
            {
                if (prefabRoot.GetComponent<MobileFighterInput>() == null)
                    prefabRoot.AddComponent<MobileFighterInput>();
            }

            // 6. AnimationEventReceiver & Animator
            if (prefabRoot.GetComponent<AnimationEventReceiver>() == null)
                prefabRoot.AddComponent<AnimationEventReceiver>();

            Animator anim = prefabRoot.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.applyRootMotion = false;
                if (anim.runtimeAnimatorController == null && templateAnim != null)
                {
                    anim.runtimeAnimatorController = templateAnim;
                }
                fc.animator = anim;
            }

            // Purge any missing MonoBehaviour scripts
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefabRoot);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, pPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            Debug.Log($"[MDI Costumes] Fully standardized combat stack on {fileName}");
        }
    }

    private static Material ConfigureMaterial(string matPath, Shader shader, Texture2D texture, bool isCloth, float specIntensity, bool alphaClip = false)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Debug.LogWarning($"[MDI Costumes] Material not found: {matPath}");
            return null;
        }

        if (mat.shader != shader) mat.shader = shader;

        if (texture != null)
        {
            mat.SetTexture("_BaseMap", texture);
            mat.SetTexture("_MainTex", texture);
        }

        // 90s Cel Anime Settings
        mat.SetFloat("_EnableOutline", 1.0f);
        mat.SetFloat("_OutlineWidth", 1.2f);
        mat.SetColor("_OutlineColor", new Color(0.09f, 0.07f, 0.12f, 1.0f));

        mat.SetFloat("_RampThreshold1", 0.42f);
        mat.SetFloat("_RampThreshold2", 0.78f);
        mat.SetFloat("_RampSmoothness", 0.008f);
        mat.SetColor("_ShadowColor", new Color(0.68f, 0.58f, 0.72f, 1.0f));

        // Specular: Strictly 0 for cloth/fabric to eliminate white glare holes!
        mat.SetFloat("_SpecIntensity", specIntensity);
        mat.SetFloat("_SpecPower", isCloth ? 16f : 48f);
        mat.SetFloat("_SpecThreshold", 0.55f);
        mat.SetColor("_SpecColor", new Color(1.0f, 0.98f, 0.92f, 1.0f));

        // Rim Light
        mat.SetColor("_RimColor", new Color(0.95f, 0.92f, 0.86f, 0.55f));
        mat.SetFloat("_RimPower", 3.5f);
        mat.SetFloat("_RimThreshold", 0.40f);

        // 35mm Celluloid Film Grade
        mat.SetFloat("_VintageWarmth", 0.40f);
        mat.SetFloat("_FilmGrain", 0.08f);
        mat.SetFloat("_HalationIntensity", 0.25f);

        // Cutout / Alpha
        mat.SetFloat("_AlphaClip", alphaClip ? 1.0f : 0.0f);
        if (alphaClip)
        {
            mat.SetFloat("_Cutoff", 0.5f);
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void SanitizeAllMaterialGlints()
    {
        string[] allMats = AssetDatabase.FindAssets("t:Material");
        int sanitizedCount = 0;

        foreach (string guid in allMats)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/")) continue;

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null || mat.shader.name != SHADER_NAME) continue;

            string lowerName = mat.name.ToLower();
            bool isClothOrSkin = lowerName.Contains("cloth") || lowerName.Contains("uniform") ||
                                 lowerName.Contains("skin") || lowerName.Contains("body") ||
                                 lowerName.Contains("tops") || lowerName.Contains("bottoms") ||
                                 lowerName.Contains("shoes") || lowerName.Contains("dress") ||
                                 lowerName.Contains("face") || lowerName.Contains("mouth") ||
                                 lowerName.Contains("brow") || lowerName.Contains("eyeline");

            if (isClothOrSkin)
            {
                if (mat.GetFloat("_SpecIntensity") > 0.01f)
                {
                    mat.SetFloat("_SpecIntensity", 0.0f);
                    EditorUtility.SetDirty(mat);
                    sanitizedCount++;
                }
            }
        }

        Debug.Log($"[MDI Costumes] Sanitized specular glints across {sanitizedCount} cloth & skin materials.");
    }
}

#endif
