using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Spawns a pooled projectile for caster and ranged 236 moves.
    /// Drives deterministic simulation projectile state into BBMatchSim while maintaining visual pooling.
    /// </summary>
    public static class BBProjectileCaster
    {
        public static bool ShouldSpawn(string characterId, string moveId)
        {
            if (string.IsNullOrEmpty(moveId)) return false;
            if (moveId.StartsWith("236"))
            {
                switch (characterId)
                {
                    case "C01":
                    case "C02":
                    case "C04":
                    case "C05":
                    case "C06":
                    case "C07":
                    case "C09":
                    case "C11":
                    case "C12":
                    case "C13":
                    case "SK05":
                    case "EX05":
                    case "EX07":
                        return true;
                    default:
                        return false;
                }
            }

            // Dedicated ranged fighters:
            // C05 Sorceress & C06 Mage: 5D signature drive
            if (characterId == "C05" || characterId == "C06")
            {
                if (moveId == "5D") return true;
            }
            // C09 Archer & C12 Crossbow: 5B, 5C, 5D projectile shots
            else if (characterId == "C09" || characterId == "C12")
            {
                if (moveId == "5B" || moveId == "5C" || moveId == "5D") return true;
            }

            return false;
        }

        public static void SpawnDeterministic(BBFighterBrain brain, string moveId)
        {
            if (brain == null || BBMatchSim.Instance == null) return;
            int ownerIdx = (BBMatchSim.Instance.p1Brain == brain) ? 0 : 1;
            int facing = brain.fighter != null ? brain.fighter.FacingDirection : (ownerIdx == 0 ? 1 : -1);
            Vector2 pos = new Vector2(brain.transform.position.x + (facing * 0.7f), brain.transform.position.y + 1.1f);
            Vector2 vel = new Vector2(facing * 8.5f, 0f);

            float damage = 700f;
            int hitstop = 8;
            int hitstun = 18;
            int blockstun = 12;
            BBGuard guard = BBGuard.Mid;

            if (brain.currentAttack != null)
            {
                if (brain.currentAttack.damage > 0f) damage = brain.currentAttack.damage;
                if (brain.currentAttack.hitstopAttacker > 0) hitstop = brain.currentAttack.hitstopAttacker;
                if (brain.currentAttack.hitstunTicks > 0) hitstun = brain.currentAttack.hitstunTicks;
                if (brain.currentAttack.blockstunTicks > 0) blockstun = brain.currentAttack.blockstunTicks;
                guard = brain.currentAttack.guard;
            }

            string charId = brain.fighter != null && brain.fighter.appliedDefinition != null
                ? brain.fighter.appliedDefinition.id
                : (brain.profile != null ? brain.profile.characterId : "Projectile");

            BBMatchSim.Instance.SpawnProjectile(ownerIdx, pos, vel, damage, 0.4f, 0.4f, guard, 120, charId + "_" + moveId, hitstop, hitstun, blockstun);
        }

        public static MDIProjectile Spawn(FighterController owner, string moveId)
        {
            if (owner == null) return null;
            var brain = owner.GetComponent<BBFighterBrain>();
            if (brain != null)
            {
                SpawnDeterministic(brain, moveId);
            }

            Vector3 pos = owner.transform.position + new Vector3(owner.FacingDirection * 0.7f, 1.1f, 0f);
            pos.z = 0f;
            string name = owner.appliedDefinition != null ? owner.appliedDefinition.displayName : owner.fighterName;
            return ProjectileFactory.SpawnPooled(name, owner, pos, owner.FacingDirection);
        }
    }
}
