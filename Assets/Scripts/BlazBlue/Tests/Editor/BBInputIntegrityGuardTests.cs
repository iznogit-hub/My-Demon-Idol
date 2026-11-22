#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace DemonIdol.BlazBlue.Tests
{
    public class BBInputIntegrityGuardTests
    {
        private static readonly Regex DisallowedPatterns = new Regex(
            @"\.TryStartMoveId\s*\(|\.InjectFrame\s*\(|\.bbState\s*=(?!=)|virtual\w*Mask\s*=(?!=)|\.health\s*=(?!=)|hitstopRemaining\s*=(?!=)|TakeDamage\s*\(|UpdateHealth\s*\(|OnCleanHit\s*\(|ShowRoundEnded\s*\(|StartAttack\s*\(",
            RegexOptions.Compiled
        );

        [Test]
        public void NoDirectFighterStateOrBypassesInEditorOrTests()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string[] searchDirs = new string[]
            {
                Path.Combine(Application.dataPath, "Scripts", "Editor"),
                Path.Combine(Application.dataPath, "Scripts", "BlazBlue", "Tests"),
                Path.Combine(Application.dataPath, "Scripts", "BlazBlue", "Editor"),
                Path.Combine(Application.dataPath, "Tests")
            };

            int filesChecked = 0;
            int violations = 0;
            string violationReport = "";

            foreach (var dir in searchDirs)
            {
                if (!Directory.Exists(dir)) continue;

                var csFiles = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
                foreach (var file in csFiles)
                {
                    // Exclude this guard test itself
                    if (Path.GetFileName(file) == "BBInputIntegrityGuardTests.cs") continue;

                    filesChecked++;
                    var lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        // Skip single-line comments
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("//") || trimmed.StartsWith("/*")) continue;

                        var match = DisallowedPatterns.Match(line);
                        if (match.Success)
                        {
                            violations++;
                            string relativePath = file.Replace(projectRoot, "").TrimStart(Path.DirectorySeparatorChar, '/');
                            violationReport += $"\n[VIOLATION] {relativePath}:{i + 1} -> {match.Value} in: \"{trimmed}\"";
                        }
                    }
                }
            }

            if (violations > 0)
            {
                Assert.Fail($"Input integrity guard failed! Found {violations} bypass calls across {filesChecked} files. Verifiers and tools must press keys and never call TryStartMoveId, InjectFrame, or set bbState directly:{violationReport}");
            }

            Debug.Log($"<color=#00e676>[BBInputIntegrityGuardTests] PASS: {filesChecked} Editor/Test files scanned, 0 state bypass violations found.</color>");
        }

        public static void RunGuardTests()
        {
            var test = new BBInputIntegrityGuardTests();
            test.NoDirectFighterStateOrBypassesInEditorOrTests();
        }
    }
}
#endif
