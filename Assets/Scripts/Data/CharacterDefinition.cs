using System;
using UnityEngine;
using DemonIdol.UI;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Content Contract: CharacterDefinition ScriptableObject.
    /// Defines a playable combatant: art assets, stats, archetype, 
    /// MoveSet reference, and VideoBindings reference.
    /// Authored completely without modifying code.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterDefinition", menuName = "DemonIdol/Combat/CharacterDefinition")]
    public class CharacterDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "Kai";
        public string displayName = "Kai";
        public string title = "The Crimson Shadow";
        public string archetype = "Rushdown / Mixup";
        public string signatureStageId = "ST01";
        [TextArea(2, 5)]
        public string description = "Swift ninja operative mastering lethal frame-traps, Gatling cancels, and Void Rift domain expansions.";

        [Header("Art & Visuals")]
        public GameObject modelPrefab;
        public RuntimeAnimatorController animatorController;
        public Avatar avatar;
        public Sprite portraitSprite;
        public Sprite bannerSprite;

        [Header("Tactical Stats (0 - 100)")]
        [Range(0f, 100f)] public float speed = 90f;
        [Range(0f, 100f)] public float power = 75f;
        [Range(0f, 100f)] public float reach = 65f;
        [Range(0f, 100f)] public float defense = 70f;

        [Header("Domain Expansion Astral Finish")]
        public string domainName = "Void Rift";
        [TextArea(2, 4)]
        public string domainDescription = "Freezes spacetime for 20 combat ticks, shattering enemy barrier integrity with an unavoidable fatal strike.";

        [Header("Combat MoveSet & Video Bindings")]
        public MoveSet moveSet;
        public VideoBindings videoBindings;

        [Header("Legacy Mapping")]
        public string ufeCharacterName = "Mike";

        [Header("Akio depth movelist")]
        [Tooltip("Opt-in. Only C01 Akio sets this. Every other fighter leaves it off and keeps the shared routes.")]
        public bool akioDepthMovelist = false;
        public AkioClipLibrary akioClips;

        public string GetEffectiveUFEName()
        {
            // Id mapping wins over the class default "Mike"
            string fromId = id switch
            {
                "C01" or "Kai" => "Mike",
                "C02" or "Reika" => "Ellen",
                "C03" or "Arin" => "Robot_Kyle",
                "C04" or "Mira" => "Suzan",
                "C05" or "Zev" => "Ethan",
                "C06" or "Nox" => "Mecanim_Bot",
                _ => null
            };
            if (!string.IsNullOrEmpty(fromId)) return fromId;

            if (!string.IsNullOrEmpty(ufeCharacterName)) return ufeCharacterName;
            return "Mike";
        }

        /// <summary>
        /// Converts to a lightweight runtime FighterProfile for UI compatibility.
        /// </summary>
        public FighterProfile ToFighterProfile()
        {
            return new FighterProfile
            {
                id = id,
                displayName = displayName,
                title = title,
                archetype = archetype,
                description = description,
                prefabPath = modelPrefab != null ? modelPrefab.name : $"Prefabs/{id}",
                speed = Mathf.RoundToInt(speed),
                power = Mathf.RoundToInt(power),
                reach = Mathf.RoundToInt(reach),
                defense = Mathf.RoundToInt(defense),
                domainName = domainName,
                domainDescription = domainDescription,
                themeColor = SumiStyleTokens.PaperWhite
            };
        }
    }
}
