using ClimateSafe.Api.Models;

namespace ClimateSafe.Api.Services;

internal static class AssessmentScoringService
{
    public const int MaximumAnswerScore = ClimateSafe.Core.AssessmentRules.MaximumAnswerScore;

    public static (double Score, string Level) Evaluate(IReadOnlyList<ScoredAnswer> answers)
    {
        return ClimateSafe.Core.AssessmentRules.Evaluate(answers.Select(answer => answer.Score));
    }

    public static bool IsGap(ScoredAnswer answer) =>
        ClimateSafe.Core.AssessmentRules.IsGap(answer.Score, answer.Threshold);

    public static string Priority(ScoredAnswer answer) =>
        Priority(answer.Score * 100.0 / MaximumAnswerScore);

    public static string Priority(double score) => ClimateSafe.Core.AssessmentRules.Priority(score);
}
