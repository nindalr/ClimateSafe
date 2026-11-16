using System;
using System.Collections.Generic;
using System.Linq;

namespace ClimateSafe
{
    internal abstract class HazardRecommendationProvider
    {
        public abstract IReadOnlyList<Recommendation> GenerateRecommendations(
            IReadOnlyList<DimensionGap> gaps);

        protected IReadOnlyList<Recommendation> BuildRecommendations(
            IReadOnlyList<DimensionGap> gaps, int hazardId, IReadOnlyList<string> advice)
        {
            ArgumentNullException.ThrowIfNull(gaps);
            var recommendations = new List<Recommendation>();
            var dimensionIds = new HashSet<int>();
            foreach (DimensionGap gap in gaps.OrderBy(gap => gap?.Score))
            {
                if (gap == null || gap.DimensionId < 1 || gap.DimensionId > advice.Count ||
                    !double.IsFinite(gap.Score) || gap.Score < 0 || gap.Score > 100 ||
                    !dimensionIds.Add(gap.DimensionId))
                {
                    throw new ArgumentException("Data kelemahan dimensi tidak valid atau berulang.", nameof(gaps));
                }

                // ID katalog lokal dibedakan per hazard dan dimensi.
                int recommendationId = hazardId * 100 + gap.DimensionId;
                string priority = gap.Score < 40 ? "Tinggi" : "Sedang";
                recommendations.Add(new Recommendation(recommendationId,
                    advice[gap.DimensionId - 1], priority));
            }

            return recommendations.AsReadOnly();
        }
    }
}
