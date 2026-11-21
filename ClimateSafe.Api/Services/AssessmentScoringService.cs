using ClimateSafe.Api.Models;

namespace ClimateSafe.Api.Services;

internal static class AssessmentScoringService
{
    public const int MaximumAnswerScore = 3;
    private const double ReadyScore = 80;
    private const double ModeratelyReadyScore = 40;
    private const double HighPriorityBelow = 40;

    public static (double Score, string Level) Evaluate(IReadOnlyList<ScoredAnswer> answers)
    {
        double score = answers.Sum(answer => answer.Score) * 100.0 / (answers.Count * MaximumAnswerScore);
        string level = score >= ReadyScore ? "Siap" : score >= ModeratelyReadyScore ? "Cukup Siap" : "Belum Siap";
        return (score, level);
    }

    public static bool IsGap(ScoredAnswer answer) =>
        answer.Score * 100.0 / MaximumAnswerScore < answer.Threshold;

    public static string Priority(ScoredAnswer answer) =>
        answer.Score * 100.0 / MaximumAnswerScore < HighPriorityBelow ? "Tinggi" : "Sedang";
}
