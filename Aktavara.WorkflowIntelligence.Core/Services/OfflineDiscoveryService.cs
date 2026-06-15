namespace Aktavara.WorkflowIntelligence.Core.Services;

using Aktavara.WorkflowIntelligence.Core.Interfaces;
using Aktavara.WorkflowIntelligence.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

public class OfflineDiscoveryService : IOfflineDiscoveryService
{
    private readonly IWorkflowLibrary _workflowLibrary;
    private readonly ILogger<OfflineDiscoveryService> _logger;

    public OfflineDiscoveryService(
        IWorkflowLibrary workflowLibrary,
        ILogger<OfflineDiscoveryService> logger)
    {
        _workflowLibrary = workflowLibrary;
        _logger = logger;
    }

    public InferredWorkflowSuggestion InferWorkflowSuggestion(
        IEnumerable<ActivityEvent> events,
        string? candidateWorkflowId = null)
    {
        var eventList = events.ToList();
        _logger.LogInformation("Starting inference on {EventCount} events", eventList.Count);

        var sessions = ClusterBySessions(eventList);
        _logger.LogInformation("Clustered into {SessionCount} sessions", sessions.Count);

        var actionSequences = ExtractActionSequences(sessions);
        var frequencies = CalculateActionFrequencies(actionSequences);
        var commonSequences = FindCommonSequences(actionSequences);
        var rules = DeriveSignatureRules(frequencies);
        var states = BuildStateModel(commonSequences.FirstOrDefault() ?? new());
        var variants = DetectVariants(actionSequences, commonSequences.FirstOrDefault() ?? new());
        var riskLevel = InferRiskLevel(eventList);
        var tags = SuggestTags(eventList);
        var questions = GenerateWorkshopQuestions(variants, rules, eventList, candidateWorkflowId);
        var threshold = CalculateThreshold(actionSequences, rules);
        var notes = GenerateInferenceNotes(sessions.Count, eventList.Count, commonSequences, variants, riskLevel);

        return new InferredWorkflowSuggestion
        {
            SuggestedName = "Inferred Workflow",
            SuggestedDescription = "Workflow inferred from activity logs",
            SuggestedRiskLevel = riskLevel,
            SuggestedTags = tags,
            SuggestedRules = rules,
            SuggestedStates = states,
            SuggestedThreshold = threshold,
            Variants = variants,
            EvidenceSessions = sessions.Count,
            EvidenceEvents = eventList.Count,
            InferenceNotes = notes
        };
    }

    private Dictionary<string, List<ActivityEvent>> ClusterBySessions(List<ActivityEvent> events)
    {
        var sessions = new Dictionary<string, List<ActivityEvent>>();
        var sortedEvents = events.OrderBy(e => e.Timestamp).ToList();

        var currentSession = new List<ActivityEvent>();
        string? currentSessionKey = null;
        DateTime? lastEventTime = null;

        foreach (var evt in sortedEvents)
        {
            var sessionKey = $"{evt.UserName}:{evt.SessionId}";
            var isNewSession = currentSessionKey != sessionKey ||
                (lastEventTime.HasValue && (evt.Timestamp - lastEventTime.Value).TotalMinutes > 60);

            if (isNewSession && currentSession.Count > 0)
            {
                sessions[currentSessionKey!] = currentSession;
                currentSession = new List<ActivityEvent>();
            }

            currentSessionKey = sessionKey;
            currentSession.Add(evt);
            lastEventTime = evt.Timestamp;
        }

        if (currentSession.Count > 0 && currentSessionKey != null)
        {
            sessions[currentSessionKey] = currentSession;
        }

        return sessions;
    }

    private List<List<(int eventType, int recordKind)>> ExtractActionSequences(
        Dictionary<string, List<ActivityEvent>> sessions)
    {
        var sequences = new List<List<(int, int)>>();

        foreach (var session in sessions.Values)
        {
            var sequence = new List<(int, int)>();
            var sortedEvents = session.OrderBy(e => e.Timestamp).ToList();

            foreach (var evt in sortedEvents)
            {
                var pair = ((int)evt.EventType, (int)evt.RecordKind);
                if (sequence.Count == 0 || sequence.Last() != pair)
                {
                    sequence.Add(pair);
                }
            }

            if (sequence.Count > 0)
            {
                sequences.Add(sequence);
            }
        }

        return sequences;
    }

