namespace ClimateSafe;

internal sealed class AssessmentHistoryRepository
{
    public IReadOnlyList<AssessmentHistoryAnswer> GetAnswers(int userId, int assessmentId) =>
        RecommendationRepository.GetDetail(assessmentId).Answers
        .Select(a => new AssessmentHistoryAnswer(a.DimensionName, a.QuestionText, a.SelectedAnswer, a.Score)).ToList();
}
