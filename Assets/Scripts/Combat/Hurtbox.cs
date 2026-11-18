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
    public class Hurtbox : MonoBehaviour
    {
        [SerializeField] private BodyZone _zone = BodyZone.Torso;
        public BodyZone Zone => _zone;

        public FighterController Owner { get; private set; }

        public void Initialize(FighterController owner)
        {
            Owner = owner;
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }
    }
}
