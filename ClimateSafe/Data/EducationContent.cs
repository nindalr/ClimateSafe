using System;

namespace ClimateSafe
{
    // Sumber ringkasan materi Edukasi:
    //   Banjir: BNPB, Buku Saku Tanggap Tangkas Tangguh Menghadapi Bencana (2019),
    //     hal. 17-20.
    //   Cuaca Ekstrem: BMKG, siaran pers "Waspada Cuaca Ekstrem, Indonesia Mulai
    //     Masuk Musim Pancaroba".
    //   Suhu Panas Ekstrem: imbauan BMKG sebagaimana dimuat detikHealth (15 Okt 2025)
    //     dan pernyataan BMKG bahwa Indonesia tidak mengalami gelombang panas.
    //     Perlu dicek ulang ke bmkg.go.id.
    //
    // Format teks yang dirender halaman Edukasi:
    //   - baris biasa                -> paragraf
    //   - baris berawalan "- "       -> poin (bullet)
    //   - baris kosong               -> pemisah antar blok
    // Nilai null atau string kosong membuat halaman menampilkan state
    // "belum tersedia".
    internal static class EducationContent
    {
        private const string Banjir =
            """
            Banjir adalah bencana yang paling sering melanda Indonesia. Jenisnya ada tiga: banjir genangan, banjir bandang, dan banjir rob (air laut pasang naik ke daratan).

            Penyebab utamanya adalah hujan yang jauh di atas normal dan air laut pasang. Ulah manusia juga ikut berperan, misalnya penggunaan lahan yang tidak tepat dan membuang sampah ke sungai.

            Sebelum banjir:
            - Jangan membangun rumah di bantaran sungai dan daerah rawan banjir.
            - Jaga lingkungan sekitar, termasuk tidak membuang sampah ke sungai dan saluran air.

            Saat banjir:
            - Jauhi saluran air agar tidak terseret arus.
            - Matikan listrik di rumah.
            - Mengungsilah ke tempat aman atau posko sedini mungkin, selagi genangan masih bisa dilewati.
            - Pindahkan barang berharga ke tempat yang lebih tinggi.
            - Jika air terus naik, hubungi kantor kepala desa, lurah, atau camat.

            Setelah banjir:
            - Segera bersihkan rumah dan gunakan antiseptik untuk membunuh kuman.
            - Siapkan air bersih untuk mencegah diare.
            """;

        private const string CuacaEkstrem =
            """
            Cuaca ekstrem sering terjadi saat pancaroba, yaitu masa peralihan antara musim hujan dan kemarau. Bentuknya bisa berupa hujan lebat dalam waktu singkat yang disertai kilat, petir, dan angin kencang.

            Pagi sampai siang biasanya terik. Panas matahari membuat udara naik dan awan terbentuk, sehingga hujan biasanya turun sore sampai malam. Awan Cumulonimbus (CB) berbentuk seperti bunga kol, berwarna abu-abu, dengan tepi yang jelas. Awan ini berkaitan dengan petir, angin kencang, puting beliung, dan hujan es.

            Hujan lebat juga dapat memicu banjir bandang dan tanah longsor, terutama di daerah perbukitan.

            Yang dianjurkan BMKG:
            - Tingkatkan kewaspadaan, terutama bagi warga di daerah perbukitan rawan longsor.
            - Siapkan payung, topi, atau jas hujan saat beraktivitas di luar rumah.
            - Jaga kesehatan, karena cuaca panas dan hujan bisa silih berganti dengan cepat.
            - Pantau terus informasi dan Peringatan Dini resmi dari BMKG.
            """;

        private const string SuhuPanasEkstrem =
            """
            Suhu panas ekstrem terjadi ketika sinar matahari sangat kuat dan langit minim awan. BMKG menjelaskan salah satu penyebabnya adalah pergeseran semu matahari ke selatan, yang membuat radiasi di Indonesia bagian selatan lebih kuat.

            BMKG menyatakan Indonesia tidak mengalami gelombang panas (heatwave). Yang terjadi adalah suhu panas terik harian yang tetap perlu diwaspadai.

            Risiko kesehatan:
            - Dehidrasi (kekurangan cairan tubuh).
            - Kelelahan akibat panas.
            - Heat stroke (sengatan panas), yang bisa berakibat fatal.

            Yang dianjurkan BMKG:
            - Hindari terkena matahari langsung pukul 10.00-16.00 WIB.
            - Gunakan topi, kacamata hitam, payung, dan tabir surya saat di luar ruangan.
            - Perbanyak minum air putih.
            - Kurangi aktivitas fisik berat di luar ruangan, terutama anak-anak, lansia, dan penderita penyakit kronis.
            - Pantau cuaca lewat aplikasi InfoBMKG.
            """;

        public static string? GetSummary(string hazardName)
        {
            string name = (hazardName ?? "").Trim();

            if (string.Equals(name, "Banjir", StringComparison.OrdinalIgnoreCase)) return Banjir;
            if (string.Equals(name, "Cuaca Ekstrem", StringComparison.OrdinalIgnoreCase)) return CuacaEkstrem;
            if (string.Equals(name, "Suhu Panas Ekstrem", StringComparison.OrdinalIgnoreCase)) return SuhuPanasEkstrem;

            return null;
        }
    }
}
