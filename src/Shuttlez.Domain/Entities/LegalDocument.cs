using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

public class LegalDocument : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public string? ContentEn { get; set; }
    public bool IsActive { get; set; } = true;
}
