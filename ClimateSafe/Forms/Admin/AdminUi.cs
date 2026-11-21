using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClimateSafe
{
    // Palet, ukuran, dan helper khusus layar admin. Hanya dipakai Forms/Admin/*;
    // form sisi pengguna tidak memakai kelas ini dan tidak diubah.
    internal static class AdminUi
    {
        // ---- palet (sama dengan sisi pengguna) ----
        public static readonly Color Coral = Color.FromArgb(235, 122, 104);
        public static readonly Color CoralSoft = Color.FromArgb(253, 232, 226);
        public static readonly Color Navy = Color.FromArgb(37, 88, 150);
        public static readonly Color BtnBlue = Color.FromArgb(55, 108, 178);
        public static readonly Color BtnBlueHover = Color.FromArgb(44, 94, 164);
        public static readonly Color TitleBlue = Color.FromArgb(34, 62, 108);
        public static readonly Color Ink = Color.FromArgb(30, 41, 59);
        public static readonly Color SubInk = Color.FromArgb(82, 102, 126);
        public static readonly Color Muted = Color.FromArgb(100, 116, 139);
        public static readonly Color Bg = Color.FromArgb(238, 242, 248);
        public static readonly Color Line = Color.FromArgb(226, 232, 240);
        public static readonly Color IconBlue = Color.FromArgb(226, 236, 250);
        public static readonly Color BandBg = Color.FromArgb(250, 252, 255);
        public static readonly Color HoverBg = Color.FromArgb(246, 249, 253);

        // ---- warna tambahan khusus admin (di luar palet) ----
        public static readonly Color SuccessBg = Color.FromArgb(220, 252, 231);   // #DCFCE7
        public static readonly Color SuccessInk = Color.FromArgb(22, 101, 52);    // #166534
        public static readonly Color DangerInk = Color.FromArgb(180, 35, 24);     // #B42318
        public static readonly Color DangerBg = Color.FromArgb(254, 228, 226);    // #FEE4E2
        public static readonly Color IdlePillBg = Color.FromArgb(241, 245, 249);  // #F1F5F9
        public static readonly Color IdlePillInk = Color.FromArgb(100, 116, 139); // #64748B
        public static readonly Color DisabledBg = Color.FromArgb(241, 245, 249);  // #F1F5F9
        public static readonly Color DisabledInk = Color.FromArgb(148, 163, 184); // #94A3B8

        // ---- ukuran ----
        public const int SidebarW = 256;
        public const int MenuW = 215;
        public const int MenuH = 44;
        public const int MenuTop = 100;
        public const int MenuGap = 50;
        public const int PageMaxW = 1120;
        public const int PageMinW = 960;
        public const int PagePad = 32;
        public const int ContentTop = 104;
        public const int RowH = 48;
        public const int HeadBandH = 36;
        public const int CardRadius = 16;
        public const int FieldH = 44;
        public const int ButtonH = 44;
        public const int Gap = 24;

        // ---- kolom daftar pengguna (x dari tepi kartu) ----
        public const int ColId = 24;
        public const int ColName = 96;
        public const int ColEmail = 352;
        public const int ColRole = 684;
        public const int ColStatus = 800;

        // ---- helper teks ----
        public static Label NewLabel(Control parent, string text, int x, int y, int w, int h,
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

        public static Label AddText(Control parent, string text, int x, int y, int w, int h,
                                    float size, bool bold, Color color)
        {
            return NewLabel(parent, text, x, y, w, h, size,
                bold ? FontStyle.Bold : FontStyle.Regular, color,
                ContentAlignment.TopLeft, UI.FontName);
        }

        public static Label AddAligned(Control parent, string text, int x, int y, int w, int h,
                                       float size, bool bold, Color color, ContentAlignment align)
        {
            return NewLabel(parent, text, x, y, w, h, size,
                bold ? FontStyle.Bold : FontStyle.Regular, color, align, UI.FontName);
        }

        public static Label AddSymbol(Control parent, string glyph, int x, int y, int w, int h,
                                      float size, Color color)
        {
            return NewLabel(parent, glyph, x, y, w, h, size, FontStyle.Regular, color,
                ContentAlignment.MiddleCenter, "Segoe UI Symbol");
        }

        // Lebar teks seperti cara Label menggambar (padding bawaan TextRenderer).
        public static int LabelWidth(string text, float size, FontStyle style)
        {
            using (Font f = UI.Body(size, style))
            {
                return TextRenderer.MeasureText(text, f).Width;
            }
        }

        // Lebar teks untuk kontrol yang menggambar sendiri dengan NoPadding (PillButton, pil).
        public static int DrawnWidth(string text, float size, FontStyle style)
        {
            using (Font f = UI.Body(size, style))
            {
                return TextRenderer.MeasureText(text, f,
                    new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
            }
        }

        public static List<string> WrapLines(string text, Font font, int maxWidth)
        {
            List<string> lines = new List<string>();
            if (string.IsNullOrEmpty(text)) return lines;
            string[] words = text.Split(' ');
            string cur = "";
            foreach (string word in words)
            {
                string test = cur.Length == 0 ? word : cur + " " + word;
                if (TextRenderer.MeasureText(test, font).Width > maxWidth && cur.Length > 0)
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

        // ---- helper kontrol ----
        public static RoundPanel Card(Control parent, int x, int y, int w, int h, int radius,
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

        public static PillButton MakeButton(Control parent, string text, int x, int y, int h,
                                            Color fill, Color hover, Color fore, Color border,
                                            float size, int padding)
        {
            PillButton b = new PillButton();
            b.Text = text;
            b.Radius = 10;
            b.Fill = fill;
            b.HoverFill = hover;
            b.TextColor = fore;
            b.BorderColor = border;
            b.FocusColor = fore == Color.White ? Color.White : Navy;
            b.Font = UI.Body(size, FontStyle.Bold);
            b.Bounds = new Rectangle(x, y, DrawnWidth(text, size, FontStyle.Bold) + padding, h);
            parent.Controls.Add(b);
            return b;
        }

        // Tombol utama/sekunder yang bisa dinonaktifkan tanpa menulis warna di halaman.
        public static void SetEnabled(PillButton button, bool enabled,
                                      Color fill, Color hover, Color fore, Color border)
        {
            button.Enabled = enabled;
            button.TabStop = enabled;
            button.Fill = enabled ? fill : DisabledBg;
            button.HoverFill = enabled ? hover : DisabledBg;
            button.TextColor = enabled ? fore : DisabledInk;
            button.BorderColor = enabled ? border : DisabledBg;
            button.Cursor = enabled ? Cursors.Hand : Cursors.Default;
            button.Invalidate();
        }

        // Kotak isian: kontainer putih bergaris dengan TextBox tanpa border di dalamnya.
        public static TextBox MakeField(Control parent, int x, int y, int w, string hint)
        {
            RoundPanel box = Card(parent, x, y, w, FieldH, 8, Color.White, Line);

            TextBox input = new TextBox();
            input.BorderStyle = BorderStyle.None;
            input.Font = UI.Body(10f, FontStyle.Regular);
            input.ForeColor = Ink;
            input.BackColor = Color.White;
            input.AutoSize = false;
            input.Tag = box;
            box.Controls.Add(input);

            Label placeholder = AddAligned(box, hint, 14, 0, Math.Max(40, w - 28), FieldH, 10f, false,
                Muted, ContentAlignment.MiddleLeft);
            placeholder.Visible = string.IsNullOrEmpty(input.Text);

            void Place()
            {
                int h = input.PreferredHeight;
                input.SetBounds(14, Math.Max(0, (box.Height - h) / 2), Math.Max(40, box.Width - 28), h);
                placeholder.SetBounds(14, 0, Math.Max(40, box.Width - 28), box.Height);
            }

            Place();
            box.Resize += delegate (object? s, EventArgs e) { Place(); };
            input.TextChanged += delegate (object? s, EventArgs e)
            {
                placeholder.Visible = input.Text.Length == 0;
            };

            return input;
        }

        public static Panel Divider(Control parent, int y, int w)
        {
            Panel p = new Panel();
            p.Bounds = new Rectangle(0, y, w, 1);
            p.BackColor = Line;
            parent.Controls.Add(p);
            return p;
        }

        public static void Stretch(Control child, Control parent, int left, int right)
        {
            parent.Resize += delegate (object? s, EventArgs e)
            {
                child.SetBounds(left, child.Top, Math.Max(10, parent.Width - left - right), child.Height);
            };
        }

        public static void PinRight(Control child, Control parent, int right)
        {
            parent.Resize += delegate (object? s, EventArgs e)
            {
                child.Left = parent.Width - right - child.Width;
            };
        }

        public static void CenterIn(Control child, Control parent)
        {
            child.Left = Math.Max(0, (parent.Width - child.Width) / 2);
            parent.Resize += delegate (object? s, EventArgs e)
            {
                child.Left = Math.Max(0, (parent.Width - child.Width) / 2);
            };
        }

        public static void MakeClickable(Control c, EventHandler handler)
        {
            c.Cursor = Cursors.Hand;
            c.Click += handler;
            foreach (Control child in c.Controls)
            {
                MakeClickable(child, handler);
            }
        }

        // Tampilkan pesan kegagalan yang disediakan client API.
        public static string TechnicalDetail(Exception ex)
        {
            return ex.Message;
        }

        // Dialog konfirmasi kustom. Batal mendapat fokus awal, Enter = Batal, Esc = Batal.
        public static bool Confirm(IWin32Window owner, string title, string message,
                                   string actionText, bool danger)
        {
            using (AdminConfirmDialog dialog = new AdminConfirmDialog(title, message, actionText, danger))
            {
                return dialog.ShowDialog(owner) == DialogResult.OK;
            }
        }
    }

    internal sealed class AdminConfirmDialog : Form
    {
        public AdminConfirmDialog(string title, string message, string actionText, bool danger)
        {
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            ShowInTaskbar = false;
            KeyPreview = true;
            Font = UI.Body(9f, FontStyle.Regular);
            DoubleBuffered = true;

            const int w = 440;
            const int pad = 24;

            AdminUi.AddText(this, title, pad, pad, w - pad * 2, 26, 13f, true, AdminUi.Ink);

            List<string> lines;
            using (Font f = UI.Body(9.5f, FontStyle.Regular))
            {
                lines = AdminUi.WrapLines(message, f, w - pad * 2);
            }
            for (int i = 0; i < lines.Count; i++)
            {
                AdminUi.AddAligned(this, lines[i], pad, 58 + i * 20, w - pad * 2, 20, 9.5f, false,
                    AdminUi.SubInk, ContentAlignment.MiddleLeft);
            }

            int buttonsTop = 58 + Math.Max(1, lines.Count) * 20 + pad;

            PillButton action = AdminUi.MakeButton(this, actionText, 0, buttonsTop, 40,
                danger ? AdminUi.DangerInk : AdminUi.BtnBlue,
                danger ? AdminUi.DangerInk : AdminUi.BtnBlueHover,
                Color.White, Color.Empty, 10f, 40);
            action.Left = w - pad - action.Width;
            action.Click += delegate (object? s, EventArgs e)
            {
                DialogResult = DialogResult.OK;
                Close();
            };

            PillButton cancel = AdminUi.MakeButton(this, "Batal", 0, buttonsTop, 40,
                Color.White, AdminUi.HoverBg, AdminUi.Navy, AdminUi.Line, 10f, 40);
            cancel.Left = action.Left - 12 - cancel.Width;
            cancel.Click += delegate (object? s, EventArgs e)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            ClientSize = new Size(w, buttonsTop + 40 + pad);

            Shown += delegate (object? s, EventArgs e) { cancel.Focus(); };

            KeyDown += delegate (object? s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen p = new Pen(AdminUi.Line))
            {
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            }
        }
    }
}
