using Shuttlez.Domain.Common;
using Shuttlez.Domain.Enums;

namespace Shuttlez.Domain.Entities;

public class DriverDocument : BaseEntity
{
    public Guid DriverId { get; set; }
    public DriverDocumentType DocumentType { get; set; } = DriverDocumentType.Other;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    /// <summary>مسار نسبي تحت /uploads أو URL مطلق.</summary>
    public string StoragePath { get; set; } = string.Empty;
    public string? Notes { get; set; }
    /// <summary>من رفع الملف: driver | admin</summary>
    public string UploadedBy { get; set; } = "driver";

    public Driver Driver { get; set; } = null!;
}
