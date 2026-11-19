using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ClimateSafe
{
    internal sealed class FrmChecklist : Form
    {
        private readonly int userId;
        private readonly ChecklistService service;
        private readonly ComboBox sessions = new ComboBox();
        private readonly DataGridView grid = new DataGridView();
        private readonly Label summary = new Label();
        private readonly Label empty = new Label();
        private readonly ProgressBar progress = new ProgressBar();
        private bool loading;

        public FrmChecklist(int userId, ChecklistService? service = null)
        {
            this.userId = userId;
            this.service = service ?? new ChecklistService();
            Text = "ClimateSafe - Checklist Persiapan";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1060, 680);
            MinimumSize = new Size(900, 600);
            Font = UI.Body(10, FontStyle.Regular);
            BackColor = Color.FromArgb(238, 242, 248);
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 8
            };
            foreach (int height in new[] { 40, 32, 42, 35, 24, 42 })
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            Controls.Add(layout);
            layout.Controls.Add(new Label
            {
                Text = "Checklist Persiapan", AutoSize = true,
                Font = UI.Body(18, FontStyle.Bold), ForeColor = Color.FromArgb(34, 62, 108)
            }, 0, 0);
            layout.Controls.Add(new Label { Text = "Pilih sesi asesmen dan tandai tindakan yang sudah dikerjakan.", AutoSize = true }, 0, 1);
            sessions.Dock = DockStyle.Fill;
            sessions.DropDownStyle = ComboBoxStyle.DropDownList;
            sessions.SelectedIndexChanged += (_, _) => ShowSession();
            layout.Controls.Add(sessions, 0, 2);
            summary.AutoSize = true;
            layout.Controls.Add(summary, 0, 3);
            progress.Dock = DockStyle.Fill;
            layout.Controls.Add(progress, 0, 4);
            empty.AutoSize = true;
            empty.Padding = new Padding(0, 12, 0, 0);
            layout.Controls.Add(empty, 0, 5);
            grid.Dock = DockStyle.Fill;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            grid.DefaultCellStyle.Padding = new Padding(6);
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Done", HeaderText = "Selesai", Width = 80 });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Description", HeaderText = "Tindakan persiapan", ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CompletedAt", HeaderText = "Waktu selesai", ReadOnly = true, Width = 180
            });
            grid.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            grid.CellValueChanged += SaveStatus;
            layout.Controls.Add(grid, 0, 6);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var close = new Button { Text = "Kembali", AutoSize = true, Height = 34 };
            close.Click += (_, _) => Close();
            var reload = new Button { Text = "Muat ulang", AutoSize = true, Height = 34 };
            reload.Click += (_, _) => LoadSessions();
            buttons.Controls.Add(close);
            buttons.Controls.Add(reload);
            layout.Controls.Add(buttons, 0, 7);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            LoadSessions();
        }

        private void LoadSessions()
        {
            if (IsDisposed) return;
            try
            {
                int? selected = (sessions.SelectedItem as ChecklistSession)?.AssessmentId;
                var loaded = service.GetSessions(userId);
                loading = true;
                sessions.Items.Clear();
                foreach (ChecklistSession session in loaded) sessions.Items.Add(session);
                sessions.SelectedItem = loaded.FirstOrDefault(session => session.AssessmentId == selected)
                    ?? loaded.FirstOrDefault();
                loading = false;
                ShowSession();
            }
            catch (Exception ex)
            {
                loading = false;
                MessageBox.Show(this, "Checklist belum dapat dimuat. " + ex.Message,
                    "Checklist", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ShowSession()
        {
            if (loading) return;
            loading = true;
            try
            {
                grid.Rows.Clear();
                var session = sessions.SelectedItem as ChecklistSession;
                empty.Text = session == null ? "Belum ada asesmen. Selesaikan asesmen untuk membuat checklist."
                    : session.Items.Count == 0 ? "Tidak ada tindakan yang dihasilkan untuk sesi ini." : "";
                summary.Text = session == null ? "0 dari 0 tindakan selesai — 0%"
                    : $"{session.CompletedCount} dari {session.Items.Count} tindakan selesai — {session.Progress:0.#}%" +
                      $"     Skor asesmen {session.OverallScore:0.##}/100 · {session.Level}";
                progress.Value = session == null ? 0 : (int)Math.Round(session.Progress);
                if (session == null) return;
                foreach (ChecklistItem item in session.Items)
                {
                    int row = grid.Rows.Add(item.IsCompleted, item.Description,
                        item.CompletedAt?.ToLocalTime().ToString("dd MMM yyyy HH:mm") ?? "—");
                    grid.Rows[row].Tag = item;
                }
            }
            finally { loading = false; }
        }

        private void SaveStatus(object? sender, DataGridViewCellEventArgs e)
        {
            if (loading || e.RowIndex < 0 || e.ColumnIndex != 0) return;
            var row = grid.Rows[e.RowIndex];
            if (row.Tag is not ChecklistItem item) return;
            bool completed = Convert.ToBoolean(row.Cells[0].Value);
            try
            {
                service.SetCompleted(userId, item.ChecklistItemId, completed);
                BeginInvoke(new Action(LoadSessions));
            }
            catch (Exception ex)
            {
                loading = true;
                row.Cells[0].Value = item.IsCompleted;
                loading = false;
                MessageBox.Show(this, "Status belum tersimpan. " + ex.Message, "Checklist",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
