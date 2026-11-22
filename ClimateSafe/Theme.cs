using System.Drawing;

namespace ClimateSafe
{
    // Satu sumber warna dan ukuran dasar tampilan. Nilai diambil dari warna yang
    // sudah dipakai di Beranda (FrmDashboard) dan Asesmen (FrmAssessment).
    internal static class Theme
    {
        // Palet utama
        public static readonly Color Coral = Color.FromArgb(235, 122, 104);
        public static readonly Color CoralSoft = Color.FromArgb(253, 232, 226);
        public static readonly Color Navy = Color.FromArgb(37, 88, 150);
        public static readonly Color BtnBlue = Color.FromArgb(55, 108, 178);
        public static readonly Color BtnBlueHover = Color.FromArgb(44, 94, 164);
        public static readonly Color TitleBlue = Color.FromArgb(34, 62, 108);
        public static readonly Color Ink = Color.FromArgb(30, 41, 59);
        public static readonly Color Muted = Color.FromArgb(100, 116, 139);
        public static readonly Color Bg = Color.FromArgb(238, 242, 248);
        public static readonly Color Line = Color.FromArgb(226, 232, 240);
        public static readonly Color IconBlue = Color.FromArgb(226, 236, 250);

        // Warna lain yang sudah dipakai layar-layar sekarang
        public static readonly Color CoralHover = Color.FromArgb(224, 106, 88);
        public static readonly Color CoralInk = Color.FromArgb(159, 64, 51);
        public static readonly Color CoralBg = Color.FromArgb(254, 240, 236);
        public static readonly Color CoralLine = Color.FromArgb(249, 214, 206);
        public static readonly Color Blue = Color.FromArgb(66, 133, 205);
        public static readonly Color SubInk = Color.FromArgb(82, 102, 126);
        public static readonly Color EmptyInk = Color.FromArgb(82, 102, 126);
        public static readonly Color Line2 = Color.FromArgb(222, 230, 240);
        public static readonly Color BandBg = Color.FromArgb(250, 252, 255);
        public static readonly Color BadgeBg = Color.FromArgb(226, 235, 250);
        public static readonly Color BarHigh = Color.FromArgb(217, 80, 63);
        public static readonly Color BarTrack = Color.FromArgb(226, 234, 245);
        public static readonly Color ChipHover = Color.FromArgb(246, 249, 253);
        public static readonly Color ErrorRed = Color.FromArgb(217, 80, 63);
        public static readonly Color PillBlueBg = Color.FromArgb(228, 238, 247);
        public static readonly Color PillBlueInk = Color.FromArgb(47, 95, 148);
        public static readonly Color ReadOnlyBg = Color.FromArgb(244, 247, 252);
        public static readonly Color StepIdle = Color.FromArgb(240, 244, 250);

        // Tombol aksi utama (CTA). Coral lama tetap untuk menu aktif, lencana, dan angka.
        public static readonly Color CtaFill = Color.FromArgb(192, 75, 58);
        public static readonly Color CtaHover = Color.FromArgb(172, 62, 47);
        public static readonly Color CtaPressed = Color.FromArgb(150, 52, 40);

        // Isi tombol aksi utama: coral terang dengan teks gelap.
        // Kontras CtaBtnText di atas CtaBtnFill 6,4:1; di atas CtaBtnPressed 4,6:1.
        public static readonly Color CtaBtnFill = Color.FromArgb(235, 122, 104);
        public static readonly Color CtaBtnHover = Color.FromArgb(226, 108, 90);
        public static readonly Color CtaBtnPressed = Color.FromArgb(210, 92, 74);
        public static readonly Color CtaBtnText = Color.FromArgb(15, 23, 42);

        public const int SidebarWidth = 256;
    }
}
