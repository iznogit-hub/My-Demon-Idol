using System;
using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Default standing, crouching, and aerial hurtboxes and pushbox for a character.
    /// Authored from verified character measurements in spec section 6.4.
    /// </summary>
    [Serializable]
    public class BBBodyBoxes
    {
        [Header("Standing Boxes")]
        public BBBox standPushBox;
        public BBBox standThrowHurtBox;
        public BBBox[] standHurtBoxes = new BBBox[3]; // Head, Body, Foot

        [Header("Crouching Boxes")]
        public BBBox crouchPushBox;
        public BBBox crouchThrowHurtBox;
        public BBBox[] crouchHurtBoxes = new BBBox[3]; // Head, Body, Foot

        [Header("Airborne Boxes")]
        public BBBox airPushBox;
        public BBBox[] airHurtBoxes = new BBBox[2]; // Body, Legs (throw immune in air unless air throw)

        public static BBBodyBoxes CreateAkioPreset()
        {
            var b = new BBBodyBoxes();
            // Standing (Akio height ~1.75m, hip ~0.95m)
            b.standPushBox = new BBBox(0f, 0.90f, 0.35f, 0.90f, BBBoxKind.Push);
            b.standThrowHurtBox = new BBBox(0f, 0.95f, 0.30f, 0.70f, BBBoxKind.ThrowHurt);
            b.standHurtBoxes = new BBBox[]
            {
                new BBBox(0.02f, 1.55f, 0.22f, 0.18f, BBBoxKind.Hurt, 0, BBAttribute.Head), // Head
                new BBBox(0.00f, 1.05f, 0.28f, 0.35f, BBBoxKind.Hurt, 0, BBAttribute.Body), // Torso / Body
                new BBBox(0.05f, 0.40f, 0.32f, 0.40f, BBBoxKind.Hurt, 0, BBAttribute.Foot)  // Legs / Foot (+0.05m forward)
            };

            // Crouching (Akio crouch push h 0.55m, r 0.22m)
            b.crouchPushBox = new BBBox(0f, 0.55f, 0.40f, 0.55f, BBBoxKind.Push);
            b.crouchThrowHurtBox = new BBBox(0f, 0.50f, 0.35f, 0.45f, BBBoxKind.ThrowHurt);
            b.crouchHurtBoxes = new BBBox[]
            {
                new BBBox(0.04f, 0.95f, 0.20f, 0.12f, BBBoxKind.Hurt, 0, BBAttribute.Head), // Crouch Head (0.35 scale)
                new BBBox(0.02f, 0.65f, 0.30f, 0.24f, BBBoxKind.Hurt, 0, BBAttribute.Body), // Crouch Body (0.75 scale)
                new BBBox(0.08f, 0.22f, 0.35f, 0.22f, BBBoxKind.Hurt, 0, BBAttribute.Foot)  // Crouch Foot (0.55 scale)
            };

            // Air (Akio air push h 1.10m, r 0.22m)
            b.airPushBox = new BBBox(0f, 0.60f, 0.30f, 0.60f, BBBoxKind.Push);
            b.airHurtBoxes = new BBBox[]
            {
                new BBBox(0.00f, 0.75f, 0.25f, 0.40f, BBBoxKind.Hurt, 0, BBAttribute.Head),
                new BBBox(0.00f, 0.25f, 0.28f, 0.30f, BBBoxKind.Hurt, 0, BBAttribute.Body)
            };
            return b;
        }

        public static BBBodyBoxes CreateAikaPreset()
        {
            var b = new BBBodyBoxes();
            b.standPushBox = new BBBox(0f, 0.88f, 0.32f, 0.88f, BBBoxKind.Push);
            b.standThrowHurtBox = new BBBox(0f, 0.92f, 0.28f, 0.65f, BBBoxKind.ThrowHurt);
            b.standHurtBoxes = new BBBox[]
            {
                new BBBox(0.02f, 1.50f, 0.20f, 0.18f, BBBoxKind.Hurt, 0, BBAttribute.Head),
                new BBBox(0.00f, 1.00f, 0.26f, 0.34f, BBBoxKind.Hurt, 0, BBAttribute.Body),
                new BBBox(0.06f, 0.38f, 0.34f, 0.38f, BBBoxKind.Hurt, 0, BBAttribute.Foot)
            };
            b.crouchPushBox = new BBBox(0f, 0.50f, 0.36f, 0.50f, BBBoxKind.Push);
            b.crouchThrowHurtBox = new BBBox(0f, 0.48f, 0.32f, 0.42f, BBBoxKind.ThrowHurt);
            b.crouchHurtBoxes = new BBBox[]
            {
                new BBBox(0.03f, 0.90f, 0.18f, 0.12f, BBBoxKind.Hurt, 0, BBAttribute.Head),
                new BBBox(0.01f, 0.60f, 0.28f, 0.22f, BBBoxKind.Hurt, 0, BBAttribute.Body),
                new BBBox(0.08f, 0.20f, 0.36f, 0.20f, BBBoxKind.Hurt, 0, BBAttribute.Foot)
            };
            b.airPushBox = new BBBox(0f, 0.58f, 0.28f, 0.58f, BBBoxKind.Push);
            b.airHurtBoxes = new BBBox[]
            {
                new BBBox(0.00f, 0.70f, 0.24f, 0.38f, BBBoxKind.Hurt, 0, BBAttribute.Head),
                new BBBox(0.00f, 0.24f, 0.26f, 0.28f, BBBoxKind.Hurt, 0, BBAttribute.Body)
            };
            return b;
        }

        public static BBBodyBoxes CreateUnityChanPreset()
        {
            var b = new BBBodyBoxes();
            b.standPushBox = new BBBox(0f, 0.82f, 0.30f, 0.82f, BBBoxKind.Push);
            b.standThrowHurtBox = new BBBox(0f, 0.87f, 0.26f, 0.60f, BBBoxKind.ThrowHurt);
            b.standHurtBoxes = new BBBox[]
            {
                new BBBox(0.02f, 1.42f, 0.18f, 0.16f, BBBoxKind.Hurt, 0, BBAttribute.Head),
                new BBBox(0.00f, 0.95f, 0.24f, 0.32f, BBBoxKind.Hurt, 0, BBAttribute.Body),
                new BBBox(0.05f, 0.36f, 0.30f, 0.36f, BBBoxKind.Hurt, 0, BBAttribute.Foot)
            };
            b.crouchPushBox = new BBBox(0f, 0.46f, 0.34f, 0.46f, BBBoxKind.Push);
            b.crouchThrowHurtBox = new BBBox(0f, 0.44f, 0.30f, 0.40f, BBBoxKind.ThrowHurt);
            b.crouchHurtBoxes = new BBBox[]
            {
                new BBBox(0.02f, 0.85f, 0.17f, 0.11f, BBBoxKind.Hurt, 0, BBAttribute.Head),
                new BBBox(0.01f, 0.56f, 0.26f, 0.20f, BBBoxKind.Hurt, 0, BBAttribute.Body),
                new BBBox(0.07f, 0.18f, 0.32f, 0.18f, BBBoxKind.Hurt, 0, BBAttribute.Foot)
            };
            b.airPushBox = new BBBox(0f, 0.54f, 0.26f, 0.54f, BBBoxKind.Push);
            b.airHurtBoxes = new BBBox[]
            {
                new BBBox(0f, 0.66f, 0.22f, 0.34f, BBBoxKind.Hurt, 0, BBAttribute.Head),
                new BBBox(0f, 0.22f, 0.24f, 0.26f, BBBoxKind.Hurt, 0, BBAttribute.Body)
            };
            return b;
        }

        public static BBBodyBoxes CreatePicoChanPreset()
        {
            var b = new BBBodyBoxes();
            b.standPushBox = new BBBox(0f, 0.72f, 0.28f, 0.72f, BBBoxKind.Push);
            b.standThrowHurtBox = new BBBox(0f, 0.76f, 0.24f, 0.54f, BBBoxKind.ThrowHurt);
            b.standHurtBoxes = new BBBox[]
            {
                new BBBox(0.02f, 1.28f, 0.16f, 0.15f, BBBoxKind.Hurt, 0, BBAttribute.Head),
                new BBBox(0.00f, 0.82f, 0.22f, 0.30f, BBBoxKind.Hurt, 0, BBAttribute.Body),
                new BBBox(0.05f, 0.30f, 0.26f, 0.30f, BBBoxKind.Hurt, 0, BBAttribute.Foot)
            };
            b.crouchPushBox = new BBBox(0f, 0.42f, 0.30f, 0.42f, BBBoxKind.Push);
            b.crouchThrowHurtBox = new BBBox(0f, 0.40f, 0.26f, 0.36f, BBBoxKind.ThrowHurt);
            b.crouchHurtBoxes = new BBBox[]
            {
                new BBBox(0.02f, 0.75f, 0.15f, 0.10f, BBBoxKind.Hurt, 0, BBAttribute.Head),
                new BBBox(0.01f, 0.48f, 0.24f, 0.18f, BBBoxKind.Hurt, 0, BBAttribute.Body),
                new BBBox(0.06f, 0.16f, 0.28f, 0.16f, BBBoxKind.Hurt, 0, BBAttribute.Foot)
            };
            b.airPushBox = new BBBox(0f, 0.48f, 0.24f, 0.48f, BBBoxKind.Push);
            b.airHurtBoxes = new BBBox[]
            {
                new BBBox(0f, 0.58f, 0.20f, 0.30f, BBBoxKind.Hurt, 0, BBAttribute.Head),
                new BBBox(0f, 0.18f, 0.22f, 0.22f, BBBoxKind.Hurt, 0, BBAttribute.Body)
            };
            return b;
        }

        public static BBBodyBoxes CreateDefault() => CreateAkioPreset();
    }
}
