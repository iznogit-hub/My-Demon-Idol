using UnityEngine;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Drive used by fighters that do not have a hand-authored Drive script.
    /// D stays on this path: 5D, 2D, 6D, and j.D. A/B/C are started by the brain.
    /// </summary>
    public class BBArchetypeDrive : MonoBehaviour, IBBDrive
    {
        public string DriveName => _driveName;
        public DriveResourceKind ResourceKind => DriveResourceKind.Gauge;
        public int CurrentResource => Mathf.RoundToInt(_gauge);
        public int MaxResource => 100;
        public bool InOverdrive { get; set; }

        string _driveName = "Drive";
        float _gauge;

        public void OnMatchStart(BBFighterBrain brain)
        {
            var info = BBRosterArchetype.Get(brain != null && brain.fighter != null && brain.fighter.appliedDefinition != null
                ? brain.fighter.appliedDefinition.id
                : null);
            _driveName = string.IsNullOrEmpty(info.driveName) ? "Drive" : info.driveName;
            if (brain != null && brain.profile != null && !string.IsNullOrEmpty(brain.profile.driveName))
            {
                _driveName = brain.profile.driveName;
            }
            ResetResource();
        }

        public bool TryStartDrive(BBFighterBrain brain, sbyte facingNumpad, int tick)
        {
            if (brain == null) return false;
            string moveId;
            if (!brain.isGrounded) moveId = "j.D";
            else if (facingNumpad == 1 || facingNumpad == 2 || facingNumpad == 3) moveId = "2D";
            else if (facingNumpad == 6 || facingNumpad == 9) moveId = "6D";
            else if (facingNumpad == 4) moveId = "4D";
            else moveId = "5D";

            var move = brain.FindAttackByMoveId(moveId) ?? (!brain.isGrounded ? (brain.FindAttackByMoveId("j.B") ?? brain.FindAttackByMoveId("5D")) : brain.FindAttackByMoveId("5D"));
            if (move != null && brain.CanExecuteMove(move))
            {
                brain.StartAttack(move);
                _gauge = Mathf.Min(100f, _gauge + 8f);
                return true;
            }
            return false;
        }

        public void TickDrive(BBFighterBrain brain, int tick) { }

        public void OnDriveHit(BBFighterBrain brain, BBAttackData move, bool isBlocked)
        {
            _gauge = Mathf.Min(100f, _gauge + (isBlocked ? 4f : 10f));
        }

        public void OnKnockdown(BBFighterBrain brain)
        {
            _gauge = Mathf.Max(0f, _gauge - 25f);
        }

        public void ResetResource()
        {
            _gauge = 0f;
        }
    }
}
