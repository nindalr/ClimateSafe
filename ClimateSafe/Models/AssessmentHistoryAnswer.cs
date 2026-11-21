namespace ClimateSafe
{
    internal sealed record AssessmentHistoryAnswer(
        string DimensionName, string QuestionText, string SelectedAnswer, int Score);
}
