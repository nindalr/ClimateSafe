namespace ClimateSafe.Api.Models;

public sealed record EducationContentRequest(string? Title, string? Body, string? SourceName, string? SourceUrl);
public sealed record CreateEducationContentRequest(int HazardId, string? Title, string? Body, string? SourceName, string? SourceUrl);

public sealed record AdminEducationContent(int ContentId, int HazardId, string Title, string Body,
    string? SourceName, string? SourceUrl, bool IsPublished, DateTime? PublishedAt,
    DateTime CreatedAt, DateTime UpdatedAt, int? UpdatedBy,
    string? PublishedTitle, string? PublishedBody, string? PublishedSourceName, string? PublishedSourceUrl);

public sealed record PublishedEducationContent(int ContentId, int HazardId, string Title, string Body,
    string? SourceName, string? SourceUrl, DateTime PublishedAt);
