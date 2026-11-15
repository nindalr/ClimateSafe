using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace ClimateSafe.Auth
{
    public class FormUserDashboard : Form
    {
        private readonly UserAccount currentUser;

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

        private Panel? dashScroll;
        private Panel? dashPage;
        private RoundPanel? dashHeader;
        private RoundPanel? cardScore;
        private RoundPanel? cardQuick;
        private RoundPanel? cardPromo;
        private RoundPanel? cardChecklist;
        private RoundPanel? cardPriority;
        private RoundPanel? dashFooter;
        private bool dashBuilt;

        public FormUserDashboard(UserAccount user)
        {
            currentUser = user;
            BuildDashboard();
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
            sc.Resize += delegate (object? sender, EventArgs e) { LayoutPage(); };

            ResumeLayout(true);
            LayoutPage();
        }

        private void LayoutPage()
        {
            if (!dashBuilt || dashScroll == null || dashPage == null) return;
            if (dashHeader == null || cardScore == null || cardQuick == null || cardPromo == null ||
                cardChecklist == null || cardPriority == null || dashFooter == null) return;

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
            parent.Resize += delegate (object? s, EventArgs e)
            {
                child.SetBounds(left, child.Top, Math.Max(10, parent.Width - left - right), child.Height);
            };
        }

        private static void PinRight(Control child, Control parent, int right)
        {
            parent.Resize += delegate (object? s, EventArgs e)
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

        private static Image? LoadAsset(string baseName)
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
            AddSymbol(logo, "\u2600", 0, 0, 40, 40, 15f, coral);

            AddText(sidebar, "ClimateSafe", 80, 21, 170, 26, 13f, true, titleBlue);
            AddText(sidebar, "Siap hari ini, lebih aman nanti", 80, 48, 172, 16, 7.5f, false, muted);

            string[] glyphs = { "\u2302", "\u270E", "\u2606", "\u2611", "\u21BA", "\u263A" };
            string[] names = { "Beranda", "Asesmen", "Rekomendasi", "Checklist", "Riwayat", "Profil" };

            for (int i = 0; i < names.Length; i++)
            {
                RoundPanel item = Card(sidebar, 20, 100 + i * 50, 215, 44, 10, Color.White, Color.Empty);
                item.Tag = names[i];

                AddSymbol(item, glyphs[i], 14, 9, 26, 26, 12f, ink);
                AddAligned(item, names[i], 50, 11, 150, 22, 10f, false, ink,
                    ContentAlignment.MiddleLeft);

                RoundPanel captured = item;
                MakeClickable(item, delegate (object? sender, EventArgs e)
                {
                    string nama = (string)captured.Tag!;
                    if (nama == "Asesmen")
                    {
                        Nav.ShowAssessment(this);
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
            EventHandler openSettings = delegate (object? sender, EventArgs e)
            {
                Info("Menu Pengaturan dibuka.");
            };
            settingsIcon.Cursor = Cursors.Hand;
            settingsIcon.Click += openSettings;
            settingsText.Cursor = Cursors.Hand;
            settingsText.Click += openSettings;

            RoundPanel userCard = Card(bottom, 16, 65, 223, 57, 12, Color.White, line);

            string displayName = string.IsNullOrWhiteSpace(currentUser.Name) ? "User" : currentUser.Name;
            string initials = displayName.Length >= 2
                ? displayName.Substring(0, 2).ToUpper()
                : displayName.ToUpper();

            RoundPanel avatar = Card(userCard, 8, 9, 38, 38, 19, coralSoft, Color.Empty);
            AddAligned(avatar, initials, 0, 0, 38, 38, 8.5f, true, coral, ContentAlignment.MiddleCenter);

            AddText(userCard, displayName, 55, 10, 140, 18, 8.5f, true, ink);

            RoundPanel badge = Card(userCard, 55, 30, 46, 16, 4, Color.FromArgb(226, 235, 250), Color.Empty);
            AddAligned(badge, currentUser.Role, 0, 0, 46, 16, 7f, false, navy, ContentAlignment.MiddleCenter);

            AddChevron(userCard, 196, 20, muted);
            MakeClickable(userCard, delegate (object? sender, EventArgs e)
            {
                Info("Profil: " + currentUser.Name + " (" + currentUser.Role + ")");
            });
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
            RoundPanel header = NewCard(pageHost, 0, 0, PageMaxW, 159, 18,
                Color.FromArgb(238, 243, 251), Color.FromArgb(253, 232, 222), 0f,
                Color.FromArgb(222, 230, 240));
            header.Decorate = DrawHeaderArt;
            dashHeader = header;

            string displayName = string.IsNullOrWhiteSpace(currentUser.Name) ? "User" : currentUser.Name;
            AddText(header, "Halo, " + displayName, 28, 36, 500, 42, 20f, true, titleBlue);
            AddText(header,
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

            RoundPanel dateCard = Card(header, 704, 64, 190, 32, 16, Color.White, pillBorder);
            AddSymbol(dateCard, "\u25A3", 10, 0, 20, 32, 10f, muted);
            AddAligned(dateCard, tanggal, 34, 0, 152, 32, 8.5f, false, ink, ContentAlignment.MiddleLeft);

            RoundPanel weatherCard = Card(header, 906, 64, 147, 32, 16, Color.White, pillBorder);
            AddSymbol(weatherCard, "\u2600", 10, 0, 20, 32, 10f, Color.FromArgb(240, 170, 40));
            AddAligned(weatherCard, "Yogyakarta 28\u00B0C", 34, 0, 110, 32, 8.5f, false, ink,
                ContentAlignment.MiddleLeft);

            RoundPanel bell = Card(header, 1065, 64, 32, 32, 16, Color.White, pillBorder);
            AddEmoji(bell, "\U0001F514", 0, 0, 32, 32, 10f, muted);
            MakeClickable(bell, delegate (object? sender, EventArgs e)
            {
                Info("Tidak ada notifikasi baru.");
            });

            header.Resize += delegate (object? sender, EventArgs e)
            {
                bell.Left = header.Width - 30 - bell.Width;
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

            AddText(card, "Skor Kesiapan Terakhir", 20, 22, 260, 20, 10.5f, true, ink);

            RoundPanel circle = Card(card, 25, 71, 54, 54, 27, coral, Color.Empty);
            AddSymbol(circle, "\u2714", 0, 0, 54, 54, 20f, Color.White);

            Label scoreBig = AddAligned(card, "72", 98, 54, 120, 56, 30f, true, ink, ContentAlignment.MiddleLeft);
            int scoreW = TextRenderer.MeasureText("72", scoreBig.Font,
                new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
            scoreBig.Width = scoreW + 20;
            AddAligned(card, "/100", 98 + scoreW + 22, 74, 70, 38, 12f, false, muted, ContentAlignment.MiddleLeft);

            RoundPanel pill = Card(card, 100, 112, 104, 24, 12, coralSoft, Color.FromArgb(248, 205, 195));
            AddAligned(pill, "\u2191 Cukup Siap", 0, 0, 104, 24, 8.5f, true, coral,
                ContentAlignment.MiddleCenter);

            Stretch(Divider(card, 20, 206, C3 - 40), card, 20, 20);
            AddText(card, "Terakhir diperbarui:", 20, 218, 150, 18, 8.5f, false, muted);
            Label date = AddAligned(card, "12 Okt 2026", C3 - 20 - 141, 218, 141, 18, 8.5f, false, ink,
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
                delegate (object? sender, EventArgs e) { Nav.ShowAssessment(this); });

            AddActionItem(card, 136, "\u2606", "Lihat Rekomendasi", "Dapatkan saran sesuai hasilmu",
                delegate (object? sender, EventArgs e) { Info("Menampilkan rekomendasi untuk Anda..."); });

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

            RoundButton start = MakeButton(card, "Mulai Asesmen  \u2192", 21, 200, 145, 35,
                coral, Color.FromArgb(224, 106, 88), Color.White, Color.Empty, 8, 8.5f);
            start.Click += delegate (object? sender, EventArgs e)
            {
                Nav.ShowAssessment(this);
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
            AddText(card, "Progress Checklist Persiapan", 62, 23, 330, 20, 10.5f, true, ink);
            AddText(card, "Lacak pemenuhan logistik & keselamatan keluarga", 62, 43, 360, 16, 8.5f, false, muted);

            Label seeAll = AddAligned(card, "Lihat Semua  \u203A", ChecklistW - 22 - 125, 28, 125, 20, 8.5f,
                false, coral, ContentAlignment.TopRight);
            seeAll.Cursor = Cursors.Hand;
            seeAll.Click += delegate (object? sender, EventArgs e)
            {
                Info("Menampilkan seluruh checklist.");
            };
            PinRight(seeAll, card, 22);

            CircleProgress cp = new CircleProgress();
            cp.Bounds = new Rectangle(55, 109, 100, 100);
            cp.Percent = 60;
            cp.MainText = "6/10";
            cp.SubText = "SELESAI";
            cp.Accent = blue;
            cp.TrackColor = Color.FromArgb(226, 234, 245);
            cp.InkColor = ink;
            cp.MutedColor = muted;
            card.Controls.Add(cp);

            AddAligned(card, "60% Kesiapan Dasar Tercapai", 20, 222, 170, 18, 8.5f, false, muted,
                ContentAlignment.TopCenter);

            AddBarRow(card, 92, "Perlengkapan Darurat", "2/3", 2f / 3f, blue);
            AddBarRow(card, 134, "Jalur Evakuasi", "1/2", 0.5f, coral);
            AddBarRow(card, 176, "Kontak Darurat", "2/2 (Lengkap)", 1f, blue);
            AddBarRow(card, 218, "Rencana Logistik", "1/2", 0.5f, coral);

            Stretch(Divider(card, 20, 269, ChecklistW - 40), card, 20, 20);
            AddText(card, "TUGAS SIAGA TERKINI", 20, 286, 300, 16, 7.5f, true, muted);

            AddTask(card, 314, "Siapkan tas siaga darurat (Survival Kit)", "10 Okt 2026", 0, true);
            AddTask(card, 360, "Cek jalur evakuasi terdekat lingkungan RW", "9 Okt 2026", 0, true);
            AddTask(card, 410, "Simpan dokumen penting di kantong anti-air", "Perlu Segera", 1, false);
            AddTask(card, 462, "Ikuti pembaruan cuaca BMKG berkala", "Harian", 2, false);
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
            cb.CheckedChanged += delegate (object? sender, EventArgs e) { apply(); };

            EventHandler toggle = delegate (object? sender, EventArgs e) { cb.Checked = !cb.Checked; };
            tx.Click += toggle;
            row.Click += toggle;
        }

        private void BuildPriorities(Panel pageHost)
        {
            int pw = PageMaxW - ChecklistW - Gap;
            RoundPanel card = Card(pageHost, ChecklistW + Gap, 458, pw, 443, 16, Color.White, line);
            cardPriority = card;

            IconBox(card, "\u26A0", 20, 31, 28, coralSoft, coral);
            AddText(card, "Prioritas Utama", 64, 23, 300, 20, 10.5f, true, ink);
            AddText(card, "Dari hasil asesmen terakhir, yang perlu Anda perhatikan:",
                64, 43, 270, 32, 8.5f, false, muted);

            Stretch(Divider(card, 20, 88, pw - 40), card, 20, 20);

            AddPriority(card, 103, "1", "Perlengkapan darurat belum lengkap",
                "Senter, cadangan baterai, & obat-obatan khusus belum siap.");
            AddPriority(card, 197, "2", "Jalur evakuasi belum dipahami",
                "Titik kumpul RW 04 dan jalur aman bebas banjir belum ditentukan.");
            AddPriority(card, 292, "3", "Rencana logistik keluarga belum ada",
                "Stok air bersih dan makanan darurat untuk 3x24 jam belum disimpan.");

            Stretch(Divider(card, 20, 398, pw - 40), card, 20, 20);
            AddText(card, "Diperbarui otomatis tiap minggu", 20, 408, 220, 18, 8f, false, muted);

            Label seeAll = AddAligned(card, "Lihat Semua  \u203A", pw - 20 - 125, 408, 125, 18, 8.5f, false, coral,
                ContentAlignment.TopRight);
            seeAll.Cursor = Cursors.Hand;
            seeAll.Click += delegate (object? sender, EventArgs e)
            {
                Info("Menampilkan semua prioritas.");
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

            MakeClickable(card, delegate (object? sender, EventArgs e) { Info(message); });
        }

        private void BuildFooter(Panel pageHost)
        {
            RoundPanel footer = Card(pageHost, 0, 1356, PageMaxW, 80, 14, Color.White, line);
            dashFooter = footer;

            NewLabel(footer, "\u201C", 21, 16, 30, 40, 22f, FontStyle.Bold, coral,
                ContentAlignment.TopLeft, "Georgia");
            NewLabel(footer,
                "Kesadaran dan persiapan adalah bentuk nyata dari kepedulian terhadap masa depan.",
                55, 20, 700, 24, 10.5f, FontStyle.Italic, ink, ContentAlignment.TopLeft, UI.FontName);
            AddText(footer, "ClimateSafe", 55, 46, 200, 16, 8f, true, muted);

            int downloadX = PageMaxW - 20 - 153;
            int shareX = downloadX - 13 - 124;

            RoundButton share = MakeButton(footer, "Bagikan Dasbor", shareX, 22, 124, 35,
                Color.White, Color.FromArgb(244, 247, 252), navy, Color.FromArgb(214, 224, 238), 8, 8f);
            share.Click += delegate (object? sender, EventArgs e)
            {
                Info("Tautan dasbor disalin untuk dibagikan.");
            };

            RoundButton download = MakeButton(footer, "Unduh Laporan (PDF)", downloadX, 22, 153, 35,
                btnBlue, Color.FromArgb(44, 94, 164), Color.White, Color.Empty, 8, 8f);
            download.Click += delegate (object? sender, EventArgs e)
            {
                Info("Laporan PDF sedang disiapkan...");
            };

            PinRight(download, footer, 20);
            footer.Resize += delegate (object? sender, EventArgs e)
            {
                share.Left = download.Left - 13 - share.Width;
            };
        }
    }
}