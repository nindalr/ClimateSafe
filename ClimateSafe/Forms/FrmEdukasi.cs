using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClimateSafe
{
    // Lingkaran 56 dengan ikon buku terbuka (state "belum memilih kategori").
    internal class BookBadge : Control
    {
        public Color Circle = Color.FromArgb(226, 236, 250);
        public Color IconColor = Color.FromArgb(37, 88, 150);

        public BookBadge()
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
            using (Pen p = new Pen(IconColor, 2f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                p.LineJoin = LineJoin.Round;
                g.DrawLines(p, new PointF[]
                {
                    new PointF(cx, cy - 6f),
                    new PointF(cx - 11f, cy - 8f),
                    new PointF(cx - 11f, cy + 5f),
                    new PointF(cx, cy + 7f)
                });
                g.DrawLines(p, new PointF[]
                {
                    new PointF(cx, cy - 6f),
                    new PointF(cx + 11f, cy - 8f),
                    new PointF(cx + 11f, cy + 5f),
                    new PointF(cx, cy + 7f)
                });
                g.DrawLine(p, cx, cy - 6f, cx, cy + 7f);
            }
        }
    }

    // Kartu kategori bahaya: bisa diklik, difokus (Enter/Spasi), dan menandai pilihan
    // lewat garis tebal + lingkaran terisi, bukan hanya warna.
    internal class HazardCardView : Control
    {
        public const int CardHeight = 88;

        public string HazardName = "";
        public string Glyph = "";
        public bool Selected;

        public Color Ink = Color.FromArgb(30, 41, 59);
        public Color Navy = Color.FromArgb(37, 88, 150);
        public Color LineColor = Color.FromArgb(226, 232, 240);
        public Color HoverBack = Color.FromArgb(250, 252, 255);
        public Color IconBack = Color.FromArgb(226, 236, 250);

        private bool hovered;

        public event EventHandler Activated;

        public HazardCardView()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            BackColor = Color.FromArgb(238, 242, 248);
            Cursor = Cursors.Hand;
            Height = CardHeight;
            AccessibleRole = AccessibleRole.RadioButton;
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Width < 120 || Height < 40) return;

            Graphics g = e.Graphics;
            using (SolidBrush b = new SolidBrush(BackColor))
            {
                g.FillRectangle(b, 0, 0, Width, Height);
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle body = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = UI.RoundPath(body, 16))
            {
                using (SolidBrush b = new SolidBrush(hovered ? HoverBack : Color.White))
                {
                    g.FillPath(b, path);
                }
                using (Pen p = new Pen(Selected ? Navy : LineColor, Selected ? 2f : 1f))
                {
                    g.DrawPath(p, path);
                }
            }

            Rectangle circle = new Rectangle(20, (Height - 44) / 2, 44, 44);
            using (SolidBrush b = new SolidBrush(Selected ? Navy : IconBack))
            {
                g.FillEllipse(b, circle);
            }

            using (Font f = UI.MakeFont("Segoe UI Symbol", 13f, FontStyle.Regular))
            {
                TextRenderer.DrawText(g, Glyph, f, circle, Selected ? Color.White : Navy,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPadding);
            }

            int textX = 20 + 44 + 16;
            using (Font f = UI.Body(12f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, HazardName, f,
                    new Rectangle(textX, 0, Math.Max(60, Width - textX - 20), Height), Ink,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }

            // Satu garis saja: garis kartu (2px navy saat terpilih, 1px saat tidak).
        }
    }

    internal sealed class FrmEdukasi : Form
    {
        private readonly Color coral = Theme.Coral;
        private readonly Color coralSoft = Theme.CoralSoft;
        private readonly Color navy = Theme.Navy;
        private readonly Color titleBlue = Theme.TitleBlue;
        private readonly Color ink = Theme.Ink;
        private readonly Color muted = Theme.Muted;
        private readonly Color bg = Theme.Bg;
        private readonly Color line = Theme.Line;
        private readonly Color iconBlue = Theme.IconBlue;
        private readonly Color bandBg = Theme.BandBg;

        private const int PageMaxW = 1120;
        private const int PageMinW = 960;
        private const int ContentTop = 104;
        private const int GridTop = 34;      // label 22 + jarak 12
        private const int CardGap = 16;
        private const int BodyMaxW = 720;
        private const int BodyLineH = 21;    // 10pt dengan tinggi baris 1,6
        private const int BlockGap = 12;
        private const int HeadH = 96;        // 24 + 18 + 4 + 26 + 24

        private readonly List<RoundPanel> menuItems = new List<RoundPanel>();
        private readonly RecommendationRepository recommendationRepository = new RecommendationRepository();

        private readonly List<Hazard> hazards = new List<Hazard>();
        private readonly List<HazardCardView> cards = new List<HazardCardView>();

        private BufferedPanel eduScroll;
        private BufferedPanel eduPage;
        private BufferedPanel content;
        private BufferedPanel contentCard;
        private string? selectedHazard;
        private ApiEducation? publishedContent;
        private List<ApiEducation> publishedContents = new();
        private string? educationError;

        // Tujuan dari kartu Beranda; dipakai sekali saat data dimuat.
        public string? PendingHazard;
        private int gridBottom;
        private bool eduBuilt;

        public FrmEdukasi()
        {
            BuildEdukasi();
            Nav.Register(this);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && eduBuilt)
            {
                RefreshUserCard();
                LoadHazards();
            }
        }

        private void BuildEdukasi()
        {
            AutoScaleMode = AutoScaleMode.None;
            Text = "ClimateSafe - Edukasi";
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
            eduScroll = sc;

            BuildSidebar(sidebar);

            BufferedPanel pg = new BufferedPanel();
            pg.Size = new Size(PageMaxW, ContentTop + 520);
            pg.Location = new Point(32, 32);
            pg.BackColor = bg;
            sc.Controls.Add(pg);
            eduPage = pg;

            BuildTitleArea(eduPage);

            eduBuilt = true;
            sc.Resize += delegate (object sender, EventArgs e) { LayoutPage(); };

            ResumeLayout(true);
            LayoutPage();
            LoadHazards();
        }

        private void LayoutPage()
        {
            if (!eduBuilt || eduScroll == null || eduPage == null) return;

            int avail = eduScroll.ClientSize.Width - 64;
            int w = Math.Max(PageMinW, Math.Min(PageMaxW, avail));
            int x = Math.Max(32, (eduScroll.ClientSize.Width - w) / 2);

            eduPage.Left = x + eduScroll.AutoScrollPosition.X;
            eduPage.Width = w;
            if (content != null) content.Width = w;
        }

        // ---------- data ----------

        private void LoadHazards()
        {
            if (!eduBuilt || IsDisposed) return;

            try
            {
                IReadOnlyList<Hazard> loaded = recommendationRepository.GetHazards();
                hazards.Clear();
                foreach (Hazard h in loaded) hazards.Add(h);

                if (PendingHazard != null)
                {
                    Hazard? wanted = hazards.Find(h => string.Equals(h.Name, PendingHazard,
                        StringComparison.OrdinalIgnoreCase));
                    if (wanted != null) selectedHazard = wanted.Name;
                    PendingHazard = null;
                }

                if (selectedHazard != null && !hazards.Exists(
                        h => string.Equals(h.Name, selectedHazard, StringComparison.Ordinal)))
                {
                    selectedHazard = null;
                }

                LoadPublishedContent();
                RebuildContent();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Edukasi belum dapat dimuat. " + ex.Message,
                    "Edukasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                hazards.Clear();
                selectedHazard = null;
                publishedContent = null;
                publishedContents.Clear();
                educationError = "Data edukasi tidak dapat dimuat.";
                RebuildContent();
            }
        }

        private void LoadPublishedContent()
        {
            int? selectedId = publishedContent?.ContentId;
            publishedContent = null;
            publishedContents.Clear();
            educationError = null;
            Hazard? hazard = hazards.Find(h => h.Name == selectedHazard);
            if (hazard == null) return;
            try
            {
                publishedContents = ApiClient.Get<List<ApiEducation>>($"api/education/hazards/{hazard.HazardId}") ?? new();
                publishedContent = publishedContents.Find(x => x.ContentId == selectedId) ?? publishedContents.FirstOrDefault();
            }
            catch (Exception ex) { educationError = "Materi tidak dapat dimuat. " + ex.Message; }
        }

        // Pemetaan ikon sama dengan FrmRekomendasi.HazardGlyph.
        private static string HazardGlyph(string hazardName)
        {
            if (string.Equals(hazardName, "Cuaca Ekstrem", StringComparison.OrdinalIgnoreCase)) return "☂";
            if (string.Equals(hazardName, "Suhu Panas Ekstrem", StringComparison.OrdinalIgnoreCase)) return "☀";
            return "≈";
        }

        // ---------- render ----------

        private void RebuildContent()
        {
            cards.Clear();
            contentCard = null;

            if (content != null)
            {
                eduPage.Controls.Remove(content);
                content.Dispose();
                content = null;
            }

            int w = Math.Max(PageMinW, eduPage.Width);

            BufferedPanel host = new BufferedPanel();
            host.BackColor = bg;
            host.Bounds = new Rectangle(0, ContentTop, w, 100);
            eduPage.Controls.Add(host);
            content = host;

            AddText(host, "Pilih kategori bahaya", 0, 0, 320, 22, 11f, true, titleBlue);

            for (int i = 0; i < hazards.Count; i++)
            {
                Hazard h = hazards[i];
                HazardCardView card = new HazardCardView();
                card.HazardName = h.Name ?? "";
                card.Glyph = HazardGlyph(card.HazardName);
                card.Selected = string.Equals(card.HazardName, selectedHazard, StringComparison.Ordinal);
                card.Ink = ink;
                card.Navy = navy;
                card.LineColor = line;
                card.HoverBack = bandBg;
                card.IconBack = iconBlue;
                card.BackColor = bg;
                card.AccessibleName = card.HazardName;
                card.TabIndex = i;
                host.Controls.Add(card);

                string captured = card.HazardName;
                card.Activated += delegate (object sender, EventArgs e) { SelectHazard(captured); };
                cards.Add(card);
            }

            LayoutGrid();
            BuildContentCard();

            host.Resize += delegate (object sender, EventArgs e) { LayoutGrid(); };
        }

        private void LayoutGrid()
        {
            if (content == null) return;

            int w = content.Width;
            int cardW = Math.Max(240, (w - CardGap * 2) / 3);

            for (int i = 0; i < cards.Count; i++)
            {
                int col = i % 3;
                int row = i / 3;
                cards[i].SetBounds(col * (cardW + CardGap), GridTop + row * (HazardCardView.CardHeight + CardGap),
                    cardW, HazardCardView.CardHeight);
            }

            int rows = cards.Count == 0 ? 0 : (cards.Count + 2) / 3;
            gridBottom = rows == 0
                ? GridTop
                : GridTop + rows * HazardCardView.CardHeight + (rows - 1) * CardGap;
        }

        private void SelectHazard(string hazardName)
        {
            if (string.Equals(selectedHazard, hazardName, StringComparison.Ordinal))
            {
                return;
            }

            selectedHazard = hazardName;
            publishedContent = null;
            LoadPublishedContent();
            foreach (HazardCardView card in cards)
            {
                card.Selected = string.Equals(card.HazardName, selectedHazard, StringComparison.Ordinal);
                card.Invalidate();
            }

            Point scroll = eduScroll.AutoScrollPosition;
            eduScroll.SuspendLayout();
            content.SuspendLayout();
            try
            {
                BuildContentCard();
            }
            finally
            {
                content.ResumeLayout(true);
                eduScroll.ResumeLayout(true);
                eduScroll.AutoScrollPosition = new Point(-scroll.X, -scroll.Y);
            }
        }

        private void BuildContentCard()
        {
            if (content == null) return;

            if (contentCard != null)
            {
                content.Controls.Remove(contentCard);
                contentCard.Dispose();
                contentCard = null;
            }

            int w = content.Width;
            int top = gridBottom + 24;

            BufferedPanel holder = new BufferedPanel();
            holder.BackColor = bg;
            holder.Bounds = new Rectangle(0, top, w, 240);
            holder.TabIndex = 100;
            content.Controls.Add(holder);
            contentCard = holder;
            Stretch(holder, content, 0, 0);

            int h;
            if (selectedHazard == null) h = BuildHintCard(holder);
            else
            {
                int summaryTop = 0;
                // Pemilih materi hanya berguna bila ada lebih dari satu materi terpublikasi.
                if (publishedContents.Count > 1)
                {
                    AddText(holder, "Pilih materi", 0, 0, 250, 22, 10f, true, titleBlue);
                    var articlePicker = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList,
                        Bounds = new Rectangle(0, 26, Math.Min(600, w), 30), Font = UI.Body(10f, FontStyle.Regular),
                        DisplayMember = nameof(ApiEducation.Title), DataSource = publishedContents };
                    articlePicker.SelectedItem = publishedContent;
                    articlePicker.SelectedIndexChanged += (_, _) =>
                    {
                        if (articlePicker.SelectedItem is ApiEducation selected && selected.ContentId != publishedContent?.ContentId)
                        {
                            publishedContent = selected;
                            BuildContentCard();
                        }
                    };
                    holder.Controls.Add(articlePicker);
                    summaryTop = 72;
                }
                var summaryHolder = new BufferedPanel { BackColor = bg, Bounds = new Rectangle(0, summaryTop, w, 240) };
                holder.Controls.Add(summaryHolder);
                Stretch(summaryHolder, holder, 0, 0);
                int summaryHeight = BuildSummaryCard(summaryHolder, publishedContent?.Title ?? selectedHazard, publishedContent?.Body);
                summaryHolder.Height = summaryHeight;
                h = summaryTop + summaryHeight;
            }

            holder.Height = h;
            content.Height = top + h;
            eduPage.Height = ContentTop + content.Height + 64;
        }

        private int BuildHintCard(BufferedPanel holder)
        {
            int w = holder.Width;
            int h = 240;

            RoundPanel card = Card(holder, 0, 0, w, h, 16, Color.White, line);
            Stretch(card, holder, 0, 0);

            List<string> lines;
            using (Font f = UI.Body(12f, FontStyle.Bold))
            {
                lines = WrapLines("Pilih kategori bahaya untuk membaca ringkasan materinya.", f, 448);
            }

            int blockH = 56 + 16 + lines.Count * 26;
            int top = Math.Max(16, (h - blockH) / 2);

            BookBadge badge = new BookBadge();
            badge.Circle = iconBlue;
            badge.IconColor = navy;
            badge.Bounds = new Rectangle((w - 56) / 2, top, 56, 56);
            card.Controls.Add(badge);
            CenterIn(badge, card);

            for (int i = 0; i < lines.Count; i++)
            {
                Label text = AddAligned(card, lines[i], 0, top + 56 + 16 + i * 26, w, 26, 12f, true, ink,
                    ContentAlignment.MiddleCenter);
                Stretch(text, card, 0, 0);
            }

            return h;
        }

        private int BuildSummaryCard(BufferedPanel holder, string hazardName, string? summary)
        {
            int w = holder.Width;

            RoundPanel card = Card(holder, 0, 0, w, HeadH + 160 + 51, 16, Color.White, line);
            Stretch(card, holder, 0, 0);

            AddText(card, "Ringkasan materi", 24, 24, 240, 18, 9f, true, muted);
            AddText(card, hazardName, 24, 46, Math.Max(200, w - 48), 26, 15f, true, titleBlue);

            Panel headLine = new Panel();
            headLine.Bounds = new Rectangle(0, HeadH, w, 1);
            headLine.BackColor = line;
            card.Controls.Add(headLine);
            Stretch(headLine, card, 0, 0);

            int bodyH;
            if (string.IsNullOrWhiteSpace(summary))
            {
                bodyH = 160;
                Label empty = AddAligned(card, educationError ?? "Ringkasan materi untuk kategori ini belum tersedia.",
                    24, HeadH + 1, Math.Max(200, w - 48), bodyH, 10f, false, muted,
                    ContentAlignment.MiddleCenter);
                Stretch(empty, card, 24, 24);
            }
            else
            {
                bodyH = BuildSummaryBody(card, summary, HeadH + 1 + 24) + 48;
                string? source = publishedContent?.SourceName;
                string? sourceUrl = publishedContent?.SourceUrl;
                if (!string.IsNullOrWhiteSpace(source) || !string.IsNullOrWhiteSpace(sourceUrl))
                {
                    string sourceText = "Sumber: " + string.Join(" - ", new[] { source, sourceUrl }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    bodyH += BuildSummaryBody(card, sourceText, HeadH + 1 + bodyH - 24) + 12;
                }
            }

            BottomBand footer = new BottomBand();
            footer.Fill = bandBg;
            footer.LineColor = line;
            footer.Radius = 15;
            footer.Bounds = new Rectangle(1, HeadH + 1 + bodyH, w - 2, 51);
            card.Controls.Add(footer);
            Stretch(footer, card, 1, 1);

            AddAligned(footer,
                "Informasi ini bersifat umum dan tidak menggantikan informasi resmi dari BNPB, BPBD, dan BMKG.",
                23, 16, Math.Max(200, w - 48), 18, 9f, false, muted, ContentAlignment.TopLeft);

            int total = HeadH + 1 + bodyH + 51;
            card.Height = total;
            return total;
        }

        // Format: baris biasa = paragraf, "- " = poin, baris kosong = pemisah.
        private int BuildSummaryBody(RoundPanel card, string summary, int top)
        {
            int y = top;
            string[] rawLines = summary.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            using (Font paragraphFont = UI.Body(10f, FontStyle.Regular))
            {
                bool first = true;
                foreach (string raw in rawLines)
                {
                    string text = raw.Trim();
                    if (text.Length == 0) continue;

                    bool bullet = text.StartsWith("- ", StringComparison.Ordinal);
                    if (bullet) text = text.Substring(2).Trim();
                    if (text.Length == 0) continue;

                    if (!first) y += BlockGap;
                    first = false;

                    int textX = bullet ? 24 + 6 + 10 : 24;
                    int wrapW = BodyMaxW - (bullet ? 16 : 0);
                    List<string> lines = WrapLines(text, paragraphFont, wrapW);

                    if (bullet)
                    {
                        RoundPanel dot = Card(card, 24, y + (BodyLineH - 6) / 2, 6, 6, 3, navy, Color.Empty);
                        dot.BackColor = Color.White;
                    }

                    for (int i = 0; i < lines.Count; i++)
                    {
                        AddAligned(card, lines[i], textX, y + i * BodyLineH, wrapW, BodyLineH, 10f, false, ink,
                            ContentAlignment.MiddleLeft);
                    }

                    y += Math.Max(1, lines.Count) * BodyLineH;
                }
            }

            return y - top;
        }

        // ---------- helper ----------

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

        // Lebar teks diukur seperti Label menggambar (padding bawaan TextRenderer), agar
        // baris hasil bungkus benar-benar muat di lebar labelnya dan tidak terpotong.
        private static int LabelWidth(string text, Font font)
        {
            return TextRenderer.MeasureText(text, font).Width;
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
                while (cur.Length > 1 && TextRenderer.MeasureText(cur, font,
                    new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width > maxWidth)
                {
                    int low = 1, high = cur.Length - 1;
                    while (low < high)
                    {
                        int mid = low + (high - low + 1) / 2;
                        if (TextRenderer.MeasureText(cur.Substring(0, mid), font,
                            new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width <= maxWidth) low = mid;
                        else high = mid - 1;
                    }
                    int split = low;
                    lines.Add(cur.Substring(0, split));
                    cur = cur.Substring(split);
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
                    else if (nama == "Riwayat")
                    {
                        Nav.ShowRiwayat(this);
                    }
                });
                menuItems.Add(item);
            }
            SetActiveMenu(menuItems[5]);

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
            AddText(host, "Edukasi", 67, 5, 80, 16, 9f, true, ink);

            AddText(host, "Edukasi", 0, 26, 600, 38, 19f, true, titleBlue);
            AddText(host, "Baca ringkasan materi kesiapsiagaan berdasarkan kategori bahaya.",
                0, 66, 800, 20, 9.5f, false, muted);
        }
    }
}
