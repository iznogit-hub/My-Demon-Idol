#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using DemonIdol.Network;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Release Build Preprocessor Guard.
    /// Runs before every build and strictly blocks non-development release builds if:
    /// 1. MDI_ALLOW_MOCK_AUTH is defined in Scripting Define Symbols.
    /// 2. Any hardcoded Photon App ID or GUID secret is found in tracked assets or scripts.
    /// </summary>
    public class DemonIdolReleaseBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            bool isDevelopment = (report.summary.options & BuildOptions.Development) != 0;

            // In development builds, local mock testing is permitted.
            if (isDevelopment)
            {
                Debug.Log("<color=#00e676>[ReleaseBuildGuard] Development build detected — dev mock auth permitted.</color>");
                return;
            }

            Debug.Log("<color=#D4AF37>[ReleaseBuildGuard] Verifying security preconditions for NON-DEVELOPMENT RELEASE build...</color>");

            // 1. Verify MDI_ALLOW_MOCK_AUTH is not defined
            NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(report.summary.platformGroup);
            string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
            if (!string.IsNullOrEmpty(defines) && defines.Contains("MDI_ALLOW_MOCK_AUTH"))
            {
                throw new BuildFailedException("[ReleaseBuildGuard] BLOCKED: MDI_ALLOW_MOCK_AUTH is defined in Scripting Define Symbols for a non-development release build! Remove this symbol before building public releases.");
            }

            // 2. Verify QuantumPvPManager has empty quantumAppId
            var tempGo = new GameObject("Temp_ReleaseGuard_SecurityCheck");
            try
            {
                var mgr = tempGo.AddComponent<QuantumPvPManager>();
                if (!string.IsNullOrEmpty(mgr.quantumAppId))
                {
                    throw new BuildFailedException($"[ReleaseBuildGuard] BLOCKED: QuantumPvPManager.quantumAppId has hardcoded default '{mgr.quantumAppId}'! It must be empty in repository and release builds.");
                }
            }
            finally
            {
                Object.DestroyImmediate(tempGo);
            }

            // 3. Scan for hardcoded GUID secrets in C# scripts
            string assetsDir = Application.dataPath;
            var guidRegex = new Regex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", RegexOptions.Compiled);
            var csFiles = Directory.GetFiles(assetsDir, "*.cs", SearchOption.AllDirectories);
            foreach (var file in csFiles)
            {
                // Skip test files and editor archive from release secret scan
                if (file.Contains("/Tests/") || file.Contains("\\Tests\\") || file.Contains("/Editor_Archive/") || file.Contains("\\Editor_Archive\\"))
                    continue;

                string text = File.ReadAllText(file);
                var match = guidRegex.Match(text);
                if (match.Success)
                {
                    throw new BuildFailedException($"[ReleaseBuildGuard] BLOCKED: Hardcoded GUID secret pattern '{match.Value}' found in script '{file}'.");
                }
            }

            // 4. Scan .asset files for hardcoded Photon App IDs
            var appIdPattern = new Regex(@"(?:AppId(?:Realtime|Fusion|Quantum|Chat|Voice)|quantumAppId)\s*:\s*([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})", RegexOptions.Compiled);
            var assetFiles = Directory.GetFiles(assetsDir, "*.asset", SearchOption.AllDirectories);
            foreach (var file in assetFiles)
            {
                string text = File.ReadAllText(file);
                var match = appIdPattern.Match(text);
                if (match.Success)
                {
                    throw new BuildFailedException($"[ReleaseBuildGuard] BLOCKED: Hardcoded Photon App ID '{match.Groups[1].Value}' found in asset '{file}'.");
                }
            }

            Debug.Log("<color=#00e676>[ReleaseBuildGuard] Release build security guard passed cleanly. All release gates verified.</color>");
        }
    }
}
#endif