    private Dictionary<(int eventType, int recordKind), double> CalculateActionFrequencies(
        List<List<(int, int)>> sequences)
    {
        var frequencies = new Dictionary<(int, int), int>();
        var totalSessions = sequences.Count;

        foreach (var sequence in sequences)
        {
            var uniquePairs = sequence.Distinct();
            foreach (var pair in uniquePairs)
            {
                if (!frequencies.ContainsKey(pair))
                {
                    frequencies[pair] = 0;
                }
                frequencies[pair]++;
            }
        }

        return frequencies.ToDictionary(
            kvp => kvp.Key,
            kvp => totalSessions > 0 ? (double)kvp.Value / totalSessions : 0);
    }

    private List<List<(int eventType, int recordKind)>> FindCommonSequences(
        List<List<(int, int)>> sequences)
    {
        var sequenceFrequencies = new Dictionary<string, (int count, List<(int, int)> sequence)>();

        foreach (var sequence in sequences)
        {
            for (int len = 2; len <= Math.Min(6, sequence.Count); len++)
            {
                for (int i = 0; i <= sequence.Count - len; i++)
                {
                    var subseq = sequence.Skip(i).Take(len).ToList();
                    var key = string.Join("|", subseq.Select(p => $"{p.eventType}:{p.recordKind}"));

                    if (!sequenceFrequencies.ContainsKey(key))
                    {
                        sequenceFrequencies[key] = (0, subseq);
                    }
                    sequenceFrequencies[key] = (sequenceFrequencies[key].count + 1, subseq);
                }
            }
        }

        return sequenceFrequencies
            .Where(kvp => kvp.Value.count > 1)
            .OrderByDescending(kvp => kvp.Value.count)
            .Take(5)
            .Select(kvp => kvp.Value.sequence)
            .ToList();
    }

    private List<WorkflowSignatureRule> DeriveSignatureRules(
        Dictionary<(int eventType, int recordKind), double> frequencies)
    {
        var rules = new List<WorkflowSignatureRule>();

        foreach (var (pair, frequency) in frequencies)
        {
            if (frequency < 0.2)
                continue;

            var (required, weight) = frequency switch
            {
                > 0.8 => (true, 0.35),
                > 0.5 => (false, 0.20),
                _ => (false, 0.10)
            };

            var eventTypeName = Enum.GetName((ActivityEventType)pair.eventType) ?? "Unknown";
            var recordKindName = Enum.GetName((RecordKind)pair.recordKind) ?? "Unknown";

            rules.Add(new WorkflowSignatureRule
            {
                RuleId = $"{eventTypeName.ToLower()}_{recordKindName.ToLower()}",
                EventType = pair.eventType,
                RecordKind = pair.recordKind,
                Required = required,
                Weight = weight,
                Description = $"User {Humanize(eventTypeName)} {recordKindName} records",
                MaxAgeMinutes = null,
                MissingPenalty = required ? 0.25 : 0.10
            });
        }

        // Normalize weights
        var totalWeight = rules.Sum(r => r.Weight);
        if (totalWeight > 0)
        {
            foreach (var rule in rules)
            {
                rule.Weight /= totalWeight;
            }
        }

        return rules;
    }

    private List<WorkflowStateDefinition> BuildStateModel(List<(int eventType, int recordKind)> sequence)
    {
        var states = new List<WorkflowStateDefinition>();

        for (int i = 0; i < sequence.Count; i++)
        {
            var (eventType, recordKind) = sequence[i];
            var eventTypeName = Enum.GetName((ActivityEventType)eventType) ?? "Unknown";
            var recordKindName = Enum.GetName((RecordKind)recordKind) ?? "Unknown";

            states.Add(new WorkflowStateDefinition
            {
                StateId = SlugifyName($"{eventTypeName}_{recordKindName}"),
                Name = $"{Humanize(eventTypeName)} {recordKindName}",
                Description = $"User has {Humanize(eventTypeName).ToLower()} {recordKindName.ToLower()} records",
                Sequence = i,
                IsTerminal = i == sequence.Count - 1,
                NextStateId = i < sequence.Count - 1 ? SlugifyName($"{Enum.GetName((ActivityEventType)sequence[i + 1].eventType)}_{Enum.GetName((RecordKind)sequence[i + 1].recordKind)}") : null,
                HelpGuideId = string.Empty,
                Metadata = new Dictionary<string, object>()
            });
        }

        return states;
    }

