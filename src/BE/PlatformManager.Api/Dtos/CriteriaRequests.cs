namespace PlatformManager.Api.Dtos;

public class CreateCriteriaRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public decimal MaxScore { get; set; }
}

public class UpdateCriteriaRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public decimal MaxScore { get; set; }
}

public class UpsertAssessmentRequest
{
    public decimal? ProgressPercent { get; set; }
    public string? Note { get; set; }
}

public class CriteriaDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
}
