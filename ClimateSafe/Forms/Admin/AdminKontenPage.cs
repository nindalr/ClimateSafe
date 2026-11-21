namespace ClimateSafe;

internal sealed class AdminKontenPage : BufferedPanel
{
    private readonly Form owner;
    private readonly ComboBox hazards = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox articles = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox title = new();
    private readonly TextBox body = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true };
    private readonly TextBox sourceName = new();
    private readonly TextBox sourceUrl = new();
    private readonly Label status;
    private readonly PillButton save;
    private readonly PillButton publish;
    private readonly PillButton unpublish;
    private int selectedIndex = -1;
    private int selectedArticleIndex = -1;
    private bool loading;
    private bool dirty;
    private ApiAdminEducation? current;

    public AdminKontenPage(Form owner)
    {
        this.owner = owner;
        BackColor = AdminUi.Bg;
        Width = AdminUi.PageMaxW;
        Height = 920;
        AdminUi.AddText(this, "Admin / Kelola Konten", 0, 5, 220, 18, 9f, false, AdminUi.Muted);
        AdminUi.AddText(this, "Kelola Konten Edukasi", 0, 26, 600, 38, 19f, true, AdminUi.TitleBlue);
        AdminUi.AddText(this, "Pilih bahaya, simpan draft, lalu publikasikan materi yang sudah diperiksa.", 0, 66, 850, 20, 9.5f, false, AdminUi.SubInk);

        var card = AdminUi.Card(this, 0, AdminUi.ContentTop, Width, 780, AdminUi.CardRadius, Color.White, AdminUi.Line);
        AdminUi.Stretch(card, this, 0, 0);
        AddLabel(card, "Kategori bahaya", 24, 22);
        hazards.Bounds = new Rectangle(24, 45, 400, 30);
        hazards.Font = UI.Body(10f, FontStyle.Regular);
        hazards.DisplayMember = nameof(ApiHazard.Name);
        card.Controls.Add(hazards);
        hazards.SelectedIndexChanged += (_, _) => ChangeHazard();
        status = AdminUi.AddText(card, "Pilih kategori bahaya.", 460, 48, 430, 44, 9f, false, AdminUi.SubInk);
        var reload = AdminUi.MakeButton(card, "Muat Ulang", 920, 40, 36, Color.White, AdminUi.HoverBg, AdminUi.Navy, AdminUi.Line, 9f, 28);
        reload.Click += (_, _) => { if (hazards.Items.Count == 0) LoadHazards(); else ChangeHazard(); };

        AddLabel(card, "Materi", 24, 96);
        articles.Bounds = new Rectangle(24, 120, 600, 30);
        articles.Font = UI.Body(10f, FontStyle.Regular);
        articles.DisplayMember = nameof(ApiAdminEducation.Title);
        card.Controls.Add(articles);
        articles.SelectedIndexChanged += (_, _) => ChangeArticle();
        var add = AdminUi.MakeButton(card, "Materi Baru", 644, 116, 36, Color.White, AdminUi.HoverBg, AdminUi.Navy, AdminUi.Line, 9f, 28);
        add.Click += (_, _) => NewArticle();
        AddLabel(card, "Judul", 24, 166);
        AddField(card, title, 24, 190, 600, 30);
        AddLabel(card, "Isi materi", 24, 236);
        AddField(card, body, 24, 260, 900, 250);
        AdminUi.AddText(card, "Gunakan baris baru untuk paragraf dan awali baris dengan '- ' untuk poin.", 24, 517, 850, 20, 9f, false, AdminUi.Muted);
        AddLabel(card, "Nama sumber (opsional)", 24, 548);
        AddField(card, sourceName, 24, 572, 440, 30);
        AddLabel(card, "URL sumber (opsional)", 484, 548);
        AddField(card, sourceUrl, 484, 572, 440, 30);

        save = AdminUi.MakeButton(card, "Simpan Draft", 24, 636, 44, AdminUi.BtnBlue, AdminUi.BtnBlueHover, Color.White, Color.Empty, 10f, 40);
        publish = AdminUi.MakeButton(card, "Publikasikan", save.Right + 12, 636, 44, AdminUi.Coral, AdminUi.Coral, Color.White, Color.Empty, 10f, 40);
        unpublish = AdminUi.MakeButton(card, "Tarik Publikasi", publish.Right + 12, 636, 44, Color.White, AdminUi.HoverBg, AdminUi.Navy, AdminUi.Line, 10f, 40);
        save.Click += (_, _) => SaveDraft();
        publish.Click += (_, _) => ChangePublication(true);
        unpublish.Click += (_, _) => ChangePublication(false);
        foreach (var field in new[] { title, body, sourceName, sourceUrl })
            field.TextChanged += (_, _) => { if (!loading) { dirty = true; UpdateStatus(); } };
        SetEnabled(false);
        LoadHazards();
    }

    private static void AddLabel(Control parent, string text, int x, int y) =>
        AdminUi.AddText(parent, text, x, y, 400, 20, 9.5f, true, AdminUi.Ink);

    private static void AddField(Control parent, TextBox field, int x, int y, int width, int height)
    {
        field.Bounds = new Rectangle(x, y, width, height);
        field.Font = UI.Body(10f, FontStyle.Regular);
        parent.Controls.Add(field);
    }

    public bool ConfirmDiscard() => !dirty || MessageBox.Show(owner,
        "Ada perubahan draft yang belum disimpan. Abaikan perubahan?", "Kelola Konten",
        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

    private void LoadHazards()
    {
        try
        {
            var items = ApiClient.Get<List<ApiHazard>>("api/hazards") ?? new();
            hazards.DataSource = items;
            if (items.Count == 0) status.Text = "Belum ada kategori bahaya.";
        }
        catch (Exception ex) { ShowError(ex); status.Text = "Kategori bahaya tidak dapat dimuat. Buka ulang halaman untuk mencoba lagi."; }
    }

    private void ChangeHazard()
    {
        if (loading) return;
        if (!ConfirmDiscard())
        {
            loading = true;
            hazards.SelectedIndex = selectedIndex;
            loading = false;
            return;
        }
        selectedIndex = hazards.SelectedIndex;
        current = null;
        dirty = false;
        SetEnabled(false);
        if (hazards.SelectedItem is not ApiHazard hazard) { ClearFields(); UpdateStatus(); return; }
        try
        {
            LoadArticles(hazard.Id);
            SetEnabled(true);
        }
        catch (Exception ex) { ClearFields(); ShowError(ex); status.Text = "Materi tidak dapat dimuat. Gunakan Muat Ulang."; }
    }

    private void LoadArticles(int hazardId, int? selectId = null)
    {
        var items = ApiClient.Get<List<ApiAdminEducation>>($"api/admin/education?hazardId={hazardId}") ?? new();
        loading = true;
        articles.DataSource = items;
        int index = selectId.HasValue ? items.FindIndex(x => x.ContentId == selectId.Value) : (items.Count > 0 ? 0 : -1);
        articles.SelectedIndex = index;
        selectedArticleIndex = index;
        current = index < 0 ? null : items[index];
        loading = false;
        LoadFields();
    }

    private void ChangeArticle()
    {
        if (loading) return;
        if (!ConfirmDiscard())
        {
            loading = true;
            articles.SelectedIndex = selectedArticleIndex;
            loading = false;
            return;
        }
        selectedArticleIndex = articles.SelectedIndex;
        current = articles.SelectedItem as ApiAdminEducation;
        LoadFields();
    }

    private void NewArticle()
    {
        if (hazards.SelectedItem is not ApiHazard || !title.Enabled || !ConfirmDiscard()) return;
        loading = true;
        articles.SelectedIndex = -1;
        selectedArticleIndex = -1;
        current = null;
        loading = false;
        LoadFields();
        title.Focus();
    }

    private void ClearFields()
    {
        loading = true;
        title.Clear(); body.Clear(); sourceName.Clear(); sourceUrl.Clear();
        loading = false;
    }

    private void LoadFields()
    {
        loading = true;
        title.Text = current?.Title ?? "";
        body.Text = current?.Body ?? "";
        sourceName.Text = current?.SourceName ?? "";
        sourceUrl.Text = current?.SourceUrl ?? "";
        dirty = false;
        loading = false;
        UpdateStatus();
    }

    private void SetEnabled(bool enabled)
    {
        title.Enabled = body.Enabled = sourceName.Enabled = sourceUrl.Enabled = enabled;
        articles.Enabled = enabled;
        save.Enabled = publish.Enabled = unpublish.Enabled = enabled;
    }

    private void UpdateStatus()
    {
        string state = current == null ? "Belum ada draft" : current.IsPublished ? "Terpublikasi" : "Draft belum dipublikasikan";
        bool savedChanges = current?.IsPublished == true &&
            (current.Title != current.PublishedTitle || current.Body != current.PublishedBody ||
             current.SourceName != current.PublishedSourceName || current.SourceUrl != current.PublishedSourceUrl);
        status.Text = state + (savedChanges ? " · Draft berbeda dari versi publik" : "") + (dirty ? " · Ada perubahan belum disimpan" : "");
    }

    private void SaveDraft()
    {
        if (hazards.SelectedItem is not ApiHazard hazard) return;
        try
        {
            var request = new { hazardId = hazard.Id, title = title.Text, body = body.Text, sourceName = sourceName.Text, sourceUrl = sourceUrl.Text };
            current = (current == null
                ? ApiClient.Post<ApiAdminEducation>("api/admin/education", request)
                : ApiClient.Put<ApiAdminEducation>($"api/admin/education/{current.ContentId}", request))
                ?? throw new InvalidOperationException("Respons materi kosong.");
            LoadFields();
            LoadArticles(hazard.Id, current.ContentId);
            MessageBox.Show(owner, "Draft berhasil disimpan.", "Kelola Konten", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void ChangePublication(bool shouldPublish)
    {
        if (hazards.SelectedItem is not ApiHazard hazard) return;
        if (dirty || current == null)
        {
            MessageBox.Show(owner, "Simpan draft terlebih dahulu sebelum mengubah publikasi.", "Kelola Konten", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!shouldPublish && !current.IsPublished)
        {
            MessageBox.Show(owner, "Materi belum dipublikasikan.", "Kelola Konten", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            string action = shouldPublish ? "publish" : "unpublish";
            current = ApiClient.Post<ApiAdminEducation>($"api/admin/education/{current.ContentId}/{action}")
                ?? throw new InvalidOperationException("Respons materi kosong.");
            LoadArticles(hazard.Id, current.ContentId);
            MessageBox.Show(owner, shouldPublish ? "Materi berhasil dipublikasikan." : "Publikasi berhasil ditarik.",
                "Kelola Konten", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void ShowError(Exception ex) => MessageBox.Show(owner, ex.Message, "Kelola Konten", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
