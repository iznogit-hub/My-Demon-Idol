namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Aika's Drive, Resonance Nail. 5D, 2D and j.D spend a nail stock. 6D detonates.
    /// Stocks regenerate on their own. Overdrive stops the stock cost.
    /// </summary>
    public static class AikaResonanceNail
    {
        public const int MaxNails = 5;

        public static int Spend(int stocks, bool overdrive)
        {
            if (overdrive) return stocks;
            return stocks > 0 ? stocks - 1 : 0;
        }
    }
}
