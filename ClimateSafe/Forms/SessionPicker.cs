using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClimateSafe
{
    // Daftar pilihan yang digambar sendiri. Tidak bisa menerima fokus:
    // seluruh papan tombol ditangani oleh SessionPicker.
    internal class SessionPopup : Control
    {
        public const int RowHeight = 46;
        private const int PadX = 16;

        public List<string> Items = new List<string>();
        public int SelectedIndex = -1;
        public int HighlightIndex = -1;

        public Color Line = Color.FromArgb(226, 232, 240);
        public Color Ink = Color.FromArgb(30, 41, 59);
        public Color Navy = Color.FromArgb(37, 88, 150);
        public Color SelectedBack = Color.FromArgb(226, 236, 250);
        public Color HoverBack = Color.FromArgb(250, 252, 255);

        public event EventHandler<int> ItemChosen;

        public SessionPopup()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            BackColor = Color.White;
        }

        public int RowAt(int y)
        {
            if (Items.Count == 0) return -1;
            int index = y / RowHeight;
            if (index < 0 || index >= Items.Count) return -1;
            return index;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index = RowAt(e.Y);
            if (index != HighlightIndex)
            {
                HighlightIndex = index;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (HighlightIndex != SelectedIndex)
            {
                HighlightIndex = SelectedIndex;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int index = RowAt(e.Y);
            if (index >= 0 && ItemChosen != null) ItemChosen(this, index);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Width < 8 || Height < 8) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle outer = new Rectangle(0, 0, Width - 1, Height - 1);

            using (GraphicsPath shape = UI.RoundPath(outer, 12))
            {
                using (SolidBrush white = new SolidBrush(Color.White))
                {
                    g.FillPath(white, shape);
                }

                Region old = g.Clip;
                g.SetClip(shape, CombineMode.Intersect);

                for (int i = 0; i < Items.Count; i++)
                {
                    Rectangle row = new Rectangle(0, i * RowHeight, Width, RowHeight);
                    bool selected = (i == SelectedIndex);

                    if (selected)
                    {
                        using (SolidBrush b = new SolidBrush(SelectedBack)) g.FillRectangle(b, row);
                    }
                    else if (i == HighlightIndex)
                    {
                        using (SolidBrush b = new SolidBrush(HoverBack)) g.FillRectangle(b, row);
                    }

                    using (Font f = UI.Body(10.5f, selected ? FontStyle.Bold : FontStyle.Regular))
                    {
                        TextRenderer.DrawText(g, Items[i], f,
                            new Rectangle(row.X + PadX, row.Y, row.Width - PadX * 2 - 24, row.Height),
                            selected ? Navy : Ink,
                            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                    }

                    if (selected)
                    {
                        using (Font f = UI.MakeFont("Segoe UI Symbol", 10.5f, FontStyle.Regular))
                        {
                            TextRenderer.DrawText(g, "✓", f,
                                new Rectangle(row.Right - PadX - 24, row.Y, 24, row.Height), Navy,
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                                TextFormatFlags.NoPadding);
                        }
                    }
                }

                g.Clip = old;

                using (Pen p = new Pen(Line))
                {
                    g.DrawPath(p, shape);
                }
            }
        }
    }

    // Pemilih sesi asesmen: kotak tertutup + popover yang muncul tepat di bawahnya.
    internal class SessionPicker : Control, IMessageFilter
    {
        private const int PadX = 16;
        private const int CaretBox = 16;

        private readonly List<string> labels = new List<string>();
        private SessionPopup popup;
        private int selectedIndex = -1;
        private bool filterOn;

        public Color Line = Color.FromArgb(226, 232, 240);
        public Color Ink = Color.FromArgb(30, 41, 59);
        public Color Muted = Color.FromArgb(100, 116, 139);
        public Color Navy = Color.FromArgb(37, 88, 150);

        public event EventHandler SelectedIndexChanged;

        public SessionPicker()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }

        public bool IsOpen
        {
            get { return popup != null; }
        }

        public int SelectedIndex
        {
            get { return selectedIndex; }
        }

        public void SetItems(IList<string> items, int selected)
        {
            labels.Clear();
            if (items != null) labels.AddRange(items);
            selectedIndex = (selected >= 0 && selected < labels.Count) ? selected : (labels.Count > 0 ? 0 : -1);
            Invalidate();
        }

        // ---------- popover ----------

        public void OpenPopup()
        {
            if (popup != null || labels.Count == 0 || Parent == null) return;

            popup = new SessionPopup();
            popup.Line = Line;
            popup.Ink = Ink;
            popup.Navy = Navy;
            popup.Items.AddRange(labels);
            popup.SelectedIndex = selectedIndex;
            popup.HighlightIndex = selectedIndex;
            popup.Bounds = new Rectangle(Left, Bottom + 4, Width, labels.Count * SessionPopup.RowHeight);
            popup.ItemChosen += PopupItemChosen;

            Parent.Controls.Add(popup);
            popup.BringToFront();

            if (!filterOn)
            {
                Application.AddMessageFilter(this);
                filterOn = true;
            }
            Invalidate();
        }

        public void ClosePopup()
        {
            if (filterOn)
            {
                Application.RemoveMessageFilter(this);
                filterOn = false;
            }
            if (popup != null)
            {
                SessionPopup old = popup;
                popup = null;
                old.ItemChosen -= PopupItemChosen;
                if (old.Parent != null) old.Parent.Controls.Remove(old);
                old.Dispose();
            }
            Invalidate();
        }

        private void PopupItemChosen(object sender, int index)
        {
            Choose(index);
        }

        private void Choose(int index)
        {
            ClosePopup();
            Focus();
            if (index < 0 || index >= labels.Count || index == selectedIndex) return;
            selectedIndex = index;
            Invalidate();
            if (SelectedIndexChanged != null) SelectedIndexChanged(this, EventArgs.Empty);
        }

        private void MoveHighlight(int delta)
        {
            if (popup == null) return;
            int next = popup.HighlightIndex + delta;
            if (next < 0) next = 0;
            if (next > labels.Count - 1) next = labels.Count - 1;
            if (next != popup.HighlightIndex)
            {
                popup.HighlightIndex = next;
                popup.Invalidate();
            }
        }

        // ---------- interaksi ----------

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (popup == null) OpenPopup(); else ClosePopup();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key == Keys.Up || key == Keys.Down || key == Keys.Enter ||
                key == Keys.Space || key == Keys.Escape)
            {
                return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (popup == null)
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space || e.KeyCode == Keys.Down)
                {
                    OpenPopup();
                    e.Handled = true;
                }
                return;
            }

            if (e.KeyCode == Keys.Down) { MoveHighlight(1); e.Handled = true; }
            else if (e.KeyCode == Keys.Up) { MoveHighlight(-1); e.Handled = true; }
            else if (e.KeyCode == Keys.Enter) { Choose(popup.HighlightIndex); e.Handled = true; }
            else if (e.KeyCode == Keys.Escape) { ClosePopup(); Focus(); e.Handled = true; }
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

        public bool PreFilterMessage(ref Message m)
        {
            // WM_LBUTTONDOWN, WM_RBUTTONDOWN, WM_MBUTTONDOWN, WM_NCLBUTTONDOWN
            if (popup != null &&
                (m.Msg == 0x201 || m.Msg == 0x204 || m.Msg == 0x207 || m.Msg == 0x00A1))
            {
                Point screen = Cursor.Position;
                if (!Covers(this, screen) && !Covers(popup, screen))
                {
                    ClosePopup();
                }
            }
            return false;
        }

        private static bool Covers(Control c, Point screen)
        {
            if (c == null || c.IsDisposed || !c.IsHandleCreated) return false;
            return c.RectangleToScreen(c.ClientRectangle).Contains(screen);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            ClosePopup();
            base.OnHandleDestroyed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ClosePopup();
            base.Dispose(disposing);
        }

        // ---------- gambar ----------

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Width < 16 || Height < 8) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle outer = new Rectangle(0, 0, Width - 1, Height - 1);
            bool active = (popup != null) || Focused;

            using (GraphicsPath shape = UI.RoundPath(outer, 10))
            {
                using (SolidBrush white = new SolidBrush(Color.White))
                {
                    g.FillPath(white, shape);
                }
                using (Pen p = new Pen(active ? Navy : Line, active ? 2f : 1f))
                {
                    g.DrawPath(p, shape);
                }
            }

            string text = (selectedIndex >= 0 && selectedIndex < labels.Count) ? labels[selectedIndex] : "";
            using (Font f = UI.Body(11f, FontStyle.Regular))
            {
                TextRenderer.DrawText(g, text, f,
                    new Rectangle(PadX, 0, Math.Max(10, Width - PadX * 2 - CaretBox - 8), Height), Ink,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }

            DrawCaret(g, new Rectangle(Width - PadX - CaretBox, (Height - CaretBox) / 2, CaretBox, CaretBox),
                popup != null);
        }

        private void DrawCaret(Graphics g, Rectangle box, bool up)
        {
            float cx = box.X + box.Width / 2f;
            float cy = box.Y + box.Height / 2f;
            using (GraphicsPath path = new GraphicsPath())
            using (Pen p = new Pen(up ? Navy : Muted, 1.8f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                p.LineJoin = LineJoin.Round;
                if (up)
                {
                    path.AddLines(new PointF[]
                    {
                        new PointF(cx - 5f, cy + 2.5f),
                        new PointF(cx, cy - 2.5f),
                        new PointF(cx + 5f, cy + 2.5f)
                    });
                }
                else
                {
                    path.AddLines(new PointF[]
                    {
                        new PointF(cx - 5f, cy - 2.5f),
                        new PointF(cx, cy + 2.5f),
                        new PointF(cx + 5f, cy - 2.5f)
                    });
                }
                g.DrawPath(p, path);
            }
        }
    }
}
