using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace ClimateSafe
{
    // Tombol pil yang bisa difokus keyboard (Enter/Spasi). RoundButton tidak selectable,
    // jadi kontrol ini dipakai untuk tombol dan chip filter di halaman Riwayat.
    internal class PillButton : Control
    {
        public Color Fill = Color.White;
        public Color HoverFill = Color.White;
        public Color TextColor = Color.Black;
        public Color BorderColor = Color.Empty;
        public Color FocusColor = Color.FromArgb(37, 88, 150);
        public int Radius = 8;

        private bool hovered;

        public PillButton()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hovered = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key == Keys.Space || key == Keys.Enter) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                OnClick(EventArgs.Empty);
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
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
            if (Width < 4 || Height < 4) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using (GraphicsPath path = UI.RoundPath(rect, Radius))
            {
                using (SolidBrush b = new SolidBrush(hovered ? HoverFill : Fill))
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

            TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width, Height), TextColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding);

            if (Focused)
            {
                using (GraphicsPath ring = UI.RoundPath(new Rectangle(1, 1, Width - 3, Height - 3), Radius))
                using (Pen p = new Pen(FocusColor, 2f))
                {
                    g.DrawPath(p, ring);
                }
            }
        }
    }

    // Pita bawah kartu: sudut bawah mengikuti radius kartu agar lengkungan tidak tertutup.
    internal class BottomBand : Control
    {
        public Color Fill = Color.FromArgb(250, 252, 255);
        public Color LineColor = Color.FromArgb(226, 232, 240);
        public int Radius = 15;

        public BottomBand()
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
            if (Width < 8 || Height < 8) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = new GraphicsPath())
            {
                int d = Radius * 2;
                path.AddLine(0, 0, Width - 1, 0);
                path.AddArc(Width - 1 - d, Height - 1 - d, d, d, 0, 90);
                path.AddArc(0, Height - 1 - d, d, d, 90, 90);
                path.CloseFigure();
                using (SolidBrush b = new SolidBrush(Fill)) g.FillPath(b, path);
            }

            g.SmoothingMode = SmoothingMode.None;
            using (SolidBrush b = new SolidBrush(LineColor))
            {
                g.FillRectangle(b, 0, 0, Width, 1);
            }
        }
    }

    // Lingkaran 56 dengan ikon jam, dipakai pada state kosong Riwayat.
    internal class ClockBadge : Control
    {
        public Color Circle = Color.FromArgb(226, 236, 250);
        public Color IconColor = Color.FromArgb(37, 88, 150);

        public ClockBadge()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(56, 56);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (SolidBrush b = new SolidBrush(Circle))
            {
                g.FillEllipse(b, 0, 0, Width - 1, Height - 1);
            }

            float cx = Width / 2f;
            float cy = Height / 2f;
            float r = 11f;
            using (Pen p = new Pen(IconColor, 2f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                g.DrawEllipse(p, cx - r, cy - r, r * 2, r * 2);
                g.DrawLine(p, cx, cy, cx, cy - 6f);
                g.DrawLine(p, cx, cy, cx + 5f, cy + 2f);
            }
        }
    }

    // Satu baris riwayat: 4 kolom dan chevron, bisa diklik dan difokus (Enter/Spasi).
    internal class RiwayatRowView : Control
    {
        public const int RowHeight = 80;
        private const int PadX = 24;
        private const int ColHazard = 24;
        private const int ColScore = 264;
        private const int ColDiff = 444;
        private const int ColChecklist = 624;

        public int AssessmentId;
        public string HazardText = "";
        public string DateText = "";
        public string ScoreText = "";
        public string LevelText = "";
        public bool LevelNotReady;
        public int DeltaDirection;      // 1 naik, -1 turun
        public string DeltaText = "";   // teks di dalam pil; kosong berarti teks biasa
        public string DeltaPlain = "";  // teks biasa (Asesmen pertama / Tidak berubah)
        public string ChecklistText = "";
        public bool HasBar;
        public float Progress;
        public bool Open;

        public Color Ink = Color.FromArgb(30, 41, 59);
        public Color SubInk = Color.FromArgb(82, 102, 126);
        public Color Muted = Color.FromArgb(100, 116, 139);
        public Color TitleBlue = Color.FromArgb(34, 62, 108);
        public Color Navy = Color.FromArgb(37, 88, 150);
        public Color LineColor = Color.FromArgb(226, 232, 240);
        public Color HoverBack = Color.FromArgb(250, 252, 255);
        public Color PillBlueBg = Color.FromArgb(228, 238, 247);
        public Color PillBlueInk = Color.FromArgb(47, 95, 148);
        public Color PillCoralBg = Color.FromArgb(253, 232, 226);
        public Color PillCoralInk = Color.FromArgb(159, 64, 51);
        public Color BarTrack = Color.FromArgb(226, 234, 245);

        private bool hovered;

        public event EventHandler Activated;

        public RiwayatRowView()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            BackColor = Color.White;
            Cursor = Cursors.Hand;
            Height = RowHeight;
        }

        private void Raise()
        {
            if (Activated != null) Activated(this, EventArgs.Empty);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hovered = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Raise();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key == Keys.Space || key == Keys.Enter) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                Raise();
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        private static int TextW(string text, float size, FontStyle style)
        {
            using (Font f = UI.Body(size, style))
            {
                return TextRenderer.MeasureText(text, f,
                    new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
            }
        }

        private static void Draw(Graphics g, string text, float size, FontStyle style,
                                 Rectangle rect, Color color, TextFormatFlags flags)
        {
            using (Font f = UI.Body(size, style))
            {
                TextRenderer.DrawText(g, text, f, rect, color, flags | TextFormatFlags.NoPadding);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Width < 200 || Height < 40) return;

            Graphics g = e.Graphics;
            using (SolidBrush b = new SolidBrush(Open || hovered ? HoverBack : Color.White))
            {
                g.FillRectangle(b, 0, 0, Width, Height);
            }

            TextFormatFlags left = TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                                   TextFormatFlags.EndEllipsis;

            // Kolom 1: bahaya dan tanggal
            Draw(g, HazardText, 12f, FontStyle.Bold,
                new Rectangle(ColHazard, 18, ColScore - ColHazard - 16, 22), Ink, left);
            Draw(g, DateText, 9f, FontStyle.Regular,
                new Rectangle(ColHazard, 44, ColScore - ColHazard - 16, 18), SubInk, left);

            // Kolom 2: skor dan level
            int x = ColScore;
            int sw = TextW(ScoreText, 15f, FontStyle.Bold);
            Draw(g, ScoreText, 15f, FontStyle.Bold, new Rectangle(x, 0, sw + 4, Height), TitleBlue, left);
            x += sw + 2;
            int uw = TextW("/100", 10f, FontStyle.Regular);
            Draw(g, "/100", 10f, FontStyle.Regular, new Rectangle(x, 0, uw + 4, Height), Muted, left);
            x += uw + 10;
            if (!string.IsNullOrEmpty(LevelText))
            {
                DrawPill(g, x, LevelText,
                    LevelNotReady ? PillCoralBg : PillBlueBg,
                    LevelNotReady ? PillCoralInk : PillBlueInk, 0);
            }

            // Kolom 3: perubahan skor
            if (!string.IsNullOrEmpty(DeltaText))
            {
                bool up = DeltaDirection > 0;
                DrawPill(g, ColDiff, DeltaText,
                    up ? PillBlueBg : PillCoralBg,
                    up ? PillBlueInk : PillCoralInk, up ? 1 : -1);
            }
            else
            {
                Draw(g, DeltaPlain, 9f, FontStyle.Regular,
                    new Rectangle(ColDiff, 0, ColChecklist - ColDiff - 16, Height), Muted, left);
            }

            // Kolom 4: checklist
            int colW = Math.Max(140, Width - PadX - 32 - 16 - ColChecklist);
            if (HasBar)
            {
                Draw(g, ChecklistText, 9f, FontStyle.Regular,
                    new Rectangle(ColChecklist, 24, colW, 18), Muted, left);
                DrawBar(g, new Rectangle(ColChecklist, 50, 140, 6));
            }
            else
            {
                Draw(g, ChecklistText, 9f, FontStyle.Regular,
                    new Rectangle(ColChecklist, 0, colW, Height), Muted, left);
            }

            DrawChevron(g, Width - PadX - 18, (Height - 18) / 2, Open);

            g.SmoothingMode = SmoothingMode.None;
            if (!Open)
            {
                using (SolidBrush b = new SolidBrush(LineColor))
                {
                    g.FillRectangle(b, 0, Height - 1, Width, 1);
                }
            }

            if (Focused)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p = new Pen(Navy, 2f))
                {
                    g.DrawRectangle(p, 1, 1, Width - 3, Height - 3);
                }
            }
        }

        private int DrawPill(Graphics g, int x, string text, Color back, Color fore, int arrow)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int tw = TextW(text, 9f, FontStyle.Bold);
            int lead = arrow == 0 ? 0 : 13;
            int w = 9 + lead + tw + 9;
            int h = 22;
            int y = (Height - h) / 2;

            using (GraphicsPath path = UI.RoundPath(new Rectangle(x, y, w, h), h / 2))
            using (SolidBrush b = new SolidBrush(back))
            {
                g.FillPath(b, path);
            }

            int tx = x + 9;
            if (arrow != 0)
            {
                DrawTriangle(g, tx, Height / 2, arrow, fore);
                tx += lead;
            }
            Draw(g, text, 9f, FontStyle.Bold, new Rectangle(tx, y, tw + 4, h), fore,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            return w;
        }

        private static void DrawTriangle(Graphics g, int left, int cy, int dir, Color color)
        {
            PointF[] points = dir > 0
                ? new PointF[]
                  {
                      new PointF(left + 4f, cy - 3.5f),
                      new PointF(left + 8f, cy + 2.5f),
                      new PointF(left, cy + 2.5f)
                  }
                : new PointF[]
                  {
                      new PointF(left + 4f, cy + 3.5f),
                      new PointF(left + 8f, cy - 2.5f),
                      new PointF(left, cy - 2.5f)
                  };
            using (SolidBrush b = new SolidBrush(color))
            {
                g.FillPolygon(b, points);
            }
        }

        private void DrawBar(Graphics g, Rectangle rect)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int rad = rect.Height / 2;
            using (GraphicsPath track = UI.RoundPath(rect, rad))
            using (SolidBrush b = new SolidBrush(BarTrack))
            {
                g.FillPath(b, track);
            }

            float v = Math.Max(0f, Math.Min(1f, Progress));
            int w = (int)(rect.Width * v);
            if (w > 0)
            {
                w = Math.Max(w, rect.Height);
                using (GraphicsPath fill = UI.RoundPath(new Rectangle(rect.X, rect.Y, w, rect.Height), rad))
                using (SolidBrush b = new SolidBrush(Navy))
                {
                    g.FillPath(b, fill);
                }
            }
        }

        private void DrawChevron(Graphics g, int x, int y, bool up)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float cx = x + 9f;
            float cy = y + 9f;
            using (Pen p = new Pen(Muted, 2f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                p.LineJoin = LineJoin.Round;
                PointF[] points = up
                    ? new PointF[]
                      {
                          new PointF(cx - 5f, cy + 2.5f),
                          new PointF(cx, cy - 2.5f),
                          new PointF(cx + 5f, cy + 2.5f)
                      }
                    : new PointF[]
                      {
                          new PointF(cx - 5f, cy - 2.5f),
                          new PointF(cx, cy + 2.5f),
                          new PointF(cx + 5f, cy - 2.5f)
                      };
                g.DrawLines(p, points);
            }
        }
    }

    internal sealed class FrmRiwayat : Form
    {
        private readonly Color coral = Color.FromArgb(235, 122, 104);
        private readonly Color coralSoft = Color.FromArgb(253, 232, 226);
        private readonly Color coralInk = Color.FromArgb(159, 64, 51);
        private readonly Color barHigh = Color.FromArgb(217, 80, 63);
        private readonly Color navy = Color.FromArgb(37, 88, 150);
        private readonly Color btnBlue = Color.FromArgb(55, 108, 178);
        private readonly Color btnBlueHover = Color.FromArgb(44, 94, 164);
        private readonly Color titleBlue = Color.FromArgb(34, 62, 108);
        private readonly Color ink = Color.FromArgb(30, 41, 59);
        private readonly Color subInk = Color.FromArgb(82, 102, 126);
        private readonly Color muted = Color.FromArgb(100, 116, 139);
        private readonly Color bg = Color.FromArgb(238, 242, 248);
        private readonly Color line = Color.FromArgb(226, 232, 240);
        private readonly Color iconBlue = Color.FromArgb(226, 236, 250);
        private readonly Color bandBg = Color.FromArgb(250, 252, 255);
        private readonly Color pillBlueBg = Color.FromArgb(228, 238, 247);
        private readonly Color pillBlueInk = Color.FromArgb(47, 95, 148);
        private readonly Color barTrack = Color.FromArgb(226, 234, 245);
        private readonly Color chipHover = Color.FromArgb(246, 249, 253);

        private const int PageMaxW = 1120;
        private const int PageMinW = 960;
        private const int ContentTop = 104;
        private const int CardTop = 56;
        private const int HeadRows = 88;     // header kartu 52 + judul kolom 36
        private const string AllFilter = "Semua";
        // Aturan skor berasal dari ScoringRules.
        private const string NotReadyLevel = ScoringRules.NotReadyLevel;

        private readonly List<RoundPanel> menuItems = new List<RoundPanel>();
        private readonly ChecklistService checklistService;
        private readonly RecommendationRepository recommendationRepository;
        private readonly AssessmentHistoryRepository historyRepository;

        private readonly List<ChecklistSession> sessions = new List<ChecklistSession>();
        private readonly Dictionary<int, List<DimensionScore>> dimensionCache =
            new Dictionary<int, List<DimensionScore>>();
        private readonly Dictionary<int, IReadOnlyList<AssessmentHistoryAnswer>> answerCache =
            new Dictionary<int, IReadOnlyList<AssessmentHistoryAnswer>>();
        private readonly List<RiwayatRowView> rowViews = new List<RiwayatRowView>();

        private BufferedPanel riwScroll;
        private BufferedPanel riwPage;
        private BufferedPanel content;
        private RoundPanel listCard;
        private BufferedPanel detailPanel;
        private BottomBand cardFooter;
        private string filter = AllFilter;
        private int openId;
        private bool riwBuilt;

        public FrmRiwayat() : this(new ChecklistService(), new RecommendationRepository(),
            new AssessmentHistoryRepository())
        {
        }

        internal FrmRiwayat(ChecklistService checklistService,
            RecommendationRepository recommendationRepository, AssessmentHistoryRepository historyRepository)
        {
            this.checklistService = checklistService;
            this.recommendationRepository = recommendationRepository;
            this.historyRepository = historyRepository;
            BuildRiwayat();
            Nav.Register(this);
        }

        private static int CurrentUserId
        {
            get { return Session.CurrentUser == null ? 0 : Session.CurrentUser.UserId; }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && riwBuilt)
            {
                RefreshUserCard();
                LoadSessions();
            }
        }

        private void BuildRiwayat()
        {
            AutoScaleMode = AutoScaleMode.None;
            Text = "ClimateSafe - Riwayat Asesmen";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1440, 900);
            MinimumSize = new Size(1280, 720);
            WindowState = FormWindowState.Maximized;
            BackColor = bg;
            Font = UI.Body(9f, FontStyle.Regular);

            SuspendLayout();

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
            sc.AutoScrollMargin = new Size(0, 64);
            sc.BackColor = bg;
            sc.Margin = Padding.Empty;
            root.Controls.Add(sc, 1, 0);
            riwScroll = sc;

            BuildSidebar(sidebar);

            BufferedPanel pg = new BufferedPanel();
            pg.Size = new Size(PageMaxW, ContentTop + 520);
            pg.Location = new Point(32, 32);
            pg.BackColor = bg;
            sc.Controls.Add(pg);
            riwPage = pg;

            BuildTitleArea(riwPage);

            riwBuilt = true;
            sc.Resize += delegate (object sender, EventArgs e) { LayoutPage(); };

            ResumeLayout(true);
            LayoutPage();
            LoadSessions();
        }

        private void LayoutPage()
        {
            if (!riwBuilt || riwScroll == null || riwPage == null) return;

            int avail = riwScroll.ClientSize.Width - 64;
            int w = Math.Max(PageMinW, Math.Min(PageMaxW, avail));
            int x = Math.Max(32, (riwScroll.ClientSize.Width - w) / 2);

            riwPage.Left = x + riwScroll.AutoScrollPosition.X;
            riwPage.Width = w;
            if (content != null) content.Width = w;
        }

        // ---------- data ----------

        private void LoadSessions()
        {
            if (!riwBuilt || IsDisposed) return;

            try
            {
                IReadOnlyList<ChecklistSession> loaded = CurrentUserId == 0
                    ? new List<ChecklistSession>()
                    : checklistService.GetSessions(CurrentUserId);

                sessions.Clear();
                sessions.AddRange(loaded.OrderByDescending(s => s.CreatedAt)
                                        .ThenByDescending(s => s.AssessmentId));
                dimensionCache.Clear();
                answerCache.Clear();
                openId = 0;

                List<string> hazards = HazardFilters();
                if (filter != AllFilter && !hazards.Contains(filter)) filter = AllFilter;

                RebuildContent();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Riwayat belum dapat dimuat. " + ex.Message,
                    "Riwayat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                sessions.Clear();
                dimensionCache.Clear();
                answerCache.Clear();
                openId = 0;
                filter = AllFilter;
                if (content == null) RebuildContent();
            }
        }

        private List<string> HazardFilters()
        {
            List<string> names = new List<string>();
            foreach (ChecklistSession s in sessions)
            {
                string name = s.HazardName ?? "";
                if (name.Length > 0 && !names.Contains(name)) names.Add(name);
            }
            names.Sort(StringComparer.CurrentCulture);
            return names;
        }

        private List<ChecklistSession> Filtered()
        {
            if (filter == AllFilter) return new List<ChecklistSession>(sessions);
            return sessions.Where(s => string.Equals(s.HazardName, filter, StringComparison.Ordinal)).ToList();
        }

        private static int DisplayScore(double value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        // Pembanding: asesmen bahaya sama yang tanggalnya lebih awal, dari seluruh daftar.
        private int? Delta(ChecklistSession session)
        {
            ChecklistSession? previous = null;
            foreach (ChecklistSession other in sessions)
            {
                if (other.AssessmentId == session.AssessmentId) continue;
                if (!string.Equals(other.HazardName, session.HazardName, StringComparison.Ordinal)) continue;
                if (other.CreatedAt > session.CreatedAt) continue;
                if (other.CreatedAt == session.CreatedAt && other.AssessmentId > session.AssessmentId) continue;
                if (previous == null || other.CreatedAt > previous.CreatedAt ||
                    (other.CreatedAt == previous.CreatedAt && other.AssessmentId > previous.AssessmentId))
                {
                    previous = other;
                }
            }
            if (previous == null) return null;
            return DisplayScore(session.OverallScore) - DisplayScore(previous.OverallScore);
        }

        // ---------- render ----------

        private void RebuildContent()
        {
            rowViews.Clear();
            listCard = null;
            detailPanel = null;
            cardFooter = null;

            if (content != null)
            {
                riwPage.Controls.Remove(content);
                content.Dispose();
                content = null;
            }

            int w = Math.Max(PageMinW, riwPage.Width);

            BufferedPanel host = new BufferedPanel();
            host.BackColor = bg;
            host.Bounds = new Rectangle(0, ContentTop, w, 100);
            host.TabIndex = 1;
            riwPage.Controls.Add(host);
            content = host;

            if (sessions.Count == 0)
            {
                host.Height = BuildEmptyCard(host);
                riwPage.Height = ContentTop + host.Height + 64;
                return;
            }

            BuildChips(host);
            BuildCard(host);
            LayoutCard();
        }

        private void BuildChips(BufferedPanel host)
        {
            List<string> names = new List<string>();
            names.Add(AllFilter);
            names.AddRange(HazardFilters());

            int x = 0;
            for (int i = 0; i < names.Count; i++)
            {
                string name = names[i];
                bool on = name == filter;
                int chipW = Measure(name, 9.5f, on ? FontStyle.Bold : FontStyle.Regular) + 32;

                PillButton chip = new PillButton();
                chip.Text = name;
                chip.Bounds = new Rectangle(x, 0, chipW, 36);
                chip.Radius = 18;
                chip.Fill = on ? navy : Color.White;
                chip.HoverFill = on ? navy : chipHover;
                chip.TextColor = on ? Color.White : ink;
                chip.BorderColor = on ? Color.Empty : line;
                chip.FocusColor = navy;
                chip.Font = UI.Body(9.5f, on ? FontStyle.Bold : FontStyle.Regular);
                chip.TabIndex = i;
                host.Controls.Add(chip);

                string captured = name;
                chip.Click += delegate (object sender, EventArgs e) { SelectFilter(captured); };

                x += chipW + 8;
            }
        }

        private void SelectFilter(string name)
        {
            if (filter == name) return;
            filter = name;
            openId = 0;
            RebuildContent();
        }

        private void BuildCard(BufferedPanel host)
        {
            int w = host.Width;
            List<ChecklistSession> rows = Filtered();

            RoundPanel card = Card(host, 0, CardTop, w, HeadRows + 100, 16, Color.White, line);
            card.TabIndex = 100;
            Stretch(card, host, 0, 0);
            listCard = card;

            AddText(card, "Daftar asesmen", 24, 17, 240, 22, 11f, true, titleBlue);

            string countText = rows.Count + " asesmen";
            int countW = Measure(countText, 9f, FontStyle.Regular) + 12;
            Label count = AddAligned(card, countText, w - 24 - countW, 17, countW, 20, 9f, false, muted,
                ContentAlignment.MiddleRight);
            PinRight(count, card, 24);

            Panel headLine = new Panel();
            headLine.Bounds = new Rectangle(0, 51, w, 1);
            headLine.BackColor = line;
            card.Controls.Add(headLine);
            Stretch(headLine, card, 0, 0);

            BufferedPanel band = new BufferedPanel();
            band.Bounds = new Rectangle(1, 52, w - 2, 36);
            band.BackColor = bandBg;
            card.Controls.Add(band);
            Stretch(band, card, 1, 1);

            AddAligned(band, "Bahaya dan tanggal", 23, 0, 230, 36, 9f, true, muted, ContentAlignment.MiddleLeft);
            AddAligned(band, "Skor dan level", 263, 0, 170, 36, 9f, true, muted, ContentAlignment.MiddleLeft);
            AddAligned(band, "Perubahan skor", 443, 0, 170, 36, 9f, true, muted, ContentAlignment.MiddleLeft);
            AddAligned(band, "Checklist", 623, 0, 170, 36, 9f, true, muted, ContentAlignment.MiddleLeft);

            Panel bandLine = new Panel();
            bandLine.Bounds = new Rectangle(0, 87, w, 1);
            bandLine.BackColor = line;
            card.Controls.Add(bandLine);
            Stretch(bandLine, card, 0, 0);

            for (int i = 0; i < rows.Count; i++)
            {
                ChecklistSession s = rows[i];
                RiwayatRowView row = new RiwayatRowView();
                row.AssessmentId = s.AssessmentId;
                row.Ink = ink;
                row.SubInk = subInk;
                row.Muted = muted;
                row.TitleBlue = titleBlue;
                row.Navy = navy;
                row.LineColor = line;
                row.HoverBack = bandBg;
                row.PillBlueBg = pillBlueBg;
                row.PillBlueInk = pillBlueInk;
                row.PillCoralBg = coralSoft;
                row.PillCoralInk = coralInk;
                row.BarTrack = barTrack;

                row.HazardText = s.HazardName ?? "";
                DateTime local = s.CreatedAt.ToLocalTime();
                row.DateText = local.ToString("dd MMM yyyy") + " · " + local.ToString("HH:mm");
                row.ScoreText = DisplayScore(s.OverallScore).ToString("0");
                row.LevelText = s.Level ?? "";
                row.LevelNotReady = string.Equals(row.LevelText, NotReadyLevel,
                    StringComparison.OrdinalIgnoreCase);

                int? delta = Delta(s);
                if (delta == null)
                {
                    row.DeltaPlain = "Asesmen pertama";
                }
                else if (delta.Value == 0)
                {
                    row.DeltaPlain = "Tidak berubah";
                }
                else
                {
                    row.DeltaDirection = delta.Value > 0 ? 1 : -1;
                    row.DeltaText = Math.Abs(delta.Value) + " poin";
                }

                int total = s.Items.Count;
                if (total == 0)
                {
                    row.ChecklistText = "Tidak ada tindakan";
                    row.HasBar = false;
                }
                else
                {
                    row.ChecklistText = s.CompletedCount + " dari " + total + " tindakan";
                    row.HasBar = true;
                    row.Progress = (float)(s.Progress / 100.0);
                }

                row.Width = w;
                row.Top = HeadRows + i * RiwayatRowView.RowHeight;
                row.TabIndex = 110 + i * 2;
                card.Controls.Add(row);
                Stretch(row, card, 0, 0);

                int captured = s.AssessmentId;
                row.Activated += delegate (object sender, EventArgs e) { ToggleRow(captured); };
                rowViews.Add(row);
            }

            BottomBand footer = new BottomBand();
            footer.Fill = bandBg;
            footer.LineColor = line;
            footer.Radius = 15;
            footer.Bounds = new Rectangle(1, 0, w - 2, 51);
            card.Controls.Add(footer);
            Stretch(footer, card, 1, 1);
            cardFooter = footer;

            AddAligned(footer,
                "Perubahan skor dibandingkan dengan asesmen sebelumnya untuk bahaya yang sama.",
                23, 16, w - 48, 18, 9f, false, muted, ContentAlignment.TopLeft);
        }

        private void LayoutCard()
        {
            if (listCard == null || cardFooter == null) return;

            int y = HeadRows;
            foreach (RiwayatRowView row in rowViews)
            {
                row.Top = y;
                y += row.Height;
                if (detailPanel != null && row.AssessmentId == openId)
                {
                    detailPanel.Top = y;
                    y += detailPanel.Height;
                }
            }

            cardFooter.Top = y;
            y += cardFooter.Height;

            listCard.Height = y;
            content.Height = CardTop + y;
            riwPage.Height = ContentTop + content.Height + 64;
        }

        private void ToggleRow(int assessmentId)
        {
            if (openId != assessmentId)
            {
                if (!dimensionCache.ContainsKey(assessmentId))
                {
                    try
                    {
                        var scores = recommendationRepository.GetDimensionScores(CurrentUserId, assessmentId).ToList();
                        var answers = historyRepository.GetAnswers(CurrentUserId, assessmentId);
                        dimensionCache[assessmentId] = scores;
                        answerCache[assessmentId] = answers;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "Riwayat belum dapat dimuat. " + ex.Message,
                            "Riwayat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                openId = assessmentId;
            }
            else
            {
                openId = 0;
            }

            ApplyOpenState();
        }

        private void ApplyOpenState()
        {
            if (listCard == null) return;

            Point scroll = riwScroll.AutoScrollPosition;
            riwScroll.SuspendLayout();
            listCard.SuspendLayout();
            try
            {
                if (detailPanel != null)
                {
                    listCard.Controls.Remove(detailPanel);
                    detailPanel.Dispose();
                    detailPanel = null;
                }

                if (openId != 0) BuildDetail(listCard, openId);

                foreach (RiwayatRowView row in rowViews)
                {
                    row.Open = row.AssessmentId == openId;
                    row.Invalidate();
                }

                LayoutCard();
            }
            finally
            {
                listCard.ResumeLayout(true);
                riwScroll.ResumeLayout(true);
                riwScroll.AutoScrollPosition = new Point(-scroll.X, -scroll.Y);
            }
        }

        private void BuildDetail(RoundPanel card, int assessmentId)
        {
            List<DimensionScore> scores;
            if (!dimensionCache.TryGetValue(assessmentId, out scores)) scores = new List<DimensionScore>();

            int w = card.Width;
            BufferedPanel panel = new BufferedPanel();
            panel.BackColor = bandBg;
            panel.Bounds = new Rectangle(0, HeadRows, w, 100);
            foreach (RiwayatRowView row in rowViews)
            {
                if (row.AssessmentId == assessmentId) panel.TabIndex = row.TabIndex + 1;
            }
            card.Controls.Add(panel);
            Stretch(panel, card, 0, 0);
            detailPanel = panel;

            Panel topLine = new Panel();
            topLine.Bounds = new Rectangle(0, 0, w, 1);
            topLine.BackColor = line;
            panel.Controls.Add(topLine);
            Stretch(topLine, panel, 0, 0);

            int y = 16;
            if (scores.Count > 0)
            {
                AddText(panel, "Skor per dimensi", 24, y, 240, 18, 9f, true, muted);
                y += 18 + 16;

                for (int i = 0; i < scores.Count; i++)
                {
                    DimensionScore s = scores[i];
                    AddAligned(panel, s.DimensionName, 24, y, 200, 20, 10f, false, ink,
                        ContentAlignment.MiddleLeft);

                    TargetBar bar = new TargetBar();
                    bar.Bounds = new Rectangle(224, y + 3, 360, 14);
                    bar.Value = (float)(s.Score / 100.0);
                    bar.Target = (float)(s.PassingThreshold / 100.0);
                    bar.BarColor = s.Score < AssessmentRules.HighPriorityBelow ? barHigh : navy;
                    bar.TrackColor = barTrack;
                    bar.TargetColor = ink;
                    panel.Controls.Add(bar);

                    string scoreText = DisplayScore(s.Score).ToString("0") + "/100";
                    AddAligned(panel, scoreText, 604, y, 70, 20, 10f, true, titleBlue,
                        ContentAlignment.MiddleLeft);

                    y += 20;
                    if (i < scores.Count - 1) y += 12;
                }
                y += 20;
            }

            if (scores.Count > 0)
            {
                AddAligned(panel, "Garis hitam menandai target tiap dimensi saat ini.", 24, y, 320, 40, 9f, false, muted,
                    ContentAlignment.MiddleLeft);
            }

            y += 52;
            AddText(panel, "Jawaban asesmen", 24, y, 280, 24, 10f, true, titleBlue);
            y += 32;
            if (!answerCache.TryGetValue(assessmentId, out var answers) || answers.Count == 0)
            {
                AddText(panel, "Tidak ada jawaban tersimpan untuk sesi ini.", 24, y, 550, 30, 9f, false, muted);
                y += 40;
            }
            else
            {
                foreach (AssessmentHistoryAnswer answer in answers)
                {
                    AddText(panel, answer.DimensionName + " · Bobot " + answer.Score + "/3",
                        24, y, 650, 25, 9f, true, navy);
                    y += 27;
                    // Gunakan lebar minimum halaman agar teks tetap terbaca saat jendela diperkecil.
                    using Font font = UI.Body(9f, FontStyle.Regular);
                    string detail = answer.QuestionText + "\nJawaban: " + answer.SelectedAnswer;
                    int height = TextRenderer.MeasureText(detail, font, new Size(750, int.MaxValue),
                        TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height + 10;
                    Label label = AddText(panel, detail, 24, y, 750, height, 9f, false, ink);
                    label.UseMnemonic = false;
                    y += height + 16;
                }
            }

            int buttonW = Measure("Buka Checklist", 9.5f, FontStyle.Bold) + 48;
            PillButton open = new PillButton();
            open.Text = "Buka Checklist";
            open.Bounds = new Rectangle(w - 24 - buttonW, y, buttonW, 40);
            open.Radius = 8;
            open.Fill = Color.White;
            open.HoverFill = chipHover;
            open.TextColor = navy;
            open.BorderColor = line;
            open.FocusColor = navy;
            open.Font = UI.Body(9.5f, FontStyle.Bold);
            panel.Controls.Add(open);
            PinRight(open, panel, 24);

            int captured = assessmentId;
            open.Click += delegate (object sender, EventArgs e) { Nav.ShowChecklist(this, captured); };

            panel.Height = y + 40 + 24;
        }

        private int BuildEmptyCard(BufferedPanel host)
        {
            int w = host.Width;
            int h = 460;

            RoundPanel card = Card(host, 0, 0, w, h, 16, Color.White, line);
            Stretch(card, host, 0, 0);

            string message = "Belum ada riwayat asesmen. Selesaikan asesmen untuk mulai memantau perkembanganmu.";
            List<string> lines;
            using (Font f = UI.Body(12f, FontStyle.Bold))
            {
                lines = WrapLines(message, f, 448);
            }

            int blockH = 56 + 24 + lines.Count * 26 + 24 + 44;
            int top = Math.Max(24, (h - blockH) / 2);

            ClockBadge badge = new ClockBadge();
            badge.Circle = iconBlue;
            badge.IconColor = navy;
            badge.Bounds = new Rectangle((w - 56) / 2, top, 56, 56);
            card.Controls.Add(badge);
            CenterIn(badge, card);

            int ty = top + 56 + 24;
            for (int i = 0; i < lines.Count; i++)
            {
                Label lineLabel = AddAligned(card, lines[i], 0, ty + i * 26, w, 26, 12f, true, ink,
                    ContentAlignment.MiddleCenter);
                Stretch(lineLabel, card, 0, 0);
            }

            int buttonW = Measure("Mulai Asesmen", 9.5f, FontStyle.Bold) + 48;
            PillButton start = new PillButton();
            start.Text = "Mulai Asesmen";
            start.Bounds = new Rectangle((w - buttonW) / 2, ty + lines.Count * 26 + 24, buttonW, 44);
            start.Radius = 8;
            start.Fill = btnBlue;
            start.HoverFill = btnBlueHover;
            start.TextColor = Color.White;
            start.FocusColor = Color.White;
            start.Font = UI.Body(9.5f, FontStyle.Bold);
            card.Controls.Add(start);
            CenterIn(start, card);
            start.Click += delegate (object sender, EventArgs e) { Nav.ShowAssessment(this); };

            return h;
        }

        // ---------- helper ----------

        private static int Measure(string text, float size, FontStyle style)
        {
            using (Font f = UI.Body(size, style))
            {
                return TextRenderer.MeasureText(text, f,
                    new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
            }
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

        private RoundPanel Card(Control parent, int x, int y, int w, int h, int radius,
                                Color fill, Color border)
        {
            RoundPanel p = new RoundPanel();
            p.Bounds = new Rectangle(x, y, w, h);
            p.Radius = radius;
            p.Fill = fill;
            p.BorderColor = border;
            parent.Controls.Add(p);
            return p;
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

        private static void CenterIn(Control child, Control parent)
        {
            child.Left = Math.Max(0, (parent.Width - child.Width) / 2);
            parent.Resize += delegate (object s, EventArgs e)
            {
                child.Left = Math.Max(0, (parent.Width - child.Width) / 2);
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

        private static List<string> WrapLines(string text, Font font, int maxWidth)
        {
            List<string> lines = new List<string>();
            if (string.IsNullOrEmpty(text)) return lines;
            string[] words = text.Split(' ');
            string cur = "";
            foreach (string word in words)
            {
                string test = cur.Length == 0 ? word : cur + " " + word;
                int tw = TextRenderer.MeasureText(test, font,
                    new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
                if (tw > maxWidth && cur.Length > 0)
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

        // ---------- sidebar & header ----------

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

            string[] glyphs = { "⌂", "✎", "☆", "☑", "↺", "▤" };
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
                    if (nama == "Beranda")
                    {
                        Nav.ShowDashboard(this);
                    }
                    else if (nama == "Asesmen")
                    {
                        Nav.ShowAssessment(this);
                    }
                    else if (nama == "Rekomendasi")
                    {
                        Nav.ShowRekomendasi(this);
                    }
                    else if (nama == "Checklist")
                    {
                        Nav.ShowChecklist(this);
                    }
                    else if (nama == "Edukasi")
                    {
                        Nav.ShowEdukasi(this);
                    }
                    else if (nama != "Riwayat")
                    {
                        Info("Menu " + nama + " dibuka.");
                    }
                });
                menuItems.Add(item);
            }
            SetActiveMenu(menuItems[4]);

            BufferedPanel bottom = new BufferedPanel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 89;
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

            RoundPanel badge = Card(user, 55, 30, 36, 16, 4, Color.FromArgb(226, 235, 250), Color.Empty);
            AddAligned(badge, Session.RoleText, 0, 0, 36, 16, 7f, false, navy, ContentAlignment.MiddleCenter);

            AddChevron(user, 196, 20, muted);
            EventHandler openProfil = delegate (object sender, EventArgs e) { Nav.ShowProfil(this); };
            MakeClickable(user, openProfil);
            user.Activated += openProfil;
        }

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

        private void BuildTitleArea(Panel host)
        {
            Label crumb = AddText(host, "Beranda", 0, 5, 52, 16, 9f, false, muted);
            crumb.Cursor = Cursors.Hand;
            crumb.Click += delegate (object sender, EventArgs e) { Nav.ShowDashboard(this); };
            AddText(host, "/", 55, 5, 10, 16, 9f, false, muted);
            AddText(host, "Riwayat", 67, 5, 80, 16, 9f, true, ink);

            AddText(host, "Riwayat Asesmen", 0, 26, 600, 38, 19f, true, titleBlue);
            AddText(host, "Lihat hasil asesmen sebelumnya dan perubahan skor kesiapsiagaanmu dari waktu ke waktu.",
                0, 66, 800, 20, 9.5f, false, muted);

            int buttonW = Measure("Asesmen Baru", 9.5f, FontStyle.Bold) + 48;
            PillButton newAssessment = new PillButton();
            newAssessment.Text = "Asesmen Baru";
            newAssessment.Bounds = new Rectangle(host.Width - buttonW, 26, buttonW, 40);
            newAssessment.Radius = 8;
            newAssessment.Fill = btnBlue;
            newAssessment.HoverFill = btnBlueHover;
            newAssessment.TextColor = Color.White;
            newAssessment.FocusColor = Color.White;
            newAssessment.Font = UI.Body(9.5f, FontStyle.Bold);
            newAssessment.TabIndex = 0;
            host.Controls.Add(newAssessment);
            PinRight(newAssessment, host, 0);
            newAssessment.Click += delegate (object sender, EventArgs e) { Nav.ShowAssessment(this); };
        }
    }
}
