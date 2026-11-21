using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using ClimateSafe.Auth;

namespace ClimateSafe
{
    // Halaman "Kelola Pengguna" di area konten FrmAdmin. Data dari AdminService
    // (milik backend); halaman ini tidak menulis SQL.
    internal sealed class AdminPenggunaPage : BufferedPanel
    {
        private sealed class UserItem
        {
            public int Id;
            public string Name = "";
            public string Email = "";
            public string Role = "";
            public string Status = "";
        }

        private readonly Form owner;
        private readonly UserAccount admin;
        private readonly AdminService service = new AdminService();
        private readonly List<UserItem> users = new List<UserItem>();
        private readonly List<AdminUserRow> rows = new List<AdminUserRow>();

        private TextBox searchInput;
        private BufferedPanel? body;
        private TextBox? nameInput;
        private PillButton? saveButton;

        private string? loadError;
        private string? banner;
        private int selectedId;
        private bool built;

        private const int SearchTop = AdminUi.ContentTop;
        private const int BodyTop = AdminUi.ContentTop + AdminUi.FieldH + 20;

        public AdminPenggunaPage(Form owner, UserAccount admin)
        {
            this.owner = owner;
            this.admin = admin;

            BackColor = AdminUi.Bg;
            Width = AdminUi.PageMaxW;

            BuildHeader();
            searchInput = BuildSearchRow();
            built = true;

            ReloadData();
        }

        // ---------- kerangka halaman ----------

        private void BuildHeader()
        {
            AdminUi.AddText(this, "Admin", 0, 5, 44, 16, 9f, false, AdminUi.Muted);
            AdminUi.AddText(this, "/", 46, 5, 10, 16, 9f, false, AdminUi.Muted);
            AdminUi.AddText(this, "Kelola Pengguna", 58, 5, 130, 16, 9f, true, AdminUi.Ink);

            AdminUi.AddText(this, "Kelola Pengguna", 0, 26, 600, 38, 19f, true, AdminUi.TitleBlue);
            AdminUi.AddText(this, "Lihat, ubah, nonaktifkan, atau hapus akun pengguna.",
                0, 66, 800, 20, 9.5f, false, AdminUi.SubInk);
        }

        private TextBox BuildSearchRow()
        {
            TextBox input = AdminUi.MakeField(this, 0, SearchTop, 420,
                "Cari berdasarkan nama atau email");
            input.TextChanged += delegate (object? s, EventArgs e) { RebuildBody(); };

            PillButton reload = AdminUi.MakeButton(this, "Muat Ulang", 0, SearchTop, AdminUi.FieldH,
                Color.White, AdminUi.HoverBg, AdminUi.Navy, AdminUi.Line, 10f, 40);
            reload.Left = Width - reload.Width;
            AdminUi.PinRight(reload, this, 0);
            reload.Click += delegate (object? s, EventArgs e) { ReloadData(); };

            return input;
        }

        // ---------- data ----------

        public void ReloadData()
        {
            if (!built) return;

            loadError = null;
            users.Clear();

            try
            {
                DataTable table = service.GetUsers(admin);
                foreach (DataRow row in table.Rows)
                {
                    UserItem item = new UserItem();
                    item.Id = Convert.ToInt32(row["user_id"]);
                    item.Name = row["name"]?.ToString() ?? "";
                    item.Email = row["email"]?.ToString() ?? "";
                    item.Role = row["role"]?.ToString() ?? "";
                    item.Status = row["status"]?.ToString() ?? "";
                    users.Add(item);
                }
            }
            catch (Exception ex)
            {
                loadError = AdminUi.TechnicalDetail(ex);
                users.Clear();
            }

            if (Find(selectedId) == null) selectedId = 0;
            RebuildBody();
        }

        private UserItem? Find(int id)
        {
            foreach (UserItem item in users)
            {
                if (item.Id == id) return item;
            }
            return null;
        }

        private List<UserItem> Filtered()
        {
            string query = (searchInput.Text ?? "").Trim();
            if (query.Length == 0) return new List<UserItem>(users);

            List<UserItem> result = new List<UserItem>();
            foreach (UserItem item in users)
            {
                if (item.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                    item.Email.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    result.Add(item);
                }
            }
            return result;
        }

        private static string StatusText(string status)
        {
            return string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase)
                ? "Aktif" : "Nonaktif";
        }

        // ---------- render ----------

