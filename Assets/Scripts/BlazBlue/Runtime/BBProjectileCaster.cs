using UnityEngine;
using DemonIdol.Combat;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Spawns a pooled projectile for caster and ranged 236 moves.
    /// </summary>
    public static class BBProjectileCaster
    {
        public static bool ShouldSpawn(string characterId, string moveId)
        {
            if (string.IsNullOrEmpty(moveId) || !moveId.StartsWith("236")) return false;
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

        public static MDIProjectile Spawn(FighterController owner, string moveId)
        {
            if (owner == null) return null;
            Vector3 pos = owner.transform.position + new Vector3(owner.FacingDirection * 0.7f, 1.1f, 0f);
            pos.z = 0f;
            string name = owner.appliedDefinition != null ? owner.appliedDefinition.displayName : owner.fighterName;
            return ProjectileFactory.SpawnPooled(name, owner, pos, owner.FacingDirection);
        }
    }
}
