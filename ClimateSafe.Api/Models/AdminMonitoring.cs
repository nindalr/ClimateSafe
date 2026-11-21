namespace ClimateSafe.Api.Models;

public sealed record MonitoringAssessment(int AssessmentId, DateTime CreatedAt, string UserName,
    string HazardName, double OverallScore, string Level);

public sealed record MonitoringReport(long TotalUsers, long ActiveUsers, long TotalAssessments,
    int Page, int PageSize, DateTimeOffset StartUtc, DateTimeOffset EndUtc,
    IReadOnlyList<MonitoringAssessment> Assessments);
