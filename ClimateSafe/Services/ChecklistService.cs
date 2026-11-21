namespace ClimateSafe;

internal sealed class ChecklistService
{
    public IReadOnlyList<ChecklistSession> GetSessions(int userId)
    {
        var summaries = ApiClient.Get<List<ApiSession>>("api/assessments/sessions") ?? new();
        var sessions = new List<ChecklistSession>();
        foreach (var summary in summaries)
        {
            sessions.Add(new ChecklistSession(summary.Id, summary.HazardName, summary.CreatedAt,
                summary.OverallScore, summary.Level, summary.Checklist.Select(ToItem).ToList()));
        }
        return sessions;
    }

    public ChecklistItem SetCompleted(int userId, int itemId, bool completed) => ToItem(
        ApiClient.Patch<ApiItem>($"api/checklist/{itemId}", new { isCompleted = completed })
        ?? throw new InvalidOperationException("Respons checklist tidak tersedia."));

    private static ChecklistItem ToItem(ApiItem item) => new(item.Id, item.Description,
        item.AssessmentId, item.RecommendationId, item.IsCompleted, item.CompletedAt);
}