        private void RebuildBody()
        {
            if (!built) return;

            SuspendLayout();
            try
            {
                rows.Clear();
                nameInput = null;
                saveButton = null;

                if (body != null)
                {
                    Controls.Remove(body);
                    body.Dispose();
                    body = null;
                }

                int w = Math.Max(AdminUi.PageMinW, Width);

                BufferedPanel host = new BufferedPanel();
                host.BackColor = AdminUi.Bg;
                host.Bounds = new Rectangle(0, BodyTop, w, 100);
                Controls.Add(host);
                body = host;
                AdminUi.Stretch(host, this, 0, 0);

                int y = 0;
                if (loadError != null)
                {
                    y = BuildErrorCard(host, y);
                }
                else if (users.Count == 0)
                {
                    y = BuildMessageCard(host, y, "Belum ada pengguna",
                        "Belum ada akun pengguna yang terdaftar di dalam sistem aplikasi.", null, null);
                }
                else
                {
                    List<UserItem> visible = Filtered();
                    if (visible.Count == 0)
                    {
                        y = BuildMessageCard(host, y, "Tidak ada akun yang cocok", null,
                            "Bersihkan Pencarian",
                            delegate (object? s, EventArgs e) { searchInput.Text = ""; });
                    }
                    else
                    {
                        y = BuildListCard(host, y, visible);
                        if (banner != null) y = BuildBanner(host, y + AdminUi.Gap) ;
                        UserItem? selected = Find(selectedId);
                        if (selected != null) y = BuildActionCard(host, y + AdminUi.Gap, selected);
                    }
                }

                host.Height = y;
                Height = BodyTop + y + AdminUi.PagePad;
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        private int BuildListCard(BufferedPanel host, int top, List<UserItem> visible)
        {
            int w = host.Width;
            RoundPanel card = AdminUi.Card(host, 0, top, w, 100, AdminUi.CardRadius,
                Color.White, AdminUi.Line);
            card.ClipRegion = true;
            AdminUi.Stretch(card, host, 0, 0);

            BufferedPanel band = new BufferedPanel();
            band.Bounds = new Rectangle(1, 1, w - 2, AdminUi.HeadBandH);
            band.BackColor = AdminUi.BandBg;
            card.Controls.Add(band);
            AdminUi.Stretch(band, card, 1, 1);

            AdminUi.AddAligned(band, "ID", AdminUi.ColId - 1, 0, 60, AdminUi.HeadBandH, 9f, true,
                AdminUi.Muted, ContentAlignment.MiddleLeft);
            AdminUi.AddAligned(band, "Nama", AdminUi.ColName - 1, 0, 200, AdminUi.HeadBandH, 9f, true,
                AdminUi.Muted, ContentAlignment.MiddleLeft);
            AdminUi.AddAligned(band, "Email", AdminUi.ColEmail - 1, 0, 240, AdminUi.HeadBandH, 9f, true,
                AdminUi.Muted, ContentAlignment.MiddleLeft);
            AdminUi.AddAligned(band, "Peran", AdminUi.ColRole - 1, 0, 100, AdminUi.HeadBandH, 9f, true,
                AdminUi.Muted, ContentAlignment.MiddleLeft);
            AdminUi.AddAligned(band, "Status", AdminUi.ColStatus - 1, 0, 110, AdminUi.HeadBandH, 9f, true,
                AdminUi.Muted, ContentAlignment.MiddleLeft);

            int y = 1 + AdminUi.HeadBandH;
            Panel headLine = AdminUi.Divider(card, y, w);
            AdminUi.Stretch(headLine, card, 0, 0);
            y += 1;

            for (int i = 0; i < visible.Count; i++)
            {
                UserItem item = visible[i];

                AdminUserRow row = new AdminUserRow();
                row.UserId = item.Id;
                row.NameText = item.Name;
                row.EmailText = item.Email;
                row.RoleText = item.Role;
                row.StatusText = item.Status;
                row.IsSelf = item.Id == admin.UserId;
                row.Selected = item.Id == selectedId;
                row.Bounds = new Rectangle(0, y, w, AdminUi.RowH);
                card.Controls.Add(row);
                AdminUi.Stretch(row, card, 0, 0);
                rows.Add(row);

                int captured = item.Id;
                row.Activated += delegate (object? s, EventArgs e) { Select(captured); };

                y += AdminUi.RowH;

                if (i < visible.Count - 1)
                {
                    Panel sep = AdminUi.Divider(card, y, w);
                    AdminUi.Stretch(sep, card, 0, 0);
                    y += 1;
                }
            }

            y += 1;
            card.Height = y;
            return top + y;
        }

        private int BuildBanner(BufferedPanel host, int top)
        {
            int w = host.Width;
            RoundPanel card = AdminUi.Card(host, 0, top, w, 48, 8, AdminUi.SuccessBg, Color.Empty);
            AdminUi.Stretch(card, host, 0, 0);

            AdminUi.AddAligned(card, banner ?? "", 20, 0, Math.Max(200, w - 160), 48, 9.5f, true,
                AdminUi.SuccessInk, ContentAlignment.MiddleLeft);

            PillButton close = AdminUi.MakeButton(card, "Tutup", 0, 8, 32,
                Color.White, AdminUi.HoverBg, AdminUi.SuccessInk, Color.Empty, 9f, 28);
            close.Left = w - 20 - close.Width;
            AdminUi.PinRight(close, card, 20);
            close.Click += delegate (object? s, EventArgs e)
            {
                banner = null;
                RebuildBody();
            };

            return top + 48;
        }

        private int BuildActionCard(BufferedPanel host, int top, UserItem item)
        {
            int w = host.Width;
            RoundPanel card = AdminUi.Card(host, 0, top, w, 100, AdminUi.CardRadius,
                Color.White, AdminUi.Line);
            AdminUi.Stretch(card, host, 0, 0);

            AdminUi.AddText(card, "Aksi Akun: " + item.Name + " (ID: " + item.Id.ToString("0") + ")",
                24, 20, Math.Max(240, w - 48), 24, 11f, true, AdminUi.TitleBlue);
            AdminUi.AddText(card, "Peran: " + item.Role + " - Status: " + StatusText(item.Status),
                24, 48, Math.Max(240, w - 48), 20, 9.5f, false, AdminUi.Muted);

            AdminUi.AddText(card, "Nama Pengguna", 24, 84, 200, 16, 9f, true, AdminUi.Muted);
            int fieldW = Math.Min(420, Math.Max(240, w - 48));
            TextBox input = AdminUi.MakeField(card, 24, 106, fieldW, "");
            input.Text = item.Name;
            nameInput = input;

            int buttonsTop = 106 + AdminUi.FieldH + 20;
            bool self = item.Id == admin.UserId;
            bool active = string.Equals(item.Status, "Active", StringComparison.OrdinalIgnoreCase);

            PillButton save = AdminUi.MakeButton(card, "Simpan Nama", 24, buttonsTop, AdminUi.ButtonH,
                AdminUi.BtnBlue, AdminUi.BtnBlueHover, Color.White, Color.Empty, 10f, 44);
            saveButton = save;
            AdminUi.SetEnabled(save, false, AdminUi.BtnBlue, AdminUi.BtnBlueHover, Color.White, Color.Empty);
            save.Click += delegate (object? s, EventArgs e) { SaveName(item); };

            input.TextChanged += delegate (object? s, EventArgs e)
            {
                string value = (input.Text ?? "").Trim();
                bool on = value.Length > 0 && !string.Equals(value, item.Name, StringComparison.Ordinal);
                AdminUi.SetEnabled(save, on, AdminUi.BtnBlue, AdminUi.BtnBlueHover,
                    Color.White, Color.Empty);
            };

            string toggleText = active ? "Nonaktifkan Akun" : "Aktifkan Akun";
            PillButton toggle = AdminUi.MakeButton(card, toggleText, save.Right + 12, buttonsTop,
                AdminUi.ButtonH, Color.White, AdminUi.HoverBg, AdminUi.Navy, AdminUi.Line, 10f, 44);
            AdminUi.SetEnabled(toggle, !self, Color.White, AdminUi.HoverBg, AdminUi.Navy, AdminUi.Line);
            toggle.Click += delegate (object? s, EventArgs e) { ToggleStatus(item, active); };

            PillButton remove = AdminUi.MakeButton(card, "Hapus Akun", toggle.Right + 12, buttonsTop,
                AdminUi.ButtonH, Color.White, AdminUi.DangerBg, AdminUi.DangerInk, AdminUi.DangerInk,
                10f, 44);
            AdminUi.SetEnabled(remove, !self, Color.White, AdminUi.DangerBg, AdminUi.DangerInk,
                AdminUi.DangerInk);
            remove.Click += delegate (object? s, EventArgs e) { DeleteUser(item); };

            int bottom = buttonsTop + AdminUi.ButtonH;
            if (self)
            {
                AdminUi.AddText(card, "Tidak bisa menonaktifkan atau menghapus akun sendiri.",
                    24, bottom + 12, Math.Max(240, w - 48), 18, 9f, false, AdminUi.Muted);
                bottom += 12 + 18;
            }

            int h = bottom + 24;
            card.Height = h;
            return top + h;
        }

        private int BuildMessageCard(BufferedPanel host, int top, string title, string? message,
                                     string? buttonText, EventHandler? action)
        {
            int w = host.Width;
            int h = 300;
            RoundPanel card = AdminUi.Card(host, 0, top, w, h, AdminUi.CardRadius,
                Color.White, AdminUi.Line);
            AdminUi.Stretch(card, host, 0, 0);

            Label titleLabel = AdminUi.AddAligned(card, title, 0, 104, w, 28, 13f, true, AdminUi.TitleBlue,
                ContentAlignment.TopCenter);
            AdminUi.Stretch(titleLabel, card, 0, 0);

            if (message != null)
            {
                Label messageLabel = AdminUi.AddAligned(card, message, 24, 140, w - 48, 22, 9.5f, false,
                    AdminUi.SubInk, ContentAlignment.TopCenter);
                AdminUi.Stretch(messageLabel, card, 24, 24);
            }

            if (buttonText != null && action != null)
            {
                PillButton button = AdminUi.MakeButton(card, buttonText, 0, 180, AdminUi.ButtonH,
                    Color.White, AdminUi.HoverBg, AdminUi.Navy, AdminUi.Line, 10f, 44);
                button.Click += action;
                AdminUi.CenterIn(button, card);
            }

            return top + h;
        }

        private int BuildErrorCard(BufferedPanel host, int top)
        {
            int w = host.Width;
            RoundPanel card = AdminUi.Card(host, 0, top, w, 100, AdminUi.CardRadius,
                Color.White, AdminUi.Line);
            AdminUi.Stretch(card, host, 0, 0);

            AdminUi.AddText(card, "Data pengguna gagal dimuat", 24, 24, Math.Max(240, w - 48), 26,
                13f, true, AdminUi.TitleBlue);

            List<string> lines;
            string detail = "Detail teknis: " + (loadError ?? "");
            using (Font f = UI.Body(9f, FontStyle.Regular))
            {
                lines = AdminUi.WrapLines(detail, f, Math.Max(240, w - 48));
            }
            for (int i = 0; i < lines.Count; i++)
            {
                Label lineLabel = AdminUi.AddText(card, lines[i], 24, 58 + i * 18,
                    Math.Max(240, w - 48), 18, 9f, false, AdminUi.Muted);
                AdminUi.Stretch(lineLabel, card, 24, 24);
            }

            int buttonTop = 58 + Math.Max(1, lines.Count) * 18 + 16;
            PillButton retry = AdminUi.MakeButton(card, "Coba Lagi", 24, buttonTop, AdminUi.ButtonH,
                AdminUi.BtnBlue, AdminUi.BtnBlueHover, Color.White, Color.Empty, 10f, 44);
            retry.Click += delegate (object? s, EventArgs e) { ReloadData(); };

            int h = buttonTop + AdminUi.ButtonH + 24;
            card.Height = h;
            return top + h;
        }

        // ---------- aksi ----------

        private void Select(int id)
        {
            if (selectedId == id) return;
            selectedId = id;
            banner = null;
            RebuildBody();
        }

        private void SaveName(UserItem item)
        {
            if (nameInput == null) return;
            string value = (nameInput.Text ?? "").Trim();
            if (value.Length == 0 || string.Equals(value, item.Name, StringComparison.Ordinal)) return;

            try
            {
                service.UpdateName(admin, item.Id, value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, "Nama belum dapat disimpan.\nDetail teknis: " +
                    AdminUi.TechnicalDetail(ex), "Kelola Pengguna",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            banner = "Nama berhasil diubah";
            ReloadData();
        }

        private void ToggleStatus(UserItem item, bool active)
        {
            // Perlindungan di kode, bukan hanya di UI.
            if (item.Id == admin.UserId) return;

            if (active && !AdminUi.Confirm(owner, "Nonaktifkan akun?",
                    "Akun " + item.Name + " akan dinonaktifkan. Pengguna tidak dapat masuk ke sistem " +
                    "sampai akun diaktifkan kembali.", "Ya, Nonaktifkan", false))
            {
                return;
            }

            try
            {
                service.ToggleStatus(admin, item.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, "Status akun belum dapat diubah.\nDetail teknis: " +
                    AdminUi.TechnicalDetail(ex), "Kelola Pengguna",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            banner = active ? "Akun dinonaktifkan" : "Akun diaktifkan";
            ReloadData();
        }

        private void DeleteUser(UserItem item)
        {
            // Perlindungan di kode, bukan hanya di UI.
            if (item.Id == admin.UserId) return;

            if (!AdminUi.Confirm(owner, "Hapus akun?",
                    "Akun " + item.Name + " akan dihapus dan tidak bisa dikembalikan.",
                    "Ya, Hapus", true))
            {
                return;
            }

            int nextId = NeighbourId(item.Id);

            try
            {
                service.DeleteUser(admin, item.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, "Akun belum dapat dihapus.\nDetail teknis: " +
                    AdminUi.TechnicalDetail(ex), "Kelola Pengguna",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            selectedId = nextId;
            banner = "Akun dihapus";
            ReloadData();
        }

        // Baris terdekat setelah penghapusan: berikutnya bila ada, kalau tidak sebelumnya.
        private int NeighbourId(int removedId)
        {
            List<UserItem> visible = Filtered();
            for (int i = 0; i < visible.Count; i++)
            {
                if (visible[i].Id != removedId) continue;
                if (i + 1 < visible.Count) return visible[i + 1].Id;
                if (i > 0) return visible[i - 1].Id;
                return 0;
            }
            return 0;
        }
    }
}
