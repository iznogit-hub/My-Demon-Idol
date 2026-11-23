using UnityEngine;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Handles pushback physics and corner wall reflection.
    /// If defender is pinned against the stage edge, pushes the attacker backward instead.
    /// </summary>
    [System.Obsolete("Superseded by BBMatchSim.ApplyPushback and internal bounds. Do not use in new code.", false)]
    public static class PushbackSolver
    {
        public const float ARENA_LIMIT_X = 10.5f; // Half-width of 2.5D arena strictly bounded inside single 32m backdrop
        public const float WALL_COLLISION_THRESHOLD = 9.8f; // Wall bounce / splat rebound trigger boundary

        public static float ActiveArenaLimitX { get; private set; } = ARENA_LIMIT_X;
        public static float ActiveWallThreshold { get; private set; } = WALL_COLLISION_THRESHOLD;

        public static void SetArenaBounds(float limitX, float wallThreshold)
        {
            ActiveArenaLimitX = Mathf.Max(5.0f, limitX);
            ActiveWallThreshold = Mathf.Clamp(wallThreshold, 4.0f, ActiveArenaLimitX);
        }

        public static void ResetArenaBounds()
        {
            ActiveArenaLimitX = ARENA_LIMIT_X;
            ActiveWallThreshold = WALL_COLLISION_THRESHOLD;
        }

        public static void ApplyPushback(
            FighterController attacker, 
            FighterController defender, 
            float pushbackAmount, 
            int attackerFacingDir,
            float multiplier = 1.0f)
        {
            if (attacker == null || defender == null) return;

            float effectivePushback = pushbackAmount * multiplier;
            float defenderPosX = defender.transform.position.x;
            bool isDefenderAtWall = Mathf.Abs(defenderPosX) >= (ActiveArenaLimitX - 0.75f);

            if (isDefenderAtWall)
            {
                // Defender is cornered! Push attacker backward instead
                attacker.ApplyHorizontalDisplacement(-attackerFacingDir * effectivePushback);
            }
            else
            {
                // Defender takes standard pushback
                defender.ApplyHorizontalDisplacement(attackerFacingDir * effectivePushback);
            }
        }

        public static bool IsWallCollision(Vector3 position)
        {
            return Mathf.Abs(position.x) >= ActiveWallThreshold;
        }

        public static bool IsWallCollision(Vector3 position, float velX, out int wallDirection)
        {
            return CheckWallImpact(position, velX, out wallDirection);
        }

        public static bool CheckWallCollision(Vector3 position, out int wallDirection)
        {
            if (position.x >= ActiveWallThreshold)
            {
                wallDirection = 1; // Right wall
                return true;
            }
            if (position.x <= -ActiveWallThreshold)
            {
                wallDirection = -1; // Left wall
                return true;
            }

            wallDirection = 0;
            return false;
        }

        public static bool CheckWallImpact(Vector3 position, float velX, out int wallDirection)
        {
            if (position.x >= ActiveWallThreshold && velX >= 0)
            {
                wallDirection = 1;
                return true;
            }
            if (position.x <= -ActiveWallThreshold && velX <= 0)
            {
                wallDirection = -1;
                return true;
            }

            wallDirection = 0;
            return false;
        }

        public static float ClampToArena(float xPos)
        {
            return Mathf.Clamp(xPos, -ActiveArenaLimitX, ActiveArenaLimitX);
        }
    }
}
