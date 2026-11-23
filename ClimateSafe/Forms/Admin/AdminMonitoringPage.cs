namespace ClimateSafe;

internal sealed class AdminMonitoringPage : BufferedPanel
{
    private const int FilterH = 144;
    private const int TableHeadH = 72;
    private const int GridRowH = 44;
    private const int FooterH = 72;
    private const int StateH = 160;
    private const int CardPad = 24;
    private const int CellPad = CardPad - 1;
    private const int ControlH = 40;
    private const int SummaryNoteTop = 136;

    private readonly int summaryH;

    private sealed class PeriodSelector : BufferedPanel
    {
        private readonly PillButton[] buttons;
        private readonly Font activeFont = UI.Body(10.5f, FontStyle.Bold);
        private readonly Font idleFont = UI.Body(10.5f, FontStyle.Regular);
        private int selected;

        public event EventHandler? SelectedChanged;

        public PeriodSelector(string[] labels)
        {
            BackColor = AdminUi.IdlePillBg;
            Height = ControlH;
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
                    Bounds = new Rectangle(x, 4, AdminUi.DrawnWidth(labels[i], 10.5f, FontStyle.Bold) + 28, ControlH - 8)
                };
                int index = i;
                button.Click += (_, _) => Select(index, true);
                button.MouseEnter += (_, _) => { if (index != selected) Repaint(index, AdminUi.Ink); };
                button.MouseLeave += (_, _) => { if (index != selected) Repaint(index, AdminUi.SubInk); };
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
                buttons[i].TextColor = on ? AdminUi.Ink : AdminUi.SubInk;
                buttons[i].BorderColor = on ? AdminUi.Line : Color.Empty;
                buttons[i].Invalidate();
            }
            if (notify) SelectedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private readonly AdminDropdown hazards = new() { Format = o => ((ApiHazard)o).Name };
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

    private static int TextH(float size, FontStyle style)
    {
        using (Font f = UI.Body(size, style))
        {
            return TextRenderer.MeasureText("Ag", f).Height;
        }
    }

    private static Label AddAt(Control parent, string text, int left, int y, int w, int h,
                               float size, bool bold, Color color)
    {
        int pad = AdminUi.TextPad(size, bold ? FontStyle.Bold : FontStyle.Regular);
        return AdminUi.AddText(parent, text, left - pad, y, w + pad, h, size, bold, color);
    }

