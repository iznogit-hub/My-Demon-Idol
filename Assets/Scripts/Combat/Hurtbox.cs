using System;
using UnityEngine;

namespace DemonIdol.Combat
{
    public enum BodyZone
    {
        Head,
        Torso,
        Legs
    }

    /// <summary>
    /// Retired legacy combat hurtbox stub.
    /// Active combat box math is performed deterministically by BBMatchSim / BBBoxBuffer.
    /// Retained to maintain component GUID integrity on character prefabs without PhysX overhead.
    /// </summary>
    [System.Obsolete("Superseded by BBBodyBoxes and BBBoxBuffer in Assets/Scripts/BlazBlue/. Preserved for prefab GUID integrity.", false)]
    public class Hurtbox : MonoBehaviour
    {
        [SerializeField] private BodyZone _zone = BodyZone.Torso;
        public BodyZone Zone => _zone;

        [SerializeField] private FighterController _owner;
        public FighterController Owner
        {
            get => _owner != null ? _owner : (_owner = GetComponentInParent<FighterController>());
            private set => _owner = value;
        }

        public void Initialize(FighterController owner) => Owner = owner;
    }
}
