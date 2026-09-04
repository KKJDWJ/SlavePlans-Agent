using System;
using System.Collections.Generic;

namespace SlaveSplit.Services.AI
{
    public enum LocalIntent { CreateTask, ReadTask, UpdateTask, DeleteTask, ClearConversation, StartTask, CompleteTask, BlockTask, AddMemo, History, Briefing, GitHubQuery, GitHubImport, General, Unknown }
    public sealed class IntentTrainingExample { public LocalIntent Intent { get; set; } public string Text { get; set; } public bool UserAdded { get; set; } }
    public sealed class IntentClassificationResult
    {
        public LocalIntent Intent { get; set; } public double Confidence { get; set; } public LocalIntent SecondIntent { get; set; } public double SecondConfidence { get; set; } public string MatchedExample { get; set; } public bool Accepted { get; set; }
    }
    public interface IIntentTrainingDataRepository
    {
        IList<IntentTrainingExample> Load();
        void AddExample(LocalIntent intent, string text);
    }
    public interface ILocalIntentClassifier
    {
        IntentClassificationResult Classify(string text);
        void AddTrainingExample(LocalIntent intent, string text);
        int TrainingCount { get; }
    }
}
