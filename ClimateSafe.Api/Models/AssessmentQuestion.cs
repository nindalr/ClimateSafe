namespace ClimateSafe.Api.Models;

public sealed record QuestionOption(int Id, string Text, int DisplayOrder);
public sealed record AssessmentQuestion(int Id, int DimensionId, string DimensionName,
    string Text, string? Description, IReadOnlyList<QuestionOption> Options);
