using System;
using System.IO;
using Newtonsoft.Json;
namespace SlaveSplit.Services
{
    public sealed class UserSettings
    {
        public UserSettings() { StartupPage = "Chat"; }
        public string StartupPage { get; set; }
        public bool AdminMode { get; set; }
    }
    public sealed class UserSettingsService
    {
        private readonly string path;
        public UserSettingsService(string directory = null) { string root = directory ?? Environment.GetEnvironmentVariable("SLAVESPLIT_DATA_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SlaveSplit"); Directory.CreateDirectory(root); path = Path.Combine(root, "settings.json"); }
        public UserSettings Load() { try { return File.Exists(path) ? JsonConvert.DeserializeObject<UserSettings>(File.ReadAllText(path)) ?? new UserSettings() : new UserSettings(); } catch { return new UserSettings(); } }
        public void Save(UserSettings value) { string temp = path + ".tmp"; File.WriteAllText(temp, JsonConvert.SerializeObject(value, Formatting.Indented)); if (File.Exists(path)) { string backup = path + ".bak"; File.Replace(temp, path, backup, true); } else File.Move(temp, path); }
    }
}
