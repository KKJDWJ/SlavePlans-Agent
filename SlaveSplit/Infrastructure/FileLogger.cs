using System;
using System.IO;
namespace SlaveSplit.Infrastructure
{
    public sealed class FileLogger
    {
        private readonly string path; private readonly object sync = new object();
        public FileLogger(string directory) { Directory.CreateDirectory(directory); path = Path.Combine(directory, "errors.log"); }
        public void Error(string operation, Exception error) { try { lock (sync) File.AppendAllText(path, DateTime.Now.ToString("O") + " [ERROR] " + operation + Environment.NewLine + error + Environment.NewLine); } catch { } }
    }
}
