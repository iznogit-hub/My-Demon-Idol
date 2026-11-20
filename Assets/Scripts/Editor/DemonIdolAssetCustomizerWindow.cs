#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.Editor;

namespace DemonIdol.EditorTools
{
    /// <summary>
    /// Master Fighter & Asset Customization Studio.
    /// Provides a single, clean visual dashboard to precisely customize every asset in Demon Idol:
    /// - 1. Identity & RPG Combat Stats (Speed, Power, Reach, Defense, Domain Finisher)
    /// - 2. Visuals, 3D Models, Costumes & Toon Materials (Shaders, Outlines, Colors)
    /// - 3. Combat MoveSets & Frame Data (S/A/R ticks, Damage, Hitstun, Hitboxes, Advantage)
    /// - 4. Animations & Mecanim Controllers (Idle, Attacks, Reactions, Locomotion)
    /// - 5. Quick Asset Jumps & Ping Helpers (Direct 1-click focus in Inspector or Project)
    /// </summary>
    public class DemonIdolAssetCustomizerWindow : EditorWindow
    {
        private const string CHARACTERS_DIR = "Assets/CombatData/Characters";
        private const string MOVESETS_DIR = "Assets/CombatData/MoveSets";
        private const string PREFABS_DIR = "Assets/Prefabs/Characters";

        private static readonly string[] CharacterIds = new string[]
        {
            "C01", "C02", "C04", "C05", "C06", "C07",
            "C08", "C09", "C10", "C11", "C12", "C13", "C14"
        };

        private static readonly string[] CharacterLabels = new string[]
        {
            "C01 - Akio (Highschool)",
            "C02 - Aika (Highschool)",
            "C04 - UnityChan (Locomotion)",
            "C05 - Akane (Anime Brawler)",
            "C06 - Cultist (Occult Grappler)",
            "C07 - StellarWitch (Zoner / Arcane)",
            "C08 - SanpakuChan (Counter / Parry)",
            "C09 - PicoChan (Trickster / Aerial)",
            "C10 - Reika / Kiki (Mixup Striker)",
            "C11 - Casual1 (Footsies)",
            "C12 - Fina (Lead Heroine)",
            "C13 - SapphiArtchan (Combo Martial)",
            "C14 - Muryotaisu (Heavy Juggernaut)"
        };

        private int _selectedCharIndex = 10; // Default to C12 Fina
        private int _selectedTab = 0;
        private readonly string[] _tabNames = new string[]
        {
            "👑 Identity & Stats",
            "🎨 Visuals & Model",
            "⚔️ Combat Frame Data",
            "💃 Animator & Clips",
            "📁 Asset File Links"
        };

        private Vector2 _scrollPos;
        private CharacterDefinition _currentCharDef;
        private MoveSet _currentMoveSet;
        private GameObject _currentPrefab;

        public static void OpenWindow()
        {
            var window = GetWindow<DemonIdolAssetCustomizerWindow>("Fighter Studio");
            window.minSize = new Vector2(650, 680);
            window.Show();
        }

        private void OnEnable()
        {
            LoadCurrentCharacter();
        }

        private void LoadCurrentCharacter()
        {
            if (_selectedCharIndex < 0 || _selectedCharIndex >= CharacterIds.Length) return;
            string id = CharacterIds[_selectedCharIndex];

            // 1. Load CharacterDefinition
            string[] defFiles = Directory.GetFiles(CHARACTERS_DIR, $"CharacterDefinition_{id}*.asset");
            if (defFiles != null && defFiles.Length > 0)
            {
                _currentCharDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(defFiles[0]);
            }
            else
            {
                _currentCharDef = null;
            }

            // 2. Load MoveSet
            if (_currentCharDef != null && _currentCharDef.moveSet != null)
            {
                _currentMoveSet = _currentCharDef.moveSet;
            }
            else
            {
                string[] msFiles = Directory.GetFiles(MOVESETS_DIR, $"{id}*_MoveSet.asset");
                if (msFiles != null && msFiles.Length > 0)
                {
                    _currentMoveSet = AssetDatabase.LoadAssetAtPath<MoveSet>(msFiles[0]);
                }
                else
                {
                    _currentMoveSet = null;
                }
            }

            // 3. Load Prefab
            if (_currentCharDef != null && _currentCharDef.modelPrefab != null)
            {
                _currentPrefab = _currentCharDef.modelPrefab;
            }
            else
            {
                string[] prefabs = Directory.GetFiles(PREFABS_DIR, $"{id}_*.prefab");
                if (prefabs != null && prefabs.Length > 0)
                {
                    _currentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[0]);
                }
                else
                {
                    _currentPrefab = null;
                }
            }
        }

