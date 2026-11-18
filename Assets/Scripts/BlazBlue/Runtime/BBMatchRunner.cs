using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.UI;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Owns the physics-free BlazBlue sim for one match.
    /// Classic fighters are left alone unless the scheme is BlazBlue and both girls have a profile.
    /// </summary>
    public class BBMatchRunner : MonoBehaviour
    {
        public static event System.Action MatchReady;

        BBSim _sim;
        FighterController _p1;
        FighterController _p2;
        BBInputSampler _pad1;
        BBInputSampler _pad2;
        BBAnimationDriver _anim1;
        BBAnimationDriver _anim2;
        bool[] _classicInput;
        bool[] _classicHit;
        bool[] _classicHurt;
        bool[] _classicBody;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Listen()
        {
            HomegrownCombatDirector.OnMatchInitialized -= TryStart;
            HomegrownCombatDirector.OnMatchInitialized += TryStart;
        }

        static void TryStart()
        {
            if (SettingsPanel.ControlScheme != ControlScheme.BlazBlue4Button)
                return;
            HomegrownCombatDirector director = HomegrownCombatDirector.Instance;
            if (director == null) return;
            FighterController a = director.Fighter1;
            FighterController b = director.Fighter2;
            if (a == null || b == null) return;
            if (a.AppliedDefinition == null || !a.AppliedDefinition.BlazBlueReady
                || b.AppliedDefinition == null || !b.AppliedDefinition.BlazBlueReady)
            {
                Debug.LogWarning("BlazBlue scheme is on, but a fighter has no blazBlueProfile. Staying on Classic.");
                return;
            }
            BBMatchRunner runner = director.GetComponent<BBMatchRunner>();
            if (runner == null) runner = director.gameObject.AddComponent<BBMatchRunner>();
            runner.Begin(a, b);
            if (MatchReady != null) MatchReady();
        }

        public void Begin(FighterController a, FighterController b)
        {
            _p1 = a;
            _p2 = b;
            _classicInput = new bool[2];
            _classicHit = new bool[2];
            _classicHurt = new bool[2];
            _classicBody = new bool[2];
            Arm(a, 0);
            Arm(b, 1);
            BBSystemTuning asset = Resources.Load<BBSystemTuning>("BBSystemTuning");
            BBTuning tuning = asset != null ? asset.ToCore() : BBTuning.Default();
            _sim = new BBSim();
            BBMoveDef[] table = new BBMoveDef[64];
            int count = a.AppliedDefinition.blazBlueProfile.moveSet.CopyDefs(table);
            _sim.Reset(table, count, tuning);
            _sim.a.maxHp = a.AppliedDefinition.blazBlueProfile.maxHp;
            _sim.b.maxHp = b.AppliedDefinition.blazBlueProfile.maxHp;
            _sim.a.hp = _sim.a.maxHp;
            _sim.b.hp = _sim.b.maxHp;
            _sim.a.x = a.transform.position.x;
            _sim.b.x = b.transform.position.x;
            _sim.a.stylish = SettingsPanel.SimpleSpecials;
            _sim.b.stylish = SettingsPanel.SimpleSpecials;
            _anim1 = a.GetComponent<BBAnimationDriver>();
            if (_anim1 == null) _anim1 = a.gameObject.AddComponent<BBAnimationDriver>();
            _anim2 = b.GetComponent<BBAnimationDriver>();
            if (_anim2 == null) _anim2 = b.gameObject.AddComponent<BBAnimationDriver>();
            var tick = CombatTickEngine.Instance;
            if (tick != null)
            {
                tick.OnTick -= OnTick;
                tick.OnTick += OnTick;
            }
        }

        void Arm(FighterController fc, int index)
        {
            fc.externalSimulation = true;
            var input = fc.GetComponent<MobileFighterInput>();
            _classicInput[index] = input != null && input.enabled;
            if (input != null) input.enabled = false;
            var sampler = fc.GetComponent<BBInputSampler>();
            if (sampler == null) sampler = fc.gameObject.AddComponent<BBInputSampler>();
            if (index == 0) _pad1 = sampler;
            else _pad2 = sampler;
            Hitbox box = fc.GetComponentInChildren<Hitbox>();
            _classicHit[index] = box != null && box.enabled;
            if (box != null) box.enabled = false;
            Hurtbox hurt = fc.GetComponentInChildren<Hurtbox>();
            _classicHurt[index] = hurt != null && hurt.enabled;
            if (hurt != null) hurt.enabled = false;
            AkioBodyFit fit = fc.GetComponent<AkioBodyFit>();
            _classicBody[index] = fit != null && fit.enabled;
            if (fit != null) fit.enabled = false;
            if (fc.AppliedDefinition.blazBlueProfile.bbAnimator != null)
            {
                Animator animator = fc.GetComponentInChildren<Animator>();
                if (animator != null) animator.runtimeAnimatorController = fc.AppliedDefinition.blazBlueProfile.bbAnimator;
            }
        }

        void OnTick(int tick)
        {
            if (_sim == null || _p1 == null || _p2 == null) return;
            BBInputFrame f1 = _pad1 != null ? _pad1.Consume() : new BBInputFrame { numpad = 5 };
            BBInputFrame f2 = _pad2 != null && _p2.isPlayerControlled ? _pad2.Consume() : new BBInputFrame { numpad = 5 };
            _sim.Tick(f1, f2);
            Apply(_p1, _sim.a, _anim1);
            Apply(_p2, _sim.b, _anim2);
        }

        void Apply(FighterController fc, BBFighterState state, BBAnimationDriver anim)
        {
            Vector3 pos = fc.transform.position;
            pos.x = state.x;
            pos.y = state.y;
            pos.z = 0f;
            fc.transform.position = pos;
            float max = fc.maxHealth <= 0f ? 1f : fc.maxHealth;
            fc.currentHealth = max * (state.hp / (float)Mathf.Max(1, state.maxHp));
            if (state.hp <= 0 && fc.State != FighterState.Dead)
                fc.SetState(FighterState.Dead);
            if (anim != null && state.moveId > 0 && fc.AppliedDefinition != null && fc.AppliedDefinition.blazBlueProfile != null)
            {
                BBMoveSet set = fc.AppliedDefinition.blazBlueProfile.moveSet;
                if (set != null && set.moves != null)
                {
                    int index = state.moveId - 1;
                    if (index >= 0 && index < set.moves.Length && set.moves[index] != null && set.moves[index].clip != null)
                    {
                        float length = Mathf.Max(1, set.moves[index].startupFrames + set.moves[index].activeFrames + set.moves[index].recoveryFrames);
                        anim.Seek(set.moves[index].clip, state.moveTick / length);
                    }
                }
            }
        }

        void OnDisable()
        {
            if (CombatTickEngine.Instance != null) CombatTickEngine.Instance.OnTick -= OnTick;
            Restore(_p1, 0);
            Restore(_p2, 1);
        }

        void Restore(FighterController fc, int index)
        {
            if (fc == null || _classicInput == null) return;
            fc.externalSimulation = false;
            var input = fc.GetComponent<MobileFighterInput>();
            if (input != null) input.enabled = _classicInput[index];
            Hitbox box = fc.GetComponentInChildren<Hitbox>();
            if (box != null) box.enabled = _classicHit[index];
            Hurtbox hurt = fc.GetComponentInChildren<Hurtbox>();
            if (hurt != null) hurt.enabled = _classicHurt[index];
            AkioBodyFit fit = fc.GetComponent<AkioBodyFit>();
            if (fit != null) fit.enabled = _classicBody[index];
        }

        public BBSim Sim => _sim;
    }
}
