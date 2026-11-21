using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClimateSafe
{
    // Ikon bahaya Banjir: tiga garis gelombang.
    internal class WaveIcon : Control
    {
        public Color LineColor = Color.SteelBlue;

        public WaveIcon()
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
            if (Width < 12 || Height < 12) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float w = Width;
            float gap = Height / 4f;
            float top = Height / 2f - gap;

            using (Pen p = new Pen(LineColor, 1.8f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                for (int i = 0; i < 3; i++)
                {
                    float y = top + i * gap;
                    using (GraphicsPath path = new GraphicsPath())
                    {
                        path.AddBezier(w * 0.12f, y, w * 0.25f, y - gap * 0.6f,
                                       w * 0.38f, y + gap * 0.6f, w * 0.5f, y);
                        path.AddBezier(w * 0.5f, y, w * 0.62f, y - gap * 0.6f,
                                       w * 0.75f, y + gap * 0.6f, w * 0.88f, y);
                        g.DrawPath(p, path);
                    }
                }
            }
        }
    }

    public class FrmRekomendasi : Form
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
        private readonly Color muted = Color.FromArgb(100, 116, 139);
        private readonly Color bg = Color.FromArgb(238, 242, 248);
        private readonly Color line = Color.FromArgb(226, 232, 240);
        private readonly Color iconBlue = Color.FromArgb(226, 236, 250);
        private readonly Color bandBg = Color.FromArgb(250, 252, 255);
        private readonly Color pillBlueBg = Color.FromArgb(228, 238, 247);
        private readonly Color pillBlueInk = Color.FromArgb(47, 95, 148);
        private readonly Color barTrack = Color.FromArgb(226, 234, 245);

        private const int PageMaxW = 1120;
        private const int PageMinW = 960;
        private const int ContentTop = 104;


        private readonly List<RoundPanel> menuItems = new List<RoundPanel>();
        private readonly ChecklistService checklistService = new ChecklistService();
        private readonly RecommendationService recommendationService = new RecommendationService();
        private readonly RecommendationRepository recommendationRepository = new RecommendationRepository();

        private BufferedPanel recScroll;
        private BufferedPanel recPage;
        private BufferedPanel content;
        private bool recBuilt;

        public FrmRekomendasi()
        {
            BuildRekomendasi();
            Nav.Register(this);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && recBuilt)
            {
                RefreshUserCard();
                RefreshData();
            }
        }

        private void BuildRekomendasi()
        {
            AutoScaleMode = AutoScaleMode.None;
            Text = "ClimateSafe - Rekomendasi";
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
            sc.AutoScrollMargin = new Size(0, 40);
            sc.BackColor = bg;
            sc.Margin = Padding.Empty;
            root.Controls.Add(sc, 1, 0);
            recScroll = sc;

            BuildSidebar(sidebar);

            BufferedPanel pg = new BufferedPanel();
            pg.Size = new Size(PageMaxW, ContentTop + 520);
            pg.Location = new Point(32, 48);
            pg.BackColor = bg;
            sc.Controls.Add(pg);
            recPage = pg;

            BuildTitleArea(recPage);

            recBuilt = true;
            sc.Resize += delegate (object sender, EventArgs e) { LayoutPage(); };

            ResumeLayout(true);
            LayoutPage();
            RefreshData();
        }

        private void LayoutPage()
        {
            if (!recBuilt || recScroll == null || recPage == null) return;

            int avail = recScroll.ClientSize.Width - 64;
            int w = Math.Max(PageMinW, Math.Min(PageMaxW, avail));
            int x = Math.Max(32, (recScroll.ClientSize.Width - w) / 2);

            recPage.Left = x + recScroll.AutoScrollPosition.X;
            recPage.Width = w;
            if (content != null) content.Width = w;
        }

        // ---------- data ----------

        private void RefreshData()
        {
            if (!recBuilt) return;

            ChecklistSession? session = null;
            IReadOnlyList<DimensionScore> scores = new List<DimensionScore>();
            IReadOnlyList<Recommendation> recommendations = new List<Recommendation>();
            int hazardId = 0;

            recPage.SuspendLayout();
            try
            {
                if (Session.CurrentUser != null)
                {
                    session = checklistService.GetSessions(Session.CurrentUser.UserId).FirstOrDefault();
                    if (session != null)
                    {
                        scores = recommendationRepository.GetDimensionScores(
                            Session.CurrentUser.UserId, session.AssessmentId);

                        List<DimensionGap> gaps = new List<DimensionGap>();
                        foreach (DimensionScore s in scores)
                        {
                            hazardId = s.HazardId;
                            if (s.Score < s.PassingThreshold)
                            {
                                gaps.Add(new DimensionGap(s.DimensionId, s.DimensionName, s.Score));
                            }
                        }
                        if (gaps.Count > 0)
                        {
                            recommendations = recommendationService.GenerateRecommendations(
                                new Hazard(hazardId, session.HazardName), gaps);
                        }
                    }
                }

                RebuildContent(session, scores, recommendations, hazardId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Data rekomendasi belum dapat dimuat.\nDetail teknis: " +
                    TechnicalDetail(ex),
                    "Rekomendasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                try
                {
                    RebuildContent(null, new List<DimensionScore>(), new List<Recommendation>(), 0);
                }
                catch (Exception)
                {
                }
            }
            finally { recPage.ResumeLayout(true); }
        }

        private void RebuildContent(ChecklistSession? session, IReadOnlyList<DimensionScore> scores,
                                    IReadOnlyList<Recommendation> recommendations, int hazardId)
        {
            if (content != null)
            {
                recPage.Controls.Remove(content);
                content.Dispose();
                content = null;
            }

            int w = Math.Max(PageMinW, recPage.Width);

            BufferedPanel host = new BufferedPanel();
            host.BackColor = bg;
            host.Bounds = new Rectangle(0, ContentTop, w, 100);
            recPage.Controls.Add(host);
            content = host;

            int h;
            if (session == null)
            {
                h = BuildEmptyCard(host, "☰", "Belum ada rekomendasi",
                    "Selesaikan asesmen terlebih dahulu agar rekomendasi dapat disusun.",
                    "Mulai Asesmen",
                    delegate (object sender, EventArgs e) { Nav.ShowAssessment(this); },
                    null, null);
            }
            else if (recommendations.Count == 0)
            {
                h = BuildEmptyCard(host, "✔", "Semua dimensi sudah siap",
                    "Tidak ada kelemahan yang perlu ditindaklanjuti untuk asesmen ini.",
                    "Buka Checklist",
                    delegate (object sender, EventArgs e) { OpenChecklist(); },
                    "Lihat Riwayat",
                    delegate (object sender, EventArgs e) { Nav.ShowRiwayat(this); });
            }
            else
            {
                h = BuildBody(host, session, scores, recommendations, hazardId);
            }

            host.Height = h;
            recPage.Height = ContentTop + h + 48;
        }

        // ---------- isi utama ----------

        private int BuildBody(BufferedPanel host, ChecklistSession session,
                              IReadOnlyList<DimensionScore> scores,
                              IReadOnlyList<Recommendation> recommendations, int hazardId)
        {
            int w = host.Width;
            int y = 0;

            BuildSummary(host, session, y, w);
            y += 88 + 16;

            Dictionary<int, DimensionScore> byDimension = new Dictionary<int, DimensionScore>();
            foreach (DimensionScore s in scores) byDimension[s.DimensionId] = s;

            List<Recommendation> high = new List<Recommendation>();
            List<Recommendation> medium = new List<Recommendation>();
            foreach (Recommendation r in recommendations)
            {
                if (string.Equals(r.Priority, "Tinggi", StringComparison.OrdinalIgnoreCase)) high.Add(r);
                else medium.Add(r);
            }

            int listH = BuildList(host, y, w, byDimension, high, medium, hazardId);
            y += listH;

            List<DimensionScore> good = new List<DimensionScore>();
            foreach (DimensionScore s in scores)
            {
                if (s.Score >= s.PassingThreshold) good.Add(s);
            }
            if (good.Count > 0)
            {
                y += 16;
                List<string> parts = new List<string>();
                foreach (DimensionScore s in good)
                {
                    parts.Add(s.DimensionName + " (" + RoundScore(s.Score) + ")");
                }
                AddSymbol(host, "✓", 0, y - 2, 18, 20, 9.5f, navy);
                Label okText = AddText(host, "Sudah baik, tidak perlu tindakan: " +
                    string.Join(" · ", parts), 22, y, Math.Max(10, w - 22), 20, 9f, false, muted);
                Stretch(okText, host, 22, 0);
                y += 20;
            }

            y += 24;
            AddText(host, "Setiap rekomendasi otomatis menjadi item di Checklist.",
                0, y + 11, 420, 20, 9f, false, muted);

            RoundButton primary = MakeButton(host, "Buka Checklist  →", 0, y,
                ButtonWidth("Buka Checklist  →"), 40,
                btnBlue, btnBlueHover, Color.White, Color.Empty, 10, 9.5f);
            primary.Click += delegate (object sender, EventArgs e) { OpenChecklist(); };

            RoundButton secondary = MakeButton(host, "Asesmen Baru", 0, y,
                ButtonWidth("Asesmen Baru"), 40,
                Color.White, Color.FromArgb(246, 249, 253), ink, line, 10, 9.5f);
            secondary.Click += delegate (object sender, EventArgs e) { Nav.ShowAssessment(this); };

            primary.Left = w - primary.Width;
            secondary.Left = primary.Left - 12 - secondary.Width;
            PinRight(primary, host, 0);
            PinRight(secondary, host, 12 + primary.Width);

            y += 40;
            return y;
        }

        private void BuildSummary(BufferedPanel host, ChecklistSession session, int y, int w)
        {
            RoundPanel card = Card(host, 0, y, w, 88, 16, Color.White, line);
            Stretch(card, host, 0, 0);

            RoundPanel circle = Card(card, 24, 24, 40, 40, 20, iconBlue, Color.Empty);
            if (string.Equals(session.HazardName, "Banjir", StringComparison.OrdinalIgnoreCase))
            {
                WaveIcon wave = new WaveIcon();
                wave.LineColor = navy;
                wave.Bounds = new Rectangle(8, 8, 24, 24);
                circle.Controls.Add(wave);
            }
            else
            {
                AddSymbol(circle, HazardGlyph(session.HazardName), 0, 0, 40, 40, 12f, navy);
            }

            DateTime local = session.CreatedAt.ToLocalTime();
            string dateText = "Asesmen " + local.ToString("dd MMM yyyy") + " · " + local.ToString("HH:mm");

            int nameW = Measure(session.HazardName, 12f, FontStyle.Bold);
            int dateW = Measure(dateText, 8.5f, FontStyle.Regular);
            int textW = Math.Max(nameW, dateW);

            AddText(card, session.HazardName, 76, 22, nameW + 8, 24, 12f, true, ink);
            AddText(card, dateText, 76, 48, dateW + 8, 18, 8.5f, false, muted);

            int divX = 76 + textW + 24;
            Panel vline = new Panel();
            vline.Bounds = new Rectangle(divX, 24, 1, 40);
            vline.BackColor = line;
            card.Controls.Add(vline);

            string scoreText = RoundScore(session.OverallScore);
            int scoreW = Measure(scoreText, 24f, FontStyle.Bold);
            int unitW = Measure("/100", 9f, FontStyle.Regular);
            int scoreX = divX + 24;
            AddText(card, scoreText, scoreX, 20, scoreW + 10, 44, 24f, true, ink);
            AddText(card, "/100", scoreX + scoreW + 6, 44, unitW + 8, 20, 9f, false, muted);

            string level = session.Level ?? "";
            int levelW = Measure(level, 8.5f, FontStyle.Bold) + 28;
            RoundPanel badge = Card(card, scoreX + scoreW + 6 + unitW + 16, 32, levelW, 24, 12,
                iconBlue, Color.Empty);
            AddAligned(badge, level, 0, 0, levelW, 24, 8.5f, true, navy, ContentAlignment.MiddleCenter);

            string doneText = session.CompletedCount + " dari " + session.Items.Count + " tindakan selesai";
            int doneW = Measure(doneText, 9f, FontStyle.Regular) + 8;
            Label done = AddAligned(card, doneText, w - 24 - doneW, 34, doneW, 20, 9f, false, muted,
                ContentAlignment.MiddleRight);
            PinRight(done, card, 24);
        }

        private int BuildList(BufferedPanel host, int y, int w,
                              Dictionary<int, DimensionScore> byDimension,
                              List<Recommendation> high, List<Recommendation> medium, int hazardId)
        {
            RoundPanel card = Card(host, 0, y, w, 100, 16, Color.White, line);
            Stretch(card, host, 0, 0);

            AddText(card, "Tindakan yang disarankan", 24, 17, 320, 24, 12f, true, ink);
            string hint = "Skor dimensi · garis tegak = target dimensi";
            int hintW = Measure(hint, 8.5f, FontStyle.Regular) + 8;
            Label hintLabel = AddAligned(card, hint, w - 24 - hintW, 18, hintW, 20, 8.5f, false, muted,
                ContentAlignment.MiddleRight);
            PinRight(hintLabel, card, 24);

            int cy = 56;
            int number = 1;

            cy = AddGroup(card, cy, ref number, byDimension, high, hazardId, true);
            cy = AddGroup(card, cy, ref number, byDimension, medium, hazardId, false);

            card.Height = cy;
            return cy;
        }

        private int AddGroup(RoundPanel card, int cy, ref int number,
                             Dictionary<int, DimensionScore> byDimension,
                             List<Recommendation> items, int hazardId, bool isHigh)
        {
            if (items.Count == 0) return cy;

            Divider(card, cy);
            cy += 1;

            Panel band = new Panel();
            band.Bounds = new Rectangle(0, cy, card.Width, 40);
            band.BackColor = bandBg;
            card.Controls.Add(band);
            Stretch(band, card, 0, 0);

            string pillText = isHigh ? "Prioritas Tinggi" : "Prioritas Sedang";
            string rangeText = isHigh
                ? "· skor di bawah " + AssessmentRules.HighPriorityBelow.ToString("0")
                : "· skor " + AssessmentRules.HighPriorityBelow.ToString("0") +
                  " sampai di bawah target dimensi";

            int pillW = Measure(pillText, 8f, FontStyle.Bold) + 24;
            RoundPanel pill = Card(band, 24, 8, pillW, 24, 12,
                isHigh ? coralSoft : pillBlueBg, Color.Empty);
            AddAligned(pill, pillText, 0, 0, pillW, 24, 8f, true,
                isHigh ? coralInk : pillBlueInk, ContentAlignment.MiddleCenter);

            int rangeW = Measure(rangeText, 8.5f, FontStyle.Regular) + 12;
            AddAligned(band, rangeText, 24 + pillW + 10, 8, rangeW, 24, 8.5f, false, muted,
                ContentAlignment.MiddleLeft);

            cy += 40;

            foreach (Recommendation r in items)
            {
                Divider(card, cy);
                cy += 1;
                AddRow(card, cy, number, r, byDimension, hazardId, isHigh);
                number++;
                cy += 96;
            }
            return cy;
        }

        private void AddRow(RoundPanel card, int top, int number, Recommendation rec,
                            Dictionary<int, DimensionScore> byDimension, int hazardId, bool isHigh)
        {
            BufferedPanel row = new BufferedPanel();
            row.Bounds = new Rectangle(0, top, card.Width, 96);
            row.BackColor = Color.White;
            card.Controls.Add(row);
            Stretch(row, card, 0, 0);

            RoundPanel circle = Card(row, 24, 32, 32, 32, 16, bg, line);
            AddAligned(circle, number.ToString(), 0, 0, 32, 32, 9f, true, muted,
                ContentAlignment.MiddleCenter);

            int dimensionId = rec.RecommendationId - hazardId * 100;
            DimensionScore? score;
            if (!byDimension.TryGetValue(dimensionId, out score)) score = null;
            string dimensionName = score == null ? "" : score.DimensionName;
            double value = score == null ? 0 : score.Score;

            AddText(row, dimensionName, 76, 16, 520, 18, 8.5f, false, muted);

            List<string> lines;
            using (Font descFont = UI.Body(12f, FontStyle.Regular))
            {
                lines = WrapLines(rec.Description, descFont, 520);
            }
            for (int i = 0; i < 2 && i < lines.Count; i++)
            {
                AddText(row, lines[i], 76, 38 + i * 22, 520, 24, 12f, false, ink);
            }

            BufferedPanel block = new BufferedPanel();
            block.Bounds = new Rectangle(Math.Max(0, row.Width - 24 - 200), 0, 200, 96);
            block.BackColor = Color.White;
            row.Controls.Add(block);
            PinRight(block, row, 24);

            string scoreText = RoundScore(value);
            int scoreW = Measure(scoreText, 15f, FontStyle.Bold);
            int unitW = Measure("/100", 9f, FontStyle.Regular);
            int startX = 200 - (scoreW + 4 + unitW);
            AddText(block, scoreText, startX, 20, scoreW + 8, 28, 15f, true, ink);
            AddText(block, "/100", startX + scoreW + 4, 29, unitW + 8, 18, 9f, false, muted);

            TargetBar bar = new TargetBar();
            bar.Bounds = new Rectangle(200 - 160, 56, 160, 16);
            bar.Value = (float)(value / 100.0);
            bar.Target = (float)((score?.PassingThreshold ?? 0) / 100.0);
            bar.BarColor = isHigh ? barHigh : navy;
            bar.TrackColor = barTrack;
            bar.TargetColor = ink;
            block.Controls.Add(bar);
        }

        private int BuildEmptyCard(BufferedPanel host, string glyph, string title, string message,
                                   string primaryText, EventHandler primaryAction,
                                   string? linkText, EventHandler? linkAction)
        {
            int w = host.Width;
            int h = 460;

            RoundPanel card = Card(host, 0, 0, w, h, 16, Color.White, line);
            Stretch(card, host, 0, 0);

            RoundPanel circle = Card(card, (w - 56) / 2, 122, 56, 56, 28, iconBlue, Color.Empty);
            AddSymbol(circle, glyph, 0, 0, 56, 56, 15f, navy);
            CenterIn(circle, card);

            Label titleLabel = AddAligned(card, title, 0, 200, w, 30, 15f, true, titleBlue,
                ContentAlignment.TopCenter);
            Stretch(titleLabel, card, 0, 0);

            Label messageLabel = AddAligned(card, message, 0, 240, w, 24, 9.5f, false, muted,
                ContentAlignment.TopCenter);
            Stretch(messageLabel, card, 0, 0);

            BufferedPanel actions = new BufferedPanel();
            actions.BackColor = Color.White;
            actions.Height = 40;
            actions.Top = 296;
            card.Controls.Add(actions);

            RoundButton primary = MakeButton(actions, primaryText, 0, 0,
                ButtonWidth(primaryText), 40, btnBlue, btnBlueHover, Color.White, Color.Empty, 10, 9.5f);
            primary.Click += primaryAction;
            int total = primary.Width;

            if (linkText != null)
            {
                int linkW = Measure(linkText, 9.5f, FontStyle.Bold) + 24;
                Label link = AddAligned(actions, linkText, total + 12, 0, linkW, 40, 9.5f, true, navy,
                    ContentAlignment.MiddleCenter);
                link.Cursor = Cursors.Hand;
                if (linkAction != null) link.Click += linkAction;
                total += 12 + linkW;
            }

            actions.Width = total;
            CenterIn(actions, card);

            return h;
        }

        private void OpenChecklist()
        {
            Nav.ShowChecklist(this);
        }

        private string HazardGlyph(string hazardName)
        {
            if (string.Equals(hazardName, "Cuaca Ekstrem", StringComparison.OrdinalIgnoreCase)) return "☂";
            if (string.Equals(hazardName, "Suhu Panas Ekstrem", StringComparison.OrdinalIgnoreCase)) return "☀";
            return "≈";
        }

        private static string RoundScore(double value)
        {
            return Math.Round(value, MidpointRounding.AwayFromZero).ToString("0");
        }

        private int ButtonWidth(string text)
        {
            return Measure(text, 9.5f, FontStyle.Bold) + 48;
        }


        // Detail teknis error database (tanpa connection string atau kredensial).
        private static string TechnicalDetail(Exception ex)
        {
            Npgsql.PostgresException? pg = ex as Npgsql.PostgresException;
            if (pg != null) return pg.MessageText + " (SQLSTATE " + pg.SqlState + ")";
            return ex.Message;
        }

        private static int Measure(string text, float size, FontStyle style)
        {
            using (Font f = UI.Body(size, style))
            {
                return TextRenderer.MeasureText(text, f,
                    new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
            }
        }

        private Panel Divider(Control parent, int y)
        {
            Panel p = new Panel();
            p.Bounds = new Rectangle(0, y, parent.Width, 1);
            p.BackColor = line;
            parent.Controls.Add(p);
            Stretch(p, parent, 0, 0);
            return p;
        }

        // ---------- helper tampilan ----------

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
                    else if (nama == "Checklist")
                    {
                        OpenChecklist();
                    }
                    else if (nama == "Riwayat")
                    {
                        Nav.ShowRiwayat(this);
                    }
                    else if (nama == "Edukasi")
                    {
                        Nav.ShowEdukasi(this);
                    }
                    else if (nama != "Rekomendasi")
                    {
                        Info("Menu " + nama + " dibuka.");
                    }
                });
                menuItems.Add(item);
            }
            SetActiveMenu(menuItems[2]);

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

            Label settingsIcon = AddSymbol(bottom, "⚙", 24, 24, 24, 24, 12f, muted);
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
            AddText(host, "Rekomendasi", 67, 5, 110, 16, 9f, true, ink);

            AddText(host, "Rekomendasi", 0, 26, 600, 38, 19f, true, titleBlue);
            AddText(host, "Langkah yang disarankan berdasarkan hasil asesmen terakhirmu.",
                0, 66, 800, 20, 9.5f, false, muted);
        }
    }
}
