namespace ClimateSafe;

internal sealed class AdminMonitoringPage : BufferedPanel
{
    private const int FilterH = 120;
    private const int SummaryH = 152;
    private const int TableHeadH = 72;
    private const int GridRowH = 44;
    private const int FooterH = 72;
    private const int StateH = 160;
    private const int CardPad = 24;
    private const int CellPad = CardPad - 1;   // grid berada 1 px dari tepi kartu

    // Segmen periode: pilihan aktif pil putih bergaris, pilihan lain tanpa pil.
    private sealed class PeriodSelector : BufferedPanel
    {
        private readonly PillButton[] buttons;
        private readonly Font activeFont = UI.Body(9.5f, FontStyle.Bold);
        private readonly Font idleFont = UI.Body(9.5f, FontStyle.Regular);
        private int selected;

        public event EventHandler? SelectedChanged;

        public PeriodSelector(string[] labels)
        {
            BackColor = AdminUi.IdlePillBg;
            Height = 36;
            buttons = new PillButton[labels.Length];

            int x = 4;
            for (int i = 0; i < labels.Length; i++)
            {
                var button = new PillButton
                {
                    Text = labels[i],
                    Radius = 8,
                    Font = activeFont,
                    FocusColor = AdminUi.Navy,
                    Bounds = new Rectangle(x, 4, AdminUi.DrawnWidth(labels[i], 9.5f, FontStyle.Bold) + 28, 28)
                };
                int index = i;
                button.Click += (_, _) => Select(index, true);
                // Pilihan tidak aktif: teks Ink saat disorot, tanpa perubahan alas.
                button.MouseEnter += (_, _) => { if (index != selected) Repaint(index, AdminUi.Ink); };
                button.MouseLeave += (_, _) => { if (index != selected) Repaint(index, AdminUi.Muted); };
                Controls.Add(button);
                buttons[i] = button;
                x = button.Right + 4;
            }

            Width = x;
            Select(0, false);
        }

        public int SelectedIndex => selected;

        private void Repaint(int index, Color text)
        {
            buttons[index].TextColor = text;
            buttons[index].Invalidate();
        }

