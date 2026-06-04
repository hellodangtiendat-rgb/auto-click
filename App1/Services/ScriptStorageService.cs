using App1.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace App1.Services
{
    public class SavedClickScript
    {
        public string Name { get; set; } = "";
        public int LoopCount { get; set; } = 1;
        public List<ClickStep> Steps { get; set; } = new();
    }

    public static class ScriptStorageService
    {
        private static readonly string ScriptsFolder =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AutoClickApp",
                "Scripts"
            );

        public static async Task SaveAsync(string scriptName, int loopCount, IEnumerable<ClickStep> steps)
        {
            Directory.CreateDirectory(ScriptsFolder);

            var script = new SavedClickScript
            {
                Name = scriptName,
                LoopCount = loopCount,
                Steps = new List<ClickStep>(steps)
            };

            string json = JsonSerializer.Serialize(script, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            string safeName = MakeSafeFileName(scriptName);
            string path = Path.Combine(ScriptsFolder, safeName + ".json");

            await File.WriteAllTextAsync(path, json);
        }

        public static async Task<SavedClickScript?> LoadAsync(string scriptName)
        {
            string safeName = MakeSafeFileName(scriptName);
            string path = Path.Combine(ScriptsFolder, safeName + ".json");

            if (!File.Exists(path))
            {
                return null;
            }

            string json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<SavedClickScript>(json);
        }

        public static List<string> GetSavedScriptNames()
        {
            Directory.CreateDirectory(ScriptsFolder);

            var result = new List<string>();

            foreach (string file in Directory.GetFiles(ScriptsFolder, "*.json"))
                result.Add(Path.GetFileNameWithoutExtension(file));

            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        public static void Delete(string scriptName)
        {
            string path = Path.Combine(ScriptsFolder, MakeSafeFileName(scriptName) + ".json");
            if (File.Exists(path))
                File.Delete(path);
        }

        public static async Task<bool> RenameAsync(string oldName, string newName)
        {
            string oldPath = Path.Combine(ScriptsFolder, MakeSafeFileName(oldName) + ".json");
            string newPath = Path.Combine(ScriptsFolder, MakeSafeFileName(newName) + ".json");

            if (!File.Exists(oldPath)) return false;
            if (File.Exists(newPath) && oldPath != newPath) return false;

            string json = await File.ReadAllTextAsync(oldPath);
            SavedClickScript? script = JsonSerializer.Deserialize<SavedClickScript>(json);
            if (script == null) return false;

            script.Name = newName;
            string updated = JsonSerializer.Serialize(script, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(newPath, updated);

            if (oldPath != newPath)
                File.Delete(oldPath);

            return true;
        }

        public static string GetSafeName(string name) => MakeSafeFileName(name);

        private static string MakeSafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            return name.Trim();
        }
    }
}