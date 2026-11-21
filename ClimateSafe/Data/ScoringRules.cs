using System;
using System.Globalization;

namespace ClimateSafe
{
    // Satu sumber aturan skor: batas level, target per dimensi, dan batas prioritas
    // rekomendasi. Nilai berasal dari keputusan tim (lihat docs/index.md bagian
    // "Aturan skor"); jangan menulis angkanya lagi di tempat lain.
    internal static class ScoringRules
    {
        // Batas level "Siap".
        public const double ReadyFrom = 80;

        // Target per dimensi: keputusan tim menetapkan nilainya sama dengan batas "Siap".
        public const double DimensionTarget = ReadyFrom;

        // Batas level "Cukup Siap".
        public const double FairFrom = 40;

        // Skor dimensi di bawah nilai ini mendapat prioritas "Tinggi".
        public const double HighPriorityBelow = 40;

        public const string ReadyLevel = "Siap";
        public const string FairLevel = "Cukup Siap";
        public const string NotReadyLevel = "Belum Siap";

        public const string HighPriority = "Tinggi";
        public const string MediumPriority = "Sedang";

        public static string GetLevel(double score)
        {
            if (score >= ReadyFrom) return ReadyLevel;
            if (score >= FairFrom) return FairLevel;
            return NotReadyLevel;
        }

        public static string GetPriority(double score)
        {
            return score < HighPriorityBelow ? HighPriority : MediumPriority;
        }

        // Angka untuk teks di layar, dibentuk dari konstanta di atas.
        public static string TargetText
        {
            get { return DimensionTarget.ToString("0", CultureInfo.InvariantCulture); }
        }

        public static string HighPriorityBelowText
        {
            get { return HighPriorityBelow.ToString("0", CultureInfo.InvariantCulture); }
        }
    }
}
