namespace Quantum
{
    using Photon.Deterministic;

    public partial class RuntimePlayer
    {
        public string FighterId = "C01";

        partial void SerializeUserData(BitStream stream)
        {
            stream.Serialize(ref FighterId);
        }
    }
}