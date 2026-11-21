using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClimateSafe
{
    // Satu baris daftar pengguna di panel admin (pola baris FrmRiwayat: kontrol kustom
    // di dalam kartu, pemisah 1px ditambahkan halaman).
    internal sealed class AdminUserRow : Control
    {
        public int UserId;
        public string NameText = "";
        public string EmailText = "";
        public string RoleText = "";
        public string StatusText = "";
        public bool IsSelf;
        public bool Selected;

        private bool hovered;
        private bool mouseFocus;
        private bool autoFocus = true;

        public event EventHandler? Activated;

        public AdminUserRow()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            BackColor = Color.White;
            Cursor = Cursors.Hand;
            Height = AdminUi.RowH;
        }

        public void ClearFocusHint()
        {
            autoFocus = true;
            mouseFocus = false;
            Invalidate();
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
            mouseFocus = true;
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
                mouseFocus = false;
                autoFocus = false;
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

        private static void Draw(Graphics g, string text, float size, FontStyle style,
                                 Rectangle rect, Color color)
        {
            using (Font f = UI.Body(size, style))
            {
                TextRenderer.DrawText(g, text, f, rect, color,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }
        }

        private static int DrawPill(Graphics g, int x, int height, string text,
                                    Color back, Color fore, float size)
        {
            int tw = AdminUi.DrawnWidth(text, size, FontStyle.Bold);
            int w = tw + 20;
            int h = 22;
            int y = (height - h) / 2;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = UI.RoundPath(new Rectangle(x, y, w, h), h / 2))
            using (SolidBrush b = new SolidBrush(back))
            {
                g.FillPath(b, path);
            }

            using (Font f = UI.Body(size, FontStyle.Bold))
            {
                TextRenderer.DrawText(g, text, f, new Rectangle(x + 10, y, tw + 4, h), fore,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
            return w;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Width < 200 || Height < 20) return;

            Graphics g = e.Graphics;
            Color back = Selected ? AdminUi.IconBlue : (hovered ? AdminUi.HoverBg : Color.White);
            using (SolidBrush b = new SolidBrush(back))
            {
                g.FillRectangle(b, 0, 0, Width, Height);
            }

            Draw(g, UserId.ToString("0"), 9.5f, FontStyle.Regular,
                new Rectangle(AdminUi.ColId, 0, AdminUi.ColName - AdminUi.ColId - 12, Height), AdminUi.Muted);

            int nameW = AdminUi.DrawnWidth(NameText, 9.5f, FontStyle.Bold);
            int nameMax = AdminUi.ColEmail - AdminUi.ColName - 12;
            Draw(g, NameText, 9.5f, FontStyle.Bold,
                new Rectangle(AdminUi.ColName, 0, Math.Min(nameW + 4, nameMax), Height), AdminUi.Ink);

            if (IsSelf)
            {
                int chipX = AdminUi.ColName + Math.Min(nameW + 4, nameMax) + 8;
                if (chipX + 80 < AdminUi.ColEmail)
                {
                    DrawPill(g, chipX, Height, "Akun Anda", AdminUi.IconBlue, AdminUi.Navy, 8f);
                }
            }

            Draw(g, EmailText, 9.5f, FontStyle.Regular,
                new Rectangle(AdminUi.ColEmail, 0, AdminUi.ColRole - AdminUi.ColEmail - 12, Height),
                AdminUi.SubInk);

            Draw(g, RoleText, 9.5f, FontStyle.Regular,
                new Rectangle(AdminUi.ColRole, 0, AdminUi.ColStatus - AdminUi.ColRole - 12, Height),
                AdminUi.SubInk);

            bool active = string.Equals(StatusText, "Active", StringComparison.OrdinalIgnoreCase);
            DrawPill(g, AdminUi.ColStatus, Height, active ? "Aktif" : "Nonaktif",
                active ? AdminUi.SuccessBg : AdminUi.IdlePillBg,
                active ? AdminUi.SuccessInk : AdminUi.IdlePillInk, 8.5f);

            if (Focused && !mouseFocus && !autoFocus)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p = new Pen(AdminUi.Navy, 2f))
                {
                    g.DrawRectangle(p, 1, 1, Width - 3, Height - 3);
                }
            }
        }
    }
}
