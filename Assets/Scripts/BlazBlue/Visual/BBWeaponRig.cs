using System;
using UnityEngine;

namespace DemonIdol.BlazBlue.Visual
{
    /// <summary>
    /// Archetype enum matching the 12 ExplosiveLLC Warrior Bundles.
    /// </summary>
    public enum BBWarriorType
    {
        None,
        Swordsman,    // One-Handed Sword
        Karate,       // Unarmed Karate Kata
        Brute,        // C03 Ren (Heavy Brawler)
        Archer,       // C04 UnityChan (Bow & Arrow)
        Spearman,     // C05 Akane (Spear / Halberd)
        Mage,         // C06 Cultist (Arcane Staff)
        Sorceress,    // C07 StellarWitch (Sorceress Focus / Levitation)
        Ninja,        // C08 SanpakuChan (Dual Ninjato Swords)
        Hammer,       // C09 PicoChan (Colossal Warhammer)
        Knight,       // C10 Reika (Sword & Shield)
        Crossbow,     // C11 Casual1 (Crossbow)
        TwoHanded     // C12 Fina (Two-Handed Greatsword)
    }

    /// <summary>
    /// Manages 3D weapon props attached to a fighter's skeletal bone sockets.
    /// Supports primary (Right Hand) and secondary / offhand (Left Hand / Forearm) weapons.
    /// Guaranteed determinism: weapons are purely visual cosmetic rigs with no physics colliders.
    /// </summary>
    [DisallowMultipleComponent]
    public class BBWeaponRig : MonoBehaviour
    {
        [Header("Warrior Identity")]
        public BBWarriorType warriorType = BBWarriorType.None;

        [Header("Primary Weapon Configuration")]
        public GameObject primaryWeaponPrefab;
        public HumanBodyBones primarySocket = HumanBodyBones.RightHand;
        public Vector3 primaryPositionOffset = Vector3.zero;
        public Vector3 primaryRotationOffset = Vector3.zero;
        public Vector3 primaryScale = Vector3.one;

        [Header("Secondary / Offhand Configuration")]
        public GameObject secondaryWeaponPrefab;
        public HumanBodyBones secondarySocket = HumanBodyBones.LeftHand;
        public Vector3 secondaryPositionOffset = Vector3.zero;
        public Vector3 secondaryRotationOffset = Vector3.zero;
        public Vector3 secondaryScale = Vector3.one;

        [Header("Runtime Instances")]
        [SerializeField] private GameObject _primaryInstance;
        [SerializeField] private GameObject _secondaryInstance;
        [SerializeField] private Transform _primaryBoneTransform;
        [SerializeField] private Transform _secondaryBoneTransform;
        [SerializeField] private BBWeaponTrail _primaryTrail;
        [SerializeField] private BBWeaponTrail _secondaryTrail;

        public GameObject PrimaryInstance => _primaryInstance;
        public GameObject SecondaryInstance => _secondaryInstance;

        private Animator _animator;
        private bool _isInitialized = false;

        private static readonly string[] RIGHT_HAND_NAMES = new string[] {
            "Right wrist", "RightHand", "mixamorig:RightHand", "Hand.R", "Hand_R",
            "Right_Hand", "Bip01 R Hand", "Bip001 R Hand", "R_Hand", "HandR", "B_R_Hand"
        };

        private static readonly string[] LEFT_HAND_NAMES = new string[] {
            "Left wrist", "LeftHand", "mixamorig:LeftHand", "Hand.L", "Hand_L",
            "Left_Hand", "Bip01 L Hand", "Bip001 L Hand", "L_Hand", "HandL", "B_L_Hand"
        };

        private static readonly string[] LEFT_FOREARM_NAMES = new string[] {
            "Left forearm", "LeftForearm", "LeftLowerArm", "mixamorig:LeftForeArm",
            "Forearm.L", "Forearm_L", "Bip01 L Forearm", "B_L_Forearm", "L_Forearm"
        };

        private void Awake()
        {
            Initialize();
        }

        public void Initialize(Animator anim = null)
        {
            if (_isInitialized && anim == null) return;

            if (anim != null)
            {
                _animator = anim;
            }
            else if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            LocateBoneTransforms();
            AttachWeapons();
            _isInitialized = true;
        }

