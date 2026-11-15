using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace ClimateSafe.Auth
{
    public class FormAdminPanel : Form
    {
        private readonly UserAccount _admin;
        private readonly UserRepository _repo = new UserRepository();

        private readonly DataGridView dgvData = new DataGridView();
        private readonly TextBox txtName = new TextBox();
        private readonly Button btnLoad = new Button();
        private readonly Button btnUpdate = new Button();
        private readonly Button btnToggle = new Button();
        private readonly Button btnDelete = new Button();

        private int selectedId = -1;
        private string selectedStatus = "";

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
            Controls.Add(dgvData);

            Load += (s, e) => LoadData();
        }

        private void LoadData()
        {
            try
            {
                dgvData.DataSource = _repo.GetAllUsers();
                selectedId = -1;
                selectedStatus = "";
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
            selectedStatus = r.Cells["status"].Value?.ToString() ?? "";
            txtName.Text = r.Cells["name"].Value?.ToString() ?? "";
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
                if (_repo.UpdateName(selectedId, txtName.Text.Trim()))
                {
                    MessageBox.Show("Data user berhasil diupdate", "Well Done!",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData();
                }
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
            string newStatus = selectedStatus == "Active" ? "Inactive" : "Active";
            try
            {
                if (_repo.SetStatus(selectedId, newStatus))
                {
                    MessageBox.Show("Status akun menjadi " + newStatus, "Well Done!",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData();
                }
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
            if (MessageBox.Show("Apakah benar anda ingin menghapus data " + txtName.Text + " ?",
                "Hapus data terkonfirmasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1) != DialogResult.Yes) return;
            try
            {
                if (_repo.DeleteUser(selectedId))
                {
                    MessageBox.Show("Data user berhasil dihapus", "Well Done!",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:" + ex.Message, "Delete FAIL!!",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