    public AdminMonitoringPage()
    {
        BackColor = AdminUi.Bg;
        Width = AdminUi.PageMaxW;
        summaryH = SummaryNoteTop + TextH(10f, FontStyle.Regular) + CardPad;

        AddAt(this, "Admin / Log & Monitoring", 0, 5, 300, 20, 10f, false, AdminUi.Muted);
        AddAt(this, "Log & Monitoring", 0, 26, 600, 38, 19f, true, AdminUi.TitleBlue);
        AddAt(this, "Pantau akun pengguna dan asesmen yang selesai.", 0, 66, 800, 24, 10f, false, AdminUi.SubInk);

        var filter = AdminUi.Card(this, 0, AdminUi.ContentTop, Width, FilterH, AdminUi.CardRadius, Color.White, AdminUi.Line);
        AdminUi.Stretch(filter, this, 0, 0);

        AddAt(filter, "Periode", CardPad, 24, 200, 20, 10f, true, AdminUi.Ink);
        periods.Location = new Point(CardPad, 52);
        filter.Controls.Add(periods);

        int hazardX = CardPad + periods.Width + AdminUi.Gap;
        AddAt(filter, "Jenis Bahaya", hazardX, 24, 260, 20, 10f, true, AdminUi.Ink);
        hazards.Bounds = new Rectangle(hazardX, 52, 260, ControlH);
        filter.Controls.Add(hazards);

        AddAt(filter, "Periode menggunakan WIB", CardPad, 100, 400, 20, 10f, false, AdminUi.Muted);

        int summaryTop = AdminUi.ContentTop + FilterH + AdminUi.Gap;
        summaryValues[0] = MakeSummary(0, summaryTop, Ico.People, "Total Pengguna", "Semua waktu");
        summaryValues[1] = MakeSummary(1, summaryTop, Ico.Check, "Pengguna Aktif", "Status akun Aktif");
        summaryValues[2] = MakeSummary(2, summaryTop, Ico.List, "Asesmen Selesai", "Sesuai filter");

        int tableTop = summaryTop + summaryH + AdminUi.Gap;
        tableCard = AdminUi.Card(this, 0, tableTop, Width, TableHeadH + GridRowH + StateH + 24, AdminUi.CardRadius,
            Color.White, AdminUi.Line);
        tableCard.ClipRegion = true;
        AdminUi.Stretch(tableCard, this, 0, 0);

        AddAt(tableCard, "Riwayat Asesmen", CardPad, 16, 320, 26, 11f, true, AdminUi.TitleBlue);
        AddAt(tableCard, "Asesmen terbaru sesuai periode dan jenis bahaya yang dipilih.",
            CardPad, 44, 560, 20, 10f, false, AdminUi.Muted);

        var reload = AdminUi.MakeButton(tableCard, "Muat Ulang", Width - CardPad - 120, 18, 36,
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

        state = AdminUi.AddAligned(tableCard, "Memuat data...", 1, TableHeadH + GridRowH, Width - 2, StateH,
            10f, false, AdminUi.Muted, ContentAlignment.MiddleCenter);
        AdminUi.Stretch(state, tableCard, 1, 1);

        previous = AdminUi.MakeButton(tableCard, "Sebelumnya", CardPad, TableHeadH + GridRowH + 20, 36,
            AdminButtonKind.Secondary, 9.5f, 32);
        next = AdminUi.MakeButton(tableCard, "Berikutnya", previous.Right + 12, previous.Top, 36,
            AdminButtonKind.Secondary, 9.5f, 32);
        pageLabel = AddAt(tableCard, "Halaman 1 dari 1", next.Right + 16, previous.Top + 8, 240, 20,
            10f, false, AdminUi.SubInk);

        previous.Click += async (_, _) => { if (page > 1) { page--; await LoadReportAsync(); } };
        next.Click += async (_, _) => { if ((long)page * pageSize < resultCount) { page++; await LoadReportAsync(); } };
        periods.SelectedChanged += async (_, _) => { if (ready) { page = 1; await LoadReportAsync(); } };
        hazards.SelectedIndexChanged += async (_, _) => { if (ready) { page = 1; await LoadReportAsync(); } };

        SetPageButtons(false);
        LayoutCards();
        Resize += (_, _) => LayoutCards();
        HandleCreated += async (_, _) => { if (!ready) await InitializeAsync(); };
    }

    private Label MakeSummary(int index, int top, string glyph, string caption, string note)
    {
        int cardW = (Width - AdminUi.Gap * 2) / 3;
        int textW = cardW - CardPad * 2;
        var card = AdminUi.Card(this, index * (cardW + AdminUi.Gap), top, cardW, summaryH,
            AdminUi.CardRadius, Color.White, AdminUi.Line);
        summaryCards[index] = card;

        var iconBox = AdminUi.Card(card, CardPad, CardPad, 40, 40, 10, AdminUi.IconBlue, Color.Empty);
        AdminUi.AddSymbol(iconBox, glyph, 0, 0, 40, 40, 13f, AdminUi.Navy);

        Label value = AddAt(card, "–", CardPad, 72, textW, TextH(20f, FontStyle.Bold), 20f, true, AdminUi.TitleBlue);
        AddAt(card, caption, CardPad, 112, textW, TextH(10f, FontStyle.Regular), 10f, false, AdminUi.SubInk);
        AddAt(card, note, CardPad, SummaryNoteTop, textW, TextH(10f, FontStyle.Regular), 10f, false, AdminUi.Muted);
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
            summaryCards[i].SetBounds(left, summaryCards[i].Top, i == 2 ? w - left : cardW, summaryH);
        }
        LayoutTable();
    }

    private void LayoutTable()
    {
        bool hasRows = grid.Rows.Count > 0;
        int headerH = grid.ColumnHeadersHeight;
        grid.Height = hasRows ? headerH + grid.Rows.Count * GridRowH : headerH;

        state.Visible = !hasRows;
        state.Top = TableHeadH + headerH;
        previous.Visible = next.Visible = pageLabel.Visible = hasRows;

        int bodyBottom = TableHeadH + grid.Height + (hasRows ? 0 : StateH);
        if (hasRows)
        {
            previous.Top = bodyBottom + 20;
            next.Top = previous.Top;
            pageLabel.Top = previous.Top + 8;
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
        grid.ColumnHeadersDefaultCellStyle.Font = UI.Body(10f, FontStyle.Bold);
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
            hazards.SetItems(items.Cast<object>());
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