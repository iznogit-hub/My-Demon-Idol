#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DemonIdol.BlazBlue.Editor
{
    [InitializeOnLoad]
    public static class BBClipInventoryTool
    {
        public const string TRIGGER_FILE = "clip_inventory.trigger";
        public const string OUTPUT_MD = "Docs/CLIP_INVENTORY.md";

        static BBClipInventoryTool()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (File.Exists(TRIGGER_FILE))
            {
                try { File.Delete(TRIGGER_FILE); } catch { }
                GenerateInventory();
            }
        }

        public struct ClipInfo
        {
            public string packName;
            public string relativePath;
            public string clipName;
            public int lengthFrames;
            public int hitFrame;
            public string moveType;
        }

        public static void GenerateInventory()
        {
            Debug.Log("<color=#00e676>[BBClipInventoryTool] Scanning all approved animation packs...</color>");

            var packs = new (string name, string folder)[]
            {
                ("Warrior Bundle 1", "Assets/ExplosiveLLC/Warrior Pack Bundle 1 FREE"),
                ("Warrior Bundle 2", "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE"),
                ("Warrior Bundle 3", "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE"),
                ("Fighter Pack Bundle", "Assets/ExplosiveLLC/Fighter Pack Bundle FREE"),
                ("Dwarf Animations", "Assets/Kevin Iglesias/Dwarf Animations"),
                ("Martial Art Animations", "Assets/Martial Art Animations Sample"),
                ("Human Spell Casting Animations", "Assets/Kevin Iglesias/Human Animations/Animations"),
                ("Human Melee Animations", "Assets/Kevin Iglesias/Human Animations/Animations"),
                ("GAMWILL Bionic Gorilla", "Assets/GAMWILL Character Pack Monster  Bionic Cartoon Zombie Gorilla")
            };

            var allClips = new List<ClipInfo>();
            var seenClips = new HashSet<string>();

            foreach (var (packName, folder) in packs)
            {
                if (!Directory.Exists(folder)) continue;

                string[] guids = AssetDatabase.FindAssets("t:Model t:AnimationClip", new[] { folder });
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path)) continue;

                    // Filter for Human Spell Casting vs Human Melee
                    if (packName == "Human Spell Casting Animations" && !path.Contains("Spellcasting"))
                    {
                        continue;
                    }
                    if (packName == "Human Melee Animations" && (path.Contains("Spellcasting") || !path.Contains("Combat")))
                    {
                        continue;
                    }

                    var objects = AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (var obj in objects)
                    {
                        if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                        {
                            string uniqueKey = $"{packName}:{clip.name}:{clip.length}";
                            if (seenClips.Contains(uniqueKey)) continue;
                            seenClips.Add(uniqueKey);

                            int frames = Mathf.Max(1, Mathf.RoundToInt(clip.length * 60f));
                            string type = ClassifyType(clip.name, path);
                            int hitFrame = SampleHitFrame(clip, frames, type);

                            allClips.Add(new ClipInfo
                            {
                                packName = packName,
                                relativePath = path,
                                clipName = clip.name,
                                lengthFrames = frames,
                                hitFrame = hitFrame,
                                moveType = type
                            });
                        }
                    }
                }
            }

            WriteInventoryMarkdown(allClips);
            Debug.Log($"<color=#00e676>[BBClipInventoryTool] Completed inventory: {allClips.Count} clips inventoried to {OUTPUT_MD}</color>");
        }

        private static string ClassifyType(string name, string path)
        {
            string n = name.ToLowerInvariant();
            string p = path.ToLowerInvariant();

            if (n.Contains("hit") || n.Contains("damage") || n.Contains("stunned")) return "hit-react";
            if (n.Contains("guard") || n.Contains("block") || n.Contains("shieldblock")) return "block";
            if (n.Contains("knock") || n.Contains("fall")) return "knockdown";
            if (n.Contains("getup") || n.Contains("standup")) return "getup";
            if (n.Contains("death") || n.Contains("die")) return "death";
            if (n.Contains("taunt") || n.Contains("win") || n.Contains("victory") || n.Contains("talk")) return "victory";
            if (n.Contains("grab") || n.Contains("clinch")) return "grab";
            if (n.Contains("magic") || n.Contains("cast") || n.Contains("spell")) return "magic";
            if (n.Contains("jump") || n.Contains("air") || n.Contains("aerial")) return "air";
            if (n.Contains("idle")) return "idle";
            if (n.Contains("walk") || n.Contains("run") || n.Contains("step") || n.Contains("dodge") || n.Contains("dash")) return "walk";
            if (n.Contains("launcher") || n.Contains("up") || n.Contains("rising")) return "launcher";
            if (n.Contains("low") || n.Contains("sweep")) return "low";
            if (n.Contains("overhead") || n.Contains("slam")) return "overhead";

            // Attacks
            if (n.Contains("atk_p_1") || n.Contains("attack1h01_r") || n.Contains("light") || n.Contains("jab")) return "light";
            if (n.Contains("atk_p_2") || n.Contains("atk_k_1") || n.Contains("attack1h01_l") || n.Contains("medium")) return "medium";
            if (n.Contains("atk_p_3") || n.Contains("atk_k_2") || n.Contains("atk_k_3") || n.Contains("attack2h") || n.Contains("heavy") || n.Contains("cleave")) return "heavy";
            if (n.Contains("attack1") || n.Contains("attack") || n.Contains("punch")) return "medium";

            return "medium";
        }

        private static int SampleHitFrame(AnimationClip clip, int totalFrames, string moveType)
        {
            if (moveType == "hit-react" || moveType == "block") return 1;
            if (moveType == "idle" || moveType == "walk" || moveType == "victory" || moveType == "death") return 0;
            if (moveType == "knockdown" || moveType == "getup") return Mathf.Max(1, totalFrames / 4);

            var bindings = AnimationUtility.GetCurveBindings(clip);
            float maxExtension = -1f;
            int bestFrame = -1;

            if (bindings != null && bindings.Length > 0)
            {
                // Look for limb / hand / foot bindings
                var targetBindings = new List<EditorCurveBinding>();
                foreach (var b in bindings)
                {
                    string prop = b.propertyName.ToLowerInvariant();
                    string path = b.path.ToLowerInvariant();
                    if (path.Contains("hand") || path.Contains("foot") || path.Contains("wrist") || path.Contains("toe") || path.Contains("weapon") || prop.Contains("reach") || prop.Contains("position"))
                    {
                        targetBindings.Add(b);
                    }
                }

                if (targetBindings.Count > 0)
                {
                    // Sample every frame from frame 2 to totalFrames - 2
                    for (int f = 2; f < totalFrames - 2; f++)
                    {
                        float time = f / 60f;
                        float sum = 0f;
                        foreach (var b in targetBindings)
                        {
                            var curve = AnimationUtility.GetEditorCurve(clip, b);
                            if (curve != null)
                            {
                                float val = Mathf.Abs(curve.Evaluate(time));
                                sum += val;
                            }
                        }

                        if (sum > maxExtension)
                        {
                            maxExtension = sum;
                            bestFrame = f;
                        }
                    }
                }
            }

            if (bestFrame > 0)
            {
                return bestFrame;
            }

            // Fallback estimation by archetype ratio
            switch (moveType)
            {
                case "light":
                    return Mathf.Clamp(Mathf.RoundToInt(totalFrames * 0.22f), 4, totalFrames - 2);
                case "medium":
                    return Mathf.Clamp(Mathf.RoundToInt(totalFrames * 0.32f), 6, totalFrames - 2);
                case "heavy":
                case "launcher":
                case "overhead":
                    return Mathf.Clamp(Mathf.RoundToInt(totalFrames * 0.42f), 8, totalFrames - 2);
                case "magic":
                    return Mathf.Clamp(Mathf.RoundToInt(totalFrames * 0.38f), 7, totalFrames - 2);
                case "grab":
                    return Mathf.Clamp(Mathf.RoundToInt(totalFrames * 0.25f), 5, totalFrames - 2);
                default:
                    return Mathf.Clamp(Mathf.RoundToInt(totalFrames * 0.33f), 5, totalFrames - 2);
            }
        }

        private static void WriteInventoryMarkdown(List<ClipInfo> clips)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Usable Animation Clips Technical Inventory");
            sb.AppendLine();
            sb.AppendLine("**Project:** *My Demon Idol* (BlazBlue Combat System)  ");
            sb.AppendLine($"**Specification:** Step 2 Clip Inventory — Total Inventoried Clips: {clips.Count}  ");
            sb.AppendLine($"**Generated:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC  ");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## 1. Summary by Approved Animation Pack");
            sb.AppendLine();
            sb.AppendLine("| # | Pack Name | Clip Count | Attack Clips | Locomotion / Stance | Hit / KD / Reactions |");
            sb.AppendLine("|:---:|---|:---:|:---:|:---:|:---:|");

            var packGroups = new Dictionary<string, List<ClipInfo>>();
            foreach (var c in clips)
            {
                if (!packGroups.ContainsKey(c.packName))
                    packGroups[c.packName] = new List<ClipInfo>();
                packGroups[c.packName].Add(c);
            }

            int pIdx = 1;
            foreach (var kvp in packGroups)
            {
                int total = kvp.Value.Count;
                int atk = kvp.Value.FindAll(x => x.moveType == "light" || x.moveType == "medium" || x.moveType == "heavy" || x.moveType == "launcher" || x.moveType == "low" || x.moveType == "overhead" || x.moveType == "magic" || x.moveType == "grab").Count;
                int loco = kvp.Value.FindAll(x => x.moveType == "idle" || x.moveType == "walk" || x.moveType == "air").Count;
                int react = kvp.Value.FindAll(x => x.moveType == "hit-react" || x.moveType == "block" || x.moveType == "knockdown" || x.moveType == "getup" || x.moveType == "death" || x.moveType == "victory").Count;
                sb.AppendLine($"| {pIdx++} | **{kvp.Key}** | {total} | {atk} | {loco} | {react} |");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## 2. Exhaustive Usable Clip Roster");
            sb.AppendLine();
            sb.AppendLine("| Pack Name | Relative Asset Path | Clip Name | Frames @60 | Hit Frame | Move Type |");
            sb.AppendLine("|---|---|---|:---:|:---:|:---:|");

            foreach (var c in clips)
            {
                sb.AppendLine($"| {c.packName} | `{c.relativePath}` | **{c.clipName}** | {c.lengthFrames} | {c.hitFrame} | `{c.moveType}` |");
            }

            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), OUTPUT_MD);
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(fullPath, sb.ToString());
        }
    }
}
#endif