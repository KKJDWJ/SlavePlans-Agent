using System;
using System.IO;
namespace SlaveSplit.Infrastructure
{
    public interface IAgentLogger { void Info(string eventName, string value); void Error(string eventName, Exception error); }
    public sealed class AgentLogger : IAgentLogger
    {
        private readonly string path; private readonly object sync = new object(); public bool IncludeMessageContent { get; set; }
        public AgentLogger(string directory = null) { string root = directory ?? Environment.GetEnvironmentVariable("SLAVESPLIT_DATA_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SlaveSplit"); try { Directory.CreateDirectory(root); path = Path.Combine(root, "agent.log"); } catch { path = null; } }
        public void Info(string eventName, string value) { Write("INFO", eventName, value); }
        public void Error(string eventName, Exception error) { Write("ERROR", eventName, error == null ? "" : error.ToString()); }
        private void Write(string level, string name, string value) { if (path == null) return; try { lock (sync) File.AppendAllText(path, DateTime.Now.ToString("O") + " [" + level + "] " + name + ": " + value + Environment.NewLine); } catch { } }
    }
}