        private void Select(int index, bool notify)
        {
            selected = index;
            for (int i = 0; i < buttons.Length; i++)
            {
                bool on = i == index;
                buttons[i].Font = on ? activeFont : idleFont;
                buttons[i].Fill = on ? Color.White : AdminUi.IdlePillBg;
                buttons[i].HoverFill = on ? Color.White : AdminUi.IdlePillBg;
                buttons[i].TextColor = on ? AdminUi.Ink : AdminUi.Muted;
                buttons[i].BorderColor = on ? AdminUi.Line : Color.Empty;
                buttons[i].Invalidate();
            }
            if (notify) SelectedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private readonly ComboBox hazards = new() { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = nameof(ApiHazard.Name) };
    private readonly PeriodSelector periods = new(new[] { "Hari ini", "7 hari terakhir", "30 hari terakhir" });
    private readonly DataGridView grid = new();
    private readonly RoundPanel[] summaryCards = new RoundPanel[3];
    private readonly Label[] summaryValues = new Label[3];
    private readonly RoundPanel tableCard;
    private readonly Label state;
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
        AdminUi.AddText(this, "Admin / Log & Monitoring", 0, 5, 300, 18, 9f, false, AdminUi.Muted);
        AdminUi.AddText(this, "Log & Monitoring", 0, 26, 600, 38, 19f, true, AdminUi.TitleBlue);
        AdminUi.AddText(this, "Pantau akun pengguna dan asesmen yang selesai.", 0, 66, 800, 20, 9.5f, false, AdminUi.SubInk);

        // ---- kartu filter ----
        var filter = AdminUi.Card(this, 0, AdminUi.ContentTop, Width, FilterH, AdminUi.CardRadius, Color.White, AdminUi.Line);
        AdminUi.Stretch(filter, this, 0, 0);

        AdminUi.AddText(filter, "Periode", CardPad, 20, 200, 20, 9.5f, true, AdminUi.Ink);
        periods.Location = new Point(CardPad, 44);
        filter.Controls.Add(periods);

        int hazardX = CardPad + periods.Width + AdminUi.Gap;
        AdminUi.AddText(filter, "Jenis Bahaya", hazardX, 20, 200, 20, 9.5f, true, AdminUi.Ink);
        StyleCombo();
        hazards.Bounds = new Rectangle(hazardX, 44, 260, 36);
        filter.Controls.Add(hazards);

        AdminUi.AddText(filter, "Periode menggunakan WIB", CardPad, 88, 400, 18, 9f, false, AdminUi.Muted);

        // ---- tiga kartu ringkasan ----
        int summaryTop = AdminUi.ContentTop + FilterH + AdminUi.Gap;
        summaryValues[0] = MakeSummary(0, summaryTop, Ico.People, "Total Pengguna", "Semua waktu");
        summaryValues[1] = MakeSummary(1, summaryTop, Ico.Check, "Pengguna Aktif", "Status akun Aktif");
        summaryValues[2] = MakeSummary(2, summaryTop, Ico.List, "Asesmen Selesai", "Sesuai filter");

        // ---- kartu tabel ----
        int tableTop = summaryTop + SummaryH + AdminUi.Gap;
        tableCard = AdminUi.Card(this, 0, tableTop, Width, TableHeadH + GridRowH + StateH + 24, AdminUi.CardRadius,
            Color.White, AdminUi.Line);
        tableCard.ClipRegion = true;
        AdminUi.Stretch(tableCard, this, 0, 0);

        AdminUi.AddText(tableCard, "Riwayat Asesmen", CardPad, 20, 320, 24, 11f, true, AdminUi.TitleBlue);
        AdminUi.AddText(tableCard, "Asesmen terbaru sesuai periode dan jenis bahaya yang dipilih.",
            CardPad, 46, 520, 18, 9f, false, AdminUi.Muted);

        var reload = AdminUi.MakeButton(tableCard, "Muat Ulang", Width - CardPad - 120, 20, 36,
            AdminButtonKind.Secondary, 9.5f, 32);
        reload.Left = Width - CardPad - reload.Width;
        AdminUi.PinRight(reload, tableCard, CardPad);
        reload.Click += async (_, _) => { if (ready) await LoadReportAsync(); else await InitializeAsync(); };

        var headLine = AdminUi.Divider(tableCard, TableHeadH - 1, Width);
        AdminUi.Stretch(headLine, tableCard, 0, 0);

        StyleGrid();
        grid.Bounds = new Rectangle(1, TableHeadH, Width - 2, GridRowH);
        tableCard.Controls.Add(grid);
        AdminUi.Stretch(grid, tableCard, 1, 1);

        // Satu teks di tengah badan tabel untuk state memuat, kosong, dan gagal.
        state = AdminUi.AddAligned(tableCard, "Memuat data...", 1, TableHeadH + GridRowH, Width - 2, StateH,
            10f, false, AdminUi.Muted, ContentAlignment.MiddleCenter);
        AdminUi.Stretch(state, tableCard, 1, 1);

        previous = AdminUi.MakeButton(tableCard, "Sebelumnya", CardPad, TableHeadH + GridRowH + 20, 36,
            AdminButtonKind.Secondary, 9.5f, 32);
        next = AdminUi.MakeButton(tableCard, "Berikutnya", previous.Right + 12, previous.Top, 36,
            AdminButtonKind.Secondary, 9.5f, 32);
        pageLabel = AdminUi.AddText(tableCard, "Halaman 1 dari 1", next.Right + 16, previous.Top + 9, 240, 20,
            9f, false, AdminUi.SubInk);

        previous.Click += async (_, _) => { if (page > 1) { page--; await LoadReportAsync(); } };
        next.Click += async (_, _) => { if ((long)page * pageSize < resultCount) { page++; await LoadReportAsync(); } };
        periods.SelectedChanged += async (_, _) => { if (ready) { page = 1; await LoadReportAsync(); } };
        hazards.SelectedIndexChanged += async (_, _) => { if (ready) { page = 1; await LoadReportAsync(); } };

        SetPageButtons(false);
        LayoutCards();
        Resize += (_, _) => LayoutCards();
        HandleCreated += async (_, _) => { if (!ready) await InitializeAsync(); };
    }

    // Susunan kartu: ikon, angka, judul, keterangan. Semua teks bertepi kiri sama
    // dengan kotak ikon; padding gambar Label dikompensasi lewat AdminUi.TextPad.
    private Label MakeSummary(int index, int top, string glyph, string caption, string note)
    {
        int cardW = (Width - AdminUi.Gap * 2) / 3;
        int textW = cardW - CardPad * 2;
        var card = AdminUi.Card(this, index * (cardW + AdminUi.Gap), top, cardW, SummaryH,
            AdminUi.CardRadius, Color.White, AdminUi.Line);
        summaryCards[index] = card;

        var iconBox = AdminUi.Card(card, CardPad, CardPad, 40, 40, 10, AdminUi.IconBlue, Color.Empty);
        AdminUi.AddSymbol(iconBox, glyph, 0, 0, 40, 40, 13f, AdminUi.Navy);

        Label value = AdminUi.AddText(card, "–", CardPad - AdminUi.TextPad(20f, FontStyle.Bold), 76,
            textW, 34, 20f, true, AdminUi.TitleBlue);
        AdminUi.AddText(card, caption, CardPad - AdminUi.TextPad(10f, FontStyle.Regular), 110,
            textW, 20, 10f, false, AdminUi.SubInk);
        AdminUi.AddText(card, note, CardPad - AdminUi.TextPad(9f, FontStyle.Regular), 132,
            textW, 16, 9f, false, AdminUi.Muted);
        return value;
    }

