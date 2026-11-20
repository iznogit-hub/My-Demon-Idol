namespace Quantum
{
    using UnityEngine;
    using DemonIdol.BlazBlue;

    /// <summary>
    /// Quantum view that drives BBFighterBrain from BBMatchState.
    /// </summary>
    public class BBMatchQuantumView : QuantumMonoBehaviour
    {
        public BBFighterBrain brain1;
        public BBFighterBrain brain2;

        public void BindBrains(BBFighterBrain p1, BBFighterBrain p2)
        {
            brain1 = p1;
            brain2 = p2;
        }

        public void UpdateView(Frame f)
        {
            if (f == null) return;
            foreach (var pair in f.GetComponentIterator<BBMatchState>())
            {
                var state = pair.Component;
                DriveBrains(state);
                break;
            }
        }

        public void DriveBrains(BBMatchState state)
        {
            FindBrainsIfMissing();

            if (brain1 != null)
            {
                brain1.SetAuthoritativeHealth((float)state.Fighter1HP);
            }
            if (brain2 != null)
            {
                brain2.SetAuthoritativeHealth((float)state.Fighter2HP);
            }
        }

        void FindBrainsIfMissing()
        {
            if (brain1 != null && brain2 != null) return;
            var brains = FindObjectsByType<BBFighterBrain>(FindObjectsSortMode.None);
            if (brains.Length > 0 && brain1 == null) brain1 = brains[0];
            if (brains.Length > 1 && brain2 == null) brain2 = brains[1];
        }

        void LateUpdate()
        {
            if (QuantumRunner.Default != null && QuantumRunner.Default.Game != null)
            {
                var f = QuantumRunner.Default.Game.Frames.Predicted;
                if (f != null)
                {
                    UpdateView(f);
                }
            }
        }
    }
}
