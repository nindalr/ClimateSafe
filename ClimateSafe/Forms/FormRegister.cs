using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClimateSafe.Auth
{
    public class FormRegister : Form
    {
        private readonly AuthService _auth = new AuthService();
        private readonly TextBox txtName = new TextBox();
        private readonly TextBox txtEmail = new TextBox();
        private readonly TextBox txtPassword = new TextBox();
        private readonly Button btnRegister = new Button();

        public FormRegister()
        {
            Text = "ClimateSafe - Register";
            ClientSize = new Size(340, 240);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Label lblName = new Label();
            lblName.Text = "Nama";
            lblName.Location = new Point(20, 25);
            lblName.AutoSize = true;
            txtName.Location = new Point(110, 22);
            txtName.Width = 210;

            Label lblEmail = new Label();
            lblEmail.Text = "Email";
            lblEmail.Location = new Point(20, 65);
            lblEmail.AutoSize = true;
            txtEmail.Location = new Point(110, 62);
            txtEmail.Width = 210;

            Label lblPass = new Label();
            lblPass.Text = "Password";
            lblPass.Location = new Point(20, 105);
            lblPass.AutoSize = true;
            txtPassword.Location = new Point(110, 102);
            txtPassword.Width = 210;
            txtPassword.UseSystemPasswordChar = true;

            btnRegister.Text = "Register";
            btnRegister.Location = new Point(110, 150);
            btnRegister.Size = new Size(110, 32);
            btnRegister.Click += btnRegister_Click;

            Controls.Add(lblName);
            Controls.Add(txtName);
            Controls.Add(lblEmail);
            Controls.Add(txtEmail);
            Controls.Add(lblPass);
            Controls.Add(txtPassword);
            Controls.Add(btnRegister);

            AcceptButton = btnRegister;
        }

        private void btnRegister_Click(object? sender, EventArgs e)
        {
            try
            {
                bool ok = _auth.Register(txtName.Text, txtEmail.Text, txtPassword.Text);
                if (ok)
                {
                    MessageBox.Show("Registrasi berhasil, silakan login", "Well Done!",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Close();
                }
                else
                {
                    MessageBox.Show("Akun dengan email ini sudah ada", "Register FAIL!!",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:" + ex.Message, "Register FAIL!!",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
