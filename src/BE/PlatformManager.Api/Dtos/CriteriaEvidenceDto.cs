namespace PlatformManager.Api.Dtos;

public class CriteriaEvidenceDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}
