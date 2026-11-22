namespace ClimateSafe;

internal sealed class AdminKontenPage : BufferedPanel
{
    private const int HazardCardH = 72;
    private const int HazardGap = 12;
    private const int ListRowH = 48;
    private const int BodyBoxH = 256;
    private const int FooterPad = 16;
    private const int FooterBtnH = 40;
    private const int FooterBtnPad = 28;
    private const int NoteLineH = 18;

    private readonly Form owner;

    private readonly BufferedPanel leftColumn = new();
    private readonly BufferedPanel hazardHost = new();
    private readonly RoundPanel materialCard;
    private readonly BufferedPanel materialHost = new();
    private readonly Label materialEmpty;
    private readonly PillButton addArticle;

    private readonly RoundPanel editCard;
    private readonly BufferedPanel footer = new();
    private readonly Label editTitle;
    private readonly TextBox title;
    private readonly TextBox body;
    private readonly TextBox sourceName;
    private readonly TextBox sourceUrl;
    private readonly RoundPanel statePill;
    private readonly Label statePillText;
    private readonly Label stateNote;
    private readonly Label successNote;
    private readonly PillButton save;
    private readonly PillButton publish;
    private readonly PillButton unpublish;
    private readonly PillButton discard;

    private readonly List<ApiHazard> hazardList = new();
    private readonly List<ApiAdminEducation> allContents = new();
    private readonly List<ApiAdminEducation> hazardContents = new();
    private readonly List<RoundPanel> hazardCards = new();
    private readonly List<RoundPanel> materialRows = new();

    private int editCardH;
    private int footerTop;
    private string noteText = "";
    private int selectedIndex = -1;
    private int selectedArticleIndex = -1;
    private bool loading;
    private bool dirty;
    private ApiAdminEducation? current;

