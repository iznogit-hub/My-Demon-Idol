namespace MDI.Akio
{
    /// <summary>
    /// Data-driven cancel test. The lists live on each AttackData asset.
    /// A route is legal on hit, on block, or both, and only inside the move's window.
    /// </summary>
    public static class CancelQuery
    {
        public static bool InWindow(int tick, int startInclusive, int endInclusive)
        {
            return tick >= startInclusive && tick <= endInclusive;
        }

        public static bool Allows(string[] onHit, string[] onBlock, string nextId, bool hit, bool blocked)
        {
            if (string.IsNullOrEmpty(nextId)) return false;
            if (hit && Contains(onHit, nextId)) return true;
            if (blocked && Contains(onBlock, nextId)) return true;
            return false;
        }

        public static bool Contains(string[] list, string id)
        {
            if (list == null || string.IsNullOrEmpty(id)) return false;
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] == id) return true;
            }
            return false;
        }
    }
}
