using System;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.Audio;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Master 2.5D Projectile for Demon Idol.
    /// Handles physical flight along X-axis (Z strictly locked to 0.0),
    /// lifetime expiration, collision with opponent hurtboxes, and impact resolution.
    /// </summary>
    public class MDIProjectile : MonoBehaviour
    {
        [Header("Projectile Properties")]
        public string projectileName = "Shinobi Kunai";
        public float speed = 14f;
        public float lifetime = 2.5f;
        public int facingDirection = 1;
        public FighterController owner;
        public AttackData attackData;
        public bool pooled;

        static readonly MDIProjectile[] Active = new MDIProjectile[8];
        static int _activeCount;

        [Header("Visual & FX")]
        public GameObject impactVfxPrefab;
        public Color trailColor = Color.white;

        private float _spawnTime;
        private bool _hasHit = false;

        private void OnEnable()
        {
            _spawnTime = Time.time;
            _hasHit = false;
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void Start()
        {
            _spawnTime = Time.time;
            // Lock Z to 0.0m combat plane
            Vector3 p = transform.position;
            p.z = 0f;
            transform.position = p;
        }

        private void Update()
        {
            if (_hasHit) return;

            // Lifetime check
            if (Time.time - _spawnTime >= lifetime)
            {
                Release();
                return;
            }
            if (ClashWithOthers()) return;

            // Move along X axis strictly
            float dx = facingDirection * speed * Time.deltaTime;
            transform.position += new Vector3(dx, 0f, 0f);

            // Arena boundary clamp check
            if (Mathf.Abs(transform.position.x) > PushbackSolver.ARENA_LIMIT_X + 2.0f)
            {
                Release();
                return;
            }

            // Check collision with opponent
            CheckCollision();
        }

        private void CheckCollision()
        {
            if (owner == null || owner.opponent == null) return;

            FighterController target = owner.opponent;
            if (target.State == FighterState.Dead) return;

            // Distance check in 2D plane
            Vector3 diff = target.transform.position - transform.position;
            diff.z = 0f;

            // Target body cylinder check: horizontal within 0.85m and vertical within 1.8m
            if (Mathf.Abs(diff.x) <= 0.85f && diff.y >= -0.3f && diff.y <= 1.8f)
            {
                OnProjectileImpact(target);
            }
        }

        private void OnProjectileImpact(FighterController target)
        {
            _hasHit = true;

            // Ensure attack data
            if (attackData == null)
            {
                attackData = ScriptableObject.CreateInstance<AttackData>();
                attackData.attackName = projectileName;
                attackData.damage = 45f;
                attackData.startupFrames = 10;
                attackData.activeFrames = 4;
                attackData.recoveryFrames = 16;
                attackData.hitLevel = HitLevel.Mid;
                attackData.guardRequirement = GuardRequirement.Normal;
                attackData.hitstopAttacker = 4;
                attackData.hitstopDefender = 8;
                attackData.pushbackOnHit = 1.2f;
                attackData.pushbackOnBlock = 1.0f;
            }

            // Deliver strike to target
            target.ReceiveIncomingStrike(owner, attackData, null);
            CombatAudioDirector.Instance?.PlayProjectileImpact();

            // Spawn impact VFX
            Vector3 hitPos = transform.position;
            if (impactVfxPrefab != null)
            {
                GameObject vfx = Instantiate(impactVfxPrefab, hitPos, Quaternion.identity);
                Destroy(vfx, 1.5f);
            }
            else if (HitEffectCoordinator.Instance != null)
            {
                HitEffectCoordinator.Instance.SpawnHitBurst(hitPos, false, false);
            }

            Release();
        }

        public void ResetPooled(string name, float newSpeed, float damage, Color trail, FighterController newOwner, Vector3 spawnPos, int facing)
        {
            projectileName = name;
            speed = newSpeed;
            owner = newOwner;
            trailColor = trail;
            facingDirection = facing;
            transform.position = spawnPos;
            _spawnTime = Time.time;
            _hasHit = false;
            if (attackData == null) attackData = ScriptableObject.CreateInstance<AttackData>();
            attackData.attackName = name;
            attackData.damage = damage;
            CombatAudioDirector.Instance?.PlayProjectileFire();
        }

        public static bool Overlaps(MDIProjectile a, MDIProjectile b)
        {
            if (a == null || b == null) return false;
            return Mathf.Abs(a.transform.position.x - b.transform.position.x) < 0.45f
                && Mathf.Abs(a.transform.position.y - b.transform.position.y) < 0.45f;
        }

        bool ClashWithOthers()
        {
            for (int i = 0; i < _activeCount; i++)
            {
                var other = Active[i];
                if (other == null || other == this || !other.gameObject.activeInHierarchy) continue;
                if (other.owner == owner) continue;
                if (!Overlaps(this, other)) continue;
                Release();
                other.Release();
                return true;
            }
            return false;
        }

        void Register()
        {
            for (int i = 0; i < _activeCount; i++)
            {
                if (Active[i] == this) return;
            }
            if (_activeCount < Active.Length) Active[_activeCount++] = this;
        }

        void Unregister()
        {
            for (int i = 0; i < _activeCount; i++)
            {
                if (Active[i] != this) continue;
                Active[i] = Active[_activeCount - 1];
                Active[_activeCount - 1] = null;
                _activeCount--;
                return;
            }
        }

        void Release()
        {
            if (pooled)
            {
                gameObject.SetActive(false);
                return;
            }
            Destroy(gameObject);
        }
    }
}
