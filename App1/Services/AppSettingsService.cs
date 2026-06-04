using App1.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace App1.Services
{
    public static class AppSettingsService
    {
        private static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AutoClickApp", "settings.json");

        public static HotkeySettings Load()
        {
            if (!File.Exists(FilePath)) return new HotkeySettings();
            try
            {
                string json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<HotkeySettings>(json) ?? new HotkeySettings();
            }
            catch
            {
                return new HotkeySettings();
            }
        }

        public static async Task SaveAsync(HotkeySettings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(FilePath, json);
        }
    }
}
