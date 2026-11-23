using UnityEngine;

namespace DemonIdol.Combat
{
    public enum BodyZone
    {
        Head,  // High attacks target
        Torso, // Mid attacks target
        Legs   // Low attacks target
    }

    [RequireComponent(typeof(Collider))]
    [System.Obsolete("Superseded by BBBodyBoxes and BBBoxBuffer in Assets/Scripts/BlazBlue/. Do not use in new code.", false)]
    public class Hurtbox : MonoBehaviour
    {
        [SerializeField] private BodyZone _zone = BodyZone.Torso;
        public BodyZone Zone => _zone;

        [SerializeField] private FighterController _owner;
        public FighterController Owner
        {
            get
            {
                if (_owner == null) _owner = GetComponentInParent<FighterController>();
                return _owner;
            }
            private set => _owner = value;
        }

        private void Awake()
        {
            if (_owner == null)
            {
                _owner = GetComponentInParent<FighterController>();
            }
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        public void Initialize(FighterController owner)
        {
            Owner = owner;
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }
    }
}
