#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using DemonIdol.Editor;

namespace DemonIdol.EditorTools
{
    /// <summary>
    /// Master In-Game Menu & UI Orchestrator Studio.
    /// Provides an interactive control center to orchestrate:
    /// - 1. Title Screen & Branding (Logo, Subtitle, Demon Lord Narrative Quote, Start Button Prompt)
    /// - 2. Default Match Settings (Default P1 Fighter, Default P2 Fighter, Default Stage, Default Game Mode)
    /// - 3. Visual Aesthetic & Theme Presets (Neo-Shibuya Purple, Blood Crimson, Neon Cyan, Sacred Amber)
    /// - 4. Direct 1-Click Scene Rebuilder & In-Editor Play Tester
    /// </summary>
    public class DemonIdolMenuOrchestratorWindow : EditorWindow
    {
        private static readonly string[] CharacterIds = new string[]
        {
            "C01", "C02", "C03", "C04", "C05", "C06", "C07",
            "C08", "C09", "C10", "C11", "C12", "C13", "C14"
        };

        private static readonly string[] CharacterLabels = new string[]
        {
            "C01 - Akio (Highschool)",
            "C02 - Aika (Highschool)",
            "C03 - Ren (Blade Rushdown)",
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

        private static readonly string[] StageIds = new string[]
        {
            "ST01", "ST02", "ST03", "ST04", "ST05", "ST06"
        };

        private static readonly string[] StageLabels = new string[]
        {
            "ST01 - Whispering Grove (Sakura)",
            "ST02 - Midnight Shibuya Crosswalk",
            "ST03 - Celestial Shrine of Solitude",
            "ST04 - Cyber Idol Hologram Stage",
            "ST05 - Infernal Abyss Nethergate",
            "ST06 - Infinite Horizon Training Dojo"
        };

        private static readonly string[] GameModes = new string[]
        {
            "Arcade", "LocalVersus", "Training"
        };

        private static readonly string[] GameModeLabels = new string[]
        {
            "Arcade Ladder (Sequential AI)",
            "Local Versus (Couch 1v1 PvP)",
            "Training Dojo (Framedata Practice)"
        };

        private int _selectedP1Index = 11; // C12 Fina
        private int _selectedP2Index = 6;  // C07 StellarWitch
        private int _selectedStageIndex = 0; // ST01
        private int _selectedModeIndex = 0;  // Arcade

        private Vector2 _scrollPos;

        [MenuItem("Demon Idol/🎬 Scenes/Main Menu & UI Orchestrator Studio", priority = 19)]
        public static void OpenWindow()
        {
            var window = GetWindow<DemonIdolMenuOrchestratorWindow>("Menu Orchestrator");
            window.minSize = new Vector2(620, 640);
            window.Show();
        }

        private void OnEnable()
        {
            SyncIndicesFromConfig();
        }

        private void SyncIndicesFromConfig()
        {
            var cfg = DemonIdolMainMenuBuilder.ActiveConfig;
            for (int i = 0; i < CharacterIds.Length; i++)
            {
                if (CharacterIds[i] == cfg.defaultP1) _selectedP1Index = i;
                if (CharacterIds[i] == cfg.defaultP2) _selectedP2Index = i;
            }
            for (int i = 0; i < StageIds.Length; i++)
            {
                if (StageIds[i] == cfg.defaultStage) _selectedStageIndex = i;
            }
            for (int i = 0; i < GameModes.Length; i++)
            {
                if (GameModes[i] == cfg.defaultMode) _selectedModeIndex = i;
            }
        }

        private void OnGUI()
        {
            DrawHeader();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawThemePresetsSection();
            EditorGUILayout.Space(8);

            DrawTitleBrandingSection();
            EditorGUILayout.Space(8);

            DrawSessionDefaultsSection();
            EditorGUILayout.Space(8);

            EditorGUILayout.EndScrollView();

            DrawActionFooter();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("DEMON IDOL — MAIN MENU & UI ORCHESTRATOR", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Orchestrate branding, title text, default fighters, color aesthetics, and menu flows in 1 click.", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        private void DrawThemePresetsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("🎨 Instant Aesthetic Theme Presets", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Apply curated visual color schemes across the Title and Menu UI with 1 click:", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🟣 Neo-Shibuya\nCursed Purple", GUILayout.Height(40)))
            {
                ApplyTheme(
                    new Color(0.95f, 0.2f, 0.9f, 1f),
                    new Color(0.2f, 0.85f, 1f, 0.9f),
                    new Color(1f, 0.85f, 0.3f, 0.8f),
                    new Color(0.7f, 0.1f, 0.8f, 0.9f)
                );
            }
            if (GUILayout.Button("🔴 Demon Lord\nBlood Crimson", GUILayout.Height(40)))
            {
                ApplyTheme(
                    new Color(1f, 0.1f, 0.2f, 1f),
                    new Color(1f, 0.4f, 0.3f, 0.9f),
                    new Color(1f, 0.85f, 0.2f, 0.9f),
                    new Color(0.85f, 0.05f, 0.15f, 0.95f)
                );
            }
            if (GUILayout.Button("🔵 Cyber Idol\nNeon Cyan", GUILayout.Height(40)))
            {
                ApplyTheme(
                    new Color(0.1f, 0.9f, 1f, 1f),
                    new Color(0.2f, 1f, 0.6f, 0.9f),
                    new Color(0.9f, 0.9f, 1f, 0.9f),
                    new Color(0.05f, 0.6f, 0.85f, 0.9f)
                );
            }
            if (GUILayout.Button("🟡 Sacred Sun\nGolden Amber", GUILayout.Height(40)))
            {
                ApplyTheme(
                    new Color(1f, 0.85f, 0.2f, 1f),
                    new Color(1f, 0.6f, 0.2f, 0.9f),
                    new Color(1f, 0.95f, 0.8f, 0.85f),
                    new Color(0.9f, 0.65f, 0.1f, 0.9f)
                );
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void ApplyTheme(Color title, Color sub, Color quote, Color btn)
        {
            var cfg = DemonIdolMainMenuBuilder.ActiveConfig;
            cfg.titleColor = title;
            cfg.subtitleColor = sub;
            cfg.quoteColor = quote;
            cfg.startButtonColor = btn;
            Repaint();
        }

        private void DrawTitleBrandingSection()
        {
            var cfg = DemonIdolMainMenuBuilder.ActiveConfig;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("👑 Title Screen & Narrative Branding", EditorStyles.boldLabel);

            cfg.titleLogo = EditorGUILayout.TextField("Title Logo Text", cfg.titleLogo);
            cfg.titleColor = EditorGUILayout.ColorField("Title Logo Color", cfg.titleColor);

            EditorGUILayout.Space(3);
            cfg.subtitle = EditorGUILayout.TextField("Subtitle / Tagline", cfg.subtitle);
            cfg.subtitleColor = EditorGUILayout.ColorField("Subtitle Color", cfg.subtitleColor);

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Demon Lord Narrative Quote:");
            cfg.quote = EditorGUILayout.TextArea(cfg.quote, GUILayout.Height(42));
            cfg.quoteColor = EditorGUILayout.ColorField("Quote Text Color", cfg.quoteColor);

            EditorGUILayout.Space(3);
            cfg.buttonStartText = EditorGUILayout.TextField("Start Button Text", cfg.buttonStartText);
            cfg.startButtonColor = EditorGUILayout.ColorField("Start Button Color", cfg.startButtonColor);

            EditorGUILayout.EndVertical();
        }

        private void DrawSessionDefaultsSection()
        {
            var cfg = DemonIdolMainMenuBuilder.ActiveConfig;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("⚔️ Match & Session Defaults", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Select which fighters and stage pre-load on the character select screen:", EditorStyles.miniLabel);

            // P1
            _selectedP1Index = EditorGUILayout.Popup("Default Player 1", _selectedP1Index, CharacterLabels);
            cfg.defaultP1 = CharacterIds[_selectedP1Index];

            // P2
            _selectedP2Index = EditorGUILayout.Popup("Default Player 2 / CPU", _selectedP2Index, CharacterLabels);
            cfg.defaultP2 = CharacterIds[_selectedP2Index];

            // Stage
            _selectedStageIndex = EditorGUILayout.Popup("Default Battle Stage", _selectedStageIndex, StageLabels);
            cfg.defaultStage = StageIds[_selectedStageIndex];

            // Mode
            _selectedModeIndex = EditorGUILayout.Popup("Default Game Mode", _selectedModeIndex, GameModeLabels);
            cfg.defaultMode = GameModes[_selectedModeIndex];

            EditorGUILayout.EndVertical();
        }

        private void DrawActionFooter()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.2f, 0.9f, 0.4f, 1f);
            if (GUILayout.Button("⚡ Rebuild Main Menu with These Settings", GUILayout.Height(36)))
            {
                DemonIdolMainMenuBuilder.BuildMainMenuScene();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("🎮 Play Test in Editor", GUILayout.Height(36), GUILayout.Width(170)))
            {
                DemonIdolMainMenuBuilder.PlayMatchInEditor();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open MainMenu Scene File", GUILayout.Height(24)))
            {
                DemonIdolMainMenuBuilder.OpenMainMenuScene();
            }
            if (GUILayout.Button("Reset Defaults", GUILayout.Height(24), GUILayout.Width(120)))
            {
                DemonIdolMainMenuBuilder.ActiveConfig = new DemonIdolMainMenuBuilder.MenuOrchestrationConfig();
                SyncIndicesFromConfig();
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
    }
}
#endif
