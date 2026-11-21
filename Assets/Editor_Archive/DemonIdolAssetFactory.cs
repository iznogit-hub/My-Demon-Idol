#if MDI_ARCHIVE
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using DemonIdol.Combat;
using DemonIdol.Characters.Editor;

namespace DemonIdol.Editor
{
    public static class DemonIdolAssetFactory
    {
        private const string FOLDER_PATH = "Assets/CombatData";

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Rebind All Character Assets & Prefabs")]
        public static void RebindAllCharactersMenu()
        {
            MDI_FighterAssembler.AssembleAllFighters();
        }

        // Consolidated into DemonIdolMasterMenu: [MenuItem("Demon Idol/Create Default Attack Data Assets")]
        public static void CreateDefaultAttackData()
        {
            if (!AssetDatabase.IsValidFolder(FOLDER_PATH))
            {
                AssetDatabase.CreateFolder("Assets", "CombatData");
            }

            // 1. Light Attack (i10 High Poke - Safe check)
            CreateOrUpdateAttack("Kai_LightAttack", data =>
            {
                data.attackName = "Twin Kinetic Jab";
                data.attackType = AttackType.Light;
                data.hitLevel = HitLevel.High;
                data.crush = CrushProperty.None;
                data.startupFrames = 9;  // i10
                data.activeFrames = 3;
                data.recoveryFrames = 11;
                data.hitstopAttacker = 5;
                data.hitstopDefender = 8;
                data.advantageOnBlock = +1;
                data.advantageOnHit = +5;
                data.advantageOnCounterHit = +8;
                data.damage = 35f;
                data.initialProration = 0.75f;
                data.comboDamageDecay = 0.92f;
                data.pushbackOnHit = 0.8f;
                data.pushbackOnBlock = 0.5f;
                data.animationTrigger = "LightAttack";
                data.canRapidCancel = true;
                data.gatlingCancels = new[] { AttackType.Medium, AttackType.Heavy, AttackType.DriveSkill };
            });

            // 1b. Low Attack (i10 Low Shin Kick - Hits standing auto-guard; must crouch block)
            CreateOrUpdateAttack("Kai_LowAttack", data =>
            {
                data.attackName = "Crouch Shin Kick";
                data.attackType = AttackType.Light;
                data.hitLevel = HitLevel.Low; // Hits standing! Must crouch-block!
                data.crush = CrushProperty.None;
                data.startupFrames = 10;
                data.activeFrames = 3;
                data.recoveryFrames = 14;
                data.hitstopAttacker = 5;
                data.hitstopDefender = 8;
                data.advantageOnBlock = -6; // Minus on block
                data.advantageOnHit = +3;
                data.advantageOnCounterHit = +6;
                data.damage = 40f;
                data.chipDamageOnBlock = 0f;
                data.initialProration = 0.70f;
                data.pushbackOnHit = 0.9f;
                data.pushbackOnBlock = 0.6f;
                data.animationTrigger = "HitLow";
                data.canRapidCancel = true;
                data.gatlingCancels = new[] { AttackType.Medium, AttackType.Heavy, AttackType.DriveSkill };
            });

            // 2. Medium Attack (i14 Mid Check - Hits crouching)
            CreateOrUpdateAttack("Kai_MediumAttack", data =>
            {
                data.attackName = "Arc Blade Thrust";
                data.attackType = AttackType.Medium;
                data.hitLevel = HitLevel.Mid;
                data.crush = CrushProperty.None;
                data.startupFrames = 13; // i14
                data.activeFrames = 4;
                data.recoveryFrames = 16;
                data.hitstopAttacker = 7;
                data.hitstopDefender = 10;
                data.advantageOnBlock = -4;
                data.advantageOnHit = +6;
                data.advantageOnCounterHit = +10;
                data.damage = 75f;
                data.initialProration = 0.90f;
                data.comboDamageDecay = 0.92f;
                data.pushbackOnHit = 1.3f;
                data.pushbackOnBlock = 0.9f;
                data.animationTrigger = "MediumAttack";
                data.canRapidCancel = true;
                data.gatlingCancels = new[] { AttackType.Heavy, AttackType.DriveSkill };
            });

            // 3. Heavy Attack (i19 Overhead Void Cleave - Launcher)
            CreateOrUpdateAttack("Kai_HeavyAttack", data =>
            {
                data.attackName = "Overhead Void Cleave";
                data.attackType = AttackType.Heavy;
                data.hitLevel = HitLevel.Overhead;
                data.crush = CrushProperty.None;
                data.startupFrames = 18;
                data.activeFrames = 5;
                data.recoveryFrames = 24;
                data.hitstopAttacker = 10;
                data.hitstopDefender = 14;
                data.counterHitStopBonus = 8;
                data.advantageOnBlock = -14;
                data.advantageOnHit = +18;
                data.advantageOnCounterHit = +24;
                data.damage = 140f;
                data.initialProration = 1.0f;
                data.isLauncher = true;
                data.causesWallSplat = true;
                data.launchVelocity = new Vector2(2.2f, 7.0f); // Pop-up launcher
                data.screenShakeTrauma = 0.35f;
                data.animationTrigger = "HeavyAttack";
                data.canRapidCancel = true;
                data.gatlingCancels = new[] { AttackType.DriveSkill };
            });

            // 4. Drive Skill (Arcana 0 Bound Extender - 1 per combo)
            CreateOrUpdateAttack("Kai_DriveSkill", data =>
            {
                data.attackName = "Arcana 0 Card Release";
                data.attackType = AttackType.DriveSkill;
                data.hitLevel = HitLevel.Mid;
                data.crush = CrushProperty.None;
                data.startupFrames = 14;
                data.activeFrames = 6;
                data.recoveryFrames = 18;
                data.hitstopAttacker = 8;
                data.hitstopDefender = 12;
                data.advantageOnBlock = -6;
                data.advantageOnHit = +12;
                data.advantageOnCounterHit = +16;
                data.damage = 110f;
                data.initialProration = 0.85f;
                data.isBoundTornado = true; // Bound juggle extender!
                data.pushbackOnHit = 2.2f;
                data.screenShakeTrauma = 0.25f;
                data.animationTrigger = "ThrowCard";
                data.canRapidCancel = true;
                data.gatlingCancels = new AttackType[0];
            });

            // 5. Jump Attack (Low Crush Overhead)
            CreateOrUpdateAttack("Kai_JumpAttack", data =>
            {
                data.attackName = "Hopkick Shadow Strike";
                data.attackType = AttackType.JumpAttack;
                data.hitLevel = HitLevel.Mid;
                data.crush = CrushProperty.LowCrush; // Invincible to Lows
                data.crushStartTick = 5;
                data.crushEndTick = 20;
                data.startupFrames = 12;
                data.activeFrames = 6;
                data.recoveryFrames = 14;
                data.hitstopAttacker = 8;
                data.hitstopDefender = 12;
                data.advantageOnBlock = -8;
                data.advantageOnHit = +10;
                data.damage = 95f;
                data.initialProration = 0.80f;
                data.pushbackOnHit = 1.6f;
                data.screenShakeTrauma = 0.2f;
                data.animationTrigger = "JumpAttack";
                data.canRapidCancel = true;
                data.gatlingCancels = new AttackType[0];
            });

            // 6. Domain Expansion (100% Super Overdrive)
            CreateOrUpdateAttack("Kai_DomainSuper", data =>
            {
                data.attackName = "Domain Expansion: Void Calligraphy";
                data.attackType = AttackType.DomainSuper;
                data.hitLevel = HitLevel.Unblockable;
                data.startupFrames = 15;
                data.activeFrames = 12;
                data.recoveryFrames = 30;
                data.hitstopAttacker = 18;
                data.hitstopDefender = 24;
                data.damage = 380f;
                data.initialProration = 1.0f;
                data.causesKnockdown = true;
                data.screenShakeTrauma = 0.6f;
                data.animationTrigger = "DomainSuper";
                data.canRapidCancel = false;
                data.gatlingCancels = new AttackType[0];
            });

            // 7. Tactical Throw (i5 Kinetic Throw - Range 1.1m, Tech window 6 ticks)
            CreateOrUpdateAttack("Kai_Throw", data =>
            {
                data.attackName = "Kinetic Throw";
                data.attackType = AttackType.Throw;
                data.hitLevel = HitLevel.Throw;
                data.crush = CrushProperty.None;
                data.startupFrames = 5;
                data.activeFrames = 2;
                data.recoveryFrames = 22;
                data.hitstopAttacker = 8;
                data.hitstopDefender = 8;
                data.counterHitStopBonus = 0;
                data.advantageOnBlock = 0;
                data.advantageOnHit = 0;
                data.advantageOnCounterHit = 0;
                data.damage = 120f;
                data.chipDamageOnBlock = 0f;
                data.initialProration = 1f;
                data.pushbackOnHit = 1.5f;
                data.pushbackOnBlock = 0.5f;
                data.causesKnockdown = true;
                data.launchVelocity = new Vector2(1.5f, 3.5f);
                data.screenShakeTrauma = 0.25f;
                data.animationTrigger = "Knockdown";
                data.canRapidCancel = false;
                data.gatlingCancels = new AttackType[0];
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#C73E2E><b>[Demon Idol]</b></color> Tekken/BlazBlue Attack Assets Created with Bound, Proration & Domain Super.");
        }

        private static void CreateOrUpdateAttack(string assetName, System.Action<AttackData> configure)
        {
            string path = $"{FOLDER_PATH}/{assetName}.asset";
            AttackData asset = AssetDatabase.LoadAssetAtPath<AttackData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<AttackData>();
                configure(asset);
                AssetDatabase.CreateAsset(asset, path);
            }
            else
            {
                configure(asset);
                EditorUtility.SetDirty(asset);
            }
        }
    }
}
#endif

#endif
