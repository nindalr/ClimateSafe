using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Net.Mail;

namespace ClimateSafe.Auth
{
    internal static class AuthUi
    {
        internal static readonly Color Bg = ColorTranslator.FromHtml("#F2F6FA"), Ink = ColorTranslator.FromHtml("#365B7D"), Muted = ColorTranslator.FromHtml("#718BA5"), Coral = ColorTranslator.FromHtml("#E98578"), Blue = ColorTranslator.FromHtml("#3F78B5"), Border = ColorTranslator.FromHtml("#D8E3EF");
        internal static Font Font(float size, FontStyle style = FontStyle.Regular) => new Font("Segoe UI", size, style);
        internal static Label Label(string text, int x, int y, int w, int h, float size, Color color, bool bold = false) => new Label { Text = text, Bounds = new Rectangle(x,y,w,h), Font = Font(size,bold ? FontStyle.Bold : FontStyle.Regular), ForeColor = color, TextAlign = ContentAlignment.MiddleLeft, UseCompatibleTextRendering = false, AutoSize = false };
        internal static GraphicsPath RoundedPath(RectangleF bounds, float radius)
        {
            var path = new GraphicsPath();
            float d = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            if (d <= 0) { path.AddRectangle(bounds); return path; }
            float x = bounds.X, y = bounds.Y, w = bounds.Width, h = bounds.Height;
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + w - d, y, d, d, 270, 90);
            path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            path.AddArc(x, y + h - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        internal static void Round(Control c, int r)
        {
            using var p = new GraphicsPath(); int w=c.Width, h=c.Height, d=r*2;
            p.AddArc(0,0,d,d,180,90); p.AddArc(w-d,0,d,d,270,90); p.AddArc(w-d,h-d,d,d,0,90); p.AddArc(0,h-d,d,d,90,90); p.CloseFigure();
            var old=c.Region; c.Region=new Region(p); old?.Dispose();
        }
        internal static Panel Setup(Form f, string title, int height, bool register)
        {
            f.Text="ClimateSafe - "+title; f.ClientSize=new Size(480,height); f.StartPosition=register?FormStartPosition.CenterParent:FormStartPosition.CenterScreen; f.FormBorderStyle=FormBorderStyle.FixedDialog; f.MaximizeBox=false; f.MinimizeBox=false; f.BackColor=Bg; f.Font=Font(10); f.AutoScaleMode=AutoScaleMode.None;
            var card=new Panel { Bounds=new Rectangle(24,24,432,height-48), BackColor=Color.White }; Round(card,16); f.Controls.Add(card);
            var brandIcon = new ClimateBrandIcon { Bounds = new Rectangle(32, 26, 44, 44), TabStop = false };
            card.Controls.Add(brandIcon);
            card.Controls.Add(Label("ClimateSafe",86,15,314,43,23,Ink,true));
            card.Controls.Add(Label("Siap hari ini, lebih aman nanti",86,58,314,25,10,Muted));
            return card;
        }
        internal static void Field(Panel card, TextBox box, string label, string placeholder, int y, bool password=false)
        {
            card.Controls.Add(Label(label,32,y,368,24,10,Ink,true));
            var frame=new Panel { Bounds=new Rectangle(32,y+29,368,44), BackColor=Border };
            var inner=new Panel { Bounds=new Rectangle(1,1,366,42), BackColor=Color.White }; frame.Controls.Add(inner); card.Controls.Add(frame);
            Round(frame,8); Round(inner,7);
            box.BorderStyle=BorderStyle.None; box.Font=Font(11); box.AutoSize=true; box.Width=password?275:342; box.Location=new Point(12, (inner.Height-box.PreferredHeight)/2); box.ForeColor=Ink; box.PlaceholderText=placeholder; box.UseSystemPasswordChar=password; inner.Controls.Add(box);
            if(password) { var toggle=new AuthRoundedButton { CornerRadius=6, Text="Lihat", Bounds=new Rectangle(292,4,70,34), FlatStyle=FlatStyle.Flat, ForeColor=Blue, BackColor=Color.White, Cursor=Cursors.Hand, TabStop=true }; toggle.FlatAppearance.BorderSize=0; toggle.Click+=(_,_)=>{box.UseSystemPasswordChar=!box.UseSystemPasswordChar; toggle.Text=box.UseSystemPasswordChar?"Lihat":"Tutup";}; inner.Controls.Add(toggle); }
        }
        internal static void Button(Panel card, Button b, string text, int y, bool primary=true)
        {
            b.Text=text; b.Bounds=new Rectangle(32,y,368,42); b.Font=Font(11,FontStyle.Bold); b.FlatStyle=FlatStyle.Flat; b.BackColor=primary?Coral:Bg; b.ForeColor=primary?Color.White:Ink; b.FlatAppearance.BorderSize=0; b.Cursor=Cursors.Hand; b.FlatAppearance.MouseOverBackColor=primary?ColorTranslator.FromHtml("#D9796D"):ColorTranslator.FromHtml("#E5EDF5"); card.Controls.Add(b);
        }
        internal static bool ValidEmail(string email) => MailAddress.TryCreate(email,out var address) && string.Equals(address.Address,email,StringComparison.OrdinalIgnoreCase);
        internal static void Warn(Form f,string message) => MessageBox.Show(f,message,"ClimateSafe",MessageBoxButtons.OK,MessageBoxIcon.Warning);
    }
    // Native Button behavior, with custom painting to avoid the square WinForms focus border.
    internal sealed class AuthRoundedButton : Button
    {
        private bool _hover;
        private bool _pressed;
        internal int CornerRadius { get; set; } = 8;

