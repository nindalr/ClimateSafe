using System;
using System.Collections.Generic;

namespace ClimateSafe
{
    internal sealed class ChecklistService
    {
        private readonly ChecklistRepository repository;

        public ChecklistService(ChecklistRepository? repository = null)
        {
            this.repository = repository ?? new ChecklistRepository();
        }

        public IReadOnlyList<ChecklistItem> GenerateItems(IReadOnlyList<Recommendation> recommendations)
        {
            ArgumentNullException.ThrowIfNull(recommendations);
            var ids = new HashSet<int>();
            var items = new List<ChecklistItem>();
            foreach (Recommendation recommendation in recommendations)
            {
                if (recommendation == null || !ids.Add(recommendation.RecommendationId))
                    throw new ArgumentException("Rekomendasi tidak valid atau berulang.", nameof(recommendations));
                items.Add(new ChecklistItem(0, recommendation.Description,
                    recommendationId: recommendation.RecommendationId));
            }
            return items.AsReadOnly();
        }

        public int SaveAssessment(int userId, Guid submissionKey, Hazard hazard,
            Assessment assessment, IReadOnlyList<Recommendation> recommendations)
        {
            var items = GenerateItems(recommendations);
            int id = repository.SaveAssessment(userId, submissionKey, hazard, assessment, recommendations, items);
            assessment.AssessmentId = id;
            return id;
        }

        public IReadOnlyList<ChecklistSession> GetSessions(int userId) => repository.GetSessions(userId);

        public ChecklistItem SetCompleted(int userId, int itemId, bool completed) =>
            repository.SetCompleted(userId, itemId, completed);
    }
}
