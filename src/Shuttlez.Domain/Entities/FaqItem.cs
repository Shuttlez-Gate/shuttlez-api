using Shuttlez.Domain.Common;

namespace Shuttlez.Domain.Entities;

public class FaqItem : BaseEntity
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
}
