using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ClimateSafe.Auth;

namespace ClimateSafe
{
    // Kerangka panel admin: sidebar (struktur sama dengan sisi pengguna) + area konten.
    // Dibangun sekali; halaman adalah panel di area konten.
    internal sealed class FrmAdmin : Form
    {
        private readonly UserAccount admin;
        private readonly List<RoundPanel> menuItems = new List<RoundPanel>();

        private BufferedPanel contentScroll;
        private Control? currentPage;
        private AdminPenggunaPage? penggunaPage;
        private AdminKontenPage? kontenPage;
        private int activeIndex = -1;
        private bool built;

        public FrmAdmin(UserAccount admin)
        {
            this.admin = admin;

            // Admin.CreateMainForm tidak mengisi Session, sedangkan kartu pengguna di
            // sidebar membacanya seperti layar sisi pengguna.
            Session.CurrentUser = admin;

            BuildAdmin();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && built) ShowPage(activeIndex < 0 ? 0 : activeIndex);
        }

        private void BuildAdmin()
        {
            AutoScaleMode = AutoScaleMode.None;
            Text = "ClimateSafe - Panel Admin";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1440, 900);
            MinimumSize = new Size(1280, 720);
            WindowState = FormWindowState.Maximized;
            BackColor = AdminUi.Bg;
            Font = UI.Body(9f, FontStyle.Regular);

