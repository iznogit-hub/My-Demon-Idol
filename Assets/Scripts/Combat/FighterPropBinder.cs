using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Binds and manages active 3D weapon props attached to a fighter's skeletal bone sockets.
    /// Resiliently attaches to Humanoid hand sockets (RightHand, LeftHand) across varied rig naming conventions.
    /// Hooks into the 3-button combat input pipeline to fire muzzle effects, blade trails, and staff spells.
    /// </summary>
    [DisallowMultipleComponent]
    public class FighterPropBinder : MonoBehaviour
    {
        [Header("Prop Configuration")]
        [SerializeField] private FighterWeaponProp _primaryWeapon;
        [SerializeField] private FighterWeaponProp _offhandWeapon;
        [SerializeField] private bool _autoBindOnStart = true;

        [Header("Runtime Attached Sockets")]
        [SerializeField] private Transform _rightHandSocket;
        [SerializeField] private Transform _leftHandSocket;

        public FighterWeaponProp PrimaryWeapon => _primaryWeapon;
        public FighterWeaponProp OffhandWeapon => _offhandWeapon;

        private FighterController _controller;
        private Animator _animator;

        private static readonly string[] RIGHT_HAND_NAMES = new string[] {
            "Right wrist", "RightHand", "mixamorig:RightHand", "Hand.R", "Hand_R",
            "Right_Hand", "Bip01 R Hand", "Bip001 R Hand", "R_Hand", "HandR", "RightHandIndex1"
        };

        private static readonly string[] LEFT_HAND_NAMES = new string[] {
            "Left wrist", "LeftHand", "mixamorig:LeftHand", "Hand.L", "Hand_L",
            "Left_Hand", "Bip01 L Hand", "Bip001 L Hand", "L_Hand", "HandL", "LeftHandIndex1"
        };

        private void Awake()
        {
            _controller = GetComponent<FighterController>();
            _animator = GetComponentInChildren<Animator>();
            LocateSockets();
        }

        private void Start()
        {
            if (_autoBindOnStart)
            {
                BindConfiguredProps();
            }
        }

        public void LocateSockets()
        {
            if (_animator == null) _animator = GetComponentInChildren<Animator>();

            if (_animator != null && _animator.isHuman)
            {
                _rightHandSocket = _animator.GetBoneTransform(HumanBodyBones.RightHand);
                _leftHandSocket = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
            }

            if (_rightHandSocket == null)
            {
                _rightHandSocket = FindBoneRecursive(transform, RIGHT_HAND_NAMES);
            }
            if (_leftHandSocket == null)
            {
                _leftHandSocket = FindBoneRecursive(transform, LEFT_HAND_NAMES);
            }
        }

        public void SetWeapons(FighterWeaponProp primary, FighterWeaponProp offhand = null)
        {
            _primaryWeapon = primary;
            _offhandWeapon = offhand;
            BindConfiguredProps();
        }

        public void BindConfiguredProps()
        {
            LocateSockets();

            if (_primaryWeapon != null)
            {
                Transform targetSocket = (_primaryWeapon.targetBone == HumanBodyBones.LeftHand) ? _leftHandSocket : _rightHandSocket;
                if (targetSocket != null)
                {
                    AttachPropToSocket(_primaryWeapon, targetSocket);
                }
            }

            if (_offhandWeapon != null)
            {
                Transform targetSocket = (_offhandWeapon.targetBone == HumanBodyBones.RightHand) ? _rightHandSocket : _leftHandSocket;
                if (targetSocket != null)
                {
                    AttachPropToSocket(_offhandWeapon, targetSocket);
                }
            }
        }

        private void AttachPropToSocket(FighterWeaponProp prop, Transform socket)
        {
            if (prop == null || socket == null) return;

            prop.transform.SetParent(socket, false);
            prop.transform.localPosition = prop.localPositionOffset;
            prop.transform.localEulerAngles = prop.localRotationOffset;
            prop.transform.localScale = prop.localScale;
            prop.gameObject.SetActive(true);
        }

        public void OnAttackStarted(AttackData attack)
        {
            if (attack == null) return;

            if (_primaryWeapon != null)
            {
                if (_primaryWeapon.archetype == WeaponArchetype.Sword)
                {
                    _primaryWeapon.ActivateTrail(true);
                }
                else if (_primaryWeapon.archetype == WeaponArchetype.Gun)
                {
                    _primaryWeapon.TriggerMuzzleFlash();
                }
                else if (_primaryWeapon.archetype == WeaponArchetype.MageStaff)
                {
                    _primaryWeapon.TriggerStaffCast();
                }
            }
        }

        public void OnAttackEnded()
        {
            if (_primaryWeapon != null && _primaryWeapon.archetype == WeaponArchetype.Sword)
            {
                _primaryWeapon.ActivateTrail(false);
            }
        }

        private Transform FindBoneRecursive(Transform root, string[] names)
        {
            if (root == null) return null;
            for (int i = 0; i < names.Length; i++)
            {
                if (string.Equals(root.name, names[i], StringComparison.OrdinalIgnoreCase))
                {
                    return root;
                }
            }

            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindBoneRecursive(root.GetChild(i), names);
                if (found != null) return found;
            }
            return null;
        }
    }
}
