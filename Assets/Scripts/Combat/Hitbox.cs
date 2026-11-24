using System;
using UnityEngine;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Retired legacy combat hitbox stub.
    /// Active combat box math is performed deterministically by BBMatchSim / BBBoxBuffer.
    /// Retained to maintain component GUID integrity on character prefabs without PhysX overhead.
    /// </summary>
    [System.Obsolete("Superseded by BBBoxTrack and BBBoxBuffer in Assets/Scripts/BlazBlue/. Preserved for prefab GUID integrity.", false)]
    public class Hitbox : MonoBehaviour
    {
        public FighterController Owner { get; private set; }
        public AttackData CurrentAttack { get; private set; }

        public void Initialize(FighterController owner) => Owner = owner;
        public void Activate(AttackData attack) => CurrentAttack = attack;
        public void Deactivate() => CurrentAttack = null;
    }
}
