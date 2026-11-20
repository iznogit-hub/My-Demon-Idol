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

        private void Awake()
        {
            InitializeScheme();
        }

        public void InitializeScheme()
        {
            isBlazBlueActive = true;
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
            host.isBlazBlueActive = true;
            host.BootstrapBlazBlueFighters(training);
        }

        public void BootstrapBlazBlueFighters(bool training)
        {
            isBlazBlueActive = true;
            var fighters = FindObjectsByType<FighterController>(FindObjectsSortMode.None);
            BBFighterBrain p1Brain = null;
            BBFighterBrain p2Brain = null;

            for (int i = 0; i < fighters.Length; i++)
            {
                var fc = fighters[i];
                if (fc == null) continue;
                if (fc.transform.parent != null && fc.transform.parent.GetComponent<FighterController>() != null) continue;

                fc.externalSimulation = true;

                var classicInput = fc.GetComponent<MobileFighterInput>();
                if (classicInput != null) classicInput.enabled = false;

                var sampler = fc.GetComponent<BBInputSampler>();
                if (sampler == null) sampler = fc.gameObject.AddComponent<BBInputSampler>();
                sampler.isPlayerControlled = fc.isPlayerControlled;
                sampler.playerIndex = i;

                var brain = fc.GetComponent<BBFighterBrain>();
                if (brain == null) brain = fc.gameObject.AddComponent<BBFighterBrain>();
                brain.fighter = fc;
                brain.sampler = sampler;

                var definition = fc.appliedDefinition;
                if (definition == null)
                {
                    definition = CharacterRegistry.GetCharacterById(fc.fighterName);
                }
                if (definition != null && definition.blazBlueProfile is BBCharacterProfile profile)
                {
                    brain.profile = profile;
                    brain.maxHealth = profile.maxHealth > 0f ? profile.maxHealth : 11000f;
                    if (brain.health <= 0f || brain.health > brain.maxHealth)
                    {
                        brain.health = brain.maxHealth;
                    }
                }

                var info = BBRosterArchetype.Get(definition != null ? definition.id : fc.fighterName);
                brain.bodyBoxes = BBBodyBoxes.CreateFromHip(info.hipHeight, info.weaponReach);
                EnsureDrive(brain, info);
                AssignAnimator(fc, brain);

                brain.MirrorHealthToFighter();

                if (p1Brain == null) p1Brain = brain;
                else if (p2Brain == null) p2Brain = brain;
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

            BBHitboxOverlay.IsOverlayActive = training;
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
