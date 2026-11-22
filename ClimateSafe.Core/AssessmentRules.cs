namespace ClimateSafe.Core;

public static class AssessmentRules
{
    public const int MaximumAnswerScore = 3;
    public const double ReadyScore = 80;
    public const double ModeratelyReadyScore = 40;
    public const double HighPriorityBelow = 40;

    public static (double Score, string Level) Evaluate(IEnumerable<int> answerScores)
    {
        ArgumentNullException.ThrowIfNull(answerScores);
        int total = 0, count = 0;
        foreach (int score in answerScores)
        {
            if (score < 0 || score > MaximumAnswerScore) throw new ArgumentOutOfRangeException(nameof(answerScores));
            total += score;
            count++;
        }
        if (count == 0) throw new ArgumentException("Jawaban tidak boleh kosong.", nameof(answerScores));
        double scorePercent = total * 100.0 / (count * MaximumAnswerScore);
        return (scorePercent, scorePercent >= ReadyScore ? "Siap" : scorePercent >= ModeratelyReadyScore ? "Cukup Siap" : "Belum Siap");
    }

    public static bool IsGap(int score, double threshold) => score * 100.0 / MaximumAnswerScore < threshold;
    public static string Priority(double scorePercent) => scorePercent < HighPriorityBelow ? "Tinggi" : "Sedang";
}
