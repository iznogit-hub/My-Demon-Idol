using System;
using System.IO;

namespace DemonIdol.Launch
{
    /// <summary>
    /// Single write protocol for project saves: temp file, backup copy, then replace.
    /// Callers own their documents. This type owns the replace rules.
    /// </summary>
    public static class AtomicFileSave
    {
        public static void Write(string path, string contents)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Save path is required.", nameof(path));

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string tempPath = path + ".tmp";
            string backupPath = path + ".bak";
            File.WriteAllText(tempPath, contents ?? string.Empty);

            if (File.Exists(path))
                File.Replace(tempPath, path, backupPath);
            else
                File.Move(tempPath, path);
        }

        public static bool TryRead(string path, out string contents, out bool recoveredFromBackup)
        {
            contents = null;
            recoveredFromBackup = false;
            if (string.IsNullOrEmpty(path))
                return false;

            if (TryReadFile(path, out contents) && IsAcceptedDocument(contents))
                return true;

            string backupPath = path + ".bak";
            if (TryReadFile(backupPath, out contents) && IsAcceptedDocument(contents))
            {
                recoveredFromBackup = true;
                return true;
            }

            contents = null;
            return false;
        }

        public static bool LooksLikeDocument(string contents)
        {
            return IsAcceptedDocument(contents);
        }

        static bool IsAcceptedDocument(string contents)
        {
            return JsonSyntax.IsValidDocument(contents);
        }

        static bool TryReadFile(string path, out string contents)
        {
            contents = null;
            if (!File.Exists(path))
                return false;
            try
            {
                contents = File.ReadAllText(path);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
