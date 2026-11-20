using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ClimateSafe
{
    // Bar skor dimensi dengan garis target tegak. Tinggi kontrol 16, bar 8 di tengah vertikal.
    internal class TargetBar : Control
    {
        private const int BarHeight = 8;

        public float Value = 0f;    // 0..1, skor asli dibagi 100
        public float Target = 0.75f;
        public Color BarColor = Color.SteelBlue;
        public Color TrackColor = Color.FromArgb(226, 234, 245);
        public Color TargetColor = Color.FromArgb(30, 41, 59);

        public TargetBar()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Width < 4 || Height < BarHeight) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int top = (Height - BarHeight) / 2;
            int rad = BarHeight / 2;
            Rectangle trackRect = new Rectangle(0, top, Width - 1, BarHeight);

            using (GraphicsPath track = UI.RoundPath(trackRect, rad))
            using (SolidBrush b = new SolidBrush(TrackColor))
            {
                g.FillPath(b, track);
            }

            float v = Math.Max(0f, Math.Min(1f, Value));
            int w = (int)((Width - 1) * v);
            if (w > 0)
            {
                w = Math.Max(w, BarHeight);
                using (GraphicsPath fill = UI.RoundPath(new Rectangle(0, top, w, BarHeight), rad))
                using (SolidBrush b = new SolidBrush(BarColor))
                {
                    g.FillPath(b, fill);
                }
            }

            float t = Math.Max(0f, Math.Min(1f, Target));
            int tx = (int)((Width - 1) * t);
            tx = Math.Min(tx, Width - 2);
            g.SmoothingMode = SmoothingMode.None;
            using (SolidBrush b = new SolidBrush(TargetColor))
            {
                g.FillRectangle(b, tx, 0, 2, Height);
            }
        }
    }
}
