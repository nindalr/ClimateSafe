using System.Collections.ObjectModel;

namespace ClimateSafe.Core;

public sealed record GuestAnswer(int DimensionId, string DimensionName, string QuestionText,
    string SelectedAnswer, int Score, double DimensionScore);

public sealed class GuestChecklistItem
{
    internal GuestChecklistItem(int dimensionId, string dimensionName, string description, string priority)
    {
        DimensionId = dimensionId;
        DimensionName = dimensionName;
        Description = description;
        Priority = priority;
    }

    public int DimensionId { get; }
    public string DimensionName { get; }
    public string Description { get; }
    public string Priority { get; }
    public bool IsCompleted { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    internal void SetCompleted(bool completed)
    {
        if (completed == IsCompleted) return;
        IsCompleted = completed;
        CompletedAt = completed ? DateTime.UtcNow : null;
    }
}

public sealed class GuestAssessmentSession
{
    private readonly ReadOnlyCollection<GuestAnswer> answers;
    private readonly ReadOnlyCollection<GuestChecklistItem> checklist;

    private GuestAssessmentSession(string catalogVersion, GuestHazard hazard, double score, string level,
        List<GuestAnswer> answers, List<GuestChecklistItem> checklist)
    {
        CatalogVersion = catalogVersion;
        HazardId = hazard.Id;
        HazardName = hazard.Name;
        OverallScore = score;
        Level = level;
        this.answers = answers.AsReadOnly();
        this.checklist = checklist.AsReadOnly();
    }

    public string CatalogVersion { get; }
    public int HazardId { get; }
    public string HazardName { get; }
    public double OverallScore { get; }
    public string Level { get; }
    public IReadOnlyList<GuestAnswer> Answers => answers;
    public IReadOnlyList<GuestChecklistItem> Checklist => checklist;
    public int CompletedCount => checklist.Count(item => item.IsCompleted);

    // The key is dimension ID and the value is display order, not a PostgreSQL option ID.
    public static GuestAssessmentSession Evaluate(int hazardId, IReadOnlyDictionary<int, int> selections)
    {
        ArgumentNullException.ThrowIfNull(selections);
        GuestCatalogData catalog = GuestCatalog.Load();
        GuestHazard hazard = catalog.Hazards.SingleOrDefault(item => item.Id == hazardId)
            ?? throw new ArgumentException("Jenis bahaya tidak ditemukan.", nameof(hazardId));
        if (hazard.Questions.Count == 0 || selections.Count != hazard.Questions.Count)
            throw new ArgumentException("Semua pertanyaan harus dijawab tepat satu kali.", nameof(selections));

        var answers = new List<GuestAnswer>();
        var checklist = new List<GuestChecklistItem>();
        foreach (GuestQuestion question in hazard.Questions.OrderBy(item => item.DimensionId))
        {
            if (!selections.TryGetValue(question.DimensionId, out int order))
                throw new ArgumentException("Semua pertanyaan harus dijawab tepat satu kali.", nameof(selections));
            GuestOption option = question.Options.SingleOrDefault(item => item.DisplayOrder == order)
                ?? throw new ArgumentException("Pilihan jawaban tidak valid.", nameof(selections));
            double dimensionScore = option.Score * 100.0 / AssessmentRules.MaximumAnswerScore;
            answers.Add(new GuestAnswer(question.DimensionId, question.DimensionName, question.Text,
                option.Text, option.Score, dimensionScore));
            if (AssessmentRules.IsGap(option.Score, question.Threshold))
                checklist.Add(new GuestChecklistItem(question.DimensionId, question.DimensionName,
                    question.Recommendation, AssessmentRules.Priority(dimensionScore)));
        }

        var (score, level) = AssessmentRules.Evaluate(answers.Select(answer => answer.Score));
        return new GuestAssessmentSession(catalog.Version, hazard, score, level, answers, checklist);
    }

    public void SetChecklistCompleted(int dimensionId, bool completed)
    {
        GuestChecklistItem item = checklist.SingleOrDefault(entry => entry.DimensionId == dimensionId)
            ?? throw new ArgumentException("Item checklist tidak ditemukan.", nameof(dimensionId));
        item.SetCompleted(completed);
    }
}
