using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ClimateSafe;

internal static class ApiClient
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public static string? Token { get; set; }

    public static T? Get<T>(string path) => Send<T>(HttpMethod.Get, path);
    public static T? Post<T>(string path, object? body = null) => Send<T>(HttpMethod.Post, path, body);
    public static T? Patch<T>(string path, object body) => Send<T>(HttpMethod.Patch, path, body);
    public static T? Put<T>(string path, object body) => Send<T>(HttpMethod.Put, path, body);
    public static T? Delete<T>(string path) => Send<T>(HttpMethod.Delete, path);

    private static T? Send<T>(HttpMethod method, string path, object? body = null)
    {
        string baseUrl = Environment.GetEnvironmentVariable("CLIMATESAFE_API_URL") ?? "http://localhost:5028";
        if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttps && !(baseUri.Scheme == Uri.UriSchemeHttp && baseUri.IsLoopback)))
            throw new InvalidOperationException("Alamat API harus menggunakan HTTPS, atau HTTP untuk localhost.");
        using var request = new HttpRequestMessage(method, new Uri(baseUri, path));
        if (!string.IsNullOrEmpty(Token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        if (body != null) request.Content = JsonContent.Create(body);
        try
        {
            using var response = Client.SendAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
            string json = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                string message = response.StatusCode == HttpStatusCode.Unauthorized ? "Sesi tidak berlaku. Silakan login kembali." :
                    response.StatusCode == HttpStatusCode.Forbidden ? "Anda tidak memiliki akses." :
                    response.StatusCode == HttpStatusCode.NotFound ? "Data tidak ditemukan." : "Permintaan API gagal.";
                try { using var document = JsonDocument.Parse(json); if (document.RootElement.TryGetProperty("message", out var value)) message = value.GetString() ?? message; }
                catch (JsonException) { }
                throw new ApiException(response.StatusCode, message);
            }
            return string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (HttpRequestException ex) { throw new InvalidOperationException("API tidak dapat dihubungi. Periksa alamat dan koneksi API.", ex); }
        catch (TaskCanceledException ex) { throw new InvalidOperationException("Permintaan API melewati batas waktu.", ex); }
    }
}

internal sealed class ApiException(HttpStatusCode status, string message) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}

internal sealed record ApiAccount(int Id, string Name, string Email, string Role, string Status, DateTime CreatedAt);
internal sealed record ApiLogin(string Token, DateTimeOffset ExpiresAt, ApiAccount Account);
internal sealed record ApiSummary(int Id, int HazardId, string HazardName, DateTime CreatedAt, double OverallScore, string Level);
internal sealed record ApiSession(int Id, string HazardName, DateTime CreatedAt, double OverallScore, string Level, List<ApiItem> Checklist);
internal sealed record ApiAnswer(string DimensionName, string QuestionText, string SelectedAnswer, int Score);
internal sealed record ApiDimension(int DimensionId, string DimensionName, double Score, double PassingThreshold);
internal sealed record ApiItem(int Id, int AssessmentId, int RecommendationId, string Description, bool IsCompleted, DateTime? CompletedAt);
internal sealed record ApiRecommendation(int RecommendationId, int DimensionId, string Description, string Priority);
internal sealed record ApiDetail(ApiSummary Assessment, List<ApiAnswer> Answers, List<ApiDimension> Dimensions, List<ApiItem> Checklist, List<ApiRecommendation> Recommendations);
internal sealed record ApiQuestion(int Id, int DimensionId, string DimensionName, string Text, string? Description, List<ApiOption> Options);
internal sealed record ApiOption(int Id, string Text, int DisplayOrder);
internal sealed record ApiSubmission(int AssessmentId, double OverallScore, string Level, bool Created);
internal sealed record ApiHazard(int Id, string Name);
internal sealed record ApiEducation(int ContentId, int HazardId, string Title, string Body, string? SourceName, string? SourceUrl, DateTime PublishedAt);
internal sealed record ApiAdminEducation(int ContentId, int HazardId, string Title, string Body, string? SourceName,
    string? SourceUrl, bool IsPublished, DateTime? PublishedAt, DateTime CreatedAt, DateTime UpdatedAt, int? UpdatedBy,
    string? PublishedTitle, string? PublishedBody, string? PublishedSourceName, string? PublishedSourceUrl);
internal sealed record ApiMonitoringAssessment(int AssessmentId, DateTime CreatedAt, string UserName,
    string HazardName, double OverallScore, string Level);
internal sealed record ApiMonitoringReport(long TotalUsers, long ActiveUsers, long TotalAssessments,
    int Page, int PageSize, DateTimeOffset StartUtc, DateTimeOffset EndUtc, List<ApiMonitoringAssessment> Assessments);
