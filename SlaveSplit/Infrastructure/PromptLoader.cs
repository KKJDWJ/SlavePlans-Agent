using System.IO;
using System.Reflection;
namespace SlaveSplit.Infrastructure { public static class PromptLoader { public static string LoadSlaveAgentPrompt() { Assembly assembly = Assembly.GetExecutingAssembly(); using (Stream stream = assembly.GetManifestResourceStream("SlaveSplit.Prompts.SlaveAgentSystemPrompt.txt")) { if (stream == null) return "Return a valid SlaveSplit command JSON object."; using (StreamReader reader = new StreamReader(stream)) return reader.ReadToEnd(); } } } }
