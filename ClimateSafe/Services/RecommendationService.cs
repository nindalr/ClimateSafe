using System;
using System.Collections.Generic;

namespace ClimateSafe
{
    internal sealed class RecommendationService
    {
        public IReadOnlyList<Recommendation> GenerateRecommendations(
            Hazard hazard, IReadOnlyList<DimensionGap> gaps)
        {
            ArgumentNullException.ThrowIfNull(hazard);
            ArgumentNullException.ThrowIfNull(gaps);

            HazardRecommendationProvider provider = hazard.HazardId switch
            {
                1 => new FloodRecommendationProvider(),
                2 => new ExtremeWeatherRecommendationProvider(),
                3 => new ExtremeHeatRecommendationProvider(),
                _ => throw new ArgumentException("Jenis bencana belum didukung.", nameof(hazard))
            };

            return provider.GenerateRecommendations(gaps);
        }
    }
}
