namespace ClimateSafe.Api.Models;

public sealed record AssessmentSubmissionRequest(Guid SubmissionKey, int HazardId, IReadOnlyList<int>? OptionIds);
public sealed record AssessmentSubmissionResult(int AssessmentId, double OverallScore, string Level, bool Created);

internal sealed record ScoredAnswer(int QuestionId, int DimensionId, string DimensionName,
    string QuestionText, int OptionId, string Value, int Score, double Threshold);
