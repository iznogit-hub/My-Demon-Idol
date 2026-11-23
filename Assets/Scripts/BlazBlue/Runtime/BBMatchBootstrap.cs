using System;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.Input;
using DemonIdol.UI;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Match initialization. BlazBlue is the only runtime path.
    /// Classic scripts stay in the project and are disabled on the fighters when a match starts.
    /// </summary>
    public class BBMatchBootstrap : MonoBehaviour
    {
        [Header("Runtime Status")]
        public bool isBlazBlueActive = false;

        bool _bootstrapped;

        private void Awake()
        {
            // Do not bootstrap here. Awake runs before HomegrownCombatDirector
            // has applied the picked roster, and AddComponent would do the same.
        }

        private void Start()
        {
            if (_bootstrapped) return;
            var director = FindFirstObjectByType<HomegrownCombatDirector>();
            if (director != null && director.isActiveAndEnabled)
                return;
            BootstrapBlazBlueFighters(false);
        }

        public void InitializeScheme()
        {
            BootstrapBlazBlueFighters(false);
        }

        public static void EnsureMatch(bool training)
        {
            var host = FindFirstObjectByType<BBMatchBootstrap>();
            if (host == null)
            {
                var hostObj = new GameObject("BBMatchBootstrap");
                host = hostObj.AddComponent<BBMatchBootstrap>();
            }
            host.BootstrapBlazBlueFighters(training);
        }

        /// <summary>
        /// P1 is the director's first fighter. FindObjectsByType order is not a player slot.
        /// </summary>
        public static int ResolvePlayerIndex(FighterController fc)
        {
            var director = HomegrownCombatDirector.Instance;
            FighterController slot1 = director != null ? director.Fighter1 : null;
            FighterController slot2 = director != null ? director.Fighter2 : null;
            return ResolvePlayerIndex(fc, slot1, slot2);
        }

        public static int ResolvePlayerIndex(FighterController fc, FighterController slot1, FighterController slot2)
        {
            if (fc == null) return 0;
            if (slot1 != null && fc == slot1) return 0;
            if (slot2 != null && fc == slot2) return 1;
            string name = fc.gameObject.name;
            if (name.IndexOf("Player1", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("P1", StringComparison.OrdinalIgnoreCase) >= 0) return 0;
            if (name.IndexOf("Player2", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("P2", StringComparison.OrdinalIgnoreCase) >= 0) return 1;
            return fc.isPlayerControlled ? 0 : 1;
        }

        public static Canvas FindBattleCanvas()
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Canvas fallback = null;
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || !canvas.isActiveAndEnabled || !canvas.gameObject.activeInHierarchy)
                    continue;
                if (canvas.gameObject.name == "Battle_HUD_Canvas")
                    return canvas;
                if (fallback == null && canvas.renderMode != RenderMode.WorldSpace)
                    fallback = canvas;
            }
            return fallback;
        }

        public void BootstrapBlazBlueFighters(bool training)
        {
            _bootstrapped = true;
            isBlazBlueActive = true;
            var fighters = FindObjectsByType<FighterController>(FindObjectsSortMode.None);
            BBFighterBrain p1Brain = null;
            BBFighterBrain p2Brain = null;

            for (int i = 0; i < fighters.Length; i++)
            {
                var fc = fighters[i];
                if (fc == null) continue;
                if (fc.transform.parent != null && fc.transform.parent.GetComponentInParent<FighterController>() != null)
                {
                    Destroy(fc);
                    continue;
                }

                var classicInput = fc.GetComponent<MobileFighterInput>();
                if (classicInput != null) classicInput.enabled = false;

                var classicAi = fc.GetComponent<CombatAIController>();
                if (classicAi != null) classicAi.enabled = false;

                var sampler = fc.GetComponent<BBInputSampler>();
                if (sampler == null) sampler = fc.gameObject.AddComponent<BBInputSampler>();
                sampler.isPlayerControlled = fc.isPlayerControlled;
                sampler.playerIndex = ResolvePlayerIndex(fc);

                var brain = fc.GetComponent<BBFighterBrain>();
                if (brain == null) brain = fc.gameObject.AddComponent<BBFighterBrain>();
                brain.fighter = fc;
                brain.sampler = sampler;

                if (!fc.isPlayerControlled && fc.GetComponent<BBAIController>() == null)
                    fc.gameObject.AddComponent<BBAIController>();

                var definition = fc.appliedDefinition;
                if (definition == null)
                {
                    definition = CharacterRegistry.GetCharacterById(fc.fighterName);
                }
                if (definition != null && definition.blazBlueProfile is BBCharacterProfile profile)
                {
                    brain.profile = profile;
                    brain.maxHealth = profile.maxHealth > 0f ? profile.maxHealth : 10000f;
                    if (brain.health <= 0f || brain.health > brain.maxHealth)
                    {
                        brain.health = brain.maxHealth;
                    }
                }

                string charId = definition != null ? definition.id : fc.fighterName;
                bool hasModel = fc.transform.Find("Model_" + charId) != null || (definition != null && definition.modelPrefab != null);
                int moveCount = 0;
                if (brain.profile != null && brain.profile.moveSet is BBMoveSet ms && ms.moves != null)
                {
                    moveCount = ms.moves.Length;
                }
                Debug.Log($"[BBMatchBootstrap] Fighter P{sampler.playerIndex + 1} ({charId}): model={(hasModel ? "resolved" : "missing")}, profile={(brain.profile != null ? brain.profile.name : "null")}, moves={moveCount} resolved");

                var info = BBRosterArchetype.Get(definition != null ? definition.id : fc.fighterName);
                float hipH = info.hipHeight;
                float hipW = 0.34f;
                if (fc.animator != null && fc.animator.isHuman)
                {
                    Transform hipsBone = fc.animator.GetBoneTransform(HumanBodyBones.Hips);
                    if (hipsBone != null)
                    {
                        float measuredH = hipsBone.position.y - fc.transform.position.y;
                        if (measuredH >= 0.5f && measuredH <= 1.5f) hipH = measuredH;
                    }
                    Transform lLeg = fc.animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                    Transform rLeg = fc.animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
                    if (lLeg != null && rLeg != null)
                    {
                        float legDist = Vector3.Distance(lLeg.position, rLeg.position);
                        if (legDist >= 0.15f && legDist <= 0.6f) hipW = legDist;
                    }
                }
                brain.bodyBoxes = BBBodyBoxes.CreateFromHip(hipH, info.weaponReach, hipW);
                EnsureDrive(brain, info);
                AssignAnimator(fc, brain);

                if (fc.GetComponent<FighterBlobShadow>() == null)
                {
                    fc.gameObject.AddComponent<FighterBlobShadow>();
                }

                brain.MirrorHealthToFighter();

                if (sampler.playerIndex == 0) p1Brain = brain;
                else p2Brain = brain;
            }

            if (p1Brain != null && p2Brain != null)
            {
                var sim = FindFirstObjectByType<BBMatchSim>();
                if (sim == null)
                {
                    var simObj = new GameObject("BBMatchSim");
                    sim = simObj.AddComponent<BBMatchSim>();
                }
                sim.RegisterFighters(p1Brain, p2Brain);
                if (sim.GetComponent<BBHitboxOverlay>() == null)
                {
                    sim.gameObject.AddComponent<BBHitboxOverlay>();
                }
            }

            if (DemonIdol.Audio.CombatAudioDirector.Instance != null)
            {
                DemonIdol.Audio.CombatAudioDirector.Instance.PlayCombatMusic();
            }

            BBHitboxOverlay.IsOverlayActive = training;
            var canvas = FindBattleCanvas();
            if (canvas != null && p1Brain != null)
            {
                // Combo readout works across all platforms (standalone, mobile, console)
                if (canvas.GetComponentInChildren<BBComboReadout>(true) == null)
                {
                    BBComboReadout.Create(canvas.transform);
                }

                if (BBTouchLayout.ShouldShowTouchControls() && canvas.GetComponentInChildren<BBTouchLayout>(true) == null)
                {
                    UiEventSystemInputBootstrap.ReconcileLoadedEventSystems();
                    BBTouchLayout.Create(canvas.transform, p1Brain.sampler);
                }
            }
        }

        static void EnsureDrive(BBFighterBrain brain, BBRosterArchetype.Info info)
        {
            var existing = brain.GetComponent<IBBDrive>();
            if (existing == null)
            {
                existing = AddDrive(brain.gameObject, info.id);
            }
            brain.drive = existing;
            existing.OnMatchStart(brain);
        }

        static IBBDrive AddDrive(GameObject host, string id)
        {
            switch (id)
            {
                case "C01": return host.AddComponent<AkioDrive>();
                case "C02": return host.AddComponent<AikaDrive>();
                case "C04": return host.AddComponent<UnityChanDrive>();
                case "C06": return host.AddComponent<CultistDrive>();
                case "C07": return host.AddComponent<StellarWitchDrive>();
                case "C08": return host.AddComponent<SanpakuChanDrive>();
                case "C09": return host.AddComponent<PicoChanDrive>();
                case "C10": return host.AddComponent<ReikaDrive>();
                case "C12": return host.AddComponent<FinaDrive>();
                default: return host.AddComponent<BBArchetypeDrive>();
            }
        }

        static void AssignAnimator(FighterController fc, BBFighterBrain brain)
        {
            var animator = fc.animator != null ? fc.animator : fc.GetComponentInChildren<Animator>();
            if (animator == null) return;
            animator.applyRootMotion = false;
            fc.animator = animator;
            var driver = fc.GetComponent<BBAnimationDriver>();
            if (driver == null) driver = fc.gameObject.AddComponent<BBAnimationDriver>();
            brain.animDriver = driver;
            driver.TeardownGraph();
            if (brain.profile != null && brain.profile.blazBlueOverrideController != null)
            {
                animator.runtimeAnimatorController = brain.profile.blazBlueOverrideController;
            }
            driver.animator = animator;
            driver.brain = brain;
            driver.InitializeGraph();
        }
    }
}