        public AuthRoundedButton()
        {
            FlatStyle = FlatStyle.Flat;
            UseVisualStyleBackColor = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Do not call base.OnPaint: it draws a rectangular focus/default outline.
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (Width < 6 || Height < 6) return;

            Color fill = BackColor;
            if (!Enabled) fill = AuthUi.Bg;
            else if (_pressed) fill = ControlPaint.Dark(BackColor, 0.08f);
            else if (_hover && !FlatAppearance.MouseOverBackColor.IsEmpty)
                fill = FlatAppearance.MouseOverBackColor;

            using (var shape = AuthUi.RoundedPath(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), CornerRadius))
            using (var brush = new SolidBrush(fill))
                g.FillPath(brush, shape);

            var textColor = Enabled ? ForeColor : AuthUi.Muted;
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);

            // Keep keyboard focus visible, but use an inset rounded stroke.
            if (Focused && Enabled)
            {
                using var focus = AuthUi.RoundedPath(new RectangleF(2f, 2f, Width - 4f, Height - 4f), Math.Max(1, CornerRadius - 2));
                using var pen = new Pen(AuthUi.Ink, 1.5f);
                g.DrawPath(pen, focus);
            }
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = false; _pressed = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) _pressed = true; Invalidate(); }
        protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; base.OnMouseUp(e); Invalidate(); }
        protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e); if (!Capture) { _pressed = false; Invalidate(); } }
        protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); if (e.KeyCode == Keys.Space) { _pressed = true; Invalidate(); } }
        protected override void OnKeyUp(KeyEventArgs e) { _pressed = false; base.OnKeyUp(e); Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); _pressed = false; Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
    }

    // Small vector sunrise mark inspired by the dashboard reference; no external asset required.
    internal sealed class ClimateBrandIcon : Control
    {
        public ClimateBrandIcon()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Color.White);
            if (Width < 4 || Height < 4) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var state = g.Save();
            g.ScaleTransform(Width / 44f, Height / 44f);
            using var tile = AuthUi.RoundedPath(new RectangleF(0.5f, 0.5f, 43f, 43f), 13f);
            using var tileBrush = new SolidBrush(AuthUi.Bg);
            g.FillPath(tileBrush, tile);
            using var sun = new SolidBrush(AuthUi.Coral);
            g.FillPie(sun, 14, 15, 16, 16, 180, 180);
            using var rays = new Pen(AuthUi.Coral, 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(rays, 22, 8, 22, 11);
            g.DrawLine(rays, 12, 12, 14, 15);
            g.DrawLine(rays, 32, 12, 30, 15);
            g.DrawLine(rays, 8, 20, 11, 20);
            g.DrawLine(rays, 33, 20, 36, 20);
            g.DrawLine(rays, 10, 24, 34, 24);
            using var waves = new Pen(AuthUi.Blue, 2.3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(waves, 12, 29, 32, 29);
            g.DrawLine(waves, 17, 34, 27, 34);
            g.Restore(state);
        }
    }

    public class FormLogin : Form
    {
        private readonly AuthService _auth = new AuthService();
        private readonly TextBox txtEmail = new TextBox(), txtPassword = new TextBox();
        private readonly Button btnLogin = new AuthRoundedButton(), btnRegister = new AuthRoundedButton();
        private readonly Button btnGuest = new AuthRoundedButton();
        public FormLogin()
        {
            var card=AuthUi.Setup(this,"Login",630,false);
            card.Controls.Add(AuthUi.Label("Selamat datang kembali",32,100,368,35,18,AuthUi.Ink,true));
            card.Controls.Add(AuthUi.Label("Masuk untuk melanjutkan kesiapsiagaan Anda.",32,137,368,30,10,AuthUi.Muted));
            AuthUi.Field(card,txtEmail,"Email","nama@email.com",184);
            AuthUi.Field(card,txtPassword,"Password","Masukkan password",272,true);
            AuthUi.Button(card,btnLogin,"Masuk",370); btnLogin.Click+=btnLogin_Click;
            AuthUi.Button(card,btnRegister,"Belum punya akun? Daftar",422,false); btnRegister.Click+=btnRegister_Click;
            // Separate guest access from account authentication.
            card.Controls.Add(new Panel { Bounds = new Rectangle(32,486,160,1), BackColor = AuthUi.Border, TabStop = false });
            var separator = AuthUi.Label("atau",192,475,48,23,10,AuthUi.Muted);
            separator.TextAlign = ContentAlignment.MiddleCenter;
            separator.TabStop = false;
            card.Controls.Add(separator);
            card.Controls.Add(new Panel { Bounds = new Rectangle(240,486,160,1), BackColor = AuthUi.Border, TabStop = false });
            AuthUi.Button(card,btnGuest,"Lanjut tanpa login",514,false);
            btnGuest.Click+=(_,_)=>{
                MessageBox.Show(this,"Asesmen tamu dapat digunakan tanpa internet. Hasil dan checklist hanya tersimpan selama aplikasi terbuka. Edukasi offline menggunakan materi bawaan yang mungkin tidak terbaru.","Tanpa Login",MessageBoxButtons.OK,MessageBoxIcon.Information);
                Nav.Reset(); Session.CurrentUser=null; Session.IsGuest=true; Session.GuestAssessment=null; ApiClient.Token=null;
                var form=new FrmPilihBahaya(); form.Show(); Hide();
            };
            AcceptButton=btnLogin; ActiveControl=txtEmail;
        }
        private async void btnLogin_Click(object? sender, EventArgs e)
        {
            if(!AuthUi.ValidEmail(txtEmail.Text.Trim())) { AuthUi.Warn(this,"Masukkan alamat email yang valid."); txtEmail.Focus(); return; }
            if(string.IsNullOrWhiteSpace(txtPassword.Text)) { AuthUi.Warn(this,"Password wajib diisi."); txtPassword.Focus(); return; }
            string email = txtEmail.Text.Trim(), password = txtPassword.Text;
            btnLogin.Enabled = btnRegister.Enabled = btnGuest.Enabled = false;
            txtEmail.Enabled = txtPassword.Enabled = false;
            btnLogin.Text = "Sedang masuk...";
            UseWaitCursor = true;
            try {
                UserAccount? user=await Task.Run(() => _auth.Login(email,password));
                if (IsDisposed) { ApiClient.Token = null; return; }
                if(user==null) { AuthUi.Warn(this,"Email/password salah atau akun nonaktif."); return; }
                Session.IsGuest=false; Session.GuestAssessment=null;
                Form main=user.CreateMainForm(); main.FormClosed+=(s,args)=>{user.Logout(); txtPassword.Clear(); Show();}; main.Show(); Hide();
            } catch(Exception ex) { if (!IsDisposed) { System.Diagnostics.Debug.WriteLine(ex); MessageBox.Show(this,"Login gagal. Periksa koneksi atau coba lagi.","ClimateSafe",MessageBoxButtons.OK,MessageBoxIcon.Error); } }
            finally { if (!IsDisposed) { btnLogin.Enabled = btnRegister.Enabled = btnGuest.Enabled = true; txtEmail.Enabled = txtPassword.Enabled = true; btnLogin.Text = "Masuk"; UseWaitCursor = false; } }
        }
        private void btnRegister_Click(object? sender,EventArgs e) { using var reg=new FormRegister(); reg.ShowDialog(this); }
    }
}
