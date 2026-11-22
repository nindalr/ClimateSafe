using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace ClimateSafe
{
    // Data hasil yang sudah dihitung di FrmAssessment; halaman ini hanya menampilkannya.
    internal sealed class HasilAsesmenData
    {
        public string HazardName = "";
        public int HazardId;
        public double OverallScore;
        public string Level = "";
        public DateTime AssessmentDate;
        public int GapCount;
        public IReadOnlyList<DimensionScore> Dimensions = new List<DimensionScore>();
        public IReadOnlyList<Recommendation> Recommendations = new List<Recommendation>();
    }

    // Cincin skor: diameter dan tebal diatur dari luar (CircleProgress memakai tebal tetap 9
    // dan menggambar teksnya sendiri, jadi tidak dipakai di sini).
    internal class ScoreRing : Control
    {
        public float Value;                 // 0..100, nilai asli (tidak dibulatkan)
        public int Thickness = 10;
        public Color Accent = Color.FromArgb(37, 88, 150);
        public Color TrackColor = Color.FromArgb(226, 234, 245);
        public string MainText = "";
        public string SubText = "";
        public Color InkColor = Color.FromArgb(34, 62, 108);
        public Color MutedColor = Color.FromArgb(100, 116, 139);

        public ScoreRing()
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
            if (Width < 40 || Height < 40) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(Thickness / 2 + 1, Thickness / 2 + 1,
                Width - Thickness - 3, Height - Thickness - 3);

            using (Pen track = new Pen(TrackColor, Thickness))
            {
                g.DrawEllipse(track, rect);
            }

            float sweep = 360f * Math.Max(0f, Math.Min(100f, Value)) / 100f;
            if (sweep > 0f)
            {
                using (Pen arc = new Pen(Accent, Thickness))
                {
                    arc.StartCap = LineCap.Round;
                    arc.EndCap = LineCap.Round;
                    g.DrawArc(arc, rect, -90f, sweep);
                }
            }

            int mainW, mainH;
            using (Font f = UI.Body(26f, FontStyle.Bold))
            {
                Size size = TextRenderer.MeasureText(MainText, f,
                    new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                mainW = size.Width;
                mainH = size.Height;
            }
            int subW;
            using (Font f = UI.Body(9f, FontStyle.Regular))
            {
                subW = TextRenderer.MeasureText(SubText, f,
                    new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
            }

            int total = mainW + 2 + subW;
            int x = (Width - total) / 2;
            int top = (Height - mainH) / 2;

            using (Font f = UI.Body(26f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, MainText, f, new Rectangle(x, top, mainW + 4, mainH),
                    InkColor, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPadding);
            }
            using (Font f = UI.Body(9f, FontStyle.Regular))
            {
                TextRenderer.DrawText(g, SubText, f,
                    new Rectangle(x + mainW + 2, top, subW + 4, mainH), MutedColor,
                    TextFormatFlags.Left | TextFormatFlags.Bottom | TextFormatFlags.NoPadding);
            }
        }
    }

    // Centang GDI+ polos (stepper, ringkasan, dan state tanpa rekomendasi).
    internal class CheckMark : Control
    {
        public Color MarkColor = Color.FromArgb(37, 88, 150);
        public Color Circle = Color.Empty;

        public CheckMark()
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

            if (Circle != Color.Empty)
            {
                using (SolidBrush b = new SolidBrush(Circle))
                {
                    g.FillEllipse(b, 0, 0, Width - 1, Height - 1);
                }
            }

            float cx = Width / 2f;
            float cy = Height / 2f;
            float unit = Math.Min(Width, Height) / 28f;
            using (Pen p = new Pen(MarkColor, 2f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                p.LineJoin = LineJoin.Round;
                g.DrawLines(p, new PointF[]
                {
                    new PointF(cx - 6f * unit, cy),
                    new PointF(cx - 1.5f * unit, cy + 4.5f * unit),
                    new PointF(cx + 6f * unit, cy - 5f * unit)
                });
            }
        }
    }

    internal sealed class FrmHasilAsesmen : Form
    {
        private readonly Color coral = Theme.Coral;
        private readonly Color coralHover = Theme.CoralHover;
        private readonly Color coralSoft = Theme.CoralSoft;
        private readonly Color coralInk = Theme.CoralInk;
        private readonly Color barHigh = Theme.BarHigh;
        private readonly Color navy = Theme.Navy;
        private readonly Color titleBlue = Theme.TitleBlue;
        private readonly Color ink = Theme.Ink;
        private readonly Color subInk = Theme.SubInk;
        private readonly Color muted = Theme.Muted;
        private readonly Color bg = Theme.Bg;
        private readonly Color line = Theme.Line;
        private readonly Color iconBlue = Theme.IconBlue;
        private readonly Color bandBg = Theme.BandBg;
        private readonly Color pillBlueBg = Theme.PillBlueBg;
        private readonly Color pillBlueInk = Theme.PillBlueInk;
        private readonly Color barTrack = Theme.BarTrack;
        private readonly Color chipHover = Theme.ChipHover;

        private const int PageMaxW = 1120;
        private const int PageMinW = 960;
        private const int ContentTop = 104;
        private const int StepH = 60;
        private const int SummaryH = 168;
        private const int LeftCardW = 460;
        private const int ColGap = 24;
        private const int HeadH = 52;
        private const int DimRowH = 64;
        // Aturan skor berasal dari ScoringRules.
        private const string NotReadyLevel = ScoringRules.NotReadyLevel;
        private const string HighPriority = ScoringRules.HighPriority;
        private const string MediumPriority = ScoringRules.MediumPriority;

        private readonly List<RoundPanel> menuItems = new List<RoundPanel>();

        private BufferedPanel hsScroll;
        private BufferedPanel hsPage;
        private BufferedPanel content;
        private Label subtitle;

        private Label sidebarName;
        private Label sidebarInitials;

        private HasilAsesmenData data = new HasilAsesmenData();
        private bool hsBuilt;

        public FrmHasilAsesmen()
        {
            BuildHasil();
            Nav.Register(this);
        }

        public void SetData(HasilAsesmenData value)
        {
            data = value ?? new HasilAsesmenData();
            if (hsBuilt) RebuildContent();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && hsBuilt)
            {
                RefreshUserCard();
                ActiveControl = null;
            }
        }

        private void BuildHasil()
        {
            AutoScaleMode = AutoScaleMode.None;
            Text = "ClimateSafe - Hasil Asesmen";
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
            sc.AutoScrollMargin = new Size(0, 32);
            sc.BackColor = bg;
            sc.Margin = Padding.Empty;
            root.Controls.Add(sc, 1, 0);
            hsScroll = sc;

            BuildSidebar(sidebar);

            BufferedPanel pg = new BufferedPanel();
            pg.Size = new Size(PageMaxW, ContentTop + 600);
            pg.Location = new Point(32, 32);
            pg.BackColor = bg;
            sc.Controls.Add(pg);
            hsPage = pg;

            BuildTitleArea(hsPage);

            hsBuilt = true;
            sc.Resize += delegate (object sender, EventArgs e) { LayoutPage(); };

            ResumeLayout(true);
            LayoutPage();
            RebuildContent();
        }

        private void LayoutPage()
        {
            if (!hsBuilt || hsScroll == null || hsPage == null) return;

            int avail = hsScroll.ClientSize.Width - 64;
            int w = Math.Max(PageMinW, Math.Min(PageMaxW, avail));
            int x = Math.Max(32, (hsScroll.ClientSize.Width - w) / 2);

            hsPage.Left = x + hsScroll.AutoScrollPosition.X;
            hsPage.Width = w;
            if (content != null) content.Width = w;
        }

        // ---------- render ----------

        private void RebuildContent()
        {
            if (!hsBuilt) return;

            hsPage.SuspendLayout();
            try
            {
                if (content != null)
                {
                    hsPage.Controls.Remove(content);
                    content.Dispose();
                    content = null;
                }

                subtitle.Text = "Asesmen " + data.HazardName + " selesai.";

                int w = Math.Max(PageMinW, hsPage.Width);

                BufferedPanel host = new BufferedPanel();
                host.BackColor = bg;
                host.Bounds = new Rectangle(0, ContentTop, w, 100);
                hsPage.Controls.Add(host);
                content = host;

                BuildStepper(host, 0);
                BuildSummary(host, StepH + 24);

                int cardsTop = StepH + 24 + SummaryH + 24;
                int leftH = BuildDimensionCard(host, cardsTop);
                int rightH = BuildRecommendationCard(host, cardsTop);
                int cardsH = Math.Max(leftH, rightH);

                int buttonsTop = cardsTop + cardsH + 24;
                BuildActions(host, buttonsTop);

                host.Height = buttonsTop + 45;
                hsPage.Height = ContentTop + host.Height + 32;
            }
            finally
            {
                hsPage.ResumeLayout(true);
            }
        }

        // Geometri mengikuti BuildStepper di FrmAssessment.
        private void BuildStepper(BufferedPanel host, int top)
        {
            int w = host.Width;
            RoundPanel card = Card(host, 0, top, w, StepH, 14, Color.White, line);
            Stretch(card, host, 0, 0);

            RoundPanel c1 = Card(card, 32, 16, 28, 28, 14, iconBlue, Color.FromArgb(200, 216, 240));
            AddCheck(c1, 0, 0, 28, navy);
            AddAligned(card, "1. Pilih Bahaya", 70, 19, 130, 22, 9f, false, ink,
                ContentAlignment.MiddleLeft);

            Panel line1 = new Panel();
            line1.Bounds = new Rectangle(189, 29, 165, 2);
            line1.BackColor = coral;
            card.Controls.Add(line1);

            RoundPanel c2 = Card(card, 381, 16, 28, 28, 14, iconBlue, Color.FromArgb(200, 216, 240));
            AddCheck(c2, 0, 0, 28, navy);
            AddAligned(card, "2. Jawab Pertanyaan", 420, 19, 200, 22, 9f, false, ink,
                ContentAlignment.MiddleLeft);

            Panel line2Bar = new Panel();
            line2Bar.Bounds = new Rectangle(585, 29, 165, 2);
            line2Bar.BackColor = coral;
            card.Controls.Add(line2Bar);

            RoundPanel c3 = Card(card, 778, 16, 28, 28, 14, coral, Color.Empty);
            AddAligned(c3, "3", 0, 0, 28, 28, 10f, true, Color.White, ContentAlignment.MiddleCenter);
            AddAligned(card, "3. Hasil Asesmen", 816, 19, 200, 22, 9f, true, titleBlue,
                ContentAlignment.MiddleLeft);
        }

        private void BuildSummary(BufferedPanel host, int top)
        {
            int w = host.Width;
            RoundPanel card = Card(host, 0, top, w, SummaryH, 16, Color.White, line);
            Stretch(card, host, 0, 0);

            bool low = data.OverallScore < AssessmentRules.HighPriorityBelow;
            ScoreRing ring = new ScoreRing();
            ring.Bounds = new Rectangle(24, 24, 120, 120);
            ring.Thickness = 10;
            ring.Value = (float)data.OverallScore;
            ring.Accent = low ? barHigh : navy;
            ring.TrackColor = barTrack;
            ring.MainText = RoundScore(data.OverallScore);
            ring.SubText = "/100";
            ring.InkColor = titleBlue;
            ring.MutedColor = muted;
            card.Controls.Add(ring);

            Panel divider = new Panel();
            divider.Bounds = new Rectangle(168, 36, 1, 96);
            divider.BackColor = line;
            card.Controls.Add(divider);

            int colLeft = 193;
            int colW = Math.Max(140, (w - colLeft - 24 - 48) / 3);

            // 1. Level kesiapan
            AddText(card, "Level kesiapan", colLeft, 36, colW, 16, 9f, true, muted);
            bool notReady = string.Equals(data.Level, NotReadyLevel, StringComparison.OrdinalIgnoreCase);
            int pillW = Measure(data.Level, 9f, FontStyle.Bold) + 28;
            RoundPanel levelPill = Card(card, colLeft, 62, pillW, 26, 13,
                notReady ? coralSoft : pillBlueBg, Color.Empty);
            AddAligned(levelPill, data.Level, 0, 0, pillW, 26, 9f, true,
                notReady ? coralInk : pillBlueInk, ContentAlignment.MiddleCenter);

            // 2. Dimensi di bawah target
            int col2 = colLeft + colW + ColGap;
            AddText(card, "Dimensi di bawah target", col2, 36, colW, 16, 9f, true, muted);
            AddText(card, data.GapCount + " dari 5", col2, 60, colW, 30, 18f, true, ink);

            // 3. Asesmen
            int col3 = col2 + colW + ColGap;
            AddText(card, "Asesmen", col3, 36, colW, 16, 9f, true, muted);
            Label hazardLabel = AddText(card, data.HazardName, col3, 62, colW, 20, 12f, true, ink);
            hazardLabel.AutoEllipsis = true;
            AddText(card, FormatDate(data.AssessmentDate), col3, 86, colW, 16, 9f, false, muted);
            AddCheck(card, col3, 110, 14, pillBlueInk);
            AddText(card, Session.IsGuest ? "Hasil hanya untuk sesi ini." : "Hasil tersimpan di Riwayat.", col3 + 20, 110, colW - 20, 16, 9f, false, muted);
        }

        private int BuildDimensionCard(BufferedPanel host, int top)
        {
            int w = LeftCardW;
            RoundPanel card = Card(host, 0, top, w, HeadH + 100, 16, Color.White, line);
            card.ClipRegion = true;

            AddAligned(card, "Skor per dimensi", 24, 0, 240, HeadH, 11f, true, titleBlue,
                ContentAlignment.MiddleLeft);

                        var targets = data.Dimensions.Select(d => d.PassingThreshold).Distinct().ToList();
            string targetText = targets.Count == 1
                ? "Target " + targets[0].ToString("0")
                : "Target tiap dimensi";
            int markerTextW = LabelWidth(targetText, 9f, FontStyle.Regular);
            Panel marker = new Panel();
            marker.Bounds = new Rectangle(w - 24 - markerTextW - 8 - 2, (HeadH - 12) / 2, 2, 12);
            marker.BackColor = muted;
            card.Controls.Add(marker);
            AddAligned(card, targetText, w - 24 - markerTextW, 0, markerTextW, HeadH, 9f, false, muted,
                ContentAlignment.MiddleLeft);

            Panel headLine = new Panel();
            headLine.Bounds = new Rectangle(0, HeadH, w, 1);
            headLine.BackColor = line;
            card.Controls.Add(headLine);

            int y = HeadH + 1;
            for (int i = 0; i < data.Dimensions.Count; i++)
            {
                DimensionScore dim = data.Dimensions[i];
                BufferedPanel row = new BufferedPanel();
                row.Bounds = new Rectangle(0, y, w, DimRowH);
                row.BackColor = Color.White;
                card.Controls.Add(row);

                int nameH = Math.Max(18, LineHeight(10f, FontStyle.Bold));

                string scoreText = RoundScore(dim.Score) + "/100";
                int scoreW = LabelWidth(scoreText, 10f, FontStyle.Bold);
                AddAligned(row, scoreText, w - 24 - scoreW, 10, scoreW, nameH, 10f, true, ink,
                    ContentAlignment.MiddleRight);

                int nameW = LabelWidth(dim.DimensionName, 10f, FontStyle.Bold);
                AddText(row, dim.DimensionName, 24, 10, nameW, nameH, 10f, true, ink);

                if (dim.Score < dim.PassingThreshold)
                {
                    bool high = dim.Score < AssessmentRules.HighPriorityBelow;
                    int tagW = Measure("Perlu ditingkatkan", 8f, FontStyle.Bold) + 16;
                    RoundPanel tag = Card(row, 24 + nameW + 8, 11, tagW, 20, 10,
                        high ? coralSoft : pillBlueBg, Color.Empty);
                    AddAligned(tag, "Perlu ditingkatkan", 0, 0, tagW, 20, 8f, true,
                        high ? coralInk : pillBlueInk, ContentAlignment.MiddleCenter);
                }

                TargetBar bar = new TargetBar();
                bar.Bounds = new Rectangle(24, 36, w - 48, 14);
                bar.Value = (float)(dim.Score / 100.0);
                bar.Target = (float)(dim.PassingThreshold / 100.0);
                bar.BarColor = dim.Score < AssessmentRules.HighPriorityBelow ? barHigh : navy;
                bar.TrackColor = barTrack;
                bar.TargetColor = muted;
                row.Controls.Add(bar);

                y += DimRowH;

                if (i < data.Dimensions.Count - 1)
                {
                    Panel sep = new Panel();
                    sep.Bounds = new Rectangle(0, y, w, 1);
                    sep.BackColor = line;
                    card.Controls.Add(sep);
                    y += 1;
                }
            }

            int footerH = BuildFooter(card, y, w, w - 48,
                "Skor hanya menggambarkan kondisi berdasarkan indikator asesmen aplikasi.");
            int total = y + footerH;
            card.Height = total;
            return total;
        }

        private int BuildRecommendationCard(BufferedPanel host, int top)
        {
            int x = LeftCardW + ColGap;
            int w = Math.Max(360, host.Width - x);

            RoundPanel card = Card(host, x, top, w, HeadH + 240, 16, Color.White, line);
            card.ClipRegion = true;
            Stretch(card, host, x, 0);

            AddAligned(card, "Rekomendasi persiapan", 24, 0, 300, HeadH, 11f, true, titleBlue,
                ContentAlignment.MiddleLeft);

            if (data.Recommendations.Count > 0)
            {
                string orderText = "Diurutkan dari prioritas tertinggi";
                int orderW = LabelWidth(orderText, 9f, FontStyle.Regular);
                Label order = AddAligned(card, orderText, w - 24 - orderW, 0, orderW, HeadH, 9f, false,
                    muted, ContentAlignment.MiddleRight);
                PinRight(order, card, 24);
            }

            Panel headLine = new Panel();
            headLine.Bounds = new Rectangle(0, HeadH, w, 1);
            headLine.BackColor = line;
            card.Controls.Add(headLine);
            Stretch(headLine, card, 0, 0);

            int y = HeadH + 1;

            if (data.Recommendations.Count == 0)
            {
                int bodyH = 240;
                BufferedPanel empty = new BufferedPanel();
                empty.Bounds = new Rectangle(0, y, w, bodyH);
                empty.BackColor = Color.White;
                card.Controls.Add(empty);
                Stretch(empty, card, 0, 0);

                CheckMark badge = new CheckMark();
                badge.Circle = pillBlueBg;
                badge.MarkColor = pillBlueInk;
                badge.Bounds = new Rectangle((w - 32) / 2, bodyH / 2 - 36, 32, 32);
                empty.Controls.Add(badge);
                CenterIn(badge, empty);

                Label text = AddAligned(empty, "Semua dimensi sudah memenuhi ambang kesiapan.",
                    24, bodyH / 2 + 8, w - 48, 20, 10f, false, subInk, ContentAlignment.MiddleCenter);
                Stretch(text, empty, 24, 24);

                y += bodyH;
            }
            else
            {
                Dictionary<int, string> dimensionNames = new Dictionary<int, string>();
                foreach (DimensionScore dim in data.Dimensions)
                {
                    dimensionNames[dim.DimensionId] = dim.DimensionName;
                }

                int textX = 24 + 56 + 14;
                int textW = Math.Max(160, w - textX - 24);
                // Dibungkus pada lebar kartu tersempit (halaman di PageMinW) supaya teks tetap
                // utuh saat jendela diperkecil; label sendiri tetap melebar mengikuti kartu.
                int wrapW = Math.Max(160, PageMinW - (LeftCardW + ColGap) - textX - 24);
                int nameH = Math.Max(18, LineHeight(10f, FontStyle.Bold));
                int descH = Math.Max(19, LineHeight(9.5f, FontStyle.Regular));

                for (int i = 0; i < data.Recommendations.Count; i++)
                {
                    Recommendation rec = data.Recommendations[i];

                    List<string> lines;
                    using (Font f = UI.Body(9.5f, FontStyle.Regular))
                    {
                        lines = WrapLines(rec.Description ?? "", f, wrapW);
                    }

                    int itemH = 16 + nameH + 4 + Math.Max(1, lines.Count) * descH + 16;

                    BufferedPanel item = new BufferedPanel();
                    item.Bounds = new Rectangle(0, y, w, itemH);
                    item.BackColor = Color.White;
                    card.Controls.Add(item);
                    Stretch(item, card, 0, 0);

                    string priority = rec.Priority ?? "";
                    bool high = string.Equals(priority, HighPriority, StringComparison.OrdinalIgnoreCase);
                    // "Sedang" dan nilai lain memakai gaya biru.
                    RoundPanel pill = Card(item, 24, 16, 56, 20, 10,
                        high ? coralSoft : pillBlueBg, Color.Empty);
                    AddAligned(pill, priority, 0, 0, 56, 20, 8f, true,
                        high ? coralInk : pillBlueInk, ContentAlignment.MiddleCenter);

                    int dimensionId = rec.RecommendationId - data.HazardId * 100;
                    string dimensionName;
                    if (!dimensionNames.TryGetValue(dimensionId, out dimensionName)) dimensionName = "";

                    Label nameLabel = AddText(item, dimensionName, textX, 16, textW, nameH, 10f, true, ink);
                    Stretch(nameLabel, item, textX, 24);

                    for (int k = 0; k < lines.Count; k++)
                    {
                        Label lineLabel = AddAligned(item, lines[k], textX,
                            16 + nameH + 4 + k * descH, textW, descH,
                            9.5f, false, subInk, ContentAlignment.MiddleLeft);
                        Stretch(lineLabel, item, textX, 24);
                    }

                    y += itemH;

                    if (i < data.Recommendations.Count - 1)
                    {
                        Panel sep = new Panel();
                        sep.Bounds = new Rectangle(0, y, w, 1);
                        sep.BackColor = line;
                        card.Controls.Add(sep);
                        Stretch(sep, card, 0, 0);
                        y += 1;
                    }
                }
            }

            int footerH = BuildFooter(card, y, w,
                PageMinW - (LeftCardW + ColGap) - 48,
                "Rekomendasi bersifat umum, bukan keputusan profesional untuk kondisi darurat tertentu.");
            int total = y + footerH;
            card.Height = total;
            return total;
        }

        private int BuildFooter(RoundPanel card, int top, int w, int wrapW, string text)
        {
            List<string> lines;
            int lineH = Math.Max(18, LineHeight(9f, FontStyle.Regular));
            using (Font f = UI.Body(9f, FontStyle.Regular))
            {
                lines = WrapLines(text, f, Math.Max(160, wrapW));
            }
            int h = 1 + 16 + Math.Max(1, lines.Count) * lineH + 16;

            BottomBand band = new BottomBand();
            band.Fill = bandBg;
            band.LineColor = line;
            band.Radius = 15;
            band.Bounds = new Rectangle(1, top, w - 2, h);
            card.Controls.Add(band);
            Stretch(band, card, 1, 1);

            for (int i = 0; i < lines.Count; i++)
            {
                Label lineLabel = AddText(band, lines[i], 23, 16 + i * lineH, w - 48, lineH, 9f, false, muted);
                Stretch(lineLabel, band, 23, 23);
            }

            return h;
        }

        private void BuildActions(BufferedPanel host, int top)
        {
            int w = host.Width;
            bool onlyHome = data.GapCount == 0;

            BufferedPanel bar = new BufferedPanel();
            bar.Bounds = new Rectangle(0, top, w, 45);
            bar.BackColor = bg;
            host.Controls.Add(bar);
            Stretch(bar, host, 0, 0);

            int homeW = Measure("Kembali ke Beranda", 10f, FontStyle.Bold) + 40;
            PillButton home = new PillButton();
            home.Text = "Kembali ke Beranda";
            home.Radius = 10;
            home.Font = UI.Body(10f, FontStyle.Bold);
            if (onlyHome)
            {
                home.Fill = coral;
                home.HoverFill = coralHover;
                home.TextColor = Color.White;
                home.FocusColor = Color.White;
                homeW = Measure("Kembali ke Beranda", 10f, FontStyle.Bold) + 48;
            }
            else
            {
                home.Fill = Color.White;
                home.HoverFill = chipHover;
                home.TextColor = navy;
                home.BorderColor = line;
                home.FocusColor = navy;
            }
            home.TabIndex = 0;
            home.Click += delegate (object sender, EventArgs e) { Nav.ShowDashboard(this); };
            bar.Controls.Add(home);

            if (onlyHome)
            {
                home.Bounds = new Rectangle(w - homeW, 0, homeW, 45);
                PinRight(home, bar, 0);
                return;
            }

            int nextW = Measure("Buka Checklist  →", 10f, FontStyle.Bold) + 48;
            PillButton next = new PillButton();
            next.Text = "Buka Checklist  →";
            next.Radius = 10;
            next.Fill = coral;
            next.HoverFill = coralHover;
            next.TextColor = Color.White;
            next.FocusColor = Color.White;
            next.Font = UI.Body(10f, FontStyle.Bold);
            next.Bounds = new Rectangle(w - nextW, 0, nextW, 45);
            next.TabIndex = 1;
            next.Click += delegate (object sender, EventArgs e) { Nav.ShowChecklist(this); };
            bar.Controls.Add(next);
            PinRight(next, bar, 0);

            home.Bounds = new Rectangle(w - nextW - 12 - homeW, 0, homeW, 45);
            PinRight(home, bar, nextW + 12);
        }

        // ---------- format & helper ----------

        private static string RoundScore(double value)
        {
            return Math.Round(value, MidpointRounding.AwayFromZero).ToString("0");
        }

        private static string FormatDate(DateTime value)
        {
            if (value == default(DateTime)) return "";
            DateTime when = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
            return when.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("id-ID"));
        }

        // Lebar kotak visual (pil dan tombol yang menggambar sendiri dengan NoPadding).
        private static int Measure(string text, float size, FontStyle style)
        {
            using (Font f = UI.Body(size, style))
            {
                return TextRenderer.MeasureText(text, f,
                    new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
            }
        }

        // Lebar dan tinggi baris untuk Label: diukur seperti Label menggambar (dengan padding
        // bawaan TextRenderer), sehingga teks tidak membungkus lalu terpotong.
        private static int LabelWidth(string text, Font font)
        {
            return TextRenderer.MeasureText(text, font).Width;
        }

        private static int LabelWidth(string text, float size, FontStyle style)
        {
            using (Font f = UI.Body(size, style)) return LabelWidth(text, f);
        }

        private static int LineHeight(Font font)
        {
            return TextRenderer.MeasureText("Hg", font).Height;
        }

        private static int LineHeight(float size, FontStyle style)
        {
            using (Font f = UI.Body(size, style)) return LineHeight(f);
        }

        private CheckMark AddCheck(Control parent, int x, int y, int size, Color color)
        {
            CheckMark mark = new CheckMark();
            mark.MarkColor = color;
            mark.Bounds = new Rectangle(x, y, size, size);
            parent.Controls.Add(mark);
            return mark;
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
                int tw = LabelWidth(test, font);
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

        private void RefreshUserCard()
        {
            if (sidebarName != null) sidebarName.Text = Session.DisplayName;
            if (sidebarInitials != null) sidebarInitials.Text = Session.Initials;
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
            SetActiveMenu(menuItems[1]);

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
            sidebarInitials = AddAligned(avatar, Session.Initials, 0, 0, 38, 38, 8.5f, true, coral,
                ContentAlignment.MiddleCenter);

            sidebarName = AddText(user, Session.DisplayName, 55, 10, 140, 18, 8.5f, true, ink);

            RoundPanel badge = Card(user, 55, 30, 36, 16, 4, Color.FromArgb(226, 235, 250), Color.Empty);
            AddAligned(badge, Session.RoleText, 0, 0, 36, 16, 7f, false, navy, ContentAlignment.MiddleCenter);

            AddChevron(user, 196, 20, muted);
            EventHandler openProfil = delegate (object sender, EventArgs e) { Nav.ShowProfil(this); };
            MakeClickable(user, openProfil);
            user.Activated += openProfil;

            // Tombol sekunder "Keluar": pola sama dengan sidebar panel admin.
            PillButton exit = new PillButton();
            exit.Text = "Keluar";
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
            AddText(host, "Asesmen", 67, 5, 80, 16, 9f, true, ink);

            AddText(host, "Hasil Asesmen", 0, 26, 600, 38, 19f, true, titleBlue);
            subtitle = AddText(host, "", 0, 66, 800, 20, 9.5f, false, subInk);
        }
    }
}
