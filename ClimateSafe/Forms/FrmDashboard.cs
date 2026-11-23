using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ClimateSafe
{
    // Kode glyph font ikon Windows (Segoe Fluent Icons / Segoe MDL2 Assets, kode sama).
    // Bila hanya Segoe UI Symbol tersedia, glyph lama dipakai sebagai cadangan.
    internal static class Ico
    {
        private static string Pick(string icon, string legacy)
        {
            return UI.LegacyIcons ? legacy : icon;
        }

        public static string Home { get { return Pick("\uE80F", "\u2302"); } }
        public static string Edit { get { return Pick("\uE70F", "\u270E"); } }
        public static string Star { get { return Pick("\uE734", "\u2606"); } }
        public static string CheckBox { get { return Pick("\uE73A", "\u2611"); } }
        public static string Check { get { return Pick("\uE73E", "\u2714"); } }
        public static string History { get { return Pick("\uE81C", "\u21BA"); } }
        public static string Library { get { return Pick("\uE8F1", "\u25A4"); } }
        public static string People { get { return Pick("\uE716", "\u263A"); } }
        public static string Page { get { return Pick("\uE7C3", "\u25A4"); } }
        public static string Report { get { return Pick("\uE9D9", "\u21BA"); } }
        public static string List { get { return Pick("\uE8FD", "\u2630"); } }
        public static string Rain { get { return Pick("\uEB42", "\u2248"); } }
        public static string Storm { get { return Pick("\uE945", "\u2602"); } }
        public static string Sun { get { return Pick("\uE706", "\u2600"); } }
        public static string Tools { get { return Pick("\uE90F", "\u2692"); } }
        public static string Route { get { return Pick("\uE72A", "\u27A4"); } }
        public static string Phone { get { return Pick("\uE717", "\u260E"); } }
        public static string Info { get { return Pick("\uE946", "\u2139"); } }
        public static string Clock { get { return Pick("\uE823", "\u25F7"); } }
        public static string Calendar { get { return Pick("\uE787", "\u25A3"); } }
        public static string Warning { get { return Pick("\uE7BA", "\u26A0"); } }
        public static string Bolt { get { return Pick("\uE945", "\u26A1"); } }
        public static string Lock { get { return Pick("\uE72E", "\U0001F512"); } }
    }

    internal static class UI
    {
        public static readonly string FontName = PickFontName(
            new[] { "Plus Jakarta Sans", "Inter", "Segoe UI" }, "Segoe UI");

        // Font ikon dipilih sekali untuk seluruh aplikasi: Segoe Fluent Icons (Windows 11),
        // lalu Segoe MDL2 Assets (Windows 10), lalu Segoe UI Symbol sebagai cadangan.
        public static readonly string IconFontName = PickFontName(
            new[] { "Segoe Fluent Icons", "Segoe MDL2 Assets", "Segoe UI Symbol" }, "Segoe UI Symbol");

        public static bool LegacyIcons
        {
            get { return IconFontName == "Segoe UI Symbol"; }
        }

        private static string PickFontName(string[] candidates, string fallback)
        {
            try
            {
                using (InstalledFontCollection fc = new InstalledFontCollection())
                {
                    foreach (string name in candidates)
                    {
                        foreach (FontFamily f in fc.Families)
                        {
                            if (string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase))
                                return name;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return fallback;
        }

        public static Font MakeFont(string name, float size, FontStyle style)
        {
            try
            {
                return new Font(name, size, style);
            }
            catch (Exception)
            {
                return new Font(FontFamily.GenericSansSerif, size, style);
            }
        }

        public static Font Body(float size, FontStyle style)
        {
            return MakeFont(FontName, size, style);
        }

        public static GraphicsPath RoundPath(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            radius = Math.Min(radius, Math.Min(r.Width, r.Height) / 2);
            if (radius <= 0 || r.Width <= 0 || r.Height <= 0)
            {
                p.AddRectangle(r);
                return p;
            }
            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        // Logo ClimateSafe: gambar 44x44 ditampilkan 40x40 di dalam kotak, rasio tetap.
        private static Image logoImage;

        public static PictureBox AddLogo(Control parent)
        {
            if (logoImage == null)
            {
                logoImage = Properties.Resources.Logo;
            }
            PictureBox pb = new PictureBox();
            pb.Image = logoImage;
            pb.SizeMode = PictureBoxSizeMode.Zoom;
            pb.BackColor = Color.Transparent;
            pb.Bounds = new Rectangle(0, 0, parent.Width, parent.Height);
            parent.Controls.Add(pb);
            return pb;
        }
    }

    internal class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            DoubleBuffered = true;
        }
    }

    internal class RoundPanel : Panel
    {
        public int Radius = 12;
        public Color Fill = Color.White;
        public Color GradientTo = Color.Empty;
        public float Angle = 0f;
        public Color BorderColor = Color.Empty;
        public Action<Graphics, Rectangle> Decorate;

        private bool clipRegion;

        public RoundPanel()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        public bool ClipRegion
        {
            get { return clipRegion; }
            set
            {
                clipRegion = value;
                UpdateRegion();
            }
        }

        private void UpdateRegion()
        {
            if (!clipRegion || Width < 2 || Height < 2) return;
            using (GraphicsPath gp = UI.RoundPath(new Rectangle(0, 0, Width, Height), Radius))
            {
                Region old = Region;
                Region = new Region(gp);
                if (old != null) old.Dispose();
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRegion();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Width < 2 || Height < 2) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using (GraphicsPath path = UI.RoundPath(rect, Radius))
            {
                if (GradientTo != Color.Empty)
                {
                    using (LinearGradientBrush br = new LinearGradientBrush(rect, Fill, GradientTo, Angle))
                    {
                        g.FillPath(br, path);
                    }
                }
                else
                {
                    using (SolidBrush br = new SolidBrush(Fill))
                    {
                        g.FillPath(br, path);
                    }
                }

                if (Decorate != null)
                {
                    GraphicsState st = g.Save();
                    g.SetClip(path, CombineMode.Intersect);
                    Decorate(g, rect);
                    g.Restore(st);
                }

                if (BorderColor != Color.Empty)
                {
                    using (Pen pen = new Pen(BorderColor))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }
        }
    }

    internal class ThinBar : Control
    {
        public float Value = 0f;
        public Color BarColor = Color.SteelBlue;
        public Color TrackColor = Color.LightGray;

        public ThinBar()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Width < 2 || Height < 2) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int rad = Height / 2;

            using (GraphicsPath track = UI.RoundPath(new Rectangle(0, 0, Width - 1, Height - 1), rad))
            using (SolidBrush b = new SolidBrush(TrackColor))
            {
                g.FillPath(b, track);
            }

            float v = Math.Max(0f, Math.Min(1f, Value));
            int w = (int)((Width - 1) * v);
            if (w > 0)
            {
                w = Math.Max(w, Height);
                using (GraphicsPath fill = UI.RoundPath(new Rectangle(0, 0, w, Height - 1), rad))
                using (SolidBrush b = new SolidBrush(BarColor))
                {
                    g.FillPath(b, fill);
                }
            }
        }
    }

    internal class CircleProgress : Control
    {
        public int Percent = 0;
        public string MainText = "";
        public string SubText = "";
        public Color Accent = Color.SteelBlue;
        public Color TrackColor = Color.LightGray;
        public Color InkColor = Color.Black;
        public Color MutedColor = Color.Gray;

        public CircleProgress()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Width < 30 || Height < 30) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            const int thick = 9;
            Rectangle rect = new Rectangle(thick / 2 + 1, thick / 2 + 1,
                Width - thick - 3, Height - thick - 3);

            using (Pen track = new Pen(TrackColor, thick))
            {
                g.DrawEllipse(track, rect);
            }

            float sweep = 360f * Math.Max(0, Math.Min(100, Percent)) / 100f;
            if (sweep > 0f)
            {
                using (Pen arc = new Pen(Accent, thick))
                {
                    arc.StartCap = LineCap.Round;
                    arc.EndCap = LineCap.Round;
                    g.DrawArc(arc, rect, -90f, sweep);
                }
            }

            using (Font f1 = UI.Body(15f, FontStyle.Bold))
            using (Font f2 = UI.Body(7f, FontStyle.Regular))
            using (SolidBrush t1 = new SolidBrush(InkColor))
            using (SolidBrush t2 = new SolidBrush(MutedColor))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                g.DrawString(MainText, f1, t1, new RectangleF(0, Height / 2f - 22, Width, 26), sf);
                g.DrawString(SubText, f2, t2, new RectangleF(0, Height / 2f + 5, Width, 16), sf);
            }
        }
    }

    internal class TickBox : Control
    {
        private bool isChecked;
        public Color OnColor = Color.Tomato;
        public Color OffBorder = Color.Silver;
        public event EventHandler CheckedChanged;

        public TickBox()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(16, 16);
            Cursor = Cursors.Hand;
        }

        public bool Checked
        {
            get { return isChecked; }
            set
            {
                if (isChecked == value) return;
                isChecked = value;
                Invalidate();
                if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Checked = !Checked;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = UI.RoundPath(new Rectangle(0, 0, Width - 1, Height - 1), 4))
            {
                if (isChecked)
                {
                    using (SolidBrush b = new SolidBrush(OnColor))
                    {
                        g.FillPath(b, path);
                    }
                    using (Pen p = new Pen(Color.White, 2f))
                    {
                        p.StartCap = LineCap.Round;
                        p.EndCap = LineCap.Round;
                        p.LineJoin = LineJoin.Round;
                        g.DrawLines(p, new PointF[]
                        {
                            new PointF(4f, 8.5f),
                            new PointF(7f, 11.5f),
                            new PointF(12f, 5f)
                        });
                    }
                }
                else
                {
                    using (SolidBrush b = new SolidBrush(Color.White))
                    {
                        g.FillPath(b, path);
                    }
                    using (Pen p = new Pen(OffBorder))
                    {
                        g.DrawPath(p, path);
                    }
                }
            }
        }
    }

    internal class CoverPicture : Control
    {
        public Image Photo;
        public Color ColorTop = Color.Gray;
        public Color ColorBottom = Color.DimGray;
        public string Emoji = "";
        public string BadgeGlyph = "";
        public Color BadgeColor = Color.SteelBlue;
        public float EmojiSize = 40f;
        public int BadgeSize = 28;
        public float BadgeFontSize = 10f;

        public CoverPicture()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Width < 2 || Height < 2) return;

            Graphics g = e.Graphics;
            Rectangle full = new Rectangle(0, 0, Width, Height);

            if (Photo != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                float scale = Math.Max((float)Width / Photo.Width, (float)Height / Photo.Height);
                float sw = Width / scale;
                float sh = Height / scale;
                float sx = (Photo.Width - sw) / 2f;
                float sy = (Photo.Height - sh) / 2f;
                g.DrawImage(Photo, full, sx, sy, sw, sh, GraphicsUnit.Pixel);
            }
            else
            {
                using (LinearGradientBrush br = new LinearGradientBrush(full, ColorTop, ColorBottom, 90f))
                {
                    g.FillRectangle(br, full);
                }
                if (!string.IsNullOrEmpty(Emoji))
                {
                    using (Font f = UI.MakeFont("Segoe UI Emoji", EmojiSize, FontStyle.Regular))
                    {
                        TextRenderer.DrawText(g, Emoji, f,
                            new Rectangle(0, 8, Width, Height - 16), Color.White,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                            TextFormatFlags.NoPadding);
                    }
                }
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (!string.IsNullOrEmpty(BadgeGlyph))
            {
                Rectangle badge = new Rectangle(12, Height - BadgeSize - 12, BadgeSize, BadgeSize);
                using (SolidBrush wb = new SolidBrush(Color.White))
                {
                    g.FillEllipse(wb, badge);
                }
                using (Font bf = UI.MakeFont(UI.IconFontName, BadgeFontSize, FontStyle.Regular))
                {
                    TextRenderer.DrawText(g, BadgeGlyph, bf, badge, BadgeColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.NoPadding);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && Photo != null)
            {
                Photo.Dispose();
                Photo = null;
            }
            base.Dispose(disposing);
        }
    }

    internal class RoundButton : Control
    {
        public Color Fill = Color.SteelBlue;
        public Color HoverFill = Color.RoyalBlue;
        public Color BorderColor = Color.Empty;
        public Color TextColor = Color.White;
        // Kosong = pakai HoverFill yang digelapkan 10%.
        public Color PressedFill = Color.Empty;
        public int Radius = 8;
        private bool hover;
        private bool pressed;

        public RoundButton()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hover = false;
            pressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            pressed = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            pressed = false;
            Invalidate();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Width < 2 || Height < 2) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using (GraphicsPath path = UI.RoundPath(rect, Radius))
            {
                Color face = Fill;
                if (hover) face = HoverFill;
                if (pressed) face = PressedFill != Color.Empty ? PressedFill : PillButton.Darken(HoverFill, 0.10);
                using (SolidBrush b = new SolidBrush(face))
                {
                    g.FillPath(b, path);
                }
                if (BorderColor != Color.Empty)
                {
                    using (Pen p = new Pen(BorderColor))
                    {
                        g.DrawPath(p, path);
                    }
                }
            }

            TextRenderer.DrawText(g, Text, Font, ClientRectangle, TextColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }
    }

    internal class Chevron : Control
    {
        public Color LineColor = Color.Gray;

        public Chevron()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(16, 16);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float cx = Width / 2f;
            float cy = Height / 2f;
            using (Pen p = new Pen(LineColor, 1.8f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                p.LineJoin = LineJoin.Round;
                g.DrawLines(p, new PointF[]
                {
                    new PointF(cx - 2.5f, cy - 5f),
                    new PointF(cx + 2.5f, cy),
                    new PointF(cx - 2.5f, cy + 5f)
                });
            }
        }
    }

    internal static class Nav
    {
        private static FrmDashboard dashboard;
        private static FrmAssessment assessment;
        private static FrmPilihBahaya pilihBahaya;
        private static FrmRekomendasi rekomendasi;
        private static FrmChecklist checklist;
        private static FrmRiwayat riwayat;
        private static FrmEdukasi edukasi;
        private static FrmProfil profil;
        private static FrmHasilAsesmen hasil;
        private static bool quitting;

        public static void Register(Form f)
        {
            FrmDashboard d = f as FrmDashboard;
            if (d != null) dashboard = d;

            FrmAssessment a = f as FrmAssessment;
            if (a != null) assessment = a;

            FrmPilihBahaya pb = f as FrmPilihBahaya;
            if (pb != null) pilihBahaya = pb;

            FrmRekomendasi r = f as FrmRekomendasi;
            if (r != null) rekomendasi = r;

            FrmChecklist c = f as FrmChecklist;
            if (c != null) checklist = c;

            FrmRiwayat h = f as FrmRiwayat;
            if (h != null) riwayat = h;

            FrmEdukasi ed = f as FrmEdukasi;
            if (ed != null) edukasi = ed;

            FrmProfil pr = f as FrmProfil;
            if (pr != null) profil = pr;

            FrmHasilAsesmen hs = f as FrmHasilAsesmen;
            if (hs != null) hasil = hs;

            f.FormClosed += delegate (object sender, FormClosedEventArgs e)
            {
                Quit();
            };
        }

        public static void ShowDashboard(Form from)
        {
            if (dashboard == null || dashboard.IsDisposed)
            {
                dashboard = new FrmDashboard();
            }
            SwitchTo(dashboard, from);
        }

        // Langkah 1 alur asesmen: halaman Pilih Bahaya.
        public static void ShowAssessment(Form from)
        {
            if (pilihBahaya == null || pilihBahaya.IsDisposed)
            {
                pilihBahaya = new FrmPilihBahaya();
            }
            SwitchTo(pilihBahaya, from);
        }

        // Langkah 2: halaman pertanyaan dengan bahaya terpilih dari langkah 1.
        public static void ShowAssessmentQuestions(Form from, int hazardIdx)
        {
            if (assessment == null || assessment.IsDisposed)
            {
                assessment = new FrmAssessment();
            }
            assessment.ApplyHazard(hazardIdx);
            SwitchTo(assessment, from);
        }

        public static void ShowRekomendasi(Form from)
        {
            if (rekomendasi == null || rekomendasi.IsDisposed)
            {
                rekomendasi = new FrmRekomendasi();
            }
            SwitchTo(rekomendasi, from);
        }

        public static void ShowChecklist(Form from, int? assessmentId = null)
        {
            if (Session.CurrentUser == null && !Session.IsGuest)
            {
                MessageBox.Show(from, "Silakan login untuk membuka checklist.", "Checklist");
                return;
            }
            if (checklist == null || checklist.IsDisposed)
            {
                checklist = new FrmChecklist();
            }
            if (assessmentId.HasValue) checklist.PendingAssessmentId = assessmentId.Value;
            SwitchTo(checklist, from);
        }

        public static void ShowRiwayat(Form from)
        {
            // Tamu boleh membuka Riwayat: halaman menampilkan kartu Mode Tamu.
            if (Session.CurrentUser == null && !Session.IsGuest)
            {
                MessageBox.Show(from, "Silakan login untuk membuka riwayat.", "Riwayat");
                return;
            }
            if (riwayat == null || riwayat.IsDisposed)
            {
                riwayat = new FrmRiwayat();
            }
            SwitchTo(riwayat, from);
        }

        // hazardName opsional: tujuan bahaya dari kartu Beranda, dibaca FrmEdukasi
        // saat halaman ditampilkan. Menu sidebar memanggil tanpa argumen.
        public static void ShowEdukasi(Form from, string? hazardName = null)
        {
            if (edukasi == null || edukasi.IsDisposed)
            {
                edukasi = new FrmEdukasi();
            }
            if (hazardName != null) edukasi.PendingHazard = hazardName;
            SwitchTo(edukasi, from);
        }

        public static void ShowProfil(Form from)
        {
            if (Session.CurrentUser == null)
            {
                MessageBox.Show(from, "Silakan login untuk membuka profil.", "Profil",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (profil == null || profil.IsDisposed)
            {
                profil = new FrmProfil();
            }
            SwitchTo(profil, from);
        }

        public static void ShowHasil(Form from, HasilAsesmenData data)
        {
            if (hasil == null || hasil.IsDisposed)
            {
                hasil = new FrmHasilAsesmen();
            }
            hasil.SetData(data);
            SwitchTo(hasil, from);
        }

        // Panggil saat login/logout supaya form lama (cache) tidak dipakai lagi
        public static void Reset()
        {
            if (dashboard != null && !dashboard.IsDisposed) dashboard.Hide();
            if (assessment != null && !assessment.IsDisposed) assessment.Hide();
            if (rekomendasi != null && !rekomendasi.IsDisposed) rekomendasi.Hide();
            if (checklist != null && !checklist.IsDisposed) checklist.Hide();
            if (riwayat != null && !riwayat.IsDisposed) riwayat.Hide();
            if (edukasi != null && !edukasi.IsDisposed) edukasi.Hide();
            if (pilihBahaya != null && !pilihBahaya.IsDisposed)
            {
                // Halaman langkah 1 dibuang mengikuti pola Profil dan Hasil.
                quitting = true;
                pilihBahaya.Hide();
                pilihBahaya.Dispose();
            }
            if (hasil != null && !hasil.IsDisposed)
            {
                // Halaman hasil dibuang agar hasil pengguna sebelumnya tidak muncul lagi.
                quitting = true;
                hasil.Hide();
                hasil.Dispose();
            }
            if (profil != null && !profil.IsDisposed)
            {
                // Halaman profil dibuang agar data pengguna sebelumnya tidak tersisa.
                // quitting ditahan supaya Dispose tidak menutup aplikasi.
                quitting = true;
                profil.Hide();
                profil.Dispose();
            }
            dashboard = null;
            assessment = null;
            pilihBahaya = null;
            rekomendasi = null;
            checklist = null;
            riwayat = null;
            edukasi = null;
            profil = null;
            hasil = null;
            quitting = false;
        }

        private static void SwitchTo(Form target, Form from)
        {
            if (target == from) return;

            if (from != null && from.WindowState != FormWindowState.Minimized)
            {
                target.WindowState = from.WindowState;
            }

            target.Show();
            target.Activate();

            if (from != null) from.Hide();
        }

        // Keluar ke halaman login tanpa menutup aplikasi. Logika logout tetap milik
        // FormLogin (handler FormClosed pada form utama: user.Logout() lalu login tampil).
        public static void Logout(Form from)
        {
            bool isGuest = Session.IsGuest;
            if (isGuest && Session.GuestAssessment != null && MessageBox.Show(from,
                "Hasil asesmen dan checklist tamu akan dihapus. Keluar sekarang?", "Keluar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            Form main = dashboard;

            quitting = true;
            if (from != null && !from.IsDisposed && from != main) from.Hide();

            Reset();            // buang semua form cache; flag quitting kembali false

            quitting = true;    // lindungi penutupan form utama
            try
            {
                if (main != null && !main.IsDisposed)
                {
                    main.Close();
                    if (isGuest) ShowLoginAgain();
                }
                else
                {
                    ShowLoginAgain();
                }
            }
            finally
            {
                if (isGuest)
                {
                    Session.GuestAssessment = null;
                    Session.IsGuest = false;
                }
                quitting = false;
            }
        }

        // Dipakai bila form utama sudah tidak ada: pastikan login tetap tampil.
        private static void ShowLoginAgain()
        {
            Session.CurrentUser = null;
            foreach (Form form in Application.OpenForms)
            {
                if (form is ClimateSafe.Auth.FormLogin && !form.IsDisposed)
                {
                    form.Show();
                    form.Activate();
                    return;
                }
            }
        }

        private static void Quit()
        {
            if (quitting) return;
            quitting = true;
            Application.Exit();
        }
    }

    public partial class FrmDashboard : Form
    {
        private readonly Color coral = Theme.Coral;
        private readonly Color coralSoft = Theme.CoralSoft;
        private readonly Color navy = Theme.Navy;
        private readonly Color blue = Theme.Blue;
        private readonly Color btnBlue = Theme.BtnBlue;
        private readonly Color titleBlue = Theme.TitleBlue;
        private readonly Color ink = Theme.Ink;
        private readonly Color muted = Theme.Muted;
        private readonly Color bg = Theme.Bg;
        private readonly Color line = Theme.Line;
        private readonly Color iconBlue = Theme.IconBlue;
        // Gaya pil level, nilai sama dengan FrmHasilAsesmen.
        private readonly Color coralInk = Theme.CoralInk;
        private readonly Color pillBlueBg = Theme.PillBlueBg;
        private readonly Color pillBlueInk = Theme.PillBlueInk;

        private const int PageMaxW = 1127;
        private const int PageMinW = 960;
        private const int Gap = 24;                  // jarak antar kartu (kelipatan 8)
        private const int C3 = 361;                  // kolom kartu bahaya (3 kolom)

        // Satu pembagian kolom dipakai baris atas dan baris bawah.
        private const int ColLeftNum = 647;
        private const int ColLeftDen = 1105;

        private const int HeaderH = 159;
        private const int RowTopY = 181;
        private const int RowTopH = 245;
        private const int RowBottomY = RowTopY + RowTopH + Gap;
        private const int RowBottomH = 443;
        private const int HazardTitleY = RowBottomY + RowBottomH + Gap;
        private const int HazardCardsY = HazardTitleY + 61;
        private const int HazardCardH = 227;
        private const int HazardPicH = 80;
        private const int FooterY = HazardCardsY + HazardCardH + Gap;
        private const int FooterH = 80;
        private const int PageH = FooterY + FooterH;

        private readonly List<RoundPanel> menuItems = new List<RoundPanel>();
        private readonly RoundPanel[] hazardCards = new RoundPanel[3];
        private readonly Label[,] hazardSummary = new Label[3, 2];

        private Panel dashScroll;
        private Panel dashPage;
        private RoundPanel dashHeader;
        private RoundPanel cardScore;
        private RoundPanel cardQuick;
        private RoundPanel cardChecklist;
        private RoundPanel cardPriority;
        private RoundPanel dashFooter;
        private bool dashBuilt;
        private ChecklistSession? latestSession;
        private readonly ChecklistService checklistService;
        private readonly WeatherService weatherService = new WeatherService();
        private readonly System.Windows.Forms.Timer weatherTimer = new System.Windows.Forms.Timer { Interval = 15 * 60 * 1000 };
        private Label weatherLabel;
        private Label weatherIcon;
        private Label weatherDetails;
        private DateTime weatherLastFetched = DateTime.MinValue;
        private bool weatherLoading;
        private int savedDataVersion;
        private Label? savedDataLoading;

        public FrmDashboard() : this(new ChecklistService())
        {
        }

        internal FrmDashboard(ChecklistService checklistService)
        {
            this.checklistService = checklistService;
            InitializeComponent();
            BuildDashboard();
            Nav.Register(this);
            weatherTimer.Tick += async (sender, e) => await RefreshWeatherAsync();
            FormClosed += (sender, e) => weatherTimer.Dispose();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && dashBuilt)
            {
                RequestHazardSummaries();
                RefreshUserCard();
                if (greetingLabel != null) greetingLabel.Text = "Halo, " + Session.FirstName;
                RefreshSavedData();
                weatherTimer.Start();
                _ = RefreshWeatherAsync();
            }
            else if (!Visible) { weatherTimer.Stop(); savedDataVersion++; }
        }

        private async System.Threading.Tasks.Task RefreshWeatherAsync()
        {
            if (weatherLoading || DateTime.UtcNow - weatherLastFetched < TimeSpan.FromMinutes(15)) return;
            weatherLoading = true;
            weatherLabel.Text = "Memuat cuaca...";
            weatherDetails.Text = "Sumber: Open-Meteo";
            try
            {
                WeatherData weather = await weatherService.GetYogyakartaAsync();
                if (IsDisposed || Disposing) return;
                weatherLabel.Text = $"Yogyakarta {weather.Temperature:0}°C";
                weatherIcon.Text = weather.Icon;
                weatherDetails.Text = $"{weather.Condition} · Open-Meteo";
                weatherLastFetched = DateTime.UtcNow;
            }
            catch (Exception ex) when (ex is System.Net.Http.HttpRequestException ||
                                       ex is System.Threading.Tasks.TaskCanceledException ||
                                       ex is System.Text.Json.JsonException || ex is IOException ||
                                       ex is InvalidOperationException || ex is FormatException)
            {
                if (IsDisposed || Disposing) return;
                weatherLabel.Text = "Tidak tersedia";
                weatherIcon.Text = "?";
                weatherDetails.Text = "Sumber: Open-Meteo";
            }
            finally { weatherLoading = false; }
        }

        internal async void RefreshSavedData()
        {
            int version = ++savedDataVersion;
            bool guest = Session.IsGuest;
            int userId = Session.CurrentUser?.UserId ?? 0;
            cardScore.Visible = cardChecklist.Visible = cardPriority.Visible = false;
            if (savedDataLoading != null) { dashPage.Controls.Remove(savedDataLoading); savedDataLoading.Dispose(); }
            savedDataLoading = AddText(dashPage, "Memuat hasil asesmen...", 20, RowTopY + 25, 400, 30, 11f, false, muted);
            try
            {
                var loaded = guest ? GuestSessionView.GetChecklistSession() : userId == 0 ? null
                    : await Task.Run(() => checklistService.GetSessions(userId).FirstOrDefault());
                if (IsDisposed || version != savedDataVersion || Session.IsGuest != guest || (Session.CurrentUser?.UserId ?? 0) != userId) return;
                latestSession = loaded;
                dashPage.SuspendLayout();
                cardScore.Dispose();
                cardChecklist.Dispose();
                cardPriority.Dispose();
                BuildScoreCard(dashPage);
                BuildChecklist(dashPage);
                BuildPriorities(dashPage);
                LayoutPage();
            }
            catch (Exception)
            {
                if (IsDisposed || version != savedDataVersion) return;
                MessageBox.Show(this, "Data asesmen belum dapat dimuat. Periksa koneksi API.",
                    "Dashboard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                if (!IsDisposed && version == savedDataVersion)
                {
                    if (savedDataLoading != null) { dashPage.Controls.Remove(savedDataLoading); savedDataLoading.Dispose(); savedDataLoading = null; }
                    cardScore.Visible = cardChecklist.Visible = cardPriority.Visible = true;
                    dashPage.ResumeLayout(true);
                }
            }
        }

        private void BuildDashboard()
        {
            AutoScaleMode = AutoScaleMode.None;
            Text = "ClimateSafe - Dashboard";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1440, 900);
            MinimumSize = new Size(1280, 720);
            WindowState = FormWindowState.Maximized;
            BackColor = bg;
            Font = UI.Body(9f, FontStyle.Regular);

            SuspendLayout();

            Controls.Clear();
            menuItems.Clear();
            dashBuilt = false;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 2;
            root.RowCount = 1;
            root.BackColor = bg;
            root.Margin = Padding.Empty;
            root.Padding = Padding.Empty;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 256));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            BufferedPanel sidebar = new BufferedPanel();
            sidebar.Dock = DockStyle.Fill;
            sidebar.BackColor = Color.White;
            sidebar.Margin = Padding.Empty;
            root.Controls.Add(sidebar, 0, 0);

            BufferedPanel sc = new BufferedPanel();
            sc.Dock = DockStyle.Fill;
            sc.AutoScroll = true;
            sc.AutoScrollMargin = new Size(0, 24);
            sc.BackColor = bg;
            sc.Margin = Padding.Empty;
            root.Controls.Add(sc, 1, 0);
            dashScroll = sc;

            BuildSidebar(sidebar);

            BufferedPanel pg = new BufferedPanel();
            pg.Size = new Size(PageMaxW, PageH);
            pg.Location = new Point(29, 24);
            pg.BackColor = bg;
            sc.Controls.Add(pg);
            dashPage = pg;

            BuildHeader(dashPage);
            BuildScoreCard(dashPage);
            BuildQuickActions(dashPage);
            BuildChecklist(dashPage);
            BuildPriorities(dashPage);
            BuildHazards(dashPage);
            BuildFooter(dashPage);

            dashBuilt = true;
            sc.Resize += delegate (object sender, EventArgs e) { LayoutPage(); };

            ResumeLayout(true);
            LayoutPage();
            RequestHazardSummaries();
        }

        private void LayoutPage()
        {
            if (!dashBuilt || dashScroll == null || dashPage == null) return;

            int avail = dashScroll.ClientSize.Width - 58;
            int w = Math.Max(PageMinW, Math.Min(PageMaxW, avail));
            int x = Math.Max(29, (dashScroll.ClientSize.Width - w) / 2);

            dashPage.Left = x + dashScroll.AutoScrollPosition.X;
            dashPage.Width = w;

            int c3 = (w - 2 * Gap) / 3;
            int lastX = 2 * (c3 + Gap);
            int lastW = w - lastX;

            dashHeader.SetBounds(0, 0, w, HeaderH);

            int cw = LeftColumnWidth(w);
            int rw = w - cw - Gap;

            cardScore.SetBounds(0, RowTopY, cw, RowTopH);
            cardQuick.SetBounds(cw + Gap, RowTopY, rw, RowTopH);

            cardChecklist.SetBounds(0, RowBottomY, cw, RowBottomH);
            cardPriority.SetBounds(cw + Gap, RowBottomY, rw, RowBottomH);

            hazardCards[0].SetBounds(0, HazardCardsY, c3, HazardCardH);
            hazardCards[1].SetBounds(c3 + Gap, HazardCardsY, c3, HazardCardH);
            hazardCards[2].SetBounds(lastX, HazardCardsY, lastW, HazardCardH);

            dashFooter.SetBounds(0, FooterY, w, FooterH);
        }

        private Label NewLabel(Control parent, string text, int x, int y, int w, int h,
                               float size, FontStyle style, Color color,
                               ContentAlignment align, string fontName)
        {
            Label lb = new Label();
            lb.AutoSize = false;
            lb.UseMnemonic = false;
            lb.Text = text;
            lb.Bounds = new Rectangle(x, y, w, h);
            lb.Font = UI.MakeFont(fontName, size, style);
            lb.ForeColor = color;
            lb.BackColor = Color.Transparent;
            lb.TextAlign = align;
            parent.Controls.Add(lb);
            return lb;
        }

        private Label AddText(Control parent, string text, int x, int y, int w, int h,
                              float size, bool bold, Color color)
        {
            return NewLabel(parent, text, x, y, w, h, size,
                bold ? FontStyle.Bold : FontStyle.Regular,
                color, ContentAlignment.TopLeft, UI.FontName);
        }

        private Label AddAligned(Control parent, string text, int x, int y, int w, int h,
                                 float size, bool bold, Color color, ContentAlignment align)
        {
            return NewLabel(parent, text, x, y, w, h, size,
                bold ? FontStyle.Bold : FontStyle.Regular,
                color, align, UI.FontName);
        }

        private Label AddSymbol(Control parent, string glyph, int x, int y, int w, int h,
                                float size, Color color)
        {
            return NewLabel(parent, glyph, x, y, w, h, size, FontStyle.Regular,
                color, ContentAlignment.MiddleCenter, UI.IconFontName);
        }

        private Chevron AddChevron(Control parent, int x, int y, Color color)
        {
            Chevron c = new Chevron();
            c.LineColor = color;
            c.Bounds = new Rectangle(x, y, 16, 16);
            parent.Controls.Add(c);
            return c;
        }

        private RoundPanel NewCard(Control parent, int x, int y, int w, int h, int radius,
                                   Color fill, Color gradient, float angle, Color border)
        {
            RoundPanel p = new RoundPanel();
            p.Bounds = new Rectangle(x, y, w, h);
            p.Radius = radius;
            p.Fill = fill;
            p.GradientTo = gradient;
            p.Angle = angle;
            p.BorderColor = border;
            parent.Controls.Add(p);
            return p;
        }

        private RoundPanel Card(Control parent, int x, int y, int w, int h, int radius,
                                Color fill, Color border)
        {
            return NewCard(parent, x, y, w, h, radius, fill, Color.Empty, 0f, border);
        }

        private RoundPanel IconBox(Control parent, string glyph, int x, int y, int size,
                                   Color fill, Color fore)
        {
            RoundPanel p = Card(parent, x, y, size, size, 8, fill, Color.Empty);
            AddSymbol(p, glyph, 0, 0, size, size, size >= 36 ? 12f : 10.5f, fore);
            return p;
        }

        private Panel Divider(Control parent, int x, int y, int w)
        {
            Panel p = new Panel();
            p.Bounds = new Rectangle(x, y, w, 1);
            p.BackColor = line;
            parent.Controls.Add(p);
            return p;
        }

        // Lebar kolom kiri; pembagian yang sama dipakai baris atas dan baris bawah.
        private static int LeftColumnWidth(int pageWidth)
        {
            return (int)((pageWidth - Gap) * (long)ColLeftNum / ColLeftDen);
        }

        // Jarak tepi atas kotak teks ke garis dasar, untuk menyejajarkan dua ukuran teks.
        private static int Ascent(Font font)
        {
            FontFamily family = font.FontFamily;
            float textH = font.GetHeight();
            return (int)Math.Round(textH * family.GetCellAscent(font.Style) /
                                   family.GetLineSpacing(font.Style));
        }

        private static int LabelWidth(string text, float size, FontStyle style)
        {
            using (Font f = UI.Body(size, style))
            {
                return TextRenderer.MeasureText(text, f).Width;
            }
        }

        // Pembulatan skor sama dengan FrmHasilAsesmen.
        private static string ScoreText(double value)
        {
            return Math.Round(value, MidpointRounding.AwayFromZero).ToString("0");
        }

        private RoundButton MakeButton(Control parent, string text, int x, int y, int w, int h,
                                       Color fill, Color hover, Color fore, Color border,
                                       int radius, float size)
        {
            RoundButton b = new RoundButton();
            b.Text = text;
            b.Bounds = new Rectangle(x, y, w, h);
            b.Fill = fill;
            b.HoverFill = hover;
            b.TextColor = fore;
            b.BorderColor = border;
            b.Radius = radius;
            b.Font = UI.Body(size, FontStyle.Bold);
            parent.Controls.Add(b);
            return b;
        }

        private static void Stretch(Control child, Control parent, int left, int right)
        {
            parent.Resize += delegate (object s, EventArgs e)
            {
                child.SetBounds(left, child.Top, Math.Max(10, parent.Width - left - right), child.Height);
            };
        }

        private static void PinRight(Control child, Control parent, int right)
        {
            parent.Resize += delegate (object s, EventArgs e)
            {
                child.Left = parent.Width - right - child.Width;
            };
        }

        private void MakeClickable(Control c, EventHandler handler)
        {
            c.Cursor = Cursors.Hand;
            c.Click += handler;
            foreach (Control child in c.Controls)
            {
                MakeClickable(child, handler);
            }
        }

        private static Image LoadAsset(string baseName)
        {
            string[] exts = { ".jpg", ".jpeg", ".png" };
            string dir = Path.Combine(Application.StartupPath, "Assets");
            foreach (string ext in exts)
            {
                string file = Path.Combine(dir, baseName + ext);
                try
                {
                    if (File.Exists(file))
                    {
                        using (Image tmp = Image.FromFile(file))
                        {
                            return new Bitmap(tmp);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            return null;
        }

        private void BuildSidebar(Panel sidebar)
        {
            Panel edge = new Panel();
            edge.Dock = DockStyle.Right;
            edge.Width = 1;
            edge.BackColor = line;
            sidebar.Controls.Add(edge);

            RoundPanel logo = Card(sidebar, 28, 24, 40, 40, 10, Color.FromArgb(244, 247, 252), line);
            UI.AddLogo(logo);

            AddText(sidebar, "ClimateSafe", 80, 21, 170, 26, 13f, true, titleBlue);
            AddText(sidebar, "Siap hari ini, lebih aman nanti", 80, 48, 172, 16, 7.5f, false, muted);

            string[] glyphs = { Ico.Home, Ico.Edit, Ico.Star, Ico.CheckBox, Ico.History, Ico.Library };
            string[] names = { "Beranda", "Asesmen", "Rekomendasi", "Checklist", "Riwayat", "Edukasi" };

            for (int i = 0; i < names.Length; i++)
            {
                RoundPanel item = Card(sidebar, 20, 100 + i * 50, 215, 44, 10, Color.White, Color.Empty);
                item.Tag = names[i];

                AddSymbol(item, glyphs[i], 14, 9, 26, 26, 12f, ink);
                AddAligned(item, names[i], 50, 11, 150, 22, 10f, false, ink,
                    ContentAlignment.MiddleLeft);

                RoundPanel captured = item;
                MakeClickable(item, delegate (object sender, EventArgs e)
                {
                    string nama = (string)captured.Tag;
                    if (nama == "Asesmen")
                    {
                        Nav.ShowAssessment(this);
                    }
                    else if (nama == "Checklist")
                    {
                        Nav.ShowChecklist(this);
                    }
                    else if (nama == "Rekomendasi")
                    {
                        Nav.ShowRekomendasi(this);
                    }
                    else if (nama == "Riwayat")
                    {
                        Nav.ShowRiwayat(this);
                    }
                    else if (nama == "Edukasi")
                    {
                        Nav.ShowEdukasi(this);
                    }
                });
                menuItems.Add(item);
            }
            SetActiveMenu(menuItems[0]);

            BufferedPanel bottom = new BufferedPanel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 150;
            bottom.BackColor = Color.FromArgb(247, 249, 252);
            sidebar.Controls.Add(bottom);

            Panel topLine = new Panel();
            topLine.Dock = DockStyle.Top;
            topLine.Height = 1;
            topLine.BackColor = line;
            bottom.Controls.Add(topLine);

            FocusCard user = new FocusCard();
            user.Bounds = new Rectangle(16, 16, 223, 57);
            user.Radius = 12;
            user.Fill = Color.White;
            user.BorderColor = line;
            user.SelectColor = navy;
            bottom.Controls.Add(user);

            RoundPanel avatar = Card(user, 8, 9, 38, 38, 19, coralSoft, Color.Empty);
            sidebarInitials = AddAligned(avatar, Session.Initials, 0, 0, 38, 38, 8.5f, true, coral, ContentAlignment.MiddleCenter);

            sidebarName = AddText(user, Session.DisplayName, 55, 10, 140, 18, 8.5f, true, ink);

            if (Session.IsGuest)
            {
                // Catatan tamu 9,75pt tidak muat di pil 36x16, jadi dipakai teks biasa.
                // Dua baris diperbolehkan: kartu memanjang ke bawah, tombol Keluar tetap di y 85.
                int noteH;
                using (Font noteFont = UI.Body(9.75f, FontStyle.Regular))
                {
                    noteH = TextRenderer.MeasureText(Session.UserCardNote, noteFont,
                        new Size(133, 0), TextFormatFlags.WordBreak).Height;
                }
                int noteTop = noteH > 18 ? 24 : 30;
                if (noteH > 18) sidebarName.Top = 4;
                AddText(user, Session.UserCardNote, 55, noteTop, 133, noteH, 9.75f, false, muted);
                user.Height = Math.Min(69, Math.Max(57, noteTop + noteH + 4));
            }
            else
            {
                int badgeW = LabelWidth(Session.RoleText, 9.75f, FontStyle.Regular) + 16;
                RoundPanel badge = Card(user, 55, 29, badgeW, 20, 6,
                    Color.FromArgb(226, 235, 250), Color.Empty);
                AddAligned(badge, Session.RoleText, 0, 0, badgeW, 20, 9.75f, false, navy,
                    ContentAlignment.MiddleCenter);
            }

            AddChevron(user, 196, 20, muted);
            EventHandler openProfil = delegate (object sender, EventArgs e) { Nav.ShowProfil(this); };
            MakeClickable(user, openProfil);
            user.Activated += openProfil;

            // Tombol sekunder "Keluar": pola sama dengan sidebar panel admin.
            PillButton exit = new PillButton();
            exit.Text = Session.ExitLabel;
            exit.Radius = 10;
            exit.Fill = Color.White;
            exit.HoverFill = Theme.ChipHover;
            exit.TextColor = navy;
            exit.BorderColor = line;
            exit.FocusColor = navy;
            exit.Font = UI.Body(10f, FontStyle.Bold);
            exit.Bounds = new Rectangle(16, 85, 223, 40);
            bottom.Controls.Add(exit);
            exit.Click += delegate (object sender, EventArgs e) { Nav.Logout(this); };
        }

        private Label greetingLabel;
        private Label sidebarName;
        private Label sidebarInitials;

        // Kartu pengguna sidebar dibaca ulang agar nama baru dari halaman Profil tampil.
        private void RefreshUserCard()
        {
            if (sidebarName != null) sidebarName.Text = Session.DisplayName;
            if (sidebarInitials != null) sidebarInitials.Text = Session.Initials;
        }

        private void SetActiveMenu(RoundPanel active)
        {
            foreach (RoundPanel item in menuItems)
            {
                bool on = (item == active);
                item.Fill = on ? coral : Color.White;
                foreach (Control c in item.Controls)
                {
                    c.ForeColor = on ? Color.White : ink;
                }
                item.Invalidate();
            }
        }

        private void BuildHeader(Panel pageHost)
        {
            dashHeader = NewCard(pageHost, 0, 0, PageMaxW, 159, 18,
                Color.FromArgb(238, 243, 251), Color.FromArgb(253, 232, 222), 0f,
                Color.FromArgb(222, 230, 240));
            dashHeader.Decorate = DrawHeaderArt;

            greetingLabel = AddText(dashHeader, "Halo, " + Session.FirstName, 28, 36, 400, 42, 20f, true, titleBlue);
            AddText(dashHeader,
                "Yuk, pastikan kamu dan orang-orang di sekitarmu siap menghadapi bencana.",
                28, 80, 480, 50, 11f, false, muted);

            string tanggal;
            try
            {
                tanggal = DateTime.Now.ToString("dddd, d MMMM yyyy", new CultureInfo("id-ID"));
            }
            catch (Exception)
            {
                tanggal = DateTime.Now.ToString("dddd, d MMMM yyyy");
            }

            Color pillBorder = Color.FromArgb(226, 232, 240);

            int dateW = 34 + LabelWidth(tanggal, 9.75f, FontStyle.Regular) + 12;
            RoundPanel dateCard = Card(dashHeader, PageMaxW - 30 - 147 - 12 - dateW, 64, dateW, 32, 16,
                Color.White, pillBorder);
            AddSymbol(dateCard, Ico.Calendar, 10, 0, 20, 32, 10f, muted);
            AddAligned(dateCard, tanggal, 34, 0, dateW - 46, 32, 9.75f, false, ink,
                ContentAlignment.MiddleLeft);

            RoundPanel weatherCard = Card(dashHeader, PageMaxW - 30 - 147, 64, 147, 32, 16,
                Color.White, pillBorder);
            // Ikon cuaca datang dari WeatherService (karakter ☀ ☁ ☂ ❄), jadi label ini
            // tetap memakai Segoe UI Symbol, bukan font ikon aplikasi.
            weatherIcon = NewLabel(weatherCard, "?", 10, 0, 20, 32, 10f, FontStyle.Regular,
                Color.FromArgb(240, 170, 40), ContentAlignment.MiddleCenter, "Segoe UI Symbol");
            weatherLabel = AddAligned(weatherCard, "Memuat cuaca...", 34, 0, 110, 32, 9.75f, false, ink,
                ContentAlignment.MiddleLeft);
            weatherDetails = AddAligned(dashHeader, "Sumber: Open-Meteo", PageMaxW - 30 - 147, 98,
                147, 18, 7f, false, muted, ContentAlignment.MiddleCenter);

            dashHeader.Resize += delegate (object sender, EventArgs e)
            {
                weatherCard.Left = dashHeader.Width - 30 - weatherCard.Width;
                weatherDetails.Left = weatherCard.Left;
                dateCard.Left = weatherCard.Left - 12 - dateCard.Width;
            };
        }

        private void DrawHeaderArt(Graphics g, Rectangle r)
        {
            int w = r.Width;
            int h = r.Height;

            using (GraphicsPath glow = new GraphicsPath())
            {
                glow.AddEllipse(w - 560, -280, 800, 520);
                using (PathGradientBrush pg = new PathGradientBrush(glow))
                {
                    pg.CenterColor = Color.FromArgb(120, 253, 205, 185);
                    pg.SurroundColors = new Color[] { Color.FromArgb(0, 253, 205, 185) };
                    g.FillPath(pg, glow);
                }
            }

            PointF[] leftHills =
            {
                new PointF(0, h - 40),
                new PointF(w * 0.12f, h - 62),
                new PointF(w * 0.25f, h - 42),
                new PointF(w * 0.40f, h - 22),
                new PointF(w * 0.50f, h)
            };
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddCurve(leftHills);
                gp.AddLine((int)(w * 0.50f), h, 0, h);
                gp.CloseFigure();
                using (SolidBrush b = new SolidBrush(Color.FromArgb(150, 222, 229, 243)))
                {
                    g.FillPath(b, gp);
                }
            }

            PointF[] hill1 =
            {
                new PointF(w - 420, h),
                new PointF(w - 300, h - 18),
                new PointF(w - 170, h - 45),
                new PointF(w - 70, h - 80),
                new PointF(w, h - 96)
            };
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddCurve(hill1);
                gp.AddLine(w, h - 96, w, h);
                gp.AddLine(w, h, w - 420, h);
                gp.CloseFigure();
                using (SolidBrush b = new SolidBrush(Color.FromArgb(190, 160, 185, 218)))
                {
                    g.FillPath(b, gp);
                }
            }

            PointF[] hill2 =
            {
                new PointF(w - 260, h),
                new PointF(w - 150, h - 14),
                new PointF(w - 60, h - 40),
                new PointF(w, h - 52)
            };
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddCurve(hill2);
                gp.AddLine(w, h - 52, w, h);
                gp.AddLine(w, h, w - 260, h);
                gp.CloseFigure();
                using (SolidBrush b = new SolidBrush(Color.FromArgb(220, 96, 130, 180)))
                {
                    g.FillPath(b, gp);
                }
            }
        }

        private void BuildScoreCard(Panel pageHost)
        {
            RoundPanel card = NewCard(pageHost, 0, RowTopY, LeftColumnWidth(PageMaxW), RowTopH, 16,
                Color.White, Color.FromArgb(253, 236, 231), 45f, line);
            cardScore = card;

            AddText(card, "Skor Kesiapan Terakhir", 20, 22, 280, 30, 10.5f, true, ink);
            AddText(card, latestSession?.HazardName ?? "", 20, 52, 280, 18, 9.75f, false, muted);

            // Angka skor dan "/100" digambar langsung pada kartu, bukan lewat Label: tepi
            // kiri kotak label memotong kaki garis miring "/" sehingga terbaca seperti tanda
            // petik. Dengan TextRenderer + NoPadding, "/100" berjarak tepat 8 px dari tepi
            // kanan teks angka dan sebaris dasar dengannya untuk skor 1-3 digit.
            string scoreText = latestSession == null ? "\u2014" : ScoreText(latestSession.OverallScore);
            card.Decorate = delegate (Graphics g, Rectangle bounds)
            {
                using (Font scoreFont = UI.Body(38f, FontStyle.Bold))
                using (Font maxFont = UI.Body(12f, FontStyle.Regular))
                {
                    Size numSize = TextRenderer.MeasureText(g, scoreText, scoreFont,
                        new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                    int numTop = 84 + (56 - numSize.Height) / 2;
                    int maxTop = numTop + Ascent(scoreFont) - Ascent(maxFont);

                    TextRenderer.DrawText(g, scoreText, scoreFont, new Point(20, numTop), ink,
                        TextFormatFlags.NoPadding);
                    TextRenderer.DrawText(g, "/100", maxFont,
                        new Point(20 + numSize.Width + 8, maxTop), muted, TextFormatFlags.NoPadding);
                }
            };

            // Gaya pil level sama dengan FrmHasilAsesmen.
            string levelText = latestSession?.Level ?? "Belum ada asesmen";
            bool notReady = string.Equals(levelText, ScoringRules.NotReadyLevel,
                StringComparison.OrdinalIgnoreCase);
            // Lencana level sebaris dengan angka dan "/100"; lebarnya diukur LabelWidth
            // (bukan Measure NoPadding) supaya skor 1-3 digit tetap utuh.
            int pillX = 20 + LabelWidth(scoreText, 38f, FontStyle.Bold) + 8
                + LabelWidth("/100", 12f, FontStyle.Regular) + 16;
            int pillW = LabelWidth(levelText, 9.75f, FontStyle.Bold) + 28;
            RoundPanel pill = Card(card, pillX, 98, pillW, 28, 14,
                notReady ? coralSoft : pillBlueBg, Color.Empty);
            AddAligned(pill, levelText, 0, 0, pillW, 28, 9.75f, true,
                notReady ? coralInk : pillBlueInk, ContentAlignment.MiddleCenter);

            Stretch(Divider(card, 20, 188, C3 - 40), card, 20, 20);
            AddText(card, "Terakhir diperbarui:", 20, 200, 150, 18, 9.75f, false, muted);
            Label date = AddAligned(card,
                latestSession?.CreatedAt.ToLocalTime().ToString("dd MMM yyyy") ?? "\u2014",
                C3 - 20 - 141, 200, 141, 18, 9.75f, false, ink, ContentAlignment.TopRight);
            PinRight(date, card, 20);
        }

        private void BuildQuickActions(Panel pageHost)
        {
            int cw = LeftColumnWidth(PageMaxW);
            int rw = PageMaxW - cw - Gap;
            RoundPanel card = Card(pageHost, cw + Gap, RowTopY, rw, RowTopH, 16, Color.White, line);
            cardQuick = card;

            IconBox(card, Ico.Bolt, 21, 22, 28, iconBlue, navy);
            AddAligned(card, "Aksi Cepat", 57, 22, 200, 22, 10.5f, true, ink, ContentAlignment.MiddleLeft);

            // Satu aksi utama di layar: gaya tombol utama (coral penuh, teks putih).
            RoundButton start = MakeButton(card, "Mulai Asesmen", 21, 66, rw - 42, 45,
                Theme.CtaBtnFill, Theme.CtaBtnHover, Theme.CtaBtnText, Color.Empty, 10, 10f);
            start.PressedFill = Theme.CtaBtnPressed;
            Stretch(start, card, 21, 21);
            start.Click += delegate (object sender, EventArgs e) { Nav.ShowAssessment(this); };

            AddActionItem(card, 127, Ico.Star, "Lihat Rekomendasi", "Dapatkan saran sesuai hasilmu",
                delegate (object sender, EventArgs e) { Nav.ShowRekomendasi(this); });

            Stretch(Divider(card, 21, 210, C3 - 42), card, 21, 21);
            AddText(card, "5 pertanyaan untuk tiap bahaya", 21, 219, 250, 18, 8.5f, false, muted);
        }

        private void AddActionItem(RoundPanel parent, int y, string glyph, string title,
                                   string description, EventHandler action)
        {
            RoundPanel box = Card(parent, 21, y, parent.Width - 42, 61, 10,
                Color.White, Color.FromArgb(222, 230, 242));
            Stretch(box, parent, 21, 21);

            IconBox(box, glyph, 12, 12, 36, Color.FromArgb(232, 240, 252), navy);
            AddText(box, title, 60, 13, 220, 20, 9.5f, true, ink);
            AddText(box, description, 60, 34, 225, 18, 8.5f, false, muted);
            Chevron chev = AddChevron(box, box.Width - 28, 22, muted);
            chev.BringToFront();
            PinRight(chev, box, 12);
            MakeClickable(box, action);
        }

        private void BuildChecklist(Panel pageHost)
        {
            int cw = LeftColumnWidth(PageMaxW);
            RoundPanel card = Card(pageHost, 0, RowBottomY, cw, RowBottomH, 16, Color.White, line);
            cardChecklist = card;

            IconBox(card, Ico.CheckBox, 20, 23, 28, iconBlue, navy);
            AddText(card, "Progress Checklist Persiapan", 62, 23, 360, 30, 10.5f, true, ink);
            AddText(card, "Lacak pemenuhan logistik & keselamatan keluarga", 62, 55, 440, 24, 9.75f,
                false, muted);

            Label seeAll = AddAligned(card, "Lihat Semua  \u203A", cw - 22 - 125, 28, 125, 20, 9.75f,
                false, Theme.CtaFill, ContentAlignment.TopRight);
            seeAll.Cursor = Cursors.Hand;
            seeAll.Click += delegate (object sender, EventArgs e)
            {
                Nav.ShowChecklist(this);
            };
            PinRight(seeAll, card, 22);

            // Isi kartu dua kolom: lingkaran progres di kiri, daftar tindakan di kanan.
            const int contentTop = 92;
            const int contentBottom = RowBottomH - 20;
            const int circle = 104;

            CircleProgress cp = new CircleProgress();
            cp.Bounds = new Rectangle(55, contentTop + (contentBottom - contentTop - circle) / 2,
                circle, circle);
            cp.Percent = latestSession == null ? 0 : (int)Math.Round(latestSession.Progress);
            cp.MainText = (latestSession?.CompletedCount ?? 0) + "/" + (latestSession?.Items.Count ?? 0);
            cp.SubText = "SELESAI";
            cp.Accent = blue;
            cp.TrackColor = Color.FromArgb(226, 234, 245);
            cp.InkColor = ink;
            cp.MutedColor = muted;
            card.Controls.Add(cp);

            int listX = 55 + circle + 24;
            Label hazardTitle = AddText(card, latestSession?.HazardName ?? "Belum ada asesmen",
                listX, contentTop, 380, 20, 9.75f, true, ink);
            Stretch(hazardTitle, card, listX, 20);

            // Tindakan diambil dari sesi yang sama dengan lingkaran (latestSession.Items yang
            // sudah dimuat), urutannya sama dengan halaman Checklist.
            int itemCount = latestSession == null ? 0 : latestSession.Items.Count;
            if (itemCount == 0)
            {
                Label empty = AddText(card, latestSession == null
                    ? "Mulai asesmen untuk membuat checklist persiapan."
                    : "Tidak ada tindakan yang dihasilkan untuk sesi ini.",
                    listX, contentTop + 30, 370, 65, 9.75f, false, muted);
                Stretch(empty, card, listX, 20);
                return;
            }

            int shown = Math.Min(5, itemCount);
            int listTop = contentTop + 30;
            int rowH = Math.Max(40, Math.Min(56, (contentBottom - listTop) / shown));
            int listY = listTop + Math.Max(0, (contentBottom - listTop - rowH * shown) / 2);

            for (int i = 0; i < shown; i++)
            {
                ChecklistItem item = latestSession.Items[i];

                BufferedPanel row = new BufferedPanel();
                row.Bounds = new Rectangle(listX, listY + i * rowH,
                    Math.Max(120, cw - listX - 20), rowH);
                row.BackColor = Color.White;
                card.Controls.Add(row);
                Stretch(row, card, listX, 20);

                // Hanya tampilan: TickBox dinonaktifkan supaya kliknya jatuh ke baris.
                TickBox tick = new TickBox();
                tick.Bounds = new Rectangle(0, (rowH - 18) / 2, 18, 18);
                tick.OnColor = blue;
                tick.OffBorder = line;
                tick.Checked = item.IsCompleted;
                tick.Enabled = false;
                row.Controls.Add(tick);

                Label title = AddAligned(row, item.Description, 30, (rowH - 20) / 2,
                    Math.Max(60, row.Width - 30), 20, 9.75f, false,
                    item.IsCompleted ? muted : ink, ContentAlignment.MiddleLeft);
                title.AutoEllipsis = true;
                Stretch(title, row, 30, 0);

                if (i < shown - 1)
                {
                    Stretch(Divider(row, 0, rowH - 1, row.Width), row, 0, 0);
                }

                MakeClickable(row, delegate (object sender, EventArgs e) { Nav.ShowChecklist(this); });
            }
        }

        private void BuildPriorities(Panel pageHost)
        {
            int cw = LeftColumnWidth(PageMaxW);
            int pw = PageMaxW - cw - Gap;
            RoundPanel card = Card(pageHost, cw + Gap, RowBottomY, pw, RowBottomH, 16, Color.White, line);
            cardPriority = card;

            // Kotak ikon dipusatkan vertikal pada blok judul (23..53) + subjudul (55..73),
            // dan glyph-nya digambar tepat di tengah kotak.
            RoundPanel warnBox = Card(card, 20, (23 + 73 - 28) / 2, 28, 28, 8, coralSoft, Color.Empty);
            warnBox.Decorate = delegate (Graphics g, Rectangle bounds)
            {
                DrawCentered(g, Ico.Warning, UI.IconFontName, 10.5f, FontStyle.Regular, coral, 28, 28);
            };
            AddText(card, "Prioritas Utama", 64, 23, 300, 30, 10.5f, true, ink);

            string prioritySubtitle = "Dari hasil asesmen terakhir, yang perlu kamu perhatikan.";
            AddText(card, prioritySubtitle, 64, 55,
                LabelWidth(prioritySubtitle, 9.75f, FontStyle.Regular), 18, 9.75f, false, muted);

            Stretch(Divider(card, 20, 88, pw - 40), card, 20, 20);

            var pendingItems = latestSession?.Items.Where(item => !item.IsCompleted).Take(3).ToList();
            if (pendingItems == null || pendingItems.Count == 0)
                AddText(card, latestSession == null ? "Selesaikan asesmen untuk melihat tindakan persiapan."
                    : "Tidak ada tindakan yang belum selesai pada sesi ini.", 24, 110, pw - 48, 70, 10, false, muted);
            else
                for (int i = 0; i < pendingItems.Count; i++)
                    AddPriority(card, 103 + i * 94, (i + 1).ToString(), pendingItems[i].Description);

            Stretch(Divider(card, 20, 398, pw - 40), card, 20, 20);

            Label seeAll = AddAligned(card, "Lihat Semua  \u203A", pw - 20 - 125, 408, 125, 18, 9.75f,
                false, Theme.CtaFill, ContentAlignment.TopRight);
            seeAll.Cursor = Cursors.Hand;
            seeAll.Click += delegate (object sender, EventArgs e)
            {
                Nav.ShowChecklist(this);
            };
            PinRight(seeAll, card, 20);
        }

        private void AddPriority(RoundPanel parent, int y, string number, string description)
        {
            const int boxH = 84;
            RoundPanel box = NewCard(parent, 20, y, parent.Width - 40, boxH, 10,
                Color.FromArgb(254, 240, 236), Color.Empty, 0f, Color.FromArgb(249, 214, 206));
            Stretch(box, parent, 20, 20);

            // Lingkaran putih bergaris coral; angka coral CTA (kontras 4,87:1) digambar
            // sendiri supaya benar-benar berada di tengah lingkaran.
            RoundPanel badge = Card(box, 16, (boxH - 24) / 2, 24, 24, 12, Color.White, Color.Empty);
            badge.Decorate = delegate (Graphics g, Rectangle bounds)
            {
                using (Pen pen = new Pen(Theme.Coral, 1.5f))
                {
                    g.DrawEllipse(pen, 0.75f, 0.75f, 22.5f, 22.5f);
                }
                DrawCentered(g, number, UI.FontName, 9.75f, FontStyle.Bold, Theme.CtaFill, 24, 24);
            };

            // Nama dimensi tidak ada pada data checklist yang dimuat Beranda, jadi teks
            // tindakan menjadi isi utama item.
            Label text = AddText(box, description, 57, 18, Math.Max(40, box.Width - 57 - 16), 20,
                9.75f, false, ink);
            Stretch(text, box, 57, 16);

            // Blok teks (satu atau dua baris) dipusatkan vertikal pada tinggi butir, diukur
            // dengan lebar pembungkus yang sedang dipakai label.
            EventHandler center = delegate (object sender, EventArgs e)
            {
                int h;
                using (Font f = UI.Body(9.75f, FontStyle.Regular))
                {
                    h = TextRenderer.MeasureText(description, f,
                        new Size(Math.Max(40, text.Width), 0), TextFormatFlags.WordBreak).Height;
                }
                text.SetBounds(text.Left, (boxH - h) / 2, text.Width, h);
            };
            box.Resize += center;
            center(box, EventArgs.Empty);
        }

        // Glyph atau angka pendek digambar tepat di tengah kotaknya.
        private static void DrawCentered(Graphics g, string text, string fontName, float size,
                                         FontStyle style, Color color, int w, int h)
        {
            using (Font f = UI.MakeFont(fontName, size, style))
            {
                TextRenderer.DrawText(g, text, f, new Rectangle(0, 0, w, h), color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPadding);
            }
        }

        private void BuildHazards(Panel pageHost)
        {
            IconBox(pageHost, Ico.Library, 0, HazardTitleY + 1, 28, iconBlue, navy);
            AddText(pageHost, "Informasi Bahaya Iklim", 41, HazardTitleY, 400, 24, 12f, true, titleBlue);
            AddText(pageHost, "Kenali jenis bahaya iklim dan langkah mitigasi terbaik.",
                41, HazardTitleY + 25, 450, 18, 9.75f, false, muted);

            // Nama bahaya dari satu sumber (FrmAssessment.hazardNames), deskripsi dari
            // materi Edukasi lewat API (EducationSummary), tujuan klik = halaman Edukasi.
            AddHazard(pageHost, 0, 0, FrmAssessment.hazardNames[0], "\U0001F30A", Ico.Rain,
                Color.FromArgb(112, 132, 146), Color.FromArgb(60, 78, 92), "banjir");

            AddHazard(pageHost, 1, C3 + Gap, FrmAssessment.hazardNames[1], "\u26C8", Ico.Storm,
                Color.FromArgb(92, 80, 120), Color.FromArgb(34, 30, 56), "cuaca");

            // Tanpa ikon kecil: lencana matahari hanya mengulang ikon besar pada kartu ini.
            AddHazard(pageHost, 2, 2 * (C3 + Gap), FrmAssessment.hazardNames[2], "\u2600", "",
                Color.FromArgb(120, 180, 235), Color.FromArgb(70, 120, 175), "panas");
        }

        private void AddHazard(Panel pageHost, int index, int x, string hazardName,
                               string emoji, string badgeGlyph, Color top, Color bottom,
                               string assetName)
        {
            RoundPanel card = Card(pageHost, x, HazardCardsY, C3, HazardCardH, 16, Color.White, line);
            card.ClipRegion = true;
            hazardCards[index] = card;

            CoverPicture pic = new CoverPicture();
            pic.Bounds = new Rectangle(1, 1, C3 - 2, HazardPicH);
            pic.EmojiSize = 26f;
            pic.BadgeSize = 22;
            pic.BadgeFontSize = 9.75f;
            pic.ColorTop = top;
            pic.ColorBottom = bottom;
            pic.Emoji = emoji;
            pic.BadgeGlyph = badgeGlyph;
            pic.BadgeColor = navy;
            pic.Photo = LoadAsset(assetName);
            card.Controls.Add(pic);
            Stretch(pic, card, 1, 1);

            AddText(card, hazardName, 16, 96, 300, 22, 10.5f, true, ink);

            for (int i = 0; i < 2; i++)
            {
                hazardSummary[index, i] = AddText(card, "", 16, 124 + i * 20, 285, 20, 9.75f, false, muted);
            }

            Stretch(Divider(card, 16, 182, C3 - 32), card, 16, 16);

            AddAligned(card, "Baca selengkapnya", 16, 194, 250, 20, 9.75f, true, navy,
                ContentAlignment.MiddleLeft);
            Chevron chev = AddChevron(card, C3 - 16 - 16, 196, navy);
            chev.BringToFront();
            PinRight(chev, card, 16);

            string captured = hazardName;
            MakeClickable(card, delegate (object sender, EventArgs e)
            {
                Nav.ShowEdukasi(this, captured);
            });
            AddCardHover(card, Color.White, Color.FromArgb(250, 252, 255));
        }

        // Latar kartu berubah saat kursor berada di atasnya, mengikuti warna hover baris
        // daftar di layar lain.
        private void AddCardHover(RoundPanel card, Color normal, Color hover)
        {
            EventHandler apply = delegate (object sender, EventArgs e)
            {
                bool over = card.ClientRectangle.Contains(card.PointToClient(Cursor.Position));
                card.Fill = over ? hover : normal;
                card.Invalidate();
            };
            HookHover(card, apply);
        }

        private void HookHover(Control c, EventHandler apply)
        {
            c.MouseEnter += apply;
            c.MouseLeave += apply;
            foreach (Control child in c.Controls)
            {
                HookHover(child, apply);
            }
        }

        // Memuat ringkasan materi dari API lalu memperbarui label di thread UI.
        private void RequestHazardSummaries()
        {
            RefreshHazardSummaries();
            EducationSummary.LoadAsync(delegate ()
            {
                if (IsDisposed || !IsHandleCreated) return;
                try
                {
                    BeginInvoke((Action)RefreshHazardSummaries);
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
            });
        }

        private void RefreshHazardSummaries()
        {
            if (IsDisposed) return;

            for (int index = 0; index < 3; index++)
            {
                string text;
                EducationState state = EducationSummary.GetState(FrmAssessment.hazardNames[index],
                    out text);
                List<string> lines = SummaryLines(EducationSummary.StateText(state, text), 285);

                for (int i = 0; i < 2; i++)
                {
                    Label label = hazardSummary[index, i];
                    if (label == null || label.IsDisposed) continue;
                    label.Text = i < lines.Count ? lines[i] : "";
                }
            }
        }

        // Deskripsi kartu bahaya: teks ringkasan dibungkus maksimal 2 baris dan
        // dipotong di batas kata.
        private static List<string> SummaryLines(string sentence, int maxWidth)
        {
            if (sentence.Length == 0) return new List<string>();

            using (Font f = UI.Body(9.75f, FontStyle.Regular))
            {
                List<string> lines = WrapText(sentence, f, maxWidth);
                if (lines.Count <= 2) return lines;

                return new List<string> { lines[0], Ellipsize(lines[1], f, maxWidth) };
            }
        }

        private static string Ellipsize(string text, Font font, int maxWidth)
        {
            string value = text.TrimEnd();
            while (value.Length > 0 &&
                   TextRenderer.MeasureText(value + "...", font).Width > maxWidth)
            {
                int space = value.LastIndexOf(' ');
                if (space <= 0) break;
                value = value.Substring(0, space).TrimEnd();
            }
            return value + "...";
        }

        // Lebar diukur seperti Label menggambar (padding bawaan TextRenderer).
        private static List<string> WrapText(string text, Font font, int maxWidth)
        {
            List<string> lines = new List<string>();
            if (string.IsNullOrEmpty(text)) return lines;

            string[] words = text.Split(' ');
            string cur = "";
            foreach (string word in words)
            {
                string test = cur.Length == 0 ? word : cur + " " + word;
                if (TextRenderer.MeasureText(test, font).Width > maxWidth && cur.Length > 0)
                {
                    lines.Add(cur);
                    cur = word;
                }
                else
                {
                    cur = test;
                }
            }
            if (cur.Length > 0) lines.Add(cur);
            return lines;
        }

        private void BuildFooter(Panel pageHost)
        {
            dashFooter = Card(pageHost, 0, FooterY, PageMaxW, FooterH, 14, Color.White, line);

            NewLabel(dashFooter, "\u201C", 21, 16, 30, 40, 22f, FontStyle.Bold, coral,
                ContentAlignment.TopLeft, "Georgia");
            Label quote = NewLabel(dashFooter,
                "Kesadaran dan persiapan adalah bentuk nyata dari kepedulian terhadap masa depan.",
                55, 20, PageMaxW - 55 - 20, 24, 10.5f, FontStyle.Italic, ink,
                ContentAlignment.TopLeft, UI.FontName);
            Stretch(quote, dashFooter, 55, 20);
            AddText(dashFooter, "ClimateSafe", 55, 46, 200, 16, 8f, true, muted);
        }

        private void lblTagline_Click(object sender, EventArgs e)
        {
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Tombol Login berhasil diklik!", "ClimateSafe");
        }
    }
}
