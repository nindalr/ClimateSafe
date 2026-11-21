namespace ClimateSafe;

internal sealed class AdminMonitoringPage : BufferedPanel
{
    private readonly ComboBox periods = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox hazards = new() { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = nameof(ApiHazard.Name) };
    private readonly DataGridView grid = new();
    private readonly Label totalUsers;
    private readonly Label activeUsers;
    private readonly Label totalAssessments;
    private readonly Label message;
    private readonly Label pageLabel;
    private readonly PillButton previous;
    private readonly PillButton next;
    private int page = 1;
    private long resultCount;
    private int pageSize = 50;
    private int requestVersion;
    private bool ready;

    public AdminMonitoringPage()
    {
        BackColor = AdminUi.Bg;
        Width = AdminUi.PageMaxW;
        Height = 820;
        AdminUi.AddText(this, "Admin / Log & Monitoring", 0, 5, 300, 18, 9f, false, AdminUi.Muted);
        AdminUi.AddText(this, "Log & Monitoring", 0, 26, 600, 38, 19f, true, AdminUi.TitleBlue);
        AdminUi.AddText(this, "Pantau akun pengguna dan asesmen yang selesai.", 0, 66, 800, 20, 9.5f, false, AdminUi.SubInk);

        totalUsers = MakeSummary("Total Pengguna", 0);
        activeUsers = MakeSummary("Pengguna Aktif", 300);
        totalAssessments = MakeSummary("Asesmen Selesai", 600);

        AdminUi.AddText(this, "Periode", 0, 238, 200, 20, 9.5f, true, AdminUi.Ink);
        periods.Bounds = new Rectangle(0, 263, 200, 30);
        periods.Font = UI.Body(10f, FontStyle.Regular);
        periods.Items.AddRange(new object[] { "Hari ini", "7 hari terakhir", "30 hari terakhir" });
        periods.SelectedIndex = 0;
        Controls.Add(periods);
        AdminUi.AddText(this, "Jenis bahaya", 220, 238, 200, 20, 9.5f, true, AdminUi.Ink);
        hazards.Bounds = new Rectangle(220, 263, 260, 30);
        hazards.Font = UI.Body(10f, FontStyle.Regular);
        Controls.Add(hazards);
        var refresh = AdminUi.MakeButton(this, "Muat Ulang", 500, 257, 38,
            Color.White, AdminUi.HoverBg, AdminUi.Navy, AdminUi.Line, 9f, 30);
        refresh.Click += async (_, _) => { if (ready) await LoadReportAsync(); else await InitializeAsync(); };
        AdminUi.AddText(this, "Periode menggunakan WIB dan mencakup hari ini.", 0, 302, 700, 20, 9f, false, AdminUi.Muted);

        grid.Bounds = new Rectangle(0, 335, Width, 370);
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.ColumnHeadersHeight = 38;
        grid.RowTemplate.Height = 34;
        grid.Columns.Add("time", "Waktu (WIB)");
        grid.Columns.Add("user", "Nama pengguna");
        grid.Columns.Add("hazard", "Jenis bahaya");
        grid.Columns.Add("score", "Skor");
        grid.Columns.Add("level", "Level");
        grid.Columns[3].FillWeight = 55;
        Controls.Add(grid);
        AdminUi.Stretch(grid, this, 0, 0);

        message = AdminUi.AddText(this, "Memuat data...", 0, 714, 800, 22, 9f, false, AdminUi.SubInk);
        previous = AdminUi.MakeButton(this, "Sebelumnya", 0, 744, 38, Color.White, AdminUi.HoverBg, AdminUi.Navy, AdminUi.Line, 9f, 30);
        next = AdminUi.MakeButton(this, "Berikutnya", previous.Right + 12, 744, 38, Color.White, AdminUi.HoverBg, AdminUi.Navy, AdminUi.Line, 9f, 30);
        pageLabel = AdminUi.AddText(this, "Halaman 1", next.Right + 20, 754, 250, 20, 9f, false, AdminUi.SubInk);
        previous.Click += async (_, _) => { if (page > 1) { page--; await LoadReportAsync(); } };
        next.Click += async (_, _) => { if ((long)page * pageSize < resultCount) { page++; await LoadReportAsync(); } };
        periods.SelectedIndexChanged += async (_, _) => { if (ready) { page = 1; await LoadReportAsync(); } };
        hazards.SelectedIndexChanged += async (_, _) => { if (ready) { page = 1; await LoadReportAsync(); } };
        SetPageButtons(false);
        HandleCreated += async (_, _) => { if (!ready) await InitializeAsync(); };
    }

    private Label MakeSummary(string caption, int x)
    {
        var card = AdminUi.Card(this, x, AdminUi.ContentTop, 280, 105, 16, Color.White, AdminUi.Line);
        AdminUi.AddText(card, caption, 20, 18, 240, 22, 10f, false, AdminUi.SubInk);
        return AdminUi.AddText(card, "–", 20, 50, 240, 35, 20f, true, AdminUi.TitleBlue);
    }

    private async Task InitializeAsync()
    {
        if (ready) return;
        int version = ++requestVersion;
        message.Text = "Memuat kategori bahaya...";
        try
        {
            var items = await Task.Run(() => ApiClient.Get<List<ApiHazard>>("api/hazards") ?? new());
            if (IsDisposed || version != requestVersion) return;
            items.Insert(0, new ApiHazard(0, "Semua bahaya"));
            hazards.DataSource = items;
            ready = true;
            await LoadReportAsync();
        }
        catch (Exception ex)
        {
            if (!IsDisposed && version == requestVersion) message.Text = "Data tidak dapat dimuat: " + ex.Message;
        }
    }

    private async Task LoadReportAsync()
    {
        int version = ++requestVersion;
        string period = periods.SelectedIndex switch { 1 => "7days", 2 => "30days", _ => "today" };
        int hazardId = (hazards.SelectedItem as ApiHazard)?.Id ?? 0;
        string path = $"api/admin/monitoring?period={period}&page={page}" + (hazardId > 0 ? $"&hazardId={hazardId}" : "");
        message.Text = "Memuat data...";
        grid.Rows.Clear();
        totalUsers.Text = activeUsers.Text = totalAssessments.Text = "–";
        SetPageButtons(false);
        try
        {
            var result = await Task.Run(() => ApiClient.Get<ApiMonitoringReport>(path))
                ?? throw new InvalidOperationException("Respons monitoring kosong.");
            if (IsDisposed || version != requestVersion) return;
            totalUsers.Text = result.TotalUsers.ToString("N0");
            activeUsers.Text = result.ActiveUsers.ToString("N0");
            totalAssessments.Text = result.TotalAssessments.ToString("N0");
            resultCount = result.TotalAssessments;
            pageSize = result.PageSize;
            foreach (var row in result.Assessments)
                grid.Rows.Add(row.CreatedAt.ToUniversalTime().AddHours(7).ToString("dd/MM/yyyy HH:mm"),
                    row.UserName, row.HazardName, row.OverallScore.ToString("0.##"), row.Level);
            message.Text = resultCount == 0 ? "Tidak ada asesmen pada filter ini." : $"{resultCount:N0} asesmen sesuai filter.";
            pageLabel.Text = $"Halaman {page}";
            SetPageButtons(true);
        }
        catch (Exception ex)
        {
            if (!IsDisposed && version == requestVersion) message.Text = "Data tidak dapat dimuat: " + ex.Message;
        }
    }

    private void SetPageButtons(bool loaded)
    {
        previous.Enabled = loaded && page > 1;
        next.Enabled = loaded && (long)page * pageSize < resultCount;
    }
}
