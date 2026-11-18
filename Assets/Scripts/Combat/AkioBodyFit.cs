using System.Collections.Generic;
using UnityEngine;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Crouch and air pushbox / hurtbox fit for Akio. Added at runtime, never on a prefab.
    /// Standing restores the controller and colliders that were cached on install.
    /// </summary>
    public class AkioBodyFit : MonoBehaviour
    {
        const float CrouchHeight = 0.55f;
        const float CrouchRadius = 0.22f;
        const float AirHeight = 1.10f;
        const float AirRadius = 0.22f;

        struct ColSnapshot
        {
            public Collider Col;
            public Vector3 Size;
            public Vector3 Center;
            public float Height;
            public float Radius;
            public int Kind;
        }

        FighterController _fc;
        CharacterController _cc;
        float _standHeight;
        float _standRadius;
        Vector3 _standCenter;
        bool _ready;
        readonly List<ColSnapshot> _cols = new List<ColSnapshot>();
        int _pose;

        public static void Ensure(FighterController fc)
        {
            if (fc == null || !fc.UsesAkioMovelist) return;
            var fit = fc.GetComponent<AkioBodyFit>();
            if (fit == null) fit = fc.gameObject.AddComponent<AkioBodyFit>();
            fit.Cache(fc);
        }

        void Cache(FighterController fc)
        {
            _fc = fc;
            if (_ready) return;
            _cc = fc.GetComponent<CharacterController>();
            if (_cc != null)
            {
                _standHeight = _cc.height;
                _standRadius = _cc.radius;
                _standCenter = _cc.center;
            }
            var hurts = fc.GetComponentsInChildren<Hurtbox>(true);
            for (int i = 0; i < hurts.Length; i++)
            {
                var col = hurts[i].GetComponent<Collider>();
                if (col == null) continue;
                var snap = new ColSnapshot { Col = col };
                if (col is CapsuleCollider cap)
                {
                    snap.Kind = 1;
                    snap.Height = cap.height;
                    snap.Radius = cap.radius;
                    snap.Center = cap.center;
                }
                else if (col is BoxCollider box)
                {
                    snap.Kind = 2;
                    snap.Size = box.size;
                    snap.Center = box.center;
                }
                else if (col is SphereCollider sphere)
                {
                    snap.Kind = 3;
                    snap.Radius = sphere.radius;
                    snap.Center = sphere.center;
                }
                _cols.Add(snap);
            }
            _ready = true;
        }

        void LateUpdate()
        {
            if (_fc == null || !_fc.UsesAkioMovelist || !_ready) return;
            int pose = 0;
            if (!_fc.IsGrounded) pose = 2;
            else if (_fc.State == FighterState.Crouch) pose = 1;
            if (pose == _pose) return;
            _pose = pose;
            Apply(pose);
        }

        static float Pick(float authored, float fallback)
        {
            return authored > 0f ? authored : fallback;
        }

        void Apply(int pose)
        {
            CharacterDefinition def = _fc != null ? _fc.AppliedDefinition : null;
            float crouchH = Pick(def != null ? def.depthCrouchHeight : 0f, CrouchHeight);
            float crouchR = Pick(def != null ? def.depthCrouchRadius : 0f, CrouchRadius);
            float airH = Pick(def != null ? def.depthAirHeight : 0f, AirHeight);
            float airR = Pick(def != null ? def.depthAirRadius : 0f, AirRadius);
            if (_cc != null)
            {
                if (pose == 1)
                {
                    _cc.height = crouchH;
                    _cc.radius = crouchR;
                    _cc.center = new Vector3(_standCenter.x, crouchH * 0.5f, _standCenter.z);
                }
                else if (pose == 2)
                {
                    _cc.height = airH;
                    _cc.radius = airR;
                    _cc.center = new Vector3(_standCenter.x, airH * 0.5f, _standCenter.z);
                }
                else
                {
                    _cc.height = _standHeight;
                    _cc.radius = _standRadius;
                    _cc.center = _standCenter;
                }
            }

            float airScale = Pick(def != null ? def.depthAirHurtScale : 0f, 0.85f);
            float leg = pose == 1 ? Pick(def != null ? def.depthCrouchLegScale : 0f, 0.55f) : pose == 2 ? airScale : 1f;
            float torso = pose == 1 ? Pick(def != null ? def.depthCrouchTorsoScale : 0f, 0.75f) : pose == 2 ? airScale : 1f;
            float head = pose == 1 ? Pick(def != null ? def.depthCrouchHeadScale : 0f, 0.35f) : 1f;
            for (int i = 0; i < _cols.Count; i++)
            {
                var snap = _cols[i];
                if (snap.Col == null) continue;
                var zone = snap.Col.GetComponent<Hurtbox>();
                float s = 1f;
                if (zone != null)
                {
                    if (zone.Zone == BodyZone.Legs) s = leg;
                    else if (zone.Zone == BodyZone.Torso) s = torso;
                    else s = head;
                }
                if (snap.Kind == 1 && snap.Col is CapsuleCollider cap)
                {
                    cap.height = snap.Height * s;
                    cap.radius = snap.Radius * Mathf.Max(0.65f, s);
                    cap.center = snap.Center;
                }
                else if (snap.Kind == 2 && snap.Col is BoxCollider box)
                {
                    box.size = new Vector3(snap.Size.x, snap.Size.y * s, snap.Size.z);
                    box.center = snap.Center;
                }
                else if (snap.Kind == 3 && snap.Col is SphereCollider sphere)
                {
                    sphere.radius = snap.Radius * Mathf.Max(0.5f, s);
                    sphere.center = snap.Center;
                }
            }
        }
    }
}
