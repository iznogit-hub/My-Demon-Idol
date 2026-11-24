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
        Brute,        // C03 Ren / EX04 OfficerK (Heavy Brawler)
        Archer,       // C04 UnityChan / C09 PicoChan (Bow & Arrow)
        Spearman,     // C05 Akane / EX01 Hugo (Spear / Halberd)
        Mage,         // C06 Cultist (Arcane Staff)
        Sorceress,    // C07 StellarWitch / C05 Akane (Sorceress Focus / Levitation)
        Ninja,        // C08 SanpakuChan / C02 Aika (Dual Ninjato Swords)
        Hammer,       // C09 PicoChan / SK08 Torgrim (Colossal Warhammer)
        Knight,       // C10 Reika / C11 Casual1 (Sword & Shield)
        Crossbow,     // C11 Casual1 / C12 Fina (Crossbow)
        TwoHanded     // C12 Fina / C14 Muryotaisu (Two-Handed Greatsword)
    }

    /// <summary>
    /// Manages 3D weapon props attached to a fighter's skeletal bone sockets.
    /// Supports primary (Right Hand) and secondary / offhand (Left Hand / Forearm) weapons.
    /// Guaranteed determinism: weapons are purely visual cosmetic rigs with no physics colliders.
    /// Implements lossyScale compensation so weapons never float or scale excessively regardless of rig FBX scale.
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
        public float primaryTargetLength = 0f;

        [Header("Secondary / Offhand Configuration")]
        public GameObject secondaryWeaponPrefab;
        public HumanBodyBones secondarySocket = HumanBodyBones.LeftHand;
        public Vector3 secondaryPositionOffset = Vector3.zero;
        public Vector3 secondaryRotationOffset = Vector3.zero;
        public Vector3 secondaryScale = Vector3.one;
        public float secondaryTargetLength = 0f;

        [Header("Runtime Instances")]
        [SerializeField] private GameObject _primaryInstance;
        [SerializeField] private GameObject _secondaryInstance;
        [SerializeField] private Transform _primaryBoneTransform;
        [SerializeField] private Transform _secondaryBoneTransform;
        [SerializeField] private BBWeaponTrail _primaryTrail;
        [SerializeField] private BBWeaponTrail _secondaryTrail;

        public GameObject PrimaryInstance => _primaryInstance;
        public GameObject SecondaryInstance => _secondaryInstance;
        public Transform PrimaryBoneTransform => _primaryBoneTransform;
        public Transform SecondaryBoneTransform => _secondaryBoneTransform;

        private Animator _animator;
        private bool _isInitialized = false;

        private static Material s_SumiGunmetalMat;
        private static Material s_SumiGoldMat;
        private static Material s_SumiVermilionMat;

        private static readonly string[] RIGHT_HAND_NAMES = new string[] {
            "Right wrist", "RightHand", "mixamorig:RightHand", "Hand.R", "Hand_R",
            "Right_Hand", "Bip01 R Hand", "Bip001 R Hand", "R_Hand", "HandR", "B_R_Hand", "RightHandIndex1"
        };

        private static readonly string[] LEFT_HAND_NAMES = new string[] {
            "Left wrist", "LeftHand", "mixamorig:LeftHand", "Hand.L", "Hand_L",
            "Left_Hand", "Bip01 L Hand", "Bip001 L Hand", "L_Hand", "HandL", "B_L_Hand", "LeftHandIndex1"
        };

        private static readonly string[] LEFT_FOREARM_NAMES = new string[] {
            "Left forearm", "LeftForearm", "LeftLowerArm", "mixamorig:LeftForeArm",
            "Forearm.L", "Forearm_L", "Bip01 L Forearm", "B_L_Forearm", "L_Forearm"
        };

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            // Re-verify bone attachment in Start in case Animator avatar or hierarchy was initialized after Awake
            if (_primaryInstance == null && primaryWeaponPrefab != null)
            {
                Initialize();
            }
            else if (_primaryBoneTransform == null)
            {
                LocateBoneTransforms();
                AttachWeapons();
            }
        }

        public void Initialize(Animator anim = null)
        {
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
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_animator != null && _animator.isHuman)
            {
                _primaryBoneTransform = _animator.GetBoneTransform(primarySocket);
                _secondaryBoneTransform = _animator.GetBoneTransform(secondarySocket);
            }

            // Fallback for non-standard bone hierarchies (Mixamo, Synty, custom skeletons)
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
            if (warriorType == BBWarriorType.Karate)
            {
                // Unarmed Karate fighter has no physical weapon props
                CleanupInstance(ref _primaryInstance);
                CleanupInstance(ref _secondaryInstance);
                return;
            }

            if (primaryWeaponPrefab != null && _primaryBoneTransform != null)
            {
                if (_primaryInstance != null) CleanupInstance(ref _primaryInstance);
                _primaryInstance = AttachWeaponToSocket(primaryWeaponPrefab, _primaryBoneTransform, primaryPositionOffset, primaryRotationOffset, primaryTargetLength, true);
                if (_primaryInstance != null)
                {
                    _primaryTrail = _primaryInstance.GetComponentInChildren<BBWeaponTrail>();
                }
            }

            if (secondaryWeaponPrefab != null && _secondaryBoneTransform != null)
            {
                if (_secondaryInstance != null) CleanupInstance(ref _secondaryInstance);
                _secondaryInstance = AttachWeaponToSocket(secondaryWeaponPrefab, _secondaryBoneTransform, secondaryPositionOffset, secondaryRotationOffset, secondaryTargetLength, false);
                if (_secondaryInstance != null)
                {
                    _secondaryTrail = _secondaryInstance.GetComponentInChildren<BBWeaponTrail>();
                }
            }
        }

        public GameObject AttachWeaponToSocket(GameObject prefab, Transform socketBone, Vector3 posOffset, Vector3 rotOffset, float explicitTargetLength, bool isPrimary)
        {
            if (prefab == null || socketBone == null) return null;

            // 1. Destroy any existing duplicate or orphaned weapon children under this socket
            DestroyOrphanedWeaponChildren(socketBone);

            // 2. Measure local mesh bounds (size along max axis) from prefab
            Bounds bounds = MeasureLocalMeshBounds(prefab);
            float maxAxisLength = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (maxAxisLength < 0.0001f) maxAxisLength = 1f;

            float targetLength = explicitTargetLength > 0.05f ? explicitTargetLength : GetDefaultTargetLength(warriorType, !isPrimary);
            float s = targetLength / maxAxisLength;

            // 3. Create neutral-rotation intermediate ScaleSocket to prevent shear/skew from non-uniform bone lossyScale
            GameObject scaleSocketGo = new GameObject((isPrimary ? "ScaleSocket_Primary_" : "ScaleSocket_Secondary_") + warriorType);
            Transform scaleSocket = scaleSocketGo.transform;
            scaleSocket.SetParent(socketBone, false);
            scaleSocket.localRotation = Quaternion.identity;

            Vector3 boneLossy = socketBone.lossyScale;
            float absX = Mathf.Max(Mathf.Abs(boneLossy.x), 0.0001f);
            float absY = Mathf.Max(Mathf.Abs(boneLossy.y), 0.0001f);
            float absZ = Mathf.Max(Mathf.Abs(boneLossy.z), 0.0001f);
            scaleSocket.localScale = new Vector3(1f / absX, 1f / absY, 1f / absZ);

            Vector3 clampedWorldOffset = posOffset;
            if (clampedWorldOffset.magnitude > 0.05f)
            {
                clampedWorldOffset = clampedWorldOffset.normalized * 0.05f;
            }
            scaleSocket.localPosition = socketBone.InverseTransformVector(clampedWorldOffset);
            scaleSocketGo.AddComponent<BBSpawnedWeaponMarker>();

            // 4. Instantiate weapon instance under ScaleSocket with uniform target scale and rotation offset
            GameObject instance = Instantiate(prefab, scaleSocket);
            instance.name = (isPrimary ? "Weapon_Primary_" : "Weapon_Secondary_") + warriorType;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(rotOffset);
            instance.transform.localScale = Vector3.one * s;
            instance.AddComponent<BBSpawnedWeaponMarker>();

            // 5. Sanitize physics and upgrade flat-gray materials to Sumi-e dark-gunmetal
            SanitizeWeaponInstance(instance);

            return instance;
        }

        public static float GetDefaultTargetLength(BBWarriorType type, bool isSecondary)
        {
            if (isSecondary)
            {
                switch (type)
                {
                    case BBWarriorType.Knight: return 0.52f; // Shield
                    case BBWarriorType.Ninja: return 0.76f;  // Offhand Ninjato
                    default: return 0.60f;
                }
            }

            switch (type)
            {
                case BBWarriorType.Ninja: return 0.76f;
                case BBWarriorType.Swordsman: return 0.85f;
                case BBWarriorType.Knight: return 0.85f;
                case BBWarriorType.TwoHanded: return 1.45f;
                case BBWarriorType.Spearman: return 1.85f;
                case BBWarriorType.Hammer: return 1.55f;
                case BBWarriorType.Crossbow: return 0.62f;
                case BBWarriorType.Archer: return 0.95f;
                case BBWarriorType.Mage: return 1.50f;
                case BBWarriorType.Sorceress: return 1.50f;
                case BBWarriorType.Brute: return 1.05f;
                default: return 0.85f;
            }
        }

        public static Bounds MeasureMeshBounds(GameObject instance) => MeasureWorldBounds(instance);

        public static Bounds MeasureWorldBounds(GameObject instance)
        {
            if (instance == null) return new Bounds(Vector3.zero, Vector3.zero);
            Bounds combined = new Bounds(instance.transform.position, Vector3.zero);
            bool hasBounds = false;

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r is TrailRenderer || r is LineRenderer) continue;
                if (!hasBounds)
                {
                    combined = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    combined.Encapsulate(r.bounds);
                }
            }

            return combined;
        }

        public static Bounds MeasureLocalMeshBounds(GameObject instance)
        {
            if (instance == null) return new Bounds(Vector3.zero, Vector3.zero);
            Bounds combined = new Bounds(Vector3.zero, Vector3.zero);
            bool hasBounds = false;

            var mfs = instance.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in mfs)
            {
                if (mf.sharedMesh != null)
                {
                    Bounds mb = mf.sharedMesh.bounds;
                    Vector3 localCenter = mf.transform.localPosition + mb.center;
                    Vector3 localSize = Vector3.Scale(mf.transform.localScale, mb.size);
                    Bounds childBounds = new Bounds(localCenter, localSize);
                    if (!hasBounds)
                    {
                        combined = childBounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combined.Encapsulate(childBounds);
                    }
                }
            }

            var smrs = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in smrs)
            {
                if (smr.sharedMesh != null)
                {
                    Bounds mb = smr.sharedMesh.bounds;
                    Vector3 localCenter = smr.transform.localPosition + mb.center;
                    Vector3 localSize = Vector3.Scale(smr.transform.localScale, mb.size);
                    Bounds childBounds = new Bounds(localCenter, localSize);
                    if (!hasBounds)
                    {
                        combined = childBounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combined.Encapsulate(childBounds);
                    }
                }
            }

            return combined;
        }

        public static void DestroyOrphanedWeaponChildren(Transform bone)
        {
            if (bone == null) return;

            // 1. Component-based safe marker cleanup
            var markers = bone.GetComponentsInChildren<BBSpawnedWeaponMarker>(true);
            for (int i = 0; i < markers.Length; i++)
            {
                if (markers[i] != null && markers[i].gameObject != null)
                {
                    if (Application.isPlaying) Destroy(markers[i].gameObject);
                    else DestroyImmediate(markers[i].gameObject);
                }
            }

            // 2. Safe cleanup of any lingering ScaleSocket or weapon roots
            for (int i = bone.childCount - 1; i >= 0; i--)
            {
                Transform child = bone.GetChild(i);
                if (child != null && (
                    child.name.StartsWith("ScaleSocket_") ||
                    child.name.StartsWith("Weapon_Primary_") ||
                    child.name.StartsWith("Weapon_Secondary_")))
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void CleanupInstance(ref GameObject instance)
        {
            if (instance != null)
            {
                Transform parent = instance.transform.parent;
                if (parent != null && parent.name.StartsWith("ScaleSocket_"))
                {
                    if (Application.isPlaying) Destroy(parent.gameObject);
                    else DestroyImmediate(parent.gameObject);
                }
                else
                {
                    if (Application.isPlaying) Destroy(instance);
                    else DestroyImmediate(instance);
                }
                instance = null;
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

            // Upgrade flat-gray materials to Sumi-e dark-gunmetal + subtle gold/vermilion accents
            UpgradeToSumiMaterials(instance);
        }

        private static void UpgradeToSumiMaterials(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            Material gunmetal = GetSumiGunmetalMaterial();

            foreach (var r in renderers)
            {
                if (r is TrailRenderer || r is LineRenderer) continue;

                Material[] mats = r.sharedMaterials;
                bool modified = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null)
                    {
                        mats[i] = gunmetal;
                        modified = true;
                        continue;
                    }

                    string matName = mats[i].name;
                    // Check if material is an untextured flat-gray pack material
                    if (matName.Contains("Ninja") || matName.Contains("Swordsman") ||
                        matName.Contains("Knight") || matName.Contains("2Handed") ||
                        matName.Contains("Spearman") || matName.Contains("Hammer") ||
                        matName.Contains("Crossbow") || matName.Contains("Archer") ||
                        matName.Contains("Mage") || matName.Contains("Sorceress") ||
                        matName.Contains("Brute") || matName.Contains("Default"))
                    {
                        mats[i] = gunmetal;
                        modified = true;
                    }
                }
                if (modified)
                {
                    r.sharedMaterials = mats;
                }
            }
        }

        public static Material GetSumiGunmetalMaterial()
        {
            if (s_SumiGunmetalMat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("Diffuse");

                s_SumiGunmetalMat = new Material(shader)
                {
                    name = "M_Sumi_Gunmetal_Dark",
                    color = new Color(0.10f, 0.11f, 0.14f, 1f) // Dark Gunmetal #1A1D24
                };
                if (s_SumiGunmetalMat.HasProperty("_BaseColor")) s_SumiGunmetalMat.SetColor("_BaseColor", new Color(0.10f, 0.11f, 0.14f, 1f));
                if (s_SumiGunmetalMat.HasProperty("_Metallic")) s_SumiGunmetalMat.SetFloat("_Metallic", 0.85f);
                if (s_SumiGunmetalMat.HasProperty("_Smoothness")) s_SumiGunmetalMat.SetFloat("_Smoothness", 0.65f);
            }
            return s_SumiGunmetalMat;
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
