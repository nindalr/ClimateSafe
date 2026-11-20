namespace ClimateSafe
{
    // Sumber ringkasan materi Edukasi.
    //
    // Isi materi BELUM tersedia: belum ada tabelnya di database dan belum ada teks
    // yang disetujui. Teks diisi tim dari sumber resmi (BNPB, BPBD, BMKG); jangan
    // menulis materi sendiri di sini.
    //
    // Format teks yang dirender halaman Edukasi:
    //   - baris biasa                -> paragraf
    //   - baris berawalan "- "       -> poin (bullet)
    //   - baris kosong               -> pemisah antar blok
    // Nilai null atau string kosong membuat halaman menampilkan state
    // "belum tersedia".
    internal static class EducationContent
    {
        public static string? GetSummary(string hazardName) => null;
    }
}
