namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Akio's Drive, Iron Rush. D is a three-step rush. Each D that lands banks one Momentum, up to 3.
    /// The sim banks the stock when a DriveMove hits. 236C and 632146D are written to spend it.
    /// </summary>
    public static class AkioIronRush
    {
        public const int MaxMomentum = 3;

        public static int Bank(int stocks)
        {
            return stocks >= MaxMomentum ? MaxMomentum : stocks + 1;
        }

        public static int RushCombinationDamage(int stocks)
        {
            if (stocks < 0) stocks = 0;
            if (stocks > MaxMomentum) stocks = MaxMomentum;
            return 900 + 150 * stocks;
        }

        public static int CrimsonDamage(int stocks)
        {
            if (stocks < 0) stocks = 0;
            if (stocks > MaxMomentum) stocks = MaxMomentum;
            return 3000 + 300 * stocks;
        }
    }
}