            SuspendLayout();

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 2;
            root.RowCount = 1;
            root.BackColor = AdminUi.Bg;
            root.Margin = Padding.Empty;
            root.Padding = Padding.Empty;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, AdminUi.SidebarW));
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
            sc.BackColor = AdminUi.Bg;
            sc.Margin = Padding.Empty;
            root.Controls.Add(sc, 1, 0);
            contentScroll = sc;

            BuildSidebar(sidebar);

            built = true;
            sc.Resize += delegate (object? sender, EventArgs e) { LayoutPage(); };

            ResumeLayout(true);
            ShowPage(0);
        }

        private void LayoutPage()
        {
            if (!built || contentScroll == null || currentPage == null) return;

            int avail = contentScroll.ClientSize.Width - AdminUi.PagePad * 2;
            int w = Math.Max(AdminUi.PageMinW, Math.Min(AdminUi.PageMaxW, avail));
            int x = Math.Max(AdminUi.PagePad, (contentScroll.ClientSize.Width - w) / 2);

            currentPage.Left = x + contentScroll.AutoScrollPosition.X;
            currentPage.Width = w;
        }

        // ---------- halaman ----------

        private void ShowPage(int index)
        {
            if (!built) return;

            if (index != activeIndex && kontenPage != null && !kontenPage.ConfirmDiscard()) return;

            SetActiveMenu(index);

            if (index == 0 && penggunaPage != null && currentPage == penggunaPage)
            {
                penggunaPage.ReloadData();
                LayoutPage();
                return;
            }
            if (index == 1 && kontenPage != null && currentPage == kontenPage)
            {
                LayoutPage();
                return;
            }

            // Halaman baru selalu mulai dari atas, juga saat halaman lama sedang di-scroll.
            contentScroll.AutoScrollPosition = new Point(0, 0);
            contentScroll.SuspendLayout();
            try
            {
                if (currentPage != null)
                {
                    contentScroll.Controls.Remove(currentPage);
                    currentPage.Dispose();
                    currentPage = null;
                    penggunaPage = null;
                    kontenPage = null;
                }

                Control page;
                if (index == 0)
                {
                    penggunaPage = new AdminPenggunaPage(this, admin);
                    page = penggunaPage;
                }
                else if (index == 1)
                {
                    kontenPage = new AdminKontenPage(this);
                    page = kontenPage;
                }
                else
                {
                    page = new AdminMonitoringPage();
                }

                page.Top = AdminUi.PagePad;
                page.Left = AdminUi.PagePad;
                contentScroll.Controls.Add(page);
                page.Top = AdminUi.PagePad;
                currentPage = page;
            }
            finally
            {
                contentScroll.ResumeLayout(true);
            }

            LayoutPage();
            ActiveControl = null;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (kontenPage != null && !kontenPage.ConfirmDiscard()) e.Cancel = true;
            base.OnFormClosing(e);
        }

        // ---------- sidebar ----------

        private void BuildSidebar(Panel sidebar)
        {
            Panel edge = new Panel();
            edge.Dock = DockStyle.Right;
            edge.Width = 1;
            edge.BackColor = AdminUi.Line;
            sidebar.Controls.Add(edge);

            RoundPanel logo = AdminUi.Card(sidebar, 28, 24, 40, 40, 10,
                Color.FromArgb(244, 247, 252), AdminUi.Line);
            UI.AddLogo(logo);

            AdminUi.AddText(sidebar, "ClimateSafe", 80, 21, 170, 26, 13f, true, AdminUi.TitleBlue);
            AdminUi.AddText(sidebar, "Panel Admin", 80, 48, 172, 16, 7.5f, false, AdminUi.Muted);

            string[] glyphs = { "☺", "▤", "↺" };
            string[] names = { "Kelola Pengguna", "Kelola Konten", "Log & Monitoring" };

            for (int i = 0; i < names.Length; i++)
            {
                RoundPanel item = AdminUi.Card(sidebar, 20, AdminUi.MenuTop + i * AdminUi.MenuGap,
                    AdminUi.MenuW, AdminUi.MenuH, 10, Color.White, Color.Empty);
                item.Tag = i;

                AdminUi.AddSymbol(item, glyphs[i], 14, 9, 26, 26, 12f, AdminUi.Ink);
                AdminUi.AddAligned(item, names[i], 50, 11, 150, 22, 10f, false, AdminUi.Ink,
                    ContentAlignment.MiddleLeft);

                int captured = i;
                AdminUi.MakeClickable(item, delegate (object? sender, EventArgs e)
                {
                    ShowPage(captured);
                });
                menuItems.Add(item);
            }

            BufferedPanel bottom = new BufferedPanel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 150;
            bottom.BackColor = Color.FromArgb(247, 249, 252);
            sidebar.Controls.Add(bottom);

            Panel topLine = new Panel();
            topLine.Dock = DockStyle.Top;
            topLine.Height = 1;
            topLine.BackColor = AdminUi.Line;
            bottom.Controls.Add(topLine);

            RoundPanel user = AdminUi.Card(bottom, 16, 16, 223, 57, 12, Color.White, AdminUi.Line);

            RoundPanel avatar = AdminUi.Card(user, 8, 9, 38, 38, 19, AdminUi.CoralSoft, Color.Empty);
            AdminUi.AddAligned(avatar, Session.Initials, 0, 0, 38, 38, 8.5f, true, AdminUi.Coral,
                ContentAlignment.MiddleCenter);

            AdminUi.AddText(user, Session.DisplayName, 55, 10, 140, 18, 8.5f, true, AdminUi.Ink);

            RoundPanel badge = AdminUi.Card(user, 55, 30, 48, 16, 4, Color.FromArgb(226, 235, 250),
                Color.Empty);
            AdminUi.AddAligned(badge, Session.RoleText, 0, 0, 48, 16, 7f, false, AdminUi.Navy,
                ContentAlignment.MiddleCenter);

            PillButton exit = AdminUi.MakeButton(bottom, "Keluar", 16, 85, 40,
                Color.White, AdminUi.HoverBg, AdminUi.Navy, AdminUi.Line, 10f, 40);
            exit.Width = 223;
            // Keluar memakai mekanisme yang sudah ada: FormLogin memasang FormClosed -> Logout.
            exit.Click += delegate (object? sender, EventArgs e) { Close(); };
        }

        private void SetActiveMenu(int index)
        {
            activeIndex = index;
            for (int i = 0; i < menuItems.Count; i++)
            {
                bool on = i == index;
                menuItems[i].Fill = on ? AdminUi.Coral : Color.White;
                foreach (Control c in menuItems[i].Controls)
                {
                    c.ForeColor = on ? Color.White : AdminUi.Ink;
                }
                menuItems[i].Invalidate();
            }
        }
    }
}
