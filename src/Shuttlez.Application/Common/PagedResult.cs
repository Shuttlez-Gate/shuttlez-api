namespace Shuttlez.Application.Common;

/// <summary>نتيجة مقسّمة لصفحات — تُستخدم في جميع قوائم لوحة التحكم.</summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize <= 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public static PagedResult<T> Empty(int page, int pageSize) =>
        new([], page, pageSize, 0);
}

/// <summary>معاملات الترقيم الموحّدة.</summary>
public readonly record struct PageRequest(int Page, int PageSize)
{
    public const int MaxPageSize = 200;

    public static PageRequest From(int? page, int? pageSize)
    {
        var safePage = page is null or < 1 ? 1 : page.Value;
        var safeSize = pageSize is null or < 1 ? 20 : Math.Min(pageSize.Value, MaxPageSize);
        return new PageRequest(safePage, safeSize);
    }

    public int Skip => (Page - 1) * PageSize;
}
