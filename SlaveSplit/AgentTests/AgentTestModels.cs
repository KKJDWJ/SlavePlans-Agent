using System;
using System.Collections.Generic;
using SlaveSplit.Models;

namespace SlaveSplit.AgentTests
{
    public enum AgentTestMode { Fast, LocalLlm, Integration, GitHub }
    public sealed class AgentTestFixture { public string Title { get; set; } public WorkTaskStatus Status { get; set; } public int DueOffsetDays { get; set; } public bool NoDueDate { get; set; } public TimeSpan? DueTime { get; set; } public bool ReminderEnabled { get; set; } public int? ReminderOffsetMinutes { get; set; } public int? CompletedOffsetDays { get; set; } public string WorkspaceId { get; set; } public TaskPriority Priority { get; set; } = TaskPriority.Normal; }
    public sealed class AgentTestStep
    {
        public string Input { get; set; } public string ExpectedContains { get; set; } public string ExpectedNotContains { get; set; } public string ExpectedOrder { get; set; } public int? ExpectedTaskCount { get; set; } public int? ExpectedMutationCount { get; set; } public string ExpectedAffectedTasks { get; set; } public string ExpectedTaskStates { get; set; } public string ExpectedCompletedAt { get; set; } public string ExpectedDueDates { get; set; } public string ExpectedPriorities { get; set; } public string ExpectedDueTimes { get; set; } public string ExpectedReminders { get; set; } public string ExpectedActiveWorkspace { get; set; } public bool PreserveCreatedAt { get; set; } public bool PreserveCompletedAt { get; set; } public string ExpectedTitle { get; set; } public string ExpectedDescription { get; set; } public WorkTaskStatus? ExpectedStatus { get; set; } public string ExpectedSource { get; set; } public bool? ExpectedFallback { get; set; } public bool ExpectSuccess { get; set; } = true; public int TimeoutSeconds { get; set; } = 15;
    }
    public sealed class AgentTestScenario
    {
        public string Id { get; set; } public string Group { get; set; } public string Name { get; set; } public string Description { get; set; } public AgentTestMode Mode { get; set; } public IList<AgentTestFixture> Fixtures { get; set; } = new List<AgentTestFixture>(); public IList<AgentTestStep> Steps { get; set; } = new List<AgentTestStep>();
    }
    public sealed class AgentTestResult
    {
        public string Status { get; set; } public string Group { get; set; } public string Scenario { get; set; } public int Step { get; set; } public string Input { get; set; } public string Expected { get; set; } public string Actual { get; set; } public string Source { get; set; } public long LatencyMs { get; set; } public string FailureReason { get; set; } public string ScenarioId { get; set; } public AgentTestMode Mode { get; set; } public bool FallbackRequested { get; set; } public bool LlmCalled { get; set; } public bool LlmSuccess { get; set; } public string LlmFailureKind { get; set; } public string LlmRawOutput { get; set; } public int? ExpectedMutationCount { get; set; } public int ActualMutationCount { get; set; } public string AffectedTasks { get; set; } public string BeforeState { get; set; } public string AfterState { get; set; } public bool WrongMutation { get; set; } public bool Passed { get { return Status == "PASS"; } } public bool Skipped { get { return Status.StartsWith("SKIPPED"); } }
    }
    public sealed class LocalAiPreflightResult { public bool Enabled { get; set; } public string Endpoint { get; set; } public string Model { get; set; } public bool Connected { get; set; } public bool ModelFound { get; set; } public bool GenerateSuccess { get; set; } public string FailureReason { get; set; } public string ServiceType { get; set; } public long GenerateLatencyMs { get; set; } public string RawOutput { get; set; } public bool Passed { get { return Enabled && Connected && ModelFound && GenerateSuccess; } } }
}
