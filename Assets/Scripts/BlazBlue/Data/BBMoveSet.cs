using UnityEngine;

namespace DemonIdol.BlazBlue
{
    [CreateAssetMenu(menuName = "DemonIdol/BlazBlue/Move Set")]
    public class BBMoveSet : ScriptableObject
    {
        public string characterId = "C01";
        public BBAttackData[] moves = new BBAttackData[0];
        public BBAttackData throwForward;
        public BBAttackData throwBack;
        public BBAttackData throwAir;
        public BBAttackData counterAssault;
        public BBAttackData crushTrigger;
        public BBAttackData burst;
        public BBAttackData overdriveStart;
        public BBAttackData exceedAccel;
        public BBAttackData astralHeat;

        public int CopyDefs(BBMoveDef[] dst)
        {
            int n = 0;
            if (moves == null || dst == null) return 0;
            for (int i = 0; i < moves.Length && n < dst.Length; i++)
            {
                BBAttackData atk = moves[i];
                if (atk == null) continue;
                BBMoveDef def = atk.ToDef(n + 1);
                if (atk.gatlingInto != null)
                {
                    for (int g = 0; g < atk.gatlingInto.Length && g < 8; g++)
                    {
                        int target = IndexOf(atk.gatlingInto[g]);
                        if (target < 0) continue;
                        int id = target + 1;
                        if (g == 0) def.gatling0 = id;
                        else if (g == 1) def.gatling1 = id;
                        else if (g == 2) def.gatling2 = id;
                        else if (g == 3) def.gatling3 = id;
                        else if (g == 4) def.gatling4 = id;
                        else if (g == 5) def.gatling5 = id;
                        else if (g == 6) def.gatling6 = id;
                        else def.gatling7 = id;
                    }
                }
                dst[n] = def;
                n++;
            }
            return n;
        }

        public int IndexOf(string moveId)
        {
            if (moves == null || string.IsNullOrEmpty(moveId)) return -1;
            for (int i = 0; i < moves.Length; i++)
            {
                if (moves[i] != null && moves[i].moveId == moveId) return i;
            }
            return -1;
        }

        public BBAttackData Find(string moveId)
        {
            int i = IndexOf(moveId);
            return i < 0 ? null : moves[i];
        }
    }
}