        private void OnGUI()
        {
            DrawHeader();

            EditorGUILayout.Space(6);
            DrawCharacterSelector();

            EditorGUILayout.Space(8);
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames, GUILayout.Height(30));

            EditorGUILayout.Space(8);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            switch (_selectedTab)
            {
                case 0:
                    DrawIdentityAndStatsTab();
                    break;
                case 1:
                    DrawVisualsAndModelTab();
                    break;
                case 2:
                    DrawCombatFrameDataTab();
                    break;
                case 3:
                    DrawAnimatorAndClipsTab();
                    break;
                case 4:
                    DrawAssetFileLinksTab();
                    break;
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);
            DrawFooterActions();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("DEMON IDOL — FIGHTER & ASSET CUSTOMIZATION STUDIO", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Directly inspect, customize, and balance all character models, frame data, and stats.", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawCharacterSelector()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Select Character:", EditorStyles.boldLabel, GUILayout.Width(130));

            int newIndex = EditorGUILayout.Popup(_selectedCharIndex, CharacterLabels, GUILayout.Height(22));
            if (newIndex != _selectedCharIndex)
            {
                _selectedCharIndex = newIndex;
                LoadCurrentCharacter();
            }

            if (GUILayout.Button("Ping Asset", GUILayout.Width(90), GUILayout.Height(22)))
            {
                if (_currentCharDef != null)
                {
                    EditorGUIUtility.PingObject(_currentCharDef);
                    Selection.activeObject = _currentCharDef;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        // =========================================================================
        // TAB 0: IDENTITY & STATS
        // =========================================================================
        private void DrawIdentityAndStatsTab()
        {
            if (_currentCharDef == null)
            {
                EditorGUILayout.HelpBox($"CharacterDefinition for {CharacterIds[_selectedCharIndex]} not found in {CHARACTERS_DIR}.", MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Character Identity & Bio", EditorStyles.boldLabel);
            _currentCharDef.displayName = EditorGUILayout.TextField("Display Name", _currentCharDef.displayName);
            _currentCharDef.title = EditorGUILayout.TextField("Title", _currentCharDef.title);
            _currentCharDef.archetype = EditorGUILayout.TextField("Archetype", _currentCharDef.archetype);
            _currentCharDef.signatureStageId = EditorGUILayout.TextField("Signature Stage", _currentCharDef.signatureStageId);

            EditorGUILayout.LabelField("Character Lore / Description:");
            _currentCharDef.description = EditorGUILayout.TextArea(_currentCharDef.description, GUILayout.Height(50));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Tactical Combat Stats (0 - 100)", EditorStyles.boldLabel);
            _currentCharDef.speed = EditorGUILayout.Slider("Speed (Movement / Dash)", _currentCharDef.speed, 0f, 100f);
            _currentCharDef.power = EditorGUILayout.Slider("Power (Damage Scaling)", _currentCharDef.power, 0f, 100f);
            _currentCharDef.reach = EditorGUILayout.Slider("Reach (Hitbox Range)", _currentCharDef.reach, 0f, 100f);
            _currentCharDef.defense = EditorGUILayout.Slider("Defense (Damage Reduction)", _currentCharDef.defense, 0f, 100f);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Domain Expansion Finisher", EditorStyles.boldLabel);
            _currentCharDef.domainName = EditorGUILayout.TextField("Domain Name", _currentCharDef.domainName);
            EditorGUILayout.LabelField("Domain Description:");
            _currentCharDef.domainDescription = EditorGUILayout.TextArea(_currentCharDef.domainDescription, GUILayout.Height(40));
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_currentCharDef);
            }
        }

        // =========================================================================
        // TAB 1: VISUALS & MODEL
        // =========================================================================
        private void DrawVisualsAndModelTab()
        {
            if (_currentCharDef == null)
            {
                EditorGUILayout.HelpBox("CharacterDefinition not loaded.", MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Art & Visual Assets", EditorStyles.boldLabel);
            _currentCharDef.modelPrefab = (GameObject)EditorGUILayout.ObjectField("Model Prefab", _currentCharDef.modelPrefab, typeof(GameObject), false);
            _currentCharDef.animatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField("Animator Controller", _currentCharDef.animatorController, typeof(RuntimeAnimatorController), false);
            _currentCharDef.avatar = (Avatar)EditorGUILayout.ObjectField("Package Avatar", _currentCharDef.avatar, typeof(Avatar), false);
            _currentCharDef.portraitSprite = (Sprite)EditorGUILayout.ObjectField("Portrait Sprite", _currentCharDef.portraitSprite, typeof(Sprite), false);
            _currentCharDef.bannerSprite = (Sprite)EditorGUILayout.ObjectField("Banner Sprite", _currentCharDef.bannerSprite, typeof(Sprite), false);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            if (_currentCharDef.modelPrefab != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Prefab Model & Materials Inspector", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Active Prefab: {_currentCharDef.modelPrefab.name}");

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Open Prefab for Editing", GUILayout.Height(26)))
                {
                    AssetDatabase.OpenAsset(_currentCharDef.modelPrefab);
                }
                if (GUILayout.Button("Select in Project", GUILayout.Height(26)))
                {
                    Selection.activeObject = _currentCharDef.modelPrefab;
                    EditorGUIUtility.PingObject(_currentCharDef.modelPrefab);
                }
                EditorGUILayout.EndHorizontal();

                // List renderers and materials on the prefab
                var renderers = _currentCharDef.modelPrefab.GetComponentsInChildren<Renderer>(true);
                if (renderers != null && renderers.Length > 0)
                {
                    EditorGUILayout.Space(6);
                    GUILayout.Label($"Meshes & Materials ({renderers.Length} Renderers):", EditorStyles.miniBoldLabel);
                    foreach (var r in renderers)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(r.gameObject.name, GUILayout.Width(180));
                        if (r.sharedMaterial != null)
                        {
                            EditorGUILayout.ObjectField(r.sharedMaterial, typeof(Material), false);
                            if (GUILayout.Button("Ping", GUILayout.Width(50)))
                            {
                                EditorGUIUtility.PingObject(r.sharedMaterial);
                            }
                        }
                        else
                        {
                            EditorGUILayout.LabelField("No Material", EditorStyles.miniLabel);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndVertical();
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_currentCharDef);
            }
        }

        // =========================================================================
        // TAB 2: COMBAT MOVE SET & FRAME DATA
        // =========================================================================
        private void DrawCombatFrameDataTab()
        {
            if (_currentMoveSet == null)
            {
                EditorGUILayout.HelpBox($"No MoveSet assigned or found for {CharacterIds[_selectedCharIndex]}.", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label($"MoveSet: {_currentMoveSet.name} ({_currentMoveSet.characterId})", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Frame data convention: S = Startup, A = Active, R = Recovery. (60 FPS ticks)", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            DrawAttackDataRow("Light Attack (J / Light)", _currentMoveSet.standingL != null ? _currentMoveSet.standingL : _currentMoveSet.lightAttack);
            DrawAttackDataRow("Heavy Attack (K / Heavy)", _currentMoveSet.standingH != null ? _currentMoveSet.standingH : _currentMoveSet.heavyAttack);
            DrawAttackDataRow("Drive Skill (L / Skill)", _currentMoveSet.neutralS != null ? _currentMoveSet.neutralS : _currentMoveSet.driveSkill);
            DrawAttackDataRow("Super Finisher (I / Super)", _currentMoveSet.domainSuper);
            DrawAttackDataRow("Defensive Burst (Space+L)", _currentMoveSet.defensiveBurst);
        }

        private void DrawAttackDataRow(string slotName, AttackData attack)
        {
            if (attack == null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"{slotName}: (Unassigned)", EditorStyles.boldLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{slotName}: {attack.attackName}", EditorStyles.boldLabel);
            if (GUILayout.Button("Ping Attack Asset", GUILayout.Width(130)))
            {
                EditorGUIUtility.PingObject(attack);
                Selection.activeObject = attack;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            attack.damage = EditorGUILayout.FloatField("Damage", attack.damage, GUILayout.Width(220));
            attack.hitLevel = (HitLevel)EditorGUILayout.EnumPopup("Hit Level", attack.hitLevel, GUILayout.Width(220));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            attack.startupFrames = EditorGUILayout.IntField("Startup (S)", attack.startupFrames, GUILayout.Width(160));
            attack.activeFrames = EditorGUILayout.IntField("Active (A)", attack.activeFrames, GUILayout.Width(160));
            attack.recoveryFrames = EditorGUILayout.IntField("Recovery (R)", attack.recoveryFrames, GUILayout.Width(160));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            attack.advantageOnHit = EditorGUILayout.IntField("Advantage (Hit)", attack.advantageOnHit, GUILayout.Width(220));
            attack.advantageOnBlock = EditorGUILayout.IntField("Advantage (Block)", attack.advantageOnBlock, GUILayout.Width(220));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            attack.hitboxOffset = EditorGUILayout.Vector3Field("Hitbox Offset", attack.hitboxOffset);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            attack.hitboxSize = EditorGUILayout.Vector3Field("Hitbox Size", attack.hitboxSize);
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(attack);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        // =========================================================================
        // TAB 3: ANIMATOR & CLIPS
        // =========================================================================
        private void DrawAnimatorAndClipsTab()
        {
            if (_currentCharDef == null || _currentCharDef.animatorController == null)
            {
                EditorGUILayout.HelpBox("No Animator Controller assigned to this character.", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Active Runtime Animator Controller & Package Avatar", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("Controller", _currentCharDef.animatorController, typeof(RuntimeAnimatorController), false);
            EditorGUILayout.ObjectField("Avatar", _currentCharDef.avatar, typeof(Avatar), false);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Animator Graph Window", GUILayout.Height(26)))
            {
                Selection.activeObject = _currentCharDef.animatorController;
                EditorApplication.ExecuteMenuItem("Window/Animation/Animator");
            }
            if (GUILayout.Button("Select Controller in Project", GUILayout.Height(26)))
            {
                Selection.activeObject = _currentCharDef.animatorController;
                EditorGUIUtility.PingObject(_currentCharDef.animatorController);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Controller Parameters & States:", EditorStyles.miniBoldLabel);

            if (_currentCharDef.animatorController is UnityEditor.Animations.AnimatorController ac)
            {
                EditorGUILayout.LabelField($"Layer Count: {ac.layers.Length} | Parameters Count: {ac.parameters.Length}");
                
                string paramSummary = "";
                for (int i = 0; i < ac.parameters.Length; i++)
                {
                    paramSummary += $"{ac.parameters[i].name} ({ac.parameters[i].type})";
                    if (i < ac.parameters.Length - 1) paramSummary += ", ";
                }
                EditorGUILayout.HelpBox($"Parameters: {paramSummary}", MessageType.None);

                if (ac.layers.Length > 0 && ac.layers[0].stateMachine != null)
                {
                    var sm = ac.layers[0].stateMachine;
                    EditorGUILayout.LabelField($"Default Idle State: {(sm.defaultState != null ? sm.defaultState.name : "None")}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Total States in Base Layer: {sm.states.Length}");
                }
            }
            EditorGUILayout.EndVertical();
        }

        // =========================================================================
        // TAB 4: ASSET FILE LINKS
        // =========================================================================
        private void DrawAssetFileLinksTab()
        {
            string id = CharacterIds[_selectedCharIndex];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label($"File Paths & Project Locations ({id})", EditorStyles.boldLabel);

            DrawPathRow("Character Definition", _currentCharDef != null ? AssetDatabase.GetAssetPath(_currentCharDef) : "Not Found", _currentCharDef);
            DrawPathRow("Fighter Prefab", _currentPrefab != null ? AssetDatabase.GetAssetPath(_currentPrefab) : "Not Found", _currentPrefab);
            DrawPathRow("Combat MoveSet", _currentMoveSet != null ? AssetDatabase.GetAssetPath(_currentMoveSet) : "Not Found", _currentMoveSet);
            DrawPathRow("Animator Controller", _currentCharDef != null && _currentCharDef.animatorController != null ? AssetDatabase.GetAssetPath(_currentCharDef.animatorController) : "Not Found", _currentCharDef != null ? _currentCharDef.animatorController : null);
            DrawPathRow("Package Avatar", _currentCharDef != null && _currentCharDef.avatar != null ? AssetDatabase.GetAssetPath(_currentCharDef.avatar) : "Not Found", _currentCharDef != null ? _currentCharDef.avatar : null);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Global Game Director Locations", EditorStyles.boldLabel);
            DrawPathRow("Main Menu Scene", "Assets/Scenes/MainMenu.unity", AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/MainMenu.unity"));
            DrawPathRow("Arcade Arena Scene", "Assets/Scenes/ArcadeArena.unity", AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/ArcadeArena.unity"));
            DrawPathRow("Combat Master Menu", "Assets/Scripts/Editor/DemonIdolMasterMenu.cs", AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/Editor/DemonIdolMasterMenu.cs"));
            DrawPathRow("Master Automator", "Assets/Scripts/Editor/DemonIdolMasterAutomator.cs", AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/Editor/DemonIdolMasterAutomator.cs"));
            EditorGUILayout.EndVertical();
        }

        private void DrawPathRow(string label, string path, UnityEngine.Object assetObj)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(150));
            EditorGUILayout.TextField(path);
            if (assetObj != null)
            {
                if (GUILayout.Button("Focus", GUILayout.Width(60)))
                {
                    Selection.activeObject = assetObj;
                    EditorGUIUtility.PingObject(assetObj);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFooterActions()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            if (GUILayout.Button("💾 Save All Asset Changes", GUILayout.Height(30)))
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("<color=#00e676>[Fighter Studio] All customized assets saved successfully!</color>");
            }

            if (GUILayout.Button("⚡ Re-Hook Character & Controller", GUILayout.Height(30)))
            {
                DemonIdol.Characters.Editor.DemonIdolRosterAnimatorHook.HookAll14FightersAndAnimators();
                LoadCurrentCharacter();
            }

            if (GUILayout.Button("🎮 Test in Play Mode", GUILayout.Height(30)))
            {
                DemonIdolMainMenuBuilder.PlayMatchInEditor();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
