using System;
using System.Collections.Generic;
using System.Linq;

namespace ClimateSafe
{
    internal sealed record ChecklistSession(
        int AssessmentId, string HazardName, DateTime CreatedAt,
        double OverallScore, string Level, IReadOnlyList<ChecklistItem> Items)
    {
        public int CompletedCount => Items.Count(item => item.IsCompleted);
        public double Progress => Items.Count == 0 ? 0 : CompletedCount * 100.0 / Items.Count;

        public override string ToString() =>
            HazardName + ", " + CreatedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm");
    }
}