    private List<WorkflowVariant> DetectVariants(
        List<List<(int, int)>> sequences,
        List<(int, int)> commonSequence)
    {
        if (commonSequence.Count == 0)
            return new List<WorkflowVariant>();

        var variants = new Dictionary<string, (int count, List<string> steps)>();

        foreach (var sequence in sequences)
        {
            if (SequencesEqual(sequence, commonSequence))
                continue;

            var differentSteps = FindDifferentSteps(sequence, commonSequence);
            if (differentSteps.Count == 0)
                continue;

            var key = string.Join("|", differentSteps);
            if (!variants.ContainsKey(key))
            {
                variants[key] = (0, differentSteps);
            }
            variants[key] = (variants[key].count + 1, differentSteps);
        }

        var result = new List<WorkflowVariant>();
        foreach (var (steps, (count, _)) in variants.Where(v => v.Value.count > 1))
        {
            result.Add(new WorkflowVariant
            {
                VariantId = $"variant_{result.Count + 1}",
                Description = $"Session variant: {string.Join(", ", steps)}",
                DifferentSteps = steps,
                OccurrenceCount = count,
                Percentage = sequences.Count > 0 ? (double)count / sequences.Count : 0
            });
        }

        return result;
    }

    private string InferRiskLevel(List<ActivityEvent> events)
    {
        var hasDeleteRecords = events.Any(e => e.EventType == ActivityEventType.DeleteRecords);
        if (hasDeleteRecords)
            return "High";

        var saveRecordsEvents = events.Where(e => e.EventType == ActivityEventType.SaveRecords).ToList();
        var hasAddedRecords = saveRecordsEvents.Any(e => e.RecordState == "Added");
        if (hasAddedRecords)
            return "High";

        var uniqueSaveRecordIds = events
            .Where(e => e.EventType == ActivityEventType.SaveRecords)
            .Select(e => e.AggregateId)
            .Distinct()
            .Count();

        if (uniqueSaveRecordIds > 1)
            return "Medium";

        var hasModifiedRecords = saveRecordsEvents.Any(e => e.RecordState == "Modified");
        if (hasModifiedRecords)
            return "Medium";

        return "Low";
    }

    private List<string> SuggestTags(List<ActivityEvent> events)
    {
        var tags = new HashSet<string>();

        // Add record kinds
        var recordKinds = events.Select(e => Enum.GetName((RecordKind)e.RecordKind))
            .Where(name => name != null)
            .Cast<string>()
            .Distinct();
        tags.UnionWith(recordKinds);

        // Add workspace types from OpenWorkspace events
        var workspaceEvents = events
            .Where(e => e.EventType == ActivityEventType.OpenWorkspace && !string.IsNullOrEmpty(e.WorkspaceKind))
            .Select(e => e.WorkspaceKind!)
            .Distinct();
        tags.UnionWith(workspaceEvents);

        return tags.OrderBy(t => t).ToList();
    }

