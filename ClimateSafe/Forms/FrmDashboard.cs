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
    internal static class UI
    {
        public static readonly string FontName = PickFontName();

        private static string PickFontName()
        {
            string[] candidates = { "Plus Jakarta Sans", "Inter", "Segoe UI" };
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
            return "Segoe UI";
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
                    using (Font f = UI.MakeFont("Segoe UI Emoji", 40f, FontStyle.Regular))
                    {
                        TextRenderer.DrawText(g, Emoji, f,
                            new Rectangle(0, 8, Width, Height - 16), Color.White,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                            TextFormatFlags.NoPadding);
                    }
                }
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle badge = new Rectangle(12, Height - 40, 28, 28);
            using (SolidBrush wb = new SolidBrush(Color.White))
            {
                g.FillEllipse(wb, badge);
            }
            if (!string.IsNullOrEmpty(BadgeGlyph))
            {
                using (Font bf = UI.MakeFont("Segoe UI Symbol", 10f, FontStyle.Regular))
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
        public int Radius = 8;
        private bool hover;

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
                using (SolidBrush b = new SolidBrush(hover ? HoverFill : Fill))
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

        public static void ShowAssessment(Form from)
        {
            if (assessment == null || assessment.IsDisposed)
            {
                assessment = new FrmAssessment();
            }
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
            if (Session.CurrentUser == null)
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
            if (Session.CurrentUser == null)
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

        public static void ShowEdukasi(Form from)
        {
            if (edukasi == null || edukasi.IsDisposed)
            {
                edukasi = new FrmEdukasi();
            }
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

        private static void Quit()
        {
            if (quitting) return;
            quitting = true;
            Application.Exit();
        }
    }

    public partial class FrmDashboard : Form
    {
        private readonly Color coral = Color.FromArgb(235, 122, 104);
        private readonly Color coralSoft = Color.FromArgb(253, 232, 226);
        private readonly Color navy = Color.FromArgb(37, 88, 150);
        private readonly Color blue = Color.FromArgb(66, 133, 205);
        private readonly Color btnBlue = Color.FromArgb(55, 108, 178);
        private readonly Color titleBlue = Color.FromArgb(34, 62, 108);
        private readonly Color ink = Color.FromArgb(30, 41, 59);
        private readonly Color muted = Color.FromArgb(100, 116, 139);
        private readonly Color bg = Color.FromArgb(238, 242, 248);
        private readonly Color line = Color.FromArgb(226, 232, 240);
        private readonly Color iconBlue = Color.FromArgb(226, 236, 250);
        private readonly Color taskDone = Color.FromArgb(246, 248, 251);

        private const int PageMaxW = 1127;
        private const int PageMinW = 960;
        private const int PageH = 1436;
        private const int Gap = 22;
        private const int C3 = 361;
        private const int ChecklistW = 647;

        private readonly List<RoundPanel> menuItems = new List<RoundPanel>();
        private readonly RoundPanel[] hazardCards = new RoundPanel[3];

        private Panel dashScroll;
        private Panel dashPage;
        private RoundPanel dashHeader;
        private RoundPanel cardScore;
        private RoundPanel cardQuick;
        private RoundPanel cardPromo;
        private RoundPanel cardChecklist;
        private RoundPanel cardPriority;
        private RoundPanel dashFooter;
        private bool dashBuilt;
        private ChecklistSession? latestSession;
        private readonly ChecklistService checklistService;

        public FrmDashboard() : this(new ChecklistService())
        {
        }

        internal FrmDashboard(ChecklistService checklistService)
        {
            this.checklistService = checklistService;
            InitializeComponent();
            BuildDashboard();
            Nav.Register(this);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && dashBuilt)
            {
                RefreshUserCard();
                if (greetingLabel != null) greetingLabel.Text = "Halo, " + Session.FirstName;
                RefreshSavedData();
            }
        }

        internal void RefreshSavedData()
        {
            try
            {
                latestSession = Session.CurrentUser == null ? null
                    : checklistService.GetSessions(Session.CurrentUser.UserId).FirstOrDefault();
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
                MessageBox.Show(this, "Data asesmen belum dapat dimuat. Periksa koneksi PostgreSQL.",
                    "Dashboard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { dashPage.ResumeLayout(true); }
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
            BuildPromoCard(dashPage);
            BuildChecklist(dashPage);
            BuildPriorities(dashPage);
            BuildHazards(dashPage);
            BuildFooter(dashPage);

            dashBuilt = true;
            sc.Resize += delegate (object sender, EventArgs e) { LayoutPage(); };

            ResumeLayout(true);
            LayoutPage();
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

            dashHeader.SetBounds(0, 0, w, 159);

            cardScore.SetBounds(0, 181, c3, 255);
            cardQuick.SetBounds(c3 + Gap, 181, c3, 255);
            cardPromo.SetBounds(lastX, 181, lastW, 255);

            int cw = (int)((w - Gap) * 647L / 1105L);
            cardChecklist.SetBounds(0, 458, cw, 522);
            cardPriority.SetBounds(cw + Gap, 458, w - cw - Gap, 443);

            hazardCards[0].SetBounds(0, 1059, c3, 274);
            hazardCards[1].SetBounds(c3 + Gap, 1059, c3, 274);
            hazardCards[2].SetBounds(lastX, 1059, lastW, 274);

            dashFooter.SetBounds(0, 1356, w, 80);
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
                color, ContentAlignment.MiddleCenter, "Segoe UI Symbol");
        }

        private Chevron AddChevron(Control parent, int x, int y, Color color)
        {
            Chevron c = new Chevron();
            c.LineColor = color;
            c.Bounds = new Rectangle(x, y, 16, 16);
            parent.Controls.Add(c);
            return c;
        }

        private Label AddEmoji(Control parent, string glyph, int x, int y, int w, int h,
                               float size, Color color)
        {
            return NewLabel(parent, glyph, x, y, w, h, size, FontStyle.Regular,
                color, ContentAlignment.MiddleCenter, "Segoe UI Emoji");
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

        // Lebar teks tombol banner (8,5pt bold), dipakai agar lebar mengikuti teks.
        private static int PromoButtonTextWidth(string text)
        {
            using (Font f = UI.Body(8.5f, FontStyle.Bold))
            {
                return TextRenderer.MeasureText(text, f,
                    new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
            }
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

        private void Info(string message)
        {
            MessageBox.Show(message, "ClimateSafe",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            string[] glyphs = { "\u2302", "\u270E", "\u2606", "\u2611", "\u21BA", "\u25A4" };
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
                    else if (nama != "Beranda")
                    {
                        Info("Menu " + nama + " dibuka.");
                    }
                });
                menuItems.Add(item);
            }
            SetActiveMenu(menuItems[0]);

            BufferedPanel bottom = new BufferedPanel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 138;
            bottom.BackColor = Color.FromArgb(247, 249, 252);
            sidebar.Controls.Add(bottom);

            Panel topLine = new Panel();
            topLine.Dock = DockStyle.Top;
            topLine.Height = 1;
            topLine.BackColor = line;
            bottom.Controls.Add(topLine);

            Label settingsIcon = AddSymbol(bottom, "\u2699", 24, 24, 24, 24, 12f, muted);
            Label settingsText = AddAligned(bottom, "Pengaturan", 56, 24, 150, 22, 10f, false,
                muted, ContentAlignment.MiddleLeft);
            EventHandler openSettings = delegate (object sender, EventArgs e)
            {
                Info("Menu Pengaturan dibuka.");
            };
            settingsIcon.Cursor = Cursors.Hand;
            settingsIcon.Click += openSettings;
            settingsText.Cursor = Cursors.Hand;
            settingsText.Click += openSettings;

            FocusCard user = new FocusCard();
            user.Bounds = new Rectangle(16, 65, 223, 57);
            user.Radius = 12;
            user.Fill = Color.White;
            user.BorderColor = line;
            user.SelectColor = navy;
            bottom.Controls.Add(user);

            RoundPanel avatar = Card(user, 8, 9, 38, 38, 19, coralSoft, Color.Empty);
            sidebarInitials = AddAligned(avatar, Session.Initials, 0, 0, 38, 38, 8.5f, true, coral, ContentAlignment.MiddleCenter);

            sidebarName = AddText(user, Session.DisplayName, 55, 10, 140, 18, 8.5f, true, ink);

            RoundPanel badge = Card(user, 55, 30, 36, 16, 4, Color.FromArgb(226, 235, 250), Color.Empty);
            AddAligned(badge, Session.RoleText, 0, 0, 36, 16, 7f, false, navy, ContentAlignment.MiddleCenter);

            AddChevron(user, 196, 20, muted);
            EventHandler openProfil = delegate (object sender, EventArgs e) { Nav.ShowProfil(this); };
            MakeClickable(user, openProfil);
            user.Activated += openProfil;
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

            RoundPanel dateCard = Card(dashHeader, 704, 64, 190, 32, 16, Color.White, pillBorder);
            AddSymbol(dateCard, "\u25A3", 10, 0, 20, 32, 10f, muted);
            AddAligned(dateCard, tanggal, 34, 0, 152, 32, 8.5f, false, ink, ContentAlignment.MiddleLeft);

            RoundPanel weatherCard = Card(dashHeader, 906, 64, 147, 32, 16, Color.White, pillBorder);
            AddSymbol(weatherCard, "\u2600", 10, 0, 20, 32, 10f, Color.FromArgb(240, 170, 40));
            AddAligned(weatherCard, "Yogyakarta 28\u00B0C", 34, 0, 110, 32, 8.5f, false, ink,
                ContentAlignment.MiddleLeft);

            RoundPanel bell = Card(dashHeader, 1065, 64, 32, 32, 16, Color.White, pillBorder);
            AddEmoji(bell, "\U0001F514", 0, 0, 32, 32, 10f, muted);
            MakeClickable(bell, delegate (object sender, EventArgs e)
            {
                Info("Tidak ada notifikasi baru.");
            });

            dashHeader.Resize += delegate (object sender, EventArgs e)
            {
                bell.Left = dashHeader.Width - 30 - bell.Width;
                weatherCard.Left = bell.Left - 12 - weatherCard.Width;
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
            RoundPanel card = NewCard(pageHost, 0, 181, C3, 255, 16,
                Color.White, Color.FromArgb(253, 236, 231), 45f, line);
            cardScore = card;

            AddText(card, "Skor Kesiapan Terakhir", 20, 22, 280, 30, 10.5f, true, ink);

            RoundPanel circle = Card(card, 25, 71, 54, 54, 27, coral, Color.Empty);
            AddSymbol(circle, "\u2714", 0, 0, 54, 54, 20f, Color.White);

            string scoreText = latestSession?.OverallScore.ToString("0.##") ?? "—";
            Label scoreBig = AddAligned(card, scoreText, 98, 54, 120, 56, 30f, true, ink, ContentAlignment.MiddleLeft);
            scoreBig.AutoSize = true;
            int scoreW = scoreBig.PreferredWidth;
            AddAligned(card, "/100", 98 + scoreW + 22, 74, 70, 38, 12f, false, muted, ContentAlignment.MiddleLeft);

            RoundPanel pill = Card(card, 100, 136, 145, 28, 12, coralSoft, Color.FromArgb(248, 205, 195));
            AddAligned(pill, latestSession?.Level ?? "Belum ada asesmen", 0, 0, 145, 24, 8.5f, true, coral,
                ContentAlignment.MiddleCenter);

            Stretch(Divider(card, 20, 206, C3 - 40), card, 20, 20);
            AddText(card, "Terakhir diperbarui:", 20, 218, 150, 18, 8.5f, false, muted);
            Label date = AddAligned(card, latestSession?.CreatedAt.ToLocalTime().ToString("dd MMM yyyy") ?? "—", C3 - 20 - 141, 218, 141, 18, 8.5f, false, ink,
                ContentAlignment.TopRight);
            PinRight(date, card, 20);
        }

        private void BuildQuickActions(Panel pageHost)
        {
            RoundPanel card = Card(pageHost, C3 + Gap, 181, C3, 255, 16, Color.White, line);
            cardQuick = card;

            IconBox(card, "\u26A1", 21, 22, 28, iconBlue, navy);
            AddAligned(card, "Aksi Cepat", 57, 22, 200, 22, 10.5f, true, ink, ContentAlignment.MiddleLeft);

            AddActionItem(card, 62, "\u270E", "Mulai Asesmen", "Cek kesiapan dan risiko bencana",
                delegate (object sender, EventArgs e) { Nav.ShowAssessment(this); });

            AddActionItem(card, 136, "\u2606", "Lihat Rekomendasi", "Dapatkan saran sesuai hasilmu",
                delegate (object sender, EventArgs e) { Nav.ShowRekomendasi(this); });

            Stretch(Divider(card, 21, 210, C3 - 42), card, 21, 21);
            AddText(card, "Estimasi asesmen: ~3 menit", 21, 219, 250, 18, 8.5f, false, muted);
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

        private void BuildPromoCard(Panel pageHost)
        {
            RoundPanel card = NewCard(pageHost, 2 * (C3 + Gap), 181, C3, 255, 16,
                Color.FromArgb(255, 244, 240), Color.FromArgb(252, 228, 220), 45f,
                Color.FromArgb(248, 215, 205));
            card.Decorate = DrawPromoArt;
            cardPromo = card;

            AddText(card, "Siap Hadapi Perubahan Iklim?", 21, 20, 165, 50, 12f, true, ink);
            AddText(card, "Langkah kecil hari ini membuat kita lebih siap menghadapi esok hari.",
                21, 78, 150, 62, 9f, false, muted);

            // Lebar diukur dari teks dengan padding yang sama seperti tombol sebelumnya
            // ("Mulai Asesmen  \u2192" pada lebar 145), posisi dan gaya tidak berubah.
            string promoText = "Buka Edukasi  \u2192";
            int promoPad = 145 - PromoButtonTextWidth("Mulai Asesmen  \u2192");
            int promoW = PromoButtonTextWidth(promoText) + promoPad;

            RoundButton start = MakeButton(card, promoText, 21, 200, promoW, 35,
                coral, Color.FromArgb(224, 106, 88), Color.White, Color.Empty, 8, 8.5f);
            start.Click += delegate (object sender, EventArgs e)
            {
                Nav.ShowEdukasi(this);
            };

            RoundPanel avatar = Card(card, C3 - 20 - 96, 32, 96, 96, 48,
                Color.FromArgb(255, 250, 247), Color.FromArgb(250, 222, 212));
            AddEmoji(avatar, "\U0001F477", 0, 0, 96, 96, 32f, ink);
            PinRight(avatar, card, 20);
        }

        private void DrawPromoArt(Graphics g, Rectangle r)
        {
            int w = r.Width;
            int h = r.Height;

            PointF[] wave =
            {
                new PointF(0, h - 78),
                new PointF(w * 0.25f, h - 92),
                new PointF(w * 0.50f, h - 72),
                new PointF(w * 0.75f, h - 60),
                new PointF(w, h - 82)
            };

            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddCurve(wave);
                gp.AddLine(w, h - 82, w, h);
                gp.AddLine(w, h, 0, h);
                gp.CloseFigure();
                using (SolidBrush b = new SolidBrush(Color.FromArgb(70, 140, 170, 215)))
                {
                    g.FillPath(b, gp);
                }
            }

            using (Pen p = new Pen(Color.FromArgb(170, 120, 165, 215), 1.5f))
            {
                g.DrawCurve(p, wave);
            }

            PointF[] wave2 = new PointF[wave.Length];
            for (int i = 0; i < wave.Length; i++)
            {
                wave2[i] = new PointF(wave[i].X, wave[i].Y + 9f);
            }
            using (Pen p = new Pen(Color.FromArgb(140, 120, 165, 215), 1.2f))
            {
                p.DashStyle = DashStyle.Dash;
                g.DrawCurve(p, wave2);
            }
        }

        private void BuildChecklist(Panel pageHost)
        {
            RoundPanel card = Card(pageHost, 0, 458, ChecklistW, 522, 16, Color.White, line);
            cardChecklist = card;

            IconBox(card, "\u2611", 20, 23, 28, iconBlue, navy);
            AddText(card, "Progress Checklist Persiapan", 62, 23, 360, 30, 10.5f, true, ink);
            AddText(card, "Lacak pemenuhan logistik & keselamatan keluarga", 62, 55, 440, 24, 8.5f, false, muted);

            Label seeAll = AddAligned(card, "Lihat Semua  \u203A", ChecklistW - 22 - 125, 28, 125, 20, 8.5f,
                false, coral, ContentAlignment.TopRight);
            seeAll.Cursor = Cursors.Hand;
            seeAll.Click += delegate (object sender, EventArgs e)
            {
                Nav.ShowChecklist(this);
            };
            PinRight(seeAll, card, 22);

            CircleProgress cp = new CircleProgress();
            cp.Bounds = new Rectangle(55, 109, 100, 100);
            cp.Percent = latestSession == null ? 0 : (int)Math.Round(latestSession.Progress);
            cp.MainText = (latestSession?.CompletedCount ?? 0) + "/" + (latestSession?.Items.Count ?? 0);
            cp.SubText = "SELESAI";
            cp.Accent = blue;
            cp.TrackColor = Color.FromArgb(226, 234, 245);
            cp.InkColor = ink;
            cp.MutedColor = muted;
            card.Controls.Add(cp);

            AddAligned(card, (latestSession?.Progress ?? 0).ToString("0.#") + "% tindakan selesai", 20, 222, 185, 30, 8.5f, false, muted,
                ContentAlignment.TopCenter);

            Label hazardTitle = AddText(card, latestSession?.HazardName ?? "Belum ada asesmen", 213, 108, 380, 28, 12, true, ink);
            Stretch(hazardTitle, card, 213, 20);
            Label checklistHint = AddText(card, latestSession == null ? "Mulai asesmen untuk membuat checklist persiapan."
                : latestSession.Items.Count == 0 ? "Tidak ada tindakan yang dihasilkan untuk sesi ini."
                : "Buka checklist untuk menandai tindakan yang sudah dikerjakan.",
                213, 148, 370, 65, 9, false, muted);
            Stretch(checklistHint, card, 213, 20);

            Stretch(Divider(card, 20, 269, ChecklistW - 40), card, 20, 20);
            AddText(card, "TUGAS SIAGA TERKINI", 20, 286, 300, 16, 7.5f, true, muted);

            var recentItems = latestSession?.Items.Take(3).ToList();
            if (recentItems != null)
                for (int i = 0; i < recentItems.Count; i++)
                {
                    ChecklistItem item = recentItems[i];
                    Label action = AddText(card, (item.IsCompleted ? "✓ " : "☐ ") + item.Description,
                        24, 318 + i * 62, ChecklistW - 48, 56, 9, false, item.IsCompleted ? muted : ink);
                    Stretch(action, card, 24, 24);
                }
        }

        private void AddBarRow(RoundPanel parent, int y, string label, string value,
                               float progress, Color color)
        {
            AddText(parent, label, 213, y, 200, 18, 8.5f, false, ink);
            Label val = AddAligned(parent, value, parent.Width - 20 - 200, y, 200, 18, 8.5f, false, muted,
                ContentAlignment.TopRight);
            PinRight(val, parent, 20);

            ThinBar bar = new ThinBar();
            bar.Bounds = new Rectangle(213, y + 24, parent.Width - 213 - 20, 6);
            bar.Value = progress;
            bar.BarColor = color;
            bar.TrackColor = Color.FromArgb(226, 234, 245);
            parent.Controls.Add(bar);
            Stretch(bar, parent, 213, 20);
        }

        private void AddTask(RoundPanel parent, int y, string text, string info, int kind, bool done)
        {
            int rw = parent.Width - 40;
            RoundPanel row = NewCard(parent, 20, y, rw, 38, 8,
                done ? taskDone : Color.White, Color.Empty, 0f,
                done ? line : Color.Empty);
            Stretch(row, parent, 20, 20);

            TickBox cb = new TickBox();
            cb.Location = new Point(13, 11);
            cb.OnColor = coral;
            cb.OffBorder = Color.FromArgb(203, 213, 225);
            cb.Checked = done;
            row.Controls.Add(cb);

            Label tx = AddAligned(row, text, 41, 8, 380, 22, 8.5f, false, ink, ContentAlignment.MiddleLeft);
            tx.Cursor = Cursors.Hand;

            Control right;
            if (kind == 1)
            {
                RoundPanel pill = Card(row, rw - 12 - 86, 8, 86, 22, 11, coralSoft,
                    Color.FromArgb(248, 205, 195));
                AddAligned(pill, info, 0, 0, 86, 22, 7.5f, true, coral, ContentAlignment.MiddleCenter);
                right = pill;
            }
            else if (kind == 2)
            {
                RoundPanel pill = Card(row, rw - 12 - 52, 8, 52, 22, 11,
                    Color.FromArgb(240, 243, 247), Color.Empty);
                AddAligned(pill, info, 0, 0, 52, 22, 7.5f, false, muted, ContentAlignment.MiddleCenter);
                right = pill;
            }
            else
            {
                right = AddAligned(row, info, rw - 12 - 120, 10, 120, 18, 8f, false, muted,
                    ContentAlignment.TopRight);
            }
            PinRight(right, row, 12);

            Font normal = UI.Body(8.5f, FontStyle.Regular);
            Font strike = UI.Body(8.5f, FontStyle.Strikeout);

            Action apply = delegate ()
            {
                tx.Font = cb.Checked ? strike : normal;
                tx.ForeColor = cb.Checked ? muted : ink;
                row.Fill = cb.Checked ? taskDone : Color.White;
                row.BorderColor = cb.Checked ? line : Color.Empty;
                row.Invalidate();
            };
            apply();
            cb.CheckedChanged += delegate (object sender, EventArgs e) { apply(); };

            EventHandler toggle = delegate (object sender, EventArgs e) { cb.Checked = !cb.Checked; };
            tx.Click += toggle;
            row.Click += toggle;
        }

        private void BuildPriorities(Panel pageHost)
        {
            int pw = PageMaxW - ChecklistW - Gap;
            RoundPanel card = Card(pageHost, ChecklistW + Gap, 458, pw, 443, 16, Color.White, line);
            cardPriority = card;

            IconBox(card, "\u26A0", 20, 31, 28, coralSoft, coral);
            AddText(card, "Prioritas Utama", 64, 23, 300, 30, 10.5f, true, ink);
            AddText(card, "Dari hasil asesmen terakhir, yang perlu Anda perhatikan:",
                64, 55, 300, 32, 8.5f, false, muted);

            Stretch(Divider(card, 20, 88, pw - 40), card, 20, 20);

            var pendingItems = latestSession?.Items.Where(item => !item.IsCompleted).Take(3).ToList();
            if (pendingItems == null || pendingItems.Count == 0)
                AddText(card, latestSession == null ? "Selesaikan asesmen untuk melihat tindakan persiapan."
                    : "Tidak ada tindakan yang belum selesai pada sesi ini.", 24, 110, pw - 48, 70, 10, false, muted);
            else
                for (int i = 0; i < pendingItems.Count; i++)
                    AddPriority(card, 103 + i * 94, (i + 1).ToString(), "Tindakan persiapan", pendingItems[i].Description);

            Stretch(Divider(card, 20, 398, pw - 40), card, 20, 20);
            AddText(card, "Dari checklist asesmen terakhir", 20, 408, 240, 18, 8f, false, muted);

            Label seeAll = AddAligned(card, "Lihat Semua  \u203A", pw - 20 - 125, 408, 125, 18, 8.5f, false, coral,
                ContentAlignment.TopRight);
            seeAll.Cursor = Cursors.Hand;
            seeAll.Click += delegate (object sender, EventArgs e)
            {
                Nav.ShowChecklist(this);
            };
            PinRight(seeAll, card, 20);
        }

        private void AddPriority(RoundPanel parent, int y, string number, string title, string description)
        {
            RoundPanel box = NewCard(parent, 20, y, parent.Width - 40, 84, 10,
                Color.FromArgb(254, 240, 236), Color.Empty, 0f, Color.FromArgb(249, 214, 206));
            Stretch(box, parent, 20, 20);

            RoundPanel badge = Card(box, 16, 20, 24, 24, 12, coral, Color.Empty);
            AddAligned(badge, number, 0, 0, 24, 24, 8.5f, true, Color.White, ContentAlignment.MiddleCenter);

            AddText(box, title, 57, 13, 330, 18, 9f, true, ink);
            AddText(box, description, 57, 34, 270, 40, 8f, false, muted);
        }

        private void BuildHazards(Panel pageHost)
        {
            IconBox(pageHost, "\u25A4", 0, 999, 28, iconBlue, navy);
            AddText(pageHost, "Informasi Bahaya Iklim", 41, 998, 400, 24, 12f, true, titleBlue);
            AddText(pageHost, "Kenali jenis bahaya iklim dan langkah mitigasi terbaik.",
                41, 1023, 450, 18, 8.5f, false, muted);

            AddHazard(pageHost, 0, 0, "Banjir & Rob",
                "Genangan air, luapan sungai, pasang rob laut, dan drainase lingkungan buruk.",
                "Panduan evakuasi", "\U0001F30A", "\u2248",
                Color.FromArgb(112, 132, 146), Color.FromArgb(60, 78, 92), "banjir",
                "Membuka panduan evakuasi banjir & rob.");

            AddHazard(pageHost, 1, C3 + Gap, "Cuaca Ekstrem",
                "Angin puting beliung, petir lebat, hujan es, dan badai tropis di pemukiman.",
                "Prosedur pelindung", "\u26C8", "\u2602",
                Color.FromArgb(92, 80, 120), Color.FromArgb(34, 30, 56), "cuaca",
                "Membuka prosedur pelindung cuaca ekstrem.");

            AddHazard(pageHost, 2, 2 * (C3 + Gap), "Suhu Panas Ekstrem",
                "Gelombang panas terik, ancaman dehidrasi, heat stroke, dan perlindungan termal.",
                "Tips hidrasi & rumah sejuk", "\u2600", "\u2600",
                Color.FromArgb(120, 180, 235), Color.FromArgb(70, 120, 175), "panas",
                "Membuka tips hidrasi & rumah sejuk.");
        }

        private void AddHazard(Panel pageHost, int index, int x, string title, string description,
                               string link, string emoji, string badgeGlyph, Color top, Color bottom,
                               string assetName, string message)
        {
            RoundPanel card = Card(pageHost, x, 1059, C3, 274, 16, Color.White, line);
            card.ClipRegion = true;
            hazardCards[index] = card;

            CoverPicture pic = new CoverPicture();
            pic.Bounds = new Rectangle(1, 1, C3 - 2, 127);
            pic.ColorTop = top;
            pic.ColorBottom = bottom;
            pic.Emoji = emoji;
            pic.BadgeGlyph = badgeGlyph;
            pic.BadgeColor = navy;
            pic.Photo = LoadAsset(assetName);
            card.Controls.Add(pic);
            Stretch(pic, card, 1, 1);

            AddText(card, title, 16, 143, 300, 22, 10.5f, true, ink);
            AddText(card, description, 16, 171, 285, 40, 9f, false, muted);

            Stretch(Divider(card, 16, 229, C3 - 32), card, 16, 16);

            AddAligned(card, link, 16, 241, 250, 20, 8.5f, true, navy, ContentAlignment.MiddleLeft);
            Chevron chev = AddChevron(card, C3 - 16 - 16, 243, navy);
            chev.BringToFront();
            PinRight(chev, card, 16);

            MakeClickable(card, delegate (object sender, EventArgs e) { Info(message); });
        }

        private void BuildFooter(Panel pageHost)
        {
            dashFooter = Card(pageHost, 0, 1356, PageMaxW, 80, 14, Color.White, line);

            NewLabel(dashFooter, "\u201C", 21, 16, 30, 40, 22f, FontStyle.Bold, coral,
                ContentAlignment.TopLeft, "Georgia");
            NewLabel(dashFooter,
                "Kesadaran dan persiapan adalah bentuk nyata dari kepedulian terhadap masa depan.",
                55, 20, 700, 24, 10.5f, FontStyle.Italic, ink, ContentAlignment.TopLeft, UI.FontName);
            AddText(dashFooter, "ClimateSafe", 55, 46, 200, 16, 8f, true, muted);

            int downloadX = PageMaxW - 20 - 153;
            int shareX = downloadX - 13 - 124;

            RoundButton share = MakeButton(dashFooter, "Bagikan Dasbor", shareX, 22, 124, 35,
                Color.White, Color.FromArgb(244, 247, 252), navy, Color.FromArgb(214, 224, 238), 8, 8f);
            share.Click += delegate (object sender, EventArgs e)
            {
                Info("Tautan dasbor disalin untuk dibagikan.");
            };

            RoundButton download = MakeButton(dashFooter, "Unduh Laporan (PDF)", downloadX, 22, 153, 35,
                btnBlue, Color.FromArgb(44, 94, 164), Color.White, Color.Empty, 8, 8f);
            download.Click += delegate (object sender, EventArgs e)
            {
                Info("Laporan PDF sedang disiapkan...");
            };

            PinRight(download, dashFooter, 20);
            dashFooter.Resize += delegate (object sender, EventArgs e)
            {
                share.Left = download.Left - 13 - share.Width;
            };
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
