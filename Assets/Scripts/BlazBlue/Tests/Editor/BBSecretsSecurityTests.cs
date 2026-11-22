#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using DemonIdol.Network;

namespace DemonIdol.BlazBlue.Tests
{
    public class BBSecretsSecurityTests
    {
        [Test]
        public void NoHardcodedPhotonAppIdInAssets()
        {
            string assetsDir = Application.dataPath;

            // 1. Verify static constants and class defaults
            var tempGo = new GameObject("Temp_SecurityTest");
            try
            {
                var mgr = tempGo.AddComponent<QuantumPvPManager>();
                Assert.IsTrue(string.IsNullOrEmpty(mgr.quantumAppId), 
                    "QuantumPvPManager.quantumAppId default value must be empty string in repository");
            }
            finally
            {
                Object.DestroyImmediate(tempGo);
            }

            // 2. Scan every .cs file under Assets for GUID-shaped secrets
            var guidRegex = new Regex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", RegexOptions.Compiled);
            var csFiles = Directory.GetFiles(assetsDir, "*.cs", SearchOption.AllDirectories);
            foreach (var file in csFiles)
            {
                string text = File.ReadAllText(file);
                var matches = guidRegex.Matches(text);
                foreach (Match m in matches)
                {
                    Assert.Fail($"Hardcoded GUID-shaped secret/App ID '{m.Value}' detected in C# script: {file}");
                }
            }

            // 3. Scan every .asset file under Assets for populated Photon App IDs
            var appIdPattern = new Regex(@"(?:AppId(?:Realtime|Fusion|Quantum|Chat|Voice)|quantumAppId)\s*:\s*([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})", RegexOptions.Compiled);
            var assetFiles = Directory.GetFiles(assetsDir, "*.asset", SearchOption.AllDirectories);
            foreach (var file in assetFiles)
            {
                string text = File.ReadAllText(file);
                var match = appIdPattern.Match(text);
                if (match.Success)
                {
                    Assert.Fail($"Hardcoded Photon App ID '{match.Groups[1].Value}' detected in asset file: {file}");
                }
            }
        }
    }
}
#endif
