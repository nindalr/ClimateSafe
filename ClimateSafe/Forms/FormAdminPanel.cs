using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace ClimateSafe.Auth
{
    public class FormAdminPanel : Form
    {
        private readonly UserAccount _admin;
        private readonly AdminService _adminService = new AdminService();

        private readonly DataGridView dgvData = new DataGridView();
        private readonly TextBox txtName = new TextBox();
        private readonly Button btnLoad = new Button();
        private readonly Button btnUpdate = new Button();
        private readonly Button btnToggle = new Button();
        private readonly Button btnDelete = new Button();
        private readonly Button btnResetPassword = new Button();

        private int selectedId = -1;
        private string selectedName = "";

        public FormAdminPanel(UserAccount admin)
        {
            _admin = admin;

            Text = "ClimateSafe - Panel Admin";
            ClientSize = new Size(780, 480);
            StartPosition = FormStartPosition.CenterScreen;

            Label title = new Label();
            title.Text = "Manajemen Pengguna (Admin: " + admin.Name + ")";
            title.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            title.Location = new Point(20, 15);
            title.AutoSize = true;

            Label lblName = new Label();
            lblName.Text = "Nama";
            lblName.Location = new Point(20, 65);
            lblName.AutoSize = true;
            txtName.Location = new Point(70, 62);
            txtName.Width = 200;

            btnUpdate.Text = "Update Nama";
            btnUpdate.Location = new Point(290, 58);
            btnUpdate.Size = new Size(110, 32);
            btnUpdate.Click += btnUpdate_Click;

            btnToggle.Text = "Aktif/Nonaktif";
            btnToggle.Location = new Point(410, 58);
            btnToggle.Size = new Size(120, 32);
            btnToggle.Click += btnToggle_Click;

            btnDelete.Text = "Delete";
            btnDelete.Location = new Point(540, 58);
            btnDelete.Size = new Size(100, 32);
            btnDelete.Click += btnDelete_Click;

            btnLoad.Text = "LoadData";
            btnLoad.Location = new Point(650, 58);
            btnLoad.Size = new Size(100, 32);
            btnLoad.Click += btnLoad_Click;

            btnResetPassword.Text = "Reset Password";
            btnResetPassword.Location = new Point(20, 460);
            btnResetPassword.Size = new Size(140, 32);
            btnResetPassword.Click += btnResetPassword_Click;
            ClientSize = new Size(780, 510);

            dgvData.Location = new Point(20, 105);
            dgvData.Size = new Size(740, 350);
            dgvData.ReadOnly = true;
            dgvData.AllowUserToAddRows = false;
            dgvData.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvData.MultiSelect = false;
            dgvData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvData.CellClick += dgvData_CellClick;

            Controls.Add(title);
            Controls.Add(lblName);
            Controls.Add(txtName);
            Controls.Add(btnUpdate);
            Controls.Add(btnToggle);
            Controls.Add(btnDelete);
            Controls.Add(btnLoad);
            Controls.Add(btnResetPassword);
            Controls.Add(dgvData);

            Load += (s, e) => LoadData();
        }

        private void LoadData()
        {
            try
            {
                dgvData.DataSource = _adminService.GetUsers(_admin);
                selectedId = -1;
                selectedName = "";
                txtName.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:" + ex.Message, "FAIL!!",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLoad_Click(object? sender, EventArgs e)
        {
            LoadData();
        }

        private void dgvData_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow r = dgvData.Rows[e.RowIndex];
            selectedId = Convert.ToInt32(r.Cells["user_id"].Value);
            selectedName = r.Cells["name"].Value?.ToString() ?? "";
            txtName.Text = selectedName;
        }

        private bool EnsureSelected()
        {
            if (selectedId < 0)
            {
                MessageBox.Show("Mohon pilih baris data terlebih dahulu", "Warning!",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return true;
        }

        private void btnUpdate_Click(object? sender, EventArgs e)
        {
            if (!EnsureSelected()) return;
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Nama tidak boleh kosong", "Warning!",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                if (_adminService.UpdateName(_admin, selectedId, txtName.Text))
                {
                    MessageBox.Show("Data user berhasil diupdate", "Well Done!",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData();
                }
                else MessageBox.Show("Akun tidak ditemukan. Muat ulang daftar pengguna.", "Admin");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:" + ex.Message, "Update FAIL!!",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnToggle_Click(object? sender, EventArgs e)
        {
            if (!EnsureSelected()) return;
            if (selectedId == _admin.UserId)
            {
                MessageBox.Show("Tidak bisa menonaktifkan akun sendiri", "Warning!",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                string? newStatus = _adminService.ToggleStatus(_admin, selectedId);
                if (newStatus != null)
                {
                    MessageBox.Show("Status akun berhasil " + newStatus, "Well Done!",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData();
                }
                else MessageBox.Show("Akun tidak ditemukan atau statusnya tidak valid.", "Admin");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:" + ex.Message, "Update FAIL!!",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDelete_Click(object? sender, EventArgs e)
        {
            if (!EnsureSelected()) return;
            if (selectedId == _admin.UserId)
            {
                MessageBox.Show("Tidak bisa menghapus akun sendiri", "Warning!",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("Apakah benar anda ingin menghapus data " + selectedName + " ?",
                "Hapus data terkonfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1) != DialogResult.Yes) return;
            try
            {
                if (_adminService.DeleteUser(_admin, selectedId))
                {
                    MessageBox.Show("Data user berhasil dihapus", "Well Done!",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData();
                }
                else MessageBox.Show("Akun tidak ditemukan. Muat ulang daftar pengguna.", "Admin");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:" + ex.Message, "Delete FAIL!!",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnResetPassword_Click(object? sender, EventArgs e)
        {
            if (!EnsureSelected()) return;
            using var dialog = new Form
            {
                Text = "Reset Password", StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(420, 190), FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false
            };
            var instruction = new Label { Text = "Password baru untuk pengguna #" + selectedId,
                Location = new Point(20, 18), AutoSize = true };
            var passwordLabel = new Label { Text = "Password baru", Location = new Point(20, 55), AutoSize = true };
            var password = new TextBox { Location = new Point(170, 52), Width = 225, UseSystemPasswordChar = true };
            var confirmLabel = new Label { Text = "Ulangi password", Location = new Point(20, 95), AutoSize = true };
            var confirm = new TextBox { Location = new Point(170, 92), Width = 225, UseSystemPasswordChar = true };
            var save = new Button { Text = "Simpan", Location = new Point(215, 135), Width = 85 };
            var cancel = new Button { Text = "Batal", Location = new Point(310, 135), Width = 85, DialogResult = DialogResult.Cancel };
            save.Click += (_, _) =>
            {
                if (password.Text != confirm.Text)
                {
                    MessageBox.Show(dialog, "Konfirmasi password tidak cocok.", "Reset Password");
                    return;
                }
                try
                {
                    if (!_adminService.ResetPassword(_admin, selectedId, password.Text))
                    {
                        MessageBox.Show(dialog, "Akun tidak ditemukan. Muat ulang daftar pengguna.", "Reset Password");
                        return;
                    }
                    password.Clear();
                    confirm.Clear();
                    dialog.DialogResult = DialogResult.OK;
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show(dialog, ex.Message, "Reset Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (Exception)
                {
                    MessageBox.Show(dialog, "Password belum dapat direset. Periksa hak akses dan koneksi API.",
                        "Reset Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            dialog.Controls.AddRange(new Control[] { instruction, passwordLabel, password, confirmLabel, confirm, save, cancel });
            dialog.AcceptButton = save;
            dialog.CancelButton = cancel;
            if (dialog.ShowDialog(this) == DialogResult.OK)
                MessageBox.Show(this, "Password berhasil direset.", "Reset Password");
        }
    }
}