    public AdminKontenPage(Form owner)
    {
        this.owner = owner;
        BackColor = AdminUi.Bg;
        // Mulai dari lebar halaman terkecil: FrmAdmin.LayoutPage melebarkan setelah
        // halaman dipasang, sehingga tidak pernah lebih lebar dari area konten.
        Width = AdminUi.PageMinW;

        AdminUi.AddText(this, "Admin / Kelola Konten", 0, 5, 220, 18, 9f, false, AdminUi.Muted);
        AdminUi.AddText(this, "Kelola Konten Edukasi", 0, 26, 600, 38, 19f, true, AdminUi.TitleBlue);
        AdminUi.AddText(this, "Pilih bahaya, simpan draft, lalu publikasikan materi yang sudah diperiksa.",
            0, 66, 850, 20, 9.5f, false, AdminUi.SubInk);

        int leftW = LeftWidth(Width);
        int rightW = Width - leftW - AdminUi.Gap;

        // ================= kolom kiri =================
        leftColumn.BackColor = AdminUi.Bg;
        leftColumn.Bounds = new Rectangle(0, AdminUi.ContentTop, leftW, 400);
        Controls.Add(leftColumn);

        AdminUi.AddText(leftColumn, "Jenis Bahaya", 0, 0, 300, 24, 11f, true, AdminUi.TitleBlue);

        hazardHost.BackColor = AdminUi.Bg;
        hazardHost.Bounds = new Rectangle(0, 32, leftW, HazardCardH);
        leftColumn.Controls.Add(hazardHost);
        AdminUi.Stretch(hazardHost, leftColumn, 0, 0);

        materialCard = AdminUi.Card(leftColumn, 0, 140, leftW, 240, AdminUi.CardRadius, Color.White, AdminUi.Line);
        materialCard.ClipRegion = true;
        AdminUi.Stretch(materialCard, leftColumn, 0, 0);
        AdminUi.AddText(materialCard, "Materi", 24, 20, 200, 24, 11f, true, AdminUi.TitleBlue);

        materialHost.BackColor = Color.White;
        materialHost.Bounds = new Rectangle(1, 56, leftW - 2, ListRowH);
        materialCard.Controls.Add(materialHost);
        AdminUi.Stretch(materialHost, materialCard, 1, 1);

        materialEmpty = AdminUi.AddText(materialCard, "Belum ada materi untuk bahaya ini.", 24, 64,
            leftW - 48, 20, 9.5f, false, AdminUi.SubInk);
        AdminUi.Stretch(materialEmpty, materialCard, 24, 24);

        addArticle = AdminUi.MakeButton(materialCard, "Materi Baru", 24, 120, 40,
            AdminButtonKind.Secondary, 9.5f, 36);
        addArticle.Click += (_, _) => NewArticle();

        // ================= kolom kanan =================
        editCard = AdminUi.Card(this, leftW + AdminUi.Gap, AdminUi.ContentTop, rightW, 700,
            AdminUi.CardRadius, Color.White, AdminUi.Line);
        editCard.ClipRegion = true;

        editTitle = AdminUi.AddText(editCard, "Edit Konten", 24, 20, rightW - 48, 24, 11f, true, AdminUi.TitleBlue);
        AdminUi.Stretch(editTitle, editCard, 24, 24);
        Label editSubtitle = AdminUi.AddText(editCard, "Draft tidak terlihat pengguna sampai dipublikasikan.",
            24, 46, rightW - 48, 18, 9f, false, AdminUi.Muted);
        AdminUi.Stretch(editSubtitle, editCard, 24, 24);
        Panel headLine = AdminUi.Divider(editCard, 76, rightW);
        AdminUi.Stretch(headLine, editCard, 0, 0);

        AdminUi.AddText(editCard, "Judul", 24, 100, 200, 16, 9f, true, AdminUi.Muted);
        title = AdminUi.MakeField(editCard, 24, 122, rightW - 48, "");
        AdminUi.Stretch(FieldBox(title), editCard, 24, 24);

        AdminUi.AddText(editCard, "Isi materi", 24, 182, 200, 16, 9f, true, AdminUi.Muted);
        body = MakeBodyBox(editCard, 24, 204, rightW - 48, BodyBoxH);

        // Teks bantu dibungkus pada lebar kartu tersempit agar tidak terpotong.
        int helpTop = 204 + BodyBoxH + 8;
        int minRightW = AdminUi.PageMinW - LeftWidth(AdminUi.PageMinW) - AdminUi.Gap;
        List<string> helpLines;
        using (Font helpFont = UI.Body(9f, FontStyle.Regular))
        {
            helpLines = AdminUi.WrapLines(
                "Tulis satu paragraf per baris. Awali baris dengan '- ' untuk membuat poin. " +
                "Beri baris kosong untuk memisahkan bagian.", helpFont, minRightW - 48);
        }
        for (int i = 0; i < helpLines.Count; i++)
        {
            Label helpLine = AdminUi.AddText(editCard, helpLines[i], 24, helpTop + i * 18, rightW - 48, 18,
                9f, false, AdminUi.Muted);
            AdminUi.Stretch(helpLine, editCard, 24, 24);
        }

        int sourceTop = helpTop + Math.Max(1, helpLines.Count) * 18 + 12;
        int halfW = (rightW - 48 - AdminUi.Gap) / 2;
        AdminUi.AddText(editCard, "Nama sumber (opsional)", 24, sourceTop, 260, 16, 9f, true, AdminUi.Muted);
        sourceName = AdminUi.MakeField(editCard, 24, sourceTop + 22, halfW, "");
        Label urlLabel = AdminUi.AddText(editCard, "URL sumber (opsional)", 24 + halfW + AdminUi.Gap, sourceTop,
            260, 16, 9f, true, AdminUi.Muted);
        sourceUrl = AdminUi.MakeField(editCard, 24 + halfW + AdminUi.Gap, sourceTop + 22, halfW, "");

        // Dua kolom sumber selalu sama lebar dan ikut lebar kartu.
        Control sourceNameBox = FieldBox(sourceName);
        Control sourceUrlBox = FieldBox(sourceUrl);
        editCard.Resize += (_, _) =>
        {
            int half = Math.Max(120, (editCard.Width - 48 - AdminUi.Gap) / 2);
            sourceNameBox.SetBounds(24, sourceNameBox.Top, half, AdminUi.FieldH);
            int rightX = 24 + half + AdminUi.Gap;
            urlLabel.Left = rightX;
            sourceUrlBox.SetBounds(rightX, sourceUrlBox.Top, half, AdminUi.FieldH);
        };

        successNote = AdminUi.AddText(editCard, "", 24, sourceTop + 22 + AdminUi.FieldH + 20, rightW - 48, 18,
            9.5f, true, AdminUi.SuccessInk);
        AdminUi.Stretch(successNote, editCard, 24, 24);


        // ---- footer kartu: status di kiri, tombol di kanan ----
        footer.BackColor = AdminUi.BandBg;
        footerTop = successNote.Bottom + 16;
        footer.Bounds = new Rectangle(1, footerTop, rightW - 2, FooterPad * 2 + 26 + 12 + FooterBtnH);
        editCard.Controls.Add(footer);
        AdminUi.Stretch(footer, editCard, 1, 1);
        AdminUi.Stretch(AdminUi.Divider(footer, 0, rightW), footer, 0, 0);

        statePill = AdminUi.Card(footer, 24, FooterPad, 110, 26, 13, AdminUi.IdlePillBg, Color.Empty);
        statePillText = AdminUi.AddAligned(statePill, "Draft", 0, 0, 110, 26, 9f, true, AdminUi.IdlePillInk,
            ContentAlignment.MiddleCenter);
        stateNote = AdminUi.AddText(footer, "", 146, FooterPad + 4, 200, 18, 9f, false, AdminUi.Muted);

        // Padding teks tombol 28 agar empat tombol muat pada kartu tersempit (halaman 960).
        save = AdminUi.MakeButton(footer, "Simpan Draft", 24, 0, FooterBtnH,
            AdminButtonKind.Primary, 10f, FooterBtnPad, false);
        publish = AdminUi.MakeButton(footer, "Publikasikan", 24, 0, FooterBtnH,
            AdminButtonKind.Secondary, 10f, FooterBtnPad, false);
        discard = AdminUi.MakeButton(footer, "Batalkan", 24, 0, FooterBtnH,
            AdminButtonKind.Quiet, 10f, FooterBtnPad, false);
        unpublish = AdminUi.MakeButton(footer, "Tarik Publikasi", 24, 0, FooterBtnH,
            AdminButtonKind.Danger, 10f, FooterBtnPad, false);

        footer.Resize += (_, _) => LayoutFooter();
        LayoutFooter();

        save.Click += (_, _) => SaveDraft();
        publish.Click += (_, _) => ChangePublication(true);
        unpublish.Click += (_, _) => ChangePublication(false);
        discard.Click += (_, _) => DiscardChanges();

        foreach (var field in new[] { title, body, sourceName, sourceUrl })
            field.TextChanged += (_, _) => { if (!loading) { dirty = true; successNote.Text = ""; UpdateStatus(); } };

        SetFieldsEnabled(false);
        Resize += (_, _) => LayoutColumns();
        LayoutColumns();
        LoadAll();
    }

