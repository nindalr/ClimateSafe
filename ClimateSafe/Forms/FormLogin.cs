using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClimateSafe.Auth
{
    public class FormLogin : Form
    {
        private readonly AuthService _auth = new AuthService();
        private readonly TextBox txtEmail = new TextBox();
        private readonly TextBox txtPassword = new TextBox();
        private readonly Button btnLogin = new Button();
        private readonly Button btnRegister = new Button();

        public FormLogin()
        {
            Text = "ClimateSafe - Login";
            ClientSize = new Size(340, 250);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            Label title = new Label();
            title.Text = "ClimateSafe";
            title.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            title.Location = new Point(20, 15);
            title.AutoSize = true;

            Label lblEmail = new Label();
            lblEmail.Text = "Email";
            lblEmail.Location = new Point(20, 70);
            lblEmail.AutoSize = true;
            txtEmail.Location = new Point(110, 67);
            txtEmail.Width = 210;

            Label lblPass = new Label();
            lblPass.Text = "Password";
            lblPass.Location = new Point(20, 110);
            lblPass.AutoSize = true;
            txtPassword.Location = new Point(110, 107);
            txtPassword.Width = 210;
            txtPassword.UseSystemPasswordChar = true;

            btnLogin.Text = "Login";
            btnLogin.Location = new Point(110, 150);
            btnLogin.Size = new Size(100, 32);
            btnLogin.Click += btnLogin_Click;

            btnRegister.Text = "Register";
            btnRegister.Location = new Point(220, 150);
            btnRegister.Size = new Size(100, 32);
            btnRegister.Click += btnRegister_Click;

            Controls.Add(title);
            Controls.Add(lblEmail);
            Controls.Add(txtEmail);
            Controls.Add(lblPass);
            Controls.Add(txtPassword);
            Controls.Add(btnLogin);
            Controls.Add(btnRegister);

            Button guest = new Button { Text = "Tanpa Login", Location = new Point(110, 195), Size = new Size(210, 32) };
            guest.Click += (_, _) =>
            {
                MessageBox.Show(this, "Asesmen tamu dapat digunakan tanpa internet. Hasil dan checklist hanya tersimpan selama aplikasi terbuka. Edukasi masih memerlukan koneksi API.",
                    "Tanpa Login", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Nav.Reset();
                Session.CurrentUser = null;
                Session.IsGuest = true;
                Session.GuestAssessment = null;
                ApiClient.Token = null;
                var form = new FrmPilihBahaya();
                form.Show();
                Hide();
            };
            Controls.Add(guest);

            AcceptButton = btnLogin;
        }

        private void btnLogin_Click(object? sender, EventArgs e)
        {
            try
            {
                UserAccount? user = _auth.Login(txtEmail.Text, txtPassword.Text);
                if (user == null)
                {
                    MessageBox.Show("Email/password salah atau akun nonaktif",
                        "Login FAIL!!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // POLYMORPHISM: User -> dashboard, Admin -> panel admin
                Session.IsGuest = false;
                Session.GuestAssessment = null;
                Form main = user.CreateMainForm();
                main.FormClosed += (s, args) =>
                {
                    user.Logout();
                    txtPassword.Clear();
                    Show();
                };
                main.Show();
                Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:" + ex.Message, "Login FAIL!!",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRegister_Click(object? sender, EventArgs e)
        {
            using (FormRegister reg = new FormRegister())
            {
                reg.ShowDialog(this);
            }
        }
    }
}
