using System.Collections.Generic;

namespace ClimateSafe
{
    internal sealed class FloodRecommendationProvider : HazardRecommendationProvider
    {
        public override IReadOnlyList<Recommendation> GenerateRecommendations(
            IReadOnlyList<DimensionGap> gaps)
        {
            return BuildRecommendations(gaps, 1, new[]
            {
                "Siapkan tas darurat dan simpan salinan dokumen penting dalam wadah kedap air.",
                "Tentukan jalur evakuasi menuju tempat lebih tinggi dan hindari rute yang melewati genangan.",
                "Catat kontak BPBD, petugas setempat, dan keluarga yang dapat dihubungi saat banjir.",
                "Rencanakan persediaan kebutuhan keluarga di tempat yang terlindung dari air banjir.",
                "Pelajari peringatan banjir serta prosedur evakuasi dari petugas setempat."
            });
        }
    }
}
