namespace Quantum
{
    using Photon.Deterministic;

    /// <summary>
    /// Quantum simulation system storing fixed-point HP, round, and the two RuntimePlayer fighter IDs.
    /// </summary>
    public unsafe class BBMatchSimulationSystem : SystemMainThread, ISignalOnPlayerAdded
    {
        public override void OnInit(Frame f)
        {
            EnsureMatchState(f);
        }

        public void OnPlayerAdded(Frame f, PlayerRef player, bool firstTime)
        {
            EnsureMatchState(f);
            var data = f.GetPlayerData(player);
            string id = !string.IsNullOrEmpty(data.FighterId) ? data.FighterId : (player == 0 ? "C01" : "C02");

            foreach (var pair in f.GetComponentIterator<BBMatchState>())
            {
                var state = pair.Component;
                if (player == 0)
                {
                    state.Fighter1Id = id;
                }
                else
                {
                    state.Fighter2Id = id;
                }
                f.Set(pair.Entity, state);
            }
        }

        public override void Update(Frame f)
        {
            EnsureMatchState(f);
        }

        private void EnsureMatchState(Frame f)
        {
            foreach (var pair in f.GetComponentIterator<BBMatchState>())
            {
                return;
            }

            var entity = f.Create();
            f.Set(entity, new BBMatchState
            {
                Fighter1HP = (FP)11000,
                Fighter2HP = (FP)11000,
                Round = 1,
                Fighter1Id = "C01",
                Fighter2Id = "C02"
            });
        }
    }
}
