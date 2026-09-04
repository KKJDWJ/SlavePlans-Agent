using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace SlaveSplit.Services.AI
{
    public sealed class IntentTrainingDataRepository : IIntentTrainingDataRepository
    {
        private const string ResourceName = "SlaveSplit.Data.IntentTrainingData.json"; private readonly string userPath;
        public IntentTrainingDataRepository(string directory = null) { string root = directory ?? Environment.GetEnvironmentVariable("SLAVESPLIT_DATA_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SlaveSplit"); Directory.CreateDirectory(root); userPath = Path.Combine(root, "intent_training_user.json"); }
        public IList<IntentTrainingExample> Load()
        {
            List<IntentTrainingExample> values = new List<IntentTrainingExample>(); try { using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)) using (StreamReader reader = stream == null ? null : new StreamReader(stream)) { if (reader != null) values.AddRange(Parse(reader.ReadToEnd(), false)); } using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SlaveSplit.Data.ClearConversationIntentTrainingData.json")) using (StreamReader reader = stream == null ? null : new StreamReader(stream)) { if (reader != null) values.AddRange(Parse(reader.ReadToEnd(), false)); } } catch { }
            try { if (File.Exists(userPath)) values.AddRange(Parse(File.ReadAllText(userPath), true)); } catch { }
            return values.Where(x => !string.IsNullOrWhiteSpace(x.Text) && x.Intent != LocalIntent.Unknown).GroupBy(x => x.Intent + "\n" + x.Text.Trim(), StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToList();
        }
        public void AddExample(LocalIntent intent, string text)
        {
            if (intent == LocalIntent.Unknown || string.IsNullOrWhiteSpace(text)) throw new ArgumentException("A valid intent and example are required."); List<StoredExample> values; try { values = File.Exists(userPath) ? JsonConvert.DeserializeObject<List<StoredExample>>(File.ReadAllText(userPath)) ?? new List<StoredExample>() : new List<StoredExample>(); } catch { values = new List<StoredExample>(); } if (values.Any(x => string.Equals(x.Intent, intent.ToString(), StringComparison.OrdinalIgnoreCase) && string.Equals(x.Text, text.Trim(), StringComparison.OrdinalIgnoreCase))) return; values.Add(new StoredExample { Intent = intent.ToString(), Text = text.Trim() }); string temp = userPath + ".tmp"; File.WriteAllText(temp, JsonConvert.SerializeObject(values, Formatting.Indented)); if (File.Exists(userPath)) File.Replace(temp, userPath, userPath + ".bak", true); else File.Move(temp, userPath);
        }
        private static IEnumerable<IntentTrainingExample> Parse(string json, bool userAdded) { List<StoredExample> values = JsonConvert.DeserializeObject<List<StoredExample>>(json) ?? new List<StoredExample>(); foreach (StoredExample value in values) { LocalIntent intent; if (Enum.TryParse<LocalIntent>(value.Intent, true, out intent)) yield return new IntentTrainingExample { Intent = intent, Text = value.Text, UserAdded = userAdded }; } }
        private sealed class StoredExample { public string Intent { get; set; } public string Text { get; set; } }
    }
}
