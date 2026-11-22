namespace ClimateSafe;

internal static class GuestSessionView
{
    public static ChecklistSession? GetChecklistSession()
    {
        var guest = Session.GuestAssessment;
        if (guest == null) return null;
        var items = guest.Checklist.Select(item => new ChecklistItem(item.DimensionId, item.Description,
            1, guest.HazardId * 100 + item.DimensionId, item.IsCompleted,
            item.CompletedAt)).ToList();
        return new ChecklistSession(1, guest.HazardName, Session.GuestAssessmentDate, guest.OverallScore, guest.Level, items);
    }

    public static List<DimensionScore> GetDimensions()
    {
        var guest = Session.GuestAssessment;
        if (guest == null) return new();
        var questions = ClimateSafe.Core.GuestCatalog.Load().Hazards.Single(h => h.Id == guest.HazardId).Questions;
        return guest.Answers.Select(a => new DimensionScore(guest.HazardId, a.DimensionId, a.DimensionName,
            a.DimensionScore, questions.Single(q => q.DimensionId == a.DimensionId).Threshold)).ToList();
    }

    public static List<Recommendation> GetRecommendations() => Session.GuestAssessment?.Checklist.Select(item =>
        new Recommendation(Session.GuestAssessment.HazardId * 100 + item.DimensionId, item.Description, item.Priority)).ToList() ?? new();
}
