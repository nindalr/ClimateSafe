using System;
using System.Windows.Forms;

namespace ClimateSafe.Auth
{
    public class FormRegister : Form
    {
        private readonly AuthService _auth=new AuthService();
        private readonly TextBox txtName=new TextBox(), txtEmail=new TextBox(), txtPassword=new TextBox(), txtConfirm=new TextBox();
        private readonly Button btnRegister=new AuthRoundedButton();
        public FormRegister()
        {
            var card=AuthUi.Setup(this,"Register",690,true);
            card.Controls.Add(AuthUi.Label("Buat akun baru",32,100,368,35,18,AuthUi.Ink,true));
            card.Controls.Add(AuthUi.Label("Mulai langkah menuju lingkungan lebih aman.",32,137,368,30,10,AuthUi.Muted));
            AuthUi.Field(card,txtName,"Nama lengkap","Masukkan nama lengkap",176);
            AuthUi.Field(card,txtEmail,"Email","nama@email.com",260);
            AuthUi.Field(card,txtPassword,"Password","Buat password",344,true);
            AuthUi.Field(card,txtConfirm,"Konfirmasi password","Ulangi password",428,true);
            AuthUi.Button(card,btnRegister,"Daftar",530); btnRegister.Click+=btnRegister_Click;
            var back=new AuthRoundedButton(); AuthUi.Button(card,back,"Sudah punya akun? Masuk",582,false); back.Click+=(_,_)=>Close();
            AcceptButton=btnRegister; ActiveControl=txtName;
        }
        private void btnRegister_Click(object? sender,EventArgs e)
        {
            if(string.IsNullOrWhiteSpace(txtName.Text)) { AuthUi.Warn(this,"Nama wajib diisi."); txtName.Focus(); return; }
            if(!AuthUi.ValidEmail(txtEmail.Text.Trim())) { AuthUi.Warn(this,"Masukkan alamat email yang valid."); txtEmail.Focus(); return; }
            if(string.IsNullOrWhiteSpace(txtPassword.Text)) { AuthUi.Warn(this,"Password wajib diisi."); txtPassword.Focus(); return; }
            if(txtPassword.Text!=txtConfirm.Text) { AuthUi.Warn(this,"Konfirmasi password tidak cocok."); txtConfirm.Focus(); return; }
            try {
                bool ok=_auth.Register(txtName.Text.Trim(),txtEmail.Text.Trim(),txtPassword.Text);
                if(ok) {
                    RecentAccounts.Add(txtEmail.Text.Trim());
                    MessageBox.Show(this,"Registrasi berhasil, silakan login.","ClimateSafe",MessageBoxButtons.OK,MessageBoxIcon.Information);
                    Close();
                }
                else AuthUi.Warn(this,"Akun dengan email ini sudah ada.");
            } catch(Exception ex) { System.Diagnostics.Debug.WriteLine(ex); MessageBox.Show(this,"Registrasi gagal. Periksa data dan koneksi Anda.","ClimateSafe",MessageBoxButtons.OK,MessageBoxIcon.Error); }
        }
    }
}