using UnityEngine;

namespace DemonIdol.BlazBlue
{
    [CreateAssetMenu(menuName = "DemonIdol/BlazBlue/Character Profile")]
    public class BBCharacterProfile : ScriptableObject
    {
        public string characterId = "C01";
        public BBMoveSet moveSet;
        public int maxHp = 11000;
        public BBDashType dashType = BBDashType.Run;
        public int airActions = 1;
        public float walkF = 3.6f;
        public float walkB = 2.8f;
        public float jumpV = 8.0f;
        public float jumpH = 3.2f;
        public float gravity = 20f;
        public RuntimeAnimatorController bbAnimator;
        public string driveName = "Drive";
        public DriveResourceKind driveResource = DriveResourceKind.None;
        public int driveResourceMax = 0;
        public float bodyWidth = 0.5f;
        public float bodyHeight = 1.6f;
        public float bodyCenterY = 0.8f;
    }
}
