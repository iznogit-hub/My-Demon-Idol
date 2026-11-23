namespace Quantum
{
    using UnityEngine;
    using DemonIdol.BlazBlue;

    /// <summary>
    /// Quantum view that drives BBFighterBrain from BBMatchState and synchronizes inputs per rollback frame.
    /// </summary>
    public class BBMatchQuantumView : QuantumMonoBehaviour
    {
        public BBFighterBrain brain1;
        public BBFighterBrain brain2;

        private BBInputFrame _latestInputP1;
        private BBInputFrame _latestInputP2;
        private byte _prevHeldP1;
        private byte _prevHeldP2;

        public BBInputFrame LatestInputP1 => _latestInputP1;
        public BBInputFrame LatestInputP2 => _latestInputP2;

        public void BindBrains(BBFighterBrain p1, BBFighterBrain p2)
        {
            brain1 = p1;
            brain2 = p2;

            if (brain1 != null)
            {
                brain1.customInputProvider = (tick) => _latestInputP1;
            }
            if (brain2 != null)
            {
                brain2.customInputProvider = (tick) => _latestInputP2;
            }
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

            unsafe
            {
                var p0Input = f.GetPlayerInput(0);
                if (p0Input != null)
                {
                    _latestInputP1 = BBQuantumInputCodec.QuantumInputToBBInputFrame(*p0Input, _prevHeldP1);
                    _prevHeldP1 = _latestInputP1.held;
                }

                var p1Input = f.GetPlayerInput(1);
                if (p1Input != null)
                {
                    _latestInputP2 = BBQuantumInputCodec.QuantumInputToBBInputFrame(*p1Input, _prevHeldP2);
                    _prevHeldP2 = _latestInputP2.held;
                }
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

            if (brain1 != null && brain1.customInputProvider == null)
            {
                brain1.customInputProvider = (tick) => _latestInputP1;
            }
            if (brain2 != null && brain2.customInputProvider == null)
            {
                brain2.customInputProvider = (tick) => _latestInputP2;
            }
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
