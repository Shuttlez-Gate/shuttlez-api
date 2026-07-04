using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

public class LegalDocument : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
