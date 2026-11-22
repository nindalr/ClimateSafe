using System.Text.Json;

namespace ClimateSafe.Core;

public sealed record GuestOption(int DisplayOrder, string Text, int Score);
public sealed record GuestQuestion(int DimensionId, string DimensionName, double Threshold,
    string Text, string Description, IReadOnlyList<GuestOption> Options, string Recommendation);
public sealed record GuestHazard(int Id, string Name, IReadOnlyList<GuestQuestion> Questions);
public sealed record GuestCatalogData(string Version, IReadOnlyList<GuestHazard> Hazards);

public static class GuestCatalog
{
    // Each caller gets its own copy; the embedded file is the initial SQL seed,
    // not necessarily the current catalog of an existing server database.
    public static GuestCatalogData Load()
    {
        using var stream = typeof(GuestCatalog).Assembly.GetManifestResourceStream("ClimateSafe.Core.guest-catalog.json")
            ?? throw new InvalidOperationException("Katalog guest tidak ditemukan.");
        return JsonSerializer.Deserialize<GuestCatalogData>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Katalog guest tidak valid.");
    }
}