    private List<string> GenerateWorkshopQuestions(
        List<WorkflowVariant> variants,
        List<WorkflowSignatureRule> rules,
        List<ActivityEvent> events,
        string? candidateWorkflowId)
    {
        var questions = new List<string>();

        // Pattern 1: Ambiguity check
        if (!string.IsNullOrEmpty(candidateWorkflowId))
        {
            var existingWorkflow = _workflowLibrary.GetById(candidateWorkflowId);
            if (existingWorkflow?.ActivitySignature != null)
            {
                var matchingRules = rules.Where(r => existingWorkflow.ActivitySignature.Any(
                    er => er.EventType == r.EventType && er.RecordKind == r.RecordKind)).Count();
                var matchPercentage = rules.Count > 0 ? (double)matchingRules / rules.Count : 0;

                if (matchPercentage > 0.6)
                {
                    questions.Add($"How is this workflow different from '{existingWorkflow.Name}'?");
                }
            }
        }

        // Pattern 2: Variants
        if (variants.Count > 0)
        {
            var variantSteps = variants.FirstOrDefault()?.DifferentSteps.FirstOrDefault();
            if (!string.IsNullOrEmpty(variantSteps))
            {
                questions.Add($"Sometimes this workflow includes {variantSteps} and sometimes it doesn't — when does that happen?");
            }
        }

        // Pattern 3: Save operations
        if (rules.Any(r => r.EventType == (int)ActivityEventType.SaveRecords))
        {
            questions.Add("What validation checks does the user perform before saving?");
        }

        // Pattern 4: Multiple record types
        var recordTypesInSave = events
            .Where(e => e.EventType == ActivityEventType.SaveRecords)
            .Select(e => Enum.GetName((RecordKind)e.RecordKind))
            .Distinct()
            .Count();

        if (recordTypesInSave > 1)
        {
            questions.Add("Are the records always saved together, or can they be saved separately?");
        }

        // Pattern 5: Always add
        questions.Add("What is the business purpose of this workflow?");
        questions.Add("How often does a user perform this task?");

        return questions;
    }

    private double CalculateThreshold(
        List<List<(int, int)>> sequences,
        List<WorkflowSignatureRule> rules)
    {
        if (sequences.Count == 0 || rules.Count == 0)
            return 0.7;

        var scores = new List<double>();
        foreach (var sequence in sequences)
        {
            var matchedRules = sequence.Where(pair =>
                rules.Any(r => r.EventType == pair.eventType && r.RecordKind == pair.recordKind)).Count();
            var score = rules.Count > 0 ? (double)matchedRules / rules.Count : 0;
            scores.Add(score);
        }

        var average = scores.Any() ? scores.Average() : 0.7;
        var threshold = average - 0.1;
        return Math.Clamp(threshold, 0.5, 0.9);
    }

    private List<string> GenerateInferenceNotes(
        int sessionCount,
        int eventCount,
        List<List<(int, int)>> commonSequences,
        List<WorkflowVariant> variants,
        string riskLevel)
    {
        var notes = new List<string>
        {
            $"Found {sessionCount} sessions with {eventCount} total events",
        };

        if (commonSequences.Count > 0)
        {
            var commonSeq = commonSequences.First();
            var appearancePercentage = commonSequences.Count > 0 ? 100 : 0;
            notes.Add($"Common sequence has {commonSeq.Count} steps (appears in {appearancePercentage}% of sessions)");
        }

        notes.Add($"Risk level set to {riskLevel}");

        if (variants.Count > 0)
        {
            notes.Add($"{variants.Count} variants detected");
        }

        return notes;
    }

    private bool SequencesEqual(List<(int, int)> seq1, List<(int, int)> seq2)
    {
        return seq1.Count == seq2.Count && seq1.SequenceEqual(seq2);
    }

    private List<string> FindDifferentSteps(List<(int, int)> sequence, List<(int, int)> commonSequence)
    {
        var differences = new List<string>();

        var seqSet = new HashSet<(int, int)>(sequence);
        var commonSet = new HashSet<(int, int)>(commonSequence);

        var onlyInSeq = seqSet.Except(commonSet).ToList();
        var onlyInCommon = commonSet.Except(seqSet).ToList();

        foreach (var (eventType, recordKind) in onlyInSeq)
        {
            differences.Add($"added {Enum.GetName((ActivityEventType)eventType)}");
        }

        foreach (var (eventType, recordKind) in onlyInCommon)
        {
            differences.Add($"skipped {Enum.GetName((ActivityEventType)eventType)}");
        }

        return differences;
    }

    private string Humanize(string pascalCase)
    {
        return Regex.Replace(pascalCase, "([A-Z])", " $1").Trim();
    }

    private string SlugifyName(string name)
    {
        return Regex.Replace(name, @"[^\w-]", "")
            .Replace(" ", "-")
            .Replace("_", "-")
            .ToLower();
    }
}
