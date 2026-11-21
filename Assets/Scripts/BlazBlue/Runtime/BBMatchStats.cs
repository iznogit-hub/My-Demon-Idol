namespace DemonIdol.BlazBlue
{
    public class BBMatchStats
    {
        public int P1Damage;
        public int P2Damage;
        public int P1MaxCombo;
        public int P2MaxCombo;
        public int P1Supers;
        public int P2Supers;
        int _p1Combo;
        int _p2Combo;

        public void RegisterHit(bool attackerIsP1, int damage, bool superMove)
        {
            if (attackerIsP1)
            {
                P1Damage += damage;
                _p1Combo++;
                if (_p1Combo > P1MaxCombo) P1MaxCombo = _p1Combo;
                _p2Combo = 0;
                if (superMove) P1Supers++;
            }
            else
            {
                P2Damage += damage;
                _p2Combo++;
                if (_p2Combo > P2MaxCombo) P2MaxCombo = _p2Combo;
                _p1Combo = 0;
                if (superMove) P2Supers++;
            }
        }

        public void RegisterWhiff()
        {
            _p1Combo = 0;
            _p2Combo = 0;
        }

        public void Reset()
        {
            P1Damage = 0;
            P2Damage = 0;
            P1MaxCombo = 0;
            P2MaxCombo = 0;
            P1Supers = 0;
            P2Supers = 0;
            _p1Combo = 0;
            _p2Combo = 0;
        }
    }
}
