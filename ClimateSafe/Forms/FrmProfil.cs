using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
using ClimateSafe.Auth;

namespace ClimateSafe
{
    // Kartu pengguna di dasar sidebar: bisa diklik dan (bila TabStop) difokus keyboard.
    internal class FocusCard : RoundPanel
    {
        public Color SelectColor = Color.FromArgb(37, 88, 150);

        // Cincin hanya untuk fokus dari keyboard. mouseFocus menandai fokus hasil klik;
        // autoFocus menandai fokus yang diberikan WinForms saat form diaktifkan
        // (Form.WmActivate memilih tab-stop pertama), bukan hasil Tab.
        private bool mouseFocus;
        private bool autoFocus = true;
        private bool hooked;

        public event EventHandler Activated;

        public FocusCard()
        {
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            Cursor = Cursors.Hand;
        }

        private void Raise()
        {
            if (Activated != null) Activated(this, EventArgs.Empty);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            mouseFocus = true;
            if (TabStop) Focus();
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
            mouseFocus = false;
            autoFocus = false;
            Invalidate();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (hooked) return;

            Form form = FindForm();
            if (form == null) return;
            hooked = true;
            form.VisibleChanged += delegate (object sender, EventArgs ev)
            {
                if (!form.Visible) return;
                autoFocus = true;
                mouseFocus = false;
                Invalidate();
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!Focused || mouseFocus || autoFocus || !ShowFocusCues) return;
            if (Width < 8 || Height < 8) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = UI.RoundPath(new Rectangle(1, 1, Width - 3, Height - 3), Radius))
            using (Pen p = new Pen(SelectColor, 2f))
            {
                g.DrawPath(p, path);
            }
        }
    }

    // Kolom isian nama: kontainer digambar sendiri, TextBox tanpa border di dalamnya.
    internal class NameField : Control
    {
        private readonly TextBox box = new TextBox();

        public Color LineColor = Color.FromArgb(226, 232, 240);
        public Color FocusColor = Color.FromArgb(37, 88, 150);
        public Color ErrorColor = Color.FromArgb(217, 80, 63);
        public bool HasError;

        public event EventHandler Submitted;
        public event EventHandler Typed;

        public NameField()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            TabStop = false;
            Height = 44;

            box.BorderStyle = BorderStyle.None;
            box.Font = UI.Body(10f, FontStyle.Regular);
            box.ForeColor = Color.FromArgb(30, 41, 59);
            box.BackColor = Color.White;
            box.AutoSize = false;
            Controls.Add(box);

            box.GotFocus += delegate (object sender, EventArgs e) { Invalidate(); };
            box.LostFocus += delegate (object sender, EventArgs e) { Invalidate(); };
            box.TextChanged += delegate (object sender, EventArgs e)
            {
                if (Typed != null) Typed(this, EventArgs.Empty);
            };
            box.KeyDown += delegate (object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    if (Submitted != null) Submitted(this, EventArgs.Empty);
                }
            };
        }

        public string Value
        {
            get { return box.Text; }
            set { box.Text = value; }
        }

        public void FocusInput()
        {
            box.Focus();
            box.SelectionStart = box.TextLength;
        }

        public void SetTabIndex(int index)
        {
            box.TabIndex = index;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            int h = box.PreferredHeight;
            box.SetBounds(14, Math.Max(0, (Height - h) / 2), Math.Max(40, Width - 28), h);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Width < 40 || Height < 20) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = UI.RoundPath(rect, 8))
            {
                using (SolidBrush b = new SolidBrush(Color.White)) g.FillPath(b, path);

                Color border = HasError ? ErrorColor : (box.Focused ? FocusColor : LineColor);
                float width = HasError || box.Focused ? 2f : 1f;
                using (Pen p = new Pen(border, width)) g.DrawPath(p, path);
            }
        }
    }

    // Pita berhasil: centang digambar GDI+ lalu teks.
    internal class SuccessBanner : Control
    {
        public Color Fill = Color.FromArgb(228, 238, 247);
        public Color Accent = Color.FromArgb(47, 95, 148);
        public string Message = "";

        public SuccessBanner()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Height = 40;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Width < 60 || Height < 20) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = UI.RoundPath(new Rectangle(0, 0, Width - 1, Height - 1), 8))
            using (SolidBrush b = new SolidBrush(Fill))
            {
                g.FillPath(b, path);
            }

            float cy = Height / 2f;
            using (Pen p = new Pen(Accent, 2f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                p.LineJoin = LineJoin.Round;
                g.DrawLines(p, new PointF[]
                {
                    new PointF(16f, cy),
                    new PointF(20.5f, cy + 4.5f),
                    new PointF(28f, cy - 5f)
                });
            }

            using (Font f = UI.Body(10f, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, Message, f,
                    new Rectangle(38, 0, Math.Max(60, Width - 50), Height), Accent,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }
    }

    internal sealed class FrmProfil : Form
    {
        private readonly Color coral = Color.FromArgb(235, 122, 104);
        private readonly Color coralSoft = Color.FromArgb(253, 232, 226);
        private readonly Color coralInk = Color.FromArgb(159, 64, 51);
        private readonly Color errorRed = Color.FromArgb(217, 80, 63);
        private readonly Color navy = Color.FromArgb(37, 88, 150);
        private readonly Color btnBlue = Color.FromArgb(55, 108, 178);
        private readonly Color btnBlueHover = Color.FromArgb(44, 94, 164);
        private readonly Color titleBlue = Color.FromArgb(34, 62, 108);
        private readonly Color ink = Color.FromArgb(30, 41, 59);
        private readonly Color subInk = Color.FromArgb(82, 102, 126);
        private readonly Color muted = Color.FromArgb(100, 116, 139);
        private readonly Color bg = Color.FromArgb(238, 242, 248);
        private readonly Color line = Color.FromArgb(226, 232, 240);
        private readonly Color bandBg = Color.FromArgb(250, 252, 255);
        private readonly Color readOnlyBg = Color.FromArgb(244, 247, 252);
        private readonly Color badgeBg = Color.FromArgb(226, 235, 250);
        private readonly Color pillBlueBg = Color.FromArgb(228, 238, 247);
        private readonly Color pillBlueInk = Color.FromArgb(47, 95, 148);

        private const int PageMaxW = 1120;
        private const int PageMinW = 960;
        private const int ContentTop = 104;
        private const int LeftW = 320;
        private const int ColGap = 24;
        private const int HeadH = 52;
        private const int FooterH = 73;   // 1 + 16 + 40 + 16

        private readonly List<RoundPanel> menuItems = new List<RoundPanel>();
        private readonly UserRepository userRepository = new UserRepository();

        private BufferedPanel prScroll;
        private BufferedPanel prPage;
        private BufferedPanel content;

        private RoundPanel leftCard;
        private Label avatarInitials;
        private Label profileName;
        private Label profileRole;
        private RoundPanel rolePill;
        private Label registeredDate;

        private RoundPanel rightCard;
        private SuccessBanner banner;
        private Label nameLabel;
        private NameField nameField;
        private Label errorLabel;
        private Label emailLabel;
        private RoundPanel emailField;
        private Label emailValue;
        private Label emailNote;
        private BottomBand footer;
        private PillButton saveButton;

        private FocusCard sidebarUser;
        private Label sidebarName;
        private Label sidebarInitials;

        private bool prBuilt;

        public FrmProfil()
        {
            BuildProfil();
            Nav.Register(this);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && prBuilt) ReloadFromSession();
        }

        private void BuildProfil()
        {
            AutoScaleMode = AutoScaleMode.None;
            Text = "ClimateSafe - Profil Pengguna";
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
            prScroll = sc;

            BuildSidebar(sidebar);

            BufferedPanel pg = new BufferedPanel();
            pg.Size = new Size(PageMaxW, ContentTop + 420);
            pg.Location = new Point(32, 32);
            pg.BackColor = bg;
            sc.Controls.Add(pg);
            prPage = pg;

            BuildTitleArea(prPage);
            BuildContent(prPage);

            prBuilt = true;
            sc.Resize += delegate (object sender, EventArgs e) { LayoutPage(); };

            ResumeLayout(true);
            LayoutPage();
            ReloadFromSession();
        }

        private void LayoutPage()
        {
            if (!prBuilt || prScroll == null || prPage == null) return;

            int avail = prScroll.ClientSize.Width - 64;
            int w = Math.Max(PageMinW, Math.Min(PageMaxW, avail));
            int x = Math.Max(32, (prScroll.ClientSize.Width - w) / 2);

            prPage.Left = x + prScroll.AutoScrollPosition.X;
            prPage.Width = w;
            if (content != null) content.Width = w;
        }

        // ---------- isi ----------

        private void BuildContent(Panel host)
        {
            int w = Math.Max(PageMinW, host.Width);

            BufferedPanel area = new BufferedPanel();
            area.BackColor = bg;
            area.Bounds = new Rectangle(0, ContentTop, w, 400);
            host.Controls.Add(area);
            content = area;

            BuildLeftCard(area);
            BuildRightCard(area);
        }

        private void BuildLeftCard(BufferedPanel area)
        {
            RoundPanel card = Card(area, 0, 0, LeftW, 269, 16, Color.White, line);
            leftCard = card;

            RoundPanel avatar = Card(card, (LeftW - 80) / 2, 24, 80, 80, 40, coralSoft, Color.Empty);
            avatarInitials = AddAligned(avatar, Session.Initials, 0, 0, 80, 80, 24f, true, coral,
                ContentAlignment.MiddleCenter);

            profileName = AddAligned(card, Session.DisplayName, 24, 116, LeftW - 48, 22, 12f, true, ink,
                ContentAlignment.MiddleCenter);
            profileName.AutoEllipsis = true;

            rolePill = Card(card, 0, 146, 80, 22, 11, badgeBg, Color.Empty);
            profileRole = AddAligned(rolePill, Session.RoleText, 0, 0, 80, 22, 8f, true, navy,
                ContentAlignment.MiddleCenter);
            CenterIn(rolePill, card);

            Panel divider = new Panel();
            divider.Bounds = new Rectangle(24, 188, LeftW - 48, 1);
            divider.BackColor = line;
            card.Controls.Add(divider);

            AddAligned(card, "Terdaftar sejak", 24, 209, LeftW - 48, 16, 9f, false, muted,
                ContentAlignment.MiddleCenter);
            registeredDate = AddAligned(card, "-", 24, 225, LeftW - 48, 20, 10f, false, ink,
                ContentAlignment.MiddleCenter);
        }

        private void BuildRightCard(BufferedPanel area)
        {
            int x = LeftW + ColGap;
            int w = Math.Max(400, area.Width - x);

            RoundPanel card = Card(area, x, 0, w, 320, 16, Color.White, line);
            card.ClipRegion = true;
            rightCard = card;
            Stretch(card, area, x, 0);

            AddAligned(card, "Data profil", 24, 0, 260, HeadH, 11f, true, titleBlue,
                ContentAlignment.MiddleLeft);

            Panel headLine = new Panel();
            headLine.Bounds = new Rectangle(0, HeadH, w, 1);
            headLine.BackColor = line;
            card.Controls.Add(headLine);
            Stretch(headLine, card, 0, 0);

            banner = new SuccessBanner();
            banner.Fill = pillBlueBg;
            banner.Accent = pillBlueInk;
            banner.Message = "Profil berhasil diperbarui.";
            banner.Bounds = new Rectangle(24, HeadH + 25, w - 48, 40);
            banner.Visible = false;
            card.Controls.Add(banner);
            Stretch(banner, card, 24, 24);

            nameLabel = AddText(card, "Nama", 24, 0, 200, 16, 9f, true, muted);

            nameField = new NameField();
            nameField.LineColor = line;
            nameField.FocusColor = navy;
            nameField.ErrorColor = errorRed;
            nameField.Bounds = new Rectangle(24, 0, w - 48, 44);
            nameField.TabIndex = 0;
            nameField.SetTabIndex(0);
            card.Controls.Add(nameField);
            Stretch(nameField, card, 24, 24);
            nameField.Typed += delegate (object sender, EventArgs e) { OnNameTyped(); };
            nameField.Submitted += delegate (object sender, EventArgs e) { SaveName(); };

            errorLabel = AddText(card, "Nama tidak boleh kosong", 24, 0, 300, 16, 9f, true, coralInk);
            errorLabel.Visible = false;

            emailLabel = AddText(card, "Email", 24, 0, 200, 16, 9f, true, muted);

            emailField = Card(card, 24, 0, w - 48, 44, 8, readOnlyBg, line);
            emailValue = AddAligned(emailField, "", 14, 0, w - 48 - 28, 44, 10f, false, subInk,
                ContentAlignment.MiddleLeft);
            Stretch(emailField, card, 24, 24);
            Stretch(emailValue, emailField, 14, 14);

            emailNote = AddText(card, "Email tidak dapat diubah.", 24, 0, 300, 16, 9f, false, muted);

            footer = new BottomBand();
            footer.Fill = bandBg;
            footer.LineColor = line;
            footer.Radius = 15;
            footer.Bounds = new Rectangle(1, 0, w - 2, FooterH);
            card.Controls.Add(footer);
            Stretch(footer, card, 1, 1);

            int buttonW = Measure("Simpan Perubahan", 10f, FontStyle.Bold) + 40;
            saveButton = new PillButton();
            saveButton.Text = "Simpan Perubahan";
            saveButton.Radius = 8;
            saveButton.Fill = btnBlue;
            saveButton.HoverFill = btnBlueHover;
            saveButton.TextColor = Color.White;
            saveButton.FocusColor = Color.White;
            saveButton.Font = UI.Body(10f, FontStyle.Bold);
            saveButton.Bounds = new Rectangle(footer.Width - 23 - buttonW, 16, buttonW, 40);
            saveButton.TabIndex = 1;
            footer.Controls.Add(saveButton);
            PinRight(saveButton, footer, 23);
            saveButton.Click += delegate (object sender, EventArgs e) { SaveName(); };

            LayoutBody();
        }

        // Menata ulang posisi isian sesuai pita/error yang tampil (kontrol tidak dibuat ulang).
        private void LayoutBody()
        {
            if (rightCard == null) return;

            rightCard.SuspendLayout();
            try
            {
                int y = HeadH + 1 + 24;

                if (banner.Visible)
                {
                    banner.Top = y;
                    y += 40 + 20;
                }

                nameLabel.Top = y;
                y += 16 + 6;
                nameField.Top = y;
                y += 44;

                if (errorLabel.Visible)
                {
                    errorLabel.Top = y + 6;
                    y += 6 + 16;
                }

                y += 20;
                emailLabel.Top = y;
                y += 16 + 6;
                emailField.Top = y;
                y += 44 + 6;
                emailNote.Top = y;
                y += 16 + 24;

                footer.Top = y;
                rightCard.Height = y + FooterH;
            }
            finally
            {
                rightCard.ResumeLayout(true);
            }

            int h = Math.Max(leftCard.Height, rightCard.Height);
            content.Height = h;
            prPage.Height = ContentTop + h + 64;
        }

        // ---------- data ----------

        private void ReloadFromSession()
        {
            if (!prBuilt || IsDisposed) return;

            UserAccount? user = Session.CurrentUser;

            banner.Visible = false;
            errorLabel.Visible = false;
            nameField.HasError = false;

            nameField.Value = user == null ? "" : (user.Name ?? "");
            emailValue.Text = user == null ? "" : (user.Email ?? "");

            registeredDate.Text = FormatCreatedAt(user);
            RefreshNameDisplays();
            LayoutBody();
            nameField.Invalidate();
        }

        private static string FormatCreatedAt(UserAccount? user)
        {
            if (user == null || user.CreatedAt == default(DateTime)) return "-";
            DateTime when = user.CreatedAt.Kind == DateTimeKind.Utc
                ? user.CreatedAt.ToLocalTime()
                : user.CreatedAt;
            return when.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("id-ID"));
        }

        private void RefreshNameDisplays()
        {
            profileName.Text = Session.DisplayName;
            avatarInitials.Text = Session.Initials;
            profileRole.Text = Session.RoleText;

            int pillW = Measure(Session.RoleText, 8f, FontStyle.Bold) + 24;
            rolePill.Width = pillW;
            profileRole.Bounds = new Rectangle(0, 0, pillW, 22);
            rolePill.Left = Math.Max(0, (leftCard.Width - pillW) / 2);

            if (sidebarName != null) sidebarName.Text = Session.DisplayName;
            if (sidebarInitials != null) sidebarInitials.Text = Session.Initials;
        }

        private void OnNameTyped()
        {
            if (!banner.Visible && !errorLabel.Visible) return;

            banner.Visible = false;
            errorLabel.Visible = false;
            nameField.HasError = false;
            nameField.Invalidate();
            LayoutBody();
        }

        private void SaveName()
        {
            if (Session.CurrentUser == null) return;

            string name = (nameField.Value ?? "").Trim();
            if (name.Length == 0)
            {
                banner.Visible = false;
                errorLabel.Visible = true;
                nameField.HasError = true;
                nameField.Invalidate();
                LayoutBody();
                nameField.FocusInput();
                return;
            }

            try
            {
                bool saved = userRepository.UpdateName(Session.CurrentUser.UserId, name);
                if (!saved)
                {
                    MessageBox.Show(this, "Profil belum dapat disimpan.", "Profil",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Profil belum dapat disimpan. " + ex.Message, "Profil",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Session.CurrentUser.Name = name;
            nameField.Value = name;
            nameField.HasError = false;
            errorLabel.Visible = false;
            banner.Visible = true;
            nameField.Invalidate();
            RefreshNameDisplays();
            LayoutBody();
            saveButton.Focus();
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
                    else if (nama == "Edukasi")
                    {
                        Nav.ShowEdukasi(this);
                    }
                });
                menuItems.Add(item);
            }
            SetActiveMenu(null);

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
            user.TabStop = false;
            user.Cursor = Cursors.Default;
            bottom.Controls.Add(user);
            sidebarUser = user;

            RoundPanel avatar = Card(user, 8, 9, 38, 38, 19, coralSoft, Color.Empty);
            sidebarInitials = AddAligned(avatar, Session.Initials, 0, 0, 38, 38, 8.5f, true, coral,
                ContentAlignment.MiddleCenter);

            sidebarName = AddText(user, Session.DisplayName, 55, 10, 140, 18, 8.5f, true, ink);

            RoundPanel badge = Card(user, 55, 30, 36, 16, 4, badgeBg, Color.Empty);
            AddAligned(badge, Session.RoleText, 0, 0, 36, 16, 7f, false, navy, ContentAlignment.MiddleCenter);

            AddChevron(user, 196, 20, muted);
        }

        private void SetActiveMenu(RoundPanel? active)
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
            AddText(host, "Profil", 67, 5, 70, 16, 9f, true, ink);

            AddText(host, "Profil Pengguna", 0, 26, 600, 38, 19f, true, titleBlue);
            AddText(host, "Lihat dan ubah data akunmu.", 0, 66, 800, 20, 9.5f, false, subInk);
        }
    }
}
