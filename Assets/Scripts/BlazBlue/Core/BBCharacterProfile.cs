using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Character profile defining a fighter's BlazBlue identity, stats,
    /// specific animation override controller, and move set reference.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBBCharacterProfile", menuName = "DemonIdol/BlazBlue/CharacterProfile")]
    public class BBCharacterProfile : ScriptableObject
    {
        [Header("Character Identity")]
        public string characterId = "C01_Akio";
        public string characterName = "Akio";
        public string driveName = "Iron Rush";

        [Header("Locomotion Tuning")]
        public float forwardWalkSpeed = 3.6f;
        public float backwardWalkSpeed = 2.8f;
        public float forwardDashSpeed = 6.5f;
        public int forwardDashDurationTicks = 12;
        public float backdashSpeed = 7.0f;
        public int backdashDurationTicks = 14;
        public int backdashInvulnTicks = 5;
        public float jumpVerticalLaunch = 8.0f;
        public float jumpHorizontalSpeed = 3.2f;
        public float gravity = 20.0f;

        [Header("AABB Pushbox Dimensions (Meters)")]
        public Vector2 standPushboxSize = new Vector2(0.70f, 1.80f);
        public Vector2 crouchPushboxSize = new Vector2(0.80f, 1.30f);
        public Vector2 airPushboxSize = new Vector2(0.60f, 1.40f);

        [Header("Animation Override Asset (Non-Destructive)")]
        public AnimatorOverrideController blazBlueOverrideController;

        [Header("MoveSet Configuration")]
        public ScriptableObject moveSet;

        [Header("Stats & Defense")]
        public float maxHealth = 1000f;
        public float defenseModifier = 1.0f;
    }
}
