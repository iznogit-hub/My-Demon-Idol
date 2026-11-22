using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Content Registry for StageDefinitions.
    /// Dynamically discovers authored StageDefinitions from Resources,
    /// providing a single source of truth for StageSelectUI without hardcoding stages.
    /// </summary>
    public static class StageRegistry
    {
        private static List<StageDefinition> _cachedStages;

        private static readonly Dictionary<string, string> _stageAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Low Poly Desert", "ST01" },
            { "ST01_LowPolyDesert", "ST01" },
            { "Reactor Core Silo", "ST01" },
            { "Central Courtyard", "ST01" },
            { "College Raimon Courtyard", "ST01" },
            { "ST01_CentralCourtyard", "ST01" },
            { "ST01", "ST01" },

            { "RTS Sci-Fi Game Assets v1", "ST02" },
            { "RTS Sci-Fi", "ST02" },
            { "ST02_RTSSciFiHangar", "ST02" },
            { "Heavy Cyber Hangar", "ST02" },
            { "Sakura Main Gate", "ST02" },
            { "College Raimon Gate", "ST02" },
            { "ST02_SakuraMainGate", "ST02" },
            { "ST02", "ST02" },

            { "Free Demo Of Low Poly Space Alien Worlds 3D Asset Pack", "ST03" },
            { "Space Alien Worlds", "ST03" },
            { "ST03_SpaceAlienWorlds", "ST03" },
            { "Neo Shibuya Colosseum", "ST03" },
            { "Rooftop Observatory", "ST03" },
            { "College Raimon Rooftop", "ST03" },
            { "ST03_RooftopObservatory", "ST03" },
            { "ST03", "ST03" },

            { "Cartoon City FREE - Low Poly City 3D Models Pack", "ST04" },
            { "Cartoon City", "ST04" },
            { "ST04_CartoonCity", "ST04" },
            { "Ink Fortress Cathedral", "ST04" },
            { "Martial Arts Dojo", "ST04" },
            { "College Raimon Dojo", "ST04" },
            { "ST04_MartialArtsDojo", "ST04" },
            { "ST04", "ST04" },

            { "PixelDrift: Metro Subway for Boomer FPS VR", "ST05" },
            { "Metro Subway", "ST05" },
            { "ST05_MetroSubway", "ST05" },
            { "Android Research Lab", "ST05" },
            { "Athletic Stadium", "ST05" },
            { "College Raimon Stadium", "ST05" },
            { "ST05_AthleticStadium", "ST05" },
            { "ST05", "ST05" },

            { "Modern Office Tower", "ST06" },
            { "mordern office tower", "ST06" },
            { "ST06_ModernOfficeTower", "ST06" },
            { "Monolith Crimson Gate", "ST06" },
            { "Transit Promenade", "ST06" },
            { "College Raimon Promenade", "ST06" },
            { "ST06_TransitPromenade", "ST06" },
            { "ST06", "ST06" },

            { "College Raimon", "ST07" },
            { "College Raimon // Academy Courtyard", "ST07" },
            { "College Raimon (Midnight Rain)", "ST07" },
            { "Midnight Rain Courtyard", "ST07" },
            { "ST07_CollegeRaimon", "ST07" },
            { "Foggy Summit Overlook", "ST07" },
            { "ST07", "ST07" },
            { "Demon Lord Sanctum", "ST08" },
            { "ST08", "ST08" },
            { "Volumetric Mist Turbine", "ST09" },
            { "ST09", "ST09" },
            { "Subway Underpass Dais", "ST10" },
            { "ST10", "ST10" },
            { "Neon Spotlight Runway", "ST11" },
            { "ST11", "ST11" },
            { "Abyssal Gargoyle Altar", "ST12" },
            { "ST12", "ST12" },
            { "flooded_grounds", "ST01" }
        };

        public static string ResolveCanonicalStageId(string idOrName)
        {
            if (string.IsNullOrEmpty(idOrName)) return idOrName;
            if (_stageAliases.TryGetValue(idOrName, out var canonical))
            {
                return canonical;
            }
            return idOrName;
        }

        public static IReadOnlyList<StageDefinition> GetAllStages()
        {
            if (_cachedStages == null || _cachedStages.Count == 0)
            {
                ReloadRegistry();
            }
            return _cachedStages;
        }

        public static StageDefinition GetPrimaryStage()
        {
            var list = GetAllStages();
            return list != null && list.Count > 0 ? list[0] : null;
        }

        public static StageDefinition GetStageById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            string canonicalId = ResolveCanonicalStageId(id);

            var list = GetAllStages();
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].id, id, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(list[i].id, canonicalId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(list[i].displayName, id, StringComparison.OrdinalIgnoreCase))
                {
                    return list[i];
                }
            }
            return list.Count > 0 ? list[0] : null;
        }

        public static void RegisterStage(StageDefinition stage)
        {
            if (ReferenceEquals(stage, null) || string.IsNullOrEmpty(stage.id)) return;
            if (_cachedStages == null) _cachedStages = new List<StageDefinition>();

            for (int i = 0; i < _cachedStages.Count; i++)
            {
                if (string.Equals(_cachedStages[i].id, stage.id, StringComparison.OrdinalIgnoreCase))
                {
                    _cachedStages[i] = stage;
                    return;
                }
            }
            _cachedStages.Add(stage);
        }

        public static void ReloadRegistry()
        {
            _cachedStages = new List<StageDefinition>();

            // 1. Authoritative ST07 and additional stages from MDIRosterDatabase
            var rosterDb = DemonIdol.Data.MDIRosterDatabase.Instance;
            if (rosterDb != null)
            {
                if (rosterDb.st07Stage != null && !_cachedStages.Contains(rosterDb.st07Stage))
                {
                    _cachedStages.Add(rosterDb.st07Stage);
                }
                if (rosterDb.additionalStages != null)
                {
                    for (int i = 0; i < rosterDb.additionalStages.Length; i++)
                    {
                        var s = rosterDb.additionalStages[i];
                        if (s != null && !_cachedStages.Contains(s))
                        {
                            _cachedStages.Add(s);
                        }
                    }
                }
            }

            // 2. Discover all StageDefinition assets in Resources
            try
            {
                var loaded = Resources.LoadAll<StageDefinition>("");
                if (loaded != null && loaded.Length > 0)
                {
                    for (int i = 0; i < loaded.Length; i++)
                    {
                        if (loaded[i] != null && !_cachedStages.Contains(loaded[i]))
                        {
                            _cachedStages.Add(loaded[i]);
                        }
                    }
                }
            }
            catch
            {
                // Standalone test runner / non-native Unity fallback
            }

            // 3. Ensure author-approved signature 6 stages exist
            EnsureStage("ST01", "Low Poly Desert", "Sun-baked canyon floor with sandstone mesa spires, wind-sculpted dunes, and warm amber dust haze.", "Desert Mirage BGM", "Prefabs/Stages/ST01_LowPolyDesert");
            EnsureStage("ST02", "RTS Sci-Fi Game Assets v1", "High-tech industrial gantry platform, modular cyber conduits, cargo units, and neon cyan status lines.", "Cyber Hangar BGM", "Prefabs/Stages/ST02_RTSSciFiHangar");
            EnsureStage("ST03", "Space Alien Worlds", "Extraterrestrial alien plateau framed by bioluminescent crystal formations and deep violet nebulae.", "Alien Worlds BGM", "Prefabs/Stages/ST03_SpaceAlienWorlds");
            EnsureStage("ST04", "Cartoon City", "Vibrant stylized downtown street corner, storefront facades, asphalt platform, and sunny daylight.", "Cartoon City BGM", "Prefabs/Stages/ST04_CartoonCity");
            EnsureStage("ST05", "PixelDrift: Metro Subway", "Gritty retro underground transit platform flanked by subway tracks, rusted pillars, and neon signs.", "Subway Underground BGM", "Prefabs/Stages/ST05_MetroSubway");
            EnsureStage("ST06", "Modern Office Tower", "High-altitude skyscraper rooftop helipad overlooking a sprawling metropolis skyline at dusk.", "Skyline Tower BGM", "Prefabs/Stages/ST06_ModernOfficeTower");
            EnsureStage("ST07", "College Raimon", "Sprawling Japanese high school & college campus courtyard lined with sakura cherry blossoms, school gates, and campus buildings.", "Academy Courtyard BGM", "Prefabs/Stages/ST07_CollegeRaimon");
            EnsureStage("ST08", "Demon Lord Sanctum", "Central fortress throne room radiating ominous pink neon energy signatures.", "Demon Lord Sanctum BGM", "Stages/Backgrounds/Stage_08_DemonLordThrone");
            EnsureStage("ST09", "Volumetric Mist Turbine", "Sub-level atmospheric intake vault with subterranean fog and amber lighting.", "Volumetric Mist Turbine BGM", "Stages/Backgrounds/Stage_09_MistTurbine");
            EnsureStage("ST10", "Subway Underpass Dais", "Underground industrial concourse flanked by twin illuminated transport tubes.", "Subway Underpass Dais BGM", "Stages/Backgrounds/Stage_10_SubwayDais");
            EnsureStage("ST11", "Neon Spotlight Runway", "Open outdoor runway bathed in overhead arc-lamp spotlights and neon pink accents.", "Neon Spotlight Runway BGM", "Stages/Backgrounds/Stage_11_NeonRunway");
            EnsureStage("ST12", "Abyssal Gargoyle Altar", "Gargoyle-flanked gateway overlooking a ruined wasteland at dusk.", "Abyssal Gargoyle Altar BGM", "Stages/Backgrounds/Stage_12_GargoyleAltar");

            // 3. Fallback for legacy flooded_grounds
            EnsureStage("flooded_grounds", "Flooded Grounds - Stone Bridge", "Ancient overgrown stone bridge suspended over mystic waters. Features wall splats and infinite vertical air space.", "Ancient Torii Drums", "Stages/Backgrounds/Stage_01_ReactorShaft");
        }

        private static void EnsureStage(string id, string displayName, string desc, string bgm, string bgResourcePath = null)
        {
            for (int i = 0; i < _cachedStages.Count; i++)
            {
                if (string.Equals(_cachedStages[i].id, id, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(_cachedStages[i].backgroundResourcePath) && !string.IsNullOrEmpty(bgResourcePath))
                    {
                        _cachedStages[i].backgroundResourcePath = bgResourcePath;
                    }
                    return; // Already present
                }
            }

            var stage = MoveSet.CreateSafeInstance<StageDefinition>();
            stage.id = id;
            stage.displayName = displayName;
            stage.sceneName = "UniversalFighter_Demo";
            stage.playableWidth = 23.0f;
            stage.wallThreshold = 10.2f;
            stage.groundSurfaceY = 0.0f;
            stage.cameraMinZ = -4.8f;
            stage.cameraMaxZ = -10.5f;
            stage.cameraMinY = 1.0f;
            stage.cameraMaxY = 6.5f;
            stage.bgmTrackName = bgm;
            stage.backgroundResourcePath = bgResourcePath;
            stage.signatureArenaAuthored = true;
            stage.description = desc;
            stage.p1SpawnPosition = new Vector3(-2.5f, 0f, 0f);
            stage.p2SpawnPosition = new Vector3(2.5f, 0f, 0f);
            _cachedStages.Add(stage);
        }
    }
}