    private static int LeftWidth(int width) => (width - AdminUi.Gap) / 3;

    private static Control FieldBox(TextBox field) => (Control)field.Tag!;

    // Kotak isi materi: kontainer putih bergaris dengan TextBox tanpa border.
    private static TextBox MakeBodyBox(Control parent, int x, int y, int w, int h)
    {
        var box = AdminUi.Card(parent, x, y, w, h, 8, Color.White, AdminUi.Line);
        var input = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Multiline = true,
            AcceptsReturn = true,
            ScrollBars = ScrollBars.Vertical,
            Font = UI.Body(10f, FontStyle.Regular),
            ForeColor = AdminUi.Ink,
            BackColor = Color.White,
            Bounds = new Rectangle(14, 12, Math.Max(80, w - 28), h - 24),
            Tag = box
        };
        box.Controls.Add(input);
        box.Resize += (_, _) => input.SetBounds(14, 12, Math.Max(80, box.Width - 28), box.Height - 24);
        AdminUi.Stretch(box, parent, x, x);
        return input;
    }

    private void LayoutColumns()
    {
        int w = Math.Max(AdminUi.PageMinW, Width);
        int leftW = LeftWidth(w);
        int rightW = w - leftW - AdminUi.Gap;

        leftColumn.SetBounds(0, AdminUi.ContentTop, leftW, leftColumn.Height);
        editCard.SetBounds(leftW + AdminUi.Gap, AdminUi.ContentTop, rightW, editCardH);
        LayoutLeftColumn();
    }

    private void LayoutLeftColumn()
    {
        int cards = Math.Max(hazardCards.Count, 1);
        int hostH = hazardCards.Count == 0 ? 72 : cards * HazardCardH + (cards - 1) * HazardGap;
        hazardHost.Height = hostH;

        int rows = materialRows.Count;
        materialHost.Height = rows == 0 ? 0 : rows * ListRowH + (rows - 1);
        materialHost.Visible = rows > 0;
        materialEmpty.Visible = rows == 0;

        int listBottom = 56 + (rows == 0 ? 28 : materialHost.Height);
        addArticle.Top = listBottom + 16;
        materialCard.Top = 32 + hostH + AdminUi.Gap;
        materialCard.Height = addArticle.Bottom + 24;

        leftColumn.Height = materialCard.Bottom;
        Height = AdminUi.ContentTop + Math.Max(leftColumn.Height, editCardH) + AdminUi.PagePad;
    }

    // Footer dua baris: status (boleh membungkus) di atas, tombol di bawah.
    private void LayoutFooter()
    {
        stateNote.Left = statePill.Right + 12;
        int noteW = Math.Max(120, footer.Width - 24 - stateNote.Left);

        List<string> lines;
        using (Font noteFont = UI.Body(9f, FontStyle.Regular))
        {
            lines = AdminUi.WrapLines(noteText, noteFont, noteW);
        }
        stateNote.Text = string.Join(Environment.NewLine, lines);
        int noteH = Math.Max(1, lines.Count) * NoteLineH;
        stateNote.SetBounds(stateNote.Left, FooterPad, noteW, noteH);

        int topRowH = Math.Max(26, noteH);
        statePill.Top = FooterPad;
        int buttonsTop = FooterPad + topRowH + 12;

        unpublish.SetBounds(24, buttonsTop, unpublish.Width, FooterBtnH);
        int x = footer.Width - 24;
        foreach (PillButton button in new[] { save, publish, discard })
        {
            x -= button.Width;
            button.SetBounds(x, buttonsTop, button.Width, FooterBtnH);
            x -= 12;
        }

        int height = buttonsTop + FooterBtnH + FooterPad;
        if (footer.Height != height) footer.Height = height;

        int cardH = footerTop + footer.Height;
        if (editCardH != cardH)
        {
            editCardH = cardH;
            editCard.Height = cardH;
            Height = AdminUi.ContentTop + Math.Max(leftColumn.Height, editCardH) + AdminUi.PagePad;
        }
    }

    // TextBox multiline memakai CRLF; API menyimpan LF.
    private static string ToBox(string? value) =>
        (value ?? "").Replace("\r\n", "\n").Replace("\n", "\r\n");

    private static string ToApi(string value) => value.Replace("\r\n", "\n");

    // ---------------- data ----------------

    private void LoadAll(int? selectContentId = null)
    {
        try
        {
            var items = ApiClient.Get<List<ApiHazard>>("api/hazards") ?? new();
            var contents = ApiClient.Get<List<ApiAdminEducation>>("api/admin/education") ?? new();

            hazardList.Clear();
            hazardList.AddRange(items);
            allContents.Clear();
            allContents.AddRange(contents);

            BuildHazardCards();

            if (hazardList.Count == 0)
            {
                selectedIndex = -1;
                current = null;
                hazardContents.Clear();
                BuildMaterialRows();
                ClearFields();
                SetFieldsEnabled(false);
                editTitle.Text = "Edit Konten";
                stateNote.Text = "Belum ada kategori bahaya.";
                return;
            }

            int index = selectedIndex >= 0 && selectedIndex < hazardList.Count ? selectedIndex : 0;
            ApplyHazard(index, selectContentId);
        }
        catch (Exception ex)
        {
            ShowError(ex);
            stateNote.Text = "Data tidak dapat dimuat. Pakai Materi Baru atau buka ulang halaman untuk mencoba lagi.";
            SetFieldsEnabled(false);
        }
    }

    private void BuildHazardCards()
    {
        foreach (var card in hazardCards) { hazardHost.Controls.Remove(card); card.Dispose(); }
        hazardCards.Clear();

        for (int i = 0; i < hazardList.Count; i++)
        {
            ApiHazard hazard = hazardList[i];
            var card = AdminUi.Card(hazardHost, 0, i * (HazardCardH + HazardGap), hazardHost.Width, HazardCardH,
                12, Color.White, AdminUi.Line);
            AdminUi.Stretch(card, hazardHost, 0, 0);

            var iconBox = AdminUi.Card(card, 16, 18, 36, 36, 8, AdminUi.IconBlue, Color.Empty);
            AdminUi.AddSymbol(iconBox, HazardGlyph(hazard.Name), 0, 0, 36, 36, 12f, AdminUi.Navy);

            AdminUi.AddText(card, hazard.Name, 64, 16, 200, 20, 10f, true, AdminUi.Ink);
            AdminUi.AddText(card, HazardStatus(hazard.Id), 64, 38, 220, 18, 9f, false, AdminUi.Muted);

            var chevron = new Chevron { LineColor = AdminUi.Muted, Bounds = new Rectangle(card.Width - 32, 28, 16, 16) };
            card.Controls.Add(chevron);
            AdminUi.PinRight(chevron, card, 16);

            int index = i;
            AdminUi.MakeClickable(card, (_, _) => SelectHazard(index));
            hazardCards.Add(card);
        }

        PaintHazardSelection();
        LayoutLeftColumn();
    }

    private string HazardStatus(int hazardId)
    {
        int total = 0;
        int published = 0;
        foreach (var item in allContents)
        {
            if (item.HazardId != hazardId) continue;
            total++;
            if (item.IsPublished) published++;
        }

        if (total == 0) return "Belum ada materi";
        if (published == 0) return "Draft saja";
        return total + " materi · " + published + " terpublikasi";
    }

    // Pemetaan ikon sama dengan FrmEdukasi.
    private static string HazardGlyph(string hazardName)
    {
        if (string.Equals(hazardName, "Cuaca Ekstrem", StringComparison.OrdinalIgnoreCase)) return "☂";
        if (string.Equals(hazardName, "Suhu Panas Ekstrem", StringComparison.OrdinalIgnoreCase)) return "☀";
        return "≈";
    }

    private void PaintHazardSelection()
    {
        for (int i = 0; i < hazardCards.Count; i++)
        {
            bool on = i == selectedIndex;
            hazardCards[i].Fill = on ? AdminUi.IconBlue : Color.White;
            hazardCards[i].BorderColor = on ? AdminUi.Navy : AdminUi.Line;
            hazardCards[i].Invalidate();
        }
    }

    private void SelectHazard(int index)
    {
        if (loading || index == selectedIndex) return;
        if (!ConfirmDiscard()) return;
        ApplyHazard(index, null);
    }

    private void ApplyHazard(int index, int? selectContentId)
    {
        selectedIndex = index;
        PaintHazardSelection();

        ApiHazard hazard = hazardList[index];
        editTitle.Text = "Edit Konten · " + hazard.Name;

        hazardContents.Clear();
        foreach (var item in allContents)
        {
            if (item.HazardId == hazard.Id) hazardContents.Add(item);
        }

        int target = selectContentId.HasValue
            ? hazardContents.FindIndex(x => x.ContentId == selectContentId.Value)
            : (hazardContents.Count > 0 ? 0 : -1);

        selectedArticleIndex = target;
        current = target < 0 ? null : hazardContents[target];
        BuildMaterialRows();
        SetFieldsEnabled(true);
        LoadFields();
    }

    private void BuildMaterialRows()
    {
        foreach (var row in materialRows) { materialHost.Controls.Remove(row); row.Dispose(); }
        materialRows.Clear();

        for (int i = 0; i < hazardContents.Count; i++)
        {
            ApiAdminEducation item = hazardContents[i];
            var row = AdminUi.Card(materialHost, 0, i * (ListRowH + 1), materialHost.Width, ListRowH, 0,
                Color.White, Color.Empty);
            AdminUi.Stretch(row, materialHost, 0, 0);

            string pillText = item.IsPublished ? "Terpublikasi" : "Draft";
            int pillW = AdminUi.LabelWidth(pillText, 8.5f, FontStyle.Bold) + 20;
            var pill = AdminUi.Card(row, materialHost.Width - 20 - pillW, (ListRowH - 22) / 2, pillW, 22, 11,
                item.IsPublished ? AdminUi.SuccessBg : AdminUi.IdlePillBg, Color.Empty);
            AdminUi.AddAligned(pill, pillText, 0, 0, pillW, 22, 8.5f, true,
                item.IsPublished ? AdminUi.SuccessInk : AdminUi.IdlePillInk, ContentAlignment.MiddleCenter);
            AdminUi.PinRight(pill, row, 20);

            Label label = AdminUi.AddAligned(row, item.Title, 20, 0,
                Math.Max(80, materialHost.Width - 48 - pillW), ListRowH, 9.5f, false, AdminUi.Ink,
                ContentAlignment.MiddleLeft);
            label.AutoEllipsis = true;
            AdminUi.Stretch(label, row, 20, 28 + pillW);

            if (i < hazardContents.Count - 1)
            {
                var separator = AdminUi.Divider(materialHost, (i + 1) * (ListRowH + 1) - 1, materialHost.Width);
                AdminUi.Stretch(separator, materialHost, 0, 0);
            }

            int index = i;
            AdminUi.MakeClickable(row, (_, _) => SelectArticle(index));
            materialRows.Add(row);
        }

        PaintArticleSelection();
        LayoutLeftColumn();
    }

    private void PaintArticleSelection()
    {
        for (int i = 0; i < materialRows.Count; i++)
        {
            bool on = i == selectedArticleIndex;
            materialRows[i].Fill = on ? AdminUi.IconBlue : Color.White;
            materialRows[i].Invalidate();
        }
    }

    private void SelectArticle(int index)
    {
        if (loading || index == selectedArticleIndex) return;
        if (!ConfirmDiscard()) return;

        selectedArticleIndex = index;
        current = index >= 0 && index < hazardContents.Count ? hazardContents[index] : null;
        PaintArticleSelection();
        LoadFields();
    }

    private void NewArticle()
    {
        if (selectedIndex < 0 || !title.Enabled || !ConfirmDiscard()) return;
        selectedArticleIndex = -1;
        current = null;
        PaintArticleSelection();
        LoadFields();
        title.Focus();
    }

    private void ClearFields()
    {
        loading = true;
        title.Clear();
        body.Clear();
        sourceName.Clear();
        sourceUrl.Clear();
        loading = false;
    }

    private void LoadFields()
    {
        loading = true;
        title.Text = current?.Title ?? "";
        body.Text = ToBox(current?.Body);
        sourceName.Text = current?.SourceName ?? "";
        sourceUrl.Text = current?.SourceUrl ?? "";
        dirty = false;
        loading = false;
        UpdateStatus();
    }

    private void DiscardChanges()
    {
        if (!dirty) return;
        successNote.Text = "";
        LoadFields();
    }

    private void SetFieldsEnabled(bool enabled)
    {
        title.Enabled = body.Enabled = sourceName.Enabled = sourceUrl.Enabled = enabled;
        AdminUi.StyleButton(addArticle, AdminButtonKind.Secondary, enabled);
        UpdateStatus();
    }

    // Satu tempat yang menentukan keadaan tombol footer dan alasannya.
    private void UpdateButtons()
    {
        bool fields = title.Enabled;
        bool filled = title.Text.Trim().Length > 0 && body.Text.Trim().Length > 0;
        bool published = current?.IsPublished == true;
        bool differs = published && DraftDiffersFromPublished();

        bool canSave = fields && dirty && filled;
        bool canDiscard = fields && dirty;
        bool canPublish = fields && current != null && !dirty && (!published || differs);
        bool canUnpublish = fields && published;

        AdminUi.StyleButton(unpublish, AdminButtonKind.Danger, canUnpublish);
        AdminUi.StyleButton(discard, AdminButtonKind.Quiet, canDiscard);
        AdminUi.StyleButton(publish, AdminButtonKind.Secondary, canPublish);
        AdminUi.StyleButton(save, AdminButtonKind.Primary, canSave);

        string note;
        if (!fields) note = "Pilih bahaya untuk mulai menyunting.";
        else if (!filled) note = "Judul dan isi materi wajib diisi.";
        else if (dirty) note = "Simpan draft dulu sebelum mempublikasikan.";
        else if (current == null) note = "Belum ada perubahan untuk disimpan.";
        else if (differs) note = "Draft berbeda dari versi publik.";
        else if (published) note = "Sudah terpublikasi dan sama dengan draft.";
        else note = "Belum ada perubahan untuk disimpan.";
        noteText = note;
        LayoutFooter();
    }

    // Perbandingan memakai data tersimpan dari API, bukan isi TextBox.
    private bool DraftDiffersFromPublished()
    {
        if (current == null || !current.IsPublished) return false;
        return current.Title != current.PublishedTitle || current.Body != current.PublishedBody ||
               current.SourceName != current.PublishedSourceName ||
               current.SourceUrl != current.PublishedSourceUrl;
    }

    private void UpdateStatus()
    {
        bool published = current?.IsPublished == true;
        string pillText = published ? "Terpublikasi" : "Draft";
        int pillW = AdminUi.LabelWidth(pillText, 9f, FontStyle.Bold) + 28;
        statePill.Width = pillW;
        statePill.Fill = published ? AdminUi.SuccessBg : AdminUi.IdlePillBg;
        statePill.Invalidate();
        statePillText.Text = pillText;
        statePillText.Bounds = new Rectangle(0, 0, pillW, 26);
        statePillText.ForeColor = published ? AdminUi.SuccessInk : AdminUi.IdlePillInk;
        stateNote.Left = statePill.Right + 12;

        UpdateButtons();
    }

    // ---------------- aksi ----------------

    public bool ConfirmDiscard() => !dirty || MessageBox.Show(owner,
        "Ada perubahan draft yang belum disimpan. Abaikan perubahan?", "Kelola Konten",
        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

    private void SaveDraft()
    {
        if (selectedIndex < 0) return;
        ApiHazard hazard = hazardList[selectedIndex];
        try
        {
            var request = new
            {
                hazardId = hazard.Id, title = title.Text, body = ToApi(body.Text),
                sourceName = sourceName.Text, sourceUrl = sourceUrl.Text
            };
            current = (current == null
                ? ApiClient.Post<ApiAdminEducation>("api/admin/education", request)
                : ApiClient.Put<ApiAdminEducation>($"api/admin/education/{current.ContentId}", request))
                ?? throw new InvalidOperationException("Respons materi kosong.");

            int contentId = current.ContentId;
            LoadAll(contentId);
            successNote.Text = "Draft berhasil disimpan.";
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void ChangePublication(bool shouldPublish)
    {
        if (selectedIndex < 0) return;
        if (dirty || current == null)
        {
            MessageBox.Show(owner, "Simpan draft terlebih dahulu sebelum mengubah publikasi.", "Kelola Konten",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!shouldPublish && !current.IsPublished)
        {
            MessageBox.Show(owner, "Materi belum dipublikasikan.", "Kelola Konten",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!shouldPublish && !AdminUi.Confirm(owner, "Tarik publikasi?",
                "Materi \"" + current.Title + "\" tidak akan terlihat pengguna sampai dipublikasikan lagi.",
                "Ya, Tarik Publikasi", true))
        {
            return;
        }

        try
        {
            string action = shouldPublish ? "publish" : "unpublish";
            current = ApiClient.Post<ApiAdminEducation>($"api/admin/education/{current.ContentId}/{action}")
                ?? throw new InvalidOperationException("Respons materi kosong.");

            int contentId = current.ContentId;
            LoadAll(contentId);
            successNote.Text = shouldPublish ? "Materi berhasil dipublikasikan." : "Publikasi berhasil ditarik.";
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void ShowError(Exception ex) =>
        MessageBox.Show(owner, ex.Message, "Kelola Konten", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
