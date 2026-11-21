namespace ClimateSafe.Api.Models;

public sealed record AssessmentSummary(
    int Id, int HazardId, string HazardName, DateTime CreatedAt,
    double OverallScore, string Level, int ChecklistTotal, int ChecklistCompleted);

public sealed record AssessmentAnswer(string DimensionName, string QuestionText, string SelectedAnswer, int Score);

public sealed record DimensionScore(int DimensionId, string DimensionName, double Score, double PassingThreshold);

public sealed record ChecklistItem(int Id, int AssessmentId, int RecommendationId,
    string Description, bool IsCompleted, DateTime? CompletedAt);

public sealed record AssessmentDetail(AssessmentSummary Assessment,
    IReadOnlyList<AssessmentAnswer> Answers, IReadOnlyList<DimensionScore> Dimensions,
    IReadOnlyList<ChecklistItem> Checklist);

public sealed record UpdateChecklistRequest(bool? IsCompleted);
