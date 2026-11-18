using System;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.Input;
using DemonIdol.UI;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Non-destructive match initialization hook.
    /// When SettingsPanel.ActiveControlScheme == BlazBlue4Button, enables the 4-button pipeline,
    /// marks fighters with externalSimulation = true, and assigns BBFighterBrain.
    /// When ControlScheme == Classic3Button, completely no-ops to guarantee zero regression.
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
            ControlScheme scheme = SettingsPanel.ActiveControlScheme;
            if (scheme == ControlScheme.Classic3Button)
            {
                isBlazBlueActive = false;
                return; // 100% untouched Classic path
            }

            isBlazBlueActive = true;
            BootstrapBlazBlueFighters();
        }

        private void BootstrapBlazBlueFighters()
        {
            var fighters = FindObjectsByType<FighterController>(FindObjectsSortMode.None);
            for (int i = 0; i < fighters.Length; i++)
            {
                var fc = fighters[i];
                if (fc == null) continue;

                // 1. Enable external simulation so Classic HandleCombatTick/Update early-return
                fc.externalSimulation = true;

                // 2. Disable Classic MobileFighterInput
                var classicInput = fc.GetComponent<MobileFighterInput>();
                if (classicInput != null)
                {
                    classicInput.enabled = false;
                }

                // 3. Attach BBInputSampler
                var sampler = fc.GetComponent<BBInputSampler>();
                if (sampler == null)
                {
                    sampler = fc.gameObject.AddComponent<BBInputSampler>();
                }
                sampler.isPlayerControlled = fc.isPlayerControlled;
                sampler.playerIndex = i;

                // 4. Attach BBFighterBrain
                var brain = fc.GetComponent<BBFighterBrain>();
                if (brain == null)
                {
                    brain = fc.gameObject.AddComponent<BBFighterBrain>();
                }
                brain.fighter = fc;
                brain.sampler = sampler;

                Debug.Log($"<color=#00e676>[BBMatchBootstrap] Fighter '{fc.fighterName}' successfully upgraded to BlazBlue 4-Button mode.</color>");
            }
        }
    }
}
