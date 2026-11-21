#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DemonIdol.Editor
{
    /// <summary>
    /// Configures Android release signing credentials dynamically from environment variables
    /// or local-only untracked configuration, keeping secrets strictly out of git.
    /// Never prints, logs, or commits passwords.
    /// </summary>
    [InitializeOnLoad]
    public static class AndroidKeystoreConfig
    {
        private const string DEFAULT_ALIAS = "demonidol";
        private const string LOCAL_CONFIG_PATH = "Keystore/signing.local.json";

        static AndroidKeystoreConfig()
        {
            ApplyKeystoreConfiguration();
        }

        public static void ApplyKeystoreConfiguration()
        {
            string keystorePath = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PATH");
            string keystorePass = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS");
            string keyaliasName = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_NAME");
            string keyaliasPass = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_PASS");

            // Fallback to local untracked json config if env vars are unset
            if (string.IsNullOrEmpty(keystorePass) && File.Exists(LOCAL_CONFIG_PATH))
            {
                try
                {
                    string json = File.ReadAllText(LOCAL_CONFIG_PATH);
                    var cfg = JsonUtility.FromJson<LocalKeystoreData>(json);
                    if (cfg != null)
                    {
                        if (string.IsNullOrEmpty(keystorePath)) keystorePath = cfg.keystorePath;
                        if (string.IsNullOrEmpty(keystorePass)) keystorePass = cfg.keystorePass;
                        if (string.IsNullOrEmpty(keyaliasName)) keyaliasName = cfg.keyaliasName;
                        if (string.IsNullOrEmpty(keyaliasPass)) keyaliasPass = cfg.keyaliasPass;
                    }
                }
                catch
                {
                    // Fail silently without leaking content
                }
            }

            if (string.IsNullOrEmpty(keyaliasName))
            {
                keyaliasName = DEFAULT_ALIAS;
            }

            PlayerSettings.Android.keyaliasName = keyaliasName;

            if (!string.IsNullOrEmpty(keystorePath) && File.Exists(keystorePath))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystorePath;
            }

            if (!string.IsNullOrEmpty(keystorePass))
            {
                PlayerSettings.Android.keystorePass = keystorePass;
            }

            if (!string.IsNullOrEmpty(keyaliasPass))
            {
                PlayerSettings.Android.keyaliasPass = keyaliasPass;
            }

            // Note: Never log or print password strings.
        }

        [Serializable]
        private class LocalKeystoreData
        {
            public string keystorePath;
            public string keystorePass;
            public string keyaliasName;
            public string keyaliasPass;
        }
    }
}
#endif
