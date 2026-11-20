using UnityEngine;
using UnityEngine.Video;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Empty art slots for one fighter. Izno drops portraits, videos, and quotes here.
    /// </summary>
    [CreateAssetMenu(fileName = "BBPresentation", menuName = "DemonIdol/BlazBlue/Character Presentation")]
    public class BBCharacterPresentation : ScriptableObject
    {
        public string characterId;
        public string displayName;
        public string driveName;
        public Color placeholderColor = Color.gray;
        public Sprite portrait;
        public VideoClip versusVideo;
        public VideoClip victoryVideo;
        public string quote;
        public AnimationClip driveClip;
        public AnimationClip specialClip;
        public AnimationClip superClip;
    }
}
