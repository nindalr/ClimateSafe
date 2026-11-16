using System.Collections.Generic;

namespace ClimateSafe
{
    internal sealed class ExtremeHeatRecommendationProvider : HazardRecommendationProvider
    {
        public override IReadOnlyList<Recommendation> GenerateRecommendations(
            IReadOnlyList<DimensionGap> gaps)
        {
            return BuildRecommendations(gaps, 3, new[]
            {
                "Siapkan wadah air minum dan perlengkapan pelindung dari sinar matahari saat bepergian.",
                "Tentukan tempat sejuk atau teduh yang dapat dijangkau saat suhu panas ekstrem.",
                "Catat kontak keluarga dan layanan darurat yang dapat dihubungi saat membutuhkan bantuan.",
                "Rencanakan ketersediaan air minum dan kebutuhan anggota keluarga selama periode panas ekstrem.",
                "Pelajari informasi peringatan suhu panas serta panduan kesiapsiagaan dari instansi terkait."
            });
        }
    }
}
