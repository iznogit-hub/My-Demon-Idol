
#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DemonIdol.Characters.Editor
{
    public static class MDI_PureHumanoidClipDeployer
    {
        private const string MDI_ANIM_DIR = "Assets/MDI_Animations";

        public struct ClipMapping
        {
            public string targetName;
            public string sourceAssetPath;
            public string sourceClipName;

            public ClipMapping(string target, string sourcePath, string clipName = null)
            {
                targetName = target;
                sourceAssetPath = sourcePath;
                sourceClipName = clipName;
            }
        }

        private static readonly ClipMapping[] Mappings = new ClipMapping[]
        {
            new ClipMapping("MDI_Anim_Hit.anim", "Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_DAMAGED00.fbx", "DAMAGED00"),
            new ClipMapping("MDI_Anim_Knockdown.anim", "Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_DAMAGED01.fbx", "DAMAGED01"),
            new ClipMapping("MDI_Anim_Lose.anim", "Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_LOSE00.fbx", "LOSE00"),
            new ClipMapping("MDI_Anim_Win.anim", "Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_WIN00.fbx", "WIN00"),
            new ClipMapping("MDI_Anim_Block.anim", "Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_WAIT00.fbx", "WAIT00"),
            new ClipMapping("MDI_Anim_Punch.anim", "Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_HANDUP00_R.fbx", "HANDUP00_R"),
            new ClipMapping("MDI_Anim_Kick.anim", "Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_SLIDE00.fbx", "SLIDE00"),
            new ClipMapping("MDI_Anim_Combo.anim", "Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_REFLESH00.fbx", "REFLESH00"),
            new ClipMapping("MDI_Anim_Special.anim", "Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_HANDUP00_R.fbx", "HANDUP00_R"),
            new ClipMapping("MDI_Anim_Super.anim", "Assets/unity-chan!/Unity-chan! Model/Art/Animations/unitychan_REFLESH00.fbx", "REFLESH00")
        };

        [MenuItem("Demon Idol/Animations/Deploy Pure Mecanim Humanoid Clips", priority = 10)]
        public static void DeployPureMecanimClips()
        {
            Debug.Log("<color=#00e676>[MDI_PureHumanoidClipDeployer] Deploying pure Mecanim humanoid combat clips...</color>");

            if (!Directory.Exists(MDI_ANIM_DIR)) Directory.CreateDirectory(MDI_ANIM_DIR);

            foreach (var mapping in Mappings)
            {
                string targetPath = $"{MDI_ANIM_DIR}/{mapping.targetName}";
                string metaPath = targetPath + ".meta";
                string existingMeta = File.Exists(metaPath) ? File.ReadAllText(metaPath) : null;

                // Load source clip from FBX
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(mapping.sourceAssetPath);
                AnimationClip sourceClip = null;
                foreach (var a in assets)
                {
                    if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    {
                        if (string.IsNullOrEmpty(mapping.sourceClipName) || clip.name == mapping.sourceClipName)
                        {
                            sourceClip = clip;
                            break;
                        }
                    }
                }

                if (sourceClip == null)
                {
                    Debug.LogWarning($"[MDI_PureHumanoidClipDeployer] Could not find clip in {mapping.sourceAssetPath}");
                    continue;
                }

                // Create new clean AnimationClip copied from source
                AnimationClip newClip = new AnimationClip();
                EditorUtility.CopySerialized(sourceClip, newClip);
                newClip.name = Path.GetFileNameWithoutExtension(mapping.targetName);

                AssetDatabase.CreateAsset(newClip, targetPath);

                // Restore original .meta file if it existed to preserve GUID
                if (!string.IsNullOrEmpty(existingMeta))
                {
                    File.WriteAllText(metaPath, existingMeta);
                }

                Debug.Log($"[MDI_PureHumanoidClipDeployer] Deployed pure Humanoid clip: {mapping.targetName} from {sourceClip.name}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#00e676><b>[MDI_PureHumanoidClipDeployer] SUCCESS: All combat clips are now 100% pure Humanoid Mecanim!</b></color>");
        }
    }
}
#endif