    private void LayoutCards()
    {
        int w = Math.Max(AdminUi.PageMinW, Width);
        int cardW = (w - AdminUi.Gap * 2) / 3;
        for (int i = 0; i < summaryCards.Length; i++)
        {
            if (summaryCards[i] == null) continue;
            int left = i * (cardW + AdminUi.Gap);
            summaryCards[i].SetBounds(left, summaryCards[i].Top, i == 2 ? w - left : cardW, SummaryH);
        }
        LayoutTable();
    }

    private void LayoutTable()
    {
        bool hasRows = grid.Rows.Count > 0;
        int headerH = grid.ColumnHeadersHeight;
        grid.Height = hasRows ? headerH + grid.Rows.Count * GridRowH : headerH;

        // Tanpa baris: satu teks state setinggi StateH menggantikan badan tabel dan
        // baris penomoran halaman disembunyikan.
        state.Visible = !hasRows;
        state.Top = TableHeadH + headerH;
        previous.Visible = next.Visible = pageLabel.Visible = hasRows;

        int bodyBottom = TableHeadH + grid.Height + (hasRows ? 0 : StateH);
        if (hasRows)
        {
            previous.Top = bodyBottom + 20;
            next.Top = previous.Top;
            pageLabel.Top = previous.Top + 9;
        }

        tableCard.Height = bodyBottom + (hasRows ? FooterH : CardPad);
        Height = tableCard.Top + tableCard.Height + AdminUi.PagePad;
    }

