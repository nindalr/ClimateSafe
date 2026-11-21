namespace ClimateSafe;

internal sealed record DimensionScore(int HazardId, int DimensionId, string DimensionName, double Score, double PassingThreshold);

internal sealed class RecommendationRepository
{
    public IReadOnlyList<Hazard> GetHazards() =>
        (ApiClient.Get<List<ApiHazard>>("api/hazards") ?? new()).Select(h => new Hazard(h.Id, h.Name)).ToList();

    public IReadOnlyList<DimensionScore> GetDimensionScores(int userId, int assessmentId)
    {
        var detail = GetDetail(assessmentId);
        return detail.Dimensions.Select(d => new DimensionScore(detail.Assessment.HazardId,
            d.DimensionId, d.DimensionName, d.Score, d.PassingThreshold)).ToList();
    }

    public Dictionary<int, string> GetDimensionNames() =>
        (ApiClient.Get<List<ApiQuestion>>("api/hazards/1/questions") ?? new())
        .ToDictionary(question => question.DimensionId, question => question.DimensionName);

    public IReadOnlyList<Recommendation> GetRecommendations(int assessmentId) =>
        GetDetail(assessmentId).Recommendations.Select(r => new Recommendation(r.RecommendationId, r.Description, r.Priority)).ToList();

    internal static ApiDetail GetDetail(int assessmentId) => ApiClient.Get<ApiDetail>($"api/assessments/{assessmentId}")
        ?? throw new InvalidOperationException("Detail asesmen tidak tersedia.");
}
