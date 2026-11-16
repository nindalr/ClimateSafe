using System.Collections.Generic;

namespace ClimateSafe
{
    internal sealed class ExtremeWeatherRecommendationProvider : HazardRecommendationProvider
    {
        public override IReadOnlyList<Recommendation> GenerateRecommendations(
            IReadOnlyList<DimensionGap> gaps)
        {
            return BuildRecommendations(gaps, 2, new[]
            {
                "Siapkan senter, baterai cadangan, dan tas darurat untuk menghadapi gangguan listrik saat badai.",
                "Tentukan bangunan kokoh sebagai tempat berlindung dan rute yang menghindari pohon serta papan reklame.",
                "Simpan kontak petugas darurat, penyedia listrik, dan keluarga untuk situasi cuaca ekstrem.",
                "Rencanakan kebutuhan keluarga jika cuaca ekstrem menghambat perjalanan atau distribusi barang.",
                "Pelajari peringatan cuaca BMKG dan prosedur berlindung saat angin kencang atau petir."
            });
        }
    }
}