    private void StyleGrid()
    {
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.AllowUserToResizeColumns = false;
        grid.RowHeadersVisible = false;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.GridColor = AdminUi.Line;
        grid.ScrollBars = ScrollBars.None;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeight = GridRowH;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.RowTemplate.Height = GridRowH;

        grid.ColumnHeadersDefaultCellStyle.BackColor = AdminUi.BandBg;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = AdminUi.Muted;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = AdminUi.BandBg;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = AdminUi.Muted;
        grid.ColumnHeadersDefaultCellStyle.Font = UI.Body(9f, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(CellPad, 0, 0, 0);

        grid.DefaultCellStyle.Font = UI.Body(10f, FontStyle.Regular);
        grid.DefaultCellStyle.ForeColor = AdminUi.Ink;
        grid.DefaultCellStyle.BackColor = Color.White;
        grid.DefaultCellStyle.SelectionBackColor = AdminUi.IconBlue;
        grid.DefaultCellStyle.SelectionForeColor = AdminUi.Ink;
        grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        grid.DefaultCellStyle.Padding = new Padding(CellPad, 0, 0, 0);

        grid.Columns.Add("time", "Waktu (WIB)");
        grid.Columns.Add("user", "Pengguna");
        grid.Columns.Add("hazard", "Jenis Bahaya");
        grid.Columns.Add("score", "Skor");
        grid.Columns.Add("level", "Level");

        grid.Columns[0].MinimumWidth = AdminUi.LabelWidth("00 Mei 0000 00:00", 10f, FontStyle.Regular) + 40;
        grid.Columns[0].FillWeight = 130;
        grid.Columns[1].FillWeight = 150;
        grid.Columns[1].DefaultCellStyle.Font = UI.Body(10f, FontStyle.Bold);
        grid.Columns[2].FillWeight = 130;
        grid.Columns[3].FillWeight = 70;
        grid.Columns[4].FillWeight = 90;
        grid.Columns[4].DefaultCellStyle.Padding = new Padding(CellPad, 0, CellPad, 0);
        grid.Columns[4].HeaderCell.Style.Padding = new Padding(CellPad, 0, CellPad, 0);
    }

    // ComboBox digambar sendiri agar tinggi 36 px dan warnanya sama dengan kartu.
    private void StyleCombo()
    {
        hazards.FlatStyle = FlatStyle.Flat;
        hazards.BackColor = Color.White;
        hazards.ForeColor = AdminUi.Ink;
        hazards.Font = UI.Body(10f, FontStyle.Regular);
        hazards.IntegralHeight = false;
        hazards.DrawMode = DrawMode.OwnerDrawFixed;
        hazards.ItemHeight = 30;
        hazards.DrawItem += (_, e) =>
        {
            if (e.Index < 0) return;
            bool on = (e.State & DrawItemState.Selected) != 0;
            using (var b = new SolidBrush(on ? AdminUi.IconBlue : Color.White))
            {
                e.Graphics.FillRectangle(b, e.Bounds);
            }
            string text = (hazards.Items[e.Index] as ApiHazard)?.Name ?? "";
            TextRenderer.DrawText(e.Graphics, text, hazards.Font,
                new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height),
                AdminUi.Ink, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        };
    }

    private void SetState(string text, bool error)
    {
        state.Text = text;
        state.ForeColor = error ? AdminUi.DangerInk : AdminUi.Muted;
    }

    private async Task InitializeAsync()
    {
        if (ready) return;
        int version = ++requestVersion;
        SetState("Memuat data...", false);
        try
        {
            var items = await Task.Run(() => ApiClient.Get<List<ApiHazard>>("api/hazards") ?? new());
            if (IsDisposed || version != requestVersion) return;
            items.Insert(0, new ApiHazard(0, "Semua Bahaya"));
            hazards.DataSource = items;
            ready = true;
            await LoadReportAsync();
        }
        catch (Exception ex)
        {
            if (!IsDisposed && version == requestVersion) SetState("Data tidak dapat dimuat: " + ex.Message, true);
        }
    }

    private async Task LoadReportAsync()
    {
        int version = ++requestVersion;
        string period = periods.SelectedIndex switch { 1 => "7days", 2 => "30days", _ => "today" };
        int hazardId = (hazards.SelectedItem as ApiHazard)?.Id ?? 0;
        string path = $"api/admin/monitoring?period={period}&page={page}" + (hazardId > 0 ? $"&hazardId={hazardId}" : "");

        SetState("Memuat data...", false);
        grid.Rows.Clear();
        summaryValues[0].Text = summaryValues[1].Text = summaryValues[2].Text = "–";
        SetPageButtons(false);
        LayoutTable();

        try
        {
            var result = await Task.Run(() => ApiClient.Get<ApiMonitoringReport>(path))
                ?? throw new InvalidOperationException("Respons monitoring kosong.");
            if (IsDisposed || version != requestVersion) return;

            summaryValues[0].Text = result.TotalUsers.ToString("N0");
            summaryValues[1].Text = result.ActiveUsers.ToString("N0");
            summaryValues[2].Text = result.TotalAssessments.ToString("N0");
            resultCount = result.TotalAssessments;
            pageSize = result.PageSize > 0 ? result.PageSize : pageSize;

            foreach (var row in result.Assessments)
            {
                int index = grid.Rows.Add(
                    row.CreatedAt.ToUniversalTime().AddHours(7).ToString("dd MMM yyyy HH:mm"),
                    row.UserName, row.HazardName,
                    Math.Round(row.OverallScore, MidpointRounding.AwayFromZero).ToString("0") + "/100",
                    row.Level);
                grid.Rows[index].Cells[4].Style.ForeColor = LevelColor(row.Level);
                grid.Rows[index].Cells[4].Style.SelectionForeColor = LevelColor(row.Level);
            }
            grid.ClearSelection();

            long pages = pageSize > 0 ? (resultCount + pageSize - 1) / pageSize : 1;
            if (pages < 1) pages = 1;
            pageLabel.Text = $"Halaman {page} dari {pages}";
            SetState("Tidak ada asesmen pada filter ini.", false);
            SetPageButtons(true);
            LayoutTable();
        }
        catch (Exception ex)
        {
            if (IsDisposed || version != requestVersion) return;
            SetState("Data tidak dapat dimuat: " + ex.Message, true);
            LayoutTable();
        }
    }

    private static Color LevelColor(string level) =>
        string.Equals(level, ScoringRules.ReadyLevel, StringComparison.OrdinalIgnoreCase) ? AdminUi.SuccessInk :
        string.Equals(level, ScoringRules.NotReadyLevel, StringComparison.OrdinalIgnoreCase) ? AdminUi.DangerInk :
        AdminUi.Navy;

    private void SetPageButtons(bool loaded)
    {
        AdminUi.StyleButton(previous, AdminButtonKind.Secondary, loaded && page > 1);
        AdminUi.StyleButton(next, AdminButtonKind.Secondary, loaded && (long)page * pageSize < resultCount);
    }
}