        public void LocateBoneTransforms()
        {
            if (_animator != null && _animator.isHuman)
            {
                _primaryBoneTransform = _animator.GetBoneTransform(primarySocket);
                _secondaryBoneTransform = _animator.GetBoneTransform(secondarySocket);
            }

            // Fallback for non-standard bone hierarchies
            if (_primaryBoneTransform == null)
            {
                _primaryBoneTransform = FindBoneRecursive(transform, RIGHT_HAND_NAMES);
            }
            if (_secondaryBoneTransform == null)
            {
                if (secondarySocket == HumanBodyBones.LeftLowerArm)
                {
                    _secondaryBoneTransform = FindBoneRecursive(transform, LEFT_FOREARM_NAMES);
                }
                else
                {
                    _secondaryBoneTransform = FindBoneRecursive(transform, LEFT_HAND_NAMES);
                }
            }
        }

        public void AttachWeapons()
        {
            if (primaryWeaponPrefab != null && _primaryBoneTransform != null && _primaryInstance == null)
            {
                _primaryInstance = Instantiate(primaryWeaponPrefab, _primaryBoneTransform);
                _primaryInstance.name = "Weapon_Primary_" + warriorType;
                _primaryInstance.transform.localPosition = primaryPositionOffset;
                _primaryInstance.transform.localRotation = Quaternion.Euler(primaryRotationOffset);
                Vector3 parentLossy = _primaryBoneTransform.lossyScale;
                Vector3 targetScale = primaryScale;
                if (Mathf.Abs(parentLossy.x) > 0.0001f) targetScale.x /= parentLossy.x;
                if (Mathf.Abs(parentLossy.y) > 0.0001f) targetScale.y /= parentLossy.y;
                if (Mathf.Abs(parentLossy.z) > 0.0001f) targetScale.z /= parentLossy.z;
                _primaryInstance.transform.localScale = targetScale;
                SanitizeWeaponInstance(_primaryInstance);
                _primaryTrail = _primaryInstance.GetComponentInChildren<BBWeaponTrail>();
            }

            if (secondaryWeaponPrefab != null && _secondaryBoneTransform != null && _secondaryInstance == null)
            {
                _secondaryInstance = Instantiate(secondaryWeaponPrefab, _secondaryBoneTransform);
                _secondaryInstance.name = "Weapon_Secondary_" + warriorType;
                _secondaryInstance.transform.localPosition = secondaryPositionOffset;
                _secondaryInstance.transform.localRotation = Quaternion.Euler(secondaryRotationOffset);
                Vector3 parentLossy = _secondaryBoneTransform.lossyScale;
                Vector3 targetScale = secondaryScale;
                if (Mathf.Abs(parentLossy.x) > 0.0001f) targetScale.x /= parentLossy.x;
                if (Mathf.Abs(parentLossy.y) > 0.0001f) targetScale.y /= parentLossy.y;
                if (Mathf.Abs(parentLossy.z) > 0.0001f) targetScale.z /= parentLossy.z;
                _secondaryInstance.transform.localScale = targetScale;
                SanitizeWeaponInstance(_secondaryInstance);
                _secondaryTrail = _secondaryInstance.GetComponentInChildren<BBWeaponTrail>();
            }
        }

        public void SetWeaponTrailsActive(bool active)
        {
            if (_primaryTrail != null) _primaryTrail.SetEmitting(active);
            if (_secondaryTrail != null) _secondaryTrail.SetEmitting(active);
        }

        public void SetWeaponsVisible(bool visible)
        {
            if (_primaryInstance != null) _primaryInstance.SetActive(visible);
            if (_secondaryInstance != null) _secondaryInstance.SetActive(visible);
        }

        private static void SanitizeWeaponInstance(GameObject instance)
        {
            if (instance == null) return;
            // Remove any Colliders so weapon props never cause physics jitter
            var colliders = instance.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
            // Remove any Rigidbodies
            var bodies = instance.GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in bodies)
            {
                if (Application.isPlaying) Destroy(rb);
                else DestroyImmediate(rb);
            }
            // Disable shadow casting on weapon trails to prevent native graphics crash in RenderShadowCasterParts
            var trails = instance.GetComponentsInChildren<TrailRenderer>(true);
            foreach (var tr in trails)
            {
                tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tr.receiveShadows = false;
                tr.Clear();
            }
            var lines = instance.GetComponentsInChildren<LineRenderer>(true);
            foreach (var lr in lines)
            {
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
            }
        }

        private static Transform FindBoneRecursive(Transform current, string[] candidates)
        {
            if (current == null) return null;

            for (int i = 0; i < candidates.Length; i++)
            {
                if (string.Equals(current.name, candidates[i], StringComparison.OrdinalIgnoreCase))
                {
                    return current;
                }
            }

            for (int i = 0; i < current.childCount; i++)
            {
                Transform found = FindBoneRecursive(current.GetChild(i), candidates);
                if (found != null) return found;
            }

            return null;
        }
    }
}
