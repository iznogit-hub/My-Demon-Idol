using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Complete move set definition for a BlazBlue fighter.
    /// Stores all normals, command normals, specials, and system moves,
    /// building zero-allocation lookup arrays on enable.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBBMoveSet", menuName = "DemonIdol/BlazBlue/Move Set")]
    public class BBMoveSet : ScriptableObject
    {
        public string characterId = "C01";
        public BBAttackData[] moves = new BBAttackData[0];

        [Header("System Moves")]
        public BBAttackData throwForward;
        public BBAttackData throwBack;
        public BBAttackData throwAir;
        public BBAttackData counterAssault;
        public BBAttackData crushTrigger;
        public BBAttackData burst;
        public BBAttackData overdriveStart;
        public BBAttackData exceedAccel;
        public BBAttackData astralHeat;

        // Cached lookup tables built on load (no Dictionary in the combat tick)
        [NonSerialized] public BBAttackData[] groundNormals;
        [NonSerialized] public BBAttackData[] airNormals;
        [NonSerialized] public BBAttackData[] commandNormals;
        [NonSerialized] public BBAttackData[] specialsByPriority;

        private void OnEnable()
        {
            RebuildLookupTables();
        }

        public void RebuildLookupTables()
        {
            if (moves == null) return;

            var gNormals = new List<BBAttackData>();
            var aNormals = new List<BBAttackData>();
            var cNormals = new List<BBAttackData>();
            var specials = new List<BBAttackData>();

            for (int i = 0; i < moves.Length; i++)
            {
                var m = moves[i];
                if (m == null) continue;

                if (m.kind == BBMoveKind.Normal)
                {
                    if (m.airOk && !m.groundOk) aNormals.Add(m);
                    else gNormals.Add(m);
                }
                else if (m.kind == BBMoveKind.CommandNormal)
                {
                    cNormals.Add(m);
                }
                else if (m.kind == BBMoveKind.Special || m.kind == BBMoveKind.DriveMove || m.kind == BBMoveKind.DistortionDrive)
                {
                    specials.Add(m);
                }
            }

            groundNormals = gNormals.ToArray();
            airNormals = aNormals.ToArray();
            commandNormals = cNormals.ToArray();
            specialsByPriority = specials.ToArray();
        }

        public BBAttackData Find(string moveId)
        {
            if (string.IsNullOrEmpty(moveId) || moves == null) return null;
            for (int i = 0; i < moves.Length; i++)
            {
                if (moves[i] != null && moves[i].moveId == moveId)
                {
                    return moves[i];
                }
            }
            return null;
        }
    }
}
