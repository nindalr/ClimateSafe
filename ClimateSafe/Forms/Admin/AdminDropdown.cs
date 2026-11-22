using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace ClimateSafe
{
    internal sealed class AdminDropdown : Control
    {
        public const int FieldHeight = 40;
        private const int RowHeight = 40;

        private readonly List<object> items = new List<object>();
        private readonly Font font = UI.Body(10.5f, FontStyle.Regular);
        private int selectedIndex = -1;
        private bool hot;
        private bool open;
        private DateTime closedAt = DateTime.MinValue;
        private DropdownPopup? popup;
        private AdminDropdownList? list;

        public Func<object, string> Format { get; set; } = o => o.ToString() ?? "";
        public event EventHandler? SelectedIndexChanged;

        public AdminDropdown()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            TabStop = true;
            Cursor = Cursors.Hand;
            Size = new Size(260, FieldHeight);
            AccessibleRole = AccessibleRole.ComboBox;
        }

        public IReadOnlyList<object> Items
        {
            get { return items; }
        }

        public object? SelectedItem
        {
            get { return selectedIndex >= 0 && selectedIndex < items.Count ? items[selectedIndex] : null; }
        }

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                int v = value < 0 || value >= items.Count ? -1 : value;
                if (v == selectedIndex) return;
                selectedIndex = v;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SetItems(IEnumerable<object> source)
        {
            CloseList();
            items.Clear();
            items.AddRange(source);
            selectedIndex = items.Count > 0 ? 0 : -1;
            Invalidate();
        }

        private void Step(int delta)
        {
            if (items.Count == 0) return;
            SelectedIndex = Math.Clamp(selectedIndex + delta, 0, items.Count - 1);
        }

        private void OpenList()
        {
            if (open || items.Count == 0 || !Enabled) return;

            AdminDropdownList l = new AdminDropdownList(items.Select(o => Format(o)).ToList(),
                selectedIndex, Width, RowHeight);
            l.Chosen += delegate (object? s, int index)
            {
                CloseList();
                SelectedIndex = index;
            };

            DropdownPopup p = new DropdownPopup();
            p.Padding = Padding.Empty;
            p.AutoSize = false;
            p.DropShadowEnabled = false;
            p.BackColor = Color.White;
            p.KeyHandler = HandleOpenKey;

            ToolStripControlHost host = new ToolStripControlHost(l);
            host.Margin = Padding.Empty;
            host.Padding = Padding.Empty;
            host.AutoSize = false;
            host.Size = l.Size;
            p.Items.Add(host);
            p.Size = l.Size;

            p.Closed += delegate (object? s, ToolStripDropDownClosedEventArgs e)
            {
                open = false;
                closedAt = DateTime.UtcNow;
                popup = null;
                list = null;
                Invalidate();
                if (IsHandleCreated) BeginInvoke(new Action(p.Dispose));
            };

            list = l;
            popup = p;
            open = true;
            p.Show(this, new Point(0, Height + 4));
            Invalidate();
        }

        private void CloseList()
        {
            if (popup != null) popup.Close();
        }

        private bool HandleOpenKey(Keys keyData)
        {
            if (list == null) return false;
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Down:
                    list.MoveHighlight(1);
                    return true;
                case Keys.Up:
                    list.MoveHighlight(-1);
                    return true;
                case Keys.Enter:
                case Keys.Space:
                    list.Commit();
                    return true;
                case Keys.Escape:
                    CloseList();
                    return true;
            }
            return false;
        }

        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key == Keys.Up || key == Keys.Down) return true;
            if (open && (key == Keys.Enter || key == Keys.Escape)) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (open)
            {
                if (HandleOpenKey(e.KeyCode))
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Down:
                    if (e.Alt) OpenList(); else Step(1);
                    e.Handled = true;
                    break;
                case Keys.Up:
                    Step(-1);
                    e.Handled = true;
                    break;
                case Keys.Enter:
                case Keys.Space:
                    OpenList();
                    e.Handled = true;
                    break;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (e.Button != MouseButtons.Left) return;
            if (open) CloseList();
            else if ((DateTime.UtcNow - closedAt).TotalMilliseconds > 250) OpenList();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            hot = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hot = false;
            Invalidate();
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
            Color back = Parent is RoundPanel rp ? rp.Fill : (Parent != null ? Parent.BackColor : Color.White);
            using (SolidBrush b = new SolidBrush(back))
            {
                g.FillRectangle(b, ClientRectangle);
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;
            bool active = Focused || open;
            Color border = active ? Theme.Navy : hot ? Theme.FieldBorderHover : Theme.FieldBorder;
            Rectangle r = active
                ? new Rectangle(1, 1, Width - 3, Height - 3)
                : new Rectangle(0, 0, Width - 1, Height - 1);

            using (GraphicsPath path = UI.RoundPath(r, 8))
            {
                using (SolidBrush b = new SolidBrush(Enabled ? Theme.FieldFill : AdminUi.DisabledBg))
                {
                    g.FillPath(b, path);
                }
                using (Pen p = new Pen(border, active ? 2f : 1f))
                {
                    g.DrawPath(p, path);
                }
            }

            object? current = SelectedItem;
            string text = current == null ? "" : Format(current);
            TextRenderer.DrawText(g, text, font,
                new Rectangle(16, 0, Math.Max(10, Width - 50), Height),
                Enabled ? Theme.Ink : AdminUi.DisabledInk,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

            float cx = Width - 16 - 5;
            float cy = Height / 2f;
            using (Pen p = new Pen(Theme.Muted, 2f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                p.LineJoin = LineJoin.Round;
                g.DrawLines(p, new PointF[]
                {
                    new PointF(cx - 5f, cy - 2f),
                    new PointF(cx, cy + 3f),
                    new PointF(cx + 5f, cy - 2f)
                });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (popup != null) popup.Close();
                font.Dispose();
            }
            base.Dispose(disposing);
        }

        private sealed class DropdownPopup : ToolStripDropDown
        {
            public Func<Keys, bool>? KeyHandler;

            protected override bool ProcessCmdKey(ref Message m, Keys keyData)
            {
                if (KeyHandler != null && KeyHandler(keyData)) return true;
                return base.ProcessCmdKey(ref m, keyData);
            }
        }
    }

    internal sealed class AdminDropdownList : Control
    {
        private readonly List<string> texts;
        private readonly int rowH;
        private readonly int selected;
        private readonly Font regular = UI.Body(10.5f, FontStyle.Regular);
        private readonly Font bold = UI.Body(10.5f, FontStyle.Bold);
        private int highlight;

        public event EventHandler<int>? Chosen;

        public AdminDropdownList(List<string> texts, int selected, int width, int rowH)
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            this.texts = texts;
            this.selected = selected;
            this.rowH = rowH;
            highlight = selected >= 0 ? selected : 0;
            BackColor = Color.White;
            Cursor = Cursors.Hand;
            Size = new Size(width, texts.Count * rowH + 2);
        }

        public void MoveHighlight(int delta)
        {
            if (texts.Count == 0) return;
            highlight = Math.Clamp(highlight + delta, 0, texts.Count - 1);
            Invalidate();
        }

        public void Commit()
        {
            if (highlight >= 0 && highlight < texts.Count) Chosen?.Invoke(this, highlight);
        }

        private int IndexAt(int y)
        {
            return Math.Clamp((y - 1) / rowH, 0, Math.Max(0, texts.Count - 1));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int i = IndexAt(e.Y);
            if (i != highlight)
            {
                highlight = i;
                Invalidate();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left) return;
            highlight = IndexAt(e.Y);
            Commit();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.Clear(Color.White);

            for (int i = 0; i < texts.Count; i++)
            {
                Rectangle row = new Rectangle(1, 1 + i * rowH, Width - 2, rowH);
                if (i == highlight)
                {
                    using (SolidBrush b = new SolidBrush(Theme.IconBlue))
                    {
                        g.FillRectangle(b, row);
                    }
                }
                bool on = i == selected;
                TextRenderer.DrawText(g, texts[i], on ? bold : regular,
                    new Rectangle(row.X + 15, row.Y, Math.Max(10, row.Width - 31), row.Height),
                    on ? Theme.Navy : Theme.Ink,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }

            using (Pen p = new Pen(Theme.FieldBorder))
            {
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                regular.Dispose();
                bold.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}