using System;
using System.Globalization;

namespace ClimateSafe
{
    // Satu sumber aturan skor di sisi tampilan: batas level dan batas prioritas
    // rekomendasi, yang meneruskan ke AssessmentRules. Target per dimensi tidak ada
    // di sini; nilainya dibaca dari tabel preparedness_dimensions (passing_threshold).
    internal static class ScoringRules
    {
        // Batas level "Siap".
        public const double ReadyFrom = AssessmentRules.ReadyScore;

        // Batas level "Cukup Siap".
        public const double FairFrom = AssessmentRules.ModeratelyReadyScore;

        // Skor dimensi di bawah nilai ini mendapat prioritas "Tinggi".
        public const double HighPriorityBelow = AssessmentRules.HighPriorityBelow;

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

        public static string HighPriorityBelowText
        {
            get { return HighPriorityBelow.ToString("0", CultureInfo.InvariantCulture); }
        }
    }
}
