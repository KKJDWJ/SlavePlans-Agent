using System;
using System.IO;
using Newtonsoft.Json;
namespace SlaveSplit.Services.GitHub
{
    public sealed class GitHubSettingsService
    {
        private readonly string path; public GitHubSettingsService(string directory = null) { string root = directory ?? Environment.GetEnvironmentVariable("SLAVESPLIT_DATA_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SlaveSplit"); Directory.CreateDirectory(root); path = Path.Combine(root, "github_settings.json"); }
        public GitHubSettings Load() { try { return File.Exists(path) ? JsonConvert.DeserializeObject<GitHubSettings>(File.ReadAllText(path)) ?? new GitHubSettings() : new GitHubSettings(); } catch { return new GitHubSettings(); } }
        public void Save(GitHubSettings settings) { string temp = path + ".tmp"; File.WriteAllText(temp, JsonConvert.SerializeObject(settings, Formatting.Indented)); if (File.Exists(path)) File.Replace(temp, path, path + ".bak", true); else File.Move(temp, path); }
    }
}
