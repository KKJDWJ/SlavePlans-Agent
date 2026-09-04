using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SlaveSplit.Services.AI
{
    public sealed class KoreanTextPreprocessor
    {
        private static readonly string[] Particles = { "으로", "에서", "에게", "까지", "부터", "처럼", "하고", "이랑", "랑", "은", "는", "이", "가", "을", "를", "에", "도", "만", "좀" };
        public IList<string> ExtractFeatures(string value)
        {
            string normalized = Normalize(value); List<string> result = new List<string>(); foreach (string raw in normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) { string token = Stem(raw); if (token.Length > 0) { result.Add("w:" + token); result.Add("w:" + token); result.Add("w:" + token); } }
            string compact = normalized.Replace(" ", ""); for (int n = 2; n <= 3; n++) for (int i = 0; i + n <= compact.Length; i++) result.Add("c" + n + ":" + compact.Substring(i, n)); return result;
        }
        public string Normalize(string value) { string text = (value ?? "").ToLowerInvariant(); text = Regex.Replace(text, "github", " 깃허브 ", RegexOptions.IgnoreCase); text = Regex.Replace(text, "issue", " 이슈 ", RegexOptions.IgnoreCase); text = Regex.Replace(text, "task", " 업무 ", RegexOptions.IgnoreCase); return Regex.Replace(text, "[^0-9a-z가-힣]+", " ").Trim(); }
        private static string Stem(string value) { string text = value; foreach (string particle in Particles) if (text.Length > particle.Length + 1 && text.EndsWith(particle, StringComparison.Ordinal)) { text = text.Substring(0, text.Length - particle.Length); break; } return text; }
    }
    public sealed class TfIdfIntentClassifier : ILocalIntentClassifier
    {
        private readonly IIntentTrainingDataRepository repository; private readonly KoreanTextPreprocessor preprocessor; private List<Document> documents; private Dictionary<string, double> idf;
        public TfIdfIntentClassifier(IIntentTrainingDataRepository repository, KoreanTextPreprocessor preprocessor = null) { this.repository = repository; this.preprocessor = preprocessor ?? new KoreanTextPreprocessor(); Rebuild(); }
        public int TrainingCount { get { return documents.Count; } }
        public IntentClassificationResult Classify(string text)
        {
            Dictionary<string, double> query = Vector(preprocessor.ExtractFeatures(text)); if (query.Count == 0) return Unknown(); List<Scored> scores = documents.Select(x => new Scored { Document = x, Score = Cosine(query, x.Vector) }).OrderByDescending(x => x.Score).ToList(); if (scores.Count == 0) return Unknown(); List<IntentScore> intents = scores.GroupBy(x => x.Document.Intent).Select(g => new IntentScore { Intent = g.Key, Score = g.OrderByDescending(x => x.Score).Take(3).Select((x, i) => x.Score * (i == 0 ? .72 : i == 1 ? .18 : .10)).Sum(), Match = g.OrderByDescending(x => x.Score).First().Document.Text }).OrderByDescending(x => x.Score).ToList(); IntentScore best = intents[0], second = intents.Count > 1 ? intents[1] : new IntentScore { Intent = LocalIntent.Unknown }; double margin = Math.Max(0, best.Score - second.Score); double confidence = Math.Min(1, best.Score * .82 + margin * .55); return new IntentClassificationResult { Intent = best.Intent, Confidence = confidence, SecondIntent = second.Intent, SecondConfidence = second.Score, MatchedExample = best.Match, Accepted = confidence >= Threshold(best.Intent) };
        }
        public void AddTrainingExample(LocalIntent intent, string text) { repository.AddExample(intent, text); Rebuild(); }
        public static double Threshold(LocalIntent intent) { if (intent == LocalIntent.DeleteTask) return .34; if (intent == LocalIntent.CreateTask || intent == LocalIntent.CompleteTask || intent == LocalIntent.GitHubImport) return .28; if (intent == LocalIntent.UpdateTask || intent == LocalIntent.StartTask || intent == LocalIntent.BlockTask || intent == LocalIntent.AddMemo) return .25; if (intent == LocalIntent.General || intent == LocalIntent.History) return .18; if (intent == LocalIntent.ReadTask) return .23; return .25; }
        private void Rebuild() { IList<IntentTrainingExample> examples = repository.Load(); List<IList<string>> features = examples.Select(x => preprocessor.ExtractFeatures(x.Text)).ToList(); idf = new Dictionary<string, double>(); int count = Math.Max(1, features.Count); foreach (string term in features.SelectMany(x => x).Distinct()) { int df = features.Count(x => x.Contains(term)); idf[term] = Math.Log((count + 1.0) / (df + 1.0)) + 1.0; } documents = examples.Select((x, i) => new Document { Intent = x.Intent, Text = x.Text, Vector = Vector(features[i]) }).ToList(); }
        private Dictionary<string, double> Vector(IList<string> features) { Dictionary<string, double> value = new Dictionary<string, double>(); int total = Math.Max(1, features.Count); foreach (var group in features.GroupBy(x => x)) { double weight; if (idf.TryGetValue(group.Key, out weight)) value[group.Key] = group.Count() / (double)total * weight; } return value; }
        private static double Cosine(Dictionary<string, double> left, Dictionary<string, double> right) { double dot = 0; foreach (var item in left) { double value; if (right.TryGetValue(item.Key, out value)) dot += item.Value * value; } double a = Math.Sqrt(left.Values.Sum(x => x * x)), b = Math.Sqrt(right.Values.Sum(x => x * x)); return a <= 0 || b <= 0 ? 0 : dot / (a * b); }
        private static IntentClassificationResult Unknown() { return new IntentClassificationResult { Intent = LocalIntent.Unknown, SecondIntent = LocalIntent.Unknown, MatchedExample = null, Accepted = false }; }
        private sealed class Document { public LocalIntent Intent; public string Text; public Dictionary<string, double> Vector; }
        private sealed class Scored { public Document Document; public double Score; }
        private sealed class IntentScore { public LocalIntent Intent; public double Score; public string Match; }
    }
}
