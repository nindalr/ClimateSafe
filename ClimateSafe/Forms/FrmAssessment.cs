using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClimateSafe
{
    internal class RadioDot : Control
    {
        private bool selected;
        public Color OnColor = Color.Tomato;
        public Color OffBorder = Color.Silver;

        public RadioDot()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        public bool Selected
        {
            get { return selected; }
            set
            {
                selected = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Width < 12 || Height < 12) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(1, 1, Width - 3, Height - 3);

            if (selected)
            {
                using (SolidBrush wb = new SolidBrush(Color.White))
                {
                    g.FillEllipse(wb, r);
                }
                using (Pen p = new Pen(OnColor, 2f))
                {
                    g.DrawEllipse(p, r);
                }
                Rectangle inner = Rectangle.Inflate(r, -4, -4);
                using (SolidBrush b = new SolidBrush(OnColor))
                {
                    g.FillEllipse(b, inner);
                }
            }
            else
            {
                using (SolidBrush wb = new SolidBrush(Color.White))
                {
                    g.FillEllipse(wb, r);
                }
                using (Pen p = new Pen(OffBorder, 1.5f))
                {
                    g.DrawEllipse(p, r);
                }
            }
        }
    }

    public partial class FrmAssessment : Form
    {
        private readonly Color coral = Color.FromArgb(235, 122, 104);
        private readonly Color coralSoft = Color.FromArgb(253, 232, 226);
        private readonly Color coralBg = Color.FromArgb(254, 240, 236);
        private readonly Color coralLine = Color.FromArgb(249, 214, 206);
        private readonly Color navy = Color.FromArgb(37, 88, 150);
        private readonly Color titleBlue = Color.FromArgb(34, 62, 108);
        private readonly Color ink = Color.FromArgb(30, 41, 59);
        private readonly Color muted = Color.FromArgb(100, 116, 139);
        private readonly Color bg = Color.FromArgb(238, 242, 248);
        private readonly Color line = Color.FromArgb(226, 232, 240);
        private readonly Color line2 = Color.FromArgb(222, 230, 240);
        private readonly Color iconBlue = Color.FromArgb(226, 236, 250);

        private const int PageMaxW = 1120;
        private const int PageMinW = 960;
        private const int PageH = 1004;
        private const int ColGap = 25;
        private const int RightW = 357;
        private const int DesignLeftW = 738;

        internal static readonly string[] hazardNames = { "Banjir", "Cuaca Ekstrem", "Suhu Panas Ekstrem" };
        internal static readonly string[] hazardLower = { "banjir", "cuaca ekstrem", "suhu panas ekstrem" };
        internal static readonly string[] hazardGlyph = { "\u2248", "\u2602", "\u2600" };
        internal static readonly string[] hazardDesc =
        {
            "Genangan, luapan sungai, dan drainase buruk.",
            "Angin kencang, hujan lebat, dan sambaran petir.",
            "Gelombang panas, dehidrasi, dan perlindungan ruangan."
        };

        internal static readonly string[] dimNames =
        {
            "Perlengkapan darurat", "Jalur evakuasi", "Kontak darurat",
            "Rencana logistik", "Pengetahuan bencana"
        };
        private readonly string[] dimUpper =
        {
            "PERLENGKAPAN DARURAT", "JALUR EVAKUASI", "KONTAK DARURAT",
            "RENCANA LOGISTIK", "PENGETAHUAN BENCANA"
        };
        internal static readonly string[] dimGlyph = { "\u2692", "\u27A4", "\u260E", "\u2630", "\u2139" };

        private readonly string[] questionText =
        {
            "Apakah kamu sudah menyiapkan perlengkapan darurat untuk menghadapi {0}?",
            "Apakah kamu tahu jalur evakuasi terdekat saat terjadi {0}?",
            "Apakah kamu punya daftar kontak darurat untuk situasi {0}?",
            "Apakah keluargamu punya rencana logistik menghadapi {0}?",
            "Seberapa paham kamu langkah penyelamatan saat {0}?"
        };

        private readonly string[] questionDesc =
        {
            "Termasuk tas siaga bencana (grab bag) berisi dokumen penting, senter, air minum, dan obat darurat.",
            "Termasuk titik kumpul terdekat, jalur aman, dan rute alternatif dari rumah maupun tempat kerja.",
            "Meliputi keluarga, tetangga, RT/RW, BPBD, puskesmas, dan layanan darurat setempat.",
            "Termasuk air bersih, makanan tahan lama, obat rutin, dan kebutuhan khusus untuk minimal 3x24 jam.",
            "Misalnya membaca peringatan dini BMKG, mengenali tanda bahaya, dan tahu apa yang harus dilakukan."
        };

        private readonly string[][] answerText =
        {
            new string[] { "Sudah lengkap dan mudah dijangkau", "Sudah ada, tetapi belum lengkap",
                           "Baru menyiapkan beberapa barang", "Belum menyiapkan sama sekali" },
            new string[] { "Hafal jalur dan titik kumpul, sudah pernah berlatih", "Tahu jalurnya, tetapi belum pernah berlatih",
                           "Hanya tahu sebagian jalur", "Belum tahu sama sekali" },
            new string[] { "Lengkap, tersimpan di ponsel dan dicetak", "Ada, tetapi hanya tersimpan di ponsel",
                           "Hanya memiliki beberapa nomor", "Belum punya sama sekali" },
            new string[] { "Sudah ada untuk 3x24 jam atau lebih", "Ada, tetapi kurang dari 3x24 jam",
                           "Baru menyiapkan sebagian kecil", "Belum ada sama sekali" },
            new string[] { "Sangat paham dan pernah berlatih", "Paham dasar-dasarnya",
                           "Pernah mendengar, tetapi belum paham", "Belum paham sama sekali" }
        };

        private int hazardIdx = 0;
        private int currentQ = 0;
        private readonly int[] answers = { -1, -1, -1, -1, -1 };
        private readonly AssessmentScoringService scoringService = new AssessmentScoringService();

        private readonly List<RoundPanel> menuItems = new List<RoundPanel>();

        private Panel asScroll;
        private Panel asPage;
        private RoundPanel stepCard;
        private RoundPanel hazardCard;
        private RoundPanel qCard;
        private RoundPanel navBar;
        private RoundPanel dimCard;
        private RoundPanel noteCard;
        private bool asBuilt;

        private readonly RoundPanel[] hzOptions = new RoundPanel[3];
        private readonly RoundPanel[] hzIconBoxes = new RoundPanel[3];
        private readonly Label[] hzIcons = new Label[3];
        private readonly RadioDot[] hzRadios = new RadioDot[3];

        private RoundPanel dimPill;
        private Label dimPillLabel;
        private Label counterLabel;
        private ThinBar counterBar;
        private readonly Label[] titleLines = new Label[3];
        private readonly Label[] descLines = new Label[3];
        private readonly RoundPanel[] answerRows = new RoundPanel[4];
        private readonly RadioDot[] answerRadios = new RadioDot[4];
        private readonly Label[] answerLabels = new Label[4];

        private readonly RoundPanel[] dimRows = new RoundPanel[5];
        private readonly RoundPanel[] dimIconBoxes = new RoundPanel[5];
        private readonly Label[] dimIcons = new Label[5];
        private readonly RoundPanel[] dimActivePills = new RoundPanel[5];

        private RoundButton btnBack;
        private RoundButton btnNext;
        private Label hintLabel;
        private Guid submissionKey = Guid.NewGuid();

        private Font titleFont;
        private Font descFont;
        private Font pillFont;

        public FrmAssessment()
        {
            BuildAssessment();
            Nav.Register(this);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && asBuilt)
            {
                RefreshUserCard();
                ResetAnswers();
                RefreshQuestion();
            }
        }

        // Dipakai Nav.ShowAssessmentQuestions: memakai logika SelectHazard yang sudah ada.
        public void ApplyHazard(int index)
        {
            if (!asBuilt) return;
            if (index < 0 || index >= hazardNames.Length) return;
            SelectHazard(index);
        }

        private void ResetAnswers()
        {
            submissionKey = Guid.NewGuid();
            Array.Fill(answers, -1);
            currentQ = 0;
        }

        private void BuildAssessment()
        {
            AutoScaleMode = AutoScaleMode.None;
            Text = "ClimateSafe - Asesmen";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1440, 900);
            MinimumSize = new Size(1280, 720);
            WindowState = FormWindowState.Maximized;
            BackColor = bg;
            Font = UI.Body(9f, FontStyle.Regular);

            titleFont = UI.Body(15f, FontStyle.Bold);
            descFont = UI.Body(9f, FontStyle.Regular);
            pillFont = UI.Body(8f, FontStyle.Bold);

            SuspendLayout();
            Controls.Clear();
            menuItems.Clear();
            asBuilt = false;

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
            asScroll = sc;

            BuildSidebar(sidebar);

            BufferedPanel pg = new BufferedPanel();
            pg.Size = new Size(PageMaxW, PageH);
            pg.Location = new Point(32, 48);
            pg.BackColor = bg;
            sc.Controls.Add(pg);
            asPage = pg;

            BuildTitleArea(asPage);
            BuildStepper(asPage);
            BuildHazardPicker(asPage);
            BuildQuestionCard(asPage);
            BuildNavBar(asPage);
            BuildDimensions(asPage);
            BuildNote(asPage);

            SelectHazard(0);
            RefreshQuestion();

            asBuilt = true;
            sc.Resize += delegate (object sender, EventArgs e) { LayoutPage(); };

            ResumeLayout(true);
            LayoutPage();
        }

        private void LayoutPage()
        {
            if (!asBuilt || asScroll == null || asPage == null) return;

            int avail = asScroll.ClientSize.Width - 64;
            int w = Math.Max(PageMinW, Math.Min(PageMaxW, avail));
            int x = Math.Max(32, (asScroll.ClientSize.Width - w) / 2);

            asPage.Left = x + asScroll.AutoScrollPosition.X;
            asPage.Width = w;

            int lw = w - ColGap - RightW;
            int rx = lw + ColGap;

            stepCard.SetBounds(0, 105, w, 60);
            hazardCard.SetBounds(0, 187, w, 230);
            qCard.SetBounds(0, 439, lw, 475);
            navBar.SetBounds(0, 931, lw, 73);
            dimCard.SetBounds(rx, 439, RightW, 350);
            noteCard.SetBounds(rx, 807, RightW, 148);
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

        private static void SetLines(Label[] labels, List<string> lines, int top, int lineHeight)
        {
            for (int i = 0; i < labels.Length; i++)
            {
                if (i < lines.Count)
                {
                    labels[i].Text = lines[i];
                    labels[i].Top = top + i * lineHeight;
                    labels[i].Visible = true;
                }
                else
                {
                    labels[i].Visible = false;
                }
            }
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
                    if (nama == "Beranda")
                    {
                        Nav.ShowDashboard(this);
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
                    else if (nama != "Asesmen")
                    {
                        Info("Menu " + nama + " dibuka.");
                    }
                });
                menuItems.Add(item);
            }
            SetActiveMenu(menuItems[1]);

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
            AddText(host, "Asesmen", 67, 5, 80, 16, 9f, true, ink);

            AddText(host, "Asesmen Kesiapsiagaan", 0, 26, 600, 38, 19f, true, titleBlue);
            AddText(host, "Jawab pertanyaan berikut untuk mengetahui kesiapanmu menghadapi bahaya iklim.",
                0, 66, 800, 20, 9.5f, false, muted);

            RoundPanel pill = Card(host, PageMaxW - 158, 29, 158, 30, 15, Color.White, line);
            AddSymbol(pill, "\u25F7", 10, 0, 20, 30, 10f, navy);
            AddAligned(pill, "Estimasi 3\u20135 menit", 32, 0, 122, 30, 8.5f, false, ink,
                ContentAlignment.MiddleLeft);
            PinRight(pill, host, 0);
        }

        private void BuildStepper(Panel host)
        {
            stepCard = Card(host, 0, 105, PageMaxW, 60, 14, Color.White, line);

            RoundPanel c1 = Card(stepCard, 32, 16, 28, 28, 14, iconBlue, Color.FromArgb(200, 216, 240));
            AddSymbol(c1, "\u2714", 0, 0, 28, 28, 8f, navy);
            AddAligned(stepCard, "1. Pilih Bahaya", 70, 19, 130, 22, 9f, false, ink,
                ContentAlignment.MiddleLeft);

            Panel line1 = new Panel();
            line1.Bounds = new Rectangle(189, 29, 165, 2);
            line1.BackColor = coral;
            stepCard.Controls.Add(line1);

            RoundPanel c2 = Card(stepCard, 381, 16, 28, 28, 14, coral, Color.Empty);
            AddAligned(c2, "2", 0, 0, 28, 28, 8.5f, true, Color.White, ContentAlignment.MiddleCenter);
            AddAligned(stepCard, "2. Jawab Pertanyaan", 420, 19, 200, 22, 9f, true, ink,
                ContentAlignment.MiddleLeft);

            Panel line2Bar = new Panel();
            line2Bar.Bounds = new Rectangle(585, 29, 165, 2);
            line2Bar.BackColor = line;
            stepCard.Controls.Add(line2Bar);

            RoundPanel c3 = Card(stepCard, 778, 16, 28, 28, 14, Color.FromArgb(240, 244, 250), line);
            AddAligned(c3, "3", 0, 0, 28, 28, 8f, false, muted, ContentAlignment.MiddleCenter);
            AddAligned(stepCard, "3. Hasil Asesmen", 816, 19, 200, 22, 9f, false, muted,
                ContentAlignment.MiddleLeft);
        }

        private void BuildHazardPicker(Panel host)
        {
            hazardCard = Card(host, 0, 187, PageMaxW, 230, 14, Color.White, line);

            AddText(hazardCard, "Pilih jenis bahaya", 25, 22, 300, 22, 10.5f, true, ink);
            Label hint = AddAligned(hazardCard, "Bisa diubah kapan saja", PageMaxW - 25 - 220, 23, 220, 18,
                8.5f, false, muted, ContentAlignment.MiddleRight);
            PinRight(hint, hazardCard, 25);

            for (int k = 0; k < 3; k++)
            {
                int idx = k;
                RoundPanel opt = Card(hazardCard, 25 + k * 362, 60, 346, 150, 10, Color.White, line2);
                hzOptions[k] = opt;

                RoundPanel ib = Card(opt, 17, 17, 36, 36, 8, iconBlue, Color.Empty);
                hzIconBoxes[k] = ib;
                hzIcons[k] = AddSymbol(ib, hazardGlyph[k], 0, 0, 36, 36, 12f, navy);

                RadioDot rd = new RadioDot();
                rd.Bounds = new Rectangle(346 - 17 - 18, 17, 18, 18);
                rd.OnColor = coral;
                rd.OffBorder = Color.FromArgb(203, 213, 225);
                opt.Controls.Add(rd);
                PinRight(rd, opt, 17);
                hzRadios[k] = rd;

                AddAligned(opt, hazardNames[k], 17, 65, 300, 22, 10f, true, ink, ContentAlignment.MiddleLeft);

                List<string> dl = WrapLines(hazardDesc[k], descFont, 266);
                for (int j = 0; j < dl.Count; j++)
                {
                    AddAligned(opt, dl[j], 17, 91 + j * 21, 310, 20, 9f, false, muted,
                        ContentAlignment.MiddleLeft);
                }

                MakeClickable(opt, delegate (object sender, EventArgs e) { SelectHazard(idx); });
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

        private void SelectHazard(int index)
        {
            if (hazardIdx != index) ResetAnswers();
            hazardIdx = index;
            for (int k = 0; k < 3; k++)
            {
                bool sel = (k == index);
                hzOptions[k].Fill = sel ? coralBg : Color.White;
                hzOptions[k].BorderColor = sel ? coral : line2;
                hzOptions[k].Invalidate();

                hzIconBoxes[k].Fill = sel ? coralSoft : iconBlue;
                hzIconBoxes[k].Invalidate();
                hzIcons[k].ForeColor = sel ? coral : navy;

                hzRadios[k].Selected = sel;
            }
            RefreshQuestion();
        }

        private void BuildQuestionCard(Panel host)
        {
            qCard = Card(host, 0, 439, DesignLeftW, 475, 14, Color.White, line);

            dimPill = Card(qCard, 29, 29, 189, 24, 6, iconBlue, Color.Empty);
            dimPillLabel = AddAligned(dimPill, "", 0, 0, 189, 24, 8f, true, navy,
                ContentAlignment.MiddleCenter);

            counterBar = new ThinBar();
            counterBar.Bounds = new Rectangle(DesignLeftW - 28 - 64, 38, 64, 5);
            counterBar.BarColor = coral;
            counterBar.TrackColor = Color.FromArgb(226, 234, 245);
            qCard.Controls.Add(counterBar);
            PinRight(counterBar, qCard, 28);

            counterLabel = AddAligned(qCard, "", DesignLeftW - 28 - 64 - 12 - 160, 32, 160, 18, 8f, false,
                muted, ContentAlignment.MiddleRight);
            qCard.Resize += delegate (object sender, EventArgs e)
            {
                counterLabel.Left = counterBar.Left - 12 - counterLabel.Width;
            };

            for (int i = 0; i < 3; i++)
            {
                titleLines[i] = AddAligned(qCard, "", 29, 71 + i * 30, 640, 30, 15f, true, titleBlue,
                    ContentAlignment.MiddleLeft);
                descLines[i] = AddAligned(qCard, "", 29, 139 + i * 23, 660, 22, 9f, false, muted,
                    ContentAlignment.MiddleLeft);
            }

            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                RoundPanel row = Card(qCard, 29, 209 + i * 62, DesignLeftW - 57, 51, 10, Color.White, line2);
                Stretch(row, qCard, 29, 28);
                answerRows[i] = row;

                RadioDot rd = new RadioDot();
                rd.Bounds = new Rectangle(17, 15, 20, 20);
                rd.OnColor = coral;
                rd.OffBorder = Color.FromArgb(203, 213, 225);
                row.Controls.Add(rd);
                answerRadios[i] = rd;

                answerLabels[i] = AddAligned(row, "", 51, 0, 600, 51, 11f, false, ink,
                    ContentAlignment.MiddleLeft);

                MakeClickable(row, delegate (object sender, EventArgs e)
                {
                    if (answers[currentQ] != idx) submissionKey = Guid.NewGuid();
                    answers[currentQ] = idx;
                    RefreshQuestion();
                });
            }
        }

        private void RefreshQuestion()
        {
            if (btnNext == null || hintLabel == null || dimRows[4] == null) return;

            int q = currentQ;

            dimPillLabel.Text = dimUpper[q];
            int pw = TextRenderer.MeasureText(dimUpper[q], pillFont,
                new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width + 26;
            dimPill.Width = pw;
            dimPillLabel.Width = pw;
            dimPill.Invalidate();

            counterLabel.Text = "Pertanyaan " + (q + 1) + " dari 5";
            counterBar.Value = (q + 1) / 5f;
            counterBar.Invalidate();

            string title = string.Format(questionText[q], hazardLower[hazardIdx]);
            List<string> tl = WrapLines(title, titleFont, 570);
            SetLines(titleLines, tl, 71, 30);

            int n = Math.Min(tl.Count, 3);
            int descTop = 71 + 30 * n + 8;
            List<string> dl = WrapLines(questionDesc[q], descFont, 590);
            SetLines(descLines, dl, descTop, 23);

            for (int i = 0; i < 4; i++)
            {
                bool sel = (answers[q] == i);
                answerLabels[i].Text = answerText[q][i];
                answerRows[i].Fill = sel ? coralBg : Color.White;
                answerRows[i].BorderColor = sel ? coral : line2;
                answerRows[i].Invalidate();
                answerRadios[i].Selected = sel;
            }

            for (int i = 0; i < 5; i++)
            {
                bool active = (i == q);
                dimRows[i].Fill = active ? coralBg : Color.White;
                dimRows[i].BorderColor = active ? Color.FromArgb(245, 196, 185) : Color.Empty;
                dimRows[i].Invalidate();

                dimIconBoxes[i].Fill = active ? Color.Transparent : iconBlue;
                dimIconBoxes[i].Invalidate();
                dimIcons[i].ForeColor = active ? coral : navy;
                dimActivePills[i].Visible = active;
            }

            int answered = 0;
            for (int i = 0; i < answers.Length; i++)
            {
                if (answers[i] >= 0) answered++;
            }
            bool hasAnswer = answers[q] >= 0;
            hintLabel.Text = hasAnswer
                ? answered + " jawaban dipilih \u00B7 Siap melanjutkan"
                : "Pilih satu jawaban untuk melanjutkan";

            btnNext.Text = (q == 4 ? "Lihat Hasil  \u2192" : "Lanjutkan  \u2192");
            btnNext.Fill = hasAnswer ? coral : Color.FromArgb(242, 180, 168);
            btnNext.HoverFill = hasAnswer ? Color.FromArgb(224, 106, 88) : Color.FromArgb(242, 180, 168);
            btnNext.Invalidate();

            btnBack.TextColor = ink;
            btnBack.Invalidate();
        }

        private void BuildNavBar(Panel host)
        {
            navBar = Card(host, 0, 931, DesignLeftW, 73, 14, Color.White, line);

            btnBack = MakeButton(navBar, "\u2190  Kembali", 25, 14, 115, 45,
                Color.White, Color.FromArgb(244, 247, 252), ink, Color.FromArgb(214, 224, 238), 8, 10f);
            btnBack.Click += delegate (object sender, EventArgs e)
            {
                if (currentQ > 0)
                {
                    currentQ--;
                    RefreshQuestion();
                }
                else
                {
                    Nav.ShowDashboard(this);
                }
            };

            btnNext = MakeButton(navBar, "Lanjutkan  \u2192", DesignLeftW - 24 - 132, 14, 132, 45,
                coral, Color.FromArgb(224, 106, 88), Color.White, Color.Empty, 8, 10f);
            btnNext.Click += delegate (object sender, EventArgs e) { GoNext(); };
            PinRight(btnNext, navBar, 24);

            hintLabel = AddAligned(navBar, "", DesignLeftW - 24 - 132 - 12 - 300, 28, 300, 18, 8f, false,
                muted, ContentAlignment.MiddleRight);
            navBar.Resize += delegate (object sender, EventArgs e)
            {
                hintLabel.Left = btnNext.Left - 12 - hintLabel.Width;
            };
        }

        private void GoNext()
        {
            if (answers[currentQ] < 0)
            {
                Info("Pilih satu jawaban terlebih dahulu.");
                return;
            }
            if (currentQ < 4)
            {
                currentQ++;
                RefreshQuestion();
                return;
            }
            for (int i = 0; i < answers.Length; i++)
            {
                if (answers[i] < 0)
                {
                    Info("Masih ada pertanyaan yang belum dijawab. Lengkapi dulu semua dimensi ya.");
                    return;
                }
            }

            try
            {
                var hazard = new Hazard(hazardIdx + 1, hazardNames[hazardIdx]);
                for (int i = 0; i < questionText.Length; i++)
                {
                    hazard.AddQuestion(new Question(i + 1,
                        string.Format(questionText[i], hazardLower[hazardIdx])));
                }

                Assessment assessment = scoringService.Evaluate(hazard, answerText, answers);
                var recommendationRepository = new RecommendationRepository();
                IReadOnlyList<DimensionGap> gaps = scoringService.IdentifyGaps(
                    assessment, dimNames, recommendationRepository.GetDimensionThresholds());
                var recommendationService = new RecommendationService();
                IReadOnlyList<Recommendation> recommendations =
                    recommendationService.GenerateRecommendations(hazard, gaps);
                if (Session.CurrentUser == null)
                    throw new InvalidOperationException("Silakan login untuk menyimpan asesmen dan checklist.");
                var checklistService = new ChecklistService();
                int assessmentId = checklistService.SaveAssessment(Session.CurrentUser.UserId,
                    submissionKey, hazard, assessment, recommendations);

                // Kegagalan setelah asesmen tersimpan ditangani terpisah supaya pesan
                // "belum tersimpan" tidak muncul untuk data yang sudah masuk.
                try
                {
                    IReadOnlyList<DimensionScore> dimensionScores =
                        recommendationRepository.GetDimensionScores(
                            Session.CurrentUser.UserId, assessmentId);

                    var hasil = new HasilAsesmenData();
                    hasil.HazardName = hazard.Name;
                    hasil.HazardId = hazard.HazardId;
                    hasil.OverallScore = assessment.OverallScore;
                    hasil.Level = assessment.Level;
                    hasil.AssessmentDate = assessment.AssessmentDate;
                    hasil.GapCount = gaps.Count;
                    hasil.Dimensions = dimensionScores;
                    hasil.Recommendations = recommendations;

                    Nav.ShowHasil(this, hasil);
                }
                catch (Npgsql.NpgsqlException ex)
                {
                    Info("Asesmen tersimpan, tetapi hasil belum dapat ditampilkan. " + ex.Message);
                    Nav.ShowDashboard(this);
                }
                catch (InvalidOperationException ex)
                {
                    Info("Asesmen tersimpan, tetapi hasil belum dapat ditampilkan. " + ex.Message);
                    Nav.ShowDashboard(this);
                }
            }
            catch (ArgumentException ex)
            {
                Info(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                Info(ex.Message);
            }
            catch (Npgsql.NpgsqlException)
            {
                Info("Asesmen belum tersimpan. Periksa koneksi PostgreSQL lalu coba lagi.");
            }
        }

        private void BuildDimensions(Panel host)
        {
            dimCard = Card(host, DesignLeftW + ColGap, 439, RightW, 350, 14, Color.White, line);

            AddText(dimCard, "Dimensi yang dinilai", 25, 26, 280, 22, 10.5f, true, ink);

            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                RoundPanel row = Card(dimCard, 25, 65 + i * 54, RightW - 50, 45, 8, Color.White, Color.Empty);
                Stretch(row, dimCard, 25, 25);
                dimRows[i] = row;

                RoundPanel ib = Card(row, 13, 6, 32, 32, 8, iconBlue, Color.Empty);
                dimIconBoxes[i] = ib;
                dimIcons[i] = AddSymbol(ib, dimGlyph[i], 0, 0, 32, 32, 10.5f, navy);

                AddAligned(row, dimNames[i], 57, 0, 180, 45, 9f, false, ink, ContentAlignment.MiddleLeft);

                RoundPanel pill = Card(row, RightW - 50 - 13 - 42, 12, 42, 20, 6, coralSoft, Color.Empty);
                AddAligned(pill, "Aktif", 0, 0, 42, 20, 7.5f, false, coral, ContentAlignment.MiddleCenter);
                PinRight(pill, row, 13);
                dimActivePills[i] = pill;

                MakeClickable(row, delegate (object sender, EventArgs e)
                {
                    currentQ = idx;
                    RefreshQuestion();
                });
            }
        }

        private void BuildNote(Panel host)
        {
            noteCard = Card(host, DesignLeftW + ColGap, 807, RightW, 127, 14, coralBg, coralLine);

            RoundPanel ib = Card(noteCard, 21, 22, 32, 32, 8, coralSoft, Color.Empty);
            AddSymbol(ib, "\u2714", 0, 0, 32, 32, 10f, coral);

            AddText(noteCard, "Isi sesuai kondisi sebenarnya", 67, 20, 270, 20, 9.5f, true, coral);

            string[] lines =
            {
                "Jawabanmu digunakan untuk",
                "mengidentifikasi rekomendasi",
                "tindakan dan checklist yang tepat."
            };
            for (int i = 0; i < lines.Length; i++)
            {
                AddAligned(noteCard, lines[i], 67, 45 + i * 21, 270, 20, 8.5f, false, muted,
                    ContentAlignment.MiddleLeft);
            }
        }
    }
}
