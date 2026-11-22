using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClimateSafe
{
    // Satu baris tindakan: kotak centang, nama dimensi, deskripsi, dan waktu selesai.
    // Digambar sendiri agar sorotan, fokus, dan klik berlaku untuk seluruh baris.
    internal class ChecklistRowView : Control
    {
        private const int PadX = 24;
        private const int PadY = 14;
        private const int BoxSize = 24;
        private const int TextX = 64;
        private const int LineH = 22;
        private const int DateW = 190;

        public string DimensionName = "";
        public List<string> Lines = new List<string>();
        public string DateText = "";
        public bool Checked;

        public Color Ink = Color.FromArgb(30, 41, 59);
        public Color Muted = Color.FromArgb(100, 116, 139);
        public Color Navy = Color.FromArgb(37, 88, 150);
        public Color HoverBack = Color.FromArgb(250, 252, 255);

        private bool hovered;

        public event EventHandler Toggled;

        public ChecklistRowView()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            // Baris checklist tidak menerima fokus: klik langsung mencentang,
            // hanya efek hover, tanpa border.
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            BackColor = Color.White;
            Cursor = Cursors.Hand;
        }

        public int DescriptionTop
        {
            get { return string.IsNullOrEmpty(DimensionName) ? PadY : PadY + 18; }
        }

        public static int MeasureHeight(bool hasDimension, int lineCount)
        {
            int top = hasDimension ? PadY + 18 : PadY;
            return Math.Max(72, top + Math.Max(1, lineCount) * LineH + PadY);
        }

        public static int TextWidth(int cardWidth)
        {
            return Math.Max(120, cardWidth - TextX - PadX - DateW - 16);
        }

        private void Raise()
        {
            if (Toggled != null) Toggled(this, EventArgs.Empty);
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
            if (Width < 40 || Height < 20) return;

            Graphics g = e.Graphics;
            using (SolidBrush b = new SolidBrush(hovered ? HoverBack : Color.White))
            {
                g.FillRectangle(b, 0, 0, Width, Height);
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;

            int descTop = DescriptionTop;
            int boxTop = descTop + (LineH - BoxSize) / 2;
            DrawBox(g, new Rectangle(PadX, boxTop, BoxSize, BoxSize));

            if (!string.IsNullOrEmpty(DimensionName))
            {
                using (Font f = UI.Body(9f, FontStyle.Regular))
                {
                    TextRenderer.DrawText(g, DimensionName, f,
                        new Rectangle(TextX, PadY, Math.Max(80, Width - TextX - PadX), 16), Muted,
                        TextFormatFlags.Left | TextFormatFlags.Top |
                        TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                }
            }

            int textW = TextWidth(Width);
            using (Font f = UI.Body(12f, FontStyle.Regular))
            {
                for (int i = 0; i < Lines.Count; i++)
                {
                    TextRenderer.DrawText(g, Lines[i], f,
                        new Rectangle(TextX, descTop + i * LineH, textW, LineH),
                        Checked ? Muted : Ink,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
            }

            if (!string.IsNullOrEmpty(DateText))
            {
                using (Font f = UI.Body(9f, FontStyle.Regular))
                {
                    TextRenderer.DrawText(g, DateText, f,
                        new Rectangle(Width - PadX - DateW, descTop, DateW, LineH), Muted,
                        TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                }
            }

        }

        // Kotak centang 24 radius 6. Digambar di sini agar TickBox milik Beranda tidak berubah.
        private void DrawBox(Graphics g, Rectangle box)
        {
            using (GraphicsPath shape = UI.RoundPath(new Rectangle(box.X, box.Y, box.Width - 1, box.Height - 1), 6))
            {
                if (Checked)
                {
                    using (SolidBrush b = new SolidBrush(Navy)) g.FillPath(b, shape);
                    using (Pen p = new Pen(Color.White, 2.4f))
                    {
                        p.StartCap = LineCap.Round;
                        p.EndCap = LineCap.Round;
                        p.LineJoin = LineJoin.Round;
                        g.DrawLines(p, new PointF[]
                        {
                            new PointF(box.X + 6f, box.Y + 12.5f),
                            new PointF(box.X + 10.5f, box.Y + 17f),
                            new PointF(box.X + 18f, box.Y + 7.5f)
                        });
                    }
                }
                else
                {
                    using (SolidBrush b = new SolidBrush(Color.White)) g.FillPath(b, shape);
                    using (Pen p = new Pen(Navy, 1.6f)) g.DrawPath(p, shape);
                }
            }
        }
    }

    internal sealed class FrmChecklist : Form
    {
        private readonly Color coral = Theme.Coral;
        private readonly Color coralSoft = Theme.CoralSoft;
        private readonly Color coralInk = Theme.CoralInk;
        private readonly Color navy = Theme.Navy;
        private readonly Color btnBlue = Theme.BtnBlue;
        private readonly Color btnBlueHover = Theme.BtnBlueHover;
        private readonly Color titleBlue = Theme.TitleBlue;
        private readonly Color ink = Theme.Ink;
        private readonly Color muted = Theme.Muted;
        private readonly Color bg = Theme.Bg;
        private readonly Color line = Theme.Line;
        private readonly Color iconBlue = Theme.IconBlue;
        private readonly Color pillBlueBg = Theme.PillBlueBg;
        private readonly Color pillBlueInk = Theme.PillBlueInk;
        private readonly Color barTrack = Theme.BarTrack;
        private readonly Color emptyInk = Theme.EmptyInk;

        private const int PageMaxW = 1120;
        private const int PageMinW = 960;
        private const int ContentTop = 104;
        private const string NotReadyLevel = ScoringRules.NotReadyLevel;

        private readonly List<RoundPanel> menuItems = new List<RoundPanel>();
        private readonly ChecklistService service;
        private readonly RecommendationRepository recommendationRepository = new RecommendationRepository();

        private List<ChecklistSession> sessions = new List<ChecklistSession>();
        private Dictionary<int, string> dimensionNames = new Dictionary<int, string>();
        private int selectedAssessmentId;

        // Diisi Nav.ShowChecklist(form, assessmentId) agar sesi tertentu terpilih saat data dimuat.
        public int PendingAssessmentId;

        private BufferedPanel clScroll;
        private BufferedPanel clPage;
        private BufferedPanel content;
        private SessionPicker picker;
        private bool clBuilt;

        public FrmChecklist(ChecklistService? service = null)
        {
            this.service = service ?? new ChecklistService();
            BuildChecklist();
            Nav.Register(this);
        }

        private static int CurrentUserId
        {
            get { return Session.CurrentUser == null ? 0 : Session.CurrentUser.UserId; }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!clBuilt) return;
            if (Visible)
            {
                RefreshUserCard();
                LoadSessions();
            }
            else if (picker != null) picker.ClosePopup();
        }

        private void BuildChecklist()
        {
            AutoScaleMode = AutoScaleMode.None;
            Text = "ClimateSafe - Checklist Persiapan";
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
            clScroll = sc;

            BuildSidebar(sidebar);

            BufferedPanel pg = new BufferedPanel();
            pg.Size = new Size(PageMaxW, ContentTop + 520);
            pg.Location = new Point(32, 48);
            pg.BackColor = bg;
            sc.Controls.Add(pg);
            clPage = pg;

            BuildTitleArea(clPage);

            clBuilt = true;
            sc.Resize += delegate (object sender, EventArgs e) { LayoutPage(); };

            ResumeLayout(true);
            LayoutPage();
            LoadSessions();
        }

        private void LayoutPage()
        {
            if (!clBuilt || clScroll == null || clPage == null) return;

            int avail = clScroll.ClientSize.Width - 64;
            int w = Math.Max(PageMinW, Math.Min(PageMaxW, avail));
            int x = Math.Max(32, (clScroll.ClientSize.Width - w) / 2);

            clPage.Left = x + clScroll.AutoScrollPosition.X;
            clPage.Width = w;
            if (content != null) content.Width = w;
        }

        // ---------- data ----------

        private void LoadSessions()
        {
            if (!clBuilt || IsDisposed) return;

            try
            {
                int keep = PendingAssessmentId != 0 ? PendingAssessmentId : selectedAssessmentId;
                PendingAssessmentId = 0;
                IReadOnlyList<ChecklistSession> loaded = Session.IsGuest
                    ? (GuestSessionView.GetChecklistSession() is ChecklistSession guest ? new List<ChecklistSession> { guest } : new List<ChecklistSession>())
                    : CurrentUserId == 0
                    ? new List<ChecklistSession>()
                    : service.GetSessions(CurrentUserId);

                sessions = loaded.ToList();
                if (dimensionNames.Count == 0 && sessions.Count > 0)
                {
                    dimensionNames = Session.IsGuest
                        ? GuestSessionView.GetDimensions().ToDictionary(d => d.DimensionId, d => d.DimensionName)
                        : recommendationRepository.GetDimensionNames();
                }

                ChecklistSession? selected = sessions.FirstOrDefault(s => s.AssessmentId == keep)
                    ?? sessions.FirstOrDefault();
                selectedAssessmentId = selected == null ? 0 : selected.AssessmentId;

                RebuildContent();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Checklist belum dapat dimuat. " + ex.Message,
                    "Checklist", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (content == null) RebuildContent();
            }
        }

        private ChecklistSession? Current
        {
            get { return sessions.FirstOrDefault(s => s.AssessmentId == selectedAssessmentId); }
        }

        private void SaveStatus(ChecklistRowView row, ChecklistItem item)
        {
            bool wanted = !item.IsCompleted;
            try
            {
                if (Session.IsGuest)
                    Session.GuestAssessment!.SetChecklistCompleted(item.ChecklistItemId, wanted);
                else service.SetCompleted(CurrentUserId, item.ChecklistItemId, wanted);
                LoadSessions();
            }
            catch (Exception ex)
            {
                if (!row.IsDisposed)
                {
                    row.Checked = item.IsCompleted;
                    row.Invalidate();
                }
                MessageBox.Show(this, "Status belum tersimpan. " + ex.Message, "Checklist",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ---------- isi halaman ----------

        private void RebuildContent()
        {
            Point keepScroll = clScroll.AutoScrollPosition;

            if (picker != null)
            {
                picker.ClosePopup();
                picker = null;
            }
            if (content != null)
            {
                clPage.Controls.Remove(content);
                content.Dispose();
                content = null;
            }

            clPage.SuspendLayout();
            try
            {
                int w = Math.Max(PageMinW, clPage.Width);

                BufferedPanel host = new BufferedPanel();
                host.BackColor = bg;
                host.Bounds = new Rectangle(0, ContentTop, w, 100);
                clPage.Controls.Add(host);
                content = host;

                int h = sessions.Count == 0 ? BuildNoSession(host) : BuildSession(host);

                host.Height = h;
                clPage.Height = ContentTop + h + 48;
            }
            finally { clPage.ResumeLayout(true); }

            clScroll.AutoScrollPosition = new Point(-keepScroll.X, -keepScroll.Y);
        }

        private int BuildSession(BufferedPanel host)
        {
            int w = host.Width;
            ChecklistSession? session = Current;
            int y = 0;

            AddText(host, "Sesi asesmen", 0, y, 200, 18, 9f, false, muted);
            y += 24;

            int reloadW = 118;
            SessionPicker sp = new SessionPicker();
            sp.Line = line;
            sp.Ink = ink;
            sp.Muted = muted;
            sp.Navy = navy;
            sp.Bounds = new Rectangle(0, y, Math.Max(200, w - 12 - reloadW), 44);
            host.Controls.Add(sp);
            picker = sp;

            List<string> labels = new List<string>();
            int selectedIndex = 0;
            for (int i = 0; i < sessions.Count; i++)
            {
                labels.Add(sessions[i].ToString());
                if (sessions[i].AssessmentId == selectedAssessmentId) selectedIndex = i;
            }
            sp.SetItems(labels, selectedIndex);
            sp.SelectedIndexChanged += delegate (object sender, EventArgs e)
            {
                int index = sp.SelectedIndex;
                if (index < 0 || index >= sessions.Count) return;
                selectedAssessmentId = sessions[index].AssessmentId;
                RebuildContent();
            };
            Stretch(sp, host, 0, 12 + reloadW);

            RoundButton reload = MakeButton(host, "Muat ulang", w - reloadW, y, reloadW, 44,
                Color.White, Color.FromArgb(246, 249, 253), navy, line, 10, 10f);
            reload.Click += delegate (object sender, EventArgs e) { LoadSessions(); };
            PinRight(reload, host, 0);

            y += 44 + 24;

            BuildSummary(host, session, y, w);
            y += 112 + 16;

            y += BuildItems(host, session, y, w);
            return y;
        }

        private void BuildSummary(BufferedPanel host, ChecklistSession? session, int y, int w)
        {
            RoundPanel card = Card(host, 0, y, w, 112, 16, Color.White, line);
            Stretch(card, host, 0, 0);

            int done = session == null ? 0 : session.CompletedCount;
            int total = session == null ? 0 : session.Items.Count;
            double progress = session == null ? 0 : session.Progress;

            AddText(card, done + " dari " + total + " tindakan selesai", 24, 24, 420, 28,
                15f, true, titleBlue);

            int rightW = 460;
            int divX = Math.Max(320, w - 24 - rightW - 24);

            Panel vline = new Panel();
            vline.Bounds = new Rectangle(divX, 24, 1, 64);
            vline.BackColor = line;
            card.Controls.Add(vline);

            string percentText = progress.ToString("0.#") + "%";
            int percentW = Measure(percentText, 11f, FontStyle.Bold) + 10;
            int barW = Math.Max(80, divX - 24 - 24 - percentW - 12);

            ThinBar bar = new ThinBar();
            bar.Bounds = new Rectangle(24, 64, barW, 10);
            bar.Value = (float)(progress / 100.0);
            bar.BarColor = navy;
            bar.TrackColor = barTrack;
            card.Controls.Add(bar);

            AddAligned(card, percentText, 24 + barW + 12, 59, percentW, 20, 11f, true,
                progress <= 0 ? muted : navy, ContentAlignment.MiddleLeft);

            int rx = divX + 24;
            string hazard = session == null ? "" : session.HazardName;
            AddText(card, hazard, rx, 24, 260, 22, 12f, true, ink);

            if (session != null)
            {
                DateTime local = session.CreatedAt.ToLocalTime();
                string dateText = "Asesmen " + local.ToString("dd MMM yyyy") + " · " + local.ToString("HH:mm");
                AddText(card, dateText, rx, 48, Measure(dateText, 9f, FontStyle.Regular) + 8, 18,
                    9f, false, muted);
            }

            string level = session == null ? "" : (session.Level ?? "");
            bool notReady = string.Equals(level, NotReadyLevel, StringComparison.OrdinalIgnoreCase);
            int pillW = Measure(level, 9f, FontStyle.Bold) + 28;
            RoundPanel pill = Card(card, w - 24 - pillW, 42, pillW, 28, 14,
                notReady ? coralSoft : pillBlueBg, Color.Empty);
            AddAligned(pill, level, 0, 0, pillW, 28, 9f, true,
                notReady ? coralInk : pillBlueInk, ContentAlignment.MiddleCenter);
            PinRight(pill, card, 24);

            string scoreText = session == null ? "0"
                : Math.Round(session.OverallScore, MidpointRounding.AwayFromZero).ToString("0");
            int scoreW = Measure(scoreText, 22f, FontStyle.Bold);
            int unitW = Measure("/100", 10f, FontStyle.Regular);
            int groupW = scoreW + 4 + unitW;
            int groupRight = w - 24 - pillW - 16;

            int capW = Measure("Skor asesmen", 9f, FontStyle.Regular) + 8;
            Label caption = AddAligned(card, "Skor asesmen", groupRight - capW, 24, capW, 18, 9f, false,
                muted, ContentAlignment.MiddleRight);
            PinRight(caption, card, 24 + pillW + 16);

            Label score = AddText(card, scoreText, groupRight - groupW, 40, scoreW + 8, 34, 22f, true, ink);
            PinRight(score, card, 24 + pillW + 16 + unitW - 4);

            Label unit = AddText(card, "/100", groupRight - unitW, 58, unitW + 8, 20, 10f, false, muted);
            PinRight(unit, card, 24 + pillW + 16 - 8);
        }

        private int BuildItems(BufferedPanel host, ChecklistSession? session, int y, int w)
        {
            RoundPanel card = Card(host, 0, y, w, 120, 16, Color.White, line);
            Stretch(card, host, 0, 0);

            int done = session == null ? 0 : session.CompletedCount;
            int total = session == null ? 0 : session.Items.Count;

            AddText(card, "Tindakan persiapan", 24, 15, 280, 22, 11f, true, ink);
            string counter = done + "/" + total;
            int counterW = Measure(counter, 9.5f, FontStyle.Regular) + 8;
            Label counterLabel = AddAligned(card, counter, w - 24 - counterW, 16, counterW, 20, 9.5f,
                false, muted, ContentAlignment.MiddleRight);
            PinRight(counterLabel, card, 24);

            int cy = 52;
            Divider(card, cy);
            cy += 1;

            if (session == null || session.Items.Count == 0)
            {
                int circleX = (w - 48) / 2;
                RoundPanel circle = Card(card, circleX, cy + 96, 48, 48, 24, iconBlue, Color.Empty);
                AddSymbol(circle, "☑", 0, 0, 48, 48, 13f, navy);
                CenterIn(circle, card);

                Label note = AddAligned(card, "Tidak ada tindakan yang dihasilkan untuk sesi ini.",
                    0, cy + 96 + 48 + 16, w, 20, 11f, false, emptyInk, ContentAlignment.TopCenter);
                Stretch(note, card, 0, 0);

                cy += 96 + 48 + 16 + 20 + 96;
                card.Height = cy;
                return cy;
            }

            // Dibungkus pada lebar halaman tersempit agar teks tetap muat saat jendela melebar.
            int textW = ChecklistRowView.TextWidth(PageMinW);
            bool first = true;

            foreach (ChecklistItem item in session.Items)
            {
                if (!first)
                {
                    Divider(card, cy);
                    cy += 1;
                }
                first = false;

                string dimension = "";
                string? name;
                if (dimensionNames.TryGetValue(item.RecommendationId % 100, out name) && name != null)
                {
                    dimension = name;
                }

                List<string> lines;
                using (Font f = UI.Body(12f, FontStyle.Regular))
                {
                    lines = WrapLines(item.Description, f, textW);
                }
                while (lines.Count > 3) lines.RemoveAt(lines.Count - 1);

                ChecklistRowView row = new ChecklistRowView();
                row.Ink = ink;
                row.Muted = muted;
                row.Navy = navy;
                row.DimensionName = dimension;
                row.Lines = lines;
                row.Checked = item.IsCompleted;
                if (item.IsCompleted && item.CompletedAt.HasValue)
                {
                    DateTime at = item.CompletedAt.Value.ToLocalTime();
                    row.DateText = "Selesai " + at.ToString("dd MMM yyyy") + " · " + at.ToString("HH:mm");
                }

                int rowH = ChecklistRowView.MeasureHeight(dimension.Length > 0, lines.Count);
                row.Bounds = new Rectangle(0, cy, card.Width, rowH);
                card.Controls.Add(row);
                Stretch(row, card, 0, 0);

                ChecklistRowView capturedRow = row;
                ChecklistItem capturedItem = item;
                row.Toggled += delegate (object sender, EventArgs e)
                {
                    SaveStatus(capturedRow, capturedItem);
                };

                cy += rowH;
            }

            card.Height = cy;
            return cy;
        }

        private int BuildNoSession(BufferedPanel host)
        {
            int w = host.Width;
            int h = 460;

            RoundPanel card = Card(host, 0, 0, w, h, 16, Color.White, line);
            Stretch(card, host, 0, 0);

            RoundPanel circle = Card(card, (w - 56) / 2, 130, 56, 56, 28, iconBlue, Color.Empty);
            AddSymbol(circle, "☑", 0, 0, 56, 56, 15f, navy);
            CenterIn(circle, card);

            string message = "Belum ada asesmen. Selesaikan asesmen untuk membuat checklist.";
            List<string> lines;
            using (Font f = UI.Body(12f, FontStyle.Bold))
            {
                lines = WrapLines(message, f, 448);
            }
            for (int i = 0; i < lines.Count; i++)
            {
                Label part = AddAligned(card, lines[i], 0, 206 + i * 24, w, 24, 12f, true, ink,
                    ContentAlignment.TopCenter);
                Stretch(part, card, 0, 0);
            }

            int buttonW = Measure("Mulai Asesmen", 10.5f, FontStyle.Bold) + 48;
            RoundButton start = MakeButton(card, "Mulai Asesmen", (w - buttonW) / 2, 286, buttonW, 44,
                btnBlue, btnBlueHover, Color.White, Color.Empty, 10, 10.5f);
            start.Click += delegate (object sender, EventArgs e) { Nav.ShowAssessment(this); };
            CenterIn(start, card);

            return h;
        }

        // ---------- helper tampilan ----------

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
            SetActiveMenu(menuItems[3]);

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
            AddText(host, "Checklist", 67, 5, 90, 16, 9f, true, ink);

            AddText(host, "Checklist Persiapan", 0, 26, 600, 38, 19f, true, titleBlue);
            AddText(host, "Pilih sesi asesmen dan tandai tindakan yang sudah dikerjakan.",
                0, 66, 800, 20, 9.5f, false, muted);
        }
    }
}
