using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClimateSafe
{
    // Kartu pilihan bahaya: satu grup radio dengan satu perhentian Tab (kartu terpilih),
    // panah mengganti pilihan, Spasi memilih, Enter diabaikan.
    internal class HazardOption : RoundPanel
    {
        public int Index;
        public Color SelectColor = Color.FromArgb(37, 88, 150);

        // Cincin fokus hanya untuk keyboard: RingAllowed diatur form saat Tab/panah dipakai.
        public bool RingAllowed;
        private bool mouseFocus;

        public event EventHandler SelectRequested;
        public event EventHandler MovePrevious;
        public event EventHandler MoveNext;

        public HazardOption()
        {
            SetStyle(ControlStyles.Selectable, true);
            TabStop = false;
            Cursor = Cursors.Hand;
        }

        public void ClearFocusHint()
        {
            RingAllowed = false;
            mouseFocus = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            mouseFocus = true;
            RingAllowed = false;
        }

        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key == Keys.Space || key == Keys.Left || key == Keys.Right ||
                key == Keys.Up || key == Keys.Down || key == Keys.Enter) return true;
            return base.IsInputKey(keyData);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key == Keys.Left || key == Keys.Up)
            {
                MarkKeyboard();
                if (MovePrevious != null) MovePrevious(this, EventArgs.Empty);
                return true;
            }
            if (key == Keys.Right || key == Keys.Down)
            {
                MarkKeyboard();
                if (MoveNext != null) MoveNext(this, EventArgs.Empty);
                return true;
            }
            if (key == Keys.Space)
            {
                MarkKeyboard();
                if (SelectRequested != null) SelectRequested(this, EventArgs.Empty);
                return true;
            }
            if (key == Keys.Enter)
            {
                return true;    // Enter tidak melakukan apa-apa di kartu ini.
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void MarkKeyboard()
        {
            mouseFocus = false;
            RingAllowed = true;
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            mouseFocus = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!Focused || mouseFocus || !RingAllowed) return;
            if (Width < 20 || Height < 20) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath ring = UI.RoundPath(new Rectangle(2, 2, Width - 5, Height - 5), Radius))
            using (Pen p = new Pen(SelectColor, 2f))
            {
                g.DrawPath(p, ring);
            }
        }
    }

    internal sealed class FrmPilihBahaya : Form
    {
        private readonly Color coral = Theme.Coral;
        private readonly Color coralHover = Theme.CoralHover;
        private readonly Color coralSoft = Theme.CoralSoft;
        private readonly Color coralBg = Theme.CoralBg;
        private readonly Color coralLine = Theme.CoralLine;
        private readonly Color navy = Theme.Navy;
        private readonly Color titleBlue = Theme.TitleBlue;
        private readonly Color ink = Theme.Ink;
        private readonly Color muted = Theme.Muted;
        private readonly Color bg = Theme.Bg;
        private readonly Color line = Theme.Line;
        private readonly Color line2 = Theme.Line2;
        private readonly Color iconBlue = Theme.IconBlue;
        private readonly Color stepIdle = Theme.StepIdle;

        private const int PageMaxW = 1120;
        private const int PageMinW = 960;
        private const int ColGap = 16;
        private const int HeadH = 52;

        private readonly List<RoundPanel> menuItems = new List<RoundPanel>();

        private readonly HazardOption[] hzOptions = new HazardOption[3];
        private readonly RoundPanel[] hzIconBoxes = new RoundPanel[3];
        private readonly Label[] hzIcons = new Label[3];
        private readonly RadioDot[] hzRadios = new RadioDot[3];
        private readonly Label[,] hzSummary = new Label[3, 2];

        private BufferedPanel pbScroll;
        private BufferedPanel pbPage;
        private RoundPanel hazardCard;
        private Label hintLabel;
        private Font descFont;

        private Label sidebarName;
        private Label sidebarInitials;

        private int hazardIdx;
        private bool pbBuilt;

        public FrmPilihBahaya()
        {
            BuildPilihBahaya();
            Nav.Register(this);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible || !pbBuilt) return;

            RefreshUserCard();
            RequestHazardSummaries();
            foreach (HazardOption option in hzOptions) option.ClearFocusHint();
            SelectHazard(0);
            ActiveControl = null;
        }

        // Tab dan panah menandai navigasi keyboard, sehingga cincin fokus boleh tampil.
        protected override bool ProcessDialogKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key == Keys.Tab || key == Keys.Left || key == Keys.Right ||
                key == Keys.Up || key == Keys.Down)
            {
                foreach (HazardOption option in hzOptions)
                {
                    if (option == null) continue;
                    option.RingAllowed = true;
                    option.Invalidate();
                }
            }
            return base.ProcessDialogKey(keyData);
        }

        private void BuildPilihBahaya()
        {
            AutoScaleMode = AutoScaleMode.None;
            Text = "ClimateSafe - Pilih Bahaya";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1440, 900);
            MinimumSize = new Size(1280, 720);
            WindowState = FormWindowState.Maximized;
            BackColor = bg;
            Font = UI.Body(9f, FontStyle.Regular);

            descFont = UI.Body(9f, FontStyle.Regular);

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
            pbScroll = sc;

            BuildSidebar(sidebar);

            BufferedPanel pg = new BufferedPanel();
            pg.Size = new Size(PageMaxW, 798);
            pg.Location = new Point(32, 32);
            pg.BackColor = bg;
            sc.Controls.Add(pg);
            pbPage = pg;

            BuildTitleArea(pbPage);
            BuildStepper(pbPage);
            BuildHazardPicker(pbPage);
            BuildDimensions(pbPage);
            BuildNote(pbPage);
            BuildActionBar(pbPage);

            pbBuilt = true;
            SelectHazard(0);

            sc.Resize += delegate (object sender, EventArgs e) { LayoutPage(); };

            ResumeLayout(true);
            LayoutPage();
            RequestHazardSummaries();
        }

        private void LayoutPage()
        {
            if (!pbBuilt || pbScroll == null || pbPage == null) return;

            int avail = pbScroll.ClientSize.Width - 64;
            int w = Math.Max(PageMinW, Math.Min(PageMaxW, avail));
            int x = Math.Max(32, (pbScroll.ClientSize.Width - w) / 2);

            pbPage.Left = x + pbScroll.AutoScrollPosition.X;
            pbPage.Width = w;
        }

        // ---------- isi ----------

        // Geometri sama dengan BuildStepper di FrmAssessment.
        private void BuildStepper(Panel host)
        {
            RoundPanel card = Card(host, 0, 105, PageMaxW, 60, 14, Color.White, line);
            Stretch(card, host, 0, 0);

            RoundPanel c1 = Card(card, 32, 16, 28, 28, 14, coral, Color.Empty);
            AddAligned(c1, "1", 0, 0, 28, 28, 8.5f, true, Color.White, ContentAlignment.MiddleCenter);
            AddAligned(card, "1. Pilih Bahaya", 70, 19, 130, 22, 9f, true, ink,
                ContentAlignment.MiddleLeft);

            Panel line1 = new Panel();
            line1.Bounds = new Rectangle(189, 29, 165, 2);
            line1.BackColor = line;
            card.Controls.Add(line1);

            RoundPanel c2 = Card(card, 381, 16, 28, 28, 14, stepIdle, line);
            AddAligned(c2, "2", 0, 0, 28, 28, 8f, false, muted, ContentAlignment.MiddleCenter);
            AddAligned(card, "2. Jawab Pertanyaan", 420, 19, 200, 22, 9f, false, muted,
                ContentAlignment.MiddleLeft);

            Panel line2Bar = new Panel();
            line2Bar.Bounds = new Rectangle(585, 29, 165, 2);
            line2Bar.BackColor = line;
            card.Controls.Add(line2Bar);

            RoundPanel c3 = Card(card, 778, 16, 28, 28, 14, stepIdle, line);
            AddAligned(c3, "3", 0, 0, 28, 28, 8f, false, muted, ContentAlignment.MiddleCenter);
            AddAligned(card, "3. Hasil Asesmen", 816, 19, 200, 22, 9f, false, muted,
                ContentAlignment.MiddleLeft);
        }

        // Geometri sama dengan BuildHazardPicker di FrmAssessment, tanpa teks hint kanan.
        private void BuildHazardPicker(Panel host)
        {
            hazardCard = Card(host, 0, 187, PageMaxW, 230, 14, Color.White, line);
            Stretch(hazardCard, host, 0, 0);

            AddText(hazardCard, "Pilih jenis bahaya", 25, 22, 300, 22, 10.5f, true, ink);

            for (int k = 0; k < 3; k++)
            {
                int idx = k;

                HazardOption opt = new HazardOption();
                opt.Index = k;
                opt.Bounds = new Rectangle(25 + k * 362, 60, 346, 150);
                opt.Radius = 10;
                opt.Fill = Color.White;
                opt.BorderColor = line2;
                opt.SelectColor = navy;
                hazardCard.Controls.Add(opt);
                hzOptions[k] = opt;

                RoundPanel ib = Card(opt, 17, 17, 36, 36, 8, iconBlue, Color.Empty);
                hzIconBoxes[k] = ib;
                hzIcons[k] = AddSymbol(ib, FrmAssessment.hazardGlyph[k], 0, 0, 36, 36, 12f, navy);

                RadioDot rd = new RadioDot();
                rd.Bounds = new Rectangle(346 - 17 - 18, 17, 18, 18);
                rd.OnColor = coral;
                rd.OffBorder = Color.FromArgb(203, 213, 225);
                rd.TabStop = false;
                opt.Controls.Add(rd);
                PinRight(rd, opt, 17);
                hzRadios[k] = rd;

                AddAligned(opt, FrmAssessment.hazardNames[k], 17, 65, 300, 22, 10f, true, ink,
                    ContentAlignment.MiddleLeft);

                for (int j = 0; j < 2; j++)
                {
                    hzSummary[k, j] = AddAligned(opt, "", 17, 91 + j * 21, 310, 20, 9f, false, muted,
                        ContentAlignment.MiddleLeft);
                }

                MakeClickable(opt, delegate (object sender, EventArgs e) { SelectHazard(idx); });
                opt.SelectRequested += delegate (object sender, EventArgs e) { SelectHazard(idx); };
                opt.MovePrevious += delegate (object sender, EventArgs e) { MoveSelection(-1); };
                opt.MoveNext += delegate (object sender, EventArgs e) { MoveSelection(1); };
            }

            hazardCard.Resize += delegate (object sender, EventArgs e)
            {
                int ow = (hazardCard.Width - 50 - 32) / 3;
                for (int k = 0; k < 3; k++)
                {
                    hzOptions[k].SetBounds(25 + k * (ow + 16), 60, ow, 150);
                }
            };
        }

        private void BuildDimensions(Panel host)
        {
            RoundPanel card = Card(host, 0, 441, PageMaxW, 141, 14, Color.White, line);
            Stretch(card, host, 0, 0);

            AddAligned(card, "Dimensi yang dinilai", 24, 0, 300, HeadH, 11f, true, titleBlue,
                ContentAlignment.MiddleLeft);

            string countText = FrmAssessment.dimNames.Length + " pertanyaan";
            int countW = Measure(countText, 9f, FontStyle.Regular) + 8;
            Label count = AddAligned(card, countText, PageMaxW - 24 - countW, 0, countW, HeadH, 9f, false,
                muted, ContentAlignment.MiddleRight);
            PinRight(count, card, 24);

            Panel headLine = new Panel();
            headLine.Bounds = new Rectangle(0, HeadH, PageMaxW, 1);
            headLine.BackColor = line;
            card.Controls.Add(headLine);
            Stretch(headLine, card, 0, 0);

            int total = FrmAssessment.dimNames.Length;
            BufferedPanel body = new BufferedPanel();
            body.Bounds = new Rectangle(24, HeadH + 1 + 24, PageMaxW - 48, 40);
            body.BackColor = Color.White;
            card.Controls.Add(body);
            Stretch(body, card, 24, 24);

            int colW = Math.Max(120, (body.Width - ColGap * (total - 1)) / total);
            for (int i = 0; i < total; i++)
            {
                BufferedPanel col = new BufferedPanel();
                col.Bounds = new Rectangle(i * (colW + ColGap), 0, colW, 40);
                col.BackColor = Color.White;
                body.Controls.Add(col);

                RoundPanel ib = Card(col, 0, 4, 32, 32, 8, iconBlue, Color.Empty);
                AddSymbol(ib, FrmAssessment.dimGlyph[i], 0, 0, 32, 32, 10.5f, navy);

                int textW = Math.Max(60, colW - 44);
                List<string> lines;
                using (Font f = UI.Body(9f, FontStyle.Regular))
                {
                    lines = WrapLines(FrmAssessment.dimNames[i], f, textW);
                }
                if (lines.Count > 2)
                {
                    lines = new List<string> { lines[0], lines[1] };
                }

                int blockH = Math.Max(1, lines.Count) * 18;
                int top = (40 - blockH) / 2;
                for (int k = 0; k < lines.Count; k++)
                {
                    AddAligned(col, lines[k], 44, top + k * 18, textW, 18, 9f, false, ink,
                        ContentAlignment.MiddleLeft);
                }

                int index = i;
                body.Resize += delegate (object sender, EventArgs e)
                {
                    int cw = Math.Max(120, (body.Width - ColGap * (total - 1)) / total);
                    col.SetBounds(index * (cw + ColGap), 0, cw, 40);
                };
            }
        }

        private void BuildNote(Panel host)
        {
            RoundPanel card = Card(host, 0, 606, PageMaxW, 64, 14, coralBg, coralLine);
            Stretch(card, host, 0, 0);

            RoundPanel ib = Card(card, 24, 16, 32, 32, 8, coralSoft, Color.Empty);
            AddSymbol(ib, "✔", 0, 0, 32, 32, 10f, coral);

            AddText(card, "Isi sesuai kondisi sebenarnya", 72, 14, 320, 20, 9.5f, true, coral);
            Label desc = AddAligned(card,
                "Jawabanmu digunakan untuk mengidentifikasi rekomendasi tindakan dan checklist yang tepat.",
                72, 34, PageMaxW - 72 - 24, 18, 8.5f, false, muted, ContentAlignment.MiddleLeft);
            Stretch(desc, card, 72, 24);
        }

        private void BuildActionBar(Panel host)
        {
            RoundPanel card = Card(host, 0, 694, PageMaxW, 72, 14, Color.White, line);
            Stretch(card, host, 0, 0);

            hintLabel = AddAligned(card, "", 24, 0, PageMaxW - 48 - 200, 72, 9f, false, muted,
                ContentAlignment.MiddleLeft);

            // Lebar tombol: teks diukur seperti kontrol menggambar + padding 24 kiri dan kanan.
            int buttonW = Math.Max(160,
                LabelWidth("Lanjutkan  →", 10f, FontStyle.Bold) + 48);
            PillButton next = new PillButton();
            next.Text = "Lanjutkan  →";
            next.Radius = 10;
            next.Fill = coral;
            next.HoverFill = coralHover;
            next.TextColor = Color.White;
            next.FocusColor = Color.White;
            next.Font = UI.Body(10f, FontStyle.Bold);
            next.Bounds = new Rectangle(PageMaxW - 24 - buttonW, 13, buttonW, 45);
            card.Controls.Add(next);
            next.BringToFront();
            PinRight(next, card, 24);

            // Label status dibatasi lebarnya (dan ikut menyusut saat jendela menyempit)
            // supaya tidak pernah menutupi tombol yang berjangkar di kanan.
            hintLabel.Width = Math.Max(120, card.Width - 24 - 24 - buttonW - 24);
            Stretch(hintLabel, card, 24, 24 + buttonW + 24);
            next.Click += delegate (object sender, EventArgs e)
            {
                Nav.ShowAssessmentQuestions(this, hazardIdx);
            };
        }

        // ---------- pilihan ----------

        private void MoveSelection(int step)
        {
            int count = hzOptions.Length;
            int index = ((hazardIdx + step) % count + count) % count;
            SelectHazard(index);
        }

        private void SelectHazard(int index)
        {
            if (!pbBuilt) return;
            if (index < 0 || index >= hzOptions.Length) return;

            bool hadFocus = false;
            foreach (HazardOption option in hzOptions)
            {
                if (option.Focused) hadFocus = true;
            }

            hazardIdx = index;

            for (int k = 0; k < hzOptions.Length; k++)
            {
                bool sel = (k == index);
                hzOptions[k].Fill = sel ? coralBg : Color.White;
                hzOptions[k].BorderColor = sel ? coral : line2;
                hzOptions[k].TabStop = sel;
                hzOptions[k].Invalidate();

                hzIconBoxes[k].Fill = sel ? coralSoft : iconBlue;
                hzIconBoxes[k].Invalidate();
                hzIcons[k].ForeColor = sel ? coral : navy;

                hzRadios[k].Selected = sel;
            }

            hintLabel.Text = FrmAssessment.hazardNames[index] + " dipilih · Siap melanjutkan";

            if (hadFocus) hzOptions[index].Focus();
        }

        // ---------- helper ----------

        // Lebar teks seperti Label/PillButton menggambar (padding bawaan TextRenderer).
        private static int LabelWidth(string text, Font font)
        {
            return TextRenderer.MeasureText(text, font).Width;
        }

        private static int LabelWidth(string text, float size, FontStyle style)
        {
            using (Font f = UI.Body(size, style)) return LabelWidth(text, f);
        }

        // Memuat ringkasan materi dari API (EducationSummary) lalu memperbarui label
        // kartu bahaya di thread UI.
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

            for (int k = 0; k < 3; k++)
            {
                string text;
                EducationState state = EducationSummary.GetState(FrmAssessment.hazardNames[k], out text);
                List<string> lines = SummaryLines(EducationSummary.StateText(state, text), descFont, 266);

                for (int j = 0; j < 2; j++)
                {
                    Label label = hzSummary[k, j];
                    if (label == null || label.IsDisposed) continue;
                    label.Text = j < lines.Count ? lines[j] : "";
                }
            }
        }

        // Teks ringkasan dibungkus maksimal 2 baris, dipotong di batas kata.
        private static List<string> SummaryLines(string sentence, Font font, int maxWidth)
        {
            if (sentence.Length == 0) return new List<string>();

            List<string> lines = WrapLines(sentence, font, maxWidth);
            if (lines.Count <= 2) return lines;
            return new List<string> { lines[0], Ellipsize(lines[1], font, maxWidth) };
        }

        private static string Ellipsize(string text, Font font, int maxWidth)
        {
            string value = text.TrimEnd();
            while (value.Length > 0 && LabelWidth(value + "...", font) > maxWidth)
            {
                int space = value.LastIndexOf(' ');
                if (space <= 0) break;
                value = value.Substring(0, space).TrimEnd();
            }
            return value + "...";
        }

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

        // Sama dengan BuildTitleArea di FrmAssessment.
        private void BuildTitleArea(Panel host)
        {
            Label crumb = AddText(host, "Beranda", 0, 5, 52, 16, 9f, false, muted);
            crumb.Cursor = Cursors.Hand;
            crumb.Click += delegate (object sender, EventArgs e) { Nav.ShowDashboard(this); };
            AddText(host, "/", 55, 5, 10, 16, 9f, false, muted);
            AddText(host, "Asesmen", 67, 5, 80, 16, 9f, true, ink);

            AddText(host, "Asesmen Kesiapsiagaan", 0, 26, 600, 38, 19f, true, titleBlue);
            AddText(host, "Jawab pertanyaan berikut untuk mengetahui kesiapanmu menghadapi bahaya iklim.",
                0, 66, 800, 20, 9.5f, false, muted);

            RoundPanel pill = Card(host, PageMaxW - 158, 29, 158, 30, 15, Color.White, line);
            AddSymbol(pill, "◷", 10, 0, 20, 30, 10f, navy);
            AddAligned(pill, "Estimasi 3–5 menit", 32, 0, 122, 30, 8.5f, false, ink,
                ContentAlignment.MiddleLeft);
            PinRight(pill, host, 0);
        }
    }
}
